using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using VapeShopPos.Data;
using VapeShopPos.Models;

namespace VapeShopPos.Services
{
    /// <summary>
    /// Products, categories and all stock changes. Stock is NEVER changed by
    /// writing current_quantity directly from the outside: every change goes
    /// through RecordMovement, which writes a stock_movements row and keeps
    /// current_quantity in sync inside the same transaction.
    /// </summary>
    public static class ProductService
    {
        // ---------------------------------------------------------------- Categories
        public static List<Category> GetCategories()
        {
            var list = new List<Category>();
            using (var conn = Database.GetConnection())
            using (var cmd = new SQLiteCommand("SELECT id, name FROM categories ORDER BY name;", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                    list.Add(new Category { Id = r.GetInt64(0), Name = r.GetString(1) });
            }
            return list;
        }

        public static long AddCategory(string name)
        {
            using (var conn = Database.GetConnection())
            {
                Database.Execute(conn, "INSERT INTO categories (name) VALUES (@n);",
                    p => p.AddWithValue("@n", name));
                return conn.LastInsertRowId;
            }
        }

        public static void DeleteCategory(long id)
        {
            using (var conn = Database.GetConnection())
                Database.Execute(conn, "DELETE FROM categories WHERE id=@id;",
                    p => p.AddWithValue("@id", id));
        }

        // ---------------------------------------------------------------- Products
        public static List<Product> GetProducts(string search = null, long? categoryId = null, bool lowStockOnly = false)
        {
            var list = new List<Product>();
            string sql = @"SELECT p.id, p.name, p.barcode, p.category_id, c.name,
                                  p.purchase_price, p.sale_price, p.current_quantity, p.min_stock_level
                           FROM products p
                           LEFT JOIN categories c ON c.id = p.category_id
                           WHERE 1=1";
            if (!string.IsNullOrWhiteSpace(search))
                sql += " AND (p.name LIKE @s OR p.barcode LIKE @s)";
            if (categoryId.HasValue)
                sql += " AND p.category_id = @cid";
            if (lowStockOnly)
                sql += " AND p.current_quantity <= p.min_stock_level";
            sql += " ORDER BY p.name;";

            using (var conn = Database.GetConnection())
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                if (!string.IsNullOrWhiteSpace(search))
                    cmd.Parameters.AddWithValue("@s", "%" + search.Trim() + "%");
                if (categoryId.HasValue)
                    cmd.Parameters.AddWithValue("@cid", categoryId.Value);
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(ReadProduct(r));
            }
            return list;
        }

        public static Product GetProductById(long id)
        {
            return QuerySingleProduct("WHERE p.id = @v", id);
        }

        public static Product GetProductByBarcode(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode)) return null;
            return QuerySingleProduct("WHERE p.barcode = @v", barcode.Trim());
        }

        private static Product QuerySingleProduct(string whereClause, object value)
        {
            string sql = @"SELECT p.id, p.name, p.barcode, p.category_id, c.name,
                                  p.purchase_price, p.sale_price, p.current_quantity, p.min_stock_level
                           FROM products p
                           LEFT JOIN categories c ON c.id = p.category_id " + whereClause + " LIMIT 1;";
            using (var conn = Database.GetConnection())
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@v", value);
                using (var r = cmd.ExecuteReader())
                    return r.Read() ? ReadProduct(r) : null;
            }
        }

        private static Product ReadProduct(SQLiteDataReader r)
        {
            return new Product
            {
                Id = r.GetInt64(0),
                Name = r.GetString(1),
                Barcode = r.IsDBNull(2) ? null : r.GetString(2),
                CategoryId = r.IsDBNull(3) ? (long?)null : r.GetInt64(3),
                CategoryName = r.IsDBNull(4) ? null : r.GetString(4),
                PurchasePrice = Convert.ToDecimal(r.GetValue(5)),
                SalePrice = Convert.ToDecimal(r.GetValue(6)),
                CurrentQuantity = r.GetInt32(7),
                MinStockLevel = r.GetInt32(8)
            };
        }

        /// <summary>
        /// Adds a product. If openingQty &gt; 0, also records an opening-stock
        /// purchase movement so current_quantity reconciles with movements.
        /// </summary>
        public static long AddProduct(Product p, int openingQty, long userId)
        {
            using (var conn = Database.GetConnection())
            using (var tx = conn.BeginTransaction())
            {
                Database.Execute(conn,
                    @"INSERT INTO products (name, barcode, category_id, purchase_price, sale_price, current_quantity, min_stock_level)
                      VALUES (@n, @b, @c, @pp, @sp, 0, @min);",
                    pr =>
                    {
                        pr.AddWithValue("@n", p.Name);
                        pr.AddWithValue("@b", NullIfEmpty(p.Barcode));
                        pr.AddWithValue("@c", (object)p.CategoryId ?? DBNull.Value);
                        pr.AddWithValue("@pp", p.PurchasePrice);
                        pr.AddWithValue("@sp", p.SalePrice);
                        pr.AddWithValue("@min", p.MinStockLevel);
                    }, tx);
                long id = conn.LastInsertRowId;

                if (openingQty != 0)
                    RecordMovement(conn, tx, id, openingQty, MovementTypes.Purchase, "رصيد افتتاحي", userId);

                tx.Commit();
                return id;
            }
        }

        /// <summary>Updates product fields. Does NOT change stock (use Restock/stock-take).</summary>
        public static void UpdateProduct(Product p)
        {
            using (var conn = Database.GetConnection())
                Database.Execute(conn,
                    @"UPDATE products SET name=@n, barcode=@b, category_id=@c,
                             purchase_price=@pp, sale_price=@sp, min_stock_level=@min
                      WHERE id=@id;",
                    pr =>
                    {
                        pr.AddWithValue("@n", p.Name);
                        pr.AddWithValue("@b", NullIfEmpty(p.Barcode));
                        pr.AddWithValue("@c", (object)p.CategoryId ?? DBNull.Value);
                        pr.AddWithValue("@pp", p.PurchasePrice);
                        pr.AddWithValue("@sp", p.SalePrice);
                        pr.AddWithValue("@min", p.MinStockLevel);
                        pr.AddWithValue("@id", p.Id);
                    });
        }

        public static void DeleteProduct(long id)
        {
            using (var conn = Database.GetConnection())
                Database.Execute(conn, "DELETE FROM products WHERE id=@id;",
                    p => p.AddWithValue("@id", id));
        }

        public static bool HasHistory(long productId)
        {
            using (var conn = Database.GetConnection())
            using (var cmd = new SQLiteCommand(
                "SELECT (SELECT COUNT(*) FROM invoice_items WHERE product_id=@id) + " +
                "(SELECT COUNT(*) FROM stock_movements WHERE product_id=@id);", conn))
            {
                cmd.Parameters.AddWithValue("@id", productId);
                return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
            }
        }

        // ---------------------------------------------------------------- Stock changes
        public static void Restock(long productId, int qty, string note, long userId)
        {
            if (qty <= 0) throw new ArgumentException("الكمية يجب أن تكون أكبر من صفر.");
            using (var conn = Database.GetConnection())
            using (var tx = conn.BeginTransaction())
            {
                RecordMovement(conn, tx, productId, qty, MovementTypes.Purchase, note, userId);
                tx.Commit();
            }
        }

        public static void RecordDamage(long productId, int qty, string note, long userId)
        {
            if (qty <= 0) throw new ArgumentException("الكمية يجب أن تكون أكبر من صفر.");
            using (var conn = Database.GetConnection())
            using (var tx = conn.BeginTransaction())
            {
                RecordMovement(conn, tx, productId, -qty, MovementTypes.Damage, note, userId);
                tx.Commit();
            }
        }

        /// <summary>
        /// THE single choke-point for stock changes. Inserts a stock_movements row
        /// and applies the same delta to products.current_quantity, all inside the
        /// caller's transaction so the two can never drift apart.
        /// </summary>
        public static void RecordMovement(SQLiteConnection conn, SQLiteTransaction tx,
            long productId, int changeQty, string movementType, string reference, long userId)
        {
            Database.Execute(conn,
                @"INSERT INTO stock_movements (product_id, datetime, change_qty, movement_type, reference, user_id)
                  VALUES (@pid, @dt, @qty, @type, @ref, @uid);",
                p =>
                {
                    p.AddWithValue("@pid", productId);
                    p.AddWithValue("@dt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    p.AddWithValue("@qty", changeQty);
                    p.AddWithValue("@type", movementType);
                    p.AddWithValue("@ref", (object)reference ?? DBNull.Value);
                    p.AddWithValue("@uid", userId);
                }, tx);

            Database.Execute(conn,
                "UPDATE products SET current_quantity = current_quantity + @qty WHERE id=@pid;",
                p =>
                {
                    p.AddWithValue("@qty", changeQty);
                    p.AddWithValue("@pid", productId);
                }, tx);
        }

        /// <summary>Theoretical on-hand = sum of all movements for the product.</summary>
        public static int GetTheoreticalQuantity(long productId)
        {
            using (var conn = Database.GetConnection())
            using (var cmd = new SQLiteCommand(
                "SELECT COALESCE(SUM(change_qty), 0) FROM stock_movements WHERE product_id=@id;", conn))
            {
                cmd.Parameters.AddWithValue("@id", productId);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public static List<Product> GetLowStockProducts()
        {
            return GetProducts(lowStockOnly: true);
        }

        public static List<StockMovement> GetMovements(long productId)
        {
            var list = new List<StockMovement>();
            using (var conn = Database.GetConnection())
            using (var cmd = new SQLiteCommand(
                @"SELECT id, product_id, datetime, change_qty, movement_type, reference, user_id
                  FROM stock_movements WHERE product_id=@id ORDER BY datetime DESC, id DESC;", conn))
            {
                cmd.Parameters.AddWithValue("@id", productId);
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new StockMovement
                        {
                            Id = r.GetInt64(0),
                            ProductId = r.GetInt64(1),
                            DateTime = DateTime.Parse(r.GetString(2), CultureInfo.InvariantCulture),
                            ChangeQty = r.GetInt32(3),
                            MovementType = r.GetString(4),
                            Reference = r.IsDBNull(5) ? null : r.GetString(5),
                            UserId = r.IsDBNull(6) ? 0 : r.GetInt64(6)
                        });
            }
            return list;
        }

        private static object NullIfEmpty(string s)
        {
            return string.IsNullOrWhiteSpace(s) ? (object)DBNull.Value : s.Trim();
        }
    }
}

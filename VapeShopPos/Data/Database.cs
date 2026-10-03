using System;
using System.Configuration;
using System.Data.SQLite;
using System.IO;
using VapeShopPos.Models;
using VapeShopPos.Services;

namespace VapeShopPos.Data
{
    /// <summary>
    /// Owns the single local SQLite file: builds the connection string, creates the
    /// schema on first run and seeds sample data. Every other class gets its
    /// connections from here.
    /// </summary>
    public static class Database
    {
        private static string _dbPath;

        /// <summary>Full path to the .db file on disk (set by Initialize).</summary>
        public static string DatabasePath { get { return _dbPath; } }

        /// <summary>
        /// Resolve the database path, create the file + schema if it does not exist,
        /// and seed sample data. Call once at startup before any other DB access.
        /// </summary>
        public static void Initialize()
        {
            string fileName = ConfigurationManager.AppSettings["DatabaseFileName"];
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = "vapeshop.db";

            // The database lives next to the executable so it is easy to find/back up.
            _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

            bool isNew = !File.Exists(_dbPath);
            if (isNew)
                SQLiteConnection.CreateFile(_dbPath);

            using (var conn = GetConnection())
            {
                CreateSchema(conn);
                if (isNew)
                    Seed(conn);
            }
        }

        /// <summary>Returns a freshly opened connection with foreign keys enforced.</summary>
        public static SQLiteConnection GetConnection()
        {
            var builder = new SQLiteConnectionStringBuilder
            {
                DataSource = _dbPath,
                ForeignKeys = true,
                // Busy timeout keeps the single-user app from throwing on brief locks.
                BusyTimeout = 3000,
                DateTimeFormat = SQLiteDateFormats.ISO8601
            };
            var conn = new SQLiteConnection(builder.ToString());
            conn.Open();
            return conn;
        }

        /// <summary>Convenience helper to run a non-query statement.</summary>
        public static int Execute(SQLiteConnection conn, string sql, Action<SQLiteParameterCollection> bind = null, SQLiteTransaction tx = null)
        {
            using (var cmd = new SQLiteCommand(sql, conn, tx))
            {
                if (bind != null) bind(cmd.Parameters);
                return cmd.ExecuteNonQuery();
            }
        }

        private static void CreateSchema(SQLiteConnection conn)
        {
            const string sql = @"
CREATE TABLE IF NOT EXISTS categories (
    id   INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS products (
    id               INTEGER PRIMARY KEY AUTOINCREMENT,
    name             TEXT NOT NULL,
    barcode          TEXT UNIQUE,
    category_id      INTEGER,
    purchase_price   REAL NOT NULL DEFAULT 0,
    sale_price       REAL NOT NULL DEFAULT 0,
    current_quantity INTEGER NOT NULL DEFAULT 0,
    min_stock_level  INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (category_id) REFERENCES categories(id)
);

CREATE TABLE IF NOT EXISTS users (
    id            INTEGER PRIMARY KEY AUTOINCREMENT,
    username      TEXT NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    role          TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS shifts (
    id           INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id      INTEGER NOT NULL,
    opened_at    TEXT NOT NULL,
    closed_at    TEXT,
    opening_cash REAL NOT NULL DEFAULT 0,
    closing_cash REAL,
    total_sales  REAL NOT NULL DEFAULT 0,
    FOREIGN KEY (user_id) REFERENCES users(id)
);

CREATE TABLE IF NOT EXISTS invoices (
    id             INTEGER PRIMARY KEY AUTOINCREMENT,
    datetime       TEXT NOT NULL,
    subtotal       REAL NOT NULL,
    discount       REAL NOT NULL DEFAULT 0,
    total          REAL NOT NULL,
    payment_method TEXT NOT NULL,
    shift_id       INTEGER NOT NULL,
    user_id        INTEGER NOT NULL,
    FOREIGN KEY (shift_id) REFERENCES shifts(id),
    FOREIGN KEY (user_id)  REFERENCES users(id)
);

CREATE TABLE IF NOT EXISTS invoice_items (
    id         INTEGER PRIMARY KEY AUTOINCREMENT,
    invoice_id INTEGER NOT NULL,
    product_id INTEGER NOT NULL,
    quantity   INTEGER NOT NULL,
    unit_price REAL NOT NULL,
    FOREIGN KEY (invoice_id) REFERENCES invoices(id),
    FOREIGN KEY (product_id) REFERENCES products(id)
);

CREATE TABLE IF NOT EXISTS stock_movements (
    id            INTEGER PRIMARY KEY AUTOINCREMENT,
    product_id    INTEGER NOT NULL,
    datetime      TEXT NOT NULL,
    change_qty    INTEGER NOT NULL,
    movement_type TEXT NOT NULL,
    reference     TEXT,
    user_id       INTEGER,
    FOREIGN KEY (product_id) REFERENCES products(id),
    FOREIGN KEY (user_id)    REFERENCES users(id)
);

CREATE TABLE IF NOT EXISTS cash_movements (
    id        INTEGER PRIMARY KEY AUTOINCREMENT,
    datetime  TEXT NOT NULL,
    direction TEXT NOT NULL,            -- 'in' = deposit to drawer, 'out' = withdraw from drawer
    amount    REAL NOT NULL,
    service   TEXT,
    shift_id  INTEGER NOT NULL,
    user_id   INTEGER NOT NULL,
    FOREIGN KEY (shift_id) REFERENCES shifts(id),
    FOREIGN KEY (user_id)  REFERENCES users(id)
);

CREATE INDEX IF NOT EXISTS idx_cash_shift        ON cash_movements(shift_id);
CREATE INDEX IF NOT EXISTS idx_movements_product ON stock_movements(product_id);
CREATE INDEX IF NOT EXISTS idx_items_invoice     ON invoice_items(invoice_id);
CREATE INDEX IF NOT EXISTS idx_invoices_shift    ON invoices(shift_id);
CREATE INDEX IF NOT EXISTS idx_products_barcode  ON products(barcode);
";
            Execute(conn, sql);
        }

        private static void Seed(SQLiteConnection conn)
        {
            using (var tx = conn.BeginTransaction())
            {
                // ---- Categories ----
                long catFlavors = InsertCategory(conn, tx, "نكهات");
                long catDevices = InsertCategory(conn, tx, "أجهزة");
                long catCoils = InsertCategory(conn, tx, "كويلات");
                long catAccessories = InsertCategory(conn, tx, "إكسسوارات");

                // ---- Users ----  (default passwords - change after first login)
                InsertUser(conn, tx, "admin", "admin123", Roles.Manager);
                InsertUser(conn, tx, "cashier", "cashier123", Roles.Cashier);

                // ---- Products (with opening stock as a 'purchase' movement) ----
                SeedProduct(conn, tx, "ليكويد فراولة 60ml", "6221000000011", catFlavors, 90, 150, 20, 5);
                SeedProduct(conn, tx, "ليكويد مانجو 60ml", "6221000000028", catFlavors, 90, 150, 15, 5);
                SeedProduct(conn, tx, "جهاز Vaporesso XROS", "6221000000035", catDevices, 450, 700, 8, 2);
                SeedProduct(conn, tx, "جهاز SMOK Nord 5", "6221000000042", catDevices, 600, 950, 5, 2);
                SeedProduct(conn, tx, "كويل 0.8 أوم (علبة 5)", "6221000000059", catCoils, 120, 200, 30, 8);
                SeedProduct(conn, tx, "كويل 0.6 أوم (علبة 5)", "6221000000066", catCoils, 120, 200, 25, 8);
                SeedProduct(conn, tx, "شاحن Type-C", "6221000000073", catAccessories, 40, 75, 40, 10);
                SeedProduct(conn, tx, "بطارية 18650", "6221000000080", catAccessories, 110, 180, 12, 4);

                tx.Commit();
            }
        }

        private static long InsertCategory(SQLiteConnection conn, SQLiteTransaction tx, string name)
        {
            Execute(conn, "INSERT INTO categories (name) VALUES (@n);",
                p => p.AddWithValue("@n", name), tx);
            return conn.LastInsertRowId;
        }

        private static long InsertUser(SQLiteConnection conn, SQLiteTransaction tx, string username, string password, string role)
        {
            Execute(conn,
                "INSERT INTO users (username, password_hash, role) VALUES (@u, @h, @r);",
                p =>
                {
                    p.AddWithValue("@u", username);
                    p.AddWithValue("@h", PasswordHasher.Hash(password));
                    p.AddWithValue("@r", role);
                }, tx);
            return conn.LastInsertRowId;
        }

        private static void SeedProduct(SQLiteConnection conn, SQLiteTransaction tx, string name, string barcode,
            long categoryId, decimal purchase, decimal sale, int openingQty, int minStock)
        {
            Execute(conn,
                @"INSERT INTO products (name, barcode, category_id, purchase_price, sale_price, current_quantity, min_stock_level)
                  VALUES (@n, @b, @c, @pp, @sp, @q, @min);",
                p =>
                {
                    p.AddWithValue("@n", name);
                    p.AddWithValue("@b", barcode);
                    p.AddWithValue("@c", categoryId);
                    p.AddWithValue("@pp", purchase);
                    p.AddWithValue("@sp", sale);
                    p.AddWithValue("@q", openingQty);
                    p.AddWithValue("@min", minStock);
                }, tx);
            long productId = conn.LastInsertRowId;

            if (openingQty != 0)
            {
                Execute(conn,
                    @"INSERT INTO stock_movements (product_id, datetime, change_qty, movement_type, reference, user_id)
                      VALUES (@pid, @dt, @qty, @type, @ref, @uid);",
                    p =>
                    {
                        p.AddWithValue("@pid", productId);
                        p.AddWithValue("@dt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        p.AddWithValue("@qty", openingQty);
                        p.AddWithValue("@type", MovementTypes.Purchase);
                        p.AddWithValue("@ref", "رصيد افتتاحي");
                        p.AddWithValue("@uid", 1);
                    }, tx);
            }
        }
    }
}

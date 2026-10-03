using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using VapeShopPos.Data;
using VapeShopPos.Models;

namespace VapeShopPos.Services
{
    public static class ShiftService
    {
        public static Shift OpenShift(long userId, decimal openingCash)
        {
            using (var conn = Database.GetConnection())
            {
                Database.Execute(conn,
                    @"INSERT INTO shifts (user_id, opened_at, opening_cash, total_sales)
                      VALUES (@uid, @opened, @cash, 0);",
                    p =>
                    {
                        p.AddWithValue("@uid", userId);
                        p.AddWithValue("@opened", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        p.AddWithValue("@cash", openingCash);
                    });
                return GetShiftById(conn.LastInsertRowId);
            }
        }

        public static Shift GetOpenShiftForUser(long userId)
        {
            using (var conn = Database.GetConnection())
            using (var cmd = new SQLiteCommand(
                @"SELECT s.id, s.user_id, u.username, s.opened_at, s.closed_at,
                         s.opening_cash, s.closing_cash, s.total_sales
                  FROM shifts s JOIN users u ON u.id = s.user_id
                  WHERE s.user_id=@uid AND s.closed_at IS NULL
                  ORDER BY s.id DESC LIMIT 1;", conn))
            {
                cmd.Parameters.AddWithValue("@uid", userId);
                using (var r = cmd.ExecuteReader())
                    return r.Read() ? ReadShift(r) : null;
            }
        }

        public static Shift GetShiftById(long id)
        {
            using (var conn = Database.GetConnection())
            using (var cmd = new SQLiteCommand(
                @"SELECT s.id, s.user_id, u.username, s.opened_at, s.closed_at,
                         s.opening_cash, s.closing_cash, s.total_sales
                  FROM shifts s JOIN users u ON u.id = s.user_id
                  WHERE s.id=@id;", conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                using (var r = cmd.ExecuteReader())
                    return r.Read() ? ReadShift(r) : null;
            }
        }

        /// <summary>Total of all sales (cash + card) recorded against this shift.</summary>
        public static decimal GetShiftSalesTotal(long shiftId)
        {
            return ScalarDecimal("SELECT COALESCE(SUM(total),0) FROM invoices WHERE shift_id=@id;", shiftId);
        }

        /// <summary>Only cash sales - used to compute expected cash in drawer.</summary>
        public static decimal GetShiftCashSalesTotal(long shiftId)
        {
            return ScalarDecimal(
                "SELECT COALESCE(SUM(total),0) FROM invoices WHERE shift_id=@id AND payment_method='cash';",
                shiftId);
        }

        public static int GetShiftInvoiceCount(long shiftId)
        {
            using (var conn = Database.GetConnection())
            using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM invoices WHERE shift_id=@id;", conn))
            {
                cmd.Parameters.AddWithValue("@id", shiftId);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Closes the shift: stores actual counted cash, total sales and the closing
        /// time, then locks it. Returns the finalized shift so the UI can show the
        /// variance (expected vs counted).
        /// </summary>
        public static Shift CloseShift(long shiftId, decimal actualCountedCash)
        {
            decimal totalSales = GetShiftSalesTotal(shiftId);
            using (var conn = Database.GetConnection())
            {
                Database.Execute(conn,
                    @"UPDATE shifts SET closed_at=@closed, closing_cash=@cash, total_sales=@sales
                      WHERE id=@id AND closed_at IS NULL;",
                    p =>
                    {
                        p.AddWithValue("@closed", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        p.AddWithValue("@cash", actualCountedCash);
                        p.AddWithValue("@sales", totalSales);
                        p.AddWithValue("@id", shiftId);
                    });
            }
            return GetShiftById(shiftId);
        }

        public static List<Shift> GetShifts(int limit = 100)
        {
            var list = new List<Shift>();
            using (var conn = Database.GetConnection())
            using (var cmd = new SQLiteCommand(
                @"SELECT s.id, s.user_id, u.username, s.opened_at, s.closed_at,
                         s.opening_cash, s.closing_cash, s.total_sales
                  FROM shifts s JOIN users u ON u.id = s.user_id
                  ORDER BY s.id DESC LIMIT @lim;", conn))
            {
                cmd.Parameters.AddWithValue("@lim", limit);
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(ReadShift(r));
            }
            return list;
        }

        /// <summary>All invoices belonging to a shift, newest first.</summary>
        public static List<Invoice> GetShiftInvoices(long shiftId)
        {
            var list = new List<Invoice>();
            using (var conn = Database.GetConnection())
            using (var cmd = new SQLiteCommand(
                @"SELECT id, datetime, subtotal, discount, total, payment_method, shift_id, user_id
                  FROM invoices WHERE shift_id=@id ORDER BY id DESC;", conn))
            {
                cmd.Parameters.AddWithValue("@id", shiftId);
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new Invoice
                        {
                            Id = r.GetInt64(0),
                            DateTime = DateTime.Parse(r.GetString(1), CultureInfo.InvariantCulture),
                            Subtotal = Convert.ToDecimal(r.GetValue(2)),
                            Discount = Convert.ToDecimal(r.GetValue(3)),
                            Total = Convert.ToDecimal(r.GetValue(4)),
                            PaymentMethod = r.GetString(5),
                            ShiftId = r.GetInt64(6),
                            UserId = r.GetInt64(7)
                        });
            }
            return list;
        }

        private static decimal ScalarDecimal(string sql, long id)
        {
            using (var conn = Database.GetConnection())
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                return Convert.ToDecimal(cmd.ExecuteScalar());
            }
        }

        private static Shift ReadShift(SQLiteDataReader r)
        {
            return new Shift
            {
                Id = r.GetInt64(0),
                UserId = r.GetInt64(1),
                UserName = r.GetString(2),
                OpenedAt = DateTime.Parse(r.GetString(3), CultureInfo.InvariantCulture),
                ClosedAt = r.IsDBNull(4) ? (DateTime?)null : DateTime.Parse(r.GetString(4), CultureInfo.InvariantCulture),
                OpeningCash = Convert.ToDecimal(r.GetValue(5)),
                ClosingCash = r.IsDBNull(6) ? (decimal?)null : Convert.ToDecimal(r.GetValue(6)),
                TotalSales = Convert.ToDecimal(r.GetValue(7))
            };
        }
    }
}

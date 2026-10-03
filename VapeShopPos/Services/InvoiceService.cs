using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.Text;
using VapeShopPos.Data;
using VapeShopPos.Models;

namespace VapeShopPos.Services
{
    /// <summary>
    /// Read-only querying of invoices and their line items, with optional filters
    /// (date range, cashier, payment method) for the standalone Invoices screen.
    /// </summary>
    public static class InvoiceService
    {
        public static Invoice GetInvoiceById(long id)
        {
            using (var conn = Database.GetConnection())
            using (var cmd = new SQLiteCommand(
                @"SELECT i.id, i.datetime, i.subtotal, i.discount, i.total,
                         i.payment_method, i.shift_id, i.user_id, u.username
                  FROM invoices i LEFT JOIN users u ON u.id = i.user_id
                  WHERE i.id=@id;", conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                using (var r = cmd.ExecuteReader())
                    return r.Read() ? ReadInvoice(r) : null;
            }
        }

        /// <summary>Line items of an invoice, including the product name.</summary>
        public static List<InvoiceItem> GetInvoiceItems(long invoiceId)
        {
            var list = new List<InvoiceItem>();
            using (var conn = Database.GetConnection())
            using (var cmd = new SQLiteCommand(
                @"SELECT it.id, it.invoice_id, it.product_id,
                         COALESCE(p.name, 'صنف محذوف') AS pname,
                         it.quantity, it.unit_price
                  FROM invoice_items it
                  LEFT JOIN products p ON p.id = it.product_id
                  WHERE it.invoice_id=@id
                  ORDER BY it.id;", conn))
            {
                cmd.Parameters.AddWithValue("@id", invoiceId);
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new InvoiceItem
                        {
                            Id = r.GetInt64(0),
                            InvoiceId = r.GetInt64(1),
                            ProductId = r.GetInt64(2),
                            ProductName = r.GetString(3),
                            Quantity = r.GetInt32(4),
                            UnitPrice = Convert.ToDecimal(r.GetValue(5))
                        });
            }
            return list;
        }

        /// <summary>
        /// All invoices matching the given optional filters, newest first.
        /// Pass null to ignore a filter. Dates are inclusive by day.
        /// </summary>
        public static List<Invoice> GetInvoices(DateTime? from, DateTime? to,
            long? userId, string paymentMethod, int limit = 1000)
        {
            var list = new List<Invoice>();
            var sql = new StringBuilder(
                @"SELECT i.id, i.datetime, i.subtotal, i.discount, i.total,
                         i.payment_method, i.shift_id, i.user_id, u.username
                  FROM invoices i LEFT JOIN users u ON u.id = i.user_id
                  WHERE 1=1");

            if (from.HasValue) sql.Append(" AND i.datetime >= @from");
            if (to.HasValue) sql.Append(" AND i.datetime <= @to");
            if (userId.HasValue) sql.Append(" AND i.user_id = @uid");
            if (!string.IsNullOrEmpty(paymentMethod)) sql.Append(" AND i.payment_method = @pm");
            sql.Append(" ORDER BY i.id DESC LIMIT @lim;");

            using (var conn = Database.GetConnection())
            using (var cmd = new SQLiteCommand(sql.ToString(), conn))
            {
                if (from.HasValue)
                    cmd.Parameters.AddWithValue("@from", from.Value.ToString("yyyy-MM-dd 00:00:00"));
                if (to.HasValue)
                    cmd.Parameters.AddWithValue("@to", to.Value.ToString("yyyy-MM-dd 23:59:59"));
                if (userId.HasValue)
                    cmd.Parameters.AddWithValue("@uid", userId.Value);
                if (!string.IsNullOrEmpty(paymentMethod))
                    cmd.Parameters.AddWithValue("@pm", paymentMethod);
                cmd.Parameters.AddWithValue("@lim", limit);

                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(ReadInvoice(r));
            }
            return list;
        }

        private static Invoice ReadInvoice(SQLiteDataReader r)
        {
            return new Invoice
            {
                Id = r.GetInt64(0),
                DateTime = DateTime.Parse(r.GetString(1), CultureInfo.InvariantCulture),
                Subtotal = Convert.ToDecimal(r.GetValue(2)),
                Discount = Convert.ToDecimal(r.GetValue(3)),
                Total = Convert.ToDecimal(r.GetValue(4)),
                PaymentMethod = r.GetString(5),
                ShiftId = r.GetInt64(6),
                UserId = r.GetInt64(7),
                UserName = r.IsDBNull(8) ? "-" : r.GetString(8)
            };
        }
    }
}

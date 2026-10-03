using System;
using System.Collections.Generic;
using System.Data.SQLite;
using VapeShopPos.Data;
using VapeShopPos.Models;

namespace VapeShopPos.Services
{
    public static class SalesService
    {
        /// <summary>
        /// Finalizes a sale atomically: writes the invoice, its line items, and one
        /// negative 'sale' stock movement per line (which also decrements
        /// current_quantity via ProductService.RecordMovement). Everything is in a
        /// single transaction, so a failure leaves no partial sale behind.
        /// </summary>
        public static Invoice CreateInvoice(IEnumerable<InvoiceItem> items, decimal discount,
            string paymentMethod, long shiftId, long userId)
        {
            var lineItems = new List<InvoiceItem>(items);
            if (lineItems.Count == 0)
                throw new InvalidOperationException("لا توجد أصناف في الفاتورة.");

            decimal subtotal = 0;
            foreach (var it in lineItems)
                subtotal += it.LineTotal;

            if (discount < 0) discount = 0;
            if (discount > subtotal) discount = subtotal;
            decimal total = subtotal - discount;

            using (var conn = Database.GetConnection())
            using (var tx = conn.BeginTransaction())
            {
                Database.Execute(conn,
                    @"INSERT INTO invoices (datetime, subtotal, discount, total, payment_method, shift_id, user_id)
                      VALUES (@dt, @sub, @disc, @total, @pm, @shift, @uid);",
                    p =>
                    {
                        p.AddWithValue("@dt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        p.AddWithValue("@sub", subtotal);
                        p.AddWithValue("@disc", discount);
                        p.AddWithValue("@total", total);
                        p.AddWithValue("@pm", paymentMethod);
                        p.AddWithValue("@shift", shiftId);
                        p.AddWithValue("@uid", userId);
                    }, tx);
                long invoiceId = conn.LastInsertRowId;

                foreach (var it in lineItems)
                {
                    Database.Execute(conn,
                        @"INSERT INTO invoice_items (invoice_id, product_id, quantity, unit_price)
                          VALUES (@inv, @pid, @qty, @price);",
                        p =>
                        {
                            p.AddWithValue("@inv", invoiceId);
                            p.AddWithValue("@pid", it.ProductId);
                            p.AddWithValue("@qty", it.Quantity);
                            p.AddWithValue("@price", it.UnitPrice);
                        }, tx);

                    // Negative movement decrements stock; reference links back to invoice.
                    ProductService.RecordMovement(conn, tx, it.ProductId, -it.Quantity,
                        MovementTypes.Sale, "فاتورة #" + invoiceId, userId);
                }

                tx.Commit();

                return new Invoice
                {
                    Id = invoiceId,
                    DateTime = DateTime.Now,
                    Subtotal = subtotal,
                    Discount = discount,
                    Total = total,
                    PaymentMethod = paymentMethod,
                    ShiftId = shiftId,
                    UserId = userId,
                    Items = lineItems
                };
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using VapeShopPos.Data;

namespace VapeShopPos.Services
{
    public class DailySummary
    {
        public DateTime Date { get; set; }
        public int InvoiceCount { get; set; }
        public decimal TotalSales { get; set; }
        public decimal CashSales { get; set; }
        public decimal CardSales { get; set; }
        public decimal TotalDiscount { get; set; }
    }

    public class BestSellerRow
    {
        public string ProductName { get; set; }
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class ProfitSummary
    {
        public decimal Revenue { get; set; }   // sum(unit_price * qty)
        public decimal Cost { get; set; }       // sum(purchase_price * qty)
        public decimal Profit { get { return Revenue - Cost; } }
    }

    public static class ReportService
    {
        /// <summary>Sales summary for a single calendar day.</summary>
        public static DailySummary GetDailySummary(DateTime day)
        {
            string start = day.Date.ToString("yyyy-MM-dd 00:00:00");
            string end = day.Date.ToString("yyyy-MM-dd 23:59:59");

            var s = new DailySummary { Date = day.Date };
            using (var conn = Database.GetConnection())
            using (var cmd = new SQLiteCommand(
                @"SELECT COUNT(*),
                         COALESCE(SUM(total),0),
                         COALESCE(SUM(CASE WHEN payment_method='cash' THEN total ELSE 0 END),0),
                         COALESCE(SUM(CASE WHEN payment_method='card' THEN total ELSE 0 END),0),
                         COALESCE(SUM(discount),0)
                  FROM invoices
                  WHERE datetime BETWEEN @start AND @end;", conn))
            {
                cmd.Parameters.AddWithValue("@start", start);
                cmd.Parameters.AddWithValue("@end", end);
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        s.InvoiceCount = r.GetInt32(0);
                        s.TotalSales = Convert.ToDecimal(r.GetValue(1));
                        s.CashSales = Convert.ToDecimal(r.GetValue(2));
                        s.CardSales = Convert.ToDecimal(r.GetValue(3));
                        s.TotalDiscount = Convert.ToDecimal(r.GetValue(4));
                    }
                }
            }
            return s;
        }

        /// <summary>Best-selling products in a date range, ordered by quantity sold.</summary>
        public static List<BestSellerRow> GetBestSellers(DateTime from, DateTime to, int topN = 20)
        {
            var list = new List<BestSellerRow>();
            using (var conn = Database.GetConnection())
            using (var cmd = new SQLiteCommand(
                @"SELECT p.name, SUM(ii.quantity) AS qty, SUM(ii.quantity * ii.unit_price) AS revenue
                  FROM invoice_items ii
                  JOIN invoices i ON i.id = ii.invoice_id
                  JOIN products p ON p.id = ii.product_id
                  WHERE i.datetime BETWEEN @start AND @end
                  GROUP BY ii.product_id
                  ORDER BY qty DESC
                  LIMIT @top;", conn))
            {
                cmd.Parameters.AddWithValue("@start", from.Date.ToString("yyyy-MM-dd 00:00:00"));
                cmd.Parameters.AddWithValue("@end", to.Date.ToString("yyyy-MM-dd 23:59:59"));
                cmd.Parameters.AddWithValue("@top", topN);
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new BestSellerRow
                        {
                            ProductName = r.GetString(0),
                            QuantitySold = Convert.ToInt32(r.GetValue(1)),
                            Revenue = Convert.ToDecimal(r.GetValue(2))
                        });
            }
            return list;
        }

        /// <summary>
        /// Profit in a date range. Revenue uses the price captured on each sale;
        /// cost uses the product's current purchase_price.
        /// </summary>
        public static ProfitSummary GetProfit(DateTime from, DateTime to)
        {
            var s = new ProfitSummary();
            using (var conn = Database.GetConnection())
            using (var cmd = new SQLiteCommand(
                @"SELECT COALESCE(SUM(ii.quantity * ii.unit_price),0),
                         COALESCE(SUM(ii.quantity * p.purchase_price),0)
                  FROM invoice_items ii
                  JOIN invoices i ON i.id = ii.invoice_id
                  JOIN products p ON p.id = ii.product_id
                  WHERE i.datetime BETWEEN @start AND @end;", conn))
            {
                cmd.Parameters.AddWithValue("@start", from.Date.ToString("yyyy-MM-dd 00:00:00"));
                cmd.Parameters.AddWithValue("@end", to.Date.ToString("yyyy-MM-dd 23:59:59"));
                using (var r = cmd.ExecuteReader())
                    if (r.Read())
                    {
                        s.Revenue = Convert.ToDecimal(r.GetValue(0));
                        s.Cost = Convert.ToDecimal(r.GetValue(1));
                    }
            }
            return s;
        }
    }
}

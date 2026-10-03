using System.Collections.Generic;
using VapeShopPos.Data;
using VapeShopPos.Models;

namespace VapeShopPos.Services
{
    /// <summary>One row in a stock-take: product, theoretical qty and counted qty.</summary>
    public class StocktakeLine
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; }
        public string CategoryName { get; set; }
        public int TheoreticalQty { get; set; }   // from stock_movements
        public int CountedQty { get; set; }        // entered by the employee

        public int Variance { get { return CountedQty - TheoreticalQty; } }
    }

    public static class StocktakeService
    {
        /// <summary>
        /// Builds the stock-take worksheet: every product with its theoretical
        /// on-hand quantity. Counted is pre-filled with the theoretical value so
        /// unchanged rows produce no variance.
        /// </summary>
        public static List<StocktakeLine> BuildWorksheet()
        {
            var lines = new List<StocktakeLine>();
            foreach (var p in ProductService.GetProducts())
            {
                lines.Add(new StocktakeLine
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    CategoryName = p.CategoryName,
                    TheoreticalQty = p.CurrentQuantity,
                    CountedQty = p.CurrentQuantity
                });
            }
            return lines;
        }

        /// <summary>
        /// Reconciles the system to the physical count. For every line with a
        /// non-zero variance, writes a stocktake_adjustment movement (which also
        /// corrects current_quantity). All adjustments share one transaction.
        /// Returns the number of products adjusted.
        /// </summary>
        public static int ApplyStocktake(IEnumerable<StocktakeLine> lines, string note, long userId)
        {
            int adjusted = 0;
            using (var conn = Database.GetConnection())
            using (var tx = conn.BeginTransaction())
            {
                foreach (var line in lines)
                {
                    int variance = line.Variance;
                    if (variance == 0) continue;

                    string reference = string.IsNullOrWhiteSpace(note) ? "جرد" : "جرد: " + note;
                    ProductService.RecordMovement(conn, tx, line.ProductId, variance,
                        MovementTypes.StocktakeAdjustment, reference, userId);
                    adjusted++;
                }
                tx.Commit();
            }
            return adjusted;
        }
    }
}

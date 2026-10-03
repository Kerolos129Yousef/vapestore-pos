using System;

namespace VapeShopPos.Models
{
    public static class MovementTypes
    {
        public const string Sale = "sale";
        public const string Purchase = "purchase";
        public const string Damage = "damage";
        public const string StocktakeAdjustment = "stocktake_adjustment";
    }

    public class StockMovement
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; }      // joined, for display
        public DateTime DateTime { get; set; }
        public int ChangeQty { get; set; }           // positive or negative
        public string MovementType { get; set; }
        public string Reference { get; set; }        // invoice id or note
        public long UserId { get; set; }
    }
}

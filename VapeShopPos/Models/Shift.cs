using System;

namespace VapeShopPos.Models
{
    public class Shift
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; }        // joined, for display
        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public decimal OpeningCash { get; set; }
        public decimal? ClosingCash { get; set; }
        public decimal TotalSales { get; set; }

        public bool IsOpen
        {
            get { return ClosedAt == null; }
        }
    }
}

namespace VapeShopPos.Models
{
    public class InvoiceItem
    {
        public long Id { get; set; }
        public long InvoiceId { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; }      // captured for display
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }       // price captured at time of sale

        public decimal LineTotal
        {
            get { return UnitPrice * Quantity; }
        }
    }
}

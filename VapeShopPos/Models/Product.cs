namespace VapeShopPos.Models
{
    public class Product
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Barcode { get; set; }           // nullable / may be empty
        public long? CategoryId { get; set; }
        public string CategoryName { get; set; }       // joined, for display
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public int CurrentQuantity { get; set; }
        public int MinStockLevel { get; set; }

        public bool IsLowStock
        {
            get { return CurrentQuantity <= MinStockLevel; }
        }
    }
}

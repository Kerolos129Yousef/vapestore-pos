using System;
using System.Collections.Generic;

namespace VapeShopPos.Models
{
    public static class PaymentMethods
    {
        public const string Cash = "cash";
        public const string Card = "card";
    }

    public class Invoice
    {
        public long Id { get; set; }
        public DateTime DateTime { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
        public string PaymentMethod { get; set; }
        public long ShiftId { get; set; }
        public long UserId { get; set; }

        public List<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    }
}

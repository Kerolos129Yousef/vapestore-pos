using System;

namespace VapeShopPos.Models
{
    public static class CashDirections
    {
        public const string In = "in";    // deposit to drawer
        public const string Out = "out";  // withdraw from drawer
    }

    /// <summary>
    /// A manual cash in/out on the drawer (e.g. money-transfer services:
    /// InstaPay / e-wallet withdraw or deposit). Affects shift drawer reconciliation.
    /// </summary>
    public class CashMovement
    {
        public long Id { get; set; }
        public DateTime DateTime { get; set; }
        public string Direction { get; set; }   // CashDirections.In / Out
        public decimal Amount { get; set; }
        public string Service { get; set; }
        public long ShiftId { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; }

        public bool IsDeposit { get { return Direction == CashDirections.In; } }
    }
}

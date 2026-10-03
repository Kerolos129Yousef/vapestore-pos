namespace VapeShopPos.Models
{
    public static class Roles
    {
        public const string Manager = "manager";
        public const string Cashier = "cashier";
    }

    public class User
    {
        public long Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; }

        public bool IsManager
        {
            get { return Role == Roles.Manager; }
        }
    }
}

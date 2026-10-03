using VapeShopPos.Models;

namespace VapeShopPos.Services
{
    /// <summary>
    /// Holds the currently logged-in user and their open shift for the lifetime
    /// of the running application (single-user desktop app).
    /// </summary>
    public static class Session
    {
        public static User CurrentUser { get; set; }
        public static Shift CurrentShift { get; set; }

        public static bool IsManager
        {
            get { return CurrentUser != null && CurrentUser.IsManager; }
        }

        public static bool HasOpenShift
        {
            get { return CurrentShift != null && CurrentShift.IsOpen; }
        }

        public static void Clear()
        {
            CurrentUser = null;
            CurrentShift = null;
        }
    }
}

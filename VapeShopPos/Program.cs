using System;
using System.Windows.Forms;
using VapeShopPos.Data;
using VapeShopPos.Forms;
using VapeShopPos.Services;

namespace VapeShopPos
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                Database.Initialize();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "تعذّر تهيئة قاعدة البيانات:\n\n" + ex.Message,
                    "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Login loop: keep showing login until the user authenticates or cancels.
            while (true)
            {
                using (var login = new LoginForm())
                {
                    if (login.ShowDialog() != DialogResult.OK || Session.CurrentUser == null)
                        return; // user cancelled -> exit app

                    Application.Run(new MainForm());

                    // MainForm closed. If it was a logout, loop back to login; otherwise exit.
                    if (!MainForm.LogoutRequested)
                        return;

                    Session.Clear();
                    MainForm.LogoutRequested = false;
                }
            }
        }
    }
}

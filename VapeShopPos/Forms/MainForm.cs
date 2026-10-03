using System;
using System.Drawing;
using System.Windows.Forms;
using VapeShopPos.Models;
using VapeShopPos.Services;

namespace VapeShopPos.Forms
{
    public class MainForm : Form
    {
        /// <summary>Set to true when the user chooses Logout so Program loops back to login.</summary>
        public static bool LogoutRequested = false;

        private Label lblStatus;
        private FlowLayoutPanel menu;

        public MainForm()
        {
            LogoutRequested = false;
            BuildUi();
            RefreshStatus();
        }

        private void BuildUi()
        {
            Text = "نظام نقطة البيع - الشاشة الرئيسية";
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(900, 600);
            UiTheme.ApplyRtl(this);

            // ---- Top bar ----
            var top = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = UiTheme.Primary };
            var lblTitle = new Label
            {
                Text = "نظام نقطة البيع 🛒",
                Font = UiTheme.HeaderFont,
                ForeColor = Color.White,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(0, 0, 20, 0)
            };
            var btnLogout = UiTheme.MakeButton("تسجيل خروج", UiTheme.PrimaryDark);
            btnLogout.Dock = DockStyle.Left;
            btnLogout.Width = 150;
            btnLogout.Click += (s, e) =>
            {
                var answer = MessageBox.Show(
                    "هل تريد تسجيل الخروج؟",
                    "تأكيد الخروج",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (answer == DialogResult.Yes)
                {
                    LogoutRequested = true;
                    Close();
                }
            };
            top.Controls.Add(lblTitle);
            top.Controls.Add(btnLogout);

            // ---- Status bar ----
            lblStatus = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                BackColor = Color.FromArgb(236, 240, 241),
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(15, 0, 0, 0),
                Font = new Font("Segoe UI", 10F)
            };

            // ---- Menu of big buttons ----
            menu = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(30),
                AutoScroll = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true
            };

            bool mgr = Session.IsManager;

            AddTile("💰 شاشة البيع (POS)", UiTheme.Accent, OpenSales);
            AddTile("🕒 الورديات", UiTheme.Primary, OpenShift);
            AddTile("🧾 الفواتير", UiTheme.Primary, OpenInvoices);

            if (mgr)
            {
                AddTile("📦 الأصناف والمخزن", UiTheme.Primary, OpenProducts);
                AddTile("➕ إضافة مخزون (توريد)", UiTheme.Primary, OpenRestock);
                AddTile("🧾 الجرد", UiTheme.Primary, OpenStocktake);
                AddTile("📊 التقارير", UiTheme.Primary, OpenReports);
                AddTile("👤 المستخدمون", UiTheme.Primary, OpenUsers);
            }

            AddTile("💾 نسخة احتياطية", UiTheme.PrimaryDark, DoBackup);

            Controls.Add(menu);
            Controls.Add(lblStatus);
            Controls.Add(top);
        }

        private void AddTile(string text, Color color, Action onClick)
        {
            var b = UiTheme.MakeButton(text, color);
            b.Width = 240;
            b.Height = 120;
            b.Margin = new Padding(15);
            b.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            b.Click += (s, e) => onClick();
            menu.Controls.Add(b);
        }

        private void RefreshStatus()
        {
            string user = Session.CurrentUser != null ? Session.CurrentUser.Username : "-";
            string role = Session.IsManager ? "مدير" : "كاشير";
            string shift = Session.HasOpenShift
                ? "وردية مفتوحة #" + Session.CurrentShift.Id
                : "لا توجد وردية مفتوحة";
            lblStatus.Text = string.Format("المستخدم: {0}  ({1})     |     {2}", user, role, shift);
        }

        // ---------------------------------------------------------------- Actions
        private void OpenSales()
        {
            if (!Session.HasOpenShift)
            {
                MessageBox.Show("يجب فتح وردية أولاً قبل البيع.", "تنبيه",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                OpenShift();
                if (!Session.HasOpenShift) return;
            }
            using (var f = new SalesForm()) f.ShowDialog(this);
            RefreshStatus();
        }

        private void OpenShift()
        {
            using (var f = new ShiftForm()) f.ShowDialog(this);
            RefreshStatus();
        }

        private void OpenProducts() { using (var f = new ProductsForm()) f.ShowDialog(this); }
        private void OpenRestock() { using (var f = new RestockForm()) f.ShowDialog(this); }
        private void OpenStocktake() { using (var f = new StocktakeForm()) f.ShowDialog(this); }
        private void OpenReports() { using (var f = new ReportsForm()) f.ShowDialog(this); }
        private void OpenInvoices() { using (var f = new InvoicesForm()) f.ShowDialog(this); }
        private void OpenUsers() { using (var f = new UsersForm()) f.ShowDialog(this); }

        private void DoBackup()
        {
            try
            {
                string path = BackupService.Backup();
                MessageBox.Show("تم إنشاء نسخة احتياطية:\n\n" + path, "نجاح",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل إنشاء النسخة الاحتياطية:\n" + ex.Message, "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using VapeShopPos.Models;
using VapeShopPos.Services;

namespace VapeShopPos.Forms
{
    public class ShiftForm : Form
    {
        private Panel _body;

        public ShiftForm()
        {
            Text = "الورديات";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(560, 480);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            UiTheme.ApplyRtl(this);

            _body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(25) };
            Controls.Add(_body);

            Render();
        }

        private void Render()
        {
            _body.Controls.Clear();
            if (Session.HasOpenShift)
                RenderOpenShift();
            else
                RenderNoShift();
        }

        // ---- No open shift: offer to open one ----
        private void RenderNoShift()
        {
            var title = new Label
            {
                Text = "فتح وردية جديدة",
                Font = UiTheme.HeaderFont,
                ForeColor = UiTheme.Primary,
                AutoSize = true,
                Location = new Point(20, 10)
            };

            var lbl = new Label { Text = "النقدية الافتتاحية في الدرج (ج.م):", AutoSize = true, Location = new Point(20, 70), Font = new Font("Segoe UI", 12F) };
            var txtCash = new TextBox { Location = new Point(20, 100), Width = 240, Font = new Font("Segoe UI", 14F), Text = "0" };
            txtCash.KeyPress += DecimalOnly;

            var btnOpen = UiTheme.MakeButton("فتح الوردية", UiTheme.Accent);
            btnOpen.Location = new Point(20, 150);
            btnOpen.Width = 240;
            btnOpen.Click += (s, e) =>
            {
                decimal cash;
                if (!decimal.TryParse(txtCash.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out cash) || cash < 0)
                    cash = 0;
                Session.CurrentShift = ShiftService.OpenShift(Session.CurrentUser.Id, cash);
                MessageBox.Show("تم فتح الوردية #" + Session.CurrentShift.Id, "نجاح",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                Render();
            };

            _body.Controls.AddRange(new Control[] { title, lbl, txtCash, btnOpen });
        }

        // ---- Open shift: show live totals + close ----
        private void RenderOpenShift()
        {
            Shift sh = Session.CurrentShift;
            decimal cashSales = ShiftService.GetShiftCashSalesTotal(sh.Id);
            decimal totalSales = ShiftService.GetShiftSalesTotal(sh.Id);
            int invoices = ShiftService.GetShiftInvoiceCount(sh.Id);
            decimal expectedCash = sh.OpeningCash + cashSales;

            var title = new Label
            {
                Text = "الوردية الحالية #" + sh.Id,
                Font = UiTheme.HeaderFont,
                ForeColor = UiTheme.Primary,
                AutoSize = true,
                Location = new Point(20, 5)
            };

            var info = new Label
            {
                AutoSize = false,
                Size = new Size(500, 200),
                Location = new Point(20, 55),
                Font = new Font("Segoe UI", 12F),
                Text =
                    "الكاشير: " + sh.UserName + "\n" +
                    "فُتحت في: " + sh.OpenedAt.ToString("yyyy-MM-dd HH:mm") + "\n\n" +
                    "النقدية الافتتاحية: " + UiTheme.Money(sh.OpeningCash) + "\n" +
                    "عدد الفواتير: " + invoices + "\n" +
                    "مبيعات نقدية: " + UiTheme.Money(cashSales) + "\n" +
                    "مبيعات بطاقة: " + UiTheme.Money(totalSales - cashSales) + "\n" +
                    "إجمالي المبيعات: " + UiTheme.Money(totalSales) + "\n\n" +
                    "النقدية المتوقعة في الدرج: " + UiTheme.Money(expectedCash)
            };

            var btnClose = UiTheme.MakeButton("تقفيل الوردية", UiTheme.Danger);
            btnClose.Location = new Point(20, 270);
            btnClose.Width = 240;
            btnClose.Height = 56;
            btnClose.Click += (s, e) => CloseShift(expectedCash);

            var btnRefresh = UiTheme.MakeButton("تحديث", UiTheme.Primary);
            btnRefresh.Location = new Point(280, 270);
            btnRefresh.Width = 240;
            btnRefresh.Height = 56;
            btnRefresh.Click += (s, e) => Render();

            _body.Controls.AddRange(new Control[] { title, info, btnClose, btnRefresh });
        }

        private void CloseShift(decimal expectedCash)
        {
            string input = Prompt("أدخل النقدية الفعلية المعدودة في الدرج (ج.م):", "تقفيل الوردية", "0");
            if (input == null) return;

            decimal counted;
            if (!decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out counted) || counted < 0)
            {
                MessageBox.Show("قيمة غير صحيحة.", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Shift closed = ShiftService.CloseShift(Session.CurrentShift.Id, counted);
            decimal diff = counted - expectedCash;
            string diffLabel = diff == 0 ? "مطابقة ✓" : (diff > 0 ? "زيادة +" : "عجز ");

            MessageBox.Show(
                "تم تقفيل الوردية #" + closed.Id + "\n\n" +
                "إجمالي المبيعات: " + UiTheme.Money(closed.TotalSales) + "\n" +
                "النقدية المتوقعة: " + UiTheme.Money(expectedCash) + "\n" +
                "النقدية المعدودة: " + UiTheme.Money(counted) + "\n" +
                "الفرق: " + diffLabel + UiTheme.Money(Math.Abs(diff)),
                "ملخص التقفيل", MessageBoxButtons.OK, MessageBoxIcon.Information);

            Session.CurrentShift = null;
            Render();
        }

        // Simple text prompt dialog (WinForms has none built in).
        private string Prompt(string text, string caption, string def)
        {
            using (var f = new Form())
            {
                f.Text = caption;
                f.StartPosition = FormStartPosition.CenterParent;
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.ClientSize = new Size(380, 160);
                f.MaximizeBox = false; f.MinimizeBox = false;
                UiTheme.ApplyRtl(f);

                var lbl = new Label { Text = text, AutoSize = false, Size = new Size(340, 40), Location = new Point(20, 15), Font = new Font("Segoe UI", 11F) };
                var tb = new TextBox { Location = new Point(20, 60), Width = 340, Font = new Font("Segoe UI", 14F), Text = def };
                tb.KeyPress += DecimalOnly;
                var ok = UiTheme.MakeButton("تأكيد", UiTheme.Accent);
                ok.Location = new Point(20, 100); ok.Width = 160; ok.DialogResult = DialogResult.OK;
                var cancel = UiTheme.MakeButton("إلغاء", UiTheme.PrimaryDark);
                cancel.Location = new Point(200, 100); cancel.Width = 160; cancel.DialogResult = DialogResult.Cancel;

                f.Controls.AddRange(new Control[] { lbl, tb, ok, cancel });
                f.AcceptButton = ok; f.CancelButton = cancel;

                return f.ShowDialog(this) == DialogResult.OK ? tb.Text : null;
            }
        }

        private void DecimalOnly(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar)) return;
            if (!char.IsDigit(e.KeyChar) && e.KeyChar != '.') e.Handled = true;
        }
    }
}

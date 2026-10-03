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
            ClientSize = new Size(660, 680);
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(600, 540);
            MaximizeBox = true;
            UiTheme.ApplyRtl(this);

            _body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(25), AutoScroll = true };

            var topBar = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = UiTheme.PanelBg, Padding = new Padding(12, 8, 12, 8) };
            var backRow = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, RightToLeft = RightToLeft.No };
            backRow.Controls.Add(UiTheme.MakeBackButton(this));
            topBar.Controls.Add(backRow);

            Controls.Add(_body);
            Controls.Add(topBar);

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
            int w = _body.ClientSize.Width;

            var title = new Label
            {
                Text = "فتح وردية جديدة",
                Font = UiTheme.HeaderFont,
                ForeColor = UiTheme.Primary,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Size = new Size(w - 50, 36),
                Location = new Point(25, 10),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var lbl = new Label { Text = "النقدية الافتتاحية في الدرج (ج.م):", AutoSize = false, TextAlign = ContentAlignment.MiddleLeft, Size = new Size(w - 50, 28), Location = new Point(25, 70), Font = new Font("Segoe UI", 12F), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            var txtCash = new TextBox { Location = new Point(w - 25 - 240, 100), Width = 240, Font = new Font("Segoe UI", 14F), Text = "0", Anchor = AnchorStyles.Top | AnchorStyles.Right };
            txtCash.KeyPress += DecimalOnly;

            var btnOpen = UiTheme.MakeButton("فتح الوردية", UiTheme.Accent);
            btnOpen.Location = new Point(w - 25 - 240, 150);
            btnOpen.Width = 240;
            btnOpen.Anchor = AnchorStyles.Top | AnchorStyles.Right;
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
            AddHistory(220);
        }

        // ---- Open shift: show live totals + close ----
        private void RenderOpenShift()
        {
            Shift sh = Session.CurrentShift;
            decimal cashSales = ShiftService.GetShiftCashSalesTotal(sh.Id);
            decimal totalSales = ShiftService.GetShiftSalesTotal(sh.Id);
            int invoices = ShiftService.GetShiftInvoiceCount(sh.Id);
            decimal expectedCash = sh.OpeningCash + cashSales;

            int w = _body.ClientSize.Width;

            var title = new Label
            {
                Text = "الوردية الحالية #" + sh.Id,
                Font = UiTheme.HeaderFont,
                ForeColor = UiTheme.Primary,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Size = new Size(w - 50, 36),
                Location = new Point(25, 5),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var info = new Label
            {
                AutoSize = false,
                Size = new Size(w - 50, 250),
                Location = new Point(25, 50),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
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
            btnClose.Size = new Size(240, 56);
            btnClose.Location = new Point(w - 25 - 240, 310);
            btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnClose.Click += (s, e) => CloseShift(expectedCash);

            var btnRefresh = UiTheme.MakeButton("تحديث", UiTheme.Primary);
            btnRefresh.Size = new Size(240, 56);
            btnRefresh.Location = new Point(w - 25 - 240 - 20 - 240, 310);
            btnRefresh.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnRefresh.Click += (s, e) => Render();

            _body.Controls.AddRange(new Control[] { title, info, btnClose, btnRefresh });
            AddHistory(385);
        }

        // ---- History of all shifts (closed + open) ----
        private void AddHistory(int topY)
        {
            var header = new Label
            {
                Text = "سجل الورديات  (دبل-كليك على الوردية لعرض التفاصيل)",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = UiTheme.Primary,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Size = new Size(_body.ClientSize.Width - 50, 28),
                Location = new Point(25, topY),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var grid = new DataGridView
            {
                Location = new Point(20, topY + 34),
                Size = new Size(_body.ClientSize.Width - 65, _body.ClientSize.Height - (topY + 34) - 25),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = UiTheme.PanelBg,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10F),
                RightToLeft = RightToLeft.Yes,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 34
            };
            grid.ColumnHeadersDefaultCellStyle.BackColor = UiTheme.Primary;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            grid.Columns.Add("id", "#");
            grid.Columns.Add("user", "الكاشير");
            grid.Columns.Add("opened", "فُتحت");
            grid.Columns.Add("closed", "أُقفلت");
            grid.Columns.Add("opening", "افتتاحية");
            grid.Columns.Add("sales", "المبيعات");
            grid.Columns.Add("counted", "المعدودة");
            grid.Columns.Add("diff", "الفرق");
            grid.Columns["id"].FillWeight = 35;
            grid.Columns["user"].FillWeight = 70;

            // Double-click a row -> open that shift's details screen.
            grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                var idCell = grid.Rows[e.RowIndex].Cells["id"].Value;
                if (idCell == null) return;
                long shiftId = Convert.ToInt64(idCell);
                using (var f = new ShiftDetailsForm(shiftId))
                    f.ShowDialog(this);
            };

            foreach (var sh in ShiftService.GetShifts())
            {
                string closedText = sh.ClosedAt.HasValue
                    ? sh.ClosedAt.Value.ToString("yyyy-MM-dd HH:mm")
                    : "مفتوحة";
                string countedText = sh.ClosingCash.HasValue ? UiTheme.Money(sh.ClosingCash.Value) : "—";

                string diffText = "—";
                if (sh.ClosedAt.HasValue && sh.ClosingCash.HasValue)
                {
                    decimal cashSales = ShiftService.GetShiftCashSalesTotal(sh.Id);
                    decimal expected = sh.OpeningCash + cashSales;
                    decimal d = sh.ClosingCash.Value - expected;
                    if (d == 0) diffText = "مطابقة ✓";
                    else diffText = (d > 0 ? "زيادة +" : "عجز ") + UiTheme.Money(Math.Abs(d));
                }

                int idx = grid.Rows.Add(
                    sh.Id,
                    sh.UserName,
                    sh.OpenedAt.ToString("yyyy-MM-dd HH:mm"),
                    closedText,
                    UiTheme.Money(sh.OpeningCash),
                    UiTheme.Money(sh.TotalSales),
                    countedText,
                    diffText);

                if (!sh.ClosedAt.HasValue)
                    grid.Rows[idx].DefaultCellStyle.BackColor = Color.FromArgb(230, 244, 234);
            }

            if (grid.Rows.Count == 0)
                header.Text = "سجل الورديات (لا توجد ورديات بعد)";

            _body.Controls.Add(header);
            _body.Controls.Add(grid);
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

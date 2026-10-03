using System;
using System.Drawing;
using System.Windows.Forms;
using VapeShopPos.Models;
using VapeShopPos.Services;

namespace VapeShopPos.Forms
{
    /// <summary>
    /// Read-only details of a single shift: summary (opening cash, sales breakdown,
    /// expected vs counted cash, variance) plus the list of invoices in that shift.
    /// </summary>
    public class ShiftDetailsForm : Form
    {
        public ShiftDetailsForm(long shiftId)
        {
            Shift sh = ShiftService.GetShiftById(shiftId);

            Text = "تفاصيل الوردية #" + shiftId;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(660, 680);
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(560, 560);
            MaximizeBox = true;
            UiTheme.ApplyRtl(this);

            var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(25), AutoScroll = true };
            Controls.Add(body);

            if (sh == null)
            {
                body.Controls.Add(new Label
                {
                    Text = "الوردية غير موجودة.",
                    Font = UiTheme.HeaderFont,
                    ForeColor = UiTheme.Danger,
                    AutoSize = true,
                    Location = new Point(20, 20)
                });
                return;
            }

            decimal cashSales = ShiftService.GetShiftCashSalesTotal(sh.Id);
            decimal totalSales = ShiftService.GetShiftSalesTotal(sh.Id);
            decimal cardSales = totalSales - cashSales;
            int invoices = ShiftService.GetShiftInvoiceCount(sh.Id);
            decimal cashIn = CashMovementService.GetShiftTotal(sh.Id, true);
            decimal cashOut = CashMovementService.GetShiftTotal(sh.Id, false);
            decimal expectedCash = sh.OpeningCash + cashSales + cashIn - cashOut;

            bool isClosed = sh.ClosedAt.HasValue;
            string status = isClosed ? "مقفولة" : "مفتوحة";

            int w = body.ClientSize.Width;

            var title = new Label
            {
                Text = "الوردية #" + sh.Id + "  (" + status + ")",
                Font = UiTheme.HeaderFont,
                ForeColor = isClosed ? UiTheme.Primary : UiTheme.Accent,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Size = new Size(w - 50, 36),
                Location = new Point(25, 5),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            string varianceText = "—";
            if (isClosed && sh.ClosingCash.HasValue)
            {
                decimal d = sh.ClosingCash.Value - expectedCash;
                if (d == 0) varianceText = "مطابقة ✓";
                else varianceText = (d > 0 ? "زيادة +" : "عجز ") + UiTheme.Money(Math.Abs(d));
            }

            var info = new Label
            {
                AutoSize = false,
                Size = new Size(w - 50, 320),
                Location = new Point(25, 50),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 12F),
                Text =
                    "الكاشير: " + sh.UserName + "\n" +
                    "فُتحت في: " + sh.OpenedAt.ToString("yyyy-MM-dd HH:mm") + "\n" +
                    "أُقفلت في: " + (isClosed ? sh.ClosedAt.Value.ToString("yyyy-MM-dd HH:mm") : "—") + "\n\n" +
                    "النقدية الافتتاحية: " + UiTheme.Money(sh.OpeningCash) + "\n" +
                    "عدد الفواتير: " + invoices + "\n" +
                    "مبيعات نقدية: " + UiTheme.Money(cashSales) + "\n" +
                    "مبيعات بطاقة: " + UiTheme.Money(cardSales) + "\n" +
                    "إجمالي المبيعات: " + UiTheme.Money(totalSales) + "\n" +
                    "إيداعات على الدرج (خدمات): " + UiTheme.Money(cashIn) + "\n" +
                    "سحوبات من الدرج (خدمات): " + UiTheme.Money(cashOut) + "\n\n" +
                    "النقدية المتوقعة في الدرج: " + UiTheme.Money(expectedCash) + "\n" +
                    "النقدية المعدودة: " + (sh.ClosingCash.HasValue ? UiTheme.Money(sh.ClosingCash.Value) : "—") + "\n" +
                    "الفرق: " + varianceText
            };

            var invHeader = new Label
            {
                Text = "فواتير الوردية  (دبل-كليك للتفاصيل)",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = UiTheme.Primary,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Size = new Size(w - 50, 28),
                Location = new Point(25, 380),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var grid = new DataGridView
            {
                Location = new Point(25, 415),
                Size = new Size(body.ClientSize.Width - 50, body.ClientSize.Height - 415 - 25),
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

            grid.Columns.Add("id", "رقم الفاتورة");
            grid.Columns.Add("dt", "الوقت");
            grid.Columns.Add("subtotal", "الإجمالي الفرعي");
            grid.Columns.Add("discount", "الخصم");
            grid.Columns.Add("total", "الإجمالي");
            grid.Columns.Add("pm", "طريقة الدفع");
            grid.Columns["id"].FillWeight = 55;

            // Double-click an invoice row -> open its full details (with line items).
            grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                var idCell = grid.Rows[e.RowIndex].Cells["id"].Value;
                if (idCell == null) return;
                long invId = Convert.ToInt64(idCell);
                using (var f = new InvoiceDetailsForm(invId))
                    f.ShowDialog(this);
            };

            foreach (var inv in ShiftService.GetShiftInvoices(sh.Id))
            {
                grid.Rows.Add(
                    inv.Id,
                    inv.DateTime.ToString("yyyy-MM-dd HH:mm"),
                    UiTheme.Money(inv.Subtotal),
                    UiTheme.Money(inv.Discount),
                    UiTheme.Money(inv.Total),
                    inv.PaymentMethod == PaymentMethods.Card ? "بطاقة" : "نقدي");
            }

            if (grid.Rows.Count == 0)
                invHeader.Text = "فواتير الوردية (لا توجد فواتير)";

            body.Controls.Add(title);
            body.Controls.Add(info);
            body.Controls.Add(invHeader);
            body.Controls.Add(grid);
        }
    }
}

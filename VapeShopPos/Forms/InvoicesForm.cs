using System;
using System.Drawing;
using System.Windows.Forms;
using VapeShopPos.Models;
using VapeShopPos.Services;

namespace VapeShopPos.Forms
{
    /// <summary>
    /// Standalone screen listing all invoices with filters (date range, cashier,
    /// payment method). Double-click an invoice to view its details and items.
    /// </summary>
    public class InvoicesForm : Form
    {
        private sealed class CashierItem
        {
            public long? Id;
            public string Name;
            public override string ToString() { return Name; }
        }

        private DateTimePicker _dtFrom;
        private DateTimePicker _dtTo;
        private ComboBox _cbCashier;
        private ComboBox _cbPayment;
        private Label _lblSummary;
        private DataGridView _grid;

        public InvoicesForm()
        {
            Text = "الفواتير";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(900, 620);
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(760, 540);
            UiTheme.ApplyRtl(this);

            BuildUi();
            LoadData();
        }

        private void BuildUi()
        {
            // ---- Filter bar (flows right-to-left under RTL) ----
            var top = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Color.FromArgb(234, 242, 248), Padding = new Padding(15, 12, 15, 8) };
            var filter = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight
            };
            var btnRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                RightToLeft = RightToLeft.No
            };

            var lblFrom = MakeFilterLabel("من:");
            _dtFrom = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 130, Font = new Font("Segoe UI", 11F), Value = DateTime.Today.AddDays(-30), Margin = new Padding(4, 2, 12, 2) };

            var lblTo = MakeFilterLabel("إلى:");
            _dtTo = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 130, Font = new Font("Segoe UI", 11F), Value = DateTime.Today, Margin = new Padding(4, 2, 12, 2) };

            var lblCashier = MakeFilterLabel("الكاشير:");
            _cbCashier = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150, Font = new Font("Segoe UI", 11F), Margin = new Padding(4, 2, 12, 2) };
            _cbCashier.Items.Add(new CashierItem { Id = null, Name = "الكل" });
            foreach (var u in UserService.GetUsers())
                _cbCashier.Items.Add(new CashierItem { Id = u.Id, Name = u.Username });
            _cbCashier.SelectedIndex = 0;

            var lblPayment = MakeFilterLabel("الدفع:");
            _cbPayment = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110, Font = new Font("Segoe UI", 11F), Margin = new Padding(4, 2, 12, 2) };
            _cbPayment.Items.AddRange(new object[] { "الكل", "نقدي", "بطاقة" });
            _cbPayment.SelectedIndex = 0;

            var btnShow = UiTheme.MakeButton("عرض", UiTheme.Primary);
            btnShow.Size = new Size(110, 40);
            btnShow.Margin = new Padding(4, 0, 8, 2);
            btnShow.Click += (s, e) => LoadData();

            var btnReset = UiTheme.MakeButton("الكل", UiTheme.PrimaryDark);
            btnReset.Size = new Size(110, 40);
            btnReset.Margin = new Padding(4, 0, 8, 2);
            btnReset.Click += (s, e) =>
            {
                _dtFrom.Value = DateTime.Today.AddDays(-30);
                _dtTo.Value = DateTime.Today;
                _cbCashier.SelectedIndex = 0;
                _cbPayment.SelectedIndex = 0;
                LoadData();
            };

            filter.Controls.AddRange(new Control[]
            {
                lblFrom, _dtFrom, lblTo, _dtTo, lblCashier, _cbCashier,
                lblPayment, _cbPayment
            });
            btnRow.Controls.Add(UiTheme.MakeBackButton(this));
            btnRow.Controls.AddRange(new Control[] { btnShow, btnReset });
            top.Controls.Add(filter);
            top.Controls.Add(btnRow);

            // ---- Summary line ----
            _lblSummary = new Label
            {
                Dock = DockStyle.Top,
                Height = 34,
                BackColor = Color.FromArgb(236, 240, 241),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = UiTheme.Primary,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(15, 0, 15, 0)
            };

            // ---- Invoices grid ----
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = UiTheme.PanelBg,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 11F),
                RightToLeft = RightToLeft.Yes,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 40,
                RowTemplate = { Height = 34 }
            };
            _grid.ColumnHeadersDefaultCellStyle.BackColor = UiTheme.Primary;
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            _grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            _grid.Columns.Add("id", "رقم الفاتورة");
            _grid.Columns.Add("dt", "التاريخ");
            _grid.Columns.Add("user", "الكاشير");
            _grid.Columns.Add("pm", "طريقة الدفع");
            _grid.Columns.Add("discount", "الخصم");
            _grid.Columns.Add("total", "الإجمالي");
            _grid.Columns["id"].FillWeight = 55;

            _grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                var idCell = _grid.Rows[e.RowIndex].Cells["id"].Value;
                if (idCell == null) return;
                long invId = Convert.ToInt64(idCell);
                using (var f = new InvoiceDetailsForm(invId))
                    f.ShowDialog(this);
            };

            var hint = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 26,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(15, 0, 0, 0),
                Text = "دبل-كليك على الفاتورة لعرض تفاصيلها وأصنافها"
            };

            Controls.Add(_grid);
            Controls.Add(hint);
            Controls.Add(_lblSummary);
            Controls.Add(top);
        }

        private Label MakeFilterLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = UiTheme.Primary,
                Margin = new Padding(8, 8, 0, 2),
                TextAlign = ContentAlignment.MiddleRight
            };
        }

        private void LoadData()
        {
            DateTime? from = _dtFrom.Value.Date;
            DateTime? to = _dtTo.Value.Date;

            long? userId = null;
            var ci = _cbCashier.SelectedItem as CashierItem;
            if (ci != null) userId = ci.Id;

            string pm = null;
            if (_cbPayment.SelectedIndex == 1) pm = PaymentMethods.Cash;
            else if (_cbPayment.SelectedIndex == 2) pm = PaymentMethods.Card;

            var invoices = InvoiceService.GetInvoices(from, to, userId, pm);

            _grid.Rows.Clear();
            decimal total = 0;
            foreach (var inv in invoices)
            {
                _grid.Rows.Add(
                    inv.Id,
                    inv.DateTime.ToString("yyyy-MM-dd HH:mm"),
                    inv.UserName,
                    inv.PaymentMethod == PaymentMethods.Card ? "بطاقة" : "نقدي",
                    UiTheme.Money(inv.Discount),
                    UiTheme.Money(inv.Total));
                total += inv.Total;
            }

            _lblSummary.Text = string.Format("عدد الفواتير: {0}     |     إجمالي المبيعات: {1}",
                invoices.Count, UiTheme.Money(total));
        }
    }
}

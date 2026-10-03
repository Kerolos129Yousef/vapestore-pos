using System;
using System.Drawing;
using System.Windows.Forms;
using VapeShopPos.Services;

namespace VapeShopPos.Forms
{
    public class ReportsForm : Form
    {
        public ReportsForm()
        {
            Text = "التقارير";
            StartPosition = FormStartPosition.CenterParent;
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(900, 600);
            UiTheme.ApplyRtl(this);

            var tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12F) };
            tabs.TabPages.Add(BuildDailyTab());
            tabs.TabPages.Add(BuildBestSellersTab());
            tabs.TabPages.Add(BuildProfitTab());
            tabs.TabPages.Add(BuildLowStockTab());
            Controls.Add(tabs);
        }

        // ---------------------------------------------------------------- Daily
        private TabPage BuildDailyTab()
        {
            var tab = new TabPage("ملخص المبيعات اليومي") { BackColor = UiTheme.Background };

            var dt = new DateTimePicker { Location = new Point(20, 20), Width = 200, Format = DateTimePickerFormat.Short, Font = new Font("Segoe UI", 12F) };
            var btn = UiTheme.MakeButton("عرض", UiTheme.Primary);
            btn.Location = new Point(240, 16); btn.Width = 120;

            var lbl = new Label { Location = new Point(20, 80), AutoSize = false, Size = new Size(600, 260), Font = new Font("Segoe UI", 14F) };

            Action load = () =>
            {
                var s = ReportService.GetDailySummary(dt.Value);
                lbl.Text =
                    "التاريخ: " + s.Date.ToString("yyyy-MM-dd") + "\n\n" +
                    "عدد الفواتير: " + s.InvoiceCount + "\n" +
                    "إجمالي المبيعات: " + UiTheme.Money(s.TotalSales) + "\n" +
                    "مبيعات نقدية: " + UiTheme.Money(s.CashSales) + "\n" +
                    "مبيعات بطاقة: " + UiTheme.Money(s.CardSales) + "\n" +
                    "إجمالي الخصومات: " + UiTheme.Money(s.TotalDiscount);
            };
            btn.Click += (s, e) => load();
            tab.Controls.AddRange(new Control[] { dt, btn, lbl });
            tab.HandleCreated += (s, e) => load();
            return tab;
        }

        // ---------------------------------------------------------------- Best sellers
        private TabPage BuildBestSellersTab()
        {
            var tab = new TabPage("الأكثر مبيعاً") { BackColor = UiTheme.Background };

            var dtFrom = new DateTimePicker { Location = new Point(20, 20), Width = 170, Format = DateTimePickerFormat.Short, Font = new Font("Segoe UI", 11F), Value = DateTime.Today.AddDays(-30) };
            var dtTo = new DateTimePicker { Location = new Point(200, 20), Width = 170, Format = DateTimePickerFormat.Short, Font = new Font("Segoe UI", 11F), Value = DateTime.Today };
            var btn = UiTheme.MakeButton("عرض", UiTheme.Primary);
            btn.Location = new Point(390, 16); btn.Width = 120;

            var grid = NewGrid();
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "name", HeaderText = "الصنف", FillWeight = 50 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "qty", HeaderText = "الكمية المباعة", FillWeight = 25 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "rev", HeaderText = "الإيراد", FillWeight = 25 });
            grid.Location = new Point(20, 65);
            grid.Size = new Size(820, 440);
            grid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            Action load = () =>
            {
                grid.Rows.Clear();
                foreach (var r in ReportService.GetBestSellers(dtFrom.Value, dtTo.Value, 50))
                    grid.Rows.Add(r.ProductName, r.QuantitySold, UiTheme.Money(r.Revenue));
            };
            btn.Click += (s, e) => load();
            tab.Controls.AddRange(new Control[] { dtFrom, dtTo, btn, grid });
            tab.HandleCreated += (s, e) => load();
            return tab;
        }

        // ---------------------------------------------------------------- Profit
        private TabPage BuildProfitTab()
        {
            var tab = new TabPage("الأرباح") { BackColor = UiTheme.Background };

            var dtFrom = new DateTimePicker { Location = new Point(20, 20), Width = 170, Format = DateTimePickerFormat.Short, Font = new Font("Segoe UI", 11F), Value = DateTime.Today.AddDays(-30) };
            var dtTo = new DateTimePicker { Location = new Point(200, 20), Width = 170, Format = DateTimePickerFormat.Short, Font = new Font("Segoe UI", 11F), Value = DateTime.Today };
            var btn = UiTheme.MakeButton("عرض", UiTheme.Primary);
            btn.Location = new Point(390, 16); btn.Width = 120;

            var lbl = new Label { Location = new Point(20, 80), AutoSize = false, Size = new Size(600, 200), Font = new Font("Segoe UI", 15F) };

            Action load = () =>
            {
                var p = ReportService.GetProfit(dtFrom.Value, dtTo.Value);
                lbl.Text =
                    "الفترة: " + dtFrom.Value.ToString("yyyy-MM-dd") + " إلى " + dtTo.Value.ToString("yyyy-MM-dd") + "\n\n" +
                    "إجمالي الإيراد: " + UiTheme.Money(p.Revenue) + "\n" +
                    "إجمالي التكلفة: " + UiTheme.Money(p.Cost) + "\n\n" +
                    "صافي الربح: " + UiTheme.Money(p.Profit);
            };
            btn.Click += (s, e) => load();
            tab.Controls.AddRange(new Control[] { dtFrom, dtTo, btn, lbl });
            tab.HandleCreated += (s, e) => load();
            return tab;
        }

        // ---------------------------------------------------------------- Low stock
        private TabPage BuildLowStockTab()
        {
            var tab = new TabPage("تنبيه المخزون المنخفض") { BackColor = UiTheme.Background };

            var grid = NewGrid();
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "name", HeaderText = "الصنف", FillWeight = 45 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cat", HeaderText = "التصنيف", FillWeight = 25 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "qty", HeaderText = "المخزون", FillWeight = 15 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "min", HeaderText = "حد التنبيه", FillWeight = 15 });
            grid.Dock = DockStyle.Fill;

            Action load = () =>
            {
                grid.Rows.Clear();
                foreach (var p in ProductService.GetLowStockProducts())
                {
                    int i = grid.Rows.Add(p.Name, p.CategoryName, p.CurrentQuantity, p.MinStockLevel);
                    grid.Rows[i].DefaultCellStyle.BackColor = Color.FromArgb(255, 235, 235);
                }
            };
            tab.Controls.Add(grid);
            tab.HandleCreated += (s, e) => load();
            return tab;
        }

        private DataGridView NewGrid()
        {
            return new DataGridView
            {
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                Font = new Font("Segoe UI", 11F),
                RowTemplate = { Height = 32 }
            };
        }
    }
}

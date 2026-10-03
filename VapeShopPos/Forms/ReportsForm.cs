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

            var host = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Background };

            var panels = new Panel[]
            {
                BuildDailyPanel(),
                BuildBestSellersPanel(),
                BuildProfitPanel(),
                BuildLowStockPanel()
            };
            var titles = new[] { "ملخص المبيعات اليومي", "الأكثر مبيعاً", "الأرباح", "تنبيه المخزون المنخفض" };

            foreach (var p in panels) { p.Dock = DockStyle.Fill; p.Visible = false; host.Controls.Add(p); }

            // ---- Custom tab bar: buttons on the right, back button on the left ----
            var tabBar = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = UiTheme.PanelBg, Padding = new Padding(10, 9, 10, 0) };

            var tabRow = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            var tabButtons = new Button[titles.Length];

            Action<int> selectTab = sel =>
            {
                for (int i = 0; i < panels.Length; i++)
                {
                    bool on = (i == sel);
                    panels[i].Visible = on;
                    tabButtons[i].BackColor = on ? UiTheme.Primary : Color.FromArgb(223, 231, 238);
                    tabButtons[i].ForeColor = on ? Color.White : Color.FromArgb(45, 45, 45);
                }
                panels[sel].BringToFront();
            };

            for (int i = 0; i < titles.Length; i++)
            {
                int idx = i;
                var b = new Button
                {
                    Text = titles[i],
                    Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat,
                    Width = 190,
                    Height = 42,
                    Margin = new Padding(0, 0, 6, 0),
                    Cursor = Cursors.Hand
                };
                b.FlatAppearance.BorderSize = 0;
                b.Click += (s, e) => selectTab(idx);
                tabButtons[i] = b;
                tabRow.Controls.Add(b);
            }

            var backRow = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, RightToLeft = RightToLeft.No };
            backRow.Controls.Add(UiTheme.MakeBackButton(this));

            tabBar.Controls.Add(tabRow);
            tabBar.Controls.Add(backRow);

            Controls.Add(host);
            Controls.Add(tabBar);

            selectTab(0);
        }

        // ---------------------------------------------------------------- Daily
        private Panel BuildDailyPanel()
        {
            var tab = new Panel { BackColor = UiTheme.Background };

            var dt = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 180, Font = new Font("Segoe UI", 12F), Margin = new Padding(4, 2, 12, 2) };
            var btn = UiTheme.MakeButton("عرض", UiTheme.Primary);
            btn.Size = new Size(120, 38); btn.Margin = new Padding(4, 0, 8, 2);
            var filter = MakeFilterBar(MakeFilterLabel("التاريخ:"), dt, btn);

            var content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = UiTheme.Background, AutoScroll = true };
            var card = MakeCard(260);
            var lbl = MakeCardLabel();
            card.Controls.Add(lbl);
            content.Controls.Add(card);

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

            tab.Controls.Add(content);
            tab.Controls.Add(filter);
            load();
            return tab;
        }

        // ---------------------------------------------------------------- Best sellers
        private Panel BuildBestSellersPanel()
        {
            var tab = new Panel { BackColor = UiTheme.Background };

            var dtFrom = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 160, Font = new Font("Segoe UI", 11F), Value = DateTime.Today.AddDays(-30), Margin = new Padding(4, 2, 12, 2) };
            var dtTo = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 160, Font = new Font("Segoe UI", 11F), Value = DateTime.Today, Margin = new Padding(4, 2, 12, 2) };
            var btn = UiTheme.MakeButton("عرض", UiTheme.Primary);
            btn.Size = new Size(120, 38); btn.Margin = new Padding(4, 0, 8, 2);
            var filter = MakeFilterBar(MakeFilterLabel("من:"), dtFrom, MakeFilterLabel("إلى:"), dtTo, btn);

            var grid = NewGrid();
            grid.Dock = DockStyle.Fill;
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "name", HeaderText = "الصنف", FillWeight = 50 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "qty", HeaderText = "الكمية المباعة", FillWeight = 25 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "rev", HeaderText = "الإيراد", FillWeight = 25 });

            Action load = () =>
            {
                grid.Rows.Clear();
                foreach (var r in ReportService.GetBestSellers(dtFrom.Value, dtTo.Value, 50))
                    grid.Rows.Add(r.ProductName, r.QuantitySold, UiTheme.Money(r.Revenue));
            };
            btn.Click += (s, e) => load();

            tab.Controls.Add(grid);
            tab.Controls.Add(filter);
            load();
            return tab;
        }

        // ---------------------------------------------------------------- Profit
        private Panel BuildProfitPanel()
        {
            var tab = new Panel { BackColor = UiTheme.Background };

            var dtFrom = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 160, Font = new Font("Segoe UI", 11F), Value = DateTime.Today.AddDays(-30), Margin = new Padding(4, 2, 12, 2) };
            var dtTo = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 160, Font = new Font("Segoe UI", 11F), Value = DateTime.Today, Margin = new Padding(4, 2, 12, 2) };
            var btn = UiTheme.MakeButton("عرض", UiTheme.Primary);
            btn.Size = new Size(120, 38); btn.Margin = new Padding(4, 0, 8, 2);
            var filter = MakeFilterBar(MakeFilterLabel("من:"), dtFrom, MakeFilterLabel("إلى:"), dtTo, btn);

            var content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = UiTheme.Background, AutoScroll = true };
            var card = MakeCard(220);
            var lbl = MakeCardLabel();
            card.Controls.Add(lbl);
            content.Controls.Add(card);

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

            tab.Controls.Add(content);
            tab.Controls.Add(filter);
            load();
            return tab;
        }

        // ---------------------------------------------------------------- Low stock
        private Panel BuildLowStockPanel()
        {
            var tab = new Panel { BackColor = UiTheme.Background };

            var grid = NewGrid();
            grid.Dock = DockStyle.Fill;
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "name", HeaderText = "الصنف", FillWeight = 45 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cat", HeaderText = "التصنيف", FillWeight = 25 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "qty", HeaderText = "المخزون", FillWeight = 15 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "min", HeaderText = "حد التنبيه", FillWeight = 15 });

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
            load();
            return tab;
        }

        // ---------------------------------------------------------------- Shared UI helpers
        private FlowLayoutPanel MakeFilterBar(params Control[] controls)
        {
            var f = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Color.FromArgb(234, 242, 248),
                Padding = new Padding(15, 12, 15, 8),
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight
            };
            f.Controls.AddRange(controls);
            return f;
        }

        private Label MakeFilterLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = UiTheme.Primary,
                Margin = new Padding(8, 10, 0, 2)
            };
        }

        private Panel MakeCard(int height)
        {
            return new Panel
            {
                Dock = DockStyle.Top,
                Height = height,
                BackColor = UiTheme.PanelBg,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(22)
            };
        }

        private Label MakeCardLabel()
        {
            // RightToLeft is inherited, so default TopLeft alignment renders on the right.
            return new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 15F)
            };
        }

        private DataGridView NewGrid()
        {
            var g = new DataGridView
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
            UiTheme.StyleGridHeader(g);
            return g;
        }
    }
}

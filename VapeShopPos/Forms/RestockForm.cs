using System;
using System.Drawing;
using System.Windows.Forms;
using VapeShopPos.Models;
using VapeShopPos.Services;

namespace VapeShopPos.Forms
{
    public class RestockForm : Form
    {
        private TextBox txtSearch;
        private DataGridView dgv;
        private TextBox txtQty;
        private TextBox txtNote;

        public RestockForm()
        {
            Text = "إضافة مخزون (توريد / تالف)";
            StartPosition = FormStartPosition.CenterParent;
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(850, 600);
            UiTheme.ApplyRtl(this);
            BuildUi();
            LoadProducts();
        }

        private void BuildUi()
        {
            var top = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = UiTheme.PanelBg, Padding = new Padding(12, 10, 12, 8) };
            var filterRow = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            var lbl = new Label { Text = "بحث:", AutoSize = true, Font = new Font("Segoe UI", 11F), Margin = new Padding(6, 12, 0, 0) };
            txtSearch = new TextBox { Width = 300, Font = new Font("Segoe UI", 12F), Margin = new Padding(4, 8, 0, 0) };
            txtSearch.TextChanged += (s, e) => LoadProducts();
            filterRow.Controls.AddRange(new Control[] { lbl, txtSearch });

            var backRow = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, RightToLeft = RightToLeft.No };
            backRow.Controls.Add(UiTheme.MakeBackButton(this));

            top.Controls.Add(filterRow);
            top.Controls.Add(backRow);

            dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                Font = new Font("Segoe UI", 11F),
                RowTemplate = { Height = 32 }
            };
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "name", HeaderText = "الصنف", FillWeight = 45 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "barcode", HeaderText = "الباركود", FillWeight = 30 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "qty", HeaderText = "المخزون الحالي", FillWeight = 25 });
            UiTheme.StyleGridHeader(dgv);

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 150, BackColor = UiTheme.PanelBg, Padding = new Padding(12, 10, 12, 10) };

            // Inputs group on the right.
            var inputRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 50, AutoSize = false, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            var lblQty = new Label { Text = "الكمية:", AutoSize = true, Font = new Font("Segoe UI", 12F), Margin = new Padding(6, 14, 0, 0) };
            txtQty = new TextBox { Width = 120, Font = new Font("Segoe UI", 14F), Text = "1", Margin = new Padding(4, 10, 24, 0) };
            txtQty.KeyPress += DigitsOnly;
            var lblNote = new Label { Text = "ملاحظة:", AutoSize = true, Font = new Font("Segoe UI", 12F), Margin = new Padding(6, 14, 0, 0) };
            txtNote = new TextBox { Width = 340, Font = new Font("Segoe UI", 12F), Margin = new Padding(4, 12, 0, 0) };
            inputRow.Controls.AddRange(new Control[] { lblQty, txtQty, lblNote, txtNote });

            // Action buttons on the left.
            var actionRow = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 66, AutoSize = false, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, RightToLeft = RightToLeft.No };
            var btnRestock = UiTheme.MakeButton("➕ توريد (إضافة للمخزون)", UiTheme.Accent);
            btnRestock.Width = 300; btnRestock.Height = 56; btnRestock.Margin = new Padding(0, 4, 12, 4);
            btnRestock.Click += (s, e) => Apply(isDamage: false);
            var btnDamage = UiTheme.MakeButton("➖ تسجيل تالف", UiTheme.Danger);
            btnDamage.Width = 260; btnDamage.Height = 56; btnDamage.Margin = new Padding(0, 4, 12, 4);
            btnDamage.Click += (s, e) => Apply(isDamage: true);
            actionRow.Controls.AddRange(new Control[] { btnRestock, btnDamage });

            bottom.Controls.Add(actionRow);
            bottom.Controls.Add(inputRow);

            Controls.Add(dgv);
            Controls.Add(bottom);
            Controls.Add(top);
        }

        private void LoadProducts()
        {
            dgv.Rows.Clear();
            foreach (var p in ProductService.GetProducts(txtSearch.Text))
            {
                int i = dgv.Rows.Add(p.Name, p.Barcode, p.CurrentQuantity);
                dgv.Rows[i].Tag = p;
            }
        }

        private void Apply(bool isDamage)
        {
            var p = dgv.CurrentRow != null ? dgv.CurrentRow.Tag as Product : null;
            if (p == null) { MessageBox.Show("اختر صنفاً أولاً.", "تنبيه"); return; }

            int qty;
            if (!int.TryParse(txtQty.Text, out qty) || qty <= 0)
            {
                MessageBox.Show("أدخل كمية صحيحة أكبر من صفر.", "تنبيه"); return;
            }

            try
            {
                if (isDamage)
                    ProductService.RecordDamage(p.Id, qty, txtNote.Text.Trim(), Session.CurrentUser.Id);
                else
                    ProductService.Restock(p.Id, qty, txtNote.Text.Trim(), Session.CurrentUser.Id);

                MessageBox.Show("تم تسجيل الحركة بنجاح.", "نجاح",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtQty.Text = "1";
                txtNote.Clear();
                LoadProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DigitsOnly(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true;
        }
    }
}

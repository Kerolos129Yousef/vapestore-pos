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
            var top = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = UiTheme.PanelBg, Padding = new Padding(12) };
            var lbl = new Label { Text = "بحث:", AutoSize = true, Location = new Point(12, 16), Font = new Font("Segoe UI", 11F) };
            txtSearch = new TextBox { Location = new Point(70, 12), Width = 300, Font = new Font("Segoe UI", 12F) };
            txtSearch.TextChanged += (s, e) => LoadProducts();
            top.Controls.Add(txtSearch);
            top.Controls.Add(lbl);

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

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 150, BackColor = UiTheme.PanelBg, Padding = new Padding(12) };

            var lblQty = new Label { Text = "الكمية:", AutoSize = true, Location = new Point(12, 18), Font = new Font("Segoe UI", 12F) };
            txtQty = new TextBox { Location = new Point(90, 14), Width = 120, Font = new Font("Segoe UI", 14F), Text = "1" };
            txtQty.KeyPress += DigitsOnly;

            var lblNote = new Label { Text = "ملاحظة:", AutoSize = true, Location = new Point(240, 18), Font = new Font("Segoe UI", 12F) };
            txtNote = new TextBox { Location = new Point(320, 14), Width = 320, Font = new Font("Segoe UI", 12F) };

            var btnRestock = UiTheme.MakeButton("➕ توريد (إضافة للمخزون)", UiTheme.Accent);
            btnRestock.Location = new Point(12, 70); btnRestock.Width = 300; btnRestock.Height = 56;
            btnRestock.Click += (s, e) => Apply(isDamage: false);

            var btnDamage = UiTheme.MakeButton("➖ تسجيل تالف", UiTheme.Danger);
            btnDamage.Location = new Point(330, 70); btnDamage.Width = 260; btnDamage.Height = 56;
            btnDamage.Click += (s, e) => Apply(isDamage: true);

            bottom.Controls.AddRange(new Control[] { lblQty, txtQty, lblNote, txtNote, btnRestock, btnDamage });

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

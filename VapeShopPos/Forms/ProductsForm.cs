using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using VapeShopPos.Models;
using VapeShopPos.Services;

namespace VapeShopPos.Forms
{
    public class ProductsForm : Form
    {
        private TextBox txtSearch;
        private ComboBox cboCategory;
        private CheckBox chkLowStock;
        private DataGridView dgv;

        public ProductsForm()
        {
            Text = "إدارة الأصناف والمخزن";
            StartPosition = FormStartPosition.CenterParent;
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(900, 600);
            UiTheme.ApplyRtl(this);
            BuildUi();
            LoadCategories();
            LoadProducts();
        }

        private void BuildUi()
        {
            var top = new Panel { Dock = DockStyle.Top, Height = 110, BackColor = UiTheme.PanelBg, Padding = new Padding(15) };

            var lblSearch = new Label { Text = "بحث:", AutoSize = true, Location = new Point(15, 18), Font = new Font("Segoe UI", 11F) };
            txtSearch = new TextBox { Location = new Point(70, 14), Width = 260, Font = new Font("Segoe UI", 12F) };
            txtSearch.TextChanged += (s, e) => LoadProducts();

            var lblCat = new Label { Text = "التصنيف:", AutoSize = true, Location = new Point(360, 18), Font = new Font("Segoe UI", 11F) };
            cboCategory = new ComboBox { Location = new Point(440, 14), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12F) };
            cboCategory.SelectedIndexChanged += (s, e) => LoadProducts();

            chkLowStock = new CheckBox { Text = "المخزون المنخفض فقط", AutoSize = true, Location = new Point(680, 16), Font = new Font("Segoe UI", 11F) };
            chkLowStock.CheckedChanged += (s, e) => LoadProducts();

            var btnAdd = UiTheme.MakeButton("➕ إضافة صنف", UiTheme.Accent);
            btnAdd.Location = new Point(15, 55); btnAdd.Width = 160;
            btnAdd.Click += (s, e) => EditProduct(null);

            var btnEdit = UiTheme.MakeButton("✏️ تعديل", UiTheme.Primary);
            btnEdit.Location = new Point(185, 55); btnEdit.Width = 140;
            btnEdit.Click += (s, e) => EditSelected();

            var btnDelete = UiTheme.MakeButton("🗑️ حذف", UiTheme.Danger);
            btnDelete.Location = new Point(335, 55); btnDelete.Width = 140;
            btnDelete.Click += (s, e) => DeleteSelected();

            var btnCats = UiTheme.MakeButton("إدارة التصنيفات", UiTheme.PrimaryDark);
            btnCats.Location = new Point(485, 55); btnCats.Width = 180;
            btnCats.Click += (s, e) => ManageCategories();

            top.Controls.AddRange(new Control[] { lblSearch, txtSearch, lblCat, cboCategory, chkLowStock, btnAdd, btnEdit, btnDelete, btnCats });

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
                RowTemplate = { Height = 34 }
            };
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "id", HeaderText = "#", FillWeight = 8 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "name", HeaderText = "الصنف", FillWeight = 30 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "barcode", HeaderText = "الباركود", FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cat", HeaderText = "التصنيف", FillWeight = 16 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "purchase", HeaderText = "سعر الشراء", FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "sale", HeaderText = "سعر البيع", FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "qty", HeaderText = "المخزون", FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "min", HeaderText = "حد التنبيه", FillWeight = 12 });
            dgv.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) EditSelected(); };

            Controls.Add(dgv);
            Controls.Add(top);
        }

        private void LoadCategories()
        {
            cboCategory.Items.Clear();
            cboCategory.Items.Add(new Category { Id = 0, Name = "كل التصنيفات" });
            foreach (var c in ProductService.GetCategories())
                cboCategory.Items.Add(c);
            cboCategory.SelectedIndex = 0;
        }

        private void LoadProducts()
        {
            long? catId = null;
            var selected = cboCategory.SelectedItem as Category;
            if (selected != null && selected.Id != 0) catId = selected.Id;

            List<Product> products = ProductService.GetProducts(txtSearch.Text, catId, chkLowStock.Checked);

            dgv.Rows.Clear();
            foreach (var p in products)
            {
                int i = dgv.Rows.Add(p.Id, p.Name, p.Barcode, p.CategoryName,
                    p.PurchasePrice.ToString("N2"), p.SalePrice.ToString("N2"),
                    p.CurrentQuantity, p.MinStockLevel);
                dgv.Rows[i].Tag = p;
                if (p.IsLowStock)
                    dgv.Rows[i].DefaultCellStyle.BackColor = Color.FromArgb(255, 235, 235);
            }
        }

        private Product Selected()
        {
            return dgv.CurrentRow != null ? dgv.CurrentRow.Tag as Product : null;
        }

        private void EditSelected()
        {
            var p = Selected();
            if (p == null) { MessageBox.Show("اختر صنفاً أولاً.", "تنبيه"); return; }
            EditProduct(p);
        }

        private void EditProduct(Product existing)
        {
            using (var f = new ProductEditForm(existing))
            {
                if (f.ShowDialog(this) == DialogResult.OK)
                {
                    LoadCategories();
                    LoadProducts();
                }
            }
        }

        private void DeleteSelected()
        {
            var p = Selected();
            if (p == null) { MessageBox.Show("اختر صنفاً أولاً.", "تنبيه"); return; }

            if (ProductService.HasHistory(p.Id))
            {
                MessageBox.Show(
                    "لا يمكن حذف هذا الصنف لأنه مرتبط بفواتير أو حركات مخزون.\n" +
                    "يمكنك تعديل بياناته بدلاً من الحذف.",
                    "غير مسموح", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("حذف الصنف \"" + p.Name + "\"؟", "تأكيد الحذف",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                ProductService.DeleteProduct(p.Id);
                LoadProducts();
            }
        }

        private void ManageCategories()
        {
            using (var f = new Form())
            {
                f.Text = "التصنيفات";
                f.StartPosition = FormStartPosition.CenterParent;
                f.ClientSize = new Size(420, 420);
                UiTheme.ApplyRtl(f);

                var lb = new ListBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12F) };
                Action reload = () =>
                {
                    lb.Items.Clear();
                    foreach (var c in ProductService.GetCategories()) lb.Items.Add(c);
                };
                reload();

                var txt = new TextBox { Dock = DockStyle.Bottom, Font = new Font("Segoe UI", 12F), Height = 32 };
                var btnAdd = UiTheme.MakeButton("إضافة تصنيف", UiTheme.Accent);
                btnAdd.Dock = DockStyle.Bottom;
                btnAdd.Click += (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(txt.Text))
                    {
                        ProductService.AddCategory(txt.Text.Trim());
                        txt.Clear(); reload();
                    }
                };
                var btnDel = UiTheme.MakeButton("حذف المحدد", UiTheme.Danger);
                btnDel.Dock = DockStyle.Bottom;
                btnDel.Click += (s, e) =>
                {
                    var c = lb.SelectedItem as Category;
                    if (c == null) return;
                    try
                    {
                        ProductService.DeleteCategory(c.Id);
                        reload();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("تعذّر الحذف (قد يكون مرتبطاً بأصناف):\n" + ex.Message, "خطأ");
                    }
                };

                f.Controls.Add(lb);
                f.Controls.Add(btnDel);
                f.Controls.Add(btnAdd);
                f.Controls.Add(txt);
                f.ShowDialog(this);
                LoadCategories();
                LoadProducts();
            }
        }
    }
}

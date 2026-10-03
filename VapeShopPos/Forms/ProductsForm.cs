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
            var top = new Panel { Dock = DockStyle.Top, Height = 66, BackColor = UiTheme.PanelBg, Padding = new Padding(12, 10, 12, 8) };

            // ---- Filter group, docked to the right (flows right-to-left under RTL) ----
            var filterRow = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };

            var lblSearch = new Label { Text = "بحث:", AutoSize = true, Font = new Font("Segoe UI", 11F), Margin = new Padding(6, 12, 0, 0) };
            txtSearch = new TextBox { Width = 240, Font = new Font("Segoe UI", 12F), Margin = new Padding(4, 8, 20, 0) };
            txtSearch.TextChanged += (s, e) => LoadProducts();

            var lblCat = new Label { Text = "التصنيف:", AutoSize = true, Font = new Font("Segoe UI", 11F), Margin = new Padding(6, 12, 0, 0) };
            cboCategory = new ComboBox { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12F), Margin = new Padding(4, 8, 20, 0) };
            cboCategory.SelectedIndexChanged += (s, e) => LoadProducts();

            chkLowStock = new CheckBox { Text = "المخزون المنخفض فقط", AutoSize = true, Font = new Font("Segoe UI", 11F), Margin = new Padding(4, 12, 0, 0) };
            chkLowStock.CheckedChanged += (s, e) => LoadProducts();

            filterRow.Controls.AddRange(new Control[] { lblSearch, txtSearch, lblCat, cboCategory, chkLowStock });

            // ---- Buttons group, docked to the left ----
            var btnRow = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, RightToLeft = RightToLeft.No };

            var btnAdd = UiTheme.MakeButton("➕ إضافة صنف", UiTheme.Accent);
            btnAdd.Width = 160; btnAdd.Height = 46; btnAdd.Margin = new Padding(0, 4, 10, 4);
            btnAdd.Click += (s, e) => EditProduct(null);

            var btnEdit = UiTheme.MakeButton("✏️ تعديل", UiTheme.Primary);
            btnEdit.Width = 140; btnEdit.Height = 46; btnEdit.Margin = new Padding(0, 4, 10, 4);
            btnEdit.Click += (s, e) => EditSelected();

            var btnDelete = UiTheme.MakeButton("🗑️ حذف", UiTheme.Danger);
            btnDelete.Width = 140; btnDelete.Height = 46; btnDelete.Margin = new Padding(0, 4, 10, 4);
            btnDelete.Click += (s, e) => DeleteSelected();

            var btnCats = UiTheme.MakeButton("إدارة التصنيفات", UiTheme.PrimaryDark);
            btnCats.Width = 180; btnCats.Height = 46; btnCats.Margin = new Padding(0, 4, 10, 4);
            btnCats.Click += (s, e) => ManageCategories();

            btnRow.Controls.Add(UiTheme.MakeBackButton(this));
            btnRow.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDelete, btnCats });

            top.Controls.Add(btnRow);
            top.Controls.Add(filterRow);

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
            UiTheme.StyleGridHeader(dgv);
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
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.MaximizeBox = false;
                f.MinimizeBox = false;
                f.ClientSize = new Size(440, 500);
                UiTheme.ApplyRtl(f);

                var lb = new ListBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12F), IntegralHeight = false };
                Action reload = () =>
                {
                    lb.Items.Clear();
                    foreach (var c in ProductService.GetCategories()) lb.Items.Add(c);
                };
                reload();

                // Bottom area: input to add a new category + action buttons, always visible.
                var bottom = new Panel { Dock = DockStyle.Bottom, Height = 200, Padding = new Padding(14), BackColor = UiTheme.PanelBg };

                var lblNew = new Label { Text = "اسم التصنيف الجديد:", Dock = DockStyle.Top, Height = 26, Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = UiTheme.Primary, TextAlign = ContentAlignment.MiddleLeft };
                var txt = new TextBox { Dock = DockStyle.Top, Font = new Font("Segoe UI", 13F), Height = 34 };
                var sp1 = new Panel { Dock = DockStyle.Top, Height = 10, BackColor = UiTheme.PanelBg };
                var btnAdd = UiTheme.MakeButton("➕ إضافة تصنيف", UiTheme.Accent);
                btnAdd.Dock = DockStyle.Top; btnAdd.Height = 46;
                var sp2 = new Panel { Dock = DockStyle.Top, Height = 10, BackColor = UiTheme.PanelBg };
                var btnDel = UiTheme.MakeButton("🗑️ حذف المحدد", UiTheme.Danger);
                btnDel.Dock = DockStyle.Top; btnDel.Height = 46;

                btnAdd.Click += (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(txt.Text))
                    {
                        ProductService.AddCategory(txt.Text.Trim());
                        txt.Clear(); reload();
                        txt.Focus();
                    }
                };
                btnDel.Click += (s, e) =>
                {
                    var c = lb.SelectedItem as Category;
                    if (c == null) { MessageBox.Show("اختر تصنيفاً أولاً.", "تنبيه"); return; }
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

                // Add in reverse so the visual order (top -> bottom) is:
                // label, textbox, [gap], add button, [gap], delete button.
                bottom.Controls.Add(btnDel);
                bottom.Controls.Add(sp2);
                bottom.Controls.Add(btnAdd);
                bottom.Controls.Add(sp1);
                bottom.Controls.Add(txt);
                bottom.Controls.Add(lblNew);

                f.Controls.Add(lb);
                f.Controls.Add(bottom);
                f.AcceptButton = btnAdd;   // Enter adds the typed category

                f.ShowDialog(this);
                LoadCategories();
                LoadProducts();
            }
        }
    }
}

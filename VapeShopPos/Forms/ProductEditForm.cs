using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using VapeShopPos.Models;
using VapeShopPos.Services;

namespace VapeShopPos.Forms
{
    public class ProductEditForm : Form
    {
        private readonly Product _existing;    // null => adding a new product

        private TextBox txtName, txtBarcode, txtPurchase, txtSale, txtMin, txtOpening;
        private ComboBox cboCategory;

        public ProductEditForm(Product existing)
        {
            _existing = existing;
            Text = existing == null ? "إضافة صنف" : "تعديل صنف";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            ClientSize = new Size(440, 470);
            UiTheme.ApplyRtl(this);
            BuildUi();
            if (existing != null) Fill(existing);
        }

        private void BuildUi()
        {
            int y = 20;
            Func<string, Control, int> row = (labelText, ctrl) =>
            {
                var lbl = new Label { Text = labelText, AutoSize = true, Location = new Point(25, y), Font = new Font("Segoe UI", 11F) };
                ctrl.Location = new Point(25, y + 26);
                ctrl.Width = 380;
                if (ctrl is TextBox || ctrl is ComboBox) ctrl.Font = new Font("Segoe UI", 12F);
                Controls.Add(lbl);
                Controls.Add(ctrl);
                y += 64;
                return y;
            };

            txtName = new TextBox();
            row("اسم الصنف *", txtName);

            txtBarcode = new TextBox();
            row("الباركود (اختياري)", txtBarcode);

            cboCategory = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var c in ProductService.GetCategories()) cboCategory.Items.Add(c);
            row("التصنيف", cboCategory);

            // Prices on one row
            var lblP = new Label { Text = "سعر الشراء", AutoSize = true, Location = new Point(25, y), Font = new Font("Segoe UI", 11F) };
            txtPurchase = new TextBox { Location = new Point(25, y + 26), Width = 180, Font = new Font("Segoe UI", 12F), Text = "0" };
            var lblS = new Label { Text = "سعر البيع *", AutoSize = true, Location = new Point(225, y), Font = new Font("Segoe UI", 11F) };
            txtSale = new TextBox { Location = new Point(225, y + 26), Width = 180, Font = new Font("Segoe UI", 12F), Text = "0" };
            Controls.AddRange(new Control[] { lblP, txtPurchase, lblS, txtSale });
            y += 64;

            // Min stock + opening qty on one row
            var lblMin = new Label { Text = "حد التنبيه", AutoSize = true, Location = new Point(25, y), Font = new Font("Segoe UI", 11F) };
            txtMin = new TextBox { Location = new Point(25, y + 26), Width = 180, Font = new Font("Segoe UI", 12F), Text = "0" };
            var lblOpen = new Label { Text = "رصيد افتتاحي", AutoSize = true, Location = new Point(225, y), Font = new Font("Segoe UI", 11F) };
            txtOpening = new TextBox { Location = new Point(225, y + 26), Width = 180, Font = new Font("Segoe UI", 12F), Text = "0" };
            Controls.AddRange(new Control[] { lblMin, txtMin, lblOpen, txtOpening });
            y += 70;

            foreach (var t in new[] { txtPurchase, txtSale }) t.KeyPress += DecimalOnly;
            foreach (var t in new[] { txtMin, txtOpening }) t.KeyPress += DigitsOnly;

            var btnSave = UiTheme.MakeButton("حفظ", UiTheme.Accent);
            btnSave.Location = new Point(25, y); btnSave.Width = 180;
            btnSave.Click += (s, e) => Save();
            var btnCancel = UiTheme.MakeButton("إلغاء", UiTheme.PrimaryDark);
            btnCancel.Location = new Point(225, y); btnCancel.Width = 180;
            btnCancel.DialogResult = DialogResult.Cancel;
            Controls.AddRange(new Control[] { btnSave, btnCancel });
            CancelButton = btnCancel;
        }

        private void Fill(Product p)
        {
            txtName.Text = p.Name;
            txtBarcode.Text = p.Barcode;
            txtPurchase.Text = p.PurchasePrice.ToString(CultureInfo.InvariantCulture);
            txtSale.Text = p.SalePrice.ToString(CultureInfo.InvariantCulture);
            txtMin.Text = p.MinStockLevel.ToString();

            // Opening-stock field only applies when adding; disable on edit.
            txtOpening.Text = p.CurrentQuantity.ToString();
            txtOpening.Enabled = false;

            for (int i = 0; i < cboCategory.Items.Count; i++)
                if (((Category)cboCategory.Items[i]).Id == p.CategoryId)
                { cboCategory.SelectedIndex = i; break; }
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("اسم الصنف مطلوب.", "تنبيه"); return;
            }

            decimal purchase = ParseDecimal(txtPurchase.Text);
            decimal sale = ParseDecimal(txtSale.Text);
            int min = ParseInt(txtMin.Text);
            var cat = cboCategory.SelectedItem as Category;

            var p = new Product
            {
                Id = _existing != null ? _existing.Id : 0,
                Name = txtName.Text.Trim(),
                Barcode = txtBarcode.Text.Trim(),
                CategoryId = cat != null ? (long?)cat.Id : null,
                PurchasePrice = purchase,
                SalePrice = sale,
                MinStockLevel = min
            };

            try
            {
                if (_existing == null)
                {
                    int opening = ParseInt(txtOpening.Text);
                    ProductService.AddProduct(p, opening, Session.CurrentUser.Id);
                }
                else
                {
                    ProductService.UpdateProduct(p);
                }
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                // Most likely a duplicate barcode (UNIQUE constraint).
                MessageBox.Show("تعذّر الحفظ:\n" + ex.Message +
                    "\n\n(تأكد أن الباركود غير مكرر)", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static decimal ParseDecimal(string s)
        {
            decimal d;
            return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out d) ? d : 0;
        }

        private static int ParseInt(string s)
        {
            int i;
            return int.TryParse(s, out i) ? i : 0;
        }

        private void DigitsOnly(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true;
        }

        private void DecimalOnly(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar)) return;
            if (!char.IsDigit(e.KeyChar) && e.KeyChar != '.') e.Handled = true;
        }
    }
}

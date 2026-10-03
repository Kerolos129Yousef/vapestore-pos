using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using VapeShopPos.Models;
using VapeShopPos.Services;

namespace VapeShopPos.Forms
{
    public class SalesForm : Form
    {
        // One line in the current invoice being built.
        private class CartLine
        {
            public long ProductId;
            public string Name;
            public decimal UnitPrice;
            public int Quantity;
            public int Available;      // stock on hand when added (for warnings)
            public decimal LineTotal { get { return UnitPrice * Quantity; } }
        }

        // Scan this special code to record a drawer cash movement (transfer services).
        private const string DrawerBarcode = "DRAWER";

        private readonly List<CartLine> _cart = new List<CartLine>();

        private TextBox txtScan;
        private DataGridView dgv;
        private Label lblSubtotal;
        private TextBox txtDiscount;
        private Label lblTotal;
        private RadioButton rbCash;
        private RadioButton rbCard;

        public SalesForm()
        {
            BuildUi();
        }

        private void BuildUi()
        {
            Text = "شاشة البيع";
            StartPosition = FormStartPosition.CenterParent;
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(900, 650);
            UiTheme.ApplyRtl(this);

            // ---- Scan / search bar ----
            var topPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(234, 242, 248),
                Padding = new Padding(18, 12, 18, 12)
            };

            var lblScan = new Label
            {
                Text = "امسح الباركود أو اكتب اسم الصنف ثم اضغط Enter",
                Dock = DockStyle.Top,
                Height = 32,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = UiTheme.Primary,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var spacer = new Panel { Dock = DockStyle.Top, Height = 8, BackColor = Color.Transparent };

            // Colored accent border around the input so it reads as the primary field.
            var fieldBorder = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = UiTheme.Accent,
                Padding = new Padding(2)
            };
            var fieldInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12, 0, 6, 0) };

            var lblIcon = new Label
            {
                Text = "🔍",
                Dock = DockStyle.Right,
                Width = 48,
                Font = new Font("Segoe UI", 18F),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.White,
                ForeColor = UiTheme.Primary
            };

            txtScan = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 22F),
                BorderStyle = BorderStyle.None
            };
            txtScan.KeyDown += TxtScan_KeyDown;

            fieldInner.Controls.Add(txtScan);
            fieldInner.Controls.Add(lblIcon);
            fieldBorder.Controls.Add(fieldInner);

            topPanel.Controls.Add(fieldBorder);
            topPanel.Controls.Add(spacer);
            topPanel.Controls.Add(lblScan);

            // ---- Cart grid ----
            dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 12F),
                RowTemplate = { Height = 38 }
            };
            UiTheme.StyleGridHeader(dgv);
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "name", HeaderText = "الصنف", ReadOnly = true, FillWeight = 40 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "price", HeaderText = "السعر", ReadOnly = true, FillWeight = 18 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "qty", HeaderText = "الكمية", FillWeight = 18 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "total", HeaderText = "الإجمالي", ReadOnly = true, FillWeight = 24 });
            dgv.CellEndEdit += Dgv_CellEndEdit;
            dgv.EditingControlShowing += (s, e) =>
            {
                if (e.Control is TextBox tb) tb.KeyPress += DigitsOnly;
            };

            // ---- Bottom: totals + actions ----
            var bottom = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.PanelBg, Padding = new Padding(15) };

            // Right column: subtotal, discount, payment, then the big final total on its
            // own row at the bottom (nothing beside it, so the large text never overlaps).
            lblSubtotal = new Label { Text = "المجموع: 0.00 ج.م", AutoSize = true, Location = new Point(20, 12), Font = new Font("Segoe UI", 12F) };

            var lblDisc = new Label { Text = "الخصم:", AutoSize = true, Location = new Point(20, 54), Font = new Font("Segoe UI", 12F) };
            txtDiscount = new TextBox { Location = new Point(90, 51), Width = 120, Font = new Font("Segoe UI", 12F), Text = "0" };
            txtDiscount.TextChanged += (s, e) => Recalculate();
            txtDiscount.KeyPress += DecimalOnly;

            rbCash = new RadioButton { Text = "نقدي 💵", Checked = true, AutoSize = true, Location = new Point(20, 90), Font = new Font("Segoe UI", 12F) };
            rbCard = new RadioButton { Text = "بطاقة 💳", AutoSize = true, Location = new Point(140, 90), Font = new Font("Segoe UI", 12F) };

            lblTotal = new Label
            {
                Text = "الإجمالي النهائي: 0.00 ج.م",
                AutoSize = true,
                Location = new Point(20, 132),
                Font = UiTheme.BigTotalFont,
                ForeColor = UiTheme.Accent
            };

            // Middle column: remove / clear buttons (top of the panel).
            var btnRemove = UiTheme.MakeButton("حذف الصنف المحدد", UiTheme.Danger);
            btnRemove.Size = new Size(200, 48);
            btnRemove.Location = new Point(300, 12);
            btnRemove.Click += (s, e) => RemoveSelected();

            var btnClear = UiTheme.MakeButton("إلغاء الفاتورة", UiTheme.PrimaryDark);
            btnClear.Size = new Size(200, 48);
            btnClear.Location = new Point(300, 68);
            btnClear.Click += (s, e) => ClearCart();

            // Left column: large checkout button.
            var btnCheckout = UiTheme.MakeButton("إتمام البيع (F2)", UiTheme.Accent);
            btnCheckout.Size = new Size(260, 110);
            btnCheckout.Location = new Point(520, 12);
            btnCheckout.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
            btnCheckout.Click += (s, e) => Checkout();

            bottom.Controls.AddRange(new Control[] { lblSubtotal, lblDisc, txtDiscount, lblTotal, rbCash, rbCard, btnRemove, btnClear, btnCheckout });

            // Fixed 3-row layout: scan bar (top), cart grid (fills), totals+actions (bottom).
            // Using a TableLayoutPanel guarantees the grid always gets the correct height
            // and never appears cut off, regardless of window size or maximize timing.
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 120F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 200F));
            root.Controls.Add(topPanel, 0, 0);
            root.Controls.Add(dgv, 0, 1);
            root.Controls.Add(bottom, 0, 2);

            var backBar = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = UiTheme.PanelBg, Padding = new Padding(12, 6, 12, 6) };
            var backRow = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, RightToLeft = RightToLeft.No };
            backRow.Controls.Add(UiTheme.MakeBackButton(this));
            backBar.Controls.Add(backRow);

            Controls.Add(root);
            Controls.Add(backBar);

            // Keyboard shortcuts: F2 checkout, Esc clears scan box.
            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F2) { Checkout(); e.Handled = true; }
            };

            Shown += (s, e) => txtScan.Focus();
        }

        // ---------------------------------------------------------------- Scanning
        private void TxtScan_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.Handled = true;
            e.SuppressKeyPress = true;

            string text = txtScan.Text.Trim();
            if (text.Length == 0) return;

            // Special drawer barcode: open the cash deposit/withdraw dialog.
            if (string.Equals(text, DrawerBarcode, StringComparison.OrdinalIgnoreCase))
            {
                using (var f = new CashMovementForm()) f.ShowDialog(this);
                txtScan.Clear();
                txtScan.Focus();
                return;
            }

            // Try exact barcode first (the common scanner path), then name search.
            Product p = ProductService.GetProductByBarcode(text);
            if (p == null)
            {
                var matches = ProductService.GetProducts(search: text);
                if (matches.Count == 0)
                {
                    MessageBox.Show("لم يتم العثور على صنف بهذا الباركود أو الاسم.", "غير موجود",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtScan.SelectAll();
                    return;
                }
                p = matches.Count == 1 ? matches[0] : PickProduct(matches);
            }

            if (p != null) AddToCart(p);
            txtScan.Clear();
            txtScan.Focus();
        }

        private Product PickProduct(List<Product> matches)
        {
            using (var picker = new Form())
            {
                picker.Text = "اختر الصنف";
                picker.StartPosition = FormStartPosition.CenterParent;
                picker.ClientSize = new Size(500, 400);
                UiTheme.ApplyRtl(picker);

                var lb = new ListBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 13F) };
                foreach (var m in matches)
                    lb.Items.Add(m.Name + "  —  " + UiTheme.Money(m.SalePrice) + "  (متاح: " + m.CurrentQuantity + ")");

                var ok = UiTheme.MakeButton("إضافة", UiTheme.Accent);
                ok.Dock = DockStyle.Bottom;
                ok.DialogResult = DialogResult.OK;
                lb.DoubleClick += (s, e) => { picker.DialogResult = DialogResult.OK; picker.Close(); };

                picker.Controls.Add(lb);
                picker.Controls.Add(ok);
                picker.AcceptButton = ok;

                if (picker.ShowDialog(this) == DialogResult.OK && lb.SelectedIndex >= 0)
                    return matches[lb.SelectedIndex];
            }
            return null;
        }

        // ---------------------------------------------------------------- Cart ops
        private void AddToCart(Product p)
        {
            var existing = _cart.FirstOrDefault(c => c.ProductId == p.Id);
            if (existing != null)
            {
                if (!EnsureStock(p, existing.Quantity + 1)) return;
                existing.Quantity++;
            }
            else
            {
                if (!EnsureStock(p, 1)) return;
                _cart.Add(new CartLine
                {
                    ProductId = p.Id,
                    Name = p.Name,
                    UnitPrice = p.SalePrice,
                    Quantity = 1,
                    Available = p.CurrentQuantity
                });
            }
            RefreshGrid();
        }

        /// <summary>Warns on overselling; cashiers are blocked, managers may override.</summary>
        private bool EnsureStock(Product p, int requestedQty)
        {
            if (requestedQty <= p.CurrentQuantity) return true;

            if (Session.IsManager)
            {
                var r = MessageBox.Show(
                    string.Format("الكمية المتاحة من \"{0}\" هي {1} فقط.\nهل تريد المتابعة (تجاوز المدير)؟",
                        p.Name, p.CurrentQuantity),
                    "تحذير: مخزون غير كافٍ", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                return r == DialogResult.Yes;
            }

            MessageBox.Show(
                string.Format("الكمية المتاحة من \"{0}\" هي {1} فقط. لا يمكن البيع بأكثر من المتاح.",
                    p.Name, p.CurrentQuantity),
                "مخزون غير كافٍ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        private void RefreshGrid()
        {
            dgv.Rows.Clear();
            foreach (var line in _cart)
            {
                int i = dgv.Rows.Add(line.Name, line.UnitPrice.ToString("N2"),
                    line.Quantity, line.LineTotal.ToString("N2"));
                dgv.Rows[i].Tag = line;
            }
            Recalculate();
        }

        private void Dgv_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (dgv.Columns[e.ColumnIndex].Name != "qty") return;
            var line = dgv.Rows[e.RowIndex].Tag as CartLine;
            if (line == null) return;

            int qty;
            if (!int.TryParse(Convert.ToString(dgv.Rows[e.RowIndex].Cells["qty"].Value), out qty) || qty < 1)
                qty = 1;

            var p = ProductService.GetProductById(line.ProductId);
            if (p != null && !EnsureStock(p, qty))
                qty = Math.Min(line.Quantity, p.CurrentQuantity < 1 ? 1 : p.CurrentQuantity);

            line.Quantity = qty;
            RefreshGrid();
        }

        private void RemoveSelected()
        {
            if (dgv.CurrentRow == null) return;
            var line = dgv.CurrentRow.Tag as CartLine;
            if (line != null) _cart.Remove(line);
            RefreshGrid();
        }

        private void ClearCart()
        {
            if (_cart.Count == 0) return;
            if (MessageBox.Show("إلغاء الفاتورة الحالية؟", "تأكيد",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _cart.Clear();
                txtDiscount.Text = "0";
                RefreshGrid();
                txtScan.Focus();
            }
        }

        private decimal ParsedDiscount()
        {
            decimal d;
            return decimal.TryParse(txtDiscount.Text, out d) && d > 0 ? d : 0;
        }

        private void Recalculate()
        {
            decimal subtotal = _cart.Sum(c => c.LineTotal);
            decimal discount = ParsedDiscount();
            if (discount > subtotal) discount = subtotal;
            decimal total = subtotal - discount;

            lblSubtotal.Text = "المجموع: " + UiTheme.Money(subtotal);
            lblTotal.Text = "الإجمالي النهائي: " + UiTheme.Money(total);
        }

        // ---------------------------------------------------------------- Checkout
        private void Checkout()
        {
            if (_cart.Count == 0)
            {
                MessageBox.Show("الفاتورة فارغة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!Session.HasOpenShift)
            {
                MessageBox.Show("لا توجد وردية مفتوحة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal subtotal = _cart.Sum(c => c.LineTotal);
            decimal discount = ParsedDiscount();
            if (discount > subtotal) discount = subtotal;
            decimal total = subtotal - discount;
            string payment = rbCard.Checked ? PaymentMethods.Card : PaymentMethods.Cash;

            var confirm = MessageBox.Show(
                string.Format("تأكيد البيع؟\n\nالإجمالي: {0}\nطريقة الدفع: {1}",
                    UiTheme.Money(total), payment == PaymentMethods.Cash ? "نقدي" : "بطاقة"),
                "إتمام البيع", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            try
            {
                var items = _cart.Select(c => new InvoiceItem
                {
                    ProductId = c.ProductId,
                    ProductName = c.Name,
                    Quantity = c.Quantity,
                    UnitPrice = c.UnitPrice
                });

                var invoice = SalesService.CreateInvoice(items, discount, payment,
                    Session.CurrentShift.Id, Session.CurrentUser.Id);

                MessageBox.Show(
                    string.Format("تم إتمام البيع ✓\n\nرقم الفاتورة: {0}\nالإجمالي: {1}",
                        invoice.Id, UiTheme.Money(invoice.Total)),
                    "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _cart.Clear();
                txtDiscount.Text = "0";
                RefreshGrid();
                txtScan.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل إتمام البيع:\n" + ex.Message, "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ---------------------------------------------------------------- Input filters
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

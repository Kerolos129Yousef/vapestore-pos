using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using VapeShopPos.Services;

namespace VapeShopPos.Forms
{
    /// <summary>
    /// Quick dialog (opened by scanning the special drawer barcode) to record a
    /// cash deposit/withdrawal on the drawer, with the service type.
    /// </summary>
    public class CashMovementForm : Form
    {
        private readonly TextBox _txtAmount;
        private readonly RadioButton _rbOut;
        private readonly RadioButton _rbIn;
        private readonly TextBox _txtService;

        public CashMovementForm()
        {
            Text = "حركة درج (سحب / إيداع)";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            ClientSize = new Size(440, 330);
            UiTheme.ApplyRtl(this);

            var title = new Label
            {
                Text = "تسجيل حركة على الدرج",
                Font = UiTheme.HeaderFont,
                ForeColor = UiTheme.Primary,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Size = new Size(400, 34),
                Location = new Point(20, 15)
            };

            var lblAmount = new Label { Text = "المبلغ (ج.م):", AutoSize = true, Location = new Point(20, 62), Font = new Font("Segoe UI", 12F) };
            _txtAmount = new TextBox { Location = new Point(20, 90), Width = 400, Font = new Font("Segoe UI", 16F) };
            _txtAmount.KeyPress += DecimalOnly;

            _rbOut = new RadioButton { Text = "سحب من الدرج", Checked = true, AutoSize = true, Location = new Point(20, 140), Font = new Font("Segoe UI", 12F) };
            _rbIn = new RadioButton { Text = "إيداع في الدرج", AutoSize = true, Location = new Point(220, 140), Font = new Font("Segoe UI", 12F) };

            var lblService = new Label { Text = "نوع الخدمة:", AutoSize = true, Location = new Point(20, 180), Font = new Font("Segoe UI", 12F) };
            _txtService = new TextBox { Location = new Point(20, 208), Width = 400, Font = new Font("Segoe UI", 13F) };

            var btnOk = UiTheme.MakeButton("حفظ", UiTheme.Accent);
            btnOk.Size = new Size(190, 50); btnOk.Location = new Point(230, 255);
            btnOk.Click += (s, e) => Save();

            var btnCancel = UiTheme.MakeButton("إلغاء", UiTheme.PrimaryDark);
            btnCancel.Size = new Size(190, 50); btnCancel.Location = new Point(20, 255);
            btnCancel.DialogResult = DialogResult.Cancel;

            Controls.AddRange(new Control[] { title, lblAmount, _txtAmount, _rbOut, _rbIn, lblService, _txtService, btnOk, btnCancel });
            AcceptButton = btnOk;
            CancelButton = btnCancel;
            Shown += (s, e) => _txtAmount.Focus();
        }

        private void Save()
        {
            if (!Session.HasOpenShift)
            {
                MessageBox.Show("لا توجد وردية مفتوحة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal amount;
            if (!decimal.TryParse(_txtAmount.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out amount) || amount <= 0)
            {
                MessageBox.Show("أدخل مبلغاً صحيحاً أكبر من صفر.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool isDeposit = _rbIn.Checked;
            long shiftId = Session.CurrentShift.Id;

            // Warn (but allow) if a withdrawal would push the drawer below zero.
            if (!isDeposit)
            {
                decimal available = Session.CurrentShift.OpeningCash
                    + ShiftService.GetShiftCashSalesTotal(shiftId)
                    + CashMovementService.GetShiftNet(shiftId);
                if (amount > available)
                {
                    var r = MessageBox.Show(
                        "الكاش المتاح في الدرج هو " + UiTheme.Money(available) + " فقط.\n" +
                        "السحب بمبلغ " + UiTheme.Money(amount) + " سيجعل رصيد الدرج بالسالب.\n\n" +
                        "هل تريد المتابعة؟",
                        "تحذير: رصيد الدرج غير كافٍ",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                    if (r != DialogResult.Yes) return;
                }
            }

            CashMovementService.Record(shiftId, Session.CurrentUser.Id, isDeposit, amount, _txtService.Text);

            MessageBox.Show(
                (isDeposit ? "تم إيداع " : "تم سحب ") + UiTheme.Money(amount) +
                (string.IsNullOrWhiteSpace(_txtService.Text) ? "" : "\nالخدمة: " + _txtService.Text.Trim()),
                "تم", MessageBoxButtons.OK, MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }

        private void DecimalOnly(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar)) return;
            if (!char.IsDigit(e.KeyChar) && e.KeyChar != '.') e.Handled = true;
        }
    }
}

using System;
using System.Drawing;
using System.Windows.Forms;
using VapeShopPos.Models;
using VapeShopPos.Services;

namespace VapeShopPos.Forms
{
    public class UserEditForm : Form
    {
        private readonly User _existing;    // null => adding
        private TextBox txtUser, txtPass;
        private ComboBox cboRole;

        public UserEditForm(User existing)
        {
            _existing = existing;
            Text = existing == null ? "إضافة مستخدم" : "تعديل مستخدم";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            ClientSize = new Size(400, 340);
            UiTheme.ApplyRtl(this);
            BuildUi();
            if (existing != null) Fill(existing);
        }

        private void BuildUi()
        {
            var lblUser = new Label { Text = "اسم المستخدم *", AutoSize = true, Location = new Point(25, 20), Font = new Font("Segoe UI", 11F) };
            txtUser = new TextBox { Location = new Point(25, 48), Width = 340, Font = new Font("Segoe UI", 12F) };

            var lblPass = new Label
            {
                Text = _existing == null ? "كلمة المرور *" : "كلمة مرور جديدة (اتركها فارغة لعدم التغيير)",
                AutoSize = true, Location = new Point(25, 95), Font = new Font("Segoe UI", 11F)
            };
            txtPass = new TextBox { Location = new Point(25, 123), Width = 340, Font = new Font("Segoe UI", 12F), UseSystemPasswordChar = true };

            var lblRole = new Label { Text = "الصلاحية", AutoSize = true, Location = new Point(25, 170), Font = new Font("Segoe UI", 11F) };
            cboRole = new ComboBox { Location = new Point(25, 198), Width = 340, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12F) };
            cboRole.Items.Add("كاشير");
            cboRole.Items.Add("مدير");
            cboRole.SelectedIndex = 0;

            var btnSave = UiTheme.MakeButton("حفظ", UiTheme.Accent);
            btnSave.Location = new Point(25, 250); btnSave.Width = 160;
            btnSave.Click += (s, e) => Save();
            var btnCancel = UiTheme.MakeButton("إلغاء", UiTheme.PrimaryDark);
            btnCancel.Location = new Point(205, 250); btnCancel.Width = 160;
            btnCancel.DialogResult = DialogResult.Cancel;
            CancelButton = btnCancel;

            Controls.AddRange(new Control[] { lblUser, txtUser, lblPass, txtPass, lblRole, cboRole, btnSave, btnCancel });
        }

        private void Fill(User u)
        {
            txtUser.Text = u.Username;
            cboRole.SelectedIndex = u.IsManager ? 1 : 0;
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(txtUser.Text))
            {
                MessageBox.Show("اسم المستخدم مطلوب.", "تنبيه"); return;
            }
            if (_existing == null && string.IsNullOrEmpty(txtPass.Text))
            {
                MessageBox.Show("كلمة المرور مطلوبة.", "تنبيه"); return;
            }

            string role = cboRole.SelectedIndex == 1 ? Roles.Manager : Roles.Cashier;

            // Guard: don't demote the last manager to cashier.
            if (_existing != null && _existing.IsManager && role == Roles.Cashier
                && UserService.CountManagers() <= 1)
            {
                MessageBox.Show("لا يمكن تغيير صلاحية آخر مدير إلى كاشير.", "غير مسموح");
                return;
            }

            try
            {
                if (_existing == null)
                    UserService.AddUser(txtUser.Text, txtPass.Text, role);
                else
                    UserService.UpdateUser(_existing.Id, txtUser.Text, role,
                        string.IsNullOrEmpty(txtPass.Text) ? null : txtPass.Text);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("تعذّر الحفظ:\n" + ex.Message +
                    "\n\n(قد يكون اسم المستخدم مكرراً)", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

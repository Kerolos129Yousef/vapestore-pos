using System;
using System.Drawing;
using System.Windows.Forms;
using VapeShopPos.Services;

namespace VapeShopPos.Forms
{
    public class LoginForm : Form
    {
        private TextBox txtUser;
        private TextBox txtPass;
        private Button btnLogin;
        private Label lblError;

        public LoginForm()
        {
            BuildUi();
        }

        private void BuildUi()
        {
            Text = "تسجيل الدخول - نظام نقطة البيع";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(420, 360);
            UiTheme.ApplyRtl(this);

            var lblTitle = new Label
            {
                Text = "نظام نقطة البيع 🛒",
                Font = UiTheme.HeaderFont,
                ForeColor = UiTheme.Primary,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 70
            };

            var lblUser = new Label { Text = "اسم المستخدم", AutoSize = true, Location = new Point(40, 90) };
            txtUser = new TextBox { Location = new Point(40, 118), Width = 330, Font = UiTheme.BaseFont };

            var lblPass = new Label { Text = "كلمة المرور", AutoSize = true, Location = new Point(40, 160) };
            txtPass = new TextBox { Location = new Point(40, 188), Width = 330, Font = UiTheme.BaseFont, UseSystemPasswordChar = true };

            btnLogin = UiTheme.MakeButton("دخول", UiTheme.Primary);
            btnLogin.Location = new Point(40, 238);
            btnLogin.Width = 330;
            btnLogin.Click += (s, e) => DoLogin();

            lblError = new Label
            {
                ForeColor = UiTheme.Danger,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(40, 296),
                Size = new Size(330, 40)
            };

            Controls.Add(lblTitle);
            Controls.Add(lblUser);
            Controls.Add(txtUser);
            Controls.Add(lblPass);
            Controls.Add(txtPass);
            Controls.Add(btnLogin);
            Controls.Add(lblError);

            AcceptButton = btnLogin;
            txtPass.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) DoLogin(); };
        }

        private void DoLogin()
        {
            lblError.Text = "";
            try
            {
                var user = UserService.Authenticate(txtUser.Text, txtPass.Text);
                if (user == null)
                {
                    lblError.Text = "اسم المستخدم أو كلمة المرور غير صحيحة.";
                    txtPass.SelectAll();
                    txtPass.Focus();
                    return;
                }
                Session.CurrentUser = user;
                Session.CurrentShift = ShiftService.GetOpenShiftForUser(user.Id);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                lblError.Text = "خطأ: " + ex.Message;
            }
        }
    }
}

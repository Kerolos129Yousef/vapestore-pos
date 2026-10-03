using System;
using System.Drawing;
using System.Windows.Forms;
using VapeShopPos.Models;
using VapeShopPos.Services;

namespace VapeShopPos.Forms
{
    public class UsersForm : Form
    {
        private DataGridView dgv;

        public UsersForm()
        {
            Text = "إدارة المستخدمين";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(640, 520);
            UiTheme.ApplyRtl(this);
            BuildUi();
            LoadUsers();
        }

        private void BuildUi()
        {
            var top = new Panel { Dock = DockStyle.Top, Height = 66, BackColor = UiTheme.PanelBg, Padding = new Padding(12, 10, 12, 8) };

            // Buttons group, docked to the left.
            var btnRow = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, RightToLeft = RightToLeft.No };

            var btnAdd = UiTheme.MakeButton("➕ إضافة مستخدم", UiTheme.Accent);
            btnAdd.Width = 180; btnAdd.Height = 46; btnAdd.Margin = new Padding(0, 4, 10, 4);
            btnAdd.Click += (s, e) => EditUser(null);

            var btnEdit = UiTheme.MakeButton("✏️ تعديل", UiTheme.Primary);
            btnEdit.Width = 140; btnEdit.Height = 46; btnEdit.Margin = new Padding(0, 4, 10, 4);
            btnEdit.Click += (s, e) => EditSelected();

            var btnDel = UiTheme.MakeButton("🗑️ حذف", UiTheme.Danger);
            btnDel.Width = 140; btnDel.Height = 46; btnDel.Margin = new Padding(0, 4, 10, 4);
            btnDel.Click += (s, e) => DeleteSelected();

            btnRow.Controls.Add(UiTheme.MakeBackButton(this));
            btnRow.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDel });
            top.Controls.Add(btnRow);

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
                Font = new Font("Segoe UI", 12F),
                RowTemplate = { Height = 36 }
            };
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "id", HeaderText = "#", FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "user", HeaderText = "اسم المستخدم", FillWeight = 50 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "role", HeaderText = "الصلاحية", FillWeight = 38 });
            UiTheme.StyleGridHeader(dgv);
            dgv.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) EditSelected(); };

            Controls.Add(dgv);
            Controls.Add(top);
        }

        private void LoadUsers()
        {
            dgv.Rows.Clear();
            foreach (var u in UserService.GetUsers())
            {
                int i = dgv.Rows.Add(u.Id, u.Username, u.IsManager ? "مدير" : "كاشير");
                dgv.Rows[i].Tag = u;
            }
        }

        private User Selected()
        {
            return dgv.CurrentRow != null ? dgv.CurrentRow.Tag as User : null;
        }

        private void EditSelected()
        {
            var u = Selected();
            if (u == null) { MessageBox.Show("اختر مستخدماً أولاً.", "تنبيه"); return; }
            EditUser(u);
        }

        private void EditUser(User existing)
        {
            using (var f = new UserEditForm(existing))
                if (f.ShowDialog(this) == DialogResult.OK)
                    LoadUsers();
        }

        private void DeleteSelected()
        {
            var u = Selected();
            if (u == null) { MessageBox.Show("اختر مستخدماً أولاً.", "تنبيه"); return; }

            if (u.Id == Session.CurrentUser.Id)
            {
                MessageBox.Show("لا يمكنك حذف المستخدم الحالي الذي سجّل الدخول.", "غير مسموح",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (u.IsManager && UserService.CountManagers() <= 1)
            {
                MessageBox.Show("لا يمكن حذف آخر مدير في النظام.", "غير مسموح",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("حذف المستخدم \"" + u.Username + "\"؟", "تأكيد",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    UserService.DeleteUser(u.Id);
                    LoadUsers();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("تعذّر الحذف (قد يكون مرتبطاً بورديات/فواتير):\n" + ex.Message, "خطأ");
                }
            }
        }
    }
}

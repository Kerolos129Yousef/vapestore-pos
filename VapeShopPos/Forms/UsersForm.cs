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
            var top = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = UiTheme.PanelBg, Padding = new Padding(12) };
            var btnAdd = UiTheme.MakeButton("➕ إضافة مستخدم", UiTheme.Accent);
            btnAdd.Location = new Point(12, 8); btnAdd.Width = 180;
            btnAdd.Click += (s, e) => EditUser(null);

            var btnEdit = UiTheme.MakeButton("✏️ تعديل", UiTheme.Primary);
            btnEdit.Location = new Point(200, 8); btnEdit.Width = 140;
            btnEdit.Click += (s, e) => EditSelected();

            var btnDel = UiTheme.MakeButton("🗑️ حذف", UiTheme.Danger);
            btnDel.Location = new Point(350, 8); btnDel.Width = 140;
            btnDel.Click += (s, e) => DeleteSelected();

            top.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDel });

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

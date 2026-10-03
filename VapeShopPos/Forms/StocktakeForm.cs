using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using VapeShopPos.Services;

namespace VapeShopPos.Forms
{
    public class StocktakeForm : Form
    {
        private DataGridView dgv;
        private TextBox txtNote;
        private Label lblSummary;
        private List<StocktakeLine> _lines;

        public StocktakeForm()
        {
            Text = "الجرد (جرد المخزون)";
            StartPosition = FormStartPosition.CenterParent;
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(900, 600);
            UiTheme.ApplyRtl(this);
            BuildUi();
            LoadWorksheet();
        }

        private void BuildUi()
        {
            var top = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = UiTheme.PanelBg, Padding = new Padding(12) };
            var title = new Label
            {
                Text = "أدخل الكمية الفعلية المعدودة لكل صنف، ثم اضغط تأكيد الجرد.",
                AutoSize = true, Location = new Point(12, 18), Font = new Font("Segoe UI", 11F)
            };
            top.Controls.Add(title);

            dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                Font = new Font("Segoe UI", 11F),
                RowTemplate = { Height = 32 }
            };
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "name", HeaderText = "الصنف", ReadOnly = true, FillWeight = 36 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cat", HeaderText = "التصنيف", ReadOnly = true, FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "theoretical", HeaderText = "النظري", ReadOnly = true, FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "counted", HeaderText = "الفعلي (المعدود)", FillWeight = 16 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "variance", HeaderText = "الفرق", ReadOnly = true, FillWeight = 14 });
            dgv.CellEndEdit += Dgv_CellEndEdit;
            dgv.EditingControlShowing += (s, e) =>
            {
                if (e.Control is TextBox tb)
                {
                    tb.KeyPress -= DigitsOnly;
                    tb.KeyPress += DigitsOnly;
                }
            };

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 120, BackColor = UiTheme.PanelBg, Padding = new Padding(12) };
            var lblNote = new Label { Text = "ملاحظة الجرد:", AutoSize = true, Location = new Point(12, 16), Font = new Font("Segoe UI", 11F) };
            txtNote = new TextBox { Location = new Point(130, 12), Width = 400, Font = new Font("Segoe UI", 12F) };

            lblSummary = new Label { AutoSize = true, Location = new Point(12, 55), Font = new Font("Segoe UI", 11F, FontStyle.Bold) };

            var btnConfirm = UiTheme.MakeButton("✓ تأكيد الجرد وتسوية المخزون", UiTheme.Accent);
            btnConfirm.Location = new Point(560, 12); btnConfirm.Width = 300; btnConfirm.Height = 90;
            btnConfirm.Click += (s, e) => Confirm();

            bottom.Controls.AddRange(new Control[] { lblNote, txtNote, lblSummary, btnConfirm });

            Controls.Add(dgv);
            Controls.Add(bottom);
            Controls.Add(top);
        }

        private void LoadWorksheet()
        {
            _lines = StocktakeService.BuildWorksheet();
            dgv.Rows.Clear();
            foreach (var line in _lines)
            {
                int i = dgv.Rows.Add(line.ProductName, line.CategoryName,
                    line.TheoreticalQty, line.CountedQty, line.Variance);
                dgv.Rows[i].Tag = line;
            }
            UpdateSummary();
        }

        private void Dgv_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (dgv.Columns[e.ColumnIndex].Name != "counted") return;
            var line = dgv.Rows[e.RowIndex].Tag as StocktakeLine;
            if (line == null) return;

            int counted;
            if (!int.TryParse(Convert.ToString(dgv.Rows[e.RowIndex].Cells["counted"].Value), out counted) || counted < 0)
                counted = line.TheoreticalQty;

            line.CountedQty = counted;
            dgv.Rows[e.RowIndex].Cells["counted"].Value = counted;
            dgv.Rows[e.RowIndex].Cells["variance"].Value = line.Variance;

            var cell = dgv.Rows[e.RowIndex].Cells["variance"];
            cell.Style.ForeColor = line.Variance == 0 ? Color.Black
                : (line.Variance < 0 ? UiTheme.Danger : UiTheme.Accent);

            UpdateSummary();
        }

        private void UpdateSummary()
        {
            int changed = 0, shortage = 0, surplus = 0;
            foreach (var l in _lines)
            {
                if (l.Variance == 0) continue;
                changed++;
                if (l.Variance < 0) shortage += -l.Variance; else surplus += l.Variance;
            }
            lblSummary.Text = string.Format("أصناف بها فروقات: {0}   |   إجمالي العجز: {1}   |   إجمالي الزيادة: {2}",
                changed, shortage, surplus);
        }

        private void Confirm()
        {
            int changed = 0;
            foreach (var l in _lines) if (l.Variance != 0) changed++;

            if (changed == 0)
            {
                MessageBox.Show("لا توجد فروقات لتسويتها.", "تنبيه",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show(
                string.Format("سيتم تسوية {0} صنفاً حسب الجرد. هل تريد المتابعة؟", changed),
                "تأكيد الجرد", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                int adjusted = StocktakeService.ApplyStocktake(_lines, txtNote.Text.Trim(), Session.CurrentUser.Id);
                MessageBox.Show("تمت تسوية " + adjusted + " صنفاً بنجاح.", "نجاح",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadWorksheet();
                txtNote.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء الجرد:\n" + ex.Message, "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DigitsOnly(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true;
        }
    }
}

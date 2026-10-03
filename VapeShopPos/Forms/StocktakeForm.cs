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
            var top = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = UiTheme.PanelBg, Padding = new Padding(12, 8, 12, 8) };
            var title = new Label
            {
                Text = "أدخل الكمية الفعلية المعدودة لكل صنف، ثم اضغط تأكيد الجرد.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,   // renders on the right under RTL
                Font = new Font("Segoe UI", 11F)
            };
            var backRow = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, RightToLeft = RightToLeft.No };
            backRow.Controls.Add(UiTheme.MakeBackButton(this));

            top.Controls.Add(title);
            top.Controls.Add(backRow);

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
            UiTheme.StyleGridHeader(dgv);

            // Single click starts editing the counted column directly.
            dgv.EditMode = DataGridViewEditMode.EditOnEnter;

            // Make the editable "counted" column stand out as an input field.
            var countedCol = dgv.Columns["counted"];
            countedCol.HeaderText = "✏️ الفعلي (المعدود)";
            countedCol.DefaultCellStyle.BackColor = Color.FromArgb(255, 249, 196);
            countedCol.DefaultCellStyle.ForeColor = Color.FromArgb(30, 30, 30);
            countedCol.DefaultCellStyle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            countedCol.DefaultCellStyle.SelectionBackColor = Color.FromArgb(255, 235, 120);
            countedCol.DefaultCellStyle.SelectionForeColor = Color.Black;
            countedCol.HeaderCell.Style.BackColor = UiTheme.Accent;
            countedCol.HeaderCell.Style.ForeColor = Color.White;

            dgv.CellEndEdit += Dgv_CellEndEdit;
            dgv.EditingControlShowing += (s, e) =>
            {
                if (e.Control is TextBox tb)
                {
                    tb.KeyPress -= DigitsOnly;
                    tb.KeyPress += DigitsOnly;
                }
            };

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 120, BackColor = UiTheme.PanelBg, Padding = new Padding(12, 10, 12, 10) };

            // Confirm button on the left.
            var actionRow = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, RightToLeft = RightToLeft.No };
            var btnConfirm = UiTheme.MakeButton("✓ تأكيد الجرد وتسوية المخزون", UiTheme.Accent);
            btnConfirm.Width = 300; btnConfirm.Height = 92; btnConfirm.Margin = new Padding(0, 2, 12, 2);
            btnConfirm.Click += (s, e) => Confirm();
            actionRow.Controls.Add(btnConfirm);

            // Note input on the right.
            var noteRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            var lblNote = new Label { Text = "ملاحظة الجرد:", AutoSize = true, Font = new Font("Segoe UI", 11F), Margin = new Padding(6, 14, 0, 0) };
            txtNote = new TextBox { Width = 400, Font = new Font("Segoe UI", 12F), Margin = new Padding(4, 10, 0, 0) };
            noteRow.Controls.AddRange(new Control[] { lblNote, txtNote });

            lblSummary = new Label { Dock = DockStyle.Top, Height = 30, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 11F, FontStyle.Bold) };

            bottom.Controls.Add(lblSummary);
            bottom.Controls.Add(noteRow);
            bottom.Controls.Add(actionRow);

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

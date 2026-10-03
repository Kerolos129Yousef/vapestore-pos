using System;
using System.Drawing;
using System.Windows.Forms;
using VapeShopPos.Models;
using VapeShopPos.Services;

namespace VapeShopPos.Forms
{
    /// <summary>Read-only details of a single invoice: header info + its line items.</summary>
    public class InvoiceDetailsForm : Form
    {
        public InvoiceDetailsForm(long invoiceId)
        {
            Invoice inv = InvoiceService.GetInvoiceById(invoiceId);

            Text = "تفاصيل الفاتورة #" + invoiceId;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(620, 540);
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(520, 460);
            MaximizeBox = true;
            UiTheme.ApplyRtl(this);

            var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(25), AutoScroll = true };
            Controls.Add(body);

            if (inv == null)
            {
                body.Controls.Add(new Label
                {
                    Text = "الفاتورة غير موجودة.",
                    Font = UiTheme.HeaderFont,
                    ForeColor = UiTheme.Danger,
                    AutoSize = true,
                    Location = new Point(25, 20)
                });
                return;
            }

            int w = body.ClientSize.Width;

            var title = new Label
            {
                Text = "فاتورة #" + inv.Id,
                Font = UiTheme.HeaderFont,
                ForeColor = UiTheme.Primary,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Size = new Size(w - 50, 36),
                Location = new Point(25, 5),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var info = new Label
            {
                AutoSize = false,
                Size = new Size(w - 50, 170),
                Location = new Point(25, 50),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 12F),
                Text =
                    "التاريخ: " + inv.DateTime.ToString("yyyy-MM-dd HH:mm") + "\n" +
                    "الكاشير: " + inv.UserName + "\n" +
                    "الوردية: #" + inv.ShiftId + "\n" +
                    "طريقة الدفع: " + (inv.PaymentMethod == PaymentMethods.Card ? "بطاقة" : "نقدي") + "\n\n" +
                    "المجموع الفرعي: " + UiTheme.Money(inv.Subtotal) + "\n" +
                    "الخصم: " + UiTheme.Money(inv.Discount) + "\n" +
                    "الإجمالي: " + UiTheme.Money(inv.Total)
            };

            var itemsHeader = new Label
            {
                Text = "أصناف الفاتورة",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = UiTheme.Primary,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Size = new Size(w - 50, 28),
                Location = new Point(25, 225),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var grid = new DataGridView
            {
                Location = new Point(25, 260),
                Size = new Size(w - 50, body.ClientSize.Height - 260 - 25),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = UiTheme.PanelBg,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10F),
                RightToLeft = RightToLeft.Yes,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 34
            };
            grid.ColumnHeadersDefaultCellStyle.BackColor = UiTheme.Primary;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            grid.Columns.Add("name", "الصنف");
            grid.Columns.Add("qty", "الكمية");
            grid.Columns.Add("price", "سعر الوحدة");
            grid.Columns.Add("total", "الإجمالي");
            grid.Columns["name"].FillWeight = 45;

            foreach (var it in InvoiceService.GetInvoiceItems(inv.Id))
            {
                grid.Rows.Add(
                    it.ProductName,
                    it.Quantity,
                    UiTheme.Money(it.UnitPrice),
                    UiTheme.Money(it.LineTotal));
            }

            if (grid.Rows.Count == 0)
                itemsHeader.Text = "أصناف الفاتورة (لا توجد أصناف)";

            body.Controls.Add(title);
            body.Controls.Add(info);
            body.Controls.Add(itemsHeader);
            body.Controls.Add(grid);
        }
    }
}

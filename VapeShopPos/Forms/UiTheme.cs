using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace VapeShopPos.Forms
{
    /// <summary>
    /// Central place for fonts, colors and EGP currency formatting so every screen
    /// looks consistent: large, readable, RTL, counter-friendly.
    /// </summary>
    public static class UiTheme
    {
        public static readonly Font BaseFont = new Font("Segoe UI", 11F);
        public static readonly Font HeaderFont = new Font("Segoe UI", 16F, FontStyle.Bold);
        public static readonly Font ButtonFont = new Font("Segoe UI", 12F, FontStyle.Bold);
        public static readonly Font BigTotalFont = new Font("Segoe UI", 22F, FontStyle.Bold);

        public static readonly Color Primary = Color.FromArgb(33, 97, 140);
        public static readonly Color PrimaryDark = Color.FromArgb(23, 74, 110);
        public static readonly Color Accent = Color.FromArgb(39, 174, 96);
        public static readonly Color Danger = Color.FromArgb(192, 57, 43);
        public static readonly Color Background = Color.FromArgb(245, 247, 250);
        public static readonly Color PanelBg = Color.White;

        /// <summary>Applies RTL layout, base font and background to a form.</summary>
        public static void ApplyRtl(Form form)
        {
            form.RightToLeft = RightToLeft.Yes;
            form.RightToLeftLayout = true;
            form.Font = BaseFont;
            form.BackColor = Background;
        }

        public static Button MakeButton(string text, Color back)
        {
            var b = new Button
            {
                Text = text,
                Font = ButtonFont,
                BackColor = back,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Height = 48,
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        /// <summary>
        /// Applies the shared table header look used across the app: a solid primary-blue
        /// header row with bold, centered white text and a comfortable height.
        /// </summary>
        public static void StyleGridHeader(DataGridView dgv)
        {
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.ColumnHeadersHeight = 42;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Primary;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = Primary;
        }

        /// <summary>
        /// A "back to main menu" button that simply closes the given screen
        /// (every screen is opened as a dialog from the main menu).
        /// </summary>
        public static Button MakeBackButton(Form owner)
        {
            var b = MakeButton("← رجوع للرئيسية", PrimaryDark);
            b.Width = 170;
            b.Height = 42;
            b.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            b.Padding = new Padding(0);
            b.Margin = new Padding(0, 0, 10, 0);
            b.Click += (s, e) => owner.Close();
            return b;
        }

        /// <summary>Formats a decimal as Egyptian Pounds, e.g. "150.00 ج.م".</summary>
        public static string Money(decimal value)
        {
            return value.ToString("N2", CultureInfo.InvariantCulture) + " ج.م";
        }
    }
}

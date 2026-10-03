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

        /// <summary>Formats a decimal as Egyptian Pounds, e.g. "150.00 ج.م".</summary>
        public static string Money(decimal value)
        {
            return value.ToString("N2", CultureInfo.InvariantCulture) + " ج.م";
        }
    }
}

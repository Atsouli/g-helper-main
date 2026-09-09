using System.Drawing.Drawing2D;

namespace GHelper.UI
{
    public sealed class RShortcutTile : RButton
    {
        public string Title { get; set; } = string.Empty;
        public string Shortcut { get; set; } = string.Empty;

        public RShortcutTile()
        {
            BorderRadius = 12;
            BorderColor = RForm.colorStandard;
            Activated = true;
            Text = string.Empty;
            Padding = new Padding(12);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            int scale = Math.Max(1, (int)Math.Round(e.Graphics.DpiX / 192f));
            int iconSize = 34 * scale;
            int iconX = 14 * scale;
            int iconY = Math.Max(8 * scale, (Height - iconSize) / 2);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var iconBrush = new SolidBrush(Color.FromArgb(ControlHelper.DarkMode ? 52 : 28, BorderColor)))
                e.Graphics.FillEllipse(iconBrush, iconX, iconY, iconSize, iconSize);

            using (var iconFont = new Font("Segoe MDL2 Assets", 10F, FontStyle.Regular, GraphicsUnit.Point))
            using (var iconTextBrush = new SolidBrush(BorderColor))
            {
                const string actionGlyph = "\uE945";
                SizeF glyphSize = e.Graphics.MeasureString(actionGlyph, iconFont);
                e.Graphics.DrawString(actionGlyph, iconFont, iconTextBrush,
                    iconX + (iconSize - glyphSize.Width) / 2,
                    iconY + (iconSize - glyphSize.Height) / 2);
            }

            int textX = iconX + iconSize + 12 * scale;
            int textWidth = Math.Max(1, Width - textX - 12 * scale);
            var titleRect = new Rectangle(textX, 9 * scale, textWidth, 24 * scale);
            var shortcutRect = new Rectangle(textX, 34 * scale, textWidth, 21 * scale);

            using var titleFont = new Font(Font.FontFamily, 9F, FontStyle.Bold, GraphicsUnit.Point);
            using var shortcutFont = new Font(Font.FontFamily, 7.5F, FontStyle.Regular, GraphicsUnit.Point);
            TextRenderer.DrawText(e.Graphics, Title, titleFont, titleRect, ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            Color muted = ControlHelper.DarkMode ? Color.FromArgb(185, 205, 215) : Color.FromArgb(85, 95, 110);
            TextRenderer.DrawText(e.Graphics, Shortcut, shortcutFont, shortcutRect, muted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
    }
}

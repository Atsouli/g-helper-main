using System.Drawing.Drawing2D;

namespace GHelper.UI
{
    public sealed class RShortcutTile : RButton
    {
        private bool deletePressed;

        public string Title { get; set; } = string.Empty;
        public string Shortcut { get; set; } = string.Empty;
        public event EventHandler? EditRequested;
        public event EventHandler? DeleteRequested;

        public RShortcutTile()
        {
            BorderRadius = 12;
            BorderColor = RForm.colorStandard;
            Activated = true;
            Text = string.Empty;
            Padding = new Padding(12);
            AccessibleDescription = "Press to run. Hold A or right-click to edit. Select the X to delete.";
        }

        public void RequestEdit() => EditRequested?.Invoke(this, EventArgs.Empty);

        private Rectangle GetDeleteRectangle()
        {
            int scale = Math.Max(1, (int)Math.Round(DeviceDpi / 192f));
            int size = 18 * scale;
            return new Rectangle(Width - size - 4 * scale, 4 * scale, size, size);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && GetDeleteRectangle().Contains(e.Location))
            {
                deletePressed = true;
                Capture = true;
                Invalidate(GetDeleteRectangle());
                return;
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (deletePressed)
            {
                bool delete = e.Button == MouseButtons.Left && GetDeleteRectangle().Contains(e.Location);
                deletePressed = false;
                Capture = false;
                Invalidate(GetDeleteRectangle());
                if (delete) DeleteRequested?.Invoke(this, EventArgs.Empty);
                return;
            }

            if (e.Button == MouseButtons.Right)
            {
                RequestEdit();
                return;
            }

            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            int scale = Math.Max(1, (int)Math.Round(e.Graphics.DpiX / 192f));
            int iconSize = 24 * scale;
            int iconX = 14 * scale;
            int iconY = Math.Max(8 * scale, (Height - iconSize) / 2);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var iconBrush = new SolidBrush(Color.FromArgb(ControlHelper.DarkMode ? 52 : 28, BorderColor)))
                e.Graphics.FillEllipse(iconBrush, iconX, iconY, iconSize, iconSize);

            using (var iconFont = new Font("Segoe MDL2 Assets", 8.5F, FontStyle.Regular, GraphicsUnit.Point))
            using (var iconTextBrush = new SolidBrush(BorderColor))
            {
                const string actionGlyph = "\uE945";
                SizeF glyphSize = e.Graphics.MeasureString(actionGlyph, iconFont);
                e.Graphics.DrawString(actionGlyph, iconFont, iconTextBrush,
                    iconX + (iconSize - glyphSize.Width) / 2,
                    iconY + (iconSize - glyphSize.Height) / 2);
            }

            int textX = iconX + iconSize + 12 * scale;
            int textWidth = Math.Max(1, Width - textX - 36 * scale + 40 * scale);
            var titleRect = new Rectangle(textX, 6 * scale, textWidth, 36 * scale);
            var shortcutRect = new Rectangle(textX, 30 * scale, textWidth, 45 * scale);

            using var titleFont = new Font(Font.FontFamily, 9F, FontStyle.Bold, GraphicsUnit.Point);
            using var shortcutFont = new Font(Font.FontFamily, 7.5F, FontStyle.Regular, GraphicsUnit.Point);
            TextRenderer.DrawText(e.Graphics, Title, titleFont, titleRect, ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);

            Color muted = ControlHelper.DarkMode ? Color.FromArgb(185, 205, 215) : Color.FromArgb(85, 95, 110);
            TextRenderer.DrawText(e.Graphics, Shortcut, shortcutFont, shortcutRect, muted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);

            Rectangle deleteRect = GetDeleteRectangle();
            Color deleteBack = deletePressed
                ? Color.FromArgb(90, 255, 75, 85)
                : Color.FromArgb(ControlHelper.DarkMode ? 48 : 24, 255, 75, 85);
            using (var deleteBrush = new SolidBrush(deleteBack))
                e.Graphics.FillEllipse(deleteBrush, deleteRect);
            using (var deletePen = new Pen(Color.FromArgb(235, 255, 92, 102), Math.Max(1.5f, scale * 1.5f)))
            {
                int inset = 5 * scale;
                e.Graphics.DrawLine(deletePen, deleteRect.Left + inset, deleteRect.Top + inset,
                    deleteRect.Right - inset, deleteRect.Bottom - inset);
                e.Graphics.DrawLine(deletePen, deleteRect.Right - inset, deleteRect.Top + inset,
                    deleteRect.Left + inset, deleteRect.Bottom - inset);
            }
        }
    }
}

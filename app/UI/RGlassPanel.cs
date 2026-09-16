using System.Drawing.Drawing2D;

namespace GHelper.UI
{
    public sealed class RGlassPanel : Panel
    {
        public int CornerRadius { get; set; } = 14;

        public RGlassPanel()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            if (Width < 4 || Height < 4) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var bounds = new Rectangle(0, 3, Width, Height - 7);
            using var path = RComboBox.RoundedRect(bounds, CornerRadius, CornerRadius);

            Color top = ControlHelper.DarkMode
                ? Color.FromArgb(205, 48, 52, 61)
                : Color.FromArgb(225, 255, 255, 255);
            Color bottom = ControlHelper.DarkMode
                ? Color.FromArgb(175, 30, 33, 41)
                : Color.FromArgb(190, 235, 241, 250);

            using (var glass = new LinearGradientBrush(bounds, top, bottom, LinearGradientMode.Vertical))
                e.Graphics.FillPath(glass, path);

            Color border = ControlHelper.DarkMode
                ? Color.FromArgb(105, 255, 255, 255)
                : Color.FromArgb(150, 255, 255, 255);
            using (var borderPen = new Pen(border, 1.2f))
                e.Graphics.DrawPath(borderPen, path);

            var highlightBounds = new Rectangle(bounds.X + 12, bounds.Y + 1, Math.Max(1, bounds.Width - 24), 2);
            using var highlight = new LinearGradientBrush(highlightBounds,
                Color.FromArgb(0, RForm.colorStandard),
                Color.FromArgb(145, RForm.colorStandard),
                LinearGradientMode.Horizontal);
            e.Graphics.FillRectangle(highlight, highlightBounds);
        }
    }
}

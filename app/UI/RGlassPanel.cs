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
            var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = RComboBox.RoundedRect(bounds, 8, 8);

            Color back = ControlHelper.DarkMode
                ? Color.FromArgb(180, 25, 27, 33)
                : Color.FromArgb(200, 255, 255, 255);

            using (var glass = new SolidBrush(back))
                e.Graphics.FillPath(glass, path);

            Color border = ControlHelper.DarkMode
                ? Color.FromArgb(60, 255, 255, 255)
                : Color.FromArgb(150, 200, 200, 200);
            using (var borderPen = new Pen(border, 1f))
                e.Graphics.DrawPath(borderPen, path);
        }
    }
}

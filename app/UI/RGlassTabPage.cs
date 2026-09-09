using System.Drawing.Drawing2D;

namespace GHelper.UI
{
    public sealed class RGlassTabPage : TabPage
    {
        public RGlassTabPage()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Rectangle bounds = ClientRectangle;
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            Color start = ControlHelper.DarkMode ? Color.FromArgb(24, 27, 34) : Color.FromArgb(241, 246, 253);
            Color end = ControlHelper.DarkMode ? Color.FromArgb(16, 18, 24) : Color.FromArgb(226, 236, 249);
            using (var background = new LinearGradientBrush(bounds, start, end, LinearGradientMode.ForwardDiagonal))
                e.Graphics.FillRectangle(background, bounds);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var glow = new SolidBrush(Color.FromArgb(ControlHelper.DarkMode ? 24 : 30, RForm.colorStandard));
            int glowSize = Math.Max(220, Width / 2);
            e.Graphics.FillEllipse(glow, Width - glowSize / 2, -glowSize / 2, glowSize, glowSize);
        }
    }
}

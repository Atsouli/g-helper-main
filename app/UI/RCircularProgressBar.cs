using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GHelper.UI
{
    public class RCircularProgressBar : Control
    {
        private int _value = 0;
        private int _maximum = 100;

        public int Value
        {
            get { return _value; }
            set
            {
                _value = Math.Max(0, Math.Min(value, _maximum));
                Invalidate();
            }
        }

        public int Maximum
        {
            get { return _maximum; }
            set
            {
                _maximum = value;
                Invalidate();
            }
        }

        public RCircularProgressBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size = new Size(100, 100);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            int lineThickness = 8;
            if (Width <= lineThickness * 2 || Height <= lineThickness * 2) return;
            Rectangle rect = new Rectangle(lineThickness, lineThickness, Width - lineThickness * 2, Height - lineThickness * 2);

            // Draw background circle
            using (Pen backPen = new Pen(Color.FromArgb(50, Color.White), lineThickness))
            {
                e.Graphics.DrawArc(backPen, rect, 0, 360);
            }

            // Draw progress arc
            if (_value > 0)
            {
                float angle = (_value / (float)_maximum) * 360f;
                using (Pen progressPen = new Pen(RForm.colorStandard, lineThickness))
                {
                    progressPen.StartCap = LineCap.Round;
                    progressPen.EndCap = LineCap.Round;
                    e.Graphics.DrawArc(progressPen, rect, -90, angle);
                }
            }

            // Draw text
            string text = $"{_value}%";
            using (Font font = new Font("Segoe UI", 16f, FontStyle.Bold))
            {
                SizeF textSize = e.Graphics.MeasureString(text, font);
                PointF textLocation = new PointF((Width - textSize.Width) / 2, (Height - textSize.Height) / 2);
                using (SolidBrush textBrush = new SolidBrush(RForm.foreMain))
                {
                    e.Graphics.DrawString(text, font, textBrush, textLocation);
                }
            }
        }
    }
}

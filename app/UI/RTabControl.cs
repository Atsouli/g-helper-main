using System.Drawing.Drawing2D;

namespace GHelper.UI
{
    public sealed class RTabControl : TabControl
    {
        private bool fittingTabs;
        private bool hideSingleTabHeader;

        public bool HideSingleTabHeader
        {
            get => hideSingleTabHeader;
            set
            {
                if (hideSingleTabHeader == value) return;
                hideSingleTabHeader = value;
                FitAllTabs();
                Invalidate();
            }
        }

        private bool IsSingleTabHeaderHidden => hideSingleTabHeader && TabCount == 1;

        public override Rectangle DisplayRectangle => IsSingleTabHeaderHidden
            ? new Rectangle(0, 0, Width, Height)
            : base.DisplayRectangle;

        public RTabControl()
        {
            DrawMode = TabDrawMode.OwnerDrawFixed;
            SizeMode = TabSizeMode.Fixed;
            ItemSize = new Size(100, 42);
            Padding = new Point(16, 6);
            Appearance = TabAppearance.Normal;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        }

        private void FitAllTabs()
        {
            if (fittingTabs || TabCount == 0 || ClientSize.Width <= 0) return;

            fittingTabs = true;
            try
            {
                if (IsSingleTabHeaderHidden)
                {
                    ItemSize = new Size(1, 1);
                    return;
                }

                int dpi = IsHandleCreated ? DeviceDpi : 96;
                int reserve = Math.Max(8, dpi / 8);
                int tabWidth = Math.Max(56, (ClientSize.Width - reserve) / TabCount);
                int tabHeight = Math.Max(30, Font.Height + Math.Max(8, dpi / 12));
                ItemSize = new Size(tabWidth, tabHeight);
            }
            finally
            {
                fittingTabs = false;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            FitAllTabs();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            FitAllTabs();
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            FitAllTabs();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            FitAllTabs();
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (IsSingleTabHeaderHidden) return;

            bool selected = e.Index == SelectedIndex;
            Rectangle bounds = GetTabRect(e.Index);
            bounds.Inflate(-3, -4);
            Color background = selected
                ? Color.FromArgb(ControlHelper.DarkMode ? 210 : 225, RForm.buttonMain)
                : Color.FromArgb(ControlHelper.DarkMode ? 70 : 85, RForm.buttonSecond);
            Color foreground = selected ? RForm.foreMain : Color.FromArgb(145, RForm.foreMain);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = RComboBox.RoundedRect(bounds, 9, 9))
            using (var backgroundBrush = new SolidBrush(background))
            using (var borderPen = new Pen(Color.FromArgb(selected ? 105 : 45, Color.White)))
            {
                e.Graphics.FillPath(backgroundBrush, path);
                e.Graphics.DrawPath(borderPen, path);
            }

            if (selected)
            {
                using var accentBrush = new SolidBrush(RForm.colorStandard);
                e.Graphics.FillRectangle(accentBrush, bounds.Left + 16, bounds.Bottom - 3, bounds.Width - 32, 3);
            }

            string label = TabPages[e.Index].Text;
            Font drawFont = Font;
            Font? compactFont = null;
            float fontSize = Font.Size;
            while (fontSize > 7F && TextRenderer.MeasureText(label, drawFont).Width > bounds.Width - 8)
            {
                compactFont?.Dispose();
                fontSize -= 0.5F;
                compactFont = new Font(Font.FontFamily, fontSize, Font.Style, GraphicsUnit.Point);
                drawFont = compactFont;
            }

            TextRenderer.DrawText(e.Graphics, label, drawFont, bounds, foreground,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            compactFont?.Dispose();
        }

        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            base.OnSelectedIndexChanged(e);
            Invalidate();
        }
    }
}

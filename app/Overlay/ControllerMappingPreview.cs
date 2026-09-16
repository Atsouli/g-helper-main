using GHelper.Ally;
using GHelper.Helpers;
using GHelper.Mode;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace GHelper.Overlay
{
    internal sealed class ControllerMappingPreview : OSDNativeForm
    {
        private readonly System.Windows.Forms.Timer fallbackTimer = new() { Interval = 12000 };
        private IReadOnlyDictionary<string, (string Primary, string Secondary)> mappings =
            new Dictionary<string, (string, string)>();
        private string modeName = "Gamepad Mode";

        public ControllerMappingPreview()
        {
            Alpha = 248;
            fallbackTimer.Tick += (_, _) => HidePreview();
        }

        public void ShowPreview()
        {
            mappings = AllyControl.GetActiveMappings().ToDictionary(
                mapping => mapping.Input,
                mapping => (mapping.Primary, mapping.Secondary));
            modeName = AllyControl.ActiveMappingModeName;

            Screen screen = Screen.PrimaryScreen ?? Screen.FromPoint(Cursor.Position);
            int width = Math.Min(1040, screen.WorkingArea.Width - 32);
            int height = Math.Min(680, screen.WorkingArea.Height - 32);
            Size = new Size(Math.Max(720, width), Math.Max(500, height));
            Location = new Point(
                screen.WorkingArea.Left + (screen.WorkingArea.Width - Width) / 2,
                screen.WorkingArea.Top + (screen.WorkingArea.Height - Height) / 2);

            Show();
            Invalidate();
            fallbackTimer.Stop();
            fallbackTimer.Start();
            Logger.WriteLine("Controller mapping preview: show");
        }

        public void HidePreview()
        {
            fallbackTimer.Stop();
            Hide();
            Logger.WriteLine("Controller mapping preview: hide");
        }

        protected override void PerformPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Rectangle outer = new(1, 1, Width - 3, Height - 3);
            using GraphicsPath outerPath = Drawing.RoundedRect(outer, 22);
            using LinearGradientBrush background = new(outer,
                Color.FromArgb(246, 18, 25, 38), Color.FromArgb(246, 8, 13, 23),
                LinearGradientMode.Vertical);
            g.FillPath(background, outerPath);
            using Pen border = new(Color.FromArgb(220, 54, 190, 255), 2f);
            g.DrawPath(border, outerPath);

            using Font titleFont = new("Segoe UI Semibold", 18f, FontStyle.Bold);
            using Font modeFont = new("Segoe UI Semibold", 10f);
            using Brush primaryText = new SolidBrush(Color.FromArgb(245, 248, 252));
            using Brush accentText = new SolidBrush(Color.FromArgb(74, 203, 255));
            g.DrawString("CURRENT CONTROLS", titleFont, primaryText, 28, 20);
            SizeF modeSize = g.MeasureString(modeName, modeFont);
            g.DrawString(modeName, modeFont, accentText, Width - modeSize.Width - 30, 28);

            float scale = Math.Min((Width - 32f) / 1000f, (Height - 82f) / 540f);
            float offsetX = (Width - 1000f * scale) / 2f;
            float offsetY = 70f + (Height - 70f - 540f * scale) / 2f;
            GraphicsState state = g.Save();
            g.TranslateTransform(offsetX, offsetY);
            g.ScaleTransform(scale, scale);
            DrawController(g);
            g.Restore(state);

            using Font hintFont = new("Segoe UI", 8.5f);
            using Brush hintBrush = new SolidBrush(Color.FromArgb(145, 165, 183));
            const string hint = "Release the ROG button to close";
            SizeF hintSize = g.MeasureString(hint, hintFont);
            g.DrawString(hint, hintFont, hintBrush, (Width - hintSize.Width) / 2f, Height - 24);
        }

        private void DrawController(Graphics g)
        {
            Rectangle destRect = new Rectangle(0, 50, 1000, 412);
            
            ColorMatrix matrix = new ColorMatrix(new float[][]
            {
                new float[] { 1, 0, 0, 0, 0 },
                new float[] { 0, 1, 0, 0, 0 },
                new float[] { 0, 0, 1, 0, 0 },
                new float[] { 0, 0, 0, 1, 0 },
                new float[] { 125f/255f, 150f/255f, 170f/255f, 0, 1 } // Light steel blue tint
            });

            using ImageAttributes attributes = new ImageAttributes();
            attributes.SetColorMatrix(matrix);

            using Bitmap allyImg = Properties.Resources.ally;
            g.DrawImage(allyImg, destRect, 0, 0, allyImg.Width, allyImg.Height, GraphicsUnit.Pixel, attributes);

            Rectangle screen = new(352, 120, 296, 236);
            using GraphicsPath screenPath = Drawing.RoundedRect(screen, 13);
            using LinearGradientBrush screenBrush = new(screen,
                Color.FromArgb(220, 12, 31, 47), Color.FromArgb(240, 7, 15, 27),
                LinearGradientMode.ForwardDiagonal);
            using Pen screenBorder = new(Color.FromArgb(150, 55, 199, 255), 2f);
            g.FillPath(screenBrush, screenPath);
            g.DrawPath(screenBorder, screenPath);
            DrawShortcutPanel(g, screen);

            (string Input, PointF Anchor)[] left =
            {
                ("LT", new(125, 95)), ("LB", new(235, 115)), ("L3", new(135, 235)),
                ("D-Pad Up", new(190, 315)), ("D-Pad Left", new(155, 350)),
                ("D-Pad Right", new(225, 350)), ("D-Pad Down", new(190, 385)),
            };
            (string Input, PointF Anchor)[] right =
            {
                ("RT", new(875, 95)), ("RB", new(765, 115)), ("R3", new(865, 350)),
                ("Y", new(810, 200)), ("X", new(770, 240)),
                ("B", new(850, 240)), ("A", new(810, 280)),
            };

            for (int index = 0; index < left.Length; index++)
                DrawCallout(g, left[index].Input, left[index].Anchor,
                    new RectangleF(8, 20 + index * 55, 180, 43));
            for (int index = 0; index < right.Length; index++)
                DrawCallout(g, right[index].Input, right[index].Anchor,
                    new RectangleF(812, 20 + index * 55, 180, 43));

            DrawCallout(g, "View", new PointF(260, 160), new RectangleF(220, 477, 130, 43));
            DrawCallout(g, "M1", new PointF(330, 205), new RectangleF(360, 477, 130, 43));
            DrawCallout(g, "M2", new PointF(670, 205), new RectangleF(510, 477, 130, 43));
            DrawCallout(g, "Menu", new PointF(740, 160), new RectangleF(650, 477, 130, 43));
        }

        private void DrawShortcutPanel(Graphics g, Rectangle screen)
        {
            using Font titleFont = new("Segoe UI Semibold", 9f, FontStyle.Bold);
            using Font shortcutFont = new("Segoe UI", 7.4f, FontStyle.Regular);
            using Brush titleBrush = new SolidBrush(Color.FromArgb(78, 205, 255));
            using Brush shortcutBrush = new SolidBrush(Color.FromArgb(235, 243, 249));
            using Brush keyBrush = new SolidBrush(Color.FromArgb(255, 214, 92));
            using StringFormat ellipsis = new() { Trimming = StringTrimming.EllipsisCharacter };

            float x = screen.Left + 13;
            float y = screen.Top + 12;
            g.DrawString("CUSTOM SHORTCUTS", titleFont, titleBrush, x, y);
            y += 27;

            string lbAction = ShortcutActionName("m4_lb", "Brightness -10%");
            string rbAction = ShortcutActionName("m4_rb", "Brightness +10%");
            string rsXAction = ShortcutActionName("m12_rs_x", "Brightness");
            string rsYAction = ShortcutActionName("m12_rs_y", "Scroll");
            string rogDPadXAction = ShortcutActionName("m4_dpad_x", "CPU max -/+ 100 MHz");
            string rogDPadYAction = ShortcutActionName("m4_dpad_y", "GPU clock +/- 100 MHz");

            List<(string Chord, string Action)> shortcuts = AllyControl.IsGamepadMode
                ?
                [
                    ("Hold ROG + D-Pad ↑/↓", rogDPadYAction),
                    ("Hold ROG + D-Pad ←/→", rogDPadXAction),
                    ("Hold ROG + LB", lbAction),
                    ("Hold ROG + RB", rbAction),
                    ("L2+R2 + D-Pad ←/→", "TDP -/+ 1 W (hold)"),
                    ("M1/M2 + Right stick", "Mouse cursor"),
                    ("M1/M2 + R2", "Left mouse click"),
                    ("M1/M2 + L2", "Right mouse click")
                ]
                :
                [
                    ("Hold ROG + D-Pad ↑/↓", rogDPadYAction),
                    ("Hold ROG + D-Pad ←/→", rogDPadXAction),
                    ("Hold ROG + LB", lbAction),
                    ("Hold ROG + RB", rbAction),
                    ("M1/M2 + Left stick", "↑Q  →E  ↓R  ←T"),
                    ("M1/M2 + R-stick ←/→", rsXAction),
                    ("M1/M2 + R-stick ↑/↓", rsYAction)
                ];

            string ccSecondaryAction = ShortcutActionName("cc_secondary");
            if (!string.IsNullOrWhiteSpace(ccSecondaryAction))
                shortcuts.Add(("M1/M2 + Command", ccSecondaryAction));

            string rogSecondaryAction = ShortcutActionName("m4_secondary");
            if (!string.IsNullOrWhiteSpace(rogSecondaryAction))
                shortcuts.Add(("M1/M2 + ROG", rogSecondaryAction));

            string rogDoubleAction = ShortcutActionName("m4_double");
            if (!string.IsNullOrWhiteSpace(rogDoubleAction))
                shortcuts.Add(("Double ROG", rogDoubleAction));

            string ccDoubleAction = ShortcutActionName("cc_double");
            if (string.IsNullOrWhiteSpace(ccDoubleAction) && ModeControl.IsAllyZ1Extreme())
                ccDoubleAction = "Automatic / Manual clocks";
            if (!string.IsNullOrWhiteSpace(ccDoubleAction))
                shortcuts.Add(("Double Command", ccDoubleAction));

            const float lineHeight = 20f;
            const float chordWidth = 145f;
            foreach ((string chord, string action) in shortcuts)
            {
                g.DrawString(chord, shortcutFont, keyBrush,
                    new RectangleF(x, y, chordWidth, lineHeight), ellipsis);
                g.DrawString(action, shortcutFont, shortcutBrush,
                    new RectangleF(x + chordWidth, y, screen.Width - chordWidth - 26, lineHeight), ellipsis);
                y += lineHeight;
            }
        }

        private static string ShortcutActionName(string configKey, string defaultAction = "")
        {
            string action = AppConfig.GetString(configKey) ?? "";
            return action switch
            {
                "" => defaultAction,
                "fan_zero_toggle" => "Toggle 0 RPM fans",
                "fan_full_toggle" => "Toggle 100% fans",
                "fan_extreme_switch" => "Switch 0 / 100% fans",
                "ally_frequency_toggle" => "Automatic / Manual clocks",
                "ghelper" => "Open G-Helper",
                "controller" => "Controller mode",
                "overlay" => "Overlay",
                "screenshot" => "Screenshot",
                "performance" => "Performance mode",
                "brightness_up" => "Brightness up",
                "brightness_down" => "Brightness down",
                "volume_up" => "Volume up",
                "volume_down" => "Volume down",
                "custom" => AppConfig.GetString("custom_" + configKey, "Custom shortcut"),
                _ => action.Replace('_', ' ')
            };
        }

        private void DrawCallout(Graphics g, string input, PointF anchor, RectangleF box)
        {
            PointF lineEnd;
            if (anchor.X < box.Left) lineEnd = new(box.Left, box.Top + box.Height / 2f);
            else if (anchor.X > box.Right) lineEnd = new(box.Right, box.Top + box.Height / 2f);
            else if (anchor.Y < box.Top) lineEnd = new(box.Left + box.Width / 2f, box.Top);
            else lineEnd = new(box.Left + box.Width / 2f, box.Bottom);

            // Draw a dot at the anchor
            using Brush anchorBrush = new SolidBrush(Color.FromArgb(170, 69, 194, 241));
            g.FillEllipse(anchorBrush, anchor.X - 3, anchor.Y - 3, 6, 6);

            using Pen connector = new(Color.FromArgb(145, 69, 194, 241), 1.5f);
            g.DrawLine(connector, anchor, lineEnd);
            using GraphicsPath path = Drawing.RoundedRect(Rectangle.Round(box), 9);
            using Brush fill = new SolidBrush(Color.FromArgb(225, 25, 35, 50));
            using Pen outline = new(Color.FromArgb(125, 84, 153, 191), 1f);
            g.FillPath(fill, path);
            g.DrawPath(outline, path);

            using Font inputFont = new("Segoe UI Semibold", 8f, FontStyle.Bold);
            using Font actionFont = new("Segoe UI", 8.5f);
            using Brush inputBrush = new SolidBrush(Color.FromArgb(72, 204, 255));
            using Brush actionBrush = new SolidBrush(Color.FromArgb(242, 247, 252));
            using StringFormat format = new() { Trimming = StringTrimming.EllipsisCharacter };
            g.DrawString(input, inputFont, inputBrush, box.X + 9, box.Y + 4);
            g.DrawString(BindingText(input), actionFont, actionBrush,
                new RectangleF(box.X + 9, box.Y + 20, box.Width - 18, 17), format);
        }

        private string BindingText(string input)
        {
            if (!mappings.TryGetValue(input, out var mapping)) return "—";
            return string.IsNullOrWhiteSpace(mapping.Secondary)
                ? mapping.Primary
                : $"{mapping.Primary}  ·  M: {mapping.Secondary}";
        }



        public new void Dispose()
        {
            fallbackTimer.Dispose();
            base.Dispose();
        }
    }
}

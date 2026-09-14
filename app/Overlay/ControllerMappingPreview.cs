using GHelper.Ally;
using GHelper.Helpers;
using GHelper.Mode;
using System.Drawing.Drawing2D;

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
            Rectangle shell = new(205, 85, 590, 365);
            using GraphicsPath shellPath = Drawing.RoundedRect(shell, 54);
            using LinearGradientBrush shellBrush = new(shell,
                Color.FromArgb(235, 52, 62, 78), Color.FromArgb(245, 25, 33, 48),
                LinearGradientMode.Vertical);
            using Pen shellBorder = new(Color.FromArgb(190, 121, 151, 178), 2f);
            g.FillPath(shellBrush, shellPath);
            g.DrawPath(shellBorder, shellPath);

            Rectangle screen = new(352, 120, 296, 236);
            using GraphicsPath screenPath = Drawing.RoundedRect(screen, 13);
            using LinearGradientBrush screenBrush = new(screen,
                Color.FromArgb(245, 12, 31, 47), Color.FromArgb(245, 7, 15, 27),
                LinearGradientMode.ForwardDiagonal);
            using Pen screenBorder = new(Color.FromArgb(150, 55, 199, 255), 2f);
            g.FillPath(screenBrush, screenPath);
            g.DrawPath(screenBorder, screenPath);
            DrawShortcutPanel(g, screen);

            DrawStick(g, new PointF(282, 190), "L3");
            DrawDPad(g, new PointF(288, 310));
            DrawFaceButtons(g, new PointF(716, 190));
            DrawStick(g, new PointF(708, 310), "R3");
            DrawSmallButton(g, new PointF(405, 383), "VIEW");
            DrawSmallButton(g, new PointF(595, 383), "MENU");
            DrawRearButton(g, new PointF(456, 414), "M1");
            DrawRearButton(g, new PointF(544, 414), "M2");
            DrawShoulder(g, new RectangleF(235, 72, 92, 22), "LT");
            DrawShoulder(g, new RectangleF(282, 96, 76, 20), "LB");
            DrawShoulder(g, new RectangleF(673, 72, 92, 22), "RT");
            DrawShoulder(g, new RectangleF(642, 96, 76, 20), "RB");

            (string Input, PointF Anchor)[] left =
            {
                ("LT", new(280, 82)), ("LB", new(320, 106)), ("L3", new(282, 190)),
                ("D-Pad Up", new(288, 282)), ("D-Pad Left", new(260, 310)),
                ("D-Pad Right", new(316, 310)), ("D-Pad Down", new(288, 338)),
            };
            (string Input, PointF Anchor)[] right =
            {
                ("RT", new(720, 82)), ("RB", new(680, 106)), ("R3", new(708, 310)),
                ("Y", new(716, 162)), ("X", new(688, 190)),
                ("B", new(744, 190)), ("A", new(716, 218)),
            };

            for (int index = 0; index < left.Length; index++)
                DrawCallout(g, left[index].Input, left[index].Anchor,
                    new RectangleF(8, 58 + index * 55, 180, 43), true);
            for (int index = 0; index < right.Length; index++)
                DrawCallout(g, right[index].Input, right[index].Anchor,
                    new RectangleF(812, 58 + index * 55, 180, 43), false);

            DrawCallout(g, "View", new PointF(405, 383), new RectangleF(220, 477, 130, 43), true);
            DrawCallout(g, "M1", new PointF(456, 414), new RectangleF(360, 477, 130, 43), true);
            DrawCallout(g, "M2", new PointF(544, 414), new RectangleF(510, 477, 130, 43), false);
            DrawCallout(g, "Menu", new PointF(595, 383), new RectangleF(650, 477, 130, 43), false);
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

            List<(string Chord, string Action)> shortcuts = AllyControl.IsGamepadMode
                ?
                [
                    ("Hold ROG + D-Pad ↑/↓", "GPU clock +/- 100 MHz"),
                    ("Hold ROG + D-Pad ←/→", "CPU max -/+ 100 MHz"),
                    ("L2+R2 + D-Pad ←/→", "TDP -/+ 1 W (hold)"),
                    ("M1/M2 + Right stick", "Mouse cursor"),
                    ("M1/M2 + R2", "Left mouse click"),
                    ("M1/M2 + L2", "Right mouse click")
                ]
                :
                [
                    ("Hold ROG + D-Pad ↑/↓", "GPU clock +/- 100 MHz"),
                    ("Hold ROG + D-Pad ←/→", "CPU max -/+ 100 MHz"),
                    ("M1/M2 + Left stick", "↑Q  →E  ↓R  ←T"),
                    ("M1/M2 + R-stick ←/→", "Brightness"),
                    ("M1/M2 + R-stick ↑/↓", "Scroll")
                ];

            string ccSecondaryAction = ShortcutActionName("cc_secondary");
            if (!string.IsNullOrWhiteSpace(ccSecondaryAction))
                shortcuts.Add(("M1/M2 + Command", ccSecondaryAction));

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

        private static string ShortcutActionName(string configKey)
        {
            string action = AppConfig.GetString(configKey) ?? "";
            return action switch
            {
                "" => "",
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

        private void DrawCallout(Graphics g, string input, PointF anchor, RectangleF box, bool leftSide)
        {
            PointF lineEnd = new(leftSide ? box.Right : box.Left, box.Top + box.Height / 2f);
            using Pen connector = new(Color.FromArgb(115, 69, 194, 241), 1.25f);
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

        private static void DrawStick(Graphics g, PointF center, string label)
        {
            using Brush outer = new SolidBrush(Color.FromArgb(245, 14, 20, 31));
            using Brush inner = new SolidBrush(Color.FromArgb(245, 47, 59, 74));
            using Pen ring = new(Color.FromArgb(170, 83, 184, 224), 2f);
            g.FillEllipse(outer, center.X - 33, center.Y - 33, 66, 66);
            g.DrawEllipse(ring, center.X - 33, center.Y - 33, 66, 66);
            g.FillEllipse(inner, center.X - 24, center.Y - 24, 48, 48);
            DrawCenteredText(g, label, center, 8f);
        }

        private static void DrawDPad(Graphics g, PointF center)
        {
            using Brush fill = new SolidBrush(Color.FromArgb(245, 18, 25, 37));
            using Pen outline = new(Color.FromArgb(180, 108, 130, 151), 1.5f);
            Rectangle vertical = new((int)center.X - 12, (int)center.Y - 39, 24, 78);
            Rectangle horizontal = new((int)center.X - 39, (int)center.Y - 12, 78, 24);
            g.FillRectangle(fill, vertical);
            g.FillRectangle(fill, horizontal);
            g.DrawRectangle(outline, vertical);
            g.DrawRectangle(outline, horizontal);
        }

        private static void DrawFaceButtons(Graphics g, PointF center)
        {
            DrawRoundButton(g, new(center.X, center.Y + 28), "A", Color.FromArgb(91, 220, 148));
            DrawRoundButton(g, new(center.X + 28, center.Y), "B", Color.FromArgb(255, 105, 113));
            DrawRoundButton(g, new(center.X - 28, center.Y), "X", Color.FromArgb(74, 203, 255));
            DrawRoundButton(g, new(center.X, center.Y - 28), "Y", Color.FromArgb(246, 210, 82));
        }

        private static void DrawRoundButton(Graphics g, PointF center, string text, Color color)
        {
            using Brush fill = new SolidBrush(Color.FromArgb(245, 20, 27, 39));
            using Pen outline = new(color, 2f);
            g.FillEllipse(fill, center.X - 13, center.Y - 13, 26, 26);
            g.DrawEllipse(outline, center.X - 13, center.Y - 13, 26, 26);
            DrawCenteredText(g, text, center, 8f, color);
        }

        private static void DrawSmallButton(Graphics g, PointF center, string text)
        {
            Rectangle rect = new((int)center.X - 25, (int)center.Y - 10, 50, 20);
            using GraphicsPath path = Drawing.RoundedRect(rect, 7);
            using Brush fill = new SolidBrush(Color.FromArgb(230, 18, 25, 37));
            using Pen outline = new(Color.FromArgb(150, 87, 153, 184), 1f);
            g.FillPath(fill, path);
            g.DrawPath(outline, path);
            DrawCenteredText(g, text, center, 6.5f);
        }

        private static void DrawRearButton(Graphics g, PointF center, string text)
        {
            Rectangle rect = new((int)center.X - 24, (int)center.Y - 10, 48, 20);
            using GraphicsPath path = Drawing.RoundedRect(rect, 8);
            using Brush fill = new SolidBrush(Color.FromArgb(220, 30, 88, 116));
            g.FillPath(fill, path);
            DrawCenteredText(g, text, center, 7f);
        }

        private static void DrawShoulder(Graphics g, RectangleF bounds, string text)
        {
            using GraphicsPath path = Drawing.RoundedRect(Rectangle.Round(bounds), 8);
            using Brush fill = new SolidBrush(Color.FromArgb(245, 28, 38, 52));
            using Pen outline = new(Color.FromArgb(175, 91, 175, 211), 1.5f);
            g.FillPath(fill, path);
            g.DrawPath(outline, path);
            DrawCenteredText(g, text, new(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2), 7f);
        }

        private static void DrawCenteredText(Graphics g, string text, PointF center, float size, Color? color = null)
        {
            using Font font = new("Segoe UI Semibold", size, FontStyle.Bold);
            using Brush brush = new SolidBrush(color ?? Color.FromArgb(238, 245, 251));
            using StringFormat format = new() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(text, font, brush, center, format);
        }

        public new void Dispose()
        {
            fallbackTimer.Dispose();
            base.Dispose();
        }
    }
}

using GHelper.Ally;
using GHelper.Helpers;
using GHelper.Mode;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace GHelper.Overlay
{
    public enum PreviewMode { ROG, M2 }

    internal sealed class ControllerMappingPreview : OSDNativeForm
    {
        private readonly System.Windows.Forms.Timer fallbackTimer = new() { Interval = 12000 };
        private IReadOnlyDictionary<string, (string Primary, string Secondary)> mappings =
            new Dictionary<string, (string, string)>();
        private string modeName = "Gamepad Mode";
        private PreviewMode _activeMode = PreviewMode.ROG;

        public bool IsM2Mode => _activeMode == PreviewMode.M2;

        // Per-mode accent colours
        private static readonly Color AccentROG = Color.FromArgb(74, 203, 255);   // cyan
        private static readonly Color AccentM2  = Color.FromArgb(255, 178, 56);   // amber
        private static readonly Color BorderROG = Color.FromArgb(220, 54, 190, 255);
        private static readonly Color BorderM2  = Color.FromArgb(220, 255, 150, 30);

        public ControllerMappingPreview()
        {
            Alpha = 248;
            fallbackTimer.Tick += (_, _) => HidePreview();
        }

        public void ShowPreview() => ShowPreview(PreviewMode.ROG);

        public void ShowPreview(PreviewMode mode)
        {
            _activeMode = mode;
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
            Logger.WriteLine($"Controller mapping preview: show ({mode})");
        }

        public void HidePreview()
        {
            fallbackTimer.Stop();
            Hide();
            Logger.WriteLine("Controller mapping preview: hide");
        }

        // ----- Painting -----

        protected override void PerformPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Color accent = _activeMode == PreviewMode.ROG ? AccentROG : AccentM2;
            Color borderColor = _activeMode == PreviewMode.ROG ? BorderROG : BorderM2;

            Rectangle outer = new(1, 1, Width - 3, Height - 3);
            using GraphicsPath outerPath = Drawing.RoundedRect(outer, 22);
            using LinearGradientBrush background = new(outer,
                Color.FromArgb(246, 18, 25, 38), Color.FromArgb(246, 8, 13, 23),
                LinearGradientMode.Vertical);
            g.FillPath(background, outerPath);
            using Pen border = new(borderColor, 2f);
            g.DrawPath(border, outerPath);

            string title = _activeMode == PreviewMode.ROG
                ? "CONTROLS  —  ROG LAYER"
                : "CONTROLS  —  M1 / M2 LAYER";

            using Font titleFont = new("Segoe UI Semibold", 18f, FontStyle.Bold);
            using Font modeFont = new("Segoe UI Semibold", 10f);
            using Brush primaryText = new SolidBrush(Color.FromArgb(245, 248, 252));
            using Brush accentText = new SolidBrush(accent);
            g.DrawString(title, titleFont, primaryText, 28, 20);
            SizeF modeSize = g.MeasureString(modeName, modeFont);
            g.DrawString(modeName, modeFont, accentText, Width - modeSize.Width - 30, 28);

            float scale = Math.Min((Width - 32f) / 1000f, (Height - 82f) / 540f);
            float offsetX = (Width - 1000f * scale) / 2f;
            float offsetY = 70f + (Height - 70f - 540f * scale) / 2f;
            GraphicsState state = g.Save();
            g.TranslateTransform(offsetX, offsetY);
            g.ScaleTransform(scale, scale);

            if (_activeMode == PreviewMode.ROG)
                DrawControllerROG(g);
            else
                DrawControllerM2(g);

            g.Restore(state);

            string hint = _activeMode == PreviewMode.ROG
                ? "Release the ROG button to close"
                : "Release to close";
            using Font hintFont = new("Segoe UI", 8.5f);
            using Brush hintBrush = new SolidBrush(Color.FromArgb(145, 165, 183));
            SizeF hintSize = g.MeasureString(hint, hintFont);
            g.DrawString(hint, hintFont, hintBrush, (Width - hintSize.Width) / 2f, Height - 24);
        }

        // ---------- ROG Layer drawing ----------

        private void DrawControllerROG(Graphics g)
        {
            DrawAllyImage(g, AccentROG);

            Rectangle screen = new(352, 120, 296, 236);
            DrawScreenPanel(g, screen);
            DrawRogShortcutPanel(g, screen);

            DrawControllerCallouts(g);
        }

        // ---------- M2 Layer drawing ----------

        private void DrawControllerM2(Graphics g)
        {
            DrawAllyImage(g, AccentM2);

            Rectangle screen = new(352, 120, 296, 236);
            DrawScreenPanel(g, screen);
            DrawM2ShortcutPanel(g, screen);

            DrawControllerCallouts(g);
        }

        private void DrawControllerCallouts(Graphics g)
        {
            Color triggers = Color.FromArgb(255, 87, 87);   // Red
            Color sticks = Color.FromArgb(255, 204, 0);     // Yellow
            Color dpad = Color.FromArgb(0, 210, 255);       // Cyan
            Color face = Color.FromArgb(0, 225, 125);       // Green
            Color misc = Color.FromArgb(180, 150, 255);     // Purple

            // --- Left side ---
            // LT and LB on same line
            DrawCallout(g, "LT", new(125, 95), new RectangleF(5, 20, 170, 43), triggers);
            DrawCallout(g, "LB", new(235, 115), new RectangleF(180, 20, 170, 43), triggers);

            // L3
            DrawCallout(g, "L3", new(135, 235), new RectangleF(92, 75, 170, 43), sticks);

            // D-Pad cross layout
            DrawCallout(g, "D-Pad Up", new(190, 315), new RectangleF(92, 130, 170, 43), dpad);
            DrawCallout(g, "D-Pad Left", new(155, 350), new RectangleF(5, 185, 170, 43), dpad);
            DrawCallout(g, "D-Pad Right", new(225, 350), new RectangleF(180, 185, 170, 43), dpad);
            DrawCallout(g, "D-Pad Down", new(190, 385), new RectangleF(92, 240, 170, 43), dpad);

            // --- Right side ---
            // RT and RB on same line
            DrawCallout(g, "RB", new(765, 115), new RectangleF(650, 20, 170, 43), triggers);
            DrawCallout(g, "RT", new(875, 95), new RectangleF(825, 20, 170, 43), triggers);

            // Face buttons diamond layout
            DrawCallout(g, "Y", new(810, 200), new RectangleF(737, 75, 170, 43), face);
            DrawCallout(g, "X", new(770, 240), new RectangleF(650, 130, 170, 43), face);
            DrawCallout(g, "B", new(850, 240), new RectangleF(825, 130, 170, 43), face);
            DrawCallout(g, "A", new(810, 280), new RectangleF(737, 185, 170, 43), face);

            // R3 at last
            DrawCallout(g, "R3", new(865, 350), new RectangleF(737, 240, 170, 43), sticks);

            // --- Top center buttons ---
            DrawCallout(g, "View", new PointF(260, 160), new RectangleF(352, 70, 130, 43), misc);
            DrawCallout(g, "Menu", new PointF(740, 160), new RectangleF(518, 70, 130, 43), misc);

            // --- Bottom center buttons ---
            DrawCallout(g, "M1", new PointF(330, 205), new RectangleF(360, 477, 130, 43), misc);
            DrawCallout(g, "M2", new PointF(670, 205), new RectangleF(510, 477, 130, 43), misc);
        }

        // ---------- Shared drawing helpers ----------

        private static void DrawAllyImage(Graphics g, Color accent)
        {
            Rectangle destRect = new Rectangle(0, 50, 1000, 412);

            // Tint based on mode accent
            float r = accent.R / 510f;
            float gr = accent.G / 510f;
            float b = accent.B / 510f;

            ColorMatrix matrix = new ColorMatrix(new float[][]
            {
                new float[] { 1, 0, 0, 0, 0 },
                new float[] { 0, 1, 0, 0, 0 },
                new float[] { 0, 0, 1, 0, 0 },
                new float[] { 0, 0, 0, 1, 0 },
                new float[] { r, gr, b, 0, 1 }
            });

            using ImageAttributes attributes = new ImageAttributes();
            attributes.SetColorMatrix(matrix);

            using Bitmap allyImg = Properties.Resources.ally;
            g.DrawImage(allyImg, destRect, 0, 0, allyImg.Width, allyImg.Height, GraphicsUnit.Pixel, attributes);
        }

        private static void DrawScreenPanel(Graphics g, Rectangle screen)
        {
            using GraphicsPath screenPath = Drawing.RoundedRect(screen, 13);
            using LinearGradientBrush screenBrush = new(screen,
                Color.FromArgb(220, 12, 31, 47), Color.FromArgb(240, 7, 15, 27),
                LinearGradientMode.ForwardDiagonal);
            using Pen screenBorder = new(Color.FromArgb(150, 55, 199, 255), 2f);
            g.FillPath(screenBrush, screenPath);
            g.DrawPath(screenBorder, screenPath);
        }

        // ---------- ROG shortcut panel (inside screen) ----------

        private void DrawRogShortcutPanel(Graphics g, Rectangle screen)
        {
            using Font titleFont = new("Segoe UI Semibold", 9f, FontStyle.Bold);
            using Font shortcutFont = new("Segoe UI", 7.4f, FontStyle.Regular);
            using Brush titleBrush = new SolidBrush(Color.FromArgb(78, 205, 255));
            using Brush shortcutBrush = new SolidBrush(Color.FromArgb(235, 243, 249));
            using Brush keyBrush = new SolidBrush(Color.FromArgb(255, 214, 92));
            using StringFormat ellipsis = new() { Trimming = StringTrimming.EllipsisCharacter };

            float x = screen.Left + 13;
            float y = screen.Top + 12;
            g.DrawString("HOLD ROG SHORTCUTS", titleFont, titleBrush, x, y);
            y += 27;

            string lbAction = ShortcutActionName("m4_lb", "Brightness -10%");
            string rbAction = ShortcutActionName("m4_rb", "Brightness +10%");
            string rogDPadXAction = ShortcutActionName("m4_dpad_x", "CPU max -/+ 100 MHz");
            string rogDPadYAction = ShortcutActionName("m4_dpad_y", "GPU clock +/- 100 MHz");

            List<(string Chord, string Action)> shortcuts =
            [
                ("ROG + D-Pad ↑/↓", rogDPadYAction),
                ("ROG + D-Pad ←/→", rogDPadXAction),
                ("ROG + LB", lbAction),
                ("ROG + RB", rbAction),
                ("L2+R2 + D-Pad ←/→", "TDP -/+ 1 W (hold)"),
            ];

            // Show per-button ROG layer actions from the new "rog_gamepad_/rog_desktop_" keys
            bool desktop = !AllyControl.IsGamepadMode;
            string[] rogButtons = { "a", "b", "x", "y", "du", "dd", "dl", "dr", "lt", "rt", "lb", "rb", "ls", "rs", "vb", "mb" };
            string[] rogNames = { "A", "B", "X", "Y", "D-Up", "D-Down", "D-Left", "D-Right", "LT", "RT", "LB", "RB", "L3", "R3", "View", "Menu" };
            for (int i = 0; i < rogButtons.Length; i++)
            {
                string key = (desktop ? "rog_desktop_" : "rog_gamepad_") + rogButtons[i];
                string action = ShortcutActionName(key);
                if (!string.IsNullOrWhiteSpace(action))
                    shortcuts.Add(($"ROG + {rogNames[i]}", action));
            }

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
                if (y + lineHeight > screen.Bottom - 8) break;
                g.DrawString(chord, shortcutFont, keyBrush,
                    new RectangleF(x, y, chordWidth, lineHeight), ellipsis);
                g.DrawString(action, shortcutFont, shortcutBrush,
                    new RectangleF(x + chordWidth, y, screen.Width - chordWidth - 26, lineHeight), ellipsis);
                y += lineHeight;
            }
        }

        // ---------- M2 shortcut panel (inside screen) ----------

        private void DrawM2ShortcutPanel(Graphics g, Rectangle screen)
        {
            using Font titleFont = new("Segoe UI Semibold", 9f, FontStyle.Bold);
            using Font shortcutFont = new("Segoe UI", 7.4f, FontStyle.Regular);
            using Brush titleBrush = new SolidBrush(AccentM2);
            using Brush shortcutBrush = new SolidBrush(Color.FromArgb(235, 243, 249));
            using Brush keyBrush = new SolidBrush(Color.FromArgb(255, 214, 92));
            using StringFormat ellipsis = new() { Trimming = StringTrimming.EllipsisCharacter };

            float x = screen.Left + 13;
            float y = screen.Top + 12;
            g.DrawString("M1 / M2 SHORTCUTS", titleFont, titleBrush, x, y);
            y += 27;

            string rsXAction = ShortcutActionName("m12_rs_x", "Brightness");
            string rsYAction = ShortcutActionName("m12_rs_y", "Scroll");

            List<(string Chord, string Action)> shortcuts = AllyControl.IsGamepadMode
                ?
                [
                    ("M1/M2 + R-stick", "Mouse cursor"),
                    ("M1/M2 + R2", "Left mouse click"),
                    ("M1/M2 + L2", "Right mouse click"),
                ]
                :
                [
                    ("M1/M2 + L-stick", "↑Q  →E  ↓R  ←T"),
                    ("M1/M2 + R-stick ←/→", rsXAction),
                    ("M1/M2 + R-stick ↑/↓", rsYAction),
                    ("M1/M2 + R2", "Left mouse click"),
                    ("M1/M2 + L2", "Right mouse click"),
                ];

            string ccSecondaryAction = ShortcutActionName("cc_secondary");
            if (!string.IsNullOrWhiteSpace(ccSecondaryAction))
                shortcuts.Add(("M1/M2 + Command", ccSecondaryAction));

            string rogSecondaryAction = ShortcutActionName("m4_secondary");
            if (!string.IsNullOrWhiteSpace(rogSecondaryAction))
                shortcuts.Add(("M1/M2 + ROG", rogSecondaryAction));

            const float lineHeight = 20f;
            const float chordWidth = 145f;
            foreach ((string chord, string action) in shortcuts)
            {
                if (y + lineHeight > screen.Bottom - 8) break;
                g.DrawString(chord, shortcutFont, keyBrush,
                    new RectangleF(x, y, chordWidth, lineHeight), ellipsis);
                g.DrawString(action, shortcutFont, shortcutBrush,
                    new RectangleF(x + chordWidth, y, screen.Width - chordWidth - 26, lineHeight), ellipsis);
                y += lineHeight;
            }
        }

        // ---------- Config helpers ----------

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
                "rtss_osd" => "RTSS OSD",
                "custom" => AppConfig.GetString("custom_" + configKey, "Custom shortcut"),
                _ => action.Replace('_', ' ')
            };
        }

        // ---------- Callout drawing ----------

        private void DrawCallout(Graphics g, string input, PointF anchor, RectangleF box, Color accent)
        {
            PointF lineEnd = ConnectorEnd(anchor, box);
            DrawConnector(g, anchor, lineEnd, accent);

            using GraphicsPath path = Drawing.RoundedRect(Rectangle.Round(box), 9);
            using Brush fill = new SolidBrush(Color.FromArgb(225, 25, 35, 50));
            using Pen outline = new(Color.FromArgb(125, accent.R / 2, accent.G / 2, accent.B / 2 + 30), 1f);
            g.FillPath(fill, path);
            g.DrawPath(outline, path);

            using Font inputFont = new("Segoe UI Semibold", 8f, FontStyle.Bold);
            using Font actionFont = new("Segoe UI", 8.5f);
            using Brush inputBrush = new SolidBrush(accent);
            using Brush actionBrush = new SolidBrush(Color.FromArgb(242, 247, 252));
            using StringFormat format = new() { Trimming = StringTrimming.EllipsisCharacter };

            // Line 1: input name  |  Line 2: Primary action + ROG layer action
            g.DrawString(input, inputFont, inputBrush, box.X + 9, box.Y + 4);
            string text = BindingText(input);

            // If ROG mode, append the third-layer action
            if (_activeMode == PreviewMode.ROG)
            {
                string rogAction = RogLayerText(input);
                if (!string.IsNullOrWhiteSpace(rogAction))
                    text += $"  ▸ ROG: {rogAction}";
            }

            g.DrawString(text, actionFont, actionBrush,
                new RectangleF(box.X + 9, box.Y + 20, box.Width - 18, 17), format);
        }

        private static void DrawChordCallout(Graphics g, string label, string action, PointF anchor, RectangleF box, Color accent)
        {
            PointF lineEnd = ConnectorEnd(anchor, box);
            DrawConnector(g, anchor, lineEnd, accent);

            using GraphicsPath path = Drawing.RoundedRect(Rectangle.Round(box), 9);
            using Brush fill = new SolidBrush(Color.FromArgb(225, 25, 35, 50));
            using Pen outline = new(Color.FromArgb(125, accent.R / 2, accent.G / 2, accent.B / 2 + 30), 1f);
            g.FillPath(fill, path);
            g.DrawPath(outline, path);

            using Font labelFont = new("Segoe UI Semibold", 8f, FontStyle.Bold);
            using Font actionFont = new("Segoe UI", 8.5f);
            using Brush labelBrush = new SolidBrush(accent);
            using Brush actionBrush = new SolidBrush(Color.FromArgb(242, 247, 252));
            using StringFormat format = new() { Trimming = StringTrimming.EllipsisCharacter };
            g.DrawString(label, labelFont, labelBrush, box.X + 9, box.Y + 4);
            g.DrawString(action, actionFont, actionBrush,
                new RectangleF(box.X + 9, box.Y + 20, box.Width - 18, 17), format);
        }

        private static PointF ConnectorEnd(PointF anchor, RectangleF box)
        {
            if (anchor.X < box.Left) return new(box.Left, box.Top + box.Height / 2f);
            if (anchor.X > box.Right) return new(box.Right, box.Top + box.Height / 2f);
            if (anchor.Y < box.Top) return new(box.Left + box.Width / 2f, box.Top);
            return new(box.Left + box.Width / 2f, box.Bottom);
        }

        private static void DrawConnector(Graphics g, PointF anchor, PointF lineEnd, Color accent)
        {
            // Intentionally left blank to remove connector lines and dots
        }

        // ---------- Binding text resolvers ----------

        private string BindingText(string input)
        {
            if (!mappings.TryGetValue(input, out var mapping)) return "—";
            return string.IsNullOrWhiteSpace(mapping.Secondary)
                ? mapping.Primary
                : $"{mapping.Primary}  ·  M: {mapping.Secondary}";
        }

        private static string BindingTextForKey(string key)
        {
            bool desktop = !AllyControl.IsGamepadMode;
            string bindKey = (desktop ? "bind_desktop_" : "bind_gamepad_") + key;
            string val = AppConfig.GetString(bindKey, AppConfig.GetString("bind_" + key, ""));
            return string.IsNullOrWhiteSpace(val) ? "—" : val;
        }

        /// <summary>
        /// Returns the user's configured "Hold ROG + button" action for the given input name.
        /// Maps input display names (e.g. "A", "D-Pad Up") to config zone keys (e.g. "a", "du").
        /// </summary>
        private static string RogLayerText(string input)
        {
            string? zoneKey = input switch
            {
                "A" => "a", "B" => "b", "X" => "x", "Y" => "y",
                "D-Pad Up" => "du", "D-Pad Down" => "dd",
                "D-Pad Left" => "dl", "D-Pad Right" => "dr",
                "LT" => "lt", "RT" => "rt", "LB" => "lb", "RB" => "rb",
                "L3" => "ls", "R3" => "rs", "View" => "vb", "Menu" => "mb",
                _ => null
            };
            if (zoneKey == null) return "";
            bool desktop = !AllyControl.IsGamepadMode;
            string configKey = (desktop ? "rog_desktop_" : "rog_gamepad_") + zoneKey;
            return ShortcutActionName(configKey);
        }

        public new void Dispose()
        {
            fallbackTimer.Dispose();
            base.Dispose();
        }
    }
}


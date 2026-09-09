using GHelper.UI;

namespace GHelper.Overlay
{
    internal sealed class RtssOverlaySettingsDialog : RForm
    {
        private readonly Dictionary<string, CheckBox> options = new();

        public RtssOverlaySettingsDialog()
        {
            Text = "Customize RTSS OSD";
            ClientSize = new Size(620, 650);
            AutoScaleMode = AutoScaleMode.Dpi;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Padding = new Padding(16);

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

            var heading = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Choose metrics shown inside the game",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var cards = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            cards.RowStyles.Add(new RowStyle(SizeType.Percent, 42F));
            cards.RowStyles.Add(new RowStyle(SizeType.Percent, 58F));
            cards.Controls.Add(CreateGroup("CPU", new[]
            {
                ("rtss_show_cpu_temp", "Temperature"),
                ("rtss_show_cpu_usage", "Usage"),
                ("rtss_show_cpu_power", "Power"),
                ("rtss_show_cpu_clock", "Frequency")
            }), 0, 0);
            cards.Controls.Add(CreateGroup("GPU", new[]
            {
                ("rtss_show_gpu_temp", "Temperature"),
                ("rtss_show_gpu_usage", "Usage"),
                ("rtss_show_gpu_power", "Power"),
                ("rtss_show_gpu_clock", "Frequency"),
                ("rtss_show_vram", "VRAM memory")
            }), 1, 0);
            var general = CreateGroup("General", new[]
            {
                ("rtss_single_line", "Single-line layout"),
                ("rtss_show_fps", "Renderer API + FPS"),
                ("rtss_show_fps_graph", "FPS history graph"),
                ("rtss_show_fps_low", "FPS 1% low"),
                ("rtss_show_frametime", "Frame time"),
                ("rtss_show_ram", "RAM usage"),
                ("rtss_show_fans", "Fan speeds"),
                ("rtss_show_battery", "Battery percentage"),
                ("rtss_show_battery_time", "Battery time remaining"),
                ("rtss_show_battery_rate", "Battery charge / drain power")
            });
            cards.Controls.Add(general, 0, 1);
            cards.SetColumnSpan(general, 2);

            var cancel = CreateButton("Cancel", DialogResult.Cancel);
            var save = CreateButton("Save", DialogResult.None);
            save.Click += (_, _) => Save();
            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };
            buttons.Controls.Add(save);
            buttons.Controls.Add(cancel);

            root.Controls.Add(heading, 0, 0);
            root.Controls.Add(cards, 0, 1);
            root.Controls.Add(buttons, 0, 2);
            Controls.Add(root);
            AcceptButton = save;
            CancelButton = cancel;
            InitTheme(true);
            EnableGlassBackdrop();
        }

        private Control CreateGroup(string title, IEnumerable<(string Key, string Label)> entries)
        {
            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(12, 8, 12, 8)
            };
            flow.Controls.Add(new Label
            {
                Text = title,
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Margin = new Padding(3, 0, 3, 5)
            });
            foreach ((string key, string label) in entries)
            {
                var check = new CheckBox
                {
                    Text = label,
                    AutoSize = true,
                    Checked = AppConfig.IsNotFalse(key),
                    Margin = new Padding(3, 3, 3, 3)
                };
                options[key] = check;
                flow.Controls.Add(check);
            }

            var panel = new RGlassPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(5),
                CornerRadius = 10,
                BackColor = Color.Transparent
            };
            panel.Controls.Add(flow);
            return panel;
        }

        private static RButton CreateButton(string text, DialogResult result) => new()
        {
            Text = text,
            DialogResult = result,
            Size = new Size(100, 36),
            Margin = new Padding(4),
            Secondary = true
        };

        private void Save()
        {
            foreach ((string key, CheckBox check) in options)
                AppConfig.Set(key, check.Checked ? 1 : 0);
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}

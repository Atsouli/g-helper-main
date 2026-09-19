import re

with open('Settings.Designer.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Replace panelBattery configuration
panel_battery_new = """            // panelBattery
            panelBattery.AccessibleRole = AccessibleRole.Grouping;
            panelBattery.AutoSize = false;
            panelBattery.Controls.Add(progressBattery);
            panelBattery.Controls.Add(labelBattery);
            panelBattery.Controls.Add(panelBatteryTitle);
            panelBattery.Dock = DockStyle.Top;
            panelBattery.Location = new Point(11, 442);
            panelBattery.Margin = new Padding(0);
            panelBattery.Name = "panelBattery";
            panelBattery.Padding = new Padding(10, 11, 10, 11);
            panelBattery.Size = new Size(440, 220);
            panelBattery.TabIndex = 2;
            panelBattery.TabStop = true;
            // progressBattery
            progressBattery.Location = new Point(145, 60);
            progressBattery.Size = new Size(150, 150);
            progressBattery.Name = "progressBattery";
            progressBattery.Value = 90;
            progressBattery.Anchor = AnchorStyles.Top | AnchorStyles.Bottom;
            // labelBattery
            labelBattery.Dock = DockStyle.Bottom;
            labelBattery.AutoSize = false;
            labelBattery.Height = 30;
            labelBattery.TextAlign = ContentAlignment.MiddleCenter;
            labelBattery.Name = "labelBattery";"""

content = re.sub(
    r'// \r?\n\s*// panelBattery\r?\n\s*//.*?panelBattery\.TabStop = true;',
    panel_battery_new,
    content,
    flags=re.DOTALL
)

with open('Settings.Designer.cs', 'w', encoding='utf-8') as f:
    f.write(content)

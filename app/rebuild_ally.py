import re

with open('Settings.Designer.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Replace panelAlly configuration
panel_ally_new = """            // panelAlly
            panelAlly.AccessibleRole = AccessibleRole.Grouping;
            panelAlly.AutoSize = false;
            panelAlly.Controls.Add(labelVolume);
            panelAlly.Controls.Add(sliderVolume);
            panelAlly.Controls.Add(labelAlly);
            panelAlly.Controls.Add(buttonMouseEmulation);
            panelAlly.Controls.Add(labelMouseEmulation);
            panelAlly.Controls.Add(panelAllyTitle);
            panelAlly.Dock = DockStyle.Top;
            panelAlly.Location = new Point(11, 1254);
            panelAlly.Margin = new Padding(0);
            panelAlly.Name = "panelAlly";
            panelAlly.Padding = new Padding(10, 20, 10, 0);
            panelAlly.Size = new Size(440, 200);
            panelAlly.TabIndex = 5;
            panelAlly.TabStop = true;
            panelAlly.Visible = true;
            // labelVolume
            labelVolume.Text = "System Volume                        Volume: 80%";
            labelVolume.Dock = DockStyle.Top;
            labelVolume.Height = 25;
            // sliderVolume
            sliderVolume.Dock = DockStyle.Top;
            sliderVolume.Height = 40;
            // labelAlly (Controller: Connected)
            labelAlly.Text = "🎮    Controller: Connected";
            labelAlly.ForeColor = Color.MediumSeaGreen;
            labelAlly.Dock = DockStyle.Top;
            labelAlly.Height = 40;
            labelAlly.TextAlign = ContentAlignment.MiddleLeft;
            // labelMouseEmulation
            labelMouseEmulation.Text = "Mouse Emulation";
            labelMouseEmulation.Dock = DockStyle.Left;
            labelMouseEmulation.Width = 300;
            labelMouseEmulation.TextAlign = ContentAlignment.MiddleLeft;
            // buttonMouseEmulation
            buttonMouseEmulation.Text = "⚪";
            buttonMouseEmulation.Dock = DockStyle.Right;
            buttonMouseEmulation.Width = 60;
            """

content = re.sub(
    r'// \r?\n\s*// panelAlly\r?\n\s*//.*?panelAlly\.Visible = false;',
    panel_ally_new,
    content,
    flags=re.DOTALL
)

with open('Settings.Designer.cs', 'w', encoding='utf-8') as f:
    f.write(content)

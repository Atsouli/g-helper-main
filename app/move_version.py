import re

with open('Settings.Designer.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Add labelVersion to panelBattery
content = re.sub(
    r'panelBattery\.Controls\.Add\(labelBattery\);',
    r'panelBattery.Controls.Add(labelBattery);\n            panelBattery.Controls.Add(labelVersion);',
    content
)

# Remove labelVersion from panelVersion
content = re.sub(
    r'panelVersion\.Controls\.Add\(labelVersion\);\s*',
    '',
    content
)

# Change labelVersion docking and alignment
content = re.sub(
    r'labelVersion\.Dock = DockStyle\.Top;',
    r'labelVersion.Dock = DockStyle.Bottom;',
    content
)
content = re.sub(
    r'labelVersion\.TextAlign = ContentAlignment\.TopCenter;',
    r'labelVersion.TextAlign = ContentAlignment.BottomCenter;',
    content
)

with open('Settings.Designer.cs', 'w', encoding='utf-8') as f:
    f.write(content)

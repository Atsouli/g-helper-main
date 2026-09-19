import re

with open('Settings.Designer.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Move buttonAddCustom out of panelCustomButtonsTitle
content = re.sub(
    r'panelCustomButtonsTitle\.Controls\.Add\(buttonAddCustom\);\s*',
    '',
    content
)

# Add it to panelCustomButtons before tableButtons
content = re.sub(
    r'panelCustomButtons\.Controls\.Add\(tableButtons\);',
    r'panelCustomButtons.Controls.Add(tableButtons);\n            panelCustomButtons.Controls.Add(buttonAddCustom);',
    content
)

# Change Dock and Text
content = re.sub(
    r'buttonAddCustom\.Dock = DockStyle\.Right;',
    r'buttonAddCustom.Dock = DockStyle.Top;',
    content
)
content = re.sub(
    r'buttonAddCustom\.Text = "\+ Add";',
    r'buttonAddCustom.Text = "Add Shortcut";',
    content
)
content = re.sub(
    r'buttonAddCustom\.Size = new Size\([0-9]+, [0-9]+\);',
    r'buttonAddCustom.Size = new Size(400, 48);',
    content
)
content = re.sub(
    r'buttonAddCustom\.Margin = new Padding\([0-9]+, [0-9]+, [0-9]+, [0-9]+\);',
    r'buttonAddCustom.Margin = new Padding(0, 0, 0, 10);',
    content
)

with open('Settings.Designer.cs', 'w', encoding='utf-8') as f:
    f.write(content)

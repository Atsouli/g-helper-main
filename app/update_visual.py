import re

with open('Settings.Designer.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Make tableVisual 2 rows, 3 columns
content = re.sub(
    r'tableVisual\.ColumnCount = \d+;',
    r'tableVisual.ColumnCount = 3;',
    content
)

content = re.sub(
    r'tableVisual\.RowCount = \d+;',
    r'tableVisual.RowCount = 2;',
    content
)

# Set controls for tableVisual
controls_block = """            tableVisual.Controls.Add(comboVisual, 0, 0);
            tableVisual.Controls.Add(comboResolution, 1, 0);
            tableVisual.Controls.Add(comboColorTemp, 2, 0);
            tableVisual.Controls.Add(comboKeyboard, 0, 1);
            tableVisual.Controls.Add(buttonKeyboardColor, 1, 1);
            tableVisual.Controls.Add(buttonKeyboard, 2, 1);"""

content = re.sub(
    r'tableVisual\.Controls\.Add\(comboVisual, [0-9]+, [0-9]+\);\s+tableVisual\.Controls\.Add\(comboResolution, [0-9]+, [0-9]+\);\s+tableVisual\.Controls\.Add\(comboColorTemp, [0-9]+, [0-9]+\);\s+tableVisual\.Controls\.Add\(comboGamut, [0-9]+, [0-9]+\);',
    controls_block,
    content
)
content = re.sub(r'tableVisual\.Controls\.Add\(buttonInstallColor, [0-9]+, [0-9]+\);\s+', '', content)

with open('Settings.Designer.cs', 'w', encoding='utf-8') as f:
    f.write(content)

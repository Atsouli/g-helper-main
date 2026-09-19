import re

with open('Settings.Designer.cs', 'r', encoding='utf-8') as f:
    content = f.read()

content = re.sub(
    r'tableButtons\.Controls\.Add\(buttonExplorer, 0, 0\);\s*tableButtons\.Controls\.Add\(buttonQuit, 2, 0\);',
    r'tableButtons.Controls.Add(buttonExplorer, 0, 0);\n            tableButtons.Controls.Add(buttonDonate, 1, 0);\n            tableButtons.Controls.Add(buttonQuit, 2, 0);',
    content
)

with open('Settings.Designer.cs', 'w', encoding='utf-8') as f:
    f.write(content)

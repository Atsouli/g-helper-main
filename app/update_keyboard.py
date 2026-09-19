import re

with open('Settings.Designer.cs', 'r', encoding='utf-8') as f:
    content = f.read()

content = re.sub(r'panelKeyboard\.Controls\.Add\(buttonKeyboardColor\);\s*', '', content)
content = re.sub(r'panelKeyboard\.Controls\.Add\(buttonKeyboard\);\s*', '', content)
content = re.sub(r'panelKeyboard\.Controls\.Add\(comboKeyboard\);\s*', '', content)

with open('Settings.Designer.cs', 'w', encoding='utf-8') as f:
    f.write(content)

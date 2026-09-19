import re

with open('Settings.Designer.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Make tableButtons 3 columns
content = re.sub(
    r'tableButtons\.ColumnCount = 4;',
    r'tableButtons.ColumnCount = 3;',
    content
)
content = re.sub(
    r'tableButtons\.ColumnStyles\.Add\(new ColumnStyle\(SizeType\.Percent, 25F\)\);\s*tableButtons\.ColumnStyles\.Add\(new ColumnStyle\(SizeType\.Percent, 25F\)\);\s*tableButtons\.ColumnStyles\.Add\(new ColumnStyle\(SizeType\.Percent, 25F\)\);\s*tableButtons\.ColumnStyles\.Add\(new ColumnStyle\(SizeType\.Percent, 25F\)\);',
    r'tableButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3F));\n            tableButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3F));\n            tableButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3F));',
    content
)

# Remove buttonUpdates from tableButtons
content = re.sub(
    r'tableButtons\.Controls\.Add\(buttonUpdates, 0, 0\);\s*',
    '',
    content
)
content = re.sub(
    r'tableButtons\.Controls\.Add\(buttonExplorer, 1, 0\);',
    r'tableButtons.Controls.Add(buttonExplorer, 0, 0);',
    content
)
content = re.sub(
    r'tableButtons\.Controls\.Add\(buttonDonate, 2, 0\);',
    r'tableButtons.Controls.Add(buttonDonate, 1, 0);',
    content
)
content = re.sub(
    r'tableButtons\.Controls\.Add\(buttonQuit, 3, 0\);',
    r'tableButtons.Controls.Add(buttonQuit, 2, 0);',
    content
)

# Rename button texts
content = re.sub(
    r'buttonDonate\.Text = ".*?";',
    r'buttonDonate.Text = "About";',
    content
)
content = re.sub(
    r'buttonExplorer\.Text = ".*?";',
    r'buttonExplorer.Text = "Configure";',
    content
)
content = re.sub(
    r'buttonQuit\.Text = ".*?";',
    r'buttonQuit.Text = "Quit";',
    content
)

with open('Settings.Designer.cs', 'w', encoding='utf-8') as f:
    f.write(content)

import re

with open('Settings.Designer.cs', 'r', encoding='utf-8') as f:
    content = f.read()

content = re.sub(
    r'tableVisual\.ColumnCount = 3;\s*tableVisual\.ColumnStyles\.Add\(new ColumnStyle\(SizeType\.Percent, 25F\)\);\s*tableVisual\.ColumnStyles\.Add\(new ColumnStyle\(SizeType\.Percent, 25F\)\);\s*tableVisual\.ColumnStyles\.Add\(new ColumnStyle\(SizeType\.Percent, 25F\)\);\s*tableVisual\.ColumnStyles\.Add\(new ColumnStyle\(SizeType\.Percent, 25F\)\);',
    r'tableVisual.ColumnCount = 3;\n            tableVisual.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3F));\n            tableVisual.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3F));\n            tableVisual.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3F));',
    content
)

with open('Settings.Designer.cs', 'w', encoding='utf-8') as f:
    f.write(content)

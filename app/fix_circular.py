import re

with open('UI/RCircularProgressBar.cs', 'r', encoding='utf-8') as f:
    content = f.read()

content = content.replace(
    'e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;\n\n            int lineThickness = 8;',
    'e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;\n\n            int lineThickness = 8;\n            if (Width <= lineThickness * 2 || Height <= lineThickness * 2) return;'
)

with open('UI/RCircularProgressBar.cs', 'w', encoding='utf-8') as f:
    f.write(content)

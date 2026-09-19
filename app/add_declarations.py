with open('Settings.Designer.cs', 'r', encoding='utf-8') as f:
    content = f.read()

new_decls = """        private RCircularProgressBar progressBattery;
        private Slider sliderVolume;
        private Label labelVolume;
        private RButton buttonMouseEmulation;
        private Label labelMouseEmulation;
"""

if 'RCircularProgressBar progressBattery;' not in content:
    content = content.replace('private System.ComponentModel.IContainer components = null;', 'private System.ComponentModel.IContainer components = null;\n' + new_decls)
    
    # Also add initialization
    new_inits = """            progressBattery = new GHelper.UI.RCircularProgressBar();
            sliderVolume = new GHelper.UI.Slider();
            labelVolume = new Label();
            buttonMouseEmulation = new GHelper.UI.RButton();
            labelMouseEmulation = new Label();
"""
    content = content.replace('components = new System.ComponentModel.Container();', 'components = new System.ComponentModel.Container();\n' + new_inits)
    
    with open('Settings.Designer.cs', 'w', encoding='utf-8') as f:
        f.write(content)

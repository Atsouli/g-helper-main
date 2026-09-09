import sys

def patch():
    file_path = "/Users/macbook/Downloads/g-helper-main/app/Handheld.Designer.cs"
    with open(file_path, "r") as f:
        content = f.read()

    # 1. Declarations
    decl_target = "        private GHelper.UI.RButton buttonROG;"
    decl_new = """        private GHelper.UI.RButton buttonROG;
        private GHelper.UI.RButton buttonLSU;
        private GHelper.UI.RButton buttonLSD;
        private GHelper.UI.RButton buttonLSL;
        private GHelper.UI.RButton buttonLSR;
        private GHelper.UI.RButton buttonRSU;
        private GHelper.UI.RButton buttonRSD;
        private GHelper.UI.RButton buttonRSL;
        private GHelper.UI.RButton buttonRSR;"""
    content = content.replace(decl_target, decl_new)

    # 2. Instantiations
    inst_target = "            buttonROG = new GHelper.UI.RButton();"
    inst_new = """            buttonROG = new GHelper.UI.RButton();
            buttonLSU = new GHelper.UI.RButton();
            buttonLSD = new GHelper.UI.RButton();
            buttonLSL = new GHelper.UI.RButton();
            buttonLSR = new GHelper.UI.RButton();
            buttonRSU = new GHelper.UI.RButton();
            buttonRSD = new GHelper.UI.RButton();
            buttonRSL = new GHelper.UI.RButton();
            buttonRSR = new GHelper.UI.RButton();"""
    content = content.replace(inst_target, inst_new)

    # 3. Controls.Add
    add_target = "            panelBindings.Controls.Add(buttonROG);"
    add_new = """            panelBindings.Controls.Add(buttonROG);
            panelBindings.Controls.Add(buttonLSU);
            panelBindings.Controls.Add(buttonLSD);
            panelBindings.Controls.Add(buttonLSL);
            panelBindings.Controls.Add(buttonLSR);
            panelBindings.Controls.Add(buttonRSU);
            panelBindings.Controls.Add(buttonRSD);
            panelBindings.Controls.Add(buttonRSL);
            panelBindings.Controls.Add(buttonRSR);"""
    content = content.replace(add_target, add_new)

    # 4. Button properties
    prop_target = """            // buttonROG
            // 
            buttonROG.Activated = false;
            buttonROG.BackColor = SystemColors.ControlLightLight;
            buttonROG.BorderColor = Color.Transparent;
            buttonROG.BorderRadius = 5;
            buttonROG.FlatStyle = FlatStyle.Flat;
            buttonROG.Location = new Point(1052, 89);
            buttonROG.Name = "buttonROG";
            buttonROG.Secondary = false;
            buttonROG.Size = new Size(136, 50);
            buttonROG.TabIndex = 63;
            buttonROG.Text = "ROG";
            buttonROG.UseVisualStyleBackColor = false;"""
            
    prop_new = prop_target + """
            // 
            // buttonLSU
            // 
            buttonLSU.Activated = false;
            buttonLSU.BackColor = SystemColors.ControlLightLight;
            buttonLSU.BorderColor = Color.Transparent;
            buttonLSU.BorderRadius = 5;
            buttonLSU.FlatStyle = FlatStyle.Flat;
            buttonLSU.Location = new Point(175, 151);
            buttonLSU.Name = "buttonLSU";
            buttonLSU.Secondary = false;
            buttonLSU.Size = new Size(136, 50);
            buttonLSU.TabIndex = 64;
            buttonLSU.Text = "LS Up";
            buttonLSU.UseVisualStyleBackColor = false;
            // 
            // buttonLSD
            // 
            buttonLSD.Activated = false;
            buttonLSD.BackColor = SystemColors.ControlLightLight;
            buttonLSD.BorderColor = Color.Transparent;
            buttonLSD.BorderRadius = 5;
            buttonLSD.FlatStyle = FlatStyle.Flat;
            buttonLSD.Location = new Point(175, 214);
            buttonLSD.Name = "buttonLSD";
            buttonLSD.Secondary = false;
            buttonLSD.Size = new Size(136, 50);
            buttonLSD.TabIndex = 65;
            buttonLSD.Text = "LS Down";
            buttonLSD.UseVisualStyleBackColor = false;
            // 
            // buttonLSL
            // 
            buttonLSL.Activated = false;
            buttonLSL.BackColor = SystemColors.ControlLightLight;
            buttonLSL.BorderColor = Color.Transparent;
            buttonLSL.BorderRadius = 5;
            buttonLSL.FlatStyle = FlatStyle.Flat;
            buttonLSL.Location = new Point(175, 277);
            buttonLSL.Name = "buttonLSL";
            buttonLSL.Secondary = false;
            buttonLSL.Size = new Size(136, 50);
            buttonLSL.TabIndex = 66;
            buttonLSL.Text = "LS Left";
            buttonLSL.UseVisualStyleBackColor = false;
            // 
            // buttonLSR
            // 
            buttonLSR.Activated = false;
            buttonLSR.BackColor = SystemColors.ControlLightLight;
            buttonLSR.BorderColor = Color.Transparent;
            buttonLSR.BorderRadius = 5;
            buttonLSR.FlatStyle = FlatStyle.Flat;
            buttonLSR.Location = new Point(175, 340);
            buttonLSR.Name = "buttonLSR";
            buttonLSR.Secondary = false;
            buttonLSR.Size = new Size(136, 50);
            buttonLSR.TabIndex = 67;
            buttonLSR.Text = "LS Right";
            buttonLSR.UseVisualStyleBackColor = false;
            // 
            // buttonRSU
            // 
            buttonRSU.Activated = false;
            buttonRSU.BackColor = SystemColors.ControlLightLight;
            buttonRSU.BorderColor = Color.Transparent;
            buttonRSU.BorderRadius = 5;
            buttonRSU.FlatStyle = FlatStyle.Flat;
            buttonRSU.Location = new Point(1052, 151);
            buttonRSU.Name = "buttonRSU";
            buttonRSU.Secondary = false;
            buttonRSU.Size = new Size(136, 50);
            buttonRSU.TabIndex = 68;
            buttonRSU.Text = "RS Up";
            buttonRSU.UseVisualStyleBackColor = false;
            // 
            // buttonRSD
            // 
            buttonRSD.Activated = false;
            buttonRSD.BackColor = SystemColors.ControlLightLight;
            buttonRSD.BorderColor = Color.Transparent;
            buttonRSD.BorderRadius = 5;
            buttonRSD.FlatStyle = FlatStyle.Flat;
            buttonRSD.Location = new Point(1052, 214);
            buttonRSD.Name = "buttonRSD";
            buttonRSD.Secondary = false;
            buttonRSD.Size = new Size(136, 50);
            buttonRSD.TabIndex = 69;
            buttonRSD.Text = "RS Down";
            buttonRSD.UseVisualStyleBackColor = false;
            // 
            // buttonRSL
            // 
            buttonRSL.Activated = false;
            buttonRSL.BackColor = SystemColors.ControlLightLight;
            buttonRSL.BorderColor = Color.Transparent;
            buttonRSL.BorderRadius = 5;
            buttonRSL.FlatStyle = FlatStyle.Flat;
            buttonRSL.Location = new Point(1052, 277);
            buttonRSL.Name = "buttonRSL";
            buttonRSL.Secondary = false;
            buttonRSL.Size = new Size(136, 50);
            buttonRSL.TabIndex = 70;
            buttonRSL.Text = "RS Left";
            buttonRSL.UseVisualStyleBackColor = false;
            // 
            // buttonRSR
            // 
            buttonRSR.Activated = false;
            buttonRSR.BackColor = SystemColors.ControlLightLight;
            buttonRSR.BorderColor = Color.Transparent;
            buttonRSR.BorderRadius = 5;
            buttonRSR.FlatStyle = FlatStyle.Flat;
            buttonRSR.Location = new Point(1052, 340);
            buttonRSR.Name = "buttonRSR";
            buttonRSR.Secondary = false;
            buttonRSR.Size = new Size(136, 50);
            buttonRSR.TabIndex = 71;
            buttonRSR.Text = "RS Right";
            buttonRSR.UseVisualStyleBackColor = false;"""
    content = content.replace(prop_target, prop_new)

    with open(file_path, "w") as f:
        f.write(content)
    print("Handheld.Designer.cs patched successfully.")

patch()

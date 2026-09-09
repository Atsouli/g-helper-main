import sys

def patch():
    file_path = "/Users/macbook/Downloads/g-helper-main/app/Handheld.cs"
    with open(file_path, "r") as f:
        content = f.read()

    # 1. ButtonBinding calls
    target_bb = '            ButtonBinding("mb", "Menu", buttonMenu);'
    new_bb = """            ButtonBinding("mb", "Menu", buttonMenu);
            ButtonBinding("ls_up", "LS Up", buttonLSU);
            ButtonBinding("ls_down", "LS Down", buttonLSD);
            ButtonBinding("ls_left", "LS Left", buttonLSL);
            ButtonBinding("ls_right", "LS Right", buttonLSR);
            ButtonBinding("rs_up", "RS Up", buttonRSU);
            ButtonBinding("rs_down", "RS Down", buttonRSD);
            ButtonBinding("rs_left", "RS Left", buttonRSL);
            ButtonBinding("rs_right", "RS Right", buttonRSR);"""
    content = content.replace(target_bb, new_bb)

    # 2. VisualiseButton calls
    target_vb = '            VisualiseButton(buttonMenu, "mb");'
    new_vb = """            VisualiseButton(buttonMenu, "mb");
            VisualiseButton(buttonLSU, "ls_up");
            VisualiseButton(buttonLSD, "ls_down");
            VisualiseButton(buttonLSL, "ls_left");
            VisualiseButton(buttonLSR, "ls_right");
            VisualiseButton(buttonRSU, "rs_up");
            VisualiseButton(buttonRSD, "rs_down");
            VisualiseButton(buttonRSL, "rs_left");
            VisualiseButton(buttonRSR, "rs_right");"""
    content = content.replace(target_vb, new_vb)

    with open(file_path, "w") as f:
        f.write(content)
    print("Handheld.cs patched successfully.")

patch()

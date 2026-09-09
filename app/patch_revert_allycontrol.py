import sys

def patch():
    file_path = "/Users/macbook/Downloads/g-helper-main/app/Ally/AllyControl.cs"
    with open(file_path, "r") as f:
        content = f.read()

    # Revert BindingZone enum
    target_enum = """        M1M2 = 8,
        Trigger = 9,
        LS_UpDown = 10,
        LS_LeftRight = 11,
        RS_UpDown = 12,
        RS_LeftRight = 13
    }"""
    new_enum = """        M1M2 = 8,
        Trigger = 9
    }"""
    content = content.replace(target_enum, new_enum)

    # Revert BindZone switch
    target_switch = """                case BindingZone.Trigger:
                    KeyL1 = GetBinding("lt", desktop, desktop ? BindShiftTab : BindLT);
                    KeyR1 = GetBinding("rt", desktop, desktop ? BindMouseR : BindRT);
                    KeyL2 = GetBinding2("lt", desktop);
                    KeyR2 = GetBinding2("rt", desktop);
                    break;
                case BindingZone.LS_UpDown:
                    KeyL1 = GetBinding("ls_up", desktop);
                    KeyR1 = GetBinding("ls_down", desktop);
                    KeyL2 = GetBinding2("ls_up", desktop);
                    KeyR2 = GetBinding2("ls_down", desktop);
                    break;
                case BindingZone.LS_LeftRight:
                    KeyL1 = GetBinding("ls_left", desktop);
                    KeyR1 = GetBinding("ls_right", desktop);
                    KeyL2 = GetBinding2("ls_left", desktop);
                    KeyR2 = GetBinding2("ls_right", desktop);
                    break;
                case BindingZone.RS_UpDown:
                    KeyL1 = GetBinding("rs_up", desktop);
                    KeyR1 = GetBinding("rs_down", desktop);
                    KeyL2 = GetBinding2("rs_up", desktop);
                    KeyR2 = GetBinding2("rs_down", desktop);
                    break;
                case BindingZone.RS_LeftRight:
                    KeyL1 = GetBinding("rs_left", desktop);
                    KeyR1 = GetBinding("rs_right", desktop);
                    KeyL2 = GetBinding2("rs_left", desktop);
                    KeyR2 = GetBinding2("rs_right", desktop);
                    break;
                default:
                    return;"""
    new_switch = """                case BindingZone.Trigger:
                    KeyL1 = GetBinding("lt", desktop, desktop ? BindShiftTab : BindLT);
                    KeyR1 = GetBinding("rt", desktop, desktop ? BindMouseR : BindRT);
                    KeyL2 = GetBinding2("lt", desktop);
                    KeyR2 = GetBinding2("rt", desktop);
                    break;
                default:
                    return;"""
    content = content.replace(target_switch, new_switch)

    # Revert ApplyBindings
    target_apply = """            BindZone(BindingZone.M1M2);
            BindZone(BindingZone.Trigger);

            BindZone(BindingZone.LS_UpDown);
            BindZone(BindingZone.LS_LeftRight);
            BindZone(BindingZone.RS_UpDown);
            BindZone(BindingZone.RS_LeftRight);

            SetTurbo();"""
    new_apply = """            BindZone(BindingZone.M1M2);
            BindZone(BindingZone.Trigger);

            SetTurbo();"""
    content = content.replace(target_apply, new_apply)

    with open(file_path, "w") as f:
        f.write(content)
    print("AllyControl.cs reverted successfully.")

patch()

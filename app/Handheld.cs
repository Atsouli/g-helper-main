using GHelper.Ally;
using GHelper.UI;

namespace GHelper
{
    public partial class Handheld : RForm
    {

        static string activeBinding = "";
        static RButton? activeButton;

        public Handheld()
        {
            InitializeComponent();
            InitTheme(true);

            Text = Properties.Strings.Controller;

            labelLSTitle.Text = Properties.Strings.LSDeadzones;
            labelRSTitle.Text = Properties.Strings.RSDeadzones;
            labelLTTitle.Text = Properties.Strings.LTDeadzones;
            labelRTTitle.Text = Properties.Strings.RTDeadzones;
            labelVibraTitle.Text = Properties.Strings.VibrationStrength;
            checkController.Text = Properties.Strings.DisableController;
            buttonReset.Text = Properties.Strings.Reset;

            labelPrimary.Text = Properties.Strings.BindingPrimary;
            labelSecondary.Text = Properties.Strings.BindingSecondary;

            Shown += Handheld_Shown;

            Init();

            trackLSMin.Scroll += Controller_Scroll;
            trackLSMax.Scroll += Controller_Scroll;
            trackRSMin.Scroll += Controller_Scroll;
            trackRSMax.Scroll += Controller_Scroll;

            trackLTMin.Scroll += Controller_Scroll;
            trackLTMax.Scroll += Controller_Scroll;
            trackRTMin.Scroll += Controller_Scroll;
            trackRTMax.Scroll += Controller_Scroll;

            trackVibra.Scroll += Controller_Scroll;

            buttonReset.Click += ButtonReset_Click;

            trackLSMin.ValueChanged += Controller_Complete;
            trackLSMax.ValueChanged += Controller_Complete;
            trackRSMin.ValueChanged += Controller_Complete;
            trackRSMax.ValueChanged += Controller_Complete;

            trackLTMin.ValueChanged += Controller_Complete;
            trackLTMax.ValueChanged += Controller_Complete;
            trackRTMin.ValueChanged += Controller_Complete;
            trackRTMax.ValueChanged += Controller_Complete;

            trackVibra.ValueChanged += Controller_Complete;

            ButtonBinding("m1", "M1", buttonM1);
            ButtonBinding("m2", "M2", buttonM2);

            ButtonBinding("a", "A", buttonA);
            ButtonBinding("b", "B", buttonB);
            ButtonBinding("x", "X", buttonX);
            ButtonBinding("y", "Y", buttonY);

            ButtonBinding("du", "DPad Up", buttonDPU);
            ButtonBinding("dd", "DPad Down", buttonDPD);

            ButtonBinding("dl", "DPad Left", buttonDPL);
            ButtonBinding("dr", "DPad Right", buttonDPR);

            ButtonBinding("rt", "Right Trigger", buttonRT);
            ButtonBinding("lt", "Left Trigger", buttonLT);

            ButtonBinding("rb", "Right Bumper", buttonRB);
            ButtonBinding("lb", "Left Bumper", buttonLB);

            ButtonBinding("rs", "Right Stick", buttonRS);
            ButtonBinding("ll", "Left Stick", buttonLS);

            ButtonBinding("vb", "View", buttonView);
            ButtonBinding("mb", "Menu", buttonMenu);
            ButtonBinding("ls_up", "LS Up", buttonLSU);
            ButtonBinding("ls_down", "LS Down", buttonLSD);
            ButtonBinding("ls_left", "LS Left", buttonLSL);
            ButtonBinding("ls_right", "LS Right", buttonLSR);
            ButtonBinding("rs_up", "RS Up", buttonRSU);
            ButtonBinding("rs_down", "RS Down", buttonRSD);
            ButtonBinding("rs_left", "RS Left", buttonRSL);
            ButtonBinding("rs_right", "RS Right", buttonRSR);

            ComboBinding(comboPrimary);
            ComboBinding(comboSecondary);
            
            comboTurboPrimary.Name = "comboTurboPrimary";
            comboTurboSecondary.Name = "comboTurboSecondary";

            ComboTurbo(comboTurboPrimary);
            ComboTurbo(comboTurboSecondary);

            comboBindingMode.Items.Add("Gamepad Mode");
            comboBindingMode.Items.Add("Desktop Mode");
            comboBindingMode.SelectedIndex = 0;
            comboBindingMode.SelectedValueChanged += ComboBindingMode_SelectedValueChanged;

            buttonCC.Click += (sender, e) => buttonSoftwareBinding_Click(buttonCC, "cc", "Cmd Center");
            buttonROG.Click += (sender, e) => buttonSoftwareBinding_Click(buttonROG, "m4", "ROG");

            comboSoftware.SelectedValueChanged += ComboSoftware_SelectedValueChanged;
            textSoftware.TextChanged += TextSoftware_TextChanged;

            comboSoftwareSecondary.SelectedValueChanged += ComboSoftwareSecondary_SelectedValueChanged;
            textSoftwareSecondary.TextChanged += TextSoftwareSecondary_TextChanged;
            comboSoftwareDouble.SelectedValueChanged += ComboSoftwareDouble_SelectedValueChanged;
            textSoftwareDouble.TextChanged += TextSoftwareDouble_TextChanged;

            checkController.Checked = AppConfig.Is("controller_disabled");
            checkController.CheckedChanged += CheckController_CheckedChanged;

        }

        private void ComboBindingMode_SelectedValueChanged(object? sender, EventArgs e)
        {
            VisualiseController();
            
            // Refresh button visualisations
            VisualiseButton(buttonM1, "m1");
            VisualiseButton(buttonM2, "m2");
            VisualiseButton(buttonCC, "cc", true);
            VisualiseButton(buttonROG, "m4", true);
            VisualiseButton(buttonA, "a");
            VisualiseButton(buttonB, "b");
            VisualiseButton(buttonX, "x");
            VisualiseButton(buttonY, "y");
            VisualiseButton(buttonDPU, "du");
            VisualiseButton(buttonDPD, "dd");
            VisualiseButton(buttonDPL, "dl");
            VisualiseButton(buttonDPR, "dr");
            VisualiseButton(buttonRT, "rt");
            VisualiseButton(buttonLT, "lt");
            VisualiseButton(buttonRB, "rb");
            VisualiseButton(buttonLB, "lb");
            VisualiseButton(buttonRS, "rs");
            VisualiseButton(buttonLS, "ls");
            VisualiseButton(buttonView, "vb");
            VisualiseButton(buttonMenu, "mb");
            VisualiseButton(buttonLSU, "ls_up");
            VisualiseButton(buttonLSD, "ls_down");
            VisualiseButton(buttonLSL, "ls_left");
            VisualiseButton(buttonLSR, "ls_right");
            VisualiseButton(buttonRSU, "rs_up");
            VisualiseButton(buttonRSD, "rs_down");
            VisualiseButton(buttonRSL, "rs_left");
            VisualiseButton(buttonRSR, "rs_right");

            if (panelBinding.Visible && activeButton != null && !isSoftwareBinding(activeBinding))
            {
                buttonBinding_Click(activeButton, EventArgs.Empty, activeBinding, labelBinding.Text.Replace(Properties.Strings.Binding + ": ", ""));
            }
            else if (panelSoftwareBinding.Visible && activeButton != null && isSoftwareBinding(activeBinding))
            {
                buttonSoftwareBinding_Click(activeButton, activeBinding, labelSoftwareBinding.Text.Replace("Binding: ", ""));
            }
        }

        private bool isSoftwareBinding(string binding)
        {
            return binding == "cc" || binding == "m4";
        }

        private string GetBindingKey(string binding)
        {
            bool desktop = comboBindingMode.SelectedIndex == 1;
            return (desktop ? "bind_desktop_" : "bind_gamepad_") + binding;
        }

        private string GetBindingKey2(string binding)
        {
            bool desktop = comboBindingMode.SelectedIndex == 1;
            return (desktop ? "bind2_desktop_" : "bind2_gamepad_") + binding;
        }

        private string GetTurboKey(string binding)
        {
            bool desktop = comboBindingMode.SelectedIndex == 1;
            return (desktop ? "turbo_desktop_" : "turbo_gamepad_") + binding;
        }

        private string GetTurboKey2(string binding)
        {
            bool desktop = comboBindingMode.SelectedIndex == 1;
            return (desktop ? "turbo2_desktop_" : "turbo2_gamepad_") + binding;
        }

        private void CheckController_CheckedChanged(object? sender, EventArgs e)
        {
            AppConfig.Set("controller_disabled", checkController.Checked ? 1 : 0);
            AllyControl.DisableXBoxController(checkController.Checked);
        }

        private static object[] BuildBindingComboItems()
        {
            var list = new List<object>();
            foreach (var (groupLabel, items) in AllyControl.BindingGroups)
            {
                if (groupLabel != "")
                    list.Add(new BindingSeparator(groupLabel));
                foreach (var (code, name) in items)
                    list.Add(new BindingItem(code, name));
            }
            return list.ToArray();
        }

        private void ComboBinding(RComboBox combo)
        {
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.DrawMode = DrawMode.OwnerDrawFixed;
            combo.Items.AddRange(BuildBindingComboItems());
            combo.DrawItem += BindingCombo_DrawItem;
            combo.SelectedValueChanged += Binding_SelectedValueChanged;
        }

        private static void BindingCombo_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || sender is not ComboBox cmb) return;

            object obj = cmb.Items[e.Index];
            bool isSep = obj is BindingSeparator;

            Color back = isSep ? RForm.buttonSecond : RForm.buttonMain;

            if (!isSep && (e.State & DrawItemState.Selected) != 0)
                back = RForm.borderMain;

            using var backBrush = new SolidBrush(back);
            e.Graphics.FillRectangle(backBrush, e.Bounds);

            string text = obj.ToString() ?? string.Empty;
            Font font = isSep
                ? new Font(e.Font ?? SystemFonts.DefaultFont, FontStyle.Bold)
                : (e.Font ?? SystemFonts.DefaultFont);

            int indent = isSep ? 2 : 10;
            var textRect = new Rectangle(e.Bounds.X + indent, e.Bounds.Y,
                                         e.Bounds.Width - indent, e.Bounds.Height);

            using var foreBrush = new SolidBrush(RForm.foreMain);
            e.Graphics.DrawString(text, font, foreBrush, textRect,
                new StringFormat { LineAlignment = StringAlignment.Center });

            if (isSep) font.Dispose();
        }

        private void ComboTurbo(RComboBox combo)
        {
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.DisplayMember = "Value";
            combo.ValueMember = "Key";
            combo.Items.Add(new KeyValuePair<int, string>(0, "Off"));
            combo.Items.Add(new KeyValuePair<int, string>(50, "50"));
            combo.Items.Add(new KeyValuePair<int, string>(100, "100"));
            combo.Items.Add(new KeyValuePair<int, string>(150, "150"));
            combo.Items.Add(new KeyValuePair<int, string>(200, "200"));
            combo.Items.Add(new KeyValuePair<int, string>(250, "250"));
            combo.Items.Add(new KeyValuePair<int, string>(300, "300"));
            combo.Items.Add(new KeyValuePair<int, string>(400, "400"));
            combo.Items.Add(new KeyValuePair<int, string>(500, "500"));
            combo.SelectedValueChanged += TurboSelectedValueChanged;
        }

        private bool _updatingBindings;

        private void Binding_SelectedValueChanged(object? sender, EventArgs e)
        {
            if (_updatingBindings || sender is null) return;
            RComboBox combo = (RComboBox)sender;

            if (combo.SelectedItem is BindingSeparator)
            {
                int next = combo.SelectedIndex + 1;
                if (next < combo.Items.Count && combo.Items[next] is BindingItem)
                    combo.SelectedIndex = next;
                return;
            }

            if (combo.SelectedItem is not BindingItem item) return;

            string bindingKey = combo.Name == "comboPrimary" ? GetBindingKey(activeBinding) : GetBindingKey2(activeBinding);

            if (item.Code != "") AppConfig.Set(bindingKey, item.Code);
            else AppConfig.Remove(bindingKey);

            VisualiseButton(activeButton, activeBinding);

            AllyControl.ApplyMode();
        }

        private void TurboSelectedValueChanged(object? sender, EventArgs e)
        {
            if (sender is null) return;
            RComboBox combo = (RComboBox)sender;
            int ms = ((KeyValuePair<int, string>)combo.SelectedItem).Key;
            string key = combo.Name == "comboTurboPrimary" ? GetTurboKey(activeBinding) : GetTurboKey2(activeBinding);
            if (ms > 0) AppConfig.Set(key, ms);
            else AppConfig.Remove(key);
            AllyControl.ApplyMode();
        }

        private void SetComboValue(RComboBox combo, string value)
        {
            _updatingBindings = true;
            foreach (var obj in combo.Items)
                if (obj is BindingItem item && item.Code == value)
                {
                    combo.SelectedItem = item;
                    _updatingBindings = false;
                    return;
                }
            combo.SelectedIndex = 0;
            _updatingBindings = false;
        }

        private void SetTurboValue(RComboBox combo, int ms)
        {
            foreach (var item in combo.Items)
                if (((KeyValuePair<int, string>)item).Key == ms)
                { combo.SelectedItem = item; return; }
            combo.SelectedIndex = 0;
        }

        private void VisualiseButton(RButton button, string binding, bool isSoftware = false)
        {
            if (button == null) return;

            if (isSoftware)
            {
                string action = AppConfig.GetString(binding);
                string secondaryAction = AppConfig.GetString(binding + "_secondary");
                string doubleAction = AppConfig.GetString(binding + "_double");
                if (!string.IsNullOrEmpty(action) || !string.IsNullOrEmpty(secondaryAction) ||
                    !string.IsNullOrEmpty(doubleAction))
                {
                    button.BorderColor = colorStandard;
                    button.Activated = true;
                }
                else
                {
                    button.Activated = false;
                }
                return;
            }

            string primary = AppConfig.GetString(GetBindingKey(binding), AppConfig.GetString("bind_" + binding, ""));
            string secondary = AppConfig.GetString(GetBindingKey2(binding), AppConfig.GetString("bind2_" + binding, ""));

            if (primary != "" || secondary != "")
            {
                button.BorderColor = colorStandard;
                button.Activated = true;
            }
            else
            {
                button.Activated = false;
            }
        }

        private void ButtonBinding(string binding, string label, RButton button)
        {
            button.Click += (sender, EventArgs) => { buttonBinding_Click(sender, EventArgs, binding, label); };
            VisualiseButton(button, binding);
        }

        void buttonBinding_Click(object sender, EventArgs e, string binding, string label)
        {

            if (sender is null) return;
            RButton button = (RButton)sender;

            panelSoftwareBinding.Visible = false;
            panelBinding.Visible = true;
            panelBinding.BringToFront();

            activeButton = button;
            activeBinding = binding;

            labelBinding.Text = Properties.Strings.Binding + ": " + label;

            SetComboValue(comboPrimary, AppConfig.GetString(GetBindingKey(binding), AppConfig.GetString("bind_" + binding, "")));
            SetComboValue(comboSecondary, AppConfig.GetString(GetBindingKey2(binding), AppConfig.GetString("bind2_" + binding, "")));

            SetTurboValue(comboTurboPrimary, AppConfig.Get(GetTurboKey(binding), AppConfig.Get("turbo_" + binding, 0)));
            SetTurboValue(comboTurboSecondary, AppConfig.Get(GetTurboKey2(binding), AppConfig.Get("turbo2_" + binding, 0)));
        }

        private void SetSoftwareKeyCombo(ComboBox combo, TextBox txbox, string name)
        {
            Dictionary<string, string> customActions = new Dictionary<string, string>
            {
              {"", "--------------"},
              {"volume_down", Properties.Strings.VolumeDown},
              {"volume_up", Properties.Strings.VolumeUp},
              {"backlight_down", Properties.Strings.BacklightDown},
              {"backlight_up", Properties.Strings.BacklightUp},
              {"mute", Properties.Strings.VolumeMute},
              {"screenshot", Properties.Strings.PrintScreen},
              {"play", Properties.Strings.PlayPause},
              {"aura", Properties.Strings.ToggleAura},
              {"performance", Properties.Strings.PerformanceMode},
              {"screen", Properties.Strings.ToggleScreen},
              {"lock", Properties.Strings.LockScreen},
              {"miniled", Properties.Strings.ToggleMiniled},
              {"fnlock", Properties.Strings.ToggleFnLock},
              {"brightness_down", Properties.Strings.BrightnessDown},
              {"brightness_up", Properties.Strings.BrightnessUp},
              {"visual", Properties.Strings.VisualMode},
              {"touchscreen", Properties.Strings.ToggleTouchscreen },
              {"micmute", Properties.Strings.MuteMic},
              {"fan_zero_toggle", "Toggle 0 RPM fans"},
              {"fan_full_toggle", "Toggle 100% fans"},
              {"fan_extreme_switch", "Switch 0 RPM / 100% fans"},
              {"ghelper", Properties.Strings.OpenGHelper},
              {"overlay", Properties.Strings.Overlay},
              {"custom", Properties.Strings.Custom},
              {"controller", "Controller Mode"}
            };

            switch (name)
            {
                case "m4":
                    customActions[""] = Properties.Strings.OpenGHelper;
                    customActions.Remove("ghelper");
                    break;
            }

            combo.DataSource = new BindingSource(customActions, null);
            combo.DisplayMember = "Value";
            combo.ValueMember = "Key";
            
            string action = AppConfig.GetString(name);
            combo.SelectedValue = (action is not null) ? action : "";

            if (combo.SelectedValue == null) combo.SelectedValue = "";

            txbox.Text = AppConfig.GetString("custom_" + name);
        }

        private void buttonSoftwareBinding_Click(RButton button, string binding, string label)
        {
            panelBinding.Visible = false;
            panelSoftwareBinding.Visible = true;
            panelSoftwareBinding.BringToFront();

            if (activeButton != null) activeButton.BackColor = SystemColors.ControlLight;

            button.BackColor = colorStandard;

            activeButton = button;
            activeBinding = binding;

            labelSoftwareBinding.Text = "Binding: " + label;

            SetSoftwareKeyCombo(comboSoftware, textSoftware, binding);
            SetSoftwareKeyCombo(comboSoftwareSecondary, textSoftwareSecondary, binding + "_secondary");

            bool supportsDoubleClick = binding == "m4" || binding == "cc";
            labelSoftwareDouble.Visible = supportsDoubleClick;
            comboSoftwareDouble.Visible = supportsDoubleClick;
            textSoftwareDouble.Visible = false;
            if (supportsDoubleClick)
                SetSoftwareKeyCombo(comboSoftwareDouble, textSoftwareDouble, binding + "_double");
        }

        private void ComboSoftware_SelectedValueChanged(object? sender, EventArgs e)
        {
            if (activeBinding == null || !isSoftwareBinding(activeBinding)) return;

            string action = comboSoftware.SelectedValue?.ToString() ?? "";
            
            if (action == "custom")
            {
                textSoftware.Visible = true;
                textSoftware.Focus();
            }
            else
            {
                textSoftware.Visible = false;
            }

            if (action != "") AppConfig.Set(activeBinding, action);
            else AppConfig.Remove(activeBinding);

            VisualiseButton(activeButton, activeBinding, true);
        }

        private void TextSoftware_TextChanged(object? sender, EventArgs e)
        {
            if (activeBinding == null || !isSoftwareBinding(activeBinding)) return;
            AppConfig.Set("custom_" + activeBinding, textSoftware.Text);
        }

        private void ComboSoftwareSecondary_SelectedValueChanged(object? sender, EventArgs e)
        {
            if (activeBinding == null || !isSoftwareBinding(activeBinding)) return;

            string action = comboSoftwareSecondary.SelectedValue?.ToString() ?? "";
            
            if (action == "custom")
            {
                textSoftwareSecondary.Visible = true;
                textSoftwareSecondary.Focus();
            }
            else
            {
                textSoftwareSecondary.Visible = false;
            }

            if (action != "") AppConfig.Set(activeBinding + "_secondary", action);
            else AppConfig.Remove(activeBinding + "_secondary");

            VisualiseButton(activeButton, activeBinding, true);
        }

        private void TextSoftwareSecondary_TextChanged(object? sender, EventArgs e)
        {
            if (activeBinding == null || !isSoftwareBinding(activeBinding)) return;
            AppConfig.Set("custom_" + activeBinding + "_secondary", textSoftwareSecondary.Text);
        }

        private void ComboSoftwareDouble_SelectedValueChanged(object? sender, EventArgs e)
        {
            if (activeBinding != "m4" && activeBinding != "cc") return;

            string action = comboSoftwareDouble.SelectedValue?.ToString() ?? "";
            if (action == "custom")
            {
                textSoftwareDouble.Visible = true;
                textSoftwareDouble.Focus();
            }
            else
            {
                textSoftwareDouble.Visible = false;
            }

            if (action != "") AppConfig.Set(activeBinding + "_double", action);
            else AppConfig.Remove(activeBinding + "_double");
            VisualiseButton(activeButton, activeBinding, true);
        }

        private void TextSoftwareDouble_TextChanged(object? sender, EventArgs e)
        {
            if (activeBinding != "m4" && activeBinding != "cc") return;
            AppConfig.Set("custom_" + activeBinding + "_double", textSoftwareDouble.Text);
        }



        private void Controller_Complete(object? sender, EventArgs e)
        {
            AllyControl.SetDeadzones();
        }

        private void ButtonReset_Click(object? sender, EventArgs e)
        {
            trackLSMin.Value = 0;
            trackLSMax.Value = 100;
            trackRSMin.Value = 0;
            trackRSMax.Value = 100;

            trackLTMin.Value = 0;
            trackLTMax.Value = 100;
            trackRTMin.Value = 0;
            trackRTMax.Value = 100;

            trackVibra.Value = 100;

            AppConfig.Remove("ls_min");
            AppConfig.Remove("ls_max");
            AppConfig.Remove("rs_min");
            AppConfig.Remove("rs_max");

            AppConfig.Remove("lt_min");
            AppConfig.Remove("lt_max");
            AppConfig.Remove("rt_min");
            AppConfig.Remove("rt_max");
            AppConfig.Remove("vibra");

            VisualiseController();

        }

        private void Init()
        {
            trackLSMin.Value = AppConfig.Get("ls_min", 0);
            trackLSMax.Value = AppConfig.Get("ls_max", 100);
            trackRSMin.Value = AppConfig.Get("rs_min", 0);
            trackRSMax.Value = AppConfig.Get("rs_max", 100);

            trackLTMin.Value = AppConfig.Get("lt_min", 0);
            trackLTMax.Value = AppConfig.Get("lt_max", 100);
            trackRTMin.Value = AppConfig.Get("rt_min", 0);
            trackRTMax.Value = AppConfig.Get("rt_max", 100);

            trackVibra.Value = AppConfig.Get("vibra", 100);

            VisualiseController();
        }

        private void VisualiseController()
        {
            labelLS.Text = $"{trackLSMin.Value} - {trackLSMax.Value}%";
            labelRS.Text = $"{trackRSMin.Value} - {trackRSMax.Value}%";

            labelLT.Text = $"{trackLTMin.Value} - {trackLTMax.Value}%";
            labelRT.Text = $"{trackRTMin.Value} - {trackRTMax.Value}%";

            labelVibra.Text = $"{trackVibra.Value}%";
        }

        private void Controller_Scroll(object? sender, EventArgs e)
        {
            AppConfig.Set("ls_min", trackLSMin.Value);
            AppConfig.Set("ls_max", trackLSMax.Value);
            AppConfig.Set("rs_min", trackRSMin.Value);
            AppConfig.Set("rs_max", trackRSMax.Value);

            AppConfig.Set("lt_min", trackLTMin.Value);
            AppConfig.Set("lt_max", trackLTMax.Value);
            AppConfig.Set("rt_min", trackRTMin.Value);
            AppConfig.Set("rt_max", trackRTMax.Value);

            AppConfig.Set("vibra", trackVibra.Value);

            VisualiseController();

        }

        private void Handheld_Shown(object? sender, EventArgs e)
        {
            Height = Program.settingsForm.Height;
            Top = Program.settingsForm.Top;
            Left = Program.settingsForm.Left - Width - 5;
        }

            private sealed class BindingItem
            {
                public string Code        { get; }
                public string DisplayName { get; }
                public BindingItem(string code, string name) { Code = code; DisplayName = name; }
                public override string ToString() => DisplayName;
            }

            private sealed class BindingSeparator
            {
                public string Label { get; }
                public BindingSeparator(string label) { Label = label; }
                public override string ToString() => Label;
            }
        }
    }

using GHelper.UI;
using GHelper.Input;

namespace GHelper
{
    internal sealed class CustomButtonDialog : RForm
    {
        private const string CustomKeyboardAction = "__custom_keyboard__";
        private readonly RTextBox textName = new();
        private readonly RComboBox comboAction = new();
        private readonly RComboBox comboKey = new();
        private readonly CheckBox checkCtrl = CreateModifierCheckBox("Ctrl");
        private readonly CheckBox checkShift = CreateModifierCheckBox("Shift");
        private readonly CheckBox checkAlt = CreateModifierCheckBox("Alt");
        private readonly CheckBox checkWin = CreateModifierCheckBox("Win");
        private readonly Panel customKeyboardPanel = new();
        private readonly RowStyle customKeyboardRow = new(SizeType.Absolute, 0);

        public string ButtonName => textName.Text.Trim();
        public string ButtonAction => IsCustomKeyboardSelected() ? BuildKeyboardCommand() : comboAction.SelectedValue?.ToString() ?? string.Empty;
        public string ButtonShortcut => IsCustomKeyboardSelected() ? BuildKeyboardLabel() : comboAction.Text;

        public CustomButtonDialog(string? name = null, string? currentAction = null, string? currentShortcut = null)
        {
            Text = string.IsNullOrEmpty(name) ? "Add shortcut tile" : "Edit shortcut tile";
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(520, 225);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Padding = new Padding(16);

            var layout = new TableLayoutPanel
            {
                ColumnCount = 1,
                RowCount = 7,
                Dock = DockStyle.Fill
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            layout.RowStyles.Add(customKeyboardRow);
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

            textName.Dock = DockStyle.Fill;
            textName.MaxLength = 40;
            textName.Text = name ?? string.Empty;
            textName.AccessibleName = "Tile name";

            Dictionary<string, string> actions = GetActions();
            int[] currentKeys = string.IsNullOrWhiteSpace(currentAction)
                ? []
                : InputDispatcher.ParseHexValues(currentAction);
            bool openAsCustomKeyboard = currentKeys.Length > 0 && !actions.ContainsKey(currentAction!);
            if (!string.IsNullOrWhiteSpace(currentAction) && currentKeys.Length == 0 && !actions.ContainsKey(currentAction))
            {
                string legacyName = string.IsNullOrWhiteSpace(currentShortcut) ? currentAction : currentShortcut;
                actions.Add(currentAction, "Existing shortcut: " + legacyName);
            }

            comboAction.Dock = DockStyle.Fill;
            comboAction.DropDownStyle = ComboBoxStyle.DropDownList;
            comboAction.MaxDropDownItems = 16;
            comboAction.NativeHeight = true;
            comboAction.AccessibleName = "Action";
            comboAction.DataSource = new BindingSource(actions, null);
            comboAction.DisplayMember = "Value";
            comboAction.ValueMember = "Key";
            comboAction.SelectedValueChanged += (_, _) => UpdateCustomKeyboardVisibility();

            comboKey.Width = 180;
            comboKey.DropDownStyle = ComboBoxStyle.DropDownList;
            comboKey.MaxDropDownItems = 16;
            comboKey.NativeHeight = true;
            comboKey.AccessibleName = "Keyboard key";
            comboKey.DataSource = new BindingSource(GetKeyboardKeys(), null);
            comboKey.DisplayMember = "Value";
            comboKey.ValueMember = "Key";

            var keyboardControls = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 5, 0, 0)
            };
            keyboardControls.Controls.Add(checkCtrl);
            keyboardControls.Controls.Add(checkShift);
            keyboardControls.Controls.Add(checkAlt);
            keyboardControls.Controls.Add(checkWin);
            keyboardControls.Controls.Add(comboKey);

            customKeyboardPanel.Dock = DockStyle.Fill;
            customKeyboardPanel.Controls.Add(keyboardControls);

            if (openAsCustomKeyboard)
            {
                ApplyKeyboardCommand(currentKeys);
                comboAction.SelectedValue = CustomKeyboardAction;
            }
            else if (!string.IsNullOrWhiteSpace(currentAction))
                comboAction.SelectedValue = currentAction;
            if (comboAction.SelectedIndex < 0 && comboAction.Items.Count > 0)
                comboAction.SelectedIndex = 0;
            UpdateCustomKeyboardVisibility();

            var hint = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Top,
                ForeColor = SystemColors.GrayText,
                Text = "Choose the action that runs when this tile is selected."
            };

            var buttonCancel = CreateDialogButton("Cancel", DialogResult.Cancel);
            var buttonSave = CreateDialogButton("Save", DialogResult.None);
            buttonSave.Click += (_, _) => Save();

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };
            buttons.Controls.Add(buttonSave);
            buttons.Controls.Add(buttonCancel);

            layout.Controls.Add(new Label { AutoSize = true, Text = "Tile name" }, 0, 0);
            layout.Controls.Add(textName, 0, 1);
            layout.Controls.Add(new Label { AutoSize = true, Text = "Action" }, 0, 2);
            layout.Controls.Add(comboAction, 0, 3);
            layout.Controls.Add(customKeyboardPanel, 0, 4);
            layout.Controls.Add(hint, 0, 5);
            layout.Controls.Add(buttons, 0, 6);
            Controls.Add(layout);

            AcceptButton = buttonSave;
            CancelButton = buttonCancel;
            Shown += (_, _) => comboAction.Focus();
            InitTheme(true);
        }

        private static Dictionary<string, string> GetActions()
        {
            Dictionary<string, string> actions = new()
            {
                { "volume_down", Properties.Strings.VolumeDown },
                { "volume_up", Properties.Strings.VolumeUp },
                { "mute", Properties.Strings.VolumeMute },
                { "play", Properties.Strings.PlayPause },
                { "brightness_down", Properties.Strings.BrightnessDown },
                { "brightness_up", Properties.Strings.BrightnessUp },
                { "backlight_down", Properties.Strings.BacklightDown },
                { "backlight_up", Properties.Strings.BacklightUp },
                { "screenshot", Properties.Strings.PrintScreen },
                { "performance", Properties.Strings.PerformanceMode },
                { "aura", Properties.Strings.ToggleAura },
                { "visual", Properties.Strings.VisualMode },
                { "miniled", Properties.Strings.ToggleMiniled },
                { "screen", Properties.Strings.ToggleScreen },
                { "lock", Properties.Strings.LockScreen },
                { "fnlock", Properties.Strings.ToggleFnLock },
                { "micmute", Properties.Strings.MuteMic },
                { "touchscreen", Properties.Strings.ToggleTouchscreen },
                { "overlay", Properties.Strings.Overlay },
                { "rtss_overlay", "RTSS OSD" },
                { "ghelper", Properties.Strings.OpenGHelper },
                { "calculator", "Calculator" }
            };

            if (AppConfig.IsDUO())
            {
                actions.Add("screenpad_down", Properties.Strings.ScreenPadDown);
                actions.Add("screenpad_up", Properties.Strings.ScreenPadUp);
            }

            if (AppConfig.IsAlly())
                actions.Add("controller", "Controller Mode");

            actions.Add(CustomKeyboardAction, "Keyboard — Custom combination…");
            AddKeyboardKeys(actions);
            return actions;
        }

        private static Dictionary<Keys, string> GetKeyboardKeys()
        {
            Dictionary<Keys, string> keys = new()
            {
                { Keys.Enter, "Enter" },
                { Keys.Escape, "Escape" },
                { Keys.Space, "Space" },
                { Keys.Tab, "Tab" },
                { Keys.Back, "Backspace" },
                { Keys.Delete, "Delete" },
                { Keys.Insert, "Insert" },
                { Keys.Home, "Home" },
                { Keys.End, "End" },
                { Keys.PageUp, "Page Up" },
                { Keys.PageDown, "Page Down" },
                { Keys.Up, "Arrow Up" },
                { Keys.Down, "Arrow Down" },
                { Keys.Left, "Arrow Left" },
                { Keys.Right, "Arrow Right" }
            };

            for (Keys key = Keys.A; key <= Keys.Z; key++) keys.TryAdd(key, key.ToString());
            for (int number = 0; number <= 9; number++) keys.TryAdd(Keys.D0 + number, number.ToString());
            for (Keys key = Keys.F1; key <= Keys.F24; key++) keys.TryAdd(key, key.ToString());
            for (int number = 0; number <= 9; number++) keys.TryAdd(Keys.NumPad0 + number, "Numpad " + number);

            keys.TryAdd(Keys.Add, "Numpad +");
            keys.TryAdd(Keys.Subtract, "Numpad -");
            keys.TryAdd(Keys.Multiply, "Numpad ×");
            keys.TryAdd(Keys.Divide, "Numpad ÷");
            keys.TryAdd(Keys.Decimal, "Numpad decimal");
            keys.TryAdd(Keys.OemMinus, "Minus (-)");
            keys.TryAdd(Keys.Oemplus, "Equals (=)");
            keys.TryAdd(Keys.Oemcomma, "Comma (,)");
            keys.TryAdd(Keys.OemPeriod, "Period (.)");
            keys.TryAdd(Keys.OemQuestion, "Slash (/)");
            keys.TryAdd(Keys.OemSemicolon, "Semicolon (;)");
            keys.TryAdd(Keys.OemQuotes, "Quote (')");
            keys.TryAdd(Keys.OemOpenBrackets, "Open bracket ([)");
            keys.TryAdd(Keys.OemCloseBrackets, "Close bracket (])");
            keys.TryAdd(Keys.OemPipe, "Backslash (\\)");
            keys.TryAdd(Keys.Oemtilde, "Backtick (`)");
            return keys;
        }

        private bool IsCustomKeyboardSelected() =>
            string.Equals(comboAction.SelectedValue?.ToString(), CustomKeyboardAction, StringComparison.Ordinal);

        private void UpdateCustomKeyboardVisibility()
        {
            bool visible = IsCustomKeyboardSelected();
            customKeyboardPanel.Visible = visible;
            customKeyboardRow.Height = visible ? 64 : 0;
            ClientSize = new Size(ClientSize.Width, visible ? 289 : 225);
        }

        private string BuildKeyboardCommand()
        {
            List<Keys> keys = [];
            if (checkCtrl.Checked) keys.Add(Keys.ControlKey);
            if (checkShift.Checked) keys.Add(Keys.ShiftKey);
            if (checkAlt.Checked) keys.Add(Keys.Menu);
            if (checkWin.Checked) keys.Add(Keys.LWin);
            if (comboKey.SelectedValue is Keys key) keys.Add(key);
            return string.Join(" ", keys.Select(keyValue => $"0x{(int)keyValue:X2}"));
        }

        private string BuildKeyboardLabel()
        {
            List<string> keys = [];
            if (checkCtrl.Checked) keys.Add("Ctrl");
            if (checkShift.Checked) keys.Add("Shift");
            if (checkAlt.Checked) keys.Add("Alt");
            if (checkWin.Checked) keys.Add("Win");
            if (comboKey.SelectedIndex >= 0) keys.Add(comboKey.Text);
            return string.Join(" + ", keys);
        }

        private void ApplyKeyboardCommand(IEnumerable<int> command)
        {
            Keys? mainKey = null;
            foreach (Keys key in command.Select(value => (Keys)value))
            {
                if (key is Keys.ControlKey or Keys.LControlKey or Keys.RControlKey) checkCtrl.Checked = true;
                else if (key is Keys.ShiftKey or Keys.LShiftKey or Keys.RShiftKey) checkShift.Checked = true;
                else if (key is Keys.Menu or Keys.LMenu or Keys.RMenu) checkAlt.Checked = true;
                else if (key is Keys.LWin or Keys.RWin) checkWin.Checked = true;
                else mainKey = key;
            }
            if (mainKey.HasValue) comboKey.SelectedValue = mainKey.Value;
        }

        private static CheckBox CreateModifierCheckBox(string text) => new()
        {
            Text = text,
            AutoSize = true,
            Margin = new Padding(3, 8, 8, 3)
        };

        private static void AddKeyboardKeys(Dictionary<string, string> actions)
        {
            AddKey(actions, "Enter", Keys.Enter);
            AddKey(actions, "Escape", Keys.Escape);
            AddKey(actions, "Space", Keys.Space);
            AddKey(actions, "Tab", Keys.Tab);
            AddKey(actions, "Backspace", Keys.Back);
            AddKey(actions, "Delete", Keys.Delete);
            AddKey(actions, "Insert", Keys.Insert);
            AddKey(actions, "Home", Keys.Home);
            AddKey(actions, "End", Keys.End);
            AddKey(actions, "Page Up", Keys.PageUp);
            AddKey(actions, "Page Down", Keys.PageDown);
            AddKey(actions, "Arrow Up", Keys.Up);
            AddKey(actions, "Arrow Down", Keys.Down);
            AddKey(actions, "Arrow Left", Keys.Left);
            AddKey(actions, "Arrow Right", Keys.Right);

            for (Keys key = Keys.A; key <= Keys.Z; key++)
                AddKey(actions, key.ToString(), key);
            for (int number = 0; number <= 9; number++)
                AddKey(actions, number.ToString(), Keys.D0 + number);
            for (Keys key = Keys.F1; key <= Keys.F24; key++)
                AddKey(actions, key.ToString(), key);

            AddShortcut(actions, "Copy (Ctrl+C)", Keys.ControlKey, Keys.C);
            AddShortcut(actions, "Paste (Ctrl+V)", Keys.ControlKey, Keys.V);
            AddShortcut(actions, "Cut (Ctrl+X)", Keys.ControlKey, Keys.X);
            AddShortcut(actions, "Undo (Ctrl+Z)", Keys.ControlKey, Keys.Z);
            AddShortcut(actions, "Redo (Ctrl+Y)", Keys.ControlKey, Keys.Y);
            AddShortcut(actions, "Select All (Ctrl+A)", Keys.ControlKey, Keys.A);
            AddShortcut(actions, "Save (Ctrl+S)", Keys.ControlKey, Keys.S);
            AddShortcut(actions, "Find (Ctrl+F)", Keys.ControlKey, Keys.F);
            AddShortcut(actions, "Print (Ctrl+P)", Keys.ControlKey, Keys.P);
            AddShortcut(actions, "Switch App (Alt+Tab)", Keys.Menu, Keys.Tab);
            AddShortcut(actions, "Close Window (Alt+F4)", Keys.Menu, Keys.F4);
            AddShortcut(actions, "Show Desktop (Win+D)", Keys.LWin, Keys.D);
            AddShortcut(actions, "File Explorer (Win+E)", Keys.LWin, Keys.E);
            AddShortcut(actions, "Task Manager (Ctrl+Shift+Esc)", Keys.ControlKey, Keys.ShiftKey, Keys.Escape);
        }

        private static void AddKey(Dictionary<string, string> actions, string label, Keys key) =>
            AddShortcut(actions, label, key);

        private static void AddShortcut(Dictionary<string, string> actions, string label, params Keys[] keys)
        {
            string command = string.Join(" ", keys.Select(key => $"0x{(int)key:X2}"));
            actions.TryAdd(command, (keys.Length == 1 ? "Key — " : "Shortcut — ") + label);
        }

        private static RButton CreateDialogButton(string text, DialogResult result) => new()
        {
            Text = text,
            DialogResult = result,
            Size = new Size(100, 36),
            Margin = new Padding(4),
            Secondary = true
        };

        private void Save()
        {
            if (ButtonName.Length == 0 || ButtonAction.Length == 0)
            {
                MessageBox.Show(this, "Enter a tile name and choose an action.", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}

using GHelper.Ally;
using GHelper.AnimeMatrix;
using GHelper.AutoUpdate;
using GHelper.Battery;
using GHelper.Display;
using GHelper.Fan;
using GHelper.Gpu;
using GHelper.Helpers;
using GHelper.Input;
using GHelper.Mode;
using GHelper.Overlay;
using GHelper.Peripherals;
using GHelper.Peripherals.Keyboard;
using GHelper.Peripherals.Mouse;
using GHelper.Properties;
using GHelper.UI;
using GHelper.USB;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Timers;

namespace GHelper
{
    public partial class SettingsForm : RForm
    {
        ContextMenuStrip contextMenuStrip = new CustomContextMenu();
        ToolStripMenuItem menuEco, menuStandard, menuUltimate, menuOptimized;
        DonateControl donateControl;

        public GPUModeControl gpuControl;
        public AllyControl allyControl;
        AutoUpdateControl updateControl;

        AsusMouseSettings? mouseSettings;
        AsusKeyboardSettings? keyboardSettings;

        public AniMatrixControl matrixControl;

        public static System.Timers.Timer sensorTimer = default!;
        private static readonly bool sensorsAlways = AppConfig.Is("sensors_always");
        private readonly System.Windows.Forms.Timer batteryTimer = new() { Interval = 200 };
        private readonly System.Windows.Forms.Timer settingsTopMostTimer = new() { Interval = 750 };
        private readonly GamepadNavigation gamepadNavigation;
        private readonly RButton buttonRtssOverlay = new();
        private readonly RButton buttonRtssCustomize = new();
        private readonly List<CustomButtonDefinition> customButtons = new();

        private const string CustomButtonsConfigKey = "custom_buttons";

        public Matrix? matrixForm;
        public Slash? slashForm;
        public Fans? fansForm;
        public Extra? extraForm;
        public Updates? updatesForm;
        public Handheld? handheldForm;

        static long lastRefresh;
        static long lastBatteryRefresh;
        static long lastLostFocus;

        bool isGpuSection = true;
        bool isMuxGpu = true;

        bool batteryMouseOver = false;
        bool batteryFullMouseOver = false;

        bool sliderGammaIgnore = false;
        bool activateCheck = false;

        public SettingsForm()
        {

            InitializeComponent();
            KeyPreview = true;
            InitTheme(true);
            EnableGlassBackdrop();
            AppConfig.Set("topmost", 1);
            TopMost = true;
            settingsTopMostTimer.Tick += (_, _) => EnsureSettingsTopMost();
            settingsTabs.BackColor = formBack;
            settingsTabs.ForeColor = foreMain;
            foreach (TabPage tab in settingsTabs.TabPages)
            {
                tab.BackColor = formBack;
                tab.ForeColor = foreMain;
            }

            gpuControl = new GPUModeControl(this);
            updateControl = new AutoUpdateControl(this);
            matrixControl = new AniMatrixControl(this);
            allyControl = new AllyControl(this);

            buttonSilent.Text = Properties.Strings.Silent;
            buttonBalanced.Text = Properties.Strings.Balanced;
            buttonTurbo.Text = Properties.Strings.Turbo;
            buttonFans.Text = Properties.Strings.FansPower;

            buttonEco.Text = Properties.Strings.EcoMode;
            buttonUltimate.Text = Properties.Strings.UltimateMode;
            buttonStandard.Text = Properties.Strings.StandardMode;
            buttonOptimized.Text = Properties.Strings.Optimized;
            buttonStopGPU.Text = Properties.Strings.StopGPUApps;

            buttonScreenAuto.Text = Properties.Strings.AutoMode;
            buttonMiniled.Text = Properties.Strings.Multizone;

            buttonKeyboardColor.Text = Properties.Strings.Color;
            buttonKeyboard.Text = Properties.Strings.Extra;

            labelPerf.Text = Properties.Strings.PerformanceMode;
            labelGPU.Text = Properties.Strings.GPUMode;
            labelSreen.Text = Properties.Strings.LaptopScreen;
            UpdateKeyboardLabel();
            labelMatrix.Text = Properties.Strings.AnimeMatrix;
            labelBatteryTitle.Text = Properties.Strings.BatteryChargeLimit;

            checkStartup.Text = Properties.Strings.RunOnStartup;

            buttonMatrix.Text = "Matrix";
            buttonQuit.Text = Properties.Strings.Quit;
            buttonUpdates.Text = Properties.Strings.Updates;
            buttonDonate.Text = Properties.Strings.Donate;

            buttonController.Text = Properties.Strings.Controller + " Settings";
            labelAlly.Text = Properties.Strings.AllyController;

            labelCustomButtons.Text = "Shortcut tiles";
            buttonAddCustom.Text = "+  Add";
            buttonAddCustom.BorderRadius = 8;
            buttonAddCustom.BorderColor = colorStandard;
            buttonAddCustom.Secondary = false;
            buttonAddCustom.Activated = true;
            panelCustomButtons.AccessibleName = "Shortcut tiles";
            buttonAddCustom.AccessibleName = "Add shortcut tile";
            buttonAddCustom.Click += ButtonAddCustom_Click;
            LoadCustomButtons();

            // Accessible Labels

            panelMatrix.AccessibleName = Properties.Strings.AnimeMatrix;
            sliderBattery.AccessibleName = Properties.Strings.BatteryChargeLimit;
            buttonQuit.AccessibleName = Properties.Strings.Quit;
            buttonUpdates.AccessibleName = Properties.Strings.BiosAndDriverUpdates;
            panelPerformance.AccessibleName = Properties.Strings.PerformanceMode;
            buttonSilent.AccessibleName = Properties.Strings.Silent;
            buttonBalanced.AccessibleName = Properties.Strings.Balanced;
            buttonTurbo.AccessibleName = Properties.Strings.Turbo;
            buttonFans.AccessibleName = Properties.Strings.FansAndPower;
            panelGPU.AccessibleName = Properties.Strings.GPUMode;
            buttonEco.AccessibleName = Properties.Strings.EcoMode;
            buttonStandard.AccessibleName = Properties.Strings.StandardMode;
            buttonOptimized.AccessibleName = Properties.Strings.Optimized;
            buttonUltimate.AccessibleName = Properties.Strings.UltimateMode;
            panelScreen.AccessibleName = Properties.Strings.LaptopScreen;

            buttonScreenAuto.AccessibleName = Properties.Strings.AutoMode;
            //button60Hz.AccessibleName = "60Hz Refresh Rate";
            //button120Hz.AccessibleName = "Maximum Refresh Rate";

            panelKeyboard.AccessibleName = Properties.Strings.LaptopKeyboard;
            buttonKeyboard.AccessibleName = Properties.Strings.ExtraSettings;
            buttonKeyboardColor.AccessibleName = Properties.Strings.LaptopKeyboard + " " + Properties.Strings.Color;
            comboKeyboard.AccessibleName = Properties.Strings.LaptopBacklight;

            FormClosing += SettingsForm_FormClosing;
            Deactivate += SettingsForm_LostFocus;
            Activated += SettingsForm_Focused;

            buttonSilent.BorderColor = colorEco;
            buttonBalanced.BorderColor = colorStandard;
            buttonTurbo.BorderColor = colorTurbo;
            buttonFans.BorderColor = colorCustom;

            buttonEco.BorderColor = colorEco;
            buttonStandard.BorderColor = colorStandard;
            buttonUltimate.BorderColor = colorTurbo;
            buttonOptimized.BorderColor = colorEco;
            buttonXGM.BorderColor = colorTurbo;

            button60Hz.BorderColor = colorGray;
            button120Hz.BorderColor = colorGray;
            buttonScreenAuto.BorderColor = colorGray;
            buttonMiniled.BorderColor = colorTurbo;
            buttonOrientation.BorderColor = colorStandard;

            buttonEnergySaver.BackColor = colorEco;
            buttonEnergySaver.ForeColor = SystemColors.ControlLightLight;
            buttonEnergySaver.Click += ButtonEnergySaver_Click;

            buttonAmdOled.BackColor = colorTurbo;
            buttonAmdOled.ForeColor = SystemColors.ControlLightLight;
            buttonAmdOled.Click += ButtonAmdOled_Click;

            buttonArmoury.BackColor = colorTurbo;
            buttonArmoury.ForeColor = SystemColors.ControlLightLight;
            buttonArmoury.Click += ButtonArmoury_Click;

            buttonSilent.Click += ButtonSilent_Click;
            buttonBalanced.Click += ButtonBalanced_Click;
            buttonTurbo.Click += ButtonTurbo_Click;

            buttonEco.Click += ButtonEco_Click;
            buttonStandard.Click += ButtonStandard_Click;
            buttonUltimate.Click += ButtonUltimate_Click;
            buttonOptimized.Click += ButtonOptimized_Click;
            buttonStopGPU.Click += ButtonStopGPU_Click;
            pictureGPU.Click += PictureGPU_Click;

            VisibleChanged += SettingsForm_VisibleChanged;

            gamepadNavigation = new GamepadNavigation(components,
                () => Visible && (ContainsFocus || Form.ActiveForm == this),
                NavigateDirection,
                ActivateFocusedControl,
                HideAll,
                ChangeSettingsTab);

            button60Hz.Click += Button60Hz_Click;
            button120Hz.Click += Button120Hz_Click;
            buttonScreenAuto.Click += ButtonScreenAuto_Click;
            buttonMiniled.Click += ButtonMiniled_Click;
            buttonOrientation.Click += ButtonOrientation_Click;
            buttonFHD.Click += ButtonFHD_Click;
            buttonHDRControl.Click += ButtonHDRControl_Click;
            toolTip.SetToolTip(buttonOrientation, "Cycle the internal display orientation");
            UpdateOrientationButton();

            buttonQuit.Click += ButtonQuit_Click;

            buttonKeyboardColor.Click += ButtonKeyboardColor_Click;
            buttonKeyboardColor.Swatch2Click += ButtonKeyboardColor2_Click;

            buttonFans.Click += ButtonFans_Click;
            buttonKeyboard.Click += ButtonKeyboard_Click;
            buttonController.Click += ButtonHandheld_Click;

            labelCPUFan.Click += LabelCPUFan_Click;
            labelGPUFan.Click += LabelCPUFan_Click;

            comboMatrix.DropDownStyle = ComboBoxStyle.DropDownList;
            comboMatrixRunning.DropDownStyle = ComboBoxStyle.DropDownList;

            comboMatrix.DropDownClosed += ComboMatrix_SelectedValueChanged;
            comboMatrixRunning.DropDownClosed += ComboMatrixRunning_SelectedValueChanged;

            buttonMatrix.Click += ButtonMatrix_Click;

            checkStartup.Checked = Startup.IsScheduled();
            checkStartup.CheckedChanged += CheckStartup_CheckedChanged;

            labelVersion.Click += LabelVersion_Click;
            labelVersion.ForeColor = Color.FromArgb(128, Color.Gray);

            buttonOptimized.MouseMove += ButtonOptimized_MouseHover;
            buttonOptimized.MouseLeave += ButtonGPU_MouseLeave;

            buttonEco.MouseMove += ButtonEco_MouseHover;
            buttonEco.MouseLeave += ButtonGPU_MouseLeave;

            buttonStandard.MouseMove += ButtonStandard_MouseHover;
            buttonStandard.MouseLeave += ButtonGPU_MouseLeave;

            buttonUltimate.MouseMove += ButtonUltimate_MouseHover;
            buttonUltimate.MouseLeave += ButtonGPU_MouseLeave;

            tableGPU.MouseMove += ButtonXGM_MouseMove;
            tableGPU.MouseLeave += ButtonGPU_MouseLeave;

            buttonXGM.Click += ButtonXGM_Click;

            buttonScreenAuto.MouseMove += ButtonScreenAuto_MouseHover;
            buttonScreenAuto.MouseLeave += ButtonScreen_MouseLeave;

            sliderScreenBrightness.MouseUp += SliderScreenBrightness_MouseUp;
            sliderScreenBrightness.KeyUp += SliderScreenBrightness_KeyUp;
            sliderScreenBrightness.ValueChanged += SliderScreenBrightness_ValueChanged;

            button60Hz.MouseMove += Button60Hz_MouseHover;
            button60Hz.MouseLeave += ButtonScreen_MouseLeave;

            button120Hz.MouseMove += Button120Hz_MouseHover;
            button120Hz.MouseLeave += ButtonScreen_MouseLeave;

            buttonFHD.MouseMove += ButtonFHD_MouseHover;
            buttonFHD.MouseLeave += ButtonScreen_MouseLeave;

            buttonExplorer.Click += ButtonExplorer_Click;
            buttonUpdates.Click += ButtonUpdates_Click;

            sliderBattery.MouseUp += SliderBattery_MouseUp;
            sliderBattery.KeyUp += SliderBattery_KeyUp;
            sliderBattery.ValueChanged += SliderBattery_ValueChanged;
            batteryTimer.Tick += (_, _) => { batteryTimer.Stop(); BatteryControl.SetBatteryChargeLimit(sliderBattery.Value); };
            if (AppConfig.IsChargeLimit6080()) sliderBattery.supportedValues = new() { 60, 65, 70, 75, 80, 100 };

            sensorTimer = new System.Timers.Timer(AppConfig.Get("sensor_timer", 1000));
            sensorTimer.Elapsed += OnTimedEvent;
            sensorTimer.Enabled = sensorsAlways;

            labelCharge.MouseEnter += PanelBattery_MouseEnter;
            labelCharge.MouseLeave += PanelBattery_MouseLeave;
            labelBattery.Click += LabelBattery_Click;

            buttonPeripheral1.Click += ButtonPeripheral_Click;
            buttonPeripheral2.Click += ButtonPeripheral_Click;
            buttonPeripheral3.Click += ButtonPeripheral_Click;

            buttonPeripheral1.MouseEnter += ButtonPeripheral_MouseEnter;
            buttonPeripheral2.MouseEnter += ButtonPeripheral_MouseEnter;
            buttonPeripheral3.MouseEnter += ButtonPeripheral_MouseEnter;

            buttonBatteryFull.MouseEnter += ButtonBatteryFull_MouseEnter;
            buttonBatteryFull.MouseLeave += ButtonBatteryFull_MouseLeave;
            buttonBatteryFull.Click += ButtonBatteryFull_Click;

            buttonControllerMode.Click += ButtonControllerMode_Click;
            buttonBacklight.Click += ButtonBacklight_Click;

            buttonFPS.Click += ButtonFPS_Click;
            buttonOverlay.Click += ButtonOverlay_Click;
            buttonOverlay.BorderColor = colorStandard;
            ConfigureRtssOverlayButton();

            buttonAutoTDP.Click += ButtonAutoTDP_Click;
            buttonAutoTDP.BorderColor = colorTurbo;

            Text = "G-Helper " + (ProcessHelper.IsUserAdministrator() ? "—" : "-") + " " + AppConfig.GetModelShort();
            TopMost = AppConfig.Is("topmost");

            //This will auto position the window again when it resizes. Might mess with position if people drag the window somewhere else.
            this.Resize += SettingsForm_Resize;

            VisualiseFnLock();
            buttonFnLock.Click += ButtonFnLock_Click;

            labelVisual.Click += LabelVisual_Click;
            labelCharge.Click += LabelCharge_Click;

            donateControl = new DonateControl(this, buttonDonate);
            donateControl.Init();

            labelBacklight.ForeColor = colorStandard;
            labelBacklight.Click += LabelBacklight_Click;

            panelPerformance.Focus();
            InitVisual();
        }

        private sealed class CustomButtonDefinition
        {
            public string Name { get; set; } = string.Empty;
            public string Action { get; set; } = string.Empty;
            public string Shortcut { get; set; } = string.Empty;
        }

        private void LoadCustomButtons()
        {
            customButtons.Clear();
            string? json = AppConfig.GetString(CustomButtonsConfigKey);

            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    customButtons.AddRange((JsonSerializer.Deserialize<List<CustomButtonDefinition>>(json) ?? [])
                        .Where(button => !string.IsNullOrWhiteSpace(button.Name) && !string.IsNullOrWhiteSpace(button.Action)));
                }
                catch (JsonException ex)
                {
                    Logger.WriteLine("Failed to load custom buttons: " + ex.Message);
                }
            }

            RenderCustomButtons();
        }

        private void SaveCustomButtons()
        {
            AppConfig.Set(CustomButtonsConfigKey, JsonSerializer.Serialize(customButtons));
            RenderCustomButtons();
        }

        private void RenderCustomButtons()
        {
            tableCustomButtons.SuspendLayout();
            tableCustomButtons.Controls.Clear();
            tableCustomButtons.ColumnStyles.Clear();
            tableCustomButtons.RowStyles.Clear();
            const int columnCount = 2;
            const int cardRowHeight = 116;
            tableCustomButtons.ColumnCount = columnCount;
            tableCustomButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableCustomButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            int rowCount = Math.Max(1, (int)Math.Ceiling(customButtons.Count / (double)columnCount));
            tableCustomButtons.RowCount = rowCount;
            tableCustomButtons.Height = customButtons.Count == 0 ? 82 : rowCount * cardRowHeight;

            if (customButtons.Count == 0)
            {
                var empty = new Label
                {
                    AutoSize = true,
                    Dock = DockStyle.Fill,
                    ForeColor = SystemColors.GrayText,
                    Padding = new Padding(8, 12, 8, 12),
                    Text = "No custom buttons yet."
                };
                tableCustomButtons.Controls.Add(empty, 0, 0);
                tableCustomButtons.SetColumnSpan(empty, columnCount);
            }
            else
            {
                for (int i = 0; i < customButtons.Count; i++)
                {
                    CustomButtonDefinition definition = customButtons[i];
                    int row = i / columnCount;
                    int column = i % columnCount;
                    if (column == 0) tableCustomButtons.RowStyles.Add(new RowStyle(SizeType.Absolute, cardRowHeight));

                    string shortcut = string.IsNullOrWhiteSpace(definition.Shortcut) ? definition.Action : definition.Shortcut;
                    var run = new RShortcutTile
                    {
                        Title = definition.Name,
                        Shortcut = shortcut,
                        Dock = DockStyle.Fill,
                        Margin = new Padding(0, 0, 0, 3),
                        BackColor = buttonMain,
                        ForeColor = foreMain,
                        Activated = false,
                        Borderless = true,
                        BorderRadius = 8
                    };
                    run.AccessibleName = definition.Name + ": " + definition.Action;
                    toolTip.SetToolTip(run, definition.Action);
                    run.Click += (_, _) => InputDispatcher.RunCustomAction(definition.Action);

                    var edit = CreateCustomButton("Edit", true, 30);
                    edit.Click += (_, _) => EditCustomButton(definition);

                    var remove = CreateCustomButton("Delete", true, 30);
                    remove.ForeColor = colorTurbo;
                    remove.Click += (_, _) => RemoveCustomButton(definition);

                    var actions = new TableLayoutPanel
                    {
                        ColumnCount = 2,
                        RowCount = 1,
                        Dock = DockStyle.Fill,
                        Margin = new Padding(0),
                        BackColor = Color.Transparent
                    };
                    actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                    actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                    actions.Controls.Add(edit, 0, 0);
                    actions.Controls.Add(remove, 1, 0);

                    var content = new TableLayoutPanel
                    {
                        ColumnCount = 1,
                        RowCount = 2,
                        Dock = DockStyle.Fill,
                        Margin = new Padding(0),
                        Padding = new Padding(0),
                        BackColor = Color.Transparent
                    };
                    content.RowStyles.Add(new RowStyle(SizeType.Percent, 64F));
                    content.RowStyles.Add(new RowStyle(SizeType.Percent, 36F));
                    content.Controls.Add(run, 0, 0);
                    content.Controls.Add(actions, 0, 1);

                    var card = new RGlassPanel
                    {
                        Dock = DockStyle.Fill,
                        Margin = new Padding(5),
                        Padding = new Padding(7),
                        BackColor = Color.Transparent,
                        CornerRadius = 10
                    };
                    card.Controls.Add(content);
                    tableCustomButtons.Controls.Add(card, column, row);
                }
            }

            tableCustomButtons.ResumeLayout(true);
            panelCustomButtons.PerformLayout();
            PerformLayout();
        }

        private static RButton CreateCustomButton(string text, bool secondary, int minimumHeight = 48)
        {
            var button = new RButton
            {
                Text = text,
                AutoEllipsis = true,
                Dock = DockStyle.Fill,
                Margin = new Padding(2),
                MinimumSize = new Size(0, minimumHeight),
                BackColor = secondary ? buttonSecond : buttonMain,
                ForeColor = foreMain,
                BorderColor = Color.Transparent,
                BorderRadius = 7,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Regular),
                Secondary = secondary
            };
            button.FlatAppearance.BorderColor = Color.FromArgb(95, colorStandard);
            return button;
        }

        private void ButtonAddCustom_Click(object? sender, EventArgs e)
        {
            using var dialog = new CustomButtonDialog();
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            customButtons.Add(new CustomButtonDefinition { Name = dialog.ButtonName, Action = dialog.ButtonAction });
            customButtons[^1].Shortcut = dialog.ButtonShortcut;
            SaveCustomButtons();
        }

        private void EditCustomButton(CustomButtonDefinition definition)
        {
            using var dialog = new CustomButtonDialog(definition.Name, definition.Action, definition.Shortcut);
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            definition.Name = dialog.ButtonName;
            definition.Action = dialog.ButtonAction;
            definition.Shortcut = dialog.ButtonShortcut;
            SaveCustomButtons();
        }

        private void RemoveCustomButton(CustomButtonDefinition definition)
        {
            if (MessageBox.Show(this, $"Remove '{definition.Name}'?", "Remove custom button",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            customButtons.Remove(definition);
            SaveCustomButtons();
        }

        private void ButtonArmoury_Click(object? sender, EventArgs e)
        {
            var dialogResult = ShowMessage("Armoury Crate is active, download official uninstaller app?", "Armoury Crate", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes) AsusService.RunArmouryUninstaller();
        }


        private void ButtonAmdOled_Click(object? sender, EventArgs e)
        {
            AmdDisplay.RunAdrenaline();
            activateCheck = true;
        }

        private void LabelBattery_Click(object? sender, EventArgs e)
        {
            HardwareControl.chargeWatt = !HardwareControl.chargeWatt;
            RefreshSensors(true);
        }

        private void ButtonEnergySaver_Click(object? sender, EventArgs e)
        {
            KeyboardHook.KeyKeyPress(Keys.LWin, Keys.A);
        }

        private void LabelBacklight_Click(object? sender, EventArgs e)
        {
            if (AppConfig.IsDynamicLighting() && DynamicLightingHelper.IsEnabled()) DynamicLightingHelper.OpenSettings();
        }

        private void ButtonFHD_Click(object? sender, EventArgs e)
        {
            ScreenControl.ToogleFHD();
        }

        private void ButtonHDRControl_Click(object? sender, EventArgs e)
        {
            ScreenControl.ToogleHDRControl();
        }

        private void SliderBattery_ValueChanged(object? sender, EventArgs e)
        {
            VisualiseBatteryTitle(sliderBattery.Value);
        }

        private void SliderBattery_KeyUp(object? sender, KeyEventArgs e)
        {
            batteryTimer.Stop();
            batteryTimer.Start();
        }

        private void SliderBattery_MouseUp(object? sender, MouseEventArgs e)
        {
            batteryTimer.Stop();
            batteryTimer.Start();
        }

        private void ButtonAutoTDP_Click(object? sender, EventArgs e)
        {
            allyControl.ToggleAutoTDP();
        }

        private void LabelCharge_Click(object? sender, EventArgs e)
        {
            BatteryControl.BatteryReport();
        }

        private void LabelVisual_Click(object? sender, EventArgs e)
        {
            labelVisual.Visible = false;
            VisualControl.forceVisual = true;
        }

        public void InitVisual()
        {

            if (AppConfig.Is("hide_visual")) return;

            if (AppConfig.IsOLED())
            {
                panelGamma.Visible = true;
                sliderGamma.Visible = true;
                labelGammaTitle.Text = Properties.Strings.FlickerFreeDimming + " / " + Properties.Strings.VisualMode;

                VisualiseBrightness();

                sliderGamma.ValueChanged += SliderGamma_ValueChanged;
                sliderGamma.MouseUp += SliderGamma_ValueChanged;

            }
            else
            {
                labelGammaTitle.Text = Properties.Strings.VisualMode;
            }

            var gamuts = VisualControl.GetGamutModes();

            // Color profiles exist
            if (gamuts.Count > 0)
            {
                tableVisual.ColumnCount = 3;
                buttonInstallColor.Visible = false;
            }
            else
            {
                // If it's possible to retrieve color profiles
                if (ColorProfileHelper.ProfileExists())
                {
                    tableVisual.ColumnCount = 2;

                    buttonInstallColor.Text = Properties.Strings.DownloadColorProfiles;
                    buttonInstallColor.Visible = true;
                    buttonInstallColor.Click += ButtonInstallColorProfile_Click;

                    panelGamma.Visible = true;
                    tableVisual.Visible = true;
                }

                return;
            }

            panelGamma.Visible = true;
            tableVisual.Visible = true;

            var visualValue = (SplendidCommand)AppConfig.Get("visual", (int)VisualControl.GetDefaultVisualMode());
            var colorTempValue = AppConfig.Get("color_temp", VisualControl.DefaultColorTemp);

            comboVisual.DropDownStyle = ComboBoxStyle.DropDownList;
            comboVisual.DataSource = new BindingSource(VisualControl.GetVisualModes(), null);
            comboVisual.DisplayMember = "Value";
            comboVisual.ValueMember = "Key";
            comboVisual.SelectedValue = visualValue;

            comboColorTemp.DropDownStyle = ComboBoxStyle.DropDownList;
            comboColorTemp.DataSource = new BindingSource(VisualControl.GetTemperatures(), null);
            comboColorTemp.DisplayMember = "Value";
            comboColorTemp.ValueMember = "Key";
            comboColorTemp.SelectedValue = colorTempValue;

            VisualControl.SetVisual(visualValue, colorTempValue, true);

            comboVisual.SelectedValueChanged += ComboVisual_SelectedValueChanged;
            comboVisual.Visible = true;
            VisualiseDisabled();

            comboColorTemp.SelectedValueChanged += ComboVisual_SelectedValueChanged;
            comboColorTemp.Visible = true;

            // Resolution dropdown
            var laptopScreen = GHelper.Display.ScreenNative.FindLaptopScreen();
            var resolutions = GHelper.Display.ScreenNative.GetResolutions(laptopScreen);
            if (resolutions.Count > 0)
            {
                comboResolution.DropDownStyle = ComboBoxStyle.DropDownList;
                comboResolution.Items.Clear();
                foreach (var r in resolutions)
                    comboResolution.Items.Add(r);
                string currentRes = GHelper.Display.ScreenNative.GetCurrentResolution(laptopScreen);
                int idx = comboResolution.Items.IndexOf(currentRes);
                comboResolution.SelectedIndex = idx >= 0 ? idx : 0;
                comboResolution.SelectedIndexChanged += ComboResolution_SelectedIndexChanged;
                comboResolution.Visible = true;
            }

            if (gamuts.Count <= 1) return;

            comboGamut.DropDownStyle = ComboBoxStyle.DropDownList;
            comboGamut.DataSource = new BindingSource(gamuts, null);
            comboGamut.DisplayMember = "Value";
            comboGamut.ValueMember = "Key";
            comboGamut.SelectedValue = (SplendidGamut)AppConfig.Get("gamut", (int)VisualControl.GetDefaultGamut());

            comboGamut.SelectedValueChanged += ComboGamut_SelectedValueChanged;
            comboGamut.Visible = true;

        }

        private void ComboResolution_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var laptopScreen = GHelper.Display.ScreenNative.FindLaptopScreen();
            if (laptopScreen is null) return;
            var selected = comboResolution.SelectedItem?.ToString();
            if (selected is null) return;
            var parts = selected.Split('x');
            if (parts.Length != 2) return;
            if (int.TryParse(parts[0], out int w) && int.TryParse(parts[1], out int h))
                GHelper.Display.ScreenNative.SetResolution(laptopScreen, w, h);
        }

        public void CycleVisualMode(int delta)
        {

            if (comboVisual.Items.Count < 1) return;

            if (delta > 0)
            {
                if (comboVisual.SelectedIndex < comboVisual.Items.Count - 1)
                    comboVisual.SelectedIndex += 1;
                else
                    comboVisual.SelectedIndex = 0;
            }
            else
            {
                if (comboVisual.SelectedIndex > 0)
                    comboVisual.SelectedIndex -= 1;
                else
                    comboVisual.SelectedIndex = comboVisual.Items.Count - 1;
            }

            Program.toast.RunToast(comboVisual.GetItemText(comboVisual.SelectedItem), ToastIcon.BrightnessUp);
        }

        private async void ButtonInstallColorProfile_Click(object? sender, EventArgs e)
        {
            await ColorProfileHelper.InstallProfile();
            InitVisual();
        }

        private void ComboGamut_SelectedValueChanged(object? sender, EventArgs e)
        {
            VisualControl.SetGamut((int)comboGamut.SelectedValue);
        }

        private void ComboVisual_SelectedValueChanged(object? sender, EventArgs e)
        {
            VisualControl.SetVisual((SplendidCommand)comboVisual.SelectedValue, (int)comboColorTemp.SelectedValue);
            VisualiseDisabled();
        }

        public void VisualiseBrightness()
        {
            if (InvokeRequired) { Invoke(VisualiseBrightness); return; }
            sliderGammaIgnore = true;
            sliderGamma.Value = VisualControl.GetBrightness();
            labelGamma.Text = sliderGamma.Value + "%";
            sliderGammaIgnore = false;
        }

        public void VisualiseAmdOled(bool status = false)
        {
            if (InvokeRequired) { Invoke(() => VisualiseAmdOled(status)); return; }
            buttonAmdOled.Visible = status;
        }

        public void VisualiseArmoury(bool status = false)
        {
            if (InvokeRequired) { Invoke(() => VisualiseArmoury(status)); return; }
            buttonArmoury.Visible = status;
        }

        public void VisualiseDisabled()
        {
            comboGamut.Enabled = comboColorTemp.Enabled = (SplendidCommand)AppConfig.Get("visual") != SplendidCommand.Disabled;
        }

        public void VisualiseGamut()
        {
            if (InvokeRequired) { Invoke(VisualiseGamut); return; }
            if (comboGamut.Items.Count > 0) comboGamut.SelectedIndex = 0;
        }

        private void SliderGamma_ValueChanged(object? sender, EventArgs e)
        {
            if (sliderGammaIgnore) return;
            VisualControl.SetBrightness(sliderGamma.Value);
        }

        private void ButtonOverlay_Click(object? sender, EventArgs e)
        {
            ToggleOverlay();
        }

        private void ConfigureRtssOverlayButton()
        {
            tableAMD.ColumnCount = 2;
            tableAMD.ColumnStyles.Clear();
            tableAMD.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableAMD.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableAMD.SetCellPosition(buttonFPS, new TableLayoutPanelCellPosition(0, 0));
            tableAMD.SetCellPosition(buttonAutoTDP, new TableLayoutPanelCellPosition(1, 0));

            buttonRtssOverlay.Name = "buttonRtssOverlay";
            buttonRtssOverlay.Text = "RTSS OSD";
            buttonRtssOverlay.AccessibleName = "RTSS in-game overlay";
            buttonRtssOverlay.Dock = DockStyle.Fill;
            buttonRtssOverlay.Margin = new Padding(4);
            buttonRtssOverlay.BackColor = buttonMain;
            buttonRtssOverlay.ForeColor = foreMain;
            buttonRtssOverlay.BorderColor = colorStandard;
            buttonRtssOverlay.BorderRadius = 5;
            buttonRtssOverlay.Image = Properties.Resources.icons8_heartbeat_32;
            buttonRtssOverlay.ImageAlign = ContentAlignment.MiddleRight;
            buttonRtssOverlay.TextImageRelation = TextImageRelation.ImageBeforeText;
            buttonRtssOverlay.Click += (_, _) => ToggleRtssOverlay();
            toolTip.SetToolTip(buttonRtssOverlay, "Show G-Helper sensor data inside games using RivaTuner Statistics Server");

            buttonRtssCustomize.Name = "buttonRtssCustomize";
            buttonRtssCustomize.Text = "Customize";
            buttonRtssCustomize.AccessibleName = "Customize RTSS overlay metrics";
            buttonRtssCustomize.Dock = DockStyle.Fill;
            buttonRtssCustomize.Margin = new Padding(4);
            buttonRtssCustomize.BackColor = buttonSecond;
            buttonRtssCustomize.ForeColor = foreMain;
            buttonRtssCustomize.BorderColor = colorStandard;
            buttonRtssCustomize.BorderRadius = 5;
            buttonRtssCustomize.Secondary = true;
            buttonRtssCustomize.Click += (_, _) => ShowRtssOverlaySettings();

            var overlayButtons = new TableLayoutPanel
            {
                ColumnCount = 3,
                RowCount = 1,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                BackColor = Color.Transparent
            };
            overlayButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));
            overlayButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));
            overlayButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));
            overlayButtons.Controls.Add(buttonOverlay, 0, 0);
            overlayButtons.Controls.Add(buttonRtssOverlay, 1, 0);
            overlayButtons.Controls.Add(buttonRtssCustomize, 2, 0);

            var title = new Label
            {
                Text = "In-game Overlay",
                Dock = DockStyle.Top,
                Height = 34,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = foreMain,
                Padding = new Padding(8, 0, 0, 0),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var overlayPanel = new RGlassPanel
            {
                Name = "panelOverlayControls",
                Dock = DockStyle.Top,
                Height = 132,
                Margin = new Padding(0),
                Padding = new Padding(20, 12, 20, 14),
                BackColor = Color.Transparent,
                AccessibleName = "In-game Overlay"
            };
            overlayPanel.Controls.Add(overlayButtons);
            overlayPanel.Controls.Add(title);
            tabPerformance.Controls.Add(overlayPanel);
        }

        private void ButtonHandheld_Click(object? sender, EventArgs e)
        {
            if (handheldForm == null || handheldForm.Text == "")
            {
                handheldForm = new Handheld();
                AddOwnedForm(handheldForm);
            }

            if (handheldForm.Visible)
            {
                handheldForm.Close();
            }
            else
            {
                //handheldForm.FormPosition();
                PrepareAuxiliaryWindow(handheldForm);
                handheldForm.Show();
                handheldForm.Activate();
            }
        }

        private void ButtonFPS_Click(object? sender, EventArgs e)
        {
            allyControl.ToggleFPSLimit();
        }

        private void ButtonBacklight_Click(object? sender, EventArgs e)
        {
            allyControl.ToggleBacklight();
        }

        private void ButtonControllerMode_Click(object? sender, EventArgs e)
        {
            allyControl.ToggleMode();
        }

        public void VisualiseAlly(bool visible = false)
        {
            if (!visible) return;
            if (InvokeRequired) { Invoke(() => VisualiseAlly(visible)); return; }

            panelAlly.Visible = true;
            panelKeyboardTitle.Visible = false;
            panelKeyboard.Padding = new Padding(panelKeyboard.Padding.Left, 0, panelKeyboard.Padding.Right, panelKeyboard.Padding.Bottom);

            buttonOverlay.Text = "G-Helper OSD";
            buttonOverlay.Activated = AppConfig.IsOverlay();
            buttonRtssOverlay.Activated = AppConfig.Is("rtss_overlay");

            tableAMD.Visible = true;
        }

        public void VisualiseController(ControllerMode mode)
        {
            switch (mode)
            {
                case ControllerMode.Gamepad:
                    buttonControllerMode.Text = "Gamepad";
                    break;
                case ControllerMode.Mouse:
                    buttonControllerMode.Text = "Mouse";
                    break;
                case ControllerMode.Skip:
                    buttonControllerMode.Text = "Skip";
                    break;
                default:
                    buttonControllerMode.Text = "Auto";
                    break;
            }
        }

        public void VisualiseBacklight(int backlight)
        {
            if (InvokeRequired) { Invoke(() => VisualiseBacklight(backlight)); return; }
            buttonBacklight.Text = Math.Round((double)backlight * 33.33).ToString() + "%";
        }

        public void VisualiseFPSLimit(int limit)
        {
            if (InvokeRequired) { Invoke(() => VisualiseFPSLimit(limit)); return; }
            buttonFPS.Text = "FPS Limit " + ((limit > 0 && limit <= 120) ? limit : "OFF");
        }

        public void VisualiseAutoTDP(bool status)
        {
            Logger.WriteLine($"Auto TDP: {status}");
            buttonAutoTDP.Activated = status;
        }

        private void SettingsForm_Focused(object? sender, EventArgs e)
        {
            EnsureSettingsTopMost();
            if (activateCheck)
            {
                buttonAmdOled.Visible = AmdDisplay.IsOledPowerOptimization();
                activateCheck = false;
            }
        }
        private void SettingsForm_LostFocus(object? sender, EventArgs e)
        {
            lastLostFocus = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        private void ButtonBatteryFull_Click(object? sender, EventArgs e)
        {
            BatteryControl.ToggleBatteryLimitFull();
        }

        private void ButtonBatteryFull_MouseLeave(object? sender, EventArgs e)
        {
            batteryFullMouseOver = false;
            RefreshSensors(true);
        }

        private void ButtonBatteryFull_MouseEnter(object? sender, EventArgs e)
        {
            batteryFullMouseOver = true;
            labelCharge.Text = Properties.Strings.BatteryLimitFull;
        }

        private void SettingsForm_Resize(object? sender, EventArgs e)
        {
            if (WindowState != FormWindowState.Normal)
            {
                WindowState = FormWindowState.Normal;
                return;
            }

            Left = Screen.FromControl(this).WorkingArea.Width - 10 - Width;
            Top = Screen.FromControl(this).WorkingArea.Height - 10 - Height;
        }

        private void PanelBattery_MouseEnter(object? sender, EventArgs e)
        {
            batteryMouseOver = true;
            ShowBatteryWear();
        }

        private void PanelBattery_MouseLeave(object? sender, EventArgs e)
        {
            batteryMouseOver = false;
            RefreshSensors(true);
        }

        private async void ShowBatteryWear()
        {
            //Refresh again only after 15 Minutes since the last refresh
            if (lastBatteryRefresh == 0 || Math.Abs(DateTimeOffset.Now.ToUnixTimeMilliseconds() - lastBatteryRefresh) > 15 * 60_000)
            {
                lastBatteryRefresh = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                await Task.Run(HardwareControl.RefreshBatteryHealth);
            }

            if (batteryMouseOver && HardwareControl.batteryHealth != -1)
            {
                labelCharge.Text = Properties.Strings.BatteryHealth + ": " + Math.Round(HardwareControl.batteryHealth, 1) + "%";
            }
        }

        private void SettingsForm_VisibleChanged(object? sender, EventArgs e)
        {
            sensorTimer.Enabled = this.Visible || sensorsAlways;
            gamepadNavigation.Enabled = Visible;
            settingsTopMostTimer.Enabled = Visible && !HasVisibleAuxiliaryWindow();
            if (this.Visible)
            {
                EnsureSettingsTopMost();
                Task.Run((Action)RefreshPeripheralsBattery);
                updateControl.CheckForUpdates();
                BeginInvoke((Action)FocusFirstControl);
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode;
            Keys modifiers = keyData & Keys.Modifiers;

            if ((key == Keys.Tab && modifiers == Keys.Control) ||
                (key == Keys.PageDown && modifiers == Keys.Control))
            {
                ChangeSettingsTab(1);
                return true;
            }

            if ((key == Keys.Tab && modifiers == (Keys.Control | Keys.Shift)) ||
                (key == Keys.PageUp && modifiers == Keys.Control))
            {
                ChangeSettingsTab(-1);
                return true;
            }

            if (key == Keys.Escape && modifiers == Keys.None)
            {
                if (GetFocusedControl() is ComboBox combo && combo.DroppedDown)
                    combo.DroppedDown = false;
                else
                    HideAll();
                return true;
            }

            if (modifiers == Keys.None && key is Keys.Up or Keys.Down or Keys.Left or Keys.Right)
            {
                Control? focused = GetFocusedControl();
                if (focused == settingsTabs && key is Keys.Left or Keys.Right)
                {
                    ChangeSettingsTab(key == Keys.Left ? -1 : 1);
                    return true;
                }

                // These controls own arrow keys for editing their current value.
                if (focused is not Slider && focused is not ComboBox &&
                    focused is not NumericUpDown && focused is not TextBoxBase)
                {
                    NavigateDirection(key);
                    return true;
                }
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void ChangeSettingsTab(int delta)
        {
            if (settingsTabs.TabCount == 0)
                return;

            int next = (settingsTabs.SelectedIndex + delta + settingsTabs.TabCount) % settingsTabs.TabCount;
            settingsTabs.SelectedIndex = next;
            settingsTabs.Select();
            BeginInvoke((Action)FocusFirstControl);
        }

        private void FocusFirstControl()
        {
            Control? first = GetNavigableControls().OrderBy(c => c.RectangleToScreen(c.ClientRectangle).Top)
                .ThenBy(c => c.RectangleToScreen(c.ClientRectangle).Left)
                .FirstOrDefault();
            if (first != null)
            {
                first.Select();
                ScrollFocusedControlIntoView(first);
            }
            else
            {
                settingsTabs.Select();
            }
        }

        private void NavigateDirection(Keys direction)
        {
            Control? current = GetFocusedControl();

            if (current is Slider slider)
            {
                if (direction is Keys.Left or Keys.Right)
                {
                    int sign = direction == Keys.Right ? 1 : -1;
                    if (slider.supportedValues.Count > 0)
                    {
                        slider.Value = sign > 0
                            ? slider.supportedValues.Where(v => v > slider.Value).DefaultIfEmpty(slider.Value).Min()
                            : slider.supportedValues.Where(v => v < slider.Value).DefaultIfEmpty(slider.Value).Max();
                    }
                    else
                    {
                        slider.Value = Math.Clamp(slider.Value + sign * slider.Step, slider.Min, slider.Max);
                    }
                    return;
                }
            }

            if (current is ComboBox combo)
            {
                if (combo.DroppedDown || direction is Keys.Left or Keys.Right)
                {
                    int sign = direction is Keys.Right or Keys.Down ? 1 : -1;
                    if (combo.Items.Count > 0)
                        combo.SelectedIndex = Math.Clamp(combo.SelectedIndex + sign, 0, combo.Items.Count - 1);
                    return;
                }
            }

            List<Control> candidates = GetNavigableControls();
            if (candidates.Count == 0)
                return;

            if (current == null || !candidates.Contains(current))
            {
                FocusFirstControl();
                return;
            }

            Rectangle currentRect = current.RectangleToScreen(current.ClientRectangle);
            Point currentCenter = new(currentRect.Left + currentRect.Width / 2, currentRect.Top + currentRect.Height / 2);

            Control? best = null;
            double bestScore = double.MaxValue;
            foreach (Control candidate in candidates)
            {
                if (candidate == current)
                    continue;

                Rectangle rect = candidate.RectangleToScreen(candidate.ClientRectangle);
                Point center = new(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
                int dx = center.X - currentCenter.X;
                int dy = center.Y - currentCenter.Y;
                bool inDirection = direction switch
                {
                    Keys.Up => dy < -3,
                    Keys.Down => dy > 3,
                    Keys.Left => dx < -3,
                    _ => dx > 3
                };
                if (!inDirection)
                    continue;

                double primary = direction is Keys.Up or Keys.Down ? Math.Abs(dy) : Math.Abs(dx);
                double secondary = direction is Keys.Up or Keys.Down ? Math.Abs(dx) : Math.Abs(dy);
                double score = primary * 4 + secondary + (secondary > primary * 2 ? secondary * 2 : 0);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            if (best != null)
            {
                best.Select();
                ScrollFocusedControlIntoView(best);
            }
        }

        private void ActivateFocusedControl()
        {
            Control? focused = GetFocusedControl();
            switch (focused)
            {
                case Button button:
                    button.PerformClick();
                    break;
                case CheckBox checkBox:
                    checkBox.Checked = !checkBox.Checked;
                    break;
                case ComboBox combo:
                    combo.DroppedDown = !combo.DroppedDown;
                    break;
                default:
                    focused?.Select();
                    break;
            }
        }

        private Control? GetFocusedControl()
        {
            Control? focused = ActiveControl;
            while (focused is ContainerControl container && container.ActiveControl != null)
                focused = container.ActiveControl;
            return focused;
        }

        private List<Control> GetNavigableControls()
        {
            List<Control> controls = new();
            if (settingsTabs.SelectedTab != null)
                CollectNavigableControls(settingsTabs.SelectedTab, controls);
            return controls;
        }

        private static void CollectNavigableControls(Control parent, List<Control> result)
        {
            foreach (Control control in parent.Controls)
            {
                if (!control.Visible || !control.Enabled)
                    continue;
                if (control.TabStop && control.CanSelect)
                    result.Add(control);
                if (control.HasChildren)
                    CollectNavigableControls(control, result);
            }
        }

        private static void ScrollFocusedControlIntoView(Control control)
        {
            for (Control? parent = control.Parent; parent != null; parent = parent.Parent)
            {
                if (parent is ScrollableControl scrollable)
                    scrollable.ScrollControlIntoView(control);
            }
        }

        private void RefreshPeripheralsBattery()
        {
            PeripheralsProvider.RefreshBatteryForAllDevices(true);
        }

        private void ButtonExplorer_Click(object? sender, EventArgs e)
        {
            try
            {
                Process.Start("explorer.exe", AppConfig.GetConfigFolder());
            }
            catch (Exception ex)
            {
                Logger.WriteLine("Failed to open explorer: " + ex.Message);
            }
        }

        private void ButtonUpdates_Click(object? sender, EventArgs e)
        {
            if (updatesForm == null || updatesForm.Text == "")
            {
                updatesForm = new Updates();
                AddOwnedForm(updatesForm);
            }

            if (updatesForm.Visible)
            {
                updatesForm.Close();
            }
            else
            {
                PrepareAuxiliaryWindow(updatesForm);
                updatesForm.Show();
                updatesForm.Activate();
            }
        }

        public void VisualiseMatrixPicture(string image)
        {
            if (matrixForm == null || matrixForm.Text == "") return;
            matrixForm.VisualiseMatrix(image);
        }

        protected override void WndProc(ref Message m)
        {

            if (m.Msg == NativeMethods.WM_POWERBROADCAST && m.WParam == (IntPtr)NativeMethods.PBT_APMSUSPEND)
            {
                Logger.WriteLine("System Suspend");
                GPUModeControl.suspended = true;
                Program.modeControl.SleepReset();
                m.Result = (IntPtr)1;
            }

            if (m.Msg == NativeMethods.WM_POWERBROADCAST && m.WParam == (IntPtr)NativeMethods.PBT_APMRESUMEAUTOMATIC)
            {
                Logger.WriteLine("System Resume");
                GPUModeControl.suspended = false;
                BatteryControl.AutoBattery();
                m.Result = (IntPtr)1;
            }

            if (m.Msg == NativeMethods.WM_POWERBROADCAST && m.WParam == (IntPtr)NativeMethods.PBT_POWERSETTINGCHANGE)
            {
                var settings = (NativeMethods.POWERBROADCAST_SETTING)m.GetLParam(typeof(NativeMethods.POWERBROADCAST_SETTING));
                if (settings.PowerSetting == NativeMethods.PowerSettingGuid.LIDSWITCH_STATE_CHANGE)
                {
                    switch (settings.Data)
                    {
                        case 0:
                            Logger.WriteLine("Lid Closed");
                            BatteryControl.AutoBattery();
                            InputDispatcher.lidClose = AniMatrixControl.lidClose = true;
                            Aura.ApplyBrightness(0, "Lid");
                            matrixControl.SetLidMode();
                            break;
                        case 1:
                            Logger.WriteLine("Lid Open");
                            InputDispatcher.InitFNLock();
                            InputDispatcher.lidClose = AniMatrixControl.lidClose = false;
                            Aura.ApplyBrightness(InputDispatcher.GetBacklight(), "Lid");
                            matrixControl.SetLidMode();
                            break;
                    }

                }
                else if (settings.PowerSetting == NativeMethods.PowerSettingGuid.EnergySaverStatus)
                {
                    Logger.WriteLine("Battery Saver: " + settings.Data);
                    buttonEnergySaver.Visible = settings.Data != 0;
                }
                else
                {
                    switch (settings.Data)
                    {
                        case 0:
                            Logger.WriteLine("Monitor Power Off");
                            Aura.SleepBrightness();
                            XGM.NotifyShutdown();
                            Program.hardwareOverlay?.SuspendForDisplayOff();
                            break;
                        case 1:
                            Logger.WriteLine("Monitor Power On");
                            GPUModeControl.suspended = false;
                            if (!Program.SetAutoModes(wakeup: true)) BatteryControl.AutoBattery();
                            Program.hardwareOverlay?.ResumeForDisplayOn();
                            break;
                        case 2:
                            Logger.WriteLine("Monitor Dimmed");
                            break;
                    }
                }
                m.Result = (IntPtr)1;
            }

            if (m.Msg == Program.WM_TASKBARCREATED)
            {
                Logger.WriteLine("Taskbar created, re-creating tray icon");
                if (Program.trayIcon is not null) Program.trayIcon.Visible = true;
            }

            try
            {
                base.WndProc(ref m);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        public void SetContextMenu()
        {
            var currentMode = Modes.GetCurrent();

            foreach (ToolStripItem item in contextMenuStrip.Items.Cast<ToolStripItem>().ToList())
            {
                if (item is ToolStripMenuItem menuItem) menuItem.Dispose();
            }
            contextMenuStrip.Items.Clear();
            contextMenuStrip.ShowCheckMargin = true;
            contextMenuStrip.ImageScalingSize = new Size(16, 16);
            contextMenuStrip.ShowImageMargin = false;
            Padding padding = new Padding(5, 5, 5, 5);

            var title = new ToolStripMenuItem(Properties.Strings.PerformanceMode);
            title.Margin = padding;
            title.Enabled = false;
            contextMenuStrip.Items.Add(title);

            foreach (var mode in Modes.GetDictonary())
            {
                var menuMode = new ToolStripMenuItem(mode.Value);
                menuMode.Tag = mode.Key;
                menuMode.Click += (sender, args) => { Program.modeControl.SetPerformanceMode(mode.Key); };
                menuMode.Margin = padding;
                menuMode.Checked = (mode.Key == currentMode);
                contextMenuStrip.Items.Add(menuMode);
            }

            contextMenuStrip.Items.Add("-");

            if (isGpuSection)
            {
                var titleGPU = new ToolStripMenuItem(Properties.Strings.GPUMode);
                titleGPU.Margin = padding;
                titleGPU.Enabled = false;
                contextMenuStrip.Items.Add(titleGPU);

                menuEco = new ToolStripMenuItem(Properties.Strings.EcoMode);
                menuEco.Click += ButtonEco_Click;
                menuEco.Margin = padding;
                menuEco.Checked = buttonEco.Activated;
                contextMenuStrip.Items.Add(menuEco);

                menuStandard = new ToolStripMenuItem(Properties.Strings.StandardMode);
                menuStandard.Click += ButtonStandard_Click;
                menuStandard.Margin = padding;
                menuStandard.Checked = buttonStandard.Activated;
                contextMenuStrip.Items.Add(menuStandard);

                menuUltimate = new ToolStripMenuItem(Properties.Strings.UltimateMode);
                menuUltimate.Click += ButtonUltimate_Click;
                menuUltimate.Margin = padding;
                menuUltimate.Checked = buttonUltimate.Activated;
                menuUltimate.Visible = isMuxGpu;
                contextMenuStrip.Items.Add(menuUltimate);

                menuOptimized = new ToolStripMenuItem(Properties.Strings.Optimized);
                menuOptimized.Click += ButtonOptimized_Click;
                menuOptimized.Margin = padding;
                menuOptimized.Checked = buttonOptimized.Activated;
                contextMenuStrip.Items.Add(menuOptimized);

                contextMenuStrip.Items.Add("-");
            }

            var bwIcon = new ToolStripMenuItem(Properties.Strings.BWTrayIcon);
            bwIcon.Margin = padding;
            bwIcon.Checked = AppConfig.IsBWIcon();
            bwIcon.Click += (sender, args) =>
            {
                bwIcon.Checked = !bwIcon.Checked;
                AppConfig.Set("bw_icon", bwIcon.Checked ? 1 : 0);
                VisualiseIcon();
            };
            contextMenuStrip.Items.Add(bwIcon);

            contextMenuStrip.Items.Add("-");

            var menuOverlay = new ToolStripMenuItem(Properties.Strings.Overlay);
            menuOverlay.Click += (sender, args) => ToggleOverlay();
            menuOverlay.Margin = padding;
            menuOverlay.Checked = AppConfig.IsOverlay();
            contextMenuStrip.Items.Add(menuOverlay);

            var menuOverlayGameOnly = new ToolStripMenuItem(Properties.Strings.OverlayOnlyInGames);
            menuOverlayGameOnly.Click += (sender, args) => ToggleOverlayGameOnly();
            menuOverlayGameOnly.Margin = padding;
            menuOverlayGameOnly.Checked = AppConfig.IsOverlayGameOnly();
            menuOverlayGameOnly.Enabled = AppConfig.IsOverlay();
            contextMenuStrip.Items.Add(menuOverlayGameOnly);

            var menuRtssOverlay = new ToolStripMenuItem("RTSS OSD");
            menuRtssOverlay.Click += (sender, args) => ToggleRtssOverlay();
            menuRtssOverlay.Margin = padding;
            menuRtssOverlay.Checked = AppConfig.Is("rtss_overlay");
            contextMenuStrip.Items.Add(menuRtssOverlay);

            var menuRtssSettings = new ToolStripMenuItem("Customize RTSS OSD...");
            menuRtssSettings.Click += (sender, args) => ShowRtssOverlaySettings();
            menuRtssSettings.Margin = padding;
            contextMenuStrip.Items.Add(menuRtssSettings);

            var quit = new ToolStripMenuItem(Properties.Strings.Quit);
            quit.Click += ButtonQuit_Click;
            quit.Margin = padding;
            contextMenuStrip.Items.Add(quit);

            //contextMenuStrip.ShowCheckMargin = true;
            contextMenuStrip.Renderer = new CustomMenuRenderer();

            InitContextMenuTheme();

            if (Program.trayIcon is not null) Program.trayIcon.ContextMenuStrip = contextMenuStrip;


        }

        public void InitContextMenuTheme()
        {
            if (contextMenuStrip is not null)
            {
                contextMenuStrip.BackColor = this.BackColor;
                contextMenuStrip.ForeColor = this.ForeColor;
            }

            donateControl?.ApplyTheme();
        }

        private void ButtonXGM_Click(object? sender, EventArgs e)
        {
            gpuControl.ToggleXGM();
        }


        public void SetVersionLabel(string label, bool update = false)
        {
            if (InvokeRequired)
                Invoke(delegate
                {
                    labelVersion.Text = label;
                    if (update) labelVersion.ForeColor = colorTurbo;
                });
            else
            {
                labelVersion.Text = label;
                if (update) labelVersion.ForeColor = colorTurbo;
            }
        }


        private void LabelVersion_Click(object? sender, EventArgs e)
        {
            updateControl.Update();
        }


        private static void OnTimedEvent(Object? source, ElapsedEventArgs? e)
        {
            Program.settingsForm.RefreshSensors();
        }

        private void ButtonFHD_MouseHover(object? sender, EventArgs e)
        {
            labelTipScreen.Text = "Switch to " + ((buttonFHD.Text == "FHD") ? "UHD" : "FHD") + " Mode";
        }

        private void Button120Hz_MouseHover(object? sender, EventArgs e)
        {
            labelTipScreen.Text = Properties.Strings.MaxRefreshTooltip;
        }

        private void Button60Hz_MouseHover(object? sender, EventArgs e)
        {
            labelTipScreen.Text = Properties.Strings.MinRefreshTooltip.Replace("60", ScreenControl.MIN_RATE.ToString());
        }

        private void ButtonScreen_MouseLeave(object? sender, EventArgs e)
        {
            labelTipScreen.Text = "";
        }

        private void ButtonScreenAuto_MouseHover(object? sender, EventArgs e)
        {
            labelTipScreen.Text = Properties.Strings.AutoRefreshTooltip.Replace("60", ScreenControl.MIN_RATE.ToString());
        }

        private void ButtonUltimate_MouseHover(object? sender, EventArgs e)
        {
            labelTipGPU.Text = Properties.Strings.UltimateGPUTooltip;
        }

        private void ButtonStandard_MouseHover(object? sender, EventArgs e)
        {
            labelTipGPU.Text = Properties.Strings.StandardGPUTooltip;
        }

        private void ButtonEco_MouseHover(object? sender, EventArgs e)
        {
            labelTipGPU.Text = Properties.Strings.EcoGPUTooltip;
        }

        private void ButtonOptimized_MouseHover(object? sender, EventArgs e)
        {
            labelTipGPU.Text = Properties.Strings.OptimizedGPUTooltip;
        }

        private void ButtonGPU_MouseLeave(object? sender, EventArgs e)
        {
            labelTipGPU.Text = "";
        }

        private void ButtonXGM_MouseMove(object? sender, MouseEventArgs e)
        {
            if (sender is null) return;
            TableLayoutPanel table = (TableLayoutPanel)sender;

            if (!buttonXGM.Visible) return;

            labelTipGPU.Text = buttonXGM.Bounds.Contains(table.PointToClient(Cursor.Position)) ?
                "XGMobile toggle works only in Standard mode" : "";

        }


        private void ButtonScreenAuto_Click(object? sender, EventArgs e)
        {
            ScreenControl.SetAutoRefresh(1);
            ScreenControl.AutoScreen();
        }


        private void CheckStartup_CheckedChanged(object? sender, EventArgs e)
        {
            if (sender is null) return;
            CheckBox chk = (CheckBox)sender;

            if (chk.Checked)
                Startup.Schedule();
            else
                Startup.UnSchedule();
        }

        private void ButtonMatrix_Click(object? sender, EventArgs e)
        {

            if (matrixControl.IsSlash)
            {
                if (slashForm == null || slashForm.Text == "")
                {
                    slashForm = new Slash();
                    AddOwnedForm(slashForm);
                }

                if (slashForm.Visible)
                {
                    slashForm.Close();
                }
                else
                {
                    slashForm.FormPosition();
                    PrepareAuxiliaryWindow(slashForm);
                    slashForm.Show();
                    slashForm.Activate();
                }

                return;
            }

            if (matrixForm == null || matrixForm.Text == "")
            {
                matrixForm = new Matrix();
                AddOwnedForm(matrixForm);
            }

            if (matrixForm.Visible)
            {
                matrixForm.Close();
            }
            else
            {
                matrixForm.FormPosition();
                PrepareAuxiliaryWindow(matrixForm);
                matrixForm.Show();
                matrixForm.Activate();
            }

        }

        public void VisualiseMatrixRunning(int mode)
        {
            if (InvokeRequired) { Invoke(() => VisualiseMatrixRunning(mode)); return; }
            comboMatrixRunning.SelectedIndex = mode;
            if (comboMatrix.SelectedIndex == 0) comboMatrix.SelectedIndex = 3;
        }

        public void SetMatrixRunning(int mode)
        {
            VisualiseMatrixRunning(mode);
            AppConfig.Set("matrix_running", mode);
            matrixControl.SetDevice();
            if (!matrixControl.IsSlash && matrixForm != null && matrixForm.Text != "") matrixForm.VisualiseMode();
        }

        private void ComboMatrixRunning_SelectedValueChanged(object? sender, EventArgs e)
        {
            SetMatrixRunning(comboMatrixRunning.SelectedIndex);
            if (!matrixControl.IsSlash && comboMatrixRunning.SelectedIndex == (int)MatrixMode.Text && (matrixForm == null || !matrixForm.Visible)) ButtonMatrix_Click(sender, e);
        }


        private void ComboMatrix_SelectedValueChanged(object? sender, EventArgs e)
        {
            AppConfig.Set("matrix_brightness", comboMatrix.SelectedIndex);
            matrixControl.SetDevice();
        }


        private void LabelCPUFan_Click(object? sender, EventArgs e)
        {
            FanSensorControl.fanRpm = !FanSensorControl.fanRpm;
            RefreshSensors(true);
        }

        private void ButtonKeyboardColor2_Click(object? sender, EventArgs e)
        {
            SetColorPicker("aura_color2", Aura.Color2);
        }

        private void ButtonKeyboard_Click(object? sender, EventArgs e)
        {
            if (extraForm == null || extraForm.Text == "")
            {
                extraForm = new Extra();
                AddOwnedForm(extraForm);
            }

            if (extraForm.Visible)
            {
                extraForm.Close();
            }
            else
            {
                PrepareAuxiliaryWindow(extraForm);
                extraForm.Show();
                extraForm.Activate();
            }
        }

        public void FansInit()
        {
            if (fansForm == null || fansForm.Text == "") return;
            Invoke(fansForm.InitAll);
        }

        public void GPUInit()
        {
            if (fansForm == null || fansForm.Text == "") return;
            Invoke(fansForm.InitGPU);
        }

        public void FansToggle(int index = 0)
        {
            if (fansForm == null || fansForm.Text == "")
            {
                fansForm = new Fans();
                AddOwnedForm(fansForm);
            }

            if (fansForm.Visible)
            {
                fansForm.Close();
            }
            else
            {
                fansForm.FormPosition();
                PrepareAuxiliaryWindow(fansForm);
                fansForm.Show();
                fansForm.Activate();
                fansForm.ToggleNavigation(index);
            }

        }

        private void ButtonFans_Click(object? sender, EventArgs e)
        {
            FansToggle();
        }

        private void SetColorPicker(string colorField, Color initial)
        {
            RColorPicker colorDlg = new RColorPicker(initial, colorField == "aura_color" && Aura.HasRandomColor());
            colorDlg.ColorChanged += c =>
            {
                AppConfig.Set(colorField, c.ToArgb());
                SetAura();
            };
            colorDlg.ShowDialog(this);
        }

        private void ButtonKeyboardColor_Click(object? sender, EventArgs e)
        {
            SetColorPicker("aura_color", Aura.Color1);
        }

        private void ButtonRearColor_Click(object? sender, EventArgs e)
        {
            SetColorPicker("rear_color", Aura.RearColor);
        }

        private void ComboRearLight_SelectedValueChanged(object? sender, EventArgs e)
        {
            AppConfig.Set("rear_mode", (int)comboRearLight.SelectedValue);
            SetAura();
        }

        public void InitRearLight()
        {
            if (!AppConfig.HasRearLight())
                return;

            Aura.RearMode = (AuraMode)AppConfig.Get("rear_mode");
            Aura.SetRearColor(AppConfig.Get("rear_color"));

            comboRearLight.DropDownStyle = ComboBoxStyle.DropDownList;
            comboRearLight.DataSource = new BindingSource(Aura.GetRearModes(), null);
            comboRearLight.DisplayMember = "Value";
            comboRearLight.ValueMember = "Key";
            comboRearLight.SelectedValue = Aura.RearMode;
            comboRearLight.SelectedValueChanged += ComboRearLight_SelectedValueChanged;

            buttonRearColor.Click += ButtonRearColor_Click;

            buttonRearColor.SwatchColor = Aura.RearColor;
            panelRearLight.Visible = true;
        }

        public void InitAura()
        {
            comboKeyboard.DropDownStyle = ComboBoxStyle.DropDownList;
            if (!Aura.IsBacklightDetected)
                Aura.Init();

            Aura.Mode = (AuraMode)AppConfig.Get("aura_mode");
            Aura.Speed = (AuraSpeed)AppConfig.Get("aura_speed");
            Aura.SetColor(AppConfig.Get("aura_color"));
            Aura.SetColor2(AppConfig.Get("aura_color2"));

            comboKeyboard.DataSource = new BindingSource(Aura.GetModes(), null);
            comboKeyboard.DisplayMember = "Value";
            comboKeyboard.ValueMember = "Key";
            comboKeyboard.SelectedValue = Aura.Mode;
            comboKeyboard.SelectedValueChanged += ComboKeyboard_SelectedValueChanged;


            if (Aura.isWhite)
            {
                buttonKeyboardColor.Visible = false;
            }

            if (AppConfig.NoAura())
            {
                comboKeyboard.Visible = false;
            }

            VisualiseAura();

            InitRearLight();
        }

        public void SetAura()
        {
            Task.Run(() =>
            {
                Aura.ApplyAura();
                VisualiseAura();
            });
        }

        private void _VisualiseAura()
        {
            buttonKeyboardColor.SwatchColor = Aura.Color1;
            buttonKeyboardColor.SwatchColor2 = Aura.HasSecondColor() ? Aura.Color2 : (Color?)null;

            if (panelRearLight.Visible) buttonRearColor.SwatchColor = Aura.RearColor;

            bool dynamic = AppConfig.IsDynamicLighting() && DynamicLightingHelper.IsEnabled() && !AppConfig.IsDynamicLightingOnly();

            if (dynamic)
            {
                labelBacklight.Cursor = Cursors.Hand;
                labelBacklight.Text = Strings.DisableDynamicLighting;
            } else if (Aura.Mode == AuraMode.AMBIENT)
            {
                labelBacklight.Cursor = Cursors.Default;
                labelBacklight.Text = Strings.AmbientModeResources;
            } else
            {
                labelBacklight.Cursor = Cursors.Default;
                labelBacklight.Text = "";
            }
        }

        public void VisualiseAura()
        {
            if (InvokeRequired)
                Invoke(_VisualiseAura);
            else
                _VisualiseAura();
        }

        public void InitMatrix()
        {

            if (!matrixControl.IsValid)
            {
                panelMatrix.Visible = false;
                return;
            }

            if (matrixControl.IsSlash)
            {
                labelMatrix.Text = "Slash Lighting";
                pictureMatrix.BackgroundImage = ControlHelper.TintImage(Properties.Resources.slash_32, foreMain);
                comboMatrixRunning.Items.Clear();

                foreach (var item in SlashDevice.Modes)
                {
                    comboMatrixRunning.Items.Add(item.Value);
                }

                buttonMatrix.Text = "Slash";
            }

            comboMatrix.SelectedIndex = Math.Max(0, Math.Min(AppConfig.Get("matrix_brightness", 0), comboMatrix.Items.Count - 1));
            comboMatrixRunning.SelectedIndex = Math.Min(AppConfig.Get("matrix_running", 0), comboMatrixRunning.Items.Count - 1);
        }


        public void CycleMatrix(int delta)
        {
            comboMatrix.SelectedIndex = Math.Min(Math.Max(0, comboMatrix.SelectedIndex + delta), comboMatrix.Items.Count - 1);
            AppConfig.Set("matrix_brightness", comboMatrix.SelectedIndex);
            matrixControl.SetDevice();
            Program.toast.RunToast(comboMatrix.GetItemText(comboMatrix.SelectedItem), delta > 0 ? ToastIcon.BacklightUp : ToastIcon.BacklightDown);
        }


        public void CycleAuraMode(int delta)
        {
            if (delta > 0)
            {
                if (comboKeyboard.SelectedIndex < comboKeyboard.Items.Count - 1)
                    comboKeyboard.SelectedIndex += 1;
                else
                    comboKeyboard.SelectedIndex = 0;
            }
            else
            {
                if (comboKeyboard.SelectedIndex > 0)
                    comboKeyboard.SelectedIndex -= 1;
                else
                    comboKeyboard.SelectedIndex = comboKeyboard.Items.Count - 1;
            }

            Program.toast.RunToast(comboKeyboard.GetItemText(comboKeyboard.SelectedItem), ToastIcon.BacklightUp);
        }

        private void ComboKeyboard_SelectedValueChanged(object? sender, EventArgs e)
        {
            AppConfig.Set("aura_mode", (int)comboKeyboard.SelectedValue);
            SetAura();
        }


        private void Button120Hz_Click(object? sender, EventArgs e)
        {
            ScreenControl.SetAutoRefresh(0);
            ScreenControl.SetScreen(ScreenControl.MAX_REFRESH, 1);
        }

        private void ButtonOrientation_Click(object? sender, EventArgs e)
        {
            string? laptopScreen = ScreenNative.FindLaptopScreen();
            if (laptopScreen is null) return;

            int current = ScreenNative.GetOrientation(laptopScreen);
            if (current < 0) return;

            if (ScreenNative.SetOrientation(laptopScreen, (current + 1) % 4) == 0)
                UpdateOrientationButton();
        }

        private void UpdateOrientationButton()
        {
            int orientation = ScreenNative.GetOrientation(ScreenNative.FindLaptopScreen());
            buttonOrientation.Enabled = orientation >= 0;
            buttonOrientation.Text = orientation switch
            {
                0 => "Orientation: Landscape",
                1 => "Orientation: Portrait",
                2 => "Orientation: Landscape flipped",
                3 => "Orientation: Portrait flipped",
                _ => "Orientation"
            };
        }

        private void Button60Hz_Click(object? sender, EventArgs e)
        {
            ScreenControl.SetAutoRefresh(0);
            ScreenControl.SetScreen(ScreenControl.MIN_RATE, 0);
        }


        private void ButtonMiniled_Click(object? sender, EventArgs e)
        {
            ScreenControl.ToogleMiniled();
        }



        public void VisualiseScreen(bool screenEnabled, bool screenAuto, int frequency, int maxFrequency, int overdrive, bool overdriveSetting, int miniled1, int miniled2, bool hdr, bool acm, int fhd, int hdrControl)
        {
            bool advancedColor = hdr || acm;
            UpdateOrientationButton();

            ButtonEnabled(button60Hz, screenEnabled);
            ButtonEnabled(button120Hz, screenEnabled);
            ButtonEnabled(buttonScreenAuto, screenEnabled);
            ButtonEnabled(buttonMiniled, screenEnabled);

            labelSreen.Text = screenEnabled
                ? Properties.Strings.LaptopScreen + ": " + frequency + "Hz" + ((overdrive == 1) ? " + " + Properties.Strings.Overdrive : "")
                : Properties.Strings.LaptopScreen + ": " + Properties.Strings.TurnedOff;

            panelScreen.AccessibleName = labelSreen.Text;

            sliderScreenBrightness.ValueChanged -= SliderScreenBrightness_ValueChanged;
            sliderScreenBrightness.Value = Display.ScreenBrightness.Get();
            sliderScreenBrightness.AccessibleName = Properties.Strings.LaptopScreen + " " + Properties.Strings.Brightness + ": " + sliderScreenBrightness.Value.ToString() + "%";
            sliderScreenBrightness.ValueChanged += SliderScreenBrightness_ValueChanged;

            button60Hz.Activated = false;
            button120Hz.Activated = false;
            buttonScreenAuto.Activated = false;

            if (screenAuto)
            {
                buttonScreenAuto.Activated = true;
            }
            else if (frequency == ScreenControl.MIN_RATE)
            {
                button60Hz.Activated = true;
            }
            else if (frequency > ScreenControl.MIN_RATE)
            {
                button120Hz.Activated = true;
            }

            button60Hz.Text = ScreenControl.MIN_RATE + "Hz";

            if (maxFrequency > ScreenControl.MIN_RATE)
            {
                button120Hz.Text = maxFrequency.ToString() + "Hz" + (overdriveSetting ? " + OD" : "");
                panelScreen.Visible = true;
                tableScreen.Visible = true;
            }
            else if (maxFrequency > 0)
            {
                tableScreen.Visible = false;
                panelScreen.Visible = AppConfig.NoGpu();
            }

            if (fhd >= 0)
            {
                buttonFHD.Visible = true;
                buttonFHD.Text = fhd > 0 ? "FHD" : "UHD";
            }

            bool hdrControlVisible = (hdr && hdrControl >= 0);

            if (miniled1 >= 0)
            {
                buttonMiniled.Visible = !hdrControlVisible;
                buttonMiniled.Enabled = !hdr;
                buttonMiniled.Activated = miniled1 == 1 || hdr;
            }
            else if (miniled2 >= 0)
            {
                buttonMiniled.Visible = !hdrControlVisible;
                buttonMiniled.Enabled = !hdr;
                if (hdr) miniled2 = 1; // Show HDR as Multizone Strong

                switch (miniled2)
                {
                    // Multizone On
                    case 0:
                        buttonMiniled.Text = Properties.Strings.Multizone;
                        buttonMiniled.BorderColor = colorStandard;
                        buttonMiniled.Activated = true;
                        break;
                    // Multizone Strong
                    case 1:
                        buttonMiniled.Text = Properties.Strings.MultizoneStrong;
                        buttonMiniled.BorderColor = colorTurbo;
                        buttonMiniled.Activated = true;
                        break;
                    // Multizone Off
                    case 2:
                        buttonMiniled.Text = Properties.Strings.OneZone;
                        buttonMiniled.BorderColor = colorStandard;
                        buttonMiniled.Activated = false;
                        break;
                }
            }
            else
            {
                buttonMiniled.Visible = false;
            }

            if (hdrControlVisible)
            {
                buttonHDRControl.Visible = true;
                buttonHDRControl.Activated = hdrControl > 0;
                buttonHDRControl.BorderColor = colorTurbo;
            } else
            {
                buttonHDRControl.Visible = false;
            }

            if (advancedColor) labelVisual.Text = Properties.Strings.VisualModesHDR;
            if (!screenEnabled) labelVisual.Text = Properties.Strings.VisualModesScreen;

            if (!screenEnabled || advancedColor)
            {
                labelVisual.Location = tableVisual.Location;
                labelVisual.Width = tableVisual.Width;
                labelVisual.Height = tableVisual.Height;
                labelVisual.Visible = true;
            }
            else
            {
                labelVisual.Visible = false;
            }


        }

        private void ButtonQuit_Click(object? sender, EventArgs e)
        {
            AsusLampArray.Release();
            matrixControl.Dispose();
            Close();
            Program.trayIcon.Visible = false;
            Application.Exit();
        }

        /// <summary>
        /// Closes all forms except the settings. Hides the settings
        /// </summary>
        public void HideAll()
        {
            this.Hide();
            if (fansForm != null && fansForm.Text != "") fansForm.Close();
            if (extraForm != null && extraForm.Text != "") extraForm.Close();
            if (updatesForm != null && updatesForm.Text != "") updatesForm.Close();
            if (matrixForm != null && matrixForm.Text != "") matrixForm.Close();
            if (slashForm != null && slashForm.Text != "") slashForm.Close();
            if (handheldForm != null && handheldForm.Text != "") handheldForm.Close();
            if (mouseSettings != null && mouseSettings.Text != "") mouseSettings.Close();
            if (keyboardSettings != null && keyboardSettings.Text != "") keyboardSettings.Close();
            MemoryHelper.TrimAfter();
        }

        /// <summary>
        /// Brings all visible windows to the top, with settings being the focus
        /// </summary>
        public void ShowAll()
        {
            this.Activate();
            EnsureSettingsTopMost();
        }

        private void EnsureSettingsTopMost()
        {
            if (!Visible || !IsHandleCreated)
                return;

            Form[] visibleAuxiliary = OwnedForms.Where(form =>
                !form.IsDisposed && form.Visible && form != this).ToArray();
            if (visibleAuxiliary.Length > 0)
            {
                // Native ComboBox lists are separate popup windows. If either the
                // owner or its child is topmost, Windows can immediately put that
                // popup behind the form and make it look as if it vanished.
                settingsTopMostTimer.Stop();
                TopMost = false;
                foreach (Form auxiliary in visibleAuxiliary)
                    auxiliary.TopMost = false;
                return;
            }

            TopMost = true;
            SetWindowPos(Handle, HwndTopMost, 0, 0, 0, 0,
                SwpNoMove | SwpNoSize | SwpNoActivate | SwpShowWindow);
        }

        private void PrepareAuxiliaryWindow(Form form)
        {
            settingsTopMostTimer.Stop();
            TopMost = false;
            form.TopMost = false;
            form.FormClosed -= AuxiliaryWindow_FormClosed;
            form.FormClosed += AuxiliaryWindow_FormClosed;
        }

        private void AuxiliaryWindow_FormClosed(object? sender, FormClosedEventArgs e)
        {
            if (Visible && IsHandleCreated)
            {
                BeginInvoke(() =>
                {
                    bool hasVisibleAuxiliary = HasVisibleAuxiliaryWindow();
                    settingsTopMostTimer.Enabled = !hasVisibleAuxiliary;
                    if (!hasVisibleAuxiliary)
                        EnsureSettingsTopMost();
                });
            }
        }

        private bool HasVisibleAuxiliaryWindow() => OwnedForms.Any(form =>
            !form.IsDisposed && form.Visible && form != this);

        private static readonly IntPtr HwndTopMost = new(-1);
        private const uint SwpNoSize = 0x0001;
        private const uint SwpNoMove = 0x0002;
        private const uint SwpNoActivate = 0x0010;
        private const uint SwpShowWindow = 0x0040;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr windowHandle, IntPtr insertAfter,
            int x, int y, int width, int height, uint flags);

        public DialogResult ShowMessage(string text, string title = "", MessageBoxButtons buttons = MessageBoxButtons.OK)
        {
            DialogResult result = DialogResult.None;
            Invoke((MethodInvoker)delegate
            {
                result = MessageBox.Show(this, text, title, buttons);
            });
            return result;
        }

        /// <summary>
        /// Check if any of fans, keyboard, update, or itself has focus
        /// </summary>
        /// <returns>Focus state</returns>
        public bool HasAnyFocus(bool lostFocusCheck = false)
        {
            return (fansForm != null && fansForm.ContainsFocus) ||
                   (extraForm != null && extraForm.ContainsFocus) ||
                   (updatesForm != null && updatesForm.ContainsFocus) ||
                   (matrixForm != null && matrixForm.ContainsFocus) ||
                   (slashForm != null && slashForm.ContainsFocus) ||
                   (handheldForm != null && handheldForm.ContainsFocus) ||
                   this.ContainsFocus ||
                   (lostFocusCheck && Math.Abs(DateTimeOffset.Now.ToUnixTimeMilliseconds() - lastLostFocus) < 300);
        }

        private void SettingsForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                HideAll();
            }
        }

        private void ButtonUltimate_Click(object? sender, EventArgs e)
        {
            gpuControl.SetGPUMode(AsusACPI.GPUModeUltimate);
        }

        private void ButtonStandard_Click(object? sender, EventArgs e)
        {
            gpuControl.SetGPUMode(AsusACPI.GPUModeStandard);
        }

        private void ButtonEco_Click(object? sender, EventArgs e)
        {
            gpuControl.SetGPUMode(AsusACPI.GPUModeEco);
        }


        private void ButtonOptimized_Click(object? sender, EventArgs e)
        {
            AppConfig.Set("gpu_auto", (AppConfig.Get("gpu_auto") == 1) ? 0 : 1);
            VisualiseGPUMode();
            gpuControl.AutoGPUMode(true);
        }

        private void ButtonStopGPU_Click(object? sender, EventArgs e)
        {
            gpuControl.KillGPUApps();
        }

        public async void RefreshSensors(bool force = false)
        {
            int throttle = (!Visible && sensorsAlways) ? 6000 : 2000;
            if (!force && Math.Abs(DateTimeOffset.Now.ToUnixTimeMilliseconds() - lastRefresh) < throttle) return;
            lastRefresh = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            string cpuTemp = "";
            string gpuTemp = "";

            string cpuFan = "";
            string gpuFan = "";
            string midFan = "";

            string battery = "";
            string charge = "";

            await Task.Run(() => HardwareControl.ReadSensors());
            if (Visible) _ = Task.Run((Action)PeripheralsProvider.RefreshBatteryForAllDevices);

            if (HardwareControl.cpuTemp > 0)
                cpuTemp = ": " + TempHelper.FormatTemp((double)HardwareControl.cpuTemp);

            if (HardwareControl.batteryCapacity > 0)
            {
                charge = Properties.Strings.BatteryCharge + ": " + HardwareControl.batteryCharge;
            }

            if (HardwareControl.batteryRate < 0)
                battery = Properties.Strings.Discharging + ": " + Math.Round(-(decimal)HardwareControl.batteryRate, 1).ToString() + "W";
            else if (HardwareControl.batteryRate > 0)
                battery = Properties.Strings.Charging + ": " + Math.Round((decimal)HardwareControl.batteryRate, 1).ToString() + "W";


            if (HardwareControl.gpuTemp > 0)
            {
                gpuTemp = ": " + TempHelper.FormatTemp((double)HardwareControl.gpuTemp);
            }

            if (HardwareControl.cpuFan is not null) cpuFan = Strings.FanSpeed + ": " + HardwareControl.cpuFan;
            if (HardwareControl.gpuFan is not null) gpuFan = Strings.FanSpeed + ": " + HardwareControl.gpuFan;
            if (HardwareControl.midFan is not null) midFan = Strings.FanSpeed + ": " + HardwareControl.midFan;

            string trayTip = "CPU" + cpuTemp + " " + cpuFan;
            if (gpuTemp.Length > 0) trayTip += "\nGPU" + gpuTemp + " " + gpuFan;
            if (battery.Length > 0) trayTip += "\n" + battery;
            
            if (Program.settingsForm.IsHandleCreated)
                Program.settingsForm.BeginInvoke(delegate
                {
                    labelCPUFan.Text = "CPU" + cpuTemp + "  " + cpuFan;
                    labelGPUFan.Text = "GPU" + gpuTemp + "  " + gpuFan;

                    if (HardwareControl.gpuFan is not null && AppConfig.NoGpu())
                        labelMidFan.Text = "GPU" + gpuTemp + " " + gpuFan;

                    if (HardwareControl.midFan is not null) 
                        labelMidFan.Text = "Mid " + midFan;
                    
                    labelBattery.Text = battery;
                    if (!batteryMouseOver && !batteryFullMouseOver) labelCharge.Text = charge;
                });

            if (Program.trayIcon is not null) Program.trayIcon.Text = trayTip;
        }

        public void LabelFansResult(string text)
        {
            if (fansForm != null && !fansForm.IsDisposed && fansForm.Text != "")
                fansForm.LabelFansResult(text);
        }

        public void ToggleOverlay(bool fromHotkey = false)
        {
            bool enable = !AppConfig.IsOverlay();
            if (enable && AppConfig.Is("rtss_overlay"))
            {
                Program.rtssOverlay?.Stop();
                AppConfig.Set("rtss_overlay", 0);
                buttonRtssOverlay.Activated = false;
            }
            AppConfig.Set("overlay", enable ? 1 : 0);
            Logger.WriteLine("Overlay " + (enable ? "On" : "Off") + (AppConfig.IsOverlayGameOnly() ? " (game only)" : ""));
            if (enable)
                Program.hardwareOverlay?.StartOverlay();
            else
                Program.hardwareOverlay?.StopOverlay();

            buttonOverlay.Activated = enable;

            if (fromHotkey && AppConfig.IsOverlayGameOnly())
                Program.toast.RunToast(Properties.Strings.Overlay + " " + (enable ? Properties.Strings.On : Properties.Strings.Off));

            SetContextMenu();
        }

        public void ToggleRtssOverlay(bool fromHotkey = false)
        {
            bool enable = !AppConfig.Is("rtss_overlay");
            if (enable)
            {
                if (AppConfig.IsOverlay())
                {
                    Program.hardwareOverlay?.StopOverlay();
                    AppConfig.Set("overlay", 0);
                    buttonOverlay.Activated = false;
                }

                if (Program.rtssOverlay?.Start() != true)
                {
                    MessageBox.Show(this,
                        "RivaTuner Statistics Server was not found. Install RTSS, then try again.",
                        "RTSS OSD", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    AppConfig.Set("rtss_overlay", 0);
                    buttonRtssOverlay.Activated = false;
                    SetContextMenu();
                    return;
                }
            }
            else
            {
                Program.rtssOverlay?.Stop();
            }

            AppConfig.Set("rtss_overlay", enable ? 1 : 0);
            buttonRtssOverlay.Activated = enable;
            if (fromHotkey)
                Program.toast.RunToast("RTSS OSD " + (enable ? Properties.Strings.On : Properties.Strings.Off));
            SetContextMenu();
        }

        private void ShowRtssOverlaySettings()
        {
            using var dialog = new RtssOverlaySettingsDialog();
            dialog.ShowDialog(this);
        }

        public void ToggleOverlayGameOnly()
        {
            AppConfig.Set("overlay_game_only", AppConfig.IsOverlayGameOnly() ? 0 : 1);
            if (AppConfig.IsOverlay())
            {
                Program.hardwareOverlay?.StopOverlay();
                Program.hardwareOverlay?.StartOverlay();
            }
            SetContextMenu();
        }

        public void ShowMode(int mode)
        {
            if (InvokeRequired)
                Invoke(delegate
                {
                    VisualiseMode(mode);
                });
            else
                VisualiseMode(mode);
        }

        protected void VisualiseMode(int mode)
        {
            buttonSilent.Activated = false;
            buttonBalanced.Activated = false;
            buttonTurbo.Activated = false;
            buttonFans.Activated = false;

            switch (mode)
            {
                case AsusACPI.PerformanceSilent:
                    buttonSilent.Activated = true;
                    break;
                case AsusACPI.PerformanceTurbo:
                    buttonTurbo.Activated = true;
                    break;
                case AsusACPI.PerformanceBalanced:
                    buttonBalanced.Activated = true;
                    break;
                default:
                    buttonFans.Activated = true;
                    buttonFans.BorderColor = Modes.GetBase(mode) switch
                    {
                        AsusACPI.PerformanceSilent => colorEco,
                        AsusACPI.PerformanceTurbo => colorTurbo,
                        AsusACPI.PerformanceFullSpeed => Color.Orange,
                        _ => colorStandard,
                    };
                    break;
            }

            foreach (var item in contextMenuStrip.Items)
            {
                if (item is ToolStripMenuItem menuItem && menuItem.Tag is not null)
                {
                    menuItem.Checked = ((int)menuItem.Tag == mode);
                }
            }
        }


        public void SetModeLabel(string modeText)
        {
            if (InvokeRequired)
            {
                Invoke(delegate
                {
                    labelPerf.Text = modeText;
                    panelPerformance.AccessibleName = labelPerf.Text;
                });
            }
            else
            {
                labelPerf.Text = modeText;
                panelPerformance.AccessibleName = labelPerf.Text;
            }

        }



        public void VisualizeXGM(int GPUMode = -1)
        {

            bool connected = Program.acpi.IsXGConnected();
            int activated = connected ? Program.acpi.DeviceGet(AsusACPI.GPUXG) : -1;
            Invoke(() => VisualizeXGM(connected, activated, GPUMode));
        }

        void VisualizeXGM(bool connected, int activated, int GPUMode)
        {
            buttonXGM.Enabled = buttonXGM.Visible = connected;

            if (!connected) return;

            if (GPUMode != -1)
                ButtonEnabled(buttonXGM, AppConfig.IsAMDiGPU() || GPUMode != AsusACPI.GPUModeEco);


            Logger.WriteLine("XGM Activated flag: " + activated);

            buttonXGM.Activated = activated == 1;

            if (activated == 1)
            {
                ButtonEnabled(buttonOptimized, false);
                ButtonEnabled(buttonEco, false);
                ButtonEnabled(buttonStandard, false);
                ButtonEnabled(buttonUltimate, false);
            }
            else
            {
                ButtonEnabled(buttonOptimized, true);
                ButtonEnabled(buttonEco, true);
                ButtonEnabled(buttonStandard, true);
                ButtonEnabled(buttonUltimate, true);
            }

        }

        public void VisualiseGPUButtons(bool eco = true, bool ultimate = true)
        {
            if (InvokeRequired) { Invoke(() => VisualiseGPUButtons(eco, ultimate)); return; }
            isMuxGpu = ultimate;

            if (!eco)
            {
                menuEco.Visible = buttonEco.Visible = false;
                menuOptimized.Visible = buttonOptimized.Visible = false;
                buttonStopGPU.Visible = true;
                tableGPU.ColumnCount = 3;
            }
            else
            {
                buttonStopGPU.Visible = false;
            }

            if (!ultimate)
            {
                menuUltimate.Visible = buttonUltimate.Visible = false;
                tableGPU.ColumnCount = 3;
            }
        }

        public void HideGPUModes(bool gpuExists)
        {
            isGpuSection = false;

            buttonEco.Visible = false;
            buttonStandard.Visible = false;
            buttonUltimate.Visible = false;
            buttonOptimized.Visible = false;
            buttonStopGPU.Visible = true;

            tableGPU.ColumnCount = 0;

            SetContextMenu();

            panelGPU.Visible = gpuExists;

        }


        public void LockGPUModes(string text = null)
        {
            if (InvokeRequired) { Invoke(() => LockGPUModes(text)); return; }
            if (text is null) text = Properties.Strings.GPUMode + ": " + Properties.Strings.GPUChanging + " ...";

            ButtonEnabled(buttonOptimized, false);
            ButtonEnabled(buttonEco, false);
            ButtonEnabled(buttonStandard, false);
            ButtonEnabled(buttonUltimate, false);
            ButtonEnabled(buttonXGM, false);

            labelGPU.Text = text;
        }

        public void VisualiseGPUMode(int GPUMode = -1)
        {
            if (InvokeRequired) { Invoke(() => VisualiseGPUMode(GPUMode)); return; }

            if (toolTip.GetToolTip(pictureGPU) != (GPUModeControl.gpuError ?? ""))
            {
                pictureGPU.BackgroundImage = GPUModeControl.gpuError is null ? Properties.Resources.icons8_video_card_32 : SystemIcons.Warning.ToBitmap();
                pictureGPU.Cursor = GPUModeControl.gpuError is null ? Cursors.Default : Cursors.Hand;
                toolTip.SetToolTip(pictureGPU, GPUModeControl.gpuError);
            }

            if (AppConfig.IsAlly())
            {
                tableGPU.Visible = false;
                labelGPU.Text = "GPU";
                if (Program.acpi.IsXGConnected())
                {
                    tableAMD.Controls.Add(buttonXGM, 1, 0);
                    VisualizeXGM();
                }
                VisualiseIcon();
                return;
            }

            ButtonEnabled(buttonOptimized, true);
            ButtonEnabled(buttonEco, true);
            ButtonEnabled(buttonStandard, true);
            ButtonEnabled(buttonUltimate, true);

            if (GPUMode == -1)
                GPUMode = AppConfig.Get("gpu_mode");

            bool GPUAuto = AppConfig.Is("gpu_auto");

            buttonEco.Activated = false;
            buttonStandard.Activated = false;
            buttonUltimate.Activated = false;
            buttonOptimized.Activated = false;

            switch (GPUMode)
            {
                case AsusACPI.GPUModeEco:
                    buttonOptimized.BorderColor = colorEco;
                    buttonEco.Activated = !GPUAuto;
                    buttonOptimized.Activated = GPUAuto;
                    labelGPU.Text = Properties.Strings.GPUMode + ": " + Properties.Strings.GPUModeEco;
                    panelGPU.AccessibleName = Properties.Strings.GPUMode + " - " + (GPUAuto ? Properties.Strings.Optimized : Properties.Strings.EcoMode);
                    break;
                case AsusACPI.GPUModeUltimate:
                    buttonUltimate.Activated = true;
                    labelGPU.Text = Properties.Strings.GPUMode + ": " + Properties.Strings.GPUModeUltimate;
                    panelGPU.AccessibleName = Properties.Strings.GPUMode + " - " + Properties.Strings.UltimateMode;
                    break;
                default:
                    buttonOptimized.BorderColor = colorStandard;
                    buttonStandard.Activated = !GPUAuto;
                    buttonOptimized.Activated = GPUAuto;
                    labelGPU.Text = Properties.Strings.GPUMode + ": " + (AppConfig.IsAlwaysUltimate() ? Properties.Strings.GPUModeUltimate : Properties.Strings.GPUModeStandard);
                    panelGPU.AccessibleName = Properties.Strings.GPUMode + " - " + (GPUAuto ? Properties.Strings.Optimized : Properties.Strings.StandardMode);
                    break;
            }

            VisualiseIcon();
            VisualizeXGM(GPUMode);

            if (isGpuSection)
            {
                menuEco.Checked = buttonEco.Activated;
                menuStandard.Checked = buttonStandard.Activated;
                menuUltimate.Checked = buttonUltimate.Activated;
                menuOptimized.Checked = buttonOptimized.Activated;
            }

            // UI Fix for small screeens
            if (Top < 0)
            {
                labelTipGPU.Visible = false;
                labelTipScreen.Visible = false;
                Top = 5;
            }

        }


        private (int, bool, bool)? lastIcon;
        private bool isDark = CheckSystemDarkModeStatus();

        public void VisualiseIcon(bool themeChange = false)
        {
            if (Program.trayIcon is null) return;
            if (themeChange) isDark = CheckSystemDarkModeStatus();

            int GPUMode = AppConfig.Get("gpu_mode");
            bool bw = AppConfig.IsBWIcon();

            if (lastIcon == (GPUMode, isDark, bw)) return;
            lastIcon = (GPUMode, isDark, bw);

            Icon newIcon = GPUMode switch
            {
                AsusACPI.GPUModeEco => bw ? (isDark ? Properties.Resources.light_eco : Properties.Resources.dark_eco) : Properties.Resources.eco,
                AsusACPI.GPUModeUltimate => bw ? (isDark ? Properties.Resources.light_standard : Properties.Resources.dark_standard) : Properties.Resources.ultimate,
                _ => bw ? (isDark ? Properties.Resources.light_standard : Properties.Resources.dark_standard) : Properties.Resources.standard,
            };

            Icon? oldIcon = Program.trayIcon.Icon;
            Program.trayIcon.Icon = newIcon;
            oldIcon?.Dispose();
        }

        private void PictureGPU_Click(object? sender, EventArgs e)
        {
            if (GPUModeControl.gpuError is null) return;
            GPUModeControl.CheckGpuError();
            Process.Start(new ProcessStartInfo("devmgmt.msc") { UseShellExecute = true });
        }

        private void ButtonSilent_Click(object? sender, EventArgs e)
        {
            Program.modeControl.SetPerformanceMode(AsusACPI.PerformanceSilent);
        }

        private void ButtonBalanced_Click(object? sender, EventArgs e)
        {
            Program.modeControl.SetPerformanceMode(AsusACPI.PerformanceBalanced);
        }

        private void ButtonTurbo_Click(object? sender, EventArgs e)
        {
            Program.modeControl.SetPerformanceMode(AsusACPI.PerformanceTurbo);
        }


        public void ButtonEnabled(RButton but, bool enabled)
        {
            but.Enabled = enabled;
            but.BackColor = but.Enabled ? Color.FromArgb(255, but.BackColor) : Color.FromArgb(100, but.BackColor);
        }

        public void VisualiseBatteryTitle(int limit)
        {
            labelBatteryTitle.Text = Properties.Strings.BatteryChargeLimit + ": " + limit.ToString() + "%";
        }

        public void VisualiseBattery(int limit)
        {
            if (InvokeRequired) { Invoke(() => VisualiseBattery(limit)); return; }
            VisualiseBatteryTitle(limit);
            sliderBattery.Value = limit;

            sliderBattery.AccessibleName = Properties.Strings.BatteryChargeLimit + ": " + limit.ToString() + "%";
            //sliderBattery.AccessibilityObject.Select(AccessibleSelection.TakeFocus);

            VisualiseBatteryFull();
        }

        public void VisualiseBatteryFull()
        {
            if (InvokeRequired) { Invoke(VisualiseBatteryFull); return; }
            if (BatteryControl.chargeFull)
            {
                buttonBatteryFull.BackColor = colorStandard;
                buttonBatteryFull.ForeColor = SystemColors.ControlLightLight;
                buttonBatteryFull.AccessibleName = Properties.Strings.BatteryChargeLimit + "100% on";
            }
            else
            {
                buttonBatteryFull.BackColor = buttonSecond;
                buttonBatteryFull.ForeColor = SystemColors.ControlDark;
                buttonBatteryFull.AccessibleName = Properties.Strings.BatteryChargeLimit + "100% off";
            }

        }


        public void UpdateKeyboardLabel()
        {
            labelKeyboard.Text = Properties.Strings.LaptopKeyboard + (PeripheralsProvider.IsAuraSync ? " +" : "");
        }

        public void VisualizePeripherals()
        {
            if (!PeripheralsProvider.IsAnyPeripheralConnect())
            {
                panelPeripherals.Visible = false;
                return;
            }

            Button[] buttons = new Button[] { buttonPeripheral1, buttonPeripheral2, buttonPeripheral3 };

            //we only support 4 devces for now. Who has more than 4 mice connected to the same PC anyways....
            List<IPeripheral> lp = PeripheralsProvider.AllPeripherals();

            for (int i = 0; i < lp.Count && i < buttons.Length; ++i)
            {
                IPeripheral m = lp.ElementAt(i);
                Button b = buttons[i];

                string id = m.GetDisplayName();
                bool ready = m.IsDeviceReady;
                bool hasBat = m.HasBattery();
                bool charging = ready && hasBat && m.Charging;
                int level = (ready && hasBat) ? Math.Min(5, (m.Battery + 10) / 20) : -1;
                bool showPercent = AppConfig.Is("mouse_battery") && ready && hasBat;
                int cacheBattery = showPercent ? m.Battery : -1;
                var state = (id, ready, charging, level, cacheBattery, b.ForeColor.ToArgb());

                if (b.Tag is ValueTuple<string, bool, bool, int, int, int> prev && prev.Equals(state) && b.Visible)
                    continue;

                b.Text = showPercent ? id + "\n" + m.Battery + "%" : id;

                Image? baseIcon = m.DeviceType() switch
                {
                    PeripheralType.Mouse => Properties.Resources.icons8_maus_48,
                    PeripheralType.Keyboard => Properties.Resources.icons8_keyboard_48,
                    _ => null,
                };

                if (baseIcon is not null)
                {
                    int ih = baseIcon.Height;
                    // icon PNG may be wider than tall (baked-in right text padding); badge/bars anchor to the glyph square
                    int iw = Math.Min(baseIcon.Width, ih);
                    Image composed = ControlHelper.TintImage(baseIcon, b.ForeColor);
                    if (!ready)
                    {
                        composed = ControlHelper.OverlayBadge(composed, Properties.Resources.icons8_cancel_48, RForm.colorTurbo, iconWidth: iw, iconHeight: ih);
                    }
                    else if (hasBat)
                    {
                        if (charging)
                            composed = ControlHelper.OverlayBadge(composed, Properties.Resources.icons8_flash_48, RForm.colorEco, iconWidth: iw, iconHeight: ih);

                        Color barColor = level <= 1 ? colorTurbo
                                       : level <= 3 ? colorStandard
                                       : colorEco;
                        composed = ControlHelper.OverlayChargeBars(composed, level, 5, barColor, iconWidth: iw, iconHeight: ih);
                    }

                    b.Image = ControlHelper.ResizeImage(composed, ControlHelper.Scale);
                }

                b.Tag = state;
                b.Visible = true;
            }

            for (int i = lp.Count; i < buttons.Length; ++i)
            {
                buttons[i].Visible = false;
            }

            panelPeripherals.Visible = true;
        }

        private void ButtonPeripheral_MouseEnter(object? sender, EventArgs e)
        {
            int index = 0;
            if (sender == buttonPeripheral2) index = 1;
            if (sender == buttonPeripheral3) index = 2;
            IPeripheral iph = PeripheralsProvider.AllPeripherals().ElementAt(index);


            if (iph is null)
            {
                return;
            }

            if (!iph.IsDeviceReady)
            {
                //Refresh battery on hover if the device is marked as "Not Ready"
                iph.ReadBattery();
            }
        }

        private void ButtonPeripheral_Click(object? sender, EventArgs e)
        {
            if (mouseSettings is not null)
            {
                mouseSettings.Close();
                return;
            }

            if (keyboardSettings is not null)
            {
                keyboardSettings.Close();
                return;
            }

            int index = 0;
            if (sender == buttonPeripheral2) index = 1;
            if (sender == buttonPeripheral3) index = 2;

            IPeripheral iph = PeripheralsProvider.AllPeripherals().ElementAt(index);

            if (iph is null)
            {
                //Can only happen when the user hits the button in the exact moment a device is disconnected.
                return;
            }

            if (iph.DeviceType() == PeripheralType.Mouse)
            {
                AsusMouse? am = iph as AsusMouse;
                if (am is null || !am.IsDeviceReady)
                {
                    //Should not happen if all device classes are implemented correctly. But better safe than sorry.
                    return;
                }
                mouseSettings = new AsusMouseSettings(am);
                AddOwnedForm(mouseSettings);
                mouseSettings.FormClosed += MouseSettings_FormClosed;
                mouseSettings.Disposed += MouseSettings_Disposed;
                if (!mouseSettings.IsDisposed)
                {
                    PrepareAuxiliaryWindow(mouseSettings);
                    mouseSettings.Show();
                    mouseSettings.Activate();
                }
                else
                {
                    mouseSettings = null;
                }

            }

            if (iph.DeviceType() == PeripheralType.Keyboard)
            {
                AsusKeyboard? kb = iph as AsusKeyboard;
                if (kb is null || !kb.IsDeviceReady)
                {
                    return;
                }
                ShowKeyboardSettings(kb);
            }
        }

        private void ShowKeyboardSettings(AsusKeyboard kb)
        {
            AsusKeyboardSettings.RequestReopen = ShowKeyboardSettings;
            keyboardSettings = new AsusKeyboardSettings(kb);
            AddOwnedForm(keyboardSettings);
            keyboardSettings.FormClosed += KeyboardSettings_FormClosed;
            keyboardSettings.Disposed += KeyboardSettings_Disposed;
            if (!keyboardSettings.IsDisposed)
            {
                PrepareAuxiliaryWindow(keyboardSettings);
                keyboardSettings.Show();
                keyboardSettings.Activate();
            }
            else
            {
                keyboardSettings = null;
            }
        }

        private void KeyboardSettings_Disposed(object? sender, EventArgs e)
        {
            keyboardSettings = null;
        }

        private void KeyboardSettings_FormClosed(object? sender, FormClosedEventArgs e)
        {
            keyboardSettings = null;
        }

        private void MouseSettings_Disposed(object? sender, EventArgs e)
        {
            mouseSettings = null;
        }

        private void MouseSettings_FormClosed(object? sender, FormClosedEventArgs e)
        {
            mouseSettings = null;
        }

        public void VisualiseAudio(double level)
        {
            if (InvokeRequired) { Invoke(() => VisualiseAudio(level)); return; }
            int filledSquares = (int)Math.Round(level/2);
            string squares = new string('|', filledSquares);
            labelMatrix.Text = $"Slash Lighting: {squares}";
        }

        public void VisualiseFnLock()
        {

            if (AppConfig.Is("fn_lock"))
            {
                buttonFnLock.BackColor = colorStandard;
                buttonFnLock.ForeColor = SystemColors.ControlLightLight;
                buttonFnLock.AccessibleName = "Fn-Lock on";
            }
            else
            {
                buttonFnLock.BackColor = buttonSecond;
                buttonFnLock.ForeColor = SystemColors.ControlDark;
                buttonFnLock.AccessibleName = "Fn-Lock off";
            }
        }


        private void ButtonFnLock_Click(object? sender, EventArgs e)
        {
            InputDispatcher.ToggleFnLock();
        }

        private void SliderScreenBrightness_ValueChanged(object? sender, EventArgs e)
        {
            Display.ScreenBrightness.Set(sliderScreenBrightness.Value);
            sliderScreenBrightness.AccessibleName = Properties.Strings.LaptopScreen + " " + Properties.Strings.Brightness + ": " + sliderScreenBrightness.Value.ToString() + "%";
        }

        private void SliderScreenBrightness_KeyUp(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Up || e.KeyCode == Keys.Left || e.KeyCode == Keys.Down)
            {
                Display.ScreenBrightness.Set(sliderScreenBrightness.Value);
            }
        }

        private void SliderScreenBrightness_MouseUp(object? sender, MouseEventArgs e)
        {
            Display.ScreenBrightness.Set(sliderScreenBrightness.Value);
        }

    }


}

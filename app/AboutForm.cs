using GHelper.UI;

namespace GHelper
{
    public partial class AboutForm : RForm
    {
        public AboutForm()
        {
            InitializeComponent();
            InitTheme(true);

            buttonClose.Click += (s, e) => Close();

            // Set background colors dynamically
            textDocs.BackColor = BackColor;
            textDocs.ForeColor = ForeColor;

            PopulateDocumentation();
        }

        private void PopulateDocumentation()
        {
            textDocs.Text = @"G-Helper Documentation

Welcome to G-Helper! This app provides lightweight, efficient control for your ASUS laptop or ROG Ally.

--- MODES ---
• Silent: Prioritizes quiet fan operation. Limits CPU/GPU power slightly.
• Balanced: Standard performance mode for general use and casual gaming.
• Turbo: Maximum performance. Fans run aggressively to keep components cool.
• Custom: Define your own fan curves and power limits via the 'Fans + Power' menu.

--- GPU MODES ---
• Eco: Disables the dedicated GPU entirely for maximum battery life.
• Standard: Optimus mode. Switches between integrated and dedicated GPU automatically.
• Ultimate: MUX Switch mode. Directly connects the display to the dedicated GPU for max performance.
• Optimized: Automatically disables the dGPU when on battery, enables it when plugged in.

--- CUSTOM FAN CURVES & TDP ---
By clicking 'Fans + Power' you can adjust the TDP (Total Design Power) limits for your CPU.
• SPL: Sustained Power Limit
• SPPT: Slow Package Power Tracking (short burst)
• FPPT: Fast Package Power Tracking (instant burst)
You can also drag points on the graph to set custom fan speeds for CPU and GPU at various temperatures.

--- ROG ALLY KEYBINDINGS ---
G-Helper allows full controller customization.
Open the 'Controller' settings to map buttons, configure joysticks, deadzones, and triggers.
• M1 / M2: Rear paddles. Can be mapped as primary or secondary (chord) buttons.
• Holding M4 (ROG button) by default activates chords (e.g. ROG + D-Pad for TDP/Clocks).
• Pressing ROG + Menu toggles the OSD mapping preview.

--- HOTKEYS ---
• M3 / M4 buttons on laptops can be assigned to custom actions.
• FN + F5 usually toggles Performance Modes.
• FN + C toggles Aura effects.

For more details and FAQs, please visit the G-Helper GitHub repository.
";
        }
    }
}

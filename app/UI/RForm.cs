using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace GHelper.UI
{
    public class RForm : Form
    {

        public static Color colorEco = Color.FromArgb(255, 6, 180, 138);
        public static Color colorStandard = Color.FromArgb(255, 58, 174, 239);
        public static Color colorTurbo = Color.FromArgb(255, 255, 32, 32);
        public static Color colorCustom = Color.FromArgb(255, 255, 128, 0);
        public static Color colorGray = Color.FromArgb(255, 168, 168, 168);


        public static Color buttonMain;
        public static Color buttonSecond;

        public static Color formBack;
        public static Color foreMain;
        public static Color borderMain;
        public static Color borderSecond;
        public static Color chartMain;
        public static Color chartGrid;

        public static bool flatTheme = false;

        [DllImport("UXTheme.dll", SetLastError = true, EntryPoint = "#138")]
        public static extern bool CheckSystemDarkModeStatus();

        [DllImport("UXTheme.dll", SetLastError = true, EntryPoint = "#135")]
        private static extern int SetPreferredAppMode(int preferredAppMode);

        [DllImport("UXTheme.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(nint hWnd, string pszSubAppName, string? pszSubIdList);

        [DllImport("DwmApi")] //System.Runtime.InteropServices
        private static extern int DwmSetWindowAttribute(nint hwnd, int attr, int[] attrValue, int attrSize);

        protected void EnableGlassBackdrop()
        {
            if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)) return;
            try
            {
                // DWMSBT_TRANSIENTWINDOW enables the Windows 11 acrylic system backdrop.
                DwmSetWindowAttribute(Handle, 38, new[] { 3 }, sizeof(int));
            }
            catch
            {
                // The custom glass surfaces remain available when DWM backdrop support is absent.
            }
        }

        public bool darkTheme = false;
        private bool themeInitialized = false;
        protected override CreateParams CreateParams
        {
            get
            {
                var parms = base.CreateParams;
                parms.Style &= ~0x02000000;  // Turn off WS_CLIPCHILDREN
                parms.ClassStyle &= ~0x00020000;
                return parms;
            }
        }
        public static void InitColors(bool darkTheme)
        {
            flatTheme = AppConfig.GetString("theme")?.ToLower() == "flat";

            if (darkTheme)
            {
                // Acrylic dark theme matching screenshot (Must be solid colors for WinForms Controls)
                buttonMain = Color.FromArgb(255, 30, 33, 41);
                buttonSecond = Color.FromArgb(255, 20, 22, 28);

                // WinForms does not support translucent background colors! It will crash with ArgumentException.
                // We must use a solid color and rely on Form.Opacity or DWM APIs for the glass effect.
                formBack = Color.FromArgb(255, 10, 10, 12); 
                foreMain = Color.White;
                borderMain = Color.FromArgb(255, 60, 60, 60);
                borderSecond = Color.FromArgb(255, 45, 45, 45);

                chartMain = Color.Black;
                chartGrid = Color.FromArgb(255, 50, 50, 50);
            }
            else
            {
                // Vibrant light theme (warm pastel/sunset)
                buttonMain = Color.FromArgb(255, 255, 245, 250);
                buttonSecond = Color.FromArgb(255, 255, 235, 240);

                formBack = Color.FromArgb(255, 252, 248, 255);
                foreMain = Color.FromArgb(255, 60, 40, 80);
                borderMain = Color.FromArgb(255, 255, 200, 220);
                borderSecond = Color.FromArgb(255, 255, 180, 200);

                chartMain = Color.FromArgb(255, 255, 250, 255);
                chartGrid = Color.FromArgb(255, 255, 190, 210);
            }
        }

        private static bool IsDarkTheme()
        {
            string? uiMode = AppConfig.GetString("ui_mode");

            if (uiMode is not null && uiMode.ToLower() == "dark")
            {
                return true;
            }

            if (uiMode is not null && uiMode.ToLower() == "light")
            {
                return false;
            }

            if (uiMode is not null && uiMode.ToLower() == "windows")
            {
                return CheckSystemDarkModeStatus();
            }

            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var registryValueObject = key?.GetValue("AppsUseLightTheme");

            if (registryValueObject == null) return false;
            return (int)registryValueObject <= 0;
        }

        public bool InitTheme(bool setDPI = false)
        {
            bool newDarkTheme = IsDarkTheme();
            bool changed = darkTheme != newDarkTheme;
            bool firstInit = !themeInitialized;
            darkTheme = newDarkTheme;
            themeInitialized = true;

            InitColors(darkTheme);

            if (setDPI)
                ControlHelper.Resize(this);

            if (changed || firstInit)
            {
                DwmSetWindowAttribute(Handle, 20, new[] { darkTheme ? 1 : 0 }, 4);
                SetPreferredAppMode(darkTheme ? 1 : 0); 
                SetWindowTheme(Handle, darkTheme ? "DarkMode_Explorer" : "Explorer", null);
                ControlHelper.Adjust(this, changed);
                this.Invalidate();
            }


            return changed;

        }

    }
}

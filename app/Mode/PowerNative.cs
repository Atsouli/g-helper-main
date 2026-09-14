using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace GHelper.Mode
{
    internal class PowerNative
    {
        [DllImport("PowrProf.dll", CharSet = CharSet.Unicode)]
        static extern UInt32 PowerWriteDCValueIndex(IntPtr RootPowerKey,
            [MarshalAs(UnmanagedType.LPStruct)] Guid SchemeGuid,
            [MarshalAs(UnmanagedType.LPStruct)] Guid SubGroupOfPowerSettingsGuid,
            [MarshalAs(UnmanagedType.LPStruct)] Guid PowerSettingGuid,
            int AcValueIndex);

        [DllImport("PowrProf.dll", CharSet = CharSet.Unicode)]
        static extern UInt32 PowerWriteACValueIndex(IntPtr RootPowerKey,
            [MarshalAs(UnmanagedType.LPStruct)] Guid SchemeGuid,
            [MarshalAs(UnmanagedType.LPStruct)] Guid SubGroupOfPowerSettingsGuid,
            [MarshalAs(UnmanagedType.LPStruct)] Guid PowerSettingGuid,
            int AcValueIndex);

        [DllImport("PowrProf.dll", CharSet = CharSet.Unicode)]
        static extern UInt32 PowerReadACValueIndex(IntPtr RootPowerKey,
            [MarshalAs(UnmanagedType.LPStruct)] Guid SchemeGuid,
            [MarshalAs(UnmanagedType.LPStruct)] Guid SubGroupOfPowerSettingsGuid,
            [MarshalAs(UnmanagedType.LPStruct)] Guid PowerSettingGuid,
            out uint AcValueIndex
            );

        [DllImport("PowrProf.dll", CharSet = CharSet.Unicode)]
        static extern UInt32 PowerReadDCValueIndex(IntPtr RootPowerKey,
            [MarshalAs(UnmanagedType.LPStruct)] Guid SchemeGuid,
            [MarshalAs(UnmanagedType.LPStruct)] Guid SubGroupOfPowerSettingsGuid,
            [MarshalAs(UnmanagedType.LPStruct)] Guid PowerSettingGuid,
            out uint AcValueIndex
            );


        [DllImport("powrprof.dll")]
        static extern uint PowerReadACValue(
            IntPtr RootPowerKey,
            Guid SchemeGuid,
            Guid SubGroupOfPowerSettingGuid,
            Guid PowerSettingGuid,
            ref int Type,
            ref IntPtr Buffer,
            ref uint BufferSize
            );


        [DllImport("PowrProf.dll", CharSet = CharSet.Unicode)]
        static extern UInt32 PowerSetActiveScheme(IntPtr RootPowerKey,
            [MarshalAs(UnmanagedType.LPStruct)] Guid SchemeGuid);

        [DllImport("PowrProf.dll", CharSet = CharSet.Unicode)]
        static extern UInt32 PowerGetActiveScheme(IntPtr UserPowerKey, out IntPtr ActivePolicyGuid);

        [DllImport("kernel32.dll")]
        static extern IntPtr LocalFree(IntPtr hMem);

        static readonly Guid GUID_CPU = new Guid("54533251-82be-4824-96c1-47b60b740d00");
        static readonly Guid GUID_BOOST = new Guid("be337238-0d82-4146-a960-4f3749d470c7");
        // PROCFREQMAX: maximum processor performance frequency in MHz.  A value of 0
        // restores Windows' automatic/default frequency management.
        static readonly Guid GUID_MAX_FREQUENCY = new Guid("75b0ae3f-bce0-45a7-8c89-c9611c25e100");
        // PROCFREQMAX1: the same limit for processor power-efficiency class 1.
        static readonly Guid GUID_MAX_FREQUENCY_1 = new Guid("75b0ae3f-bce0-45a7-8c89-c9611c25e101");
        // PROCTHROTTLEMAX must remain at 100% or it takes precedence over a MHz cap.
        static readonly Guid GUID_MAX_PROCESSOR_STATE = new Guid("bc5038f7-23e0-4960-96da-33abaf5935ec");
        // CPPC2 autonomous mode lets the processor choose performance without Windows
        // sending desired levels. Disable it while an explicit MHz cap is active.
        static readonly Guid GUID_PERF_AUTONOMOUS_MODE = new Guid("8baa4a8a-14c6-4451-8e8b-14bdbd197537");

        private static Guid GUID_SLEEP_SUBGROUP = new Guid("238c9fa8-0aad-41ed-83f4-97be242c8f20");
        private static Guid GUID_HIBERNATEIDLE = new Guid("9d7815a6-7ee4-497e-8888-515a05f02364");

        private static Guid GUID_SYSTEM_BUTTON_SUBGROUP = new Guid("4f971e89-eebd-4455-a8de-9e59040e7347");
        private static Guid GUID_LIDACTION = new Guid("5CA83367-6E45-459F-A27B-476B1D01C936");

        private static Guid GUID_SUB_PCIEXPRESS = new Guid("501a4d13-42af-4429-9fd1-a8218c268e20");
        private static Guid GUID_PCI_EXPRESS_ASPM = new Guid("ee12f906-d277-404b-b6da-e5fa1a576df5");

        private static Guid GUID_SUB_NONE = new Guid("fea3413e-7e05-4911-9a71-700331f1c294");
        private static Guid GUID_CONNECTIVITY_IN_STANDBY = new Guid("f15576e8-98b7-4186-b944-eafa664402d9");

        [DllImportAttribute("powrprof.dll", EntryPoint = "PowerGetActualOverlayScheme")]
        public static extern uint PowerGetActualOverlayScheme(out Guid ActualOverlayGuid);

        [DllImportAttribute("powrprof.dll", EntryPoint = "PowerGetEffectiveOverlayScheme")]
        public static extern uint PowerGetEffectiveOverlayScheme(out Guid EffectiveOverlayGuid);

        [DllImportAttribute("powrprof.dll", EntryPoint = "PowerSetActiveOverlayScheme")]
        public static extern uint PowerSetActiveOverlayScheme(Guid OverlaySchemeGuid);

        const string POWER_SILENT = "961cc777-2547-4f9d-8174-7d86181b8a7a";
        const string POWER_BALANCED = "00000000-0000-0000-0000-000000000000";
        const string POWER_TURBO = "ded574b5-45a0-4f42-8737-46345c09c238";

        const string PLAN_BALANCED = "381b4222-f694-41f0-9685-ff5bb260df2e";
        const string PLAN_HIGH_PERFORMANCE = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";

        static List<string> overlays = new() {
                POWER_BALANCED,
                POWER_TURBO,
                POWER_SILENT,
            };

        public static Dictionary<string, string> powerModes = new Dictionary<string, string>
            {
                { POWER_SILENT, "Best Power Efficiency" },
                { POWER_BALANCED, "Balanced" },
                { POWER_TURBO, "Best Performance" },
                { PLAN_HIGH_PERFORMANCE, "High Performance Plan"},
            };
        static Guid GetActiveScheme()
        {
            uint status = PowerGetActiveScheme(IntPtr.Zero, out IntPtr activeSchemePointer);
            if (status != 0 || activeSchemePointer == IntPtr.Zero)
                throw new InvalidOperationException($"Unable to read active power scheme ({status})");

            try
            {
                return Marshal.PtrToStructure<Guid>(activeSchemePointer);
            }
            finally
            {
                LocalFree(activeSchemePointer);
            }
        }

        public static int GetCPUBoost()
        {
            uint AcValueIndex;
            Guid activeSchemeGuid = GetActiveScheme();

            UInt32 value = PowerReadACValueIndex(IntPtr.Zero,
                 activeSchemeGuid,
                 GUID_CPU,
                 GUID_BOOST, out AcValueIndex);

            return (int)AcValueIndex;

        }

        public static void SetCPUBoost(int boost = 0)
        {
            Guid activeSchemeGuid = GetActiveScheme();

            if (boost == GetCPUBoost()) return;

            var hrAC = PowerWriteACValueIndex(
                 IntPtr.Zero,
                 activeSchemeGuid,
                 GUID_CPU,
                 GUID_BOOST,
                 boost);

            PowerSetActiveScheme(IntPtr.Zero, activeSchemeGuid);

            var hrDC = PowerWriteDCValueIndex(
                 IntPtr.Zero,
                 activeSchemeGuid,
                 GUID_CPU,
                 GUID_BOOST,
                 boost);

            PowerSetActiveScheme(IntPtr.Zero, activeSchemeGuid);

            Logger.WriteLine("Boost " + boost);
        }

        public static int GetMaximumProcessorFrequency(bool efficiencyClass1 = false, bool dc = false)
        {
            Guid activeSchemeGuid = GetActiveScheme();
            Guid setting = efficiencyClass1 ? GUID_MAX_FREQUENCY_1 : GUID_MAX_FREQUENCY;
            uint result = dc
                ? PowerReadDCValueIndex(IntPtr.Zero, activeSchemeGuid, GUID_CPU, setting, out uint value)
                : PowerReadACValueIndex(IntPtr.Zero, activeSchemeGuid, GUID_CPU, setting, out value);
            return result == 0 && value <= int.MaxValue ? (int)value : -1;
        }

        public static bool SetMaximumProcessorFrequency(int mhz)
        {
            if (mhz < 0) return false;

            Guid activeSchemeGuid = GetActiveScheme();
            uint ac = PowerWriteACValueIndex(IntPtr.Zero, activeSchemeGuid, GUID_CPU,
                GUID_MAX_FREQUENCY, mhz);
            uint dc = PowerWriteDCValueIndex(IntPtr.Zero, activeSchemeGuid, GUID_CPU,
                GUID_MAX_FREQUENCY, mhz);
            uint ac1 = PowerWriteACValueIndex(IntPtr.Zero, activeSchemeGuid, GUID_CPU,
                GUID_MAX_FREQUENCY_1, mhz);
            uint dc1 = PowerWriteDCValueIndex(IntPtr.Zero, activeSchemeGuid, GUID_CPU,
                GUID_MAX_FREQUENCY_1, mhz);
            uint maxStateAc = PowerWriteACValueIndex(IntPtr.Zero, activeSchemeGuid, GUID_CPU,
                GUID_MAX_PROCESSOR_STATE, 100);
            uint maxStateDc = PowerWriteDCValueIndex(IntPtr.Zero, activeSchemeGuid, GUID_CPU,
                GUID_MAX_PROCESSOR_STATE, 100);
            int autonomousMode = mhz == 0 ? 1 : 0;
            uint autonomousAc = PowerWriteACValueIndex(IntPtr.Zero, activeSchemeGuid, GUID_CPU,
                GUID_PERF_AUTONOMOUS_MODE, autonomousMode);
            uint autonomousDc = PowerWriteDCValueIndex(IntPtr.Zero, activeSchemeGuid, GUID_CPU,
                GUID_PERF_AUTONOMOUS_MODE, autonomousMode);
            uint apply = PowerSetActiveScheme(IntPtr.Zero, activeSchemeGuid);

            int readbackAc = GetMaximumProcessorFrequency();
            int readbackDc = GetMaximumProcessorFrequency(dc: true);
            int readbackAc1 = GetMaximumProcessorFrequency(true);
            int readbackDc1 = GetMaximumProcessorFrequency(true, true);

            // The Z1 Extreme has one homogeneous CPU core class. Some Windows builds do
            // not expose the optional class-1 setting, so only the primary AC/DC values
            // determine whether the limit was applied. Class 1 remains best effort.
            bool accepted = ac == 0 && dc == 0 && maxStateAc == 0 && maxStateDc == 0 &&
                autonomousAc == 0 && autonomousDc == 0 && apply == 0 &&
                readbackAc == mhz && readbackDc == mhz;

            Logger.WriteLine($"CPU max frequency: {(mhz == 0 ? "Automatic" : mhz + " MHz")} " +
                $"(AC:{ac}, DC:{dc}, AC1:{ac1}, DC1:{dc1}, MaxState:{maxStateAc}/{maxStateDc}, " +
                $"Autonomous:{autonomousAc}/{autonomousDc}={autonomousMode}, Apply:{apply}, " +
                $"Read AC/DC:{readbackAc}/{readbackDc}, Read AC1/DC1:{readbackAc1}/{readbackDc1})");
            return accepted;
        }

        public static string GetPowerMode()
        {
            if (GetActiveScheme().ToString() == PLAN_HIGH_PERFORMANCE) return PLAN_HIGH_PERFORMANCE;
            PowerGetEffectiveOverlayScheme(out Guid activeScheme);
            return activeScheme.ToString();
        }

        public static void SetPowerMode(string scheme)
        {

            if (scheme == PLAN_HIGH_PERFORMANCE)
            {
                SetPowerPlan(scheme);
                return;
            }
            else
            {
                // Power plan from config or defaulting to balanced
                string plan = AppConfig.GetModeString("scheme");
                if (Program.currentSource == Program.PowerSource.USBC && AppConfig.GetModeString("scheme_usbc") is string usbc) plan = usbc;
                SetPowerPlan(plan);            
            }

            if (!overlays.Contains(scheme)) return;

            Guid guidScheme = new Guid(scheme);

            uint status = PowerGetEffectiveOverlayScheme(out Guid activeScheme);

            if (GetBatterySaverStatus())
            {
                Logger.WriteLine("Battery Saver detected");
                return;
            }

            if (status != 0 || activeScheme != guidScheme)
            {
                status = PowerSetActiveOverlayScheme(guidScheme);
                Logger.WriteLine("Power Mode " + activeScheme + " -> " + scheme + ":" + (status == 0 ? "OK" : status));
            }

        }

        public static void SetPowerPlan(string scheme)
        {
            // Skipping power modes
            if (overlays.Contains(scheme)) return;

            if (scheme is null) scheme = PLAN_BALANCED;
            var activeScheme = GetActiveScheme().ToString();
            if (activeScheme == scheme) return;

            uint status = PowerSetActiveScheme(IntPtr.Zero, new Guid(scheme));
            Logger.WriteLine($"Power Plan {activeScheme} -> {scheme} :" + (status == 0 ? "OK" : status));
        }

        public static string GetDefaultPowerMode(int mode)
        {
            switch (mode)
            {
                case 1: // turbo
                    return POWER_TURBO;
                case 2: //silent
                    return POWER_SILENT;
                case 3:
                    return PLAN_HIGH_PERFORMANCE;
                default: // balanced
                    return POWER_BALANCED;
            }
        }

        public static void SetPowerMode(int mode)
        {
            SetPowerMode(GetDefaultPowerMode(mode));
        }

        public static int GetASPM()
        {
            Guid activeSchemeGuid = GetActiveScheme();
            uint activeIndex;

            PowerReadACValueIndex(IntPtr.Zero,
                    activeSchemeGuid,
                    GUID_SUB_PCIEXPRESS,
                    GUID_PCI_EXPRESS_ASPM, out activeIndex);

            return (int)activeIndex;
        }

        public static void SetASPM(int status = 0)
        {
            Guid activeSchemeGuid = GetActiveScheme();
            var currentASPM = GetASPM();
            if (currentASPM == status) return;

            var hrAC = PowerWriteACValueIndex(
                IntPtr.Zero,
                activeSchemeGuid,
                GUID_SUB_PCIEXPRESS,
                GUID_PCI_EXPRESS_ASPM,
                status);

            PowerSetActiveScheme(IntPtr.Zero, activeSchemeGuid);
            Logger.WriteLine($"Changed AC ASPM {currentASPM} -> {status}");
        }

        public static void SetBalancedASPM(int status = 0)
        {
            if (GetActiveScheme().ToString() != PLAN_BALANCED) return;
            SetASPM(status);
        }

        public static void SetConnectivityInStandby(int ac = 0, int dc = 0)
        {
            Guid activeSchemeGuid = GetActiveScheme();

            using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Control\Power\User\PowerSchemes\{activeSchemeGuid}\{GUID_CONNECTIVITY_IN_STANDBY}");
            if (key != null && (int?)key.GetValue("ACSettingIndex") == ac && (int?)key.GetValue("DCSettingIndex") == dc) return;

            var hrAC = PowerWriteACValueIndex(
                IntPtr.Zero,
                activeSchemeGuid,
                GUID_SUB_NONE,
                GUID_CONNECTIVITY_IN_STANDBY,
                ac);

            var hrDC = PowerWriteDCValueIndex(
                IntPtr.Zero,
                activeSchemeGuid,
                GUID_SUB_NONE,
                GUID_CONNECTIVITY_IN_STANDBY,
                dc);

            PowerSetActiveScheme(IntPtr.Zero, activeSchemeGuid);
            Logger.WriteLine($"Connectivity in Standby {ac}/{dc}: " + (hrAC == 0 && hrDC == 0 ? "OK" : $"{hrAC}/{hrDC}"));
        }

        public static int GetLidAction(bool ac)
        {
            Guid activeSchemeGuid = GetActiveScheme();

            uint activeIndex;
            if (ac)
                PowerReadACValueIndex(IntPtr.Zero,
                     activeSchemeGuid,
                     GUID_SYSTEM_BUTTON_SUBGROUP,
                     GUID_LIDACTION, out activeIndex);

            else
                PowerReadDCValueIndex(IntPtr.Zero,
                    activeSchemeGuid,
                    GUID_SYSTEM_BUTTON_SUBGROUP,
                    GUID_LIDACTION, out activeIndex);


            return (int)activeIndex;
        }


        public static void SetLidAction(int action, bool acOnly = false)
        {
            /**
             * 1: Do nothing
             * 2: Seelp
             * 3: Hibernate
             * 4: Shutdown
             */

            Guid activeSchemeGuid = GetActiveScheme();

            var hrAC = PowerWriteACValueIndex(
                IntPtr.Zero,
                activeSchemeGuid,
                GUID_SYSTEM_BUTTON_SUBGROUP,
                GUID_LIDACTION,
                action);

            PowerSetActiveScheme(IntPtr.Zero, activeSchemeGuid);

            if (!acOnly)
            {
                var hrDC = PowerWriteDCValueIndex(
                  IntPtr.Zero,
                  activeSchemeGuid,
                  GUID_SYSTEM_BUTTON_SUBGROUP,
                  GUID_LIDACTION,
                  action);

                PowerSetActiveScheme(IntPtr.Zero, activeSchemeGuid);
            }

            Logger.WriteLine("Changed Lid Action to " + action);
        }

        public static int GetHibernateAfter()
        {
            Guid activeSchemeGuid = GetActiveScheme();
            uint seconds;
            PowerReadDCValueIndex(IntPtr.Zero,
                    activeSchemeGuid,
                    GUID_SLEEP_SUBGROUP,
                    GUID_HIBERNATEIDLE, out seconds);

            Logger.WriteLine("Hibernate after " + seconds);
            return ((int)seconds / 60);
        }


        public static void SetHibernateAfter(int minutes)
        {
            int seconds = minutes * 60;

            Guid activeSchemeGuid = GetActiveScheme();
            var hrAC = PowerWriteDCValueIndex(
                IntPtr.Zero,
                activeSchemeGuid,
                GUID_SLEEP_SUBGROUP,
                GUID_HIBERNATEIDLE,
                seconds);

            PowerSetActiveScheme(IntPtr.Zero, activeSchemeGuid);

            Logger.WriteLine("Setting Hibernate after " + seconds + ": " + (hrAC == 0 ? "OK" : hrAC));
        }

        [DllImport("Kernel32")]
        private static extern bool GetSystemPowerStatus(SystemPowerStatus sps);
        public enum ACLineStatus : byte
        {
            Offline = 0, Online = 1, Unknown = 255
        }

        public enum BatteryFlag : byte
        {
            High = 1,
            Low = 2,
            Critical = 4,
            Charging = 8,
            NoSystemBattery = 128,
            Unknown = 255
        }

        // Fields must mirror their unmanaged counterparts, in order
        [StructLayout(LayoutKind.Sequential)]
        public class SystemPowerStatus
        {
            public ACLineStatus ACLineStatus;
            public BatteryFlag BatteryFlag;
            public Byte BatteryLifePercent;
            public Byte SystemStatusFlag;
            public Int32 BatteryLifeTime;
            public Int32 BatteryFullLifeTime;
        }

        public static bool GetBatterySaverStatus()
        {
            try
            {
                var status = Registry.GetValue(@"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\Power", "EnergySaverState", null);
                if (status == null)
                {
                    SystemPowerStatus sps = new SystemPowerStatus();
                    GetSystemPowerStatus(sps);
                    return (sps.SystemStatusFlag > 0);
                }
                return (int)status == 1;
            }
            catch (Exception e)
            {
                Logger.WriteLine("Can't check EnergySaverState" + e.Message);
                return false;
            }
        }


    }
}

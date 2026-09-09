using Microsoft.Win32;
using System.Diagnostics;
using System.Globalization;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;

namespace GHelper.Overlay
{
    /// <summary>Publishes G-Helper sensor data to RivaTuner Statistics Server's OSD slot.</summary>
    public sealed class RtssOverlay : IDisposable
    {
        private const string MapName = "RTSSSharedMemoryV2";
        private const string OwnerName = "GHelper";
        private const uint Signature = 0x52545353; // MSVC multi-character constant 'RTSS'
        private const uint MinimumVersion = 0x00020000;
        private const uint ExtendedTextVersion = 0x00020007;
        private const uint FormatTagsVersion = 0x0002000B;
        private const uint EmbeddedObjectsVersion = 0x0002000C;
        private const uint WriteLockVersion = 0x0002000E;
        private const int OwnerOffset = 256;
        private const int ExtendedTextOffset = 512;
        private const int EmbeddedBufferOffset = 256 + 256 + 4096;
        private const uint GraphSignature = 0x47523030; // MSVC multi-character constant 'GR00'
        private const uint GraphFramerate = 0x00000002;
        private const int GraphObjectSize = 36;
        private const string FpsGraphTag = "<OBJ=0>";

        private readonly System.Timers.Timer timer = new(1000) { AutoReset = false };
        private readonly object updateLock = new();
        private MemoryMappedFile? map;
        private MemoryMappedViewAccessor? view;
        private int slot = -1;
        private bool active;
        private readonly Queue<double> fpsHistory = new();
        private uint fpsHistoryProcessId;
        private bool connectionLogged;

        public bool Active => active;

        public RtssOverlay()
        {
            timer.Elapsed += (_, _) => UpdateAndSchedule();
        }

        public bool Start()
        {
            if (active) return true;

            active = true;
            ApplySensorFlags(true);
            if (!TryConnect() && !IsRtssRunning())
            {
                string? executable = FindRtssExecutable();
                if (executable == null)
                {
                    active = false;
                    ApplySensorFlags(false);
                    return false;
                }

                try
                {
                    Process.Start(new ProcessStartInfo(executable) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    Logger.WriteLine("RTSS launch failed: " + ex.Message);
                    active = false;
                    ApplySensorFlags(false);
                    return false;
                }
            }

            timer.Start();
            UpdateAndSchedule();
            return true;
        }

        public void Stop()
        {
            active = false;
            timer.Stop();
            lock (updateLock)
            {
                ReleaseSlot();
                CloseMapping();
            }
            ApplySensorFlags(false);
        }

        private void UpdateAndSchedule()
        {
            if (!active) return;
            lock (updateLock)
            {
                try
                {
                    ApplySensorFlags(true);
                    if (TryConnect())
                    {
                        // Publish first so a slow or unsupported sensor cannot block
                        // the complete OSD. Sensor values refresh on the next tick.
                        WriteOsd(BuildText());
                        try
                        {
                            HardwareControl.ReadSensorsOverlay();
                        }
                        catch (Exception sensorError)
                        {
                            Logger.WriteLine("RTSS sensor update failed: " + sensorError.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLine("RTSS OSD update failed: " + ex.Message);
                    CloseMapping();
                }
            }
            if (active) timer.Start();
        }

        private bool TryConnect()
        {
            if (view != null)
            {
                try { return view.ReadUInt32(0) == Signature; }
                catch { CloseMapping(); }
            }

            try
            {
                map = MemoryMappedFile.OpenExisting(MapName, MemoryMappedFileRights.ReadWrite);
                view = map.CreateViewAccessor(0, 0, MemoryMappedFileAccess.ReadWrite);
                if (view.ReadUInt32(0) != Signature || view.ReadUInt32(4) < MinimumVersion)
                {
                    CloseMapping();
                    return false;
                }
                slot = -1;
                if (!connectionLogged)
                {
                    Logger.WriteLine($"RTSS OSD connected: shared memory v{view.ReadUInt32(4):X8}");
                    connectionLogged = true;
                }
                return true;
            }
            catch
            {
                CloseMapping();
                return false;
            }
        }

        private void WriteOsd(string text)
        {
            if (view == null) return;
            uint version = view.ReadUInt32(4);
            int entrySize = checked((int)view.ReadUInt32(20));
            int arrayOffset = checked((int)view.ReadUInt32(24));
            int arraySize = checked((int)view.ReadUInt32(28));
            // v2.12+ entries include a 256 KiB embedded-object buffer.
            if (entrySize < 512 || entrySize > 1024 * 1024 || arraySize < 2 || arraySize > 64) return;
            if ((long)arrayOffset + (long)entrySize * arraySize > view.Capacity) return;

            TryWriteLocked(version, () =>
            {
                if (slot < 1 || slot >= arraySize || !OwnsSlot(arrayOffset + slot * entrySize))
                    slot = FindOrClaimSlot(arrayOffset, entrySize, arraySize);
                if (slot < 0) return;

                long entry = arrayOffset + (long)slot * entrySize;
                if (version >= EmbeddedObjectsVersion && text.Contains(FpsGraphTag, StringComparison.Ordinal))
                    WriteFpsGraphObject(entry, entrySize);
                if (version >= ExtendedTextVersion && entrySize > ExtendedTextOffset)
                    WriteString(entry + ExtendedTextOffset, Math.Min(4096, entrySize - ExtendedTextOffset), text);
                else
                    WriteString(entry, 256, text);

                view.Write(32, view.ReadUInt32(32) + 1);
                view.Flush();
            });
        }

        private void WriteFpsGraphObject(long entry, int entrySize)
        {
            if (view == null || entrySize < EmbeddedBufferOffset + GraphObjectSize) return;

            long graph = entry + EmbeddedBufferOffset;
            view.Write(graph, GraphSignature);
            view.Write(graph + 4, (uint)GraphObjectSize);
            view.Write(graph + 8, -18); // negative dimensions are measured in OSD characters
            view.Write(graph + 12, -2);
            view.Write(graph + 16, 1);
            view.Write(graph + 20, GraphFramerate);
            view.Write(graph + 24, 0f);
            view.Write(graph + 28, GetFpsGraphCeiling());
            view.Write(graph + 32, 0u); // RTSS supplies per-frame data for the foreground 3D app
        }

        private float GetFpsGraphCeiling()
        {
            double peak = fpsHistory.Count > 0 ? fpsHistory.Max() : 60;
            return (float)Math.Max(60, Math.Ceiling(peak / 30d) * 30d);
        }

        private unsafe bool TryWriteLocked(uint version, Action writer)
        {
            if (view == null) return false;
            byte* pointer = null;
            bool pointerAcquired = false;
            bool lockAcquired = false;
            try
            {
                if (version >= WriteLockVersion)
                {
                    view.SafeMemoryMappedViewHandle.AcquirePointer(ref pointer);
                    pointerAcquired = true;
                    pointer += view.PointerOffset;
                    if (Interlocked.CompareExchange(ref *(int*)(pointer + 36), 1, 0) != 0)
                        return false;
                    lockAcquired = true;
                }

                writer();
                return true;
            }
            finally
            {
                if (lockAcquired) Volatile.Write(ref *(int*)(pointer + 36), 0);
                if (pointerAcquired) view.SafeMemoryMappedViewHandle.ReleasePointer();
            }
        }

        private int FindOrClaimSlot(int arrayOffset, int entrySize, int arraySize)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                for (int index = 1; index < arraySize; index++)
                {
                    long entry = arrayOffset + (long)index * entrySize;
                    string owner = ReadString(entry + OwnerOffset, 256);
                    if (pass == 1 && owner.Length == 0)
                    {
                        WriteString(entry + OwnerOffset, 256, OwnerName);
                        owner = OwnerName;
                    }
                    if (string.Equals(owner, OwnerName, StringComparison.Ordinal)) return index;
                }
            }
            return -1;
        }

        private bool OwnsSlot(long entry) =>
            string.Equals(ReadString(entry + OwnerOffset, 256), OwnerName, StringComparison.Ordinal);

        private void ReleaseSlot()
        {
            if (view == null || slot < 1) return;
            try
            {
                uint version = view.ReadUInt32(4);
                int entrySize = checked((int)view.ReadUInt32(20));
                int arrayOffset = checked((int)view.ReadUInt32(24));
                long entry = arrayOffset + (long)slot * entrySize;
                TryWriteLocked(version, () =>
                {
                    if (entrySize >= 512 && entry + entrySize <= view.Capacity && OwnsSlot(entry))
                    {
                        view.WriteArray(entry, new byte[entrySize], 0, entrySize);
                        view.Write(32, view.ReadUInt32(32) + 1);
                        view.Flush();
                    }
                });
            }
            catch { }
            slot = -1;
        }

        private string BuildText()
        {
            uint version = view?.ReadUInt32(4) ?? MinimumVersion;
            bool tags = version >= FormatTagsVersion;
            List<string> lines = [];
            (uint frameTime, string api, uint processId) = ReadForegroundFrameStats(version);
            int fps = frameTime > 0 ? (int)Math.Round(1_000_000d / frameTime) : 0;
            UpdateFpsHistory(processId, fps);
            if (AppConfig.IsNotFalse("rtss_show_fps") && fps > 0)
                lines.Add(Colorize(tags, 2, $"{api} {fps} FPS"));
            if (AppConfig.IsNotFalse("rtss_show_fps_graph") && fps > 0 && version >= EmbeddedObjectsVersion)
                lines.Add(Colorize(tags, 2, "FPS " + FpsGraphTag));
            if (AppConfig.IsNotFalse("rtss_show_fps_low") && fpsHistory.Count > 0)
                lines.Add(Colorize(tags, 2, $"1% LOW {CalculateOnePercentLow()} FPS"));
            if (AppConfig.IsNotFalse("rtss_show_frametime") && frameTime > 0)
                lines.Add(Colorize(tags, 6, "FT " +
                    (frameTime / 1000d).ToString("F1", CultureInfo.InvariantCulture) + "ms"));

            string? cpu = FormatMetric("CPU", HardwareControl.cpuTemp, HardwareControl.cpuUsage,
                HardwareControl.cpuPower, HardwareControl.cpuClockMHz, tags, 0, "cpu");
            string? gpu = FormatMetric("GPU", HardwareControl.gpuTemp, HardwareControl.gpuUsage,
                HardwareControl.gpuPower, HardwareControl.gpuClockMHz, tags, 1, "gpu");
            if (cpu != null) lines.Add(cpu);
            if (gpu != null && ((HardwareControl.gpuTemp ?? 0) > 0 || HardwareControl.gpuUsage.HasValue)) lines.Add(gpu);
            decimal battery = HardwareControl.batteryCapacity;
            if (battery <= 0) battery = (decimal)HardwareControl.GetBatteryChargePercentage();
            bool showBattery = AppConfig.IsNotFalse("rtss_show_battery");
            bool showBatteryTime = AppConfig.IsNotFalse("rtss_show_battery_time");
            bool showBatteryRate = AppConfig.IsNotFalse("rtss_show_battery_rate");
            int remainingSeconds = showBatteryTime ? HardwareControl.GetBatteryRemainingTimeSeconds() : -1;
            decimal batteryRate = HardwareControl.batteryRate ?? 0;
            if ((showBattery && battery > 0) || remainingSeconds > 0 || (showBatteryRate && batteryRate != 0))
            {
                string batteryText = "BAT";
                if (showBattery && battery > 0)
                    batteryText += " " + battery.ToString("F1", CultureInfo.InvariantCulture) + "%";
                if (remainingSeconds > 0)
                    batteryText += " " + FormatRemainingTime(remainingSeconds);
                if (showBatteryRate && batteryRate != 0)
                    batteryText += " " + batteryRate.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture) + "W";
                lines.Add(Colorize(tags, 5, batteryText));
            }
            if (AppConfig.IsNotFalse("rtss_show_ram") && HardwareControl.ramUsage.HasValue)
                lines.Add(Colorize(tags, 3, "RAM " + HardwareControl.ramUsage + "%" +
                    (HardwareControl.ramUsedMb.HasValue ? $" {HardwareControl.ramUsedMb / 1024.0:F1}GB" : "")));
            if (AppConfig.IsNotFalse("rtss_show_vram") && HardwareControl.vramUsedMb.HasValue)
            {
                string vram = $"VRAM {HardwareControl.vramUsedMb.Value / 1024.0:F1}";
                if (HardwareControl.vramTotalMb.HasValue)
                    vram += $"/{HardwareControl.vramTotalMb.Value / 1024.0:F1}GB";
                if (HardwareControl.vramUsage.HasValue) vram += $" {HardwareControl.vramUsage}%";
                lines.Add(Colorize(tags, 3, vram));
            }
            if (AppConfig.IsNotFalse("rtss_show_fans"))
            {
                List<string> fans = [];
                if (HardwareControl.cpuFanRPM > 0) fans.Add($"CPU {HardwareControl.cpuFanRPM} RPM");
                if (HardwareControl.gpuFanRPM > 0) fans.Add($"GPU {HardwareControl.gpuFanRPM} RPM");
                if (fans.Count > 0) lines.Add(Colorize(tags, 4, "FAN " + string.Join("  ", fans)));
            }

            string formatHeader = tags
                ? "<C0=45C4FF><C1=FF6B6B><C2=FFE066><C3=74E39A><C4=C58CFF><C5=FF9F43><C6=FFFFFF>\r"
                : "";
            if (AppConfig.IsNotFalse("rtss_single_line"))
                return formatHeader + string.Join("  |  ", lines);

            lines.Insert(0, tags ? "<C2>G-HELPER<C>" : "G-HELPER");
            return formatHeader + string.Join("\n", lines);
        }

        private static string? FormatMetric(string name, float? temp, int? usage, float? power,
            int? clockMHz, bool tags, int color, string configPrefix)
        {
            bool showTemp = AppConfig.IsNotFalse($"rtss_show_{configPrefix}_temp");
            bool showUsage = AppConfig.IsNotFalse($"rtss_show_{configPrefix}_usage");
            bool showPower = AppConfig.IsNotFalse($"rtss_show_{configPrefix}_power");
            bool showClock = AppConfig.IsNotFalse($"rtss_show_{configPrefix}_clock");
            if (!showTemp && !showUsage && !showPower && !showClock) return null;

            string result = name;
            if (showUsage && usage.HasValue) result += $"  {usage}%";
            if (showTemp) result += temp > 0 ? $" {temp:F0}C" : " --C";
            if (showPower && power.HasValue) result += $"  {power:F1}W";
            if (showClock && clockMHz > 0) result += $"  {clockMHz}MHz";
            return Colorize(tags, color, result);
        }

        private static string Colorize(bool tags, int color, string text) =>
            tags ? $"<C{color}>{text}<C>" : text;

        private void UpdateFpsHistory(uint processId, int fps)
        {
            if (processId != fpsHistoryProcessId)
            {
                fpsHistory.Clear();
                fpsHistoryProcessId = processId;
            }
            if (processId == 0 || fps <= 0) return;
            fpsHistory.Enqueue(fps);
            while (fpsHistory.Count > 120) fpsHistory.Dequeue();
        }

        private int CalculateOnePercentLow()
        {
            if (fpsHistory.Count == 0) return 0;
            int sampleCount = Math.Max(1, (int)Math.Ceiling(fpsHistory.Count * 0.01));
            return (int)Math.Round(fpsHistory.OrderBy(value => value).Take(sampleCount).Average());
        }

        private static string FormatRemainingTime(int seconds)
        {
            int totalMinutes = Math.Max(1, (int)Math.Round(seconds / 60d));
            int hours = totalMinutes / 60;
            int minutes = totalMinutes % 60;
            return hours > 0 ? $"{hours}h {minutes:00}m" : $"{minutes}m";
        }

        private (uint FrameTime, string Api, uint ProcessId) ReadForegroundFrameStats(uint version)
        {
            if (view == null) return (0, "3D", 0);
            GetWindowThreadProcessId(GetForegroundWindow(), out uint foregroundPid);
            if (foregroundPid == 0) return (0, "3D", 0);
            try
            {
                int entrySize = checked((int)view.ReadUInt32(8));
                int arrayOffset = checked((int)view.ReadUInt32(12));
                int arraySize = checked((int)view.ReadUInt32(16));
                if (entrySize < 284 || arraySize < 1 || arraySize > 1024) return (0, "3D", foregroundPid);
                if ((long)arrayOffset + (long)entrySize * arraySize > view.Capacity) return (0, "3D", foregroundPid);
                for (int index = 0; index < arraySize; index++)
                {
                    long entry = arrayOffset + (long)index * entrySize;
                    if (view.ReadUInt32(entry) != foregroundPid) continue;
                    uint flags = view.ReadUInt32(entry + 264);
                    return (view.ReadUInt32(entry + 280), ApiName(flags, version), foregroundPid);
                }
            }
            catch { }
            return (0, "3D", foregroundPid);
        }

        private static string ApiName(uint flags, uint version)
        {
            if (version >= 0x0002000A)
            {
                return (flags & 0xFFFF) switch
                {
                    1 => "OPENGL", 2 => "DD", 3 => "D3D8", 4 => "D3D9",
                    5 => "D3D9EX", 6 => "D3D10", 7 => "D3D11", 8 => "D3D12",
                    9 => "D3D12 AFR", 10 => "VULKAN", _ => "3D"
                };
            }

            if ((flags & 0x01000000) != 0) return "D3D11";
            if ((flags & 0x00100000) != 0) return "D3D10";
            if ((flags & 0x00010000) != 0) return "OPENGL";
            if ((flags & 0x00002000) != 0) return "D3D9EX";
            if ((flags & 0x00001000) != 0) return "D3D9";
            return "3D";
        }

        private string ReadString(long offset, int length)
        {
            if (view == null) return string.Empty;
            byte[] bytes = new byte[length];
            view.ReadArray(offset, bytes, 0, length);
            int end = Array.IndexOf(bytes, (byte)0);
            return Encoding.ASCII.GetString(bytes, 0, end < 0 ? length : end);
        }

        private void WriteString(long offset, int capacity, string value)
        {
            if (view == null || capacity <= 0) return;
            byte[] destination = new byte[capacity];
            byte[] source = Encoding.ASCII.GetBytes(value);
            Array.Copy(source, destination, Math.Min(source.Length, capacity - 1));
            view.WriteArray(offset, destination, 0, destination.Length);
        }

        private static void ApplySensorFlags(bool enabled)
        {
            HardwareControl.readFans = enabled && AppConfig.IsNotFalse("rtss_show_fans");
            HardwareControl.readUsage = enabled && (AppConfig.IsNotFalse("rtss_show_cpu_usage") || AppConfig.IsNotFalse("rtss_show_gpu_usage"));
            HardwareControl.readMemory = enabled &&
                (AppConfig.IsNotFalse("rtss_show_ram") || AppConfig.IsNotFalse("rtss_show_vram"));
            HardwareControl.readClocks = enabled &&
                (AppConfig.IsNotFalse("rtss_show_cpu_clock") || AppConfig.IsNotFalse("rtss_show_gpu_clock"));
            HardwareControl.readPower = enabled && (AppConfig.IsNotFalse("rtss_show_cpu_power") || AppConfig.IsNotFalse("rtss_show_gpu_power"));
            HardwareControl.readBattery = enabled &&
                (AppConfig.IsNotFalse("rtss_show_battery") ||
                 AppConfig.IsNotFalse("rtss_show_battery_time") ||
                 AppConfig.IsNotFalse("rtss_show_battery_rate"));
        }

        private static bool IsRtssRunning()
        {
            try { return Process.GetProcessesByName("RTSS").Length > 0; }
            catch { return false; }
        }

        private static string? FindRtssExecutable()
        {
            try
            {
                using RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
                using RegistryKey? key = baseKey.OpenSubKey(@"Software\Unwinder\RTSS");
                string? installPath = key?.GetValue("InstallPath")?.ToString();
                if (!string.IsNullOrWhiteSpace(installPath))
                {
                    string registered = installPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                        ? installPath : Path.Combine(installPath, "RTSS.exe");
                    if (File.Exists(registered)) return registered;
                }
            }
            catch { }

            string fallback = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "RivaTuner Statistics Server", "RTSS.exe");
            return File.Exists(fallback) ? fallback : null;
        }

        private void CloseMapping()
        {
            view?.Dispose();
            map?.Dispose();
            view = null;
            map = null;
            slot = -1;
            connectionLogged = false;
        }

        public void Dispose()
        {
            Stop();
            timer.Dispose();
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    }
}

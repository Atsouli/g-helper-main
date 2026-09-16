using System.Runtime.InteropServices;
using GHelper.Ally;
using GHelper.Display;
using GHelper.Helpers;
using GHelper.Mode;

namespace GHelper.Input
{
    /// <summary>
    /// Global handheld controller chords:
    /// - Hold both triggers and press D-pad Left/Right for a one-watt TDP step.
    /// - Hold ROG and use D-pad Up/Down for GPU clock or Left/Right for CPU max frequency.
    /// - In gamepad mode, hold a rear paddle and use the right stick as a mouse.
    /// - In desktop mode, hold a rear paddle and move the left stick for Q/E/R/T.
    /// - In desktop mode, hold a rear paddle and move the right stick to adjust brightness or scroll.
    /// </summary>
    internal sealed class GamepadTdpControl : IDisposable
    {
        private const ushort DPadLeft = 0x0004;
        private const ushort DPadRight = 0x0008;
        private const ushort DPadUp = 0x0001;
        private const ushort DPadDown = 0x0002;
        private const ushort LeftBumper = 0x0100;
        private const ushort RightBumper = 0x0200;
        private const ushort StartButton = 0x0010;
        private const ushort BackButton = 0x0020;
        private const ushort LeftStickButton = 0x0040;
        private const ushort RightStickButton = 0x0080;
        private const ushort AButton = 0x1000;
        private const ushort BButton = 0x2000;
        private const ushort XButton = 0x4000;
        private const ushort YButton = 0x8000;
        private const ushort BumperButtons = LeftBumper | RightBumper;
        private const ushort HorizontalDirections = DPadLeft | DPadRight;
        private const ushort AllDirections = DPadUp | DPadDown | DPadLeft | DPadRight;
        private const byte TriggerThreshold = 180;
        private const int TdpRepeatDelayMs = 450;
        private const int TdpRepeatIntervalMs = 120;
        private const int FrequencyRepeatDelayMs = 450;
        private const int FrequencyRepeatIntervalMs = 140;
        private const int BrightnessChordRepeatDelayMs = 450;
        private const int BrightnessChordRepeatIntervalMs = 160;
        private const double MouseDeadzone = 0.16;
        private const double MouseSpeed = 26.0;
        private const uint MouseEventMove = 0x0001;
        private const uint MouseEventLeftDown = 0x0002;
        private const uint MouseEventLeftUp = 0x0004;
        private const uint MouseEventRightDown = 0x0008;
        private const uint MouseEventRightUp = 0x0010;
        private const uint MouseEventWheel = 0x0800;
        private const int WheelDelta = 120;
        private const int MouseGestureThreshold = 28;
        private const int BrightnessRepeatMs = 160;
        private const int ScrollRepeatMs = 90;
        private const int ScrollDirectionDebounceMs = 100;
        private const double GestureAxisDominance = 1.35;
        private const int WhKeyboardLowLevel = 13;
        private const int WhMouseLowLevel = 14;
        private const int WmMouseMove = 0x0200;
        private const int WmKeyDown = 0x0100;
        private const int WmKeyUp = 0x0101;
        private const int WmSysKeyDown = 0x0104;
        private const int WmSysKeyUp = 0x0105;

        private readonly System.Windows.Forms.Timer timer = new() { Interval = 16 };
        private ushort repeatingTdpDirection;
        private long nextTdpRepeatAt;
        private ushort repeatingFrequencyDirection;
        private long nextFrequencyRepeatAt;
        private bool frequencyElevationRequested;
        private ushort repeatingBrightnessChordBumper;
        private long nextBrightnessChordRepeatAt;
        private readonly LowLevelKeyboardProc keyboardProc;
        private readonly LowLevelMouseProc mouseProc;
        private readonly HashSet<int> suppressedDesktopStickKeys = [];
        private IntPtr keyboardHook;
        private IntPtr mouseHook;
        private int desktopMouseX;
        private int desktopMouseY;
        private long nextBrightnessAt;
        private long nextScrollAt;
        private long scrollDirectionChangedAt;
        private int pendingScrollDirection;
        private int scrollEventQueued;
        private int controllerIndex = -1;
        private double mouseRemainderX;
        private double mouseRemainderY;
        private bool previousPaddleLeftClick;
        private bool previousPaddleRightClick;
        private ushort previousRogButtons;
        private bool previousRogLeftTrigger;
        private bool previousRogRightTrigger;
        private readonly HashSet<string> previousRogStickDirections = [];

        public GamepadTdpControl()
        {
            keyboardProc = DesktopStickKeyboardHook;
            mouseProc = DesktopStickMouseHook;
            if (AppConfig.IsAlly())
            {
                keyboardHook = SetWindowsHookEx(WhKeyboardLowLevel, keyboardProc, GetModuleHandle(null), 0);
                mouseHook = SetWindowsHookEx(WhMouseLowLevel, mouseProc, GetModuleHandle(null), 0);
            }
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (!TryReadGamepad(out XInputGamepad gamepad))
            {
                repeatingTdpDirection = 0;
                ResetFrequencyChord();
                return;
            }

            bool bothTriggers = gamepad.LeftTrigger >= TriggerThreshold &&
                                gamepad.RightTrigger >= TriggerThreshold &&
                                !InputDispatcher.isRogLongPressed;
            ushort chordDirections = bothTriggers
                ? (ushort)(gamepad.Buttons & HorizontalDirections)
                : (ushort)0;
            ushort direction = (chordDirections & DPadRight) != 0
                ? DPadRight
                : (ushort)(chordDirections & DPadLeft);
            long now = Environment.TickCount64;

            if (direction == 0)
            {
                repeatingTdpDirection = 0;
            }
            else if (direction != repeatingTdpDirection)
            {
                AdjustTdp(direction == DPadRight ? 1 : -1);
                repeatingTdpDirection = direction;
                nextTdpRepeatAt = now + TdpRepeatDelayMs;
            }
            else if (now >= nextTdpRepeatAt)
            {
                AdjustTdp(direction == DPadRight ? 1 : -1);
                nextTdpRepeatAt = now + TdpRepeatIntervalMs;
            }

            HandleFrequencyChord(gamepad, now);
            HandleBrightnessChord(gamepad, now);
            HandleRogThirdLayer(gamepad);
            HandlePaddleMouse(gamepad);
        }

        private void HandleFrequencyChord(XInputGamepad gamepad, long now)
        {
            if (!InputDispatcher.isRogLongPressed)
            {
                ResetFrequencyChord();
                return;
            }

            ushort pressed = (ushort)(gamepad.Buttons & AllDirections);
            ushort direction = (pressed & DPadUp) != 0 ? DPadUp :
                (pressed & DPadDown) != 0 ? DPadDown :
                (pressed & DPadRight) != 0 ? DPadRight :
                (ushort)(pressed & DPadLeft);

            if (direction == 0)
            {
                ResetFrequencyChord();
                return;
            }

            if (direction != repeatingFrequencyDirection)
            {
                ExecuteDPadAction(direction);
                repeatingFrequencyDirection = direction;
                nextFrequencyRepeatAt = now + FrequencyRepeatDelayMs;
            }
            else if (now >= nextFrequencyRepeatAt)
            {
                ExecuteDPadAction(direction, isRepeat: true);
                nextFrequencyRepeatAt = now + FrequencyRepeatIntervalMs;
            }
        }

        private void ExecuteDPadAction(ushort direction, bool isRepeat = false)
        {
            string button = direction switch
            {
                DPadLeft => "dl",
                DPadRight => "dr",
                DPadUp => "du",
                _ => "dd"
            };
            string configKey = GetRogLayerKey(button);
            string customAction = AppConfig.GetString(configKey);
            if (string.IsNullOrWhiteSpace(customAction))
            {
                configKey = direction is DPadLeft or DPadRight ? "m4_dpad_x" : "m4_dpad_y";
                customAction = AppConfig.GetString(configKey);
            }

            if (string.IsNullOrWhiteSpace(customAction))
            {
                if (ModeControl.IsAllyZ1Extreme()) AdjustFrequency(direction);
            }
            else if (!isRepeat || customAction is "volume_up" or "volume_down" or "brightness_up" or "brightness_down" or "backlight_up" or "backlight_down")
            {
                RunConfiguredAction(customAction, configKey);
            }
        }

        private void AdjustFrequency(ushort direction)
        {
            if (direction is DPadUp or DPadDown)
            {
                // A non-elevated instance cannot safely keep changing the SMU value while
                // the one elevated apply request is starting. Allow one step per hold there.
                if (!ProcessHelper.IsUserAdministrator() && frequencyElevationRequested) return;
                bool requestElevation = !frequencyElevationRequested;
                int mhz = Program.modeControl.AdjustAllyGpuFrequency(direction == DPadUp ? 1 : -1,
                    requestElevation);
                if (!ProcessHelper.IsUserAdministrator()) frequencyElevationRequested = true;
                Program.toast.RunToast($"GPU Clock {mhz} MHz", ToastIcon.Controller);
                return;
            }

            if (!ProcessHelper.IsUserAdministrator() && frequencyElevationRequested) return;
            bool requestCpuElevation = !frequencyElevationRequested;
            (int cpuMhz, bool applied, bool elevationRequested) = Program.modeControl.AdjustAllyCpuFrequency(
                direction == DPadRight ? 1 : -1, requestCpuElevation);
            if (elevationRequested) frequencyElevationRequested = true;

            string message = applied
                ? $"CPU Max {cpuMhz} MHz"
                : elevationRequested
                    ? $"CPU Max {cpuMhz} MHz - applying as administrator"
                    : $"CPU limit failed: {cpuMhz} MHz";
            Program.toast.RunToast(message, ToastIcon.Controller);
        }

        private void ResetFrequencyChord()
        {
            repeatingFrequencyDirection = 0;
            frequencyElevationRequested = false;
        }

        private void HandleBrightnessChord(XInputGamepad gamepad, long now)
        {
            if (!InputDispatcher.isRogLongPressed)
            {
                repeatingBrightnessChordBumper = 0;
                return;
            }

            ushort pressed = (ushort)(gamepad.Buttons & BumperButtons);
            ushort bumper = (pressed & RightBumper) != 0 ? RightBumper :
                (ushort)(pressed & LeftBumper);

            if (bumper == 0)
            {
                repeatingBrightnessChordBumper = 0;
                return;
            }

            if (bumper != repeatingBrightnessChordBumper)
            {
                string configKey = GetRogLayerKey(bumper == RightBumper ? "rb" : "lb");
                string customAction = AppConfig.GetString(configKey);
                if (string.IsNullOrWhiteSpace(customAction))
                {
                    configKey = bumper == RightBumper ? "m4_rb" : "m4_lb";
                    customAction = AppConfig.GetString(configKey);
                }
                if (!string.IsNullOrWhiteSpace(customAction))
                {
                    RunConfiguredAction(customAction, configKey);
                }
                else
                {
                    AdjustScreenBrightness(bumper == RightBumper ? 10 : -10);
                }
                repeatingBrightnessChordBumper = bumper;
                nextBrightnessChordRepeatAt = now + BrightnessChordRepeatDelayMs;
            }
            else if (now >= nextBrightnessChordRepeatAt)
            {
                string configKey = GetRogLayerKey(bumper == RightBumper ? "rb" : "lb");
                string customAction = AppConfig.GetString(configKey);
                if (string.IsNullOrWhiteSpace(customAction))
                {
                    configKey = bumper == RightBumper ? "m4_rb" : "m4_lb";
                    customAction = AppConfig.GetString(configKey);
                }
                if (string.IsNullOrWhiteSpace(customAction))
                {
                    AdjustScreenBrightness(bumper == RightBumper ? 10 : -10);
                }
                else if (customAction is "volume_up" or "volume_down" or "brightness_up" or "brightness_down" or "backlight_up" or "backlight_down")
                {
                    // Only repeat volume/brightness related actions to prevent spamming apps or toggles
                    RunConfiguredAction(customAction, configKey);
                }
                nextBrightnessChordRepeatAt = now + BrightnessChordRepeatIntervalMs;
            }
        }

        private void HandleRogThirdLayer(XInputGamepad gamepad)
        {
            if (!InputDispatcher.isRogLongPressed)
            {
                previousRogButtons = 0;
                previousRogLeftTrigger = false;
                previousRogRightTrigger = false;
                previousRogStickDirections.Clear();
                return;
            }

            // D-pad and bumpers have repeat/fallback handling above.
            const ushort handledElsewhere = AllDirections | BumperButtons;
            ushort pressed = (ushort)(gamepad.Buttons & ~handledElsewhere);
            ushort newlyPressed = (ushort)(pressed & ~previousRogButtons);
            previousRogButtons = pressed;

            RunRogButton(newlyPressed, AButton, "a");
            RunRogButton(newlyPressed, BButton, "b");
            RunRogButton(newlyPressed, XButton, "x");
            RunRogButton(newlyPressed, YButton, "y");
            RunRogButton(newlyPressed, LeftStickButton, "ls");
            RunRogButton(newlyPressed, RightStickButton, "rs");
            RunRogButton(newlyPressed, BackButton, "vb");
            RunRogButton(newlyPressed, StartButton, "mb");

            bool leftTrigger = gamepad.LeftTrigger >= TriggerThreshold;
            bool rightTrigger = gamepad.RightTrigger >= TriggerThreshold;
            if (leftTrigger && !previousRogLeftTrigger) RunRogAction("lt");
            if (rightTrigger && !previousRogRightTrigger) RunRogAction("rt");
            previousRogLeftTrigger = leftTrigger;
            previousRogRightTrigger = rightTrigger;

            const short stickThreshold = 20000;
            var stickDirections = new HashSet<string>();
            if (gamepad.ThumbLX <= -stickThreshold) stickDirections.Add("ls_left");
            if (gamepad.ThumbLX >= stickThreshold) stickDirections.Add("ls_right");
            if (gamepad.ThumbLY >= stickThreshold) stickDirections.Add("ls_up");
            if (gamepad.ThumbLY <= -stickThreshold) stickDirections.Add("ls_down");
            if (gamepad.ThumbRX <= -stickThreshold) stickDirections.Add("rs_left");
            if (gamepad.ThumbRX >= stickThreshold) stickDirections.Add("rs_right");
            if (gamepad.ThumbRY >= stickThreshold) stickDirections.Add("rs_up");
            if (gamepad.ThumbRY <= -stickThreshold) stickDirections.Add("rs_down");
            foreach (string direction in stickDirections)
                if (!previousRogStickDirections.Contains(direction)) RunRogAction(direction);
            previousRogStickDirections.Clear();
            previousRogStickDirections.UnionWith(stickDirections);
        }

        private static void RunRogButton(ushort newlyPressed, ushort mask, string binding)
        {
            if ((newlyPressed & mask) != 0) RunRogAction(binding);
        }

        private static void RunRogAction(string binding)
        {
            string configKey = GetRogLayerKey(binding);
            string action = AppConfig.GetString(configKey);
            if (!string.IsNullOrWhiteSpace(action)) RunConfiguredAction(action, configKey);
        }

        private static string GetRogLayerKey(string binding) =>
            (AllyControl.IsDesktopMode ? "rog_desktop_" : "rog_gamepad_") + binding;

        private static void RunConfiguredAction(string action, string configKey)
        {
            if (action == "custom")
                action = AppConfig.GetString("custom_" + configKey);
            InputDispatcher.RunCustomAction(action);
        }

        private static void AdjustScreenBrightness(int delta)
        {
            int brightness = VisualControl.SetBrightness(delta: delta);
            if (brightness >= 0)
                Program.toast.RunToast(brightness + "%", delta > 0 ? ToastIcon.BrightnessUp : ToastIcon.BrightnessDown);
        }

        private IntPtr DesktopStickKeyboardHook(int code, IntPtr message, IntPtr data)
        {
            if (code >= 0)
            {
                int virtualKey = Marshal.ReadInt32(data);
                bool isStickKey = virtualKey is (int)Keys.W or (int)Keys.D or (int)Keys.S or (int)Keys.A;
                bool keyDown = message == (IntPtr)WmKeyDown || message == (IntPtr)WmSysKeyDown;
                bool keyUp = message == (IntPtr)WmKeyUp || message == (IntPtr)WmSysKeyUp;

                if (isStickKey && keyDown && GHelper.Ally.AllyControl.IsDesktopMode && InputDispatcher.isPaddlePressed)
                {
                    if (suppressedDesktopStickKeys.Add(virtualKey))
                        KeyboardHook.KeyPress(MapDesktopStickKey(virtualKey));
                    return (IntPtr)1;
                }

                // Suppress the matching release even if the paddle was released first.
                if (isStickKey && keyUp && suppressedDesktopStickKeys.Remove(virtualKey))
                    return (IntPtr)1;
            }

            return CallNextHookEx(keyboardHook, code, message, data);
        }

        private static Keys MapDesktopStickKey(int virtualKey) => virtualKey switch
        {
            (int)Keys.W => Keys.Q,
            (int)Keys.D => Keys.E,
            (int)Keys.S => Keys.R,
            _ => Keys.T
        };

        private IntPtr DesktopStickMouseHook(int code, IntPtr message, IntPtr data)
        {
            if (code >= 0 && message == (IntPtr)WmMouseMove &&
                GHelper.Ally.AllyControl.IsDesktopMode && InputDispatcher.isPaddlePressed)
            {
                MouseHookData mouse = Marshal.PtrToStructure<MouseHookData>(data);
                Point cursor = Cursor.Position;
                int dx = mouse.Point.X - cursor.X;
                int dy = mouse.Point.Y - cursor.Y;
                int absX = Math.Abs(dx);
                int absY = Math.Abs(dy);

                // Ignore diagonal/noisy samples until one gesture axis is clearly dominant.
                // This prevents a stick near the diagonal from alternating brightness and
                // opposite wheel events on every mouse-hook callback.
                bool horizontal = absX >= absY * GestureAxisDominance;
                bool vertical = absY >= absX * GestureAxisDominance;
                if (!horizontal && !vertical)
                {
                    desktopMouseX = 0;
                    desktopMouseY = 0;
                    return (IntPtr)1;
                }

                if (horizontal)
                {
                    desktopMouseX += dx;
                    desktopMouseY = 0;
                    pendingScrollDirection = 0;
                    long now = Environment.TickCount64;
                    if (Math.Abs(desktopMouseX) >= MouseGestureThreshold && now >= nextBrightnessAt)
                    {
                        string action = AppConfig.GetString("m12_rs_x");
                        if (string.IsNullOrWhiteSpace(action))
                        {
                            InputDispatcher.SetBrightness(desktopMouseX > 0);
                        }
                        else
                        {
                            InputDispatcher.RunCustomAction(action);
                        }
                        desktopMouseX = 0;
                        nextBrightnessAt = now + BrightnessRepeatMs;
                    }
                }
                else
                {
                    desktopMouseY += dy;
                    desktopMouseX = 0;
                    long now = Environment.TickCount64;
                    int direction = desktopMouseY < 0 ? 1 : -1;

                    if (direction != pendingScrollDirection)
                    {
                        pendingScrollDirection = direction;
                        scrollDirectionChangedAt = now;
                        desktopMouseY = Math.Sign(desktopMouseY) * Math.Min(Math.Abs(desktopMouseY), MouseGestureThreshold);
                    }
                    else if (Math.Abs(desktopMouseY) >= MouseGestureThreshold &&
                             now >= nextScrollAt &&
                             now - scrollDirectionChangedAt >= ScrollDirectionDebounceMs)
                    {
                        string action = AppConfig.GetString("m12_rs_y");
                        if (string.IsNullOrWhiteSpace(action))
                        {
                            QueueMouseWheel(direction * WheelDelta);
                        }
                        else
                        {
                            InputDispatcher.RunCustomAction(action);
                        }
                        desktopMouseY = 0;
                        nextScrollAt = now + ScrollRepeatMs;
                    }
                }

                return (IntPtr)1;
            }

            if (!InputDispatcher.isPaddlePressed)
            {
                desktopMouseX = 0;
                desktopMouseY = 0;
                pendingScrollDirection = 0;
            }
            return CallNextHookEx(mouseHook, code, message, data);
        }

        private void QueueMouseWheel(int wheel)
        {
            if (Interlocked.Exchange(ref scrollEventQueued, 1) != 0) return;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    mouse_event(MouseEventWheel, 0, 0, unchecked((uint)wheel), UIntPtr.Zero);
                }
                finally
                {
                    Volatile.Write(ref scrollEventQueued, 0);
                }
            });
        }

        private void HandlePaddleMouse(XInputGamepad gamepad)
        {
            if (!AppConfig.IsAlly() || !GHelper.Ally.AllyControl.IsGamepadMode || !InputDispatcher.isPaddlePressed)
            {
                mouseRemainderX = 0;
                mouseRemainderY = 0;
                previousPaddleLeftClick = false;
                previousPaddleRightClick = false;
                return;
            }

            bool leftClick = gamepad.RightTrigger >= TriggerThreshold;
            bool rightClick = gamepad.LeftTrigger >= TriggerThreshold;
            if (leftClick && !previousPaddleLeftClick)
                mouse_event(MouseEventLeftDown | MouseEventLeftUp, 0, 0, 0, UIntPtr.Zero);
            if (rightClick && !previousPaddleRightClick)
                mouse_event(MouseEventRightDown | MouseEventRightUp, 0, 0, 0, UIntPtr.Zero);
            previousPaddleLeftClick = leftClick;
            previousPaddleRightClick = rightClick;

            double x = ApplyMouseCurve(gamepad.ThumbRX / 32767d);
            double y = ApplyMouseCurve(gamepad.ThumbRY / 32767d);
            mouseRemainderX += x * MouseSpeed;
            mouseRemainderY -= y * MouseSpeed;

            int moveX = (int)Math.Truncate(mouseRemainderX);
            int moveY = (int)Math.Truncate(mouseRemainderY);
            mouseRemainderX -= moveX;
            mouseRemainderY -= moveY;

            if (moveX != 0 || moveY != 0)
                mouse_event(MouseEventMove, unchecked((uint)moveX), unchecked((uint)moveY), 0, UIntPtr.Zero);
        }

        private static double ApplyMouseCurve(double value)
        {
            value = Math.Clamp(value, -1d, 1d);
            double magnitude = Math.Abs(value);
            if (magnitude <= MouseDeadzone) return 0;
            double normalized = (magnitude - MouseDeadzone) / (1d - MouseDeadzone);
            return Math.Sign(value) * normalized * normalized;
        }

        private static void AdjustTdp(int delta)
        {
            int watts = Program.modeControl.AdjustTdpLimit(delta);
            Program.toast.RunToast($"TDP Limit {watts}W", ToastIcon.Controller);

            if (AppConfig.IsApplyPower())
                Task.Run(() => Program.modeControl.AutoPower());
        }

        private bool TryReadGamepad(out XInputGamepad gamepad)
        {
            gamepad = default;
            try
            {
                if (controllerIndex >= 0 && XInputGetState((uint)controllerIndex, out XInputState current) == 0)
                {
                    gamepad = current.Gamepad;
                    return true;
                }

                controllerIndex = -1;
                for (uint index = 0; index < 4; index++)
                {
                    if (XInputGetState(index, out current) == 0)
                    {
                        controllerIndex = (int)index;
                        gamepad = current.Gamepad;
                        return true;
                    }
                }
            }
            catch (DllNotFoundException)
            {
                timer.Stop();
            }
            catch (EntryPointNotFoundException)
            {
                timer.Stop();
            }

            return false;
        }

        public void Dispose()
        {
            timer.Stop();
            timer.Dispose();
            if (keyboardHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(keyboardHook);
                keyboardHook = IntPtr.Zero;
            }
            if (mouseHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(mouseHook);
                mouseHook = IntPtr.Zero;
            }
        }

        [DllImport("xinput1_4.dll")]
        private static extern uint XInputGetState(uint userIndex, out XInputState state);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extraInfo);

        private delegate IntPtr LowLevelKeyboardProc(int code, IntPtr message, IntPtr data);
        private delegate IntPtr LowLevelMouseProc(int code, IntPtr message, IntPtr data);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int hookId, LowLevelKeyboardProc callback, IntPtr module, uint threadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int hookId, LowLevelMouseProc callback, IntPtr module, uint threadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hook);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string? moduleName);

        [StructLayout(LayoutKind.Sequential)]
        private struct MouseHookData
        {
            public Point Point;
            public uint MouseData;
            public uint Flags;
            public uint Time;
            public UIntPtr ExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XInputState
        {
            public uint PacketNumber;
            public XInputGamepad Gamepad;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XInputGamepad
        {
            public ushort Buttons;
            public byte LeftTrigger;
            public byte RightTrigger;
            public short ThumbLX;
            public short ThumbLY;
            public short ThumbRX;
            public short ThumbRY;
        }
    }
}

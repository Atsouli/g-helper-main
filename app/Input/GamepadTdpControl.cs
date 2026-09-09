using System.Runtime.InteropServices;
using GHelper.Helpers;

namespace GHelper.Input
{
    /// <summary>
    /// Global handheld controller chords:
    /// - Hold both triggers and press D-pad Left/Right for a one-watt TDP step.
    /// - In gamepad mode, hold a rear paddle and use the right stick as a mouse.
    /// - In desktop mode, hold a rear paddle and move the left stick for Q/E/R/T.
    /// - In desktop mode, hold a rear paddle and move the right stick to adjust brightness or scroll.
    /// </summary>
    internal sealed class GamepadTdpControl : IDisposable
    {
        private const ushort DPadLeft = 0x0004;
        private const ushort DPadRight = 0x0008;
        private const ushort HorizontalDirections = DPadLeft | DPadRight;
        private const byte TriggerThreshold = 180;
        private const int TdpRepeatDelayMs = 450;
        private const int TdpRepeatIntervalMs = 120;
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
        private readonly LowLevelKeyboardProc keyboardProc;
        private readonly LowLevelMouseProc mouseProc;
        private readonly HashSet<int> suppressedDesktopStickKeys = [];
        private IntPtr keyboardHook;
        private IntPtr mouseHook;
        private int desktopMouseX;
        private int desktopMouseY;
        private long nextBrightnessAt;
        private int controllerIndex = -1;
        private double mouseRemainderX;
        private double mouseRemainderY;
        private bool previousPaddleLeftClick;
        private bool previousPaddleRightClick;

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
                return;
            }

            bool bothTriggers = gamepad.LeftTrigger >= TriggerThreshold &&
                                gamepad.RightTrigger >= TriggerThreshold;
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

            HandlePaddleMouse(gamepad);
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

                if (Math.Abs(dx) >= Math.Abs(dy))
                {
                    desktopMouseX += dx;
                    desktopMouseY = 0;
                    long now = Environment.TickCount64;
                    if (Math.Abs(desktopMouseX) >= MouseGestureThreshold && now >= nextBrightnessAt)
                    {
                        InputDispatcher.SetBrightness(desktopMouseX > 0);
                        desktopMouseX = 0;
                        nextBrightnessAt = now + BrightnessRepeatMs;
                    }
                }
                else
                {
                    desktopMouseY += dy;
                    desktopMouseX = 0;
                    if (Math.Abs(desktopMouseY) >= MouseGestureThreshold)
                    {
                        int wheel = desktopMouseY < 0 ? WheelDelta : -WheelDelta;
                        mouse_event(MouseEventWheel, 0, 0, unchecked((uint)wheel), UIntPtr.Zero);
                        desktopMouseY = 0;
                    }
                }

                return (IntPtr)1;
            }

            if (!InputDispatcher.isPaddlePressed)
            {
                desktopMouseX = 0;
                desktopMouseY = 0;
            }
            return CallNextHookEx(mouseHook, code, message, data);
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

using System.ComponentModel;
using System.Runtime.InteropServices;

namespace GHelper.UI
{
    /// <summary>
    /// Lightweight XInput navigation for controller-friendly forms.
    /// It only polls while Enabled and emits UI-neutral navigation commands.
    /// </summary>
    internal sealed class GamepadNavigation : Component
    {
        private const ushort DPadUp = 0x0001;
        private const ushort DPadDown = 0x0002;
        private const ushort DPadLeft = 0x0004;
        private const ushort DPadRight = 0x0008;
        private const ushort LeftShoulder = 0x0100;
        private const ushort RightShoulder = 0x0200;
        private const ushort ButtonA = 0x1000;
        private const ushort ButtonB = 0x2000;
        private const ushort DirectionButtons = DPadUp | DPadDown | DPadLeft | DPadRight;
        private const byte TriggerThreshold = 180;
        private const int LongPressMs = 650;

        private readonly System.Windows.Forms.Timer timer = new() { Interval = 60 };
        private readonly Func<bool> isActive;
        private readonly Action<Keys> navigate;
        private readonly Action activate;
        private readonly Func<bool> longActivate;
        private readonly Action back;
        private readonly Action<int> changeTab;
        private ushort previousButtons;
        private ushort repeatingDirection;
        private bool suppressDirectionsUntilRelease;
        private long nextRepeatAt;
        private int controllerIndex = -1;
        private long buttonADownAt;
        private bool buttonALongPressChecked;
        private bool buttonALongActivated;

        public bool Enabled
        {
            get => timer.Enabled;
            set
            {
                if (value)
                    timer.Start();
                else
                {
                    timer.Stop();
                    previousButtons = 0;
                    repeatingDirection = 0;
                    suppressDirectionsUntilRelease = false;
                    buttonADownAt = 0;
                    buttonALongPressChecked = false;
                    buttonALongActivated = false;
                }
            }
        }

        public GamepadNavigation(IContainer? container, Func<bool> isActive, Action<Keys> navigate,
            Action activate, Func<bool> longActivate, Action back, Action<int> changeTab)
        {
            container?.Add(this);
            this.isActive = isActive;
            this.navigate = navigate;
            this.activate = activate;
            this.longActivate = longActivate;
            this.back = back;
            this.changeTab = changeTab;
            timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (!isActive())
                return;

            if (!TryReadGamepad(out XInputGamepad gamepad))
            {
                previousButtons = 0;
                repeatingDirection = 0;
                suppressDirectionsUntilRelease = false;
                buttonADownAt = 0;
                buttonALongPressChecked = false;
                buttonALongActivated = false;
                return;
            }

            ushort buttons = gamepad.Buttons;

            ushort pressed = (ushort)(buttons & ~previousButtons);
            long now = Environment.TickCount64;
            bool aHeld = (buttons & ButtonA) != 0;
            bool aWasHeld = (previousButtons & ButtonA) != 0;
            if (aHeld && !aWasHeld)
            {
                buttonADownAt = now;
                buttonALongPressChecked = false;
                buttonALongActivated = false;
            }
            else if (aHeld && !buttonALongPressChecked && now - buttonADownAt >= LongPressMs)
            {
                buttonALongPressChecked = true;
                buttonALongActivated = longActivate();
            }
            else if (!aHeld && aWasHeld)
            {
                if (!buttonALongActivated) activate();
                buttonADownAt = 0;
                buttonALongPressChecked = false;
                buttonALongActivated = false;
            }
            if ((pressed & ButtonB) != 0) back();
            if ((pressed & LeftShoulder) != 0) changeTab(-1);
            if ((pressed & RightShoulder) != 0) changeTab(1);

            bool tdpChord = gamepad.LeftTrigger >= TriggerThreshold && gamepad.RightTrigger >= TriggerThreshold;
            ushort heldDirections = (ushort)(buttons & DirectionButtons);
            if (tdpChord && heldDirections != 0) suppressDirectionsUntilRelease = true;
            if (heldDirections == 0) suppressDirectionsUntilRelease = false;
            ushort direction = tdpChord || suppressDirectionsUntilRelease
                ? (ushort)0
                : FirstDirection(heldDirections);
            if (direction != 0 && (direction != repeatingDirection || now >= nextRepeatAt))
            {
                navigate(DirectionToKey(direction));
                nextRepeatAt = now + (direction == repeatingDirection ? 125 : 430);
                repeatingDirection = direction;
            }
            else if (direction == 0)
            {
                repeatingDirection = 0;
            }

            previousButtons = buttons;
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
            catch (DllNotFoundException) { Enabled = false; }
            catch (EntryPointNotFoundException) { Enabled = false; }
            return false;
        }

        private static ushort FirstDirection(ushort buttons)
        {
            if ((buttons & DPadUp) != 0) return DPadUp;
            if ((buttons & DPadDown) != 0) return DPadDown;
            if ((buttons & DPadLeft) != 0) return DPadLeft;
            if ((buttons & DPadRight) != 0) return DPadRight;
            return 0;
        }

        private static Keys DirectionToKey(ushort direction) => direction switch
        {
            DPadUp => Keys.Up,
            DPadDown => Keys.Down,
            DPadLeft => Keys.Left,
            _ => Keys.Right
        };

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                timer.Dispose();
            base.Dispose(disposing);
        }

        [DllImport("xinput1_4.dll")]
        private static extern uint XInputGetState(uint userIndex, out XInputState state);

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

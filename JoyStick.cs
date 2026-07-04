using Coroutine;
using GameHelper;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using WFollowBot.Settings;

namespace WFollowBotCore
{
    public class JoyStick : IDisposable
    {
        public int ControllerIndex { get; }

        public JoyStick(int index = 0)
        {
            ControllerIndex = index;
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private static bool IsKeyDown(int vk) => (GetAsyncKeyState(vk) & 0x8000) != 0;

        private static readonly Dictionary<Xbox360Button, string> ButtonLabels = new()
        {
            { Xbox360Button.A,             "A"      },
            { Xbox360Button.B,             "B"      },
            { Xbox360Button.X,             "X"      },
            { Xbox360Button.Y,             "Y"      },
            { Xbox360Button.Up,            "D-Up"   },
            { Xbox360Button.Down,          "D-Down" },
            { Xbox360Button.Left,          "D-Left" },
            { Xbox360Button.Right,         "D-Right"},
            { Xbox360Button.Start,         "Start"  },
            { Xbox360Button.Back,          "Back"   },
            { Xbox360Button.Guide,         "Guide"  },
            { Xbox360Button.LeftShoulder,  "LB"     },
            { Xbox360Button.RightShoulder, "RB"     },
            { Xbox360Button.LeftThumb,     "L3"     },
            { Xbox360Button.RightThumb,    "R3"     },
        };

        private static string FormatButton(Xbox360Button btn) =>
            ButtonLabels.TryGetValue(btn, out var label) ? label : btn.ToString();

        // ══════════════════════════════════════════════════
        //  Key name ↔ VK code resolution
        // ══════════════════════════════════════════════════

        private static readonly Dictionary<string, int> SpecialKeyNames = new()
        {
            ["ArrowUp"] = 0x26,
            ["ArrowDown"] = 0x28,
            ["ArrowLeft"] = 0x25,
            ["ArrowRight"] = 0x27,
            ["Tab"] = 0x09,
            ["Enter"] = 0x0D,
            ["Space"] = 0x20,
            ["Backspace"] = 0x08,
            ["Escape"] = 0x1B,
            ["CapsLock"] = 0x14,
            ["LShift"] = 0xA0,
            ["RShift"] = 0xA1,
            ["LControl"] = 0xA2,
            ["RControl"] = 0xA3,
            ["LAlt"] = 0xA4,
            ["RAlt"] = 0xA5,
            ["Home"] = 0x24,
            ["End"] = 0x23,
            ["PageUp"] = 0x21,
            ["PageDown"] = 0x22,
            ["Insert"] = 0x2D,
            ["Delete"] = 0x2E,
            ["F1"] = 0x70,
            ["F2"] = 0x71,
            ["F3"] = 0x72,
            ["F4"] = 0x73,
            ["F5"] = 0x74,
            ["F6"] = 0x75,
            ["F7"] = 0x76,
            ["F8"] = 0x77,
            ["F9"] = 0x78,
            ["F10"] = 0x79,
            ["F11"] = 0x7A,
            ["F12"] = 0x7B,
            ["NumLock"] = 0x90,
            ["ScrollLock"] = 0x91,
            ["NumPad0"] = 0x60,
            ["NumPad1"] = 0x61,
            ["NumPad2"] = 0x62,
            ["NumPad3"] = 0x63,
            ["NumPad4"] = 0x64,
            ["NumPad5"] = 0x65,
            ["NumPad6"] = 0x66,
            ["NumPad7"] = 0x67,
            ["NumPad8"] = 0x68,
            ["NumPad9"] = 0x69,
        };

        private static int ResolveKeyName(string name)
        {
            if (string.IsNullOrEmpty(name)) return 0;
            if (name.Length == 1 && char.IsLetterOrDigit(name[0]))
                return char.ToUpperInvariant(name[0]);
            return SpecialKeyNames.TryGetValue(name, out var vk) ? vk : 0;
        }

        private static readonly Dictionary<int, string> VkToSpecialName =
            SpecialKeyNames.ToDictionary(kv => kv.Value, kv => kv.Key);

        private string GetKeyName(int vk)
        {
            if (vk == 0) return "-";
            if (vk >= 'A' && vk <= 'Z') return ((char)vk).ToString();
            if (vk >= '0' && vk <= '9') return ((char)vk).ToString();
            return VkToSpecialName.TryGetValue(vk, out var name) ? name : $"0x{vk:X}";
        }

        // ══════════════════════════════════════════════════
        //  Instance binding tables (set by ApplyLayout)
        // ══════════════════════════════════════════════════

        private readonly Dictionary<int, Xbox360Button> _vkToButton = new();
        private readonly Dictionary<Xbox360Button, int> _buttonToVk = new();
        private int _vkStickUp, _vkStickDown, _vkStickLeft, _vkStickRight;
        private int _vkRStickUp, _vkRStickDown, _vkRStickLeft, _vkRStickRight;
        private int _vkTriggerLeft, _vkTriggerRight;

        public void ApplyLayout(KeyboardLayout? layout)
        {
            layout ??= new KeyboardLayout();
            _vkToButton.Clear();
            _buttonToVk.Clear();

            void AddBinding(string keyName, Xbox360Button btn)
            {
                var vk = ResolveKeyName(keyName);
                if (vk == 0) return;
                _vkToButton[vk] = btn;
                _buttonToVk[btn] = vk;
            }

            AddBinding(layout.KeyDPadUp, Xbox360Button.Up);
            AddBinding(layout.KeyDPadDown, Xbox360Button.Down);
            AddBinding(layout.KeyDPadLeft, Xbox360Button.Left);
            AddBinding(layout.KeyDPadRight, Xbox360Button.Right);
            AddBinding(layout.KeyA, Xbox360Button.A);
            AddBinding(layout.KeyB, Xbox360Button.B);
            AddBinding(layout.KeyX, Xbox360Button.X);
            AddBinding(layout.KeyY, Xbox360Button.Y);
            AddBinding(layout.KeyLB, Xbox360Button.LeftShoulder);
            AddBinding(layout.KeyRB, Xbox360Button.RightShoulder);
            AddBinding(layout.KeyBack, Xbox360Button.Back);
            AddBinding(layout.KeyStart, Xbox360Button.Start);
            AddBinding(layout.KeyGuide, Xbox360Button.Guide);
            AddBinding(layout.KeyL3, Xbox360Button.LeftThumb);
            AddBinding(layout.KeyR3, Xbox360Button.RightThumb);

            _vkStickUp = ResolveKeyName(layout.KeyLStickUp);
            _vkStickDown = ResolveKeyName(layout.KeyLStickDown);
            _vkStickLeft = ResolveKeyName(layout.KeyLStickLeft);
            _vkStickRight = ResolveKeyName(layout.KeyLStickRight);
            _vkRStickUp = ResolveKeyName(layout.KeyRStickUp);
            _vkRStickDown = ResolveKeyName(layout.KeyRStickDown);
            _vkRStickLeft = ResolveKeyName(layout.KeyRStickLeft);
            _vkRStickRight = ResolveKeyName(layout.KeyRStickRight);
            _vkTriggerLeft = ResolveKeyName(layout.KeyLT);
            _vkTriggerRight = ResolveKeyName(layout.KeyRT);
        }

        public string[] GetBindingLabels()
        {
            var labels = new List<string>();
            foreach (var kv in _vkToButton)
                labels.Add($"{GetKeyName(kv.Key),-8} → {FormatButton(kv.Value)}");
            labels.Add($"{GetKeyName(_vkStickUp),-8} → LS Up");
            labels.Add($"{GetKeyName(_vkStickDown),-8} → LS Down");
            labels.Add($"{GetKeyName(_vkStickLeft),-8} → LS Left");
            labels.Add($"{GetKeyName(_vkStickRight),-8} → LS Right");
            labels.Add($"{GetKeyName(_vkRStickUp),-8} → RS Up");
            labels.Add($"{GetKeyName(_vkRStickDown),-8} → RS Down");
            labels.Add($"{GetKeyName(_vkRStickLeft),-8} → RS Left");
            labels.Add($"{GetKeyName(_vkRStickRight),-8} → RS Right");
            labels.Add($"{GetKeyName(_vkTriggerLeft),-8} → LT");
            labels.Add($"{GetKeyName(_vkTriggerRight),-8} → RT");
            return labels.ToArray();
        }

        // ══════════════════════════════════════════════════
        //  Private fields
        // ══════════════════════════════════════════════════

        // ViGEm
        private ViGEmClient? _vigemClient;
        private IXbox360Controller? _controller;
        private readonly object _submitLock = new();

        // Controller state — only pressed buttons are stored (value = true).
        // Axes / sliders always have their latest value.
        private readonly ConcurrentDictionary<Xbox360Button, bool> _buttons = new();
        private readonly ConcurrentDictionary<Xbox360Axis, short> _axes = new();
        private readonly ConcurrentDictionary<Xbox360Slider, byte> _sliders = new();

        // Keep-alive + keyboard polling
        private Thread? _keepAliveThread;
        private volatile bool _running;

        // Keyboard passthrough
        private volatile bool _keyboardEnabled;
        private short _stickDeflection = 32767;
        private readonly HashSet<int> _prevKeysDown = new();
        private readonly object _kbLock = new();

        // ══════════════════════════════════════════════════
        //  Public properties
        // ══════════════════════════════════════════════════

        public bool IsRunning => _running;
        public bool ControllerReady => _running && _controller != null;

        // Keyboard
        public bool KeyboardEnabled
        {
            get => _keyboardEnabled;
            set
            {
                _keyboardEnabled = value;
                if (!value)
                {
                    lock (_kbLock)
                    {
                        foreach (var kv in _vkToButton)
                            _buttons.TryRemove(kv.Value, out _);
                        _axes.TryRemove(Xbox360Axis.LeftThumbX, out _);
                        _axes.TryRemove(Xbox360Axis.LeftThumbY, out _);
                        _axes.TryRemove(Xbox360Axis.RightThumbX, out _);
                        _axes.TryRemove(Xbox360Axis.RightThumbY, out _);
                        _sliders.TryRemove(Xbox360Slider.LeftTrigger, out _);
                        _sliders.TryRemove(Xbox360Slider.RightTrigger, out _);
                        _prevKeysDown.Clear();
                    }
                    SubmitState();
                    StateChanged?.Invoke();
                }
            }
        }

        /// <summary>Stick axis deflection magnitude for keyboard input (0–32767).</summary>
        public short StickDeflection
        {
            get => _stickDeflection;
            set => _stickDeflection = (short)Math.Clamp(value, (short)0, (short)32767);
        }

        // State readers
        public bool IsPressed(Xbox360Button button) =>
            _buttons.TryGetValue(button, out var v) && v;

        public short GetAxis(Xbox360Axis axis) =>
            _axes.TryGetValue(axis, out var v) ? v : (short)0;

        public byte GetSlider(Xbox360Slider slider) =>
            _sliders.TryGetValue(slider, out var v) ? v : (byte)0;

        public HashSet<Xbox360Button> GetActiveButtons() =>
            _buttons.Where(kv => kv.Value).Select(kv => kv.Key).ToHashSet();

        /// <summary>Raised after any state change (local or keyboard).</summary>
        public event Action? StateChanged;

        // ══════════════════════════════════════════════════
        //  Lifecycle
        // ══════════════════════════════════════════════════

        /// <summary>Start the ViGEm controller.</summary>
        /// <returns>false if ViGEm driver is not installed or controller creation failed.</returns>
        public bool Start()
        {
            if (_running) return true;

            try
            {
                _vigemClient = new ViGEmClient();
                _controller = _vigemClient.CreateXbox360Controller();
                _controller.Connect();
                _controller.AutoSubmitReport = false;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[JoyStick] ViGEm init failed: {ex.Message}");
                _controller = null;
                _vigemClient?.Dispose();
                _vigemClient = null;
                return false;
            }

            _running = true;

            _keepAliveThread = new Thread(KeepAliveLoop)
            {
                IsBackground = true,
                Name = "JoyStick-KeepAlive"
            };
            _keepAliveThread.Start();

            return true;
        }

        /// <summary>Disconnect the controller and release all resources.</summary>
        public void Stop()
        {
            if (!_running) return;
            _running = false;
            _keyboardEnabled = false;

            _keepAliveThread?.Join(500);

            lock (_submitLock)
            {
                try { _controller?.Disconnect(); } catch { }
                _controller = null;
            }
            _vigemClient?.Dispose();
            _vigemClient = null;

            _buttons.Clear();
            _axes.Clear();
            _sliders.Clear();
            lock (_kbLock) _prevKeysDown.Clear();
        }

        public void Dispose() => Stop();

        // ══════════════════════════════════════════════════
        //  Direct control API
        // ══════════════════════════════════════════════════

        public void PressButton(Xbox360Button button, bool state)
        {
            if (!_running) return;
            if (state) _buttons[button] = true;
            else _buttons.TryRemove(button, out _);
            SubmitState();
            StateChanged?.Invoke();
        }

        public void SetAxis(Xbox360Axis axis, short value)
        {
            if (!_running) return;
            _axes[axis] = value;
            SubmitState();
            StateChanged?.Invoke();
        }

        public void SetSlider(Xbox360Slider slider, byte value)
        {
            if (!_running) return;
            _sliders[slider] = value;
            SubmitState();
            StateChanged?.Invoke();
        }

        public void Reset()
        {
            if (!_running) return;
            _buttons.Clear();
            _axes.Clear();
            _sliders.Clear();
            SubmitState();
            StateChanged?.Invoke();
        }

        // ══════════════════════════════════════════════════
        //  Test sequences (coroutines)
        // ══════════════════════════════════════════════════

        public void TestButton(Xbox360Button button, float duration = 0.2f) =>
            Core.CoroutinesRegistrar.Add(CoroutineHandler.Start(TestButtonCoroutine(button, duration)));

        public void TestAllButtons(float delay = 0.15f) =>
            Core.CoroutinesRegistrar.Add(CoroutineHandler.Start(TestAllButtonsCoroutine(delay)));

        public void TestDPadRotate(float delay = 0.2f) =>
            Core.CoroutinesRegistrar.Add(CoroutineHandler.Start(DPadRotateCoroutine(delay)));

        public void TestTriggerPulse(float duration = 0.4f) =>
            Core.CoroutinesRegistrar.Add(CoroutineHandler.Start(TriggerPulseCoroutine(duration)));

        public void TestAxisSweep(Xbox360Axis axis, float duration = 0.5f) =>
            Core.CoroutinesRegistrar.Add(CoroutineHandler.Start(AxisSweepCoroutine(axis, duration)));

        // ── Coroutine implementations ──

        private IEnumerable<Wait> TestButtonCoroutine(Xbox360Button button, float duration)
        {
            PressButton(button, true);
            yield return new Wait(duration);
            PressButton(button, false);
        }

        private IEnumerable<Wait> TestAllButtonsCoroutine(float delay)
        {
            var buttons = new[]
            {
                Xbox360Button.A, Xbox360Button.B, Xbox360Button.X, Xbox360Button.Y,
                Xbox360Button.LeftShoulder, Xbox360Button.RightShoulder,
                Xbox360Button.Back, Xbox360Button.Start, Xbox360Button.Guide,
                Xbox360Button.LeftThumb, Xbox360Button.RightThumb,
                Xbox360Button.Up, Xbox360Button.Down, Xbox360Button.Left, Xbox360Button.Right,
            };
            foreach (var btn in buttons)
            {
                PressButton(btn, true);
                yield return new Wait(delay);
                PressButton(btn, false);
                yield return new Wait(delay * 0.3f);
            }
        }

        private IEnumerable<Wait> DPadRotateCoroutine(float delay)
        {
            var dirs = new (Xbox360Button btn, bool on)[]
            {
                (Xbox360Button.Up,    true ),
                (Xbox360Button.Right, true ), (Xbox360Button.Up,    false),
                (Xbox360Button.Down,  true ), (Xbox360Button.Right, false),
                (Xbox360Button.Left,  true ), (Xbox360Button.Down,  false),
                (Xbox360Button.Up,    true ), (Xbox360Button.Left,  false),
                (Xbox360Button.Up,    false),
            };
            foreach (var (btn, on) in dirs)
            {
                PressButton(btn, on);
                yield return new Wait(delay);
            }
        }

        private IEnumerable<Wait> TriggerPulseCoroutine(float duration)
        {
            const int steps = 30;
            var stepTime = duration / steps;
            for (int i = 0; i <= steps; i++)
            {
                var val = (byte)(i * 255 / steps);
                SetSlider(Xbox360Slider.LeftTrigger, val);
                SetSlider(Xbox360Slider.RightTrigger, val);
                yield return new Wait(stepTime);
            }
            SetSlider(Xbox360Slider.LeftTrigger, 0);
            SetSlider(Xbox360Slider.RightTrigger, 0);
        }

        private IEnumerable<Wait> AxisSweepCoroutine(Xbox360Axis axis, float duration)
        {
            const int steps = 40;
            var stepTime = duration / steps;
            for (int i = 0; i <= steps; i++)
            {
                SetAxis(axis, (short)(-32768 + i * 65535 / steps));
                yield return new Wait(stepTime);
            }
            SetAxis(axis, 0);
        }

        // ══════════════════════════════════════════════════
        //  Internal: state submission
        // ══════════════════════════════════════════════════

        private void SubmitState()
        {
            var ctrl = _controller;
            if (ctrl == null || !_running) return;

            lock (_submitLock)
            {
                if (!_running) return;
                ctrl.ResetReport();

                foreach (var kv in _buttons)
                    ctrl.SetButtonState(kv.Key, true);

                foreach (var kv in _axes)
                    ctrl.SetAxisValue(kv.Key, kv.Value);

                foreach (var kv in _sliders)
                    ctrl.SetSliderValue(kv.Key, kv.Value);

                ctrl.SubmitReport();
            }
        }

        // ══════════════════════════════════════════════════
        //  Internal: keep-alive + keyboard polling thread
        // ══════════════════════════════════════════════════

        private void KeepAliveLoop()
        {
            while (_running)
            {
                if (_keyboardEnabled)
                    PollKeyboard();

                SubmitState();
                Thread.Sleep(16); // ~60 fps
            }
        }

        // ══════════════════════════════════════════════════
        //  Internal: keyboard polling
        // ══════════════════════════════════════════════════

        private void PollKeyboard()
        {
            var currentKeysDown = new HashSet<int>();
            var changed = false;

            foreach (var (vk, button) in _vkToButton)
            {
                var down = IsKeyDown(vk);
                if (down) currentKeysDown.Add(vk);

                bool wasDown;
                lock (_kbLock)
                    wasDown = _prevKeysDown.Contains(vk);

                if (down && !wasDown)
                {
                    _buttons[button] = true;
                    changed = true;
                }
                else if (!down && wasDown)
                {
                    _buttons.TryRemove(button, out _);
                    changed = true;
                }
            }

            {
                short lx = 0, ly = 0;
                if (_vkStickUp != 0 && IsKeyDown(_vkStickUp)) ly += _stickDeflection;
                if (_vkStickDown != 0 && IsKeyDown(_vkStickDown)) ly -= _stickDeflection;
                if (_vkStickLeft != 0 && IsKeyDown(_vkStickLeft)) lx -= _stickDeflection;
                if (_vkStickRight != 0 && IsKeyDown(_vkStickRight)) lx += _stickDeflection;

                var stickKeys = new[] { _vkStickUp, _vkStickDown, _vkStickLeft, _vkStickRight };

                if (lx != 0 || ly != 0)
                {
                    _axes[Xbox360Axis.LeftThumbX] = lx;
                    _axes[Xbox360Axis.LeftThumbY] = ly;
                    foreach (var k in stickKeys)
                        if (k != 0) currentKeysDown.Add(k);
                    changed = true;
                }
                else
                {
                    bool wasStickActive;
                    lock (_kbLock)
                        wasStickActive = stickKeys.Any(k => k != 0 && _prevKeysDown.Contains(k));
                    if (wasStickActive)
                    {
                        _axes.TryRemove(Xbox360Axis.LeftThumbX, out _);
                        _axes.TryRemove(Xbox360Axis.LeftThumbY, out _);
                        changed = true;
                    }
                }
            }

            {
                short rx = 0, ry = 0;
                if (_vkRStickUp != 0 && IsKeyDown(_vkRStickUp)) ry += _stickDeflection;
                if (_vkRStickDown != 0 && IsKeyDown(_vkRStickDown)) ry -= _stickDeflection;
                if (_vkRStickLeft != 0 && IsKeyDown(_vkRStickLeft)) rx -= _stickDeflection;
                if (_vkRStickRight != 0 && IsKeyDown(_vkRStickRight)) rx += _stickDeflection;

                var rStickKeys = new[] { _vkRStickUp, _vkRStickDown, _vkRStickLeft, _vkRStickRight };

                if (rx != 0 || ry != 0)
                {
                    _axes[Xbox360Axis.RightThumbX] = rx;
                    _axes[Xbox360Axis.RightThumbY] = ry;
                    foreach (var k in rStickKeys)
                        if (k != 0) currentKeysDown.Add(k);
                    changed = true;
                }
                else
                {
                    bool wasRStickActive;
                    lock (_kbLock)
                        wasRStickActive = rStickKeys.Any(k => k != 0 && _prevKeysDown.Contains(k));
                    if (wasRStickActive)
                    {
                        _axes.TryRemove(Xbox360Axis.RightThumbX, out _);
                        _axes.TryRemove(Xbox360Axis.RightThumbY, out _);
                        changed = true;
                    }
                }
            }

            if (_vkTriggerLeft != 0)
                changed |= UpdateTrigger(_vkTriggerLeft, Xbox360Slider.LeftTrigger, currentKeysDown);
            if (_vkTriggerRight != 0)
                changed |= UpdateTrigger(_vkTriggerRight, Xbox360Slider.RightTrigger, currentKeysDown);

            lock (_kbLock)
            {
                _prevKeysDown.Clear();
                foreach (var vk in currentKeysDown)
                    _prevKeysDown.Add(vk);
            }

            if (changed)
                StateChanged?.Invoke();
        }

        private bool UpdateTrigger(int vk, Xbox360Slider slider, HashSet<int> currentKeysDown)
        {
            if (IsKeyDown(vk))
            {
                _sliders[slider] = 255;
                currentKeysDown.Add(vk);
                return true;
            }

            bool wasPressed;
            lock (_kbLock)
                wasPressed = _prevKeysDown.Contains(vk);

            if (wasPressed)
            {
                _sliders.TryRemove(slider, out _);
                return true;
            }
            return false;
        }
    }
}
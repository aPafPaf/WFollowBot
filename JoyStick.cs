using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Coroutine;
using GameHelper;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
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

        private static string FormatButton(Xbox360Button btn) => FormatButtonLabel(btn);

        /// <summary>
        /// Every <see cref="Xbox360Button"/> singleton exposed by
        /// Nefarius.ViGEm.Client. The library models buttons as a static
        /// class with <c>static readonly</c> fields (type-safe enum
        /// pattern), so <c>Enum.GetValues</c> cannot enumerate them.
        /// Cached once for O(1) iteration in <see cref="GetCooldownButtons"/>
        /// and other diagnostic paths.
        /// </summary>
        public static readonly Xbox360Button[] AllButtons =
        {
            Xbox360Button.A, Xbox360Button.B, Xbox360Button.X, Xbox360Button.Y,
            Xbox360Button.Up, Xbox360Button.Down, Xbox360Button.Left, Xbox360Button.Right,
            Xbox360Button.Start, Xbox360Button.Back, Xbox360Button.Guide,
            Xbox360Button.LeftShoulder, Xbox360Button.RightShoulder,
            Xbox360Button.LeftThumb, Xbox360Button.RightThumb,
        };

        // ══════════════════════════════════════════════════
        //  Key name ↔ VK code resolution
        // ══════════════════════════════════════════════════

        private static readonly Dictionary<string, int> SpecialKeyNames = new()
        {
            ["ArrowUp"] = 0x26,    ["ArrowDown"] = 0x28,
            ["ArrowLeft"] = 0x25,  ["ArrowRight"] = 0x27,
            ["Tab"] = 0x09,        ["Enter"] = 0x0D,
            ["Space"] = 0x20,      ["Backspace"] = 0x08,
            ["Escape"] = 0x1B,     ["CapsLock"] = 0x14,
            ["LShift"] = 0xA0,     ["RShift"] = 0xA1,
            ["LControl"] = 0xA2,   ["RControl"] = 0xA3,
            ["LAlt"] = 0xA4,       ["RAlt"] = 0xA5,
            ["Home"] = 0x24,       ["End"] = 0x23,
            ["PageUp"] = 0x21,     ["PageDown"] = 0x22,
            ["Insert"] = 0x2D,     ["Delete"] = 0x2E,
            ["F1"] = 0x70, ["F2"] = 0x71, ["F3"] = 0x72, ["F4"] = 0x73,
            ["F5"] = 0x74, ["F6"] = 0x75, ["F7"] = 0x76, ["F8"] = 0x77,
            ["F9"] = 0x78, ["F10"] = 0x79, ["F11"] = 0x7A, ["F12"] = 0x7B,
            ["NumLock"] = 0x90,    ["ScrollLock"] = 0x91,
            ["NumPad0"] = 0x60,    ["NumPad1"] = 0x61,
            ["NumPad2"] = 0x62,    ["NumPad3"] = 0x63,
            ["NumPad4"] = 0x64,    ["NumPad5"] = 0x65,
            ["NumPad6"] = 0x66,    ["NumPad7"] = 0x67,
            ["NumPad8"] = 0x68,    ["NumPad9"] = 0x69,
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

        // Pending timed auto-release holds. The value carries both the
        // coroutine that will fire PressButton(button, false) once its Wait
        // elapses AND the UTC timestamp of when the hold started (used by
        // the debug overlay to display hold age in seconds).
        // Touched only from the ImGui render thread (PressButtonFor + the
        // coroutine body via OnFinished), so the ConcurrentDictionary is
        // just defensive — single-thread access in normal operation.
        private readonly ConcurrentDictionary<Xbox360Button, HoldEntry> _pendingHolds = new();

        private readonly struct HoldEntry
        {
            public readonly ActiveCoroutine Coroutine;
            public readonly DateTime StartedAtUtc;
            public HoldEntry(ActiveCoroutine coroutine, DateTime startedAtUtc)
            {
                Coroutine = coroutine;
                StartedAtUtc = startedAtUtc;
            }
        }

        // Per-button rate-limit window. A press is rejected if
        // DateTime.UtcNow < _cooldownUntilUtc[button]. Anchored to PRESS
        // time (not release time) so TimedHold.Cancel() / external
        // PressButton(false) do not shorten the cooldown.
        private readonly ConcurrentDictionary<Xbox360Button, DateTime> _cooldownUntilUtc = new();

        // UTC timestamp of the most recent accepted press on each button.
        // Set alongside _cooldownUntilUtc on every tracked press (never by
        // the legacy 2-arg override). Persists until replaced by the next
        // press; cleared by Reset() / Stop(). The debug overlay uses this
        // to display "pressed N ms ago" with sub-frame precision.
        private readonly ConcurrentDictionary<Xbox360Button, DateTime> _lastPressAtUtc = new();

        // Source tag of the most recent accepted press on each button.
        // Persists indefinitely until replaced by the next press; cleared
        // by Reset() / Stop(). Used by the debug overlay only — gameplay
        // logic does not read it. Bounded by the number of distinct
        // buttons (15), so no lazy cleanup needed.
        private readonly ConcurrentDictionary<Xbox360Button, string?> _lastSource = new();

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
            _pendingHolds.Clear();
            _cooldownUntilUtc.Clear();
            _lastPressAtUtc.Clear();
            _lastSource.Clear();
            lock (_kbLock) _prevKeysDown.Clear();
        }

        public void Dispose() => Stop();

        // ══════════════════════════════════════════════════
        //  Direct control API
        // ══════════════════════════════════════════════════

        /// <summary>
        /// Direct press with no rate-limiting or source tracking. This is
        /// a full operator override: it always succeeds (controller
        /// running), never touches <c>_cooldownUntilUtc</c>, and never
        /// updates <c>_lastSource</c>. Reserved for test sequences, the
        /// keyboard passthrough, and the auto-release coroutine itself.
        /// </summary>
        public void PressButton(Xbox360Button button, bool state)
        {
            if (!_running) return;
            if (state) _buttons[button] = true;
            else _buttons.TryRemove(button, out _);
            SubmitState();
            StateChanged?.Invoke();
        }

        /// <summary>
        /// Tracked press: respects the cooldown (silent reject if
        /// <paramref name="button"/> is currently on cooldown) and
        /// records <paramref name="source"/> as the most recent press
        /// source for the button. Does not update the cooldown window
        /// itself — for that, use the 4-arg overload.
        /// </summary>
        public void PressButton(Xbox360Button button, bool state, string? source)
        {
            if (!_running) return;
            if (state)
            {
                if (IsOnCooldown(button)) return;
                _lastSource[button] = source;
                _lastPressAtUtc[button] = DateTime.UtcNow;
                _buttons[button] = true;
            }
            else
            {
                _buttons.TryRemove(button, out _);
            }
            SubmitState();
            StateChanged?.Invoke();
        }

        /// <summary>
        /// Fully-tracked press: respects the cooldown, sets a new
        /// cooldown window of <paramref name="cooldown"/> from now, and
        /// records <paramref name="source"/>. The new cooldown is
        /// <c>now + max(duration-style hold, cooldown)</c> at the
        /// caller's discretion; here we only know the cooldown.
        /// </summary>
        public void PressButton(Xbox360Button button, bool state, TimeSpan cooldown, string? source)
        {
            if (!_running) return;
            if (state)
            {
                if (IsOnCooldown(button)) return;
                var now = DateTime.UtcNow;
                if (cooldown > TimeSpan.Zero) _cooldownUntilUtc[button] = now + cooldown;
                _lastPressAtUtc[button] = now;
                _lastSource[button] = source;
                _buttons[button] = true;
            }
            else
            {
                _buttons.TryRemove(button, out _);
            }
            SubmitState();
            StateChanged?.Invoke();
        }

        // ══════════════════════════════════════════════════
        //  Timed holds (auto-release after a duration)
        // ══════════════════════════════════════════════════

        /// <summary>
        /// Press <paramref name="button"/> now and auto-release it after
        /// <paramref name="seconds"/>. If a hold on the same button is
        /// already pending it is cancelled and replaced (replace-on-re-press).
        /// Direct <see cref="PressButton(Xbox360Button,bool)"/> calls continue
        /// to work as an immediate override; the scheduled release becomes a
        /// idempotent no-op if the button was already released externally.
        /// </summary>
        /// <returns>
        /// Opaque handle for early cancellation, or <c>null</c> if the call
        /// was a no-op (controller not running, or duration non-positive).
        /// </returns>
        public TimedHold? PressButtonFor(Xbox360Button button, float seconds) =>
            PressButtonFor(button, TimeSpan.FromSeconds(seconds));

        /// <summary>
        /// Float-seconds convenience overload with explicit cooldown and
        /// source. Use this from actions to rate-limit the press and tag
        /// the call site for debug visibility.
        /// </summary>
        public TimedHold? PressButtonFor(
            Xbox360Button button, float seconds, float cooldownSeconds, string? source = null) =>
            PressButtonFor(
                button,
                TimeSpan.FromSeconds(seconds),
                TimeSpan.FromSeconds(cooldownSeconds),
                source);

        /// <summary>
        /// <see cref="PressButtonFor(Xbox360Button,float)"/> overload taking
        /// a <see cref="TimeSpan"/>. Behaviour and return value are identical.
        /// </summary>
        public TimedHold? PressButtonFor(Xbox360Button button, TimeSpan duration) =>
            PressButtonFor(button, duration, TimeSpan.Zero, source: null);

        /// <summary>
        /// <see cref="PressButtonFor(Xbox360Button,TimeSpan,TimeSpan,string)"/>
        /// overload omitting the source. Equivalent to passing
        /// <c>source: null</c> (the debug overlay will render the press as
        /// <c>(unknown)</c>).
        /// </summary>
        public TimedHold? PressButtonFor(
            Xbox360Button button, TimeSpan duration, TimeSpan cooldown) =>
            PressButtonFor(button, duration, cooldown, source: null);

        /// <summary>
        /// Canonical timed-hold entry point. Presses <paramref name="button"/>
        /// now, auto-releases after <paramref name="duration"/>, and tags
        /// the call as <paramref name="source"/> for the debug overlay.
        /// The cooldown is anchored to PRESS time (not release time); the
        /// rate-limit window is <c>now + max(duration, cooldown)</c>, so:
        /// <list type="bullet">
        /// <item>If <paramref name="cooldown"/> ≤ <paramref name="duration"/>,
        /// the next press can fire immediately after the auto-release.</item>
        /// <item>If <paramref name="cooldown"/> &gt; <paramref name="duration"/>,
        /// a "rest" period is enforced between consecutive holds.</item>
        /// </list>
        /// If a press is rejected because <paramref name="button"/> is still
        /// on cooldown, <c>null</c> is returned and no state changes
        /// (the previous hold, if any, is left untouched).
        /// </summary>
        public TimedHold? PressButtonFor(
            Xbox360Button button, TimeSpan duration, TimeSpan cooldown, string? source)
        {
            if (!_running) return null;

            double totalSeconds = duration.TotalSeconds;
            if (!double.IsFinite(totalSeconds) || totalSeconds <= 0d) return null;

            // Cooldown gate: anchored to press time. Stale entries
            // (until <= now) are removed lazily to keep the dict bounded.
            if (_cooldownUntilUtc.TryGetValue(button, out var until) && until > DateTime.UtcNow)
                return null;
            else if (_cooldownUntilUtc.TryGetValue(button, out until))
                _cooldownUntilUtc.TryRemove(button, out _);

            // Replace any existing hold for this button. Cancel() is a
            // no-op if the coroutine is already finished, so safe to call
            // unconditionally after the dict remove.
            if (_pendingHolds.TryRemove(button, out var existing))
                existing.Coroutine.Cancel();

            PressButton(button, true);

            var now = DateTime.UtcNow;
            var coroutine = CoroutineHandler.Start(
                TimedHoldCoroutine(button, totalSeconds),
                $"[JoyStick] TimedHold {FormatButtonLabel(button)}");
            Core.CoroutinesRegistrar.Add(coroutine);

            _pendingHolds[button] = new HoldEntry(coroutine, now);

            // Cooldown = max(duration, cooldown) so the next press can
            // only fire after the auto-release. Caller may pass cooldown
            // > duration to enforce a longer rest period between holds.
            var cooldownSpan = cooldown > duration ? cooldown : duration;
            _cooldownUntilUtc[button] = now + cooldownSpan;
            _lastPressAtUtc[button] = now;
            _lastSource[button] = source;

            // Belt-and-braces dict cleanup: fires whether the coroutine
            // completes naturally OR is cancelled (per ActiveCoroutine
            // contract, OnFinished is raised in both cases and IsFinished
            // is true at that point). The ReferenceEquals guard ensures
            // we don't evict a *newer* hold that already replaced us.
            coroutine.OnFinished += ac =>
            {
                if (_pendingHolds.TryGetValue(button, out var current) &&
                    ReferenceEquals(current.Coroutine, ac))
                {
                    _pendingHolds.TryRemove(button, out _);
                }
            };

            return new TimedHold(button, coroutine);
        }

        /// <summary>
        /// True iff a <see cref="PressButtonFor(Xbox360Button,float)"/> hold
        /// is currently scheduled and its auto-release has not yet fired
        /// (and was not cancelled). The underlying button bit may have
        /// been released earlier via
        /// <see cref="PressButton(Xbox360Button,bool)"/>; this only reports
        /// scheduler state.
        /// </summary>
        public bool HasPendingHold(Xbox360Button button) =>
            _pendingHolds.TryGetValue(button, out var entry) && !entry.Coroutine.IsFinished;

        /// <summary>
        /// True iff the button is currently reported as pressed to ViGEm
        /// OR a timed hold is still pending. Useful for "is anyone at all
        /// holding X right now" queries.
        /// </summary>
        public bool IsHeld(Xbox360Button button) =>
            IsPressed(button) || HasPendingHold(button);

        /// <summary>
        /// Returns the set of buttons with an active timed auto-release
        /// hold (i.e. <see cref="HasPendingHold"/> is true for each).
        /// Allocates a new set; intended for debug overlays, not hot paths.
        /// </summary>
        public HashSet<Xbox360Button> GetPendingHoldButtons()
        {
            var result = new HashSet<Xbox360Button>();
            foreach (var kv in _pendingHolds)
            {
                if (!kv.Value.Coroutine.IsFinished)
                    result.Add(kv.Key);
            }
            return result;
        }

        /// <summary>
        /// Returns the set of buttons currently on cooldown
        /// (<see cref="IsOnCooldown"/> is true). Scans every
        /// <see cref="Xbox360Button"/> value and lazily evicts
        /// stale entries from <c>_cooldownUntilUtc</c>. Bounded by the
        /// 15-button set, so O(1) for our purposes; the cost is
        /// acceptable for debug-overlay use.
        /// </summary>
        public HashSet<Xbox360Button> GetCooldownButtons()
        {
            var result = new HashSet<Xbox360Button>();
            foreach (Xbox360Button btn in AllButtons)
            {
                if (GetCooldownRemainingSeconds(btn) > 0.0)
                    result.Add(btn);
            }
            return result;
        }

        /// <summary>
        /// Seconds elapsed since the timed hold for <paramref name="button"/>
        /// was started. Returns -1.0 if the button has no active hold.
        /// Useful for the debug overlay to show how "fresh" a press is.
        /// </summary>
        public double GetHoldAgeSeconds(Xbox360Button button)
        {
            if (_pendingHolds.TryGetValue(button, out var entry) && !entry.Coroutine.IsFinished)
                return (DateTime.UtcNow - entry.StartedAtUtc).TotalSeconds;
            return -1.0;
        }

        /// <summary>
        /// Cancels every pending auto-release hold. Buttons currently held
        /// are NOT released — call <see cref="Reset"/> for that.
        /// </summary>
        /// <returns>Number of holds cancelled.</returns>
        public int CancelAllHolds()
        {
            int n = 0;
            foreach (var kv in _pendingHolds)
            {
                if (kv.Value.Coroutine.Cancel()) n++;
            }
            _pendingHolds.Clear();
            return n;
        }

        // ══════════════════════════════════════════════════
        //  Cooldown + source queries
        // ══════════════════════════════════════════════════

        /// <summary>
        /// Seconds remaining on the cooldown window for
        /// <paramref name="button"/>. Returns 0.0 if the button is not on
        /// cooldown. Cooldown is anchored to press time and is not
        /// shortened by <see cref="PressButton(Xbox360Button,bool)"/>
        /// releases or <see cref="TimedHold.Cancel"/>.
        /// </summary>
        public double GetCooldownRemainingSeconds(Xbox360Button button)
        {
            if (!_cooldownUntilUtc.TryGetValue(button, out var until)) return 0.0;
            var remaining = (until - DateTime.UtcNow).TotalSeconds;
            if (remaining <= 0.0)
            {
                _cooldownUntilUtc.TryRemove(button, out _);
                return 0.0;
            }
            return remaining;
        }

        /// <summary>
        /// True iff a new press on <paramref name="button"/> would be
        /// rejected by the cooldown. Cooldown is checked at the start of
        /// <see cref="PressButtonFor(Xbox360Button,TimeSpan,TimeSpan,string)"/>
        /// and the multi-arg <see cref="PressButton(Xbox360Button,bool,string)"/>
        /// overloads; the legacy 2-arg <see cref="PressButton(Xbox360Button,bool)"/>
        /// is a full override and ignores this flag.
        /// </summary>
        public bool IsOnCooldown(Xbox360Button button) =>
            GetCooldownRemainingSeconds(button) > 0.0;

        /// <summary>
        /// Source tag of the most recent accepted press on
        /// <paramref name="button"/>, or <c>null</c> if no press has been
        /// recorded (e.g. legacy 2-arg override, or <see cref="Reset"/>
        /// was called). Persists indefinitely until replaced by the next
        /// accepted press; cleared by <see cref="Reset"/> / <see cref="Stop"/>.
        /// </summary>
        public string? GetLastPressSource(Xbox360Button button) =>
            _lastSource.TryGetValue(button, out var src) ? src : null;

        /// <summary>
        /// UTC timestamp of the most recent accepted press on
        /// <paramref name="button"/>, or <see cref="DateTime.MinValue"/>
        /// if no press has been recorded. Useful for the debug overlay
        /// to display "pressed N ms ago" with sub-frame precision.
        /// </summary>
        public DateTime GetLastPressTimeUtc(Xbox360Button button) =>
            _lastPressAtUtc.TryGetValue(button, out var at) ? at : DateTime.MinValue;

        /// <summary>
        /// Forcibly clears the cooldown for <paramref name="button"/>.
        /// Does NOT release the button (use <see cref="PressButton(Xbox360Button,bool)"/>
        /// with <c>state: false</c>) and does NOT cancel any pending
        /// timed hold. Intended for tests, debug tools, and workflows
        /// where the caller has externally confirmed a press is safe
        /// (e.g. <see cref="TimedHold.Cancel"/> + immediate re-press).
        /// </summary>
        public void ResetCooldown(Xbox360Button button) =>
            _cooldownUntilUtc.TryRemove(button, out _);

        /// <summary>
        /// Forcibly clears cooldowns for all buttons. Equivalent to
        /// calling <see cref="ResetCooldown"/> per button. Does NOT
        /// touch pending holds or pressed bits.
        /// </summary>
        public void ResetAllCooldowns() => _cooldownUntilUtc.Clear();

        /// <summary>
        /// Short human-readable label for a button (e.g. "A", "D-Up", "LB").
        /// Falls back to the enum name for unmapped members. Static so the
        /// debug overlay can use it without owning a
        /// <see cref="JoyStick"/> instance.
        /// </summary>
        public static string FormatButtonLabel(Xbox360Button btn) =>
            ButtonLabels.TryGetValue(btn, out var label) ? label : btn.ToString();

        /// <summary>
        /// Compose a source tag of the form <c>"{action}"</c> or
        /// <c>"{action}.{detail}"</c>. Returns <c>"{action}"</c> when
        /// <paramref name="detail"/> is null or empty. Use this from
        /// action call sites for consistent source naming in the debug
        /// overlay (e.g. <c>FormatPressSource("Attack", skillName)</c>
        /// → <c>"Attack.Reave"</c>).
        /// </summary>
        public static string FormatPressSource(string action, string? detail = null) =>
            string.IsNullOrEmpty(detail) ? action : $"{action}.{detail}";

        /// <summary>
        /// Opaque handle returned by <see cref="PressButtonFor(Xbox360Button,float)"/>.
        /// Retaining it is only needed for early cancellation — the press
        /// always auto-releases on its own. <see cref="Cancel"/> is
        /// idempotent and safe to call multiple times or after the release
        /// has fired.
        /// </summary>
        public sealed class TimedHold
        {
            public Xbox360Button Button { get; }
            private readonly ActiveCoroutine _active;

            internal TimedHold(Xbox360Button button, ActiveCoroutine active)
            {
                Button = button;
                _active = active;
            }

            /// <summary>True while the auto-release has not yet fired and the coroutine was not cancelled.</summary>
            public bool IsActive => !_active.IsFinished;

            /// <summary>Cancels the pending auto-release. The button is NOT released by this call.</summary>
            public void Cancel() => _active.Cancel();
        }

        private IEnumerable<Wait> TimedHoldCoroutine(Xbox360Button button, double seconds)
        {
            yield return new Wait(seconds);
            // Natural completion: explicitly release the bit. If an
            // external caller already pressed false (or we were cancelled
            // and skipped past this line) PressButton's TryRemove on an
            // absent key is a harmless no-op. _running check guards
            // against firing release against a stopped controller.
            if (_running)
                PressButton(button, false);
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
            // Cancel pending timed holds FIRST so their natural-release
            // PressButton(false) cannot race with the Clear() below and
            // re-set a button bit that we are about to wipe.
            CancelAllHolds();
            _buttons.Clear();
            _axes.Clear();
            _sliders.Clear();
            _cooldownUntilUtc.Clear();
            _lastPressAtUtc.Clear();
            _lastSource.Clear();
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
using System.Text;

namespace MicroEngine.Input;

/// <summary>What an <see cref="ActionPrompt"/> shows: a key, a gamepad control, an on-screen control, or a plain label.</summary>
public enum PromptKind
{
    /// <summary>A keyboard key (<see cref="ActionPrompt.Key"/>).</summary>
    Key,
    /// <summary>A gamepad button (<see cref="ActionPrompt.Button"/>, named for <see cref="ActionPrompt.PadStyle"/>).</summary>
    PadButton,
    /// <summary>A direction of the left stick, a trigger or another gamepad axis.</summary>
    PadAxis,
    /// <summary>An on-screen touch control: a button's label, or a d-pad or joystick direction.</summary>
    Touch,
    /// <summary>A label set with <see cref="ActionPrompts.SetLabel"/>, or the action's name when nothing presses it on the device.</summary>
    Label,
}

/// <summary>
/// How to show one action to the player on one device: <c>"Z"</c> on a keyboard, <c>"A"</c> on an Xbox pad,
/// <c>"×"</c> on a PlayStation pad, the button's own label on a touch screen. <see cref="Text"/> is what to print;
/// <see cref="Kind"/> and the other members let a game draw a glyph instead (a key cap, a coloured pad button).
/// </summary>
/// <param name="Action">The action shown.</param>
/// <param name="Device">The device it is shown for.</param>
/// <param name="Kind">What <see cref="Text"/> stands for.</param>
/// <param name="Text">Short text for the control ("Z", "Space", "A", "←", "JUMP").</param>
public readonly record struct ActionPrompt(string Action, InputDevice Device, PromptKind Kind, string Text)
{
    /// <summary>The key, for <see cref="PromptKind.Key"/>.</summary>
    public Key Key { get; init; }
    /// <summary>The button, for <see cref="PromptKind.PadButton"/>.</summary>
    public PadButton Button { get; init; }
    /// <summary>The gamepad family the button is named for.</summary>
    public GamepadStyle PadStyle { get; init; }
    /// <summary>The touch control's id, for <see cref="PromptKind.Touch"/>.</summary>
    public string? Control { get; init; }

    /// <inheritdoc/>
    public override string ToString() => Text;
}

/// <summary>
/// Tells the player which control presses an action on the device they are using, so dialogues, tutorials, help
/// screens and hints never name a key the player does not have. The text follows the active device (the one that last
/// pressed an action, touch while the on-screen controls are shown), the action's current bindings (remapping included)
/// and the touch layout, and changes as soon as the player switches device: read it every frame, or when
/// <see cref="Version"/> changes. Games get one in <see cref="Core.Game.Prompts"/>.
/// </summary>
/// <example>
/// <code>
/// font.Draw(r, Prompts.Format("Press {attack} to swing the sword"), position, color);
/// // Keyboard: "Press Z to swing...", Xbox pad: "Press A...", PlayStation: "Press ×...", phone: "Press A..." (the touch button's label)
/// </code>
/// </example>
public sealed class ActionPrompts
{
    private readonly InputMap _input;
    private readonly VirtualControls? _touch;
    private readonly Dictionary<(string Action, InputDevice? Device), string> _labels = [];
    private (InputDevice Device, GamepadStyle Style) _seen;
    private int _version;

    /// <summary>Prompts for the actions of <paramref name="input"/> and the controls of <paramref name="touch"/> (optional).</summary>
    public ActionPrompts(InputMap input, VirtualControls? touch = null)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
        _touch = touch;
    }

    /// <summary>Forces prompts for one device (a "show keyboard controls" option, tests); null follows the player.</summary>
    public InputDevice? Override { get; set; }

    /// <summary>
    /// The device prompts are shown for: <see cref="Override"/>, else the device that last pressed an action; before any
    /// press (or after scripted input), touch when the on-screen controls are shown or the screen is touch-first, a
    /// gamepad when one is connected, otherwise the keyboard.
    /// </summary>
    public InputDevice Device
    {
        get
        {
            if (Override is { } forced and not (InputDevice.None or InputDevice.Other)) return forced;
            if (_input.LastDevice is InputDevice.Keyboard or InputDevice.Gamepad or InputDevice.Touch)
                return _input.LastDevice == InputDevice.Touch && _touch is null ? InputDevice.Keyboard : _input.LastDevice;
            if (_touch is { } t && (t.Shown || t.TouchFirst)) return InputDevice.Touch;
            return _input.PadConnected ? InputDevice.Gamepad : InputDevice.Keyboard;
        }
    }

    /// <summary>The gamepad family button names follow: <see cref="PadStyleOverride"/>, else the connected pad's.</summary>
    public GamepadStyle PadStyle => PadStyleOverride ?? _input.PadStyle;

    /// <summary>Forces button names of one gamepad family; null follows the connected pad.</summary>
    public GamepadStyle? PadStyleOverride { get; set; }

    /// <summary>
    /// Changes whenever the text of prompts may have changed (another device, another pad family), so a game that
    /// lays text out once (wrapping, measuring) knows when to do it again.
    /// </summary>
    public int Version
    {
        get
        {
            var now = (Device, PadStyle);
            if (now != _seen) { _seen = now; _version++; }
            return _version;
        }
    }

    /// <summary>
    /// Replaces the prompt of <paramref name="action"/> with <paramref name="text"/> on <paramref name="device"/>, or on
    /// every device when it is null ("tap", "the left side"). Null text removes the label.
    /// </summary>
    public void SetLabel(string action, InputDevice? device, string? text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        if (text is null) _labels.Remove((action, device)); else _labels[(action, device)] = text;
        _version++;
    }

    /// <summary>How to show <paramref name="action"/> on <paramref name="device"/> (default: <see cref="Device"/>): the first binding the device has.</summary>
    public ActionPrompt Get(string action, InputDevice? device = null) => All(action, device).FirstOrDefault(Fallback(action, device ?? Device));

    /// <summary>Every control that presses <paramref name="action"/> on the device, in binding order (an empty list when none does).</summary>
    public IReadOnlyList<ActionPrompt> All(string action, InputDevice? device = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        var d = device ?? Device;
        if (_labels.TryGetValue((action, d), out var label) || _labels.TryGetValue((action, null), out label))
            return [new ActionPrompt(action, d, PromptKind.Label, label)];
        var list = new List<ActionPrompt>();
        var bindings = _input.GetBindings(action);
        switch (d)
        {
            case InputDevice.Gamepad:
                var style = PadStyle;
                foreach (var b in bindings?.Buttons ?? [])
                    list.Add(new ActionPrompt(action, d, PromptKind.PadButton, ButtonName(b, style)) { Button = b, PadStyle = style });
                if (bindings?.Stick is { } stick and not InputMap.StickDirection.None)
                    list.Add(new ActionPrompt(action, d, PromptKind.PadAxis, "L" + Arrow(stick)) { PadStyle = style });
                foreach (var (axis, positive) in bindings?.Axes ?? [])
                    list.Add(new ActionPrompt(action, d, PromptKind.PadAxis, AxisName(axis, positive, style)) { PadStyle = style });
                break;
            case InputDevice.Touch:
                if (_touch is null) break;
                foreach (var c in _touch.Layout.Controls)
                    switch (c)
                    {
                        case TouchButton b when b.Action == action || b.Also.Contains(action):
                            list.Add(new ActionPrompt(action, d, PromptKind.Touch, b.Label is { Length: > 0 } l ? l : b.Id.ToUpperInvariant()) { Control = b.Id });
                            break;
                        case TouchDPad p when Direction(action, p.Left, p.Right, p.Up, p.Down) is { } dir:
                            list.Add(new ActionPrompt(action, d, PromptKind.Touch, Arrow(dir)) { Control = p.Id });
                            break;
                        case TouchStick s when Direction(action, s.Left, s.Right, s.Up, s.Down) is { } dir:
                            list.Add(new ActionPrompt(action, d, PromptKind.Touch, Arrow(dir)) { Control = s.Id });
                            break;
                    }
                // A touch button pressing the action first, so "confirm" shows A rather than a d-pad that also confirms.
                list.Sort((a, b) => (IsButton(a) ? 0 : 1).CompareTo(IsButton(b) ? 0 : 1));
                break;
            default:
                foreach (var k in bindings?.Keys ?? [])
                    list.Add(new ActionPrompt(action, InputDevice.Keyboard, PromptKind.Key, KeyName(k)) { Key = k });
                break;
        }
        return list;

        bool IsButton(ActionPrompt p) => _touch!.Layout.Controls.FirstOrDefault(c => c.Id == p.Control) is TouchButton;
    }

    /// <summary>The first control's text, as <see cref="Get"/>.</summary>
    public string Text(string action, InputDevice? device = null) => Get(action, device).Text;

    /// <summary>Every control's text joined with <paramref name="separator"/>: <c>"Z / Space / Enter"</c> for a help screen.</summary>
    public string TextAll(string action, string separator = " / ", InputDevice? device = null)
    {
        var all = All(action, device);
        return all.Count == 0 ? Fallback(action, device ?? Device).Text : string.Join(separator, all.Select(p => p.Text).Distinct());
    }

    /// <summary>
    /// How to show a group of directions together ("move"): on a keyboard the first key of each ("←→↑↓", "WASD"), on a
    /// gamepad "D-pad" or "Stick", on a touch screen the name of the d-pad or joystick that presses them all.
    /// </summary>
    public string Directions(string left = "left", string right = "right", string up = "up", string down = "down", InputDevice? device = null)
    {
        var d = device ?? Device;
        string[] actions = [left, right, up, down];
        switch (d)
        {
            case InputDevice.Gamepad:
            {
                var all = actions.Select(a => _input.GetBindings(a)).ToList();
                if (all.All(b => b is { Buttons.Count: > 0 } && b.Buttons.Any(x => x is >= PadButton.DPadUp and <= PadButton.DPadRight))) return "D-pad";
                if (all.All(b => b is { Stick: not InputMap.StickDirection.None })) return "Stick";
                break;
            }
            case InputDevice.Touch when _touch is not null:
            {
                var control = _touch.Layout.Controls.FirstOrDefault(c => c is TouchDPad or TouchStick && actions.All(a => All(a, d).Any(p => p.Control == c.Id)));
                if (control is TouchDPad) return "D-pad";
                if (control is TouchStick) return "Stick";
                break;
            }
        }
        var texts = actions.Select(a => Get(a, d).Text).ToList();
        return texts.All(t => t.Length == 1) ? string.Concat(texts) : string.Join(" ", texts);
    }

    /// <summary>
    /// Replaces each <c>{action}</c> in <paramref name="template"/> with that action's prompt (<c>{{</c> and <c>}}</c>
    /// are literal braces); <c>{move}</c> stands for <see cref="Directions"/>. Names that are neither bound, pressed by a
    /// touch control nor labelled are left as written. <paramref name="decorate"/> may wrap each prompt ("[Z]").
    /// </summary>
    public string Format(string template, Func<ActionPrompt, string>? decorate = null)
    {
        ArgumentNullException.ThrowIfNull(template);
        var sb = new StringBuilder(template.Length + 8);
        foreach (var part in Parse(template))
        {
            if (part.Action is null) { sb.Append(part.Text); continue; }
            var prompt = Resolve(part.Action);
            sb.Append(decorate is null ? prompt.Text : decorate(prompt));
        }
        return sb.ToString();
    }

    /// <summary>
    /// The pieces of a template: literal text, and actions with their prompts, for drawing prompts as glyphs between
    /// runs of text (see <c>MicroEngine.UI.PromptText</c>).
    /// </summary>
    public IReadOnlyList<(string Text, ActionPrompt? Prompt)> Split(string template)
    {
        ArgumentNullException.ThrowIfNull(template);
        return Parse(template).Select(p => p.Action is null ? (p.Text, (ActionPrompt?)null) : (p.Text, Resolve(p.Action))).ToList();
    }

    /// <summary>Whether <paramref name="name"/> is something a template may name.</summary>
    public bool Knows(string name) => name == "move" || _input.GetBindings(name) is not null || _labels.Keys.Any(k => k.Action == name)
        || (_touch?.Actions.Contains(name) ?? false);

    private ActionPrompt Resolve(string action) =>
        action == "move" && !_labels.Keys.Any(k => k.Action == "move")
            ? new ActionPrompt(action, Device, PromptKind.Label, Directions())
            : Get(action);

    private List<(string Text, string? Action)> Parse(string template)
    {
        var parts = new List<(string, string?)>();
        var text = new StringBuilder();
        for (int i = 0; i < template.Length; i++)
        {
            char c = template[i];
            if ((c == '{' || c == '}') && i + 1 < template.Length && template[i + 1] == c) { text.Append(c); i++; continue; }
            if (c == '{' && template.IndexOf('}', i + 1) is var end and > 0 && Knows(template[(i + 1)..end]))
            {
                if (text.Length > 0) { parts.Add((text.ToString(), null)); text.Clear(); }
                parts.Add((template[i..(end + 1)], template[(i + 1)..end]));
                i = end;
                continue;
            }
            text.Append(c);
        }
        if (text.Length > 0) parts.Add((text.ToString(), null));
        return parts;
    }

    private ActionPrompt Fallback(string action, InputDevice device)
    {
        // Nothing on this device presses the action: a keyboard prompt is still better than nothing on a desktop, but a
        // touch player has no keys, so they get the action's own name.
        if (device == InputDevice.Gamepad && All(action, InputDevice.Keyboard) is [var key, ..]) return key;
        return new ActionPrompt(action, device, PromptKind.Label, action.ToUpperInvariant());
    }

    private static InputMap.StickDirection? Direction(string action, string left, string right, string up, string down) =>
        action == left ? InputMap.StickDirection.Left : action == right ? InputMap.StickDirection.Right
        : action == up ? InputMap.StickDirection.Up : action == down ? InputMap.StickDirection.Down : null;

    private static string Arrow(InputMap.StickDirection d) => d switch
    {
        InputMap.StickDirection.Left => "←",
        InputMap.StickDirection.Right => "→",
        InputMap.StickDirection.Up => "↑",
        _ => "↓",
    };

    /// <summary>The name players read on a key cap: "Z", "Space", "Esc", "←".</summary>
    public static string KeyName(Key key) => key switch
    {
        Key.Space => "Space",
        Key.Enter => "Enter",
        Key.Escape => "Esc",
        Key.Backspace => "Backspace",
        Key.Tab => "Tab",
        Key.LeftShift or Key.RightShift => "Shift",
        Key.LeftControl => "Ctrl",
        Key.Left => "←",
        Key.Right => "→",
        Key.Up => "↑",
        Key.Down => "↓",
        >= Key.D0 and <= Key.D9 => ((char)('0' + (key - Key.D0))).ToString(),
        _ => key.ToString(),
    };

    /// <summary>
    /// The name printed on a gamepad button of <paramref name="style"/>. <see cref="PadButton"/> names positions as on
    /// an Xbox pad; PlayStation pads show shapes there (×, ○, □, △, all in the built-in font), Nintendo pads swap A/B and
    /// X/Y.
    /// </summary>
    public static string ButtonName(PadButton button, GamepadStyle style = GamepadStyle.Xbox) => (style, button) switch
    {
        (GamepadStyle.PlayStation, PadButton.A) => "×",
        (GamepadStyle.PlayStation, PadButton.B) => "○",
        (GamepadStyle.PlayStation, PadButton.X) => "□",
        (GamepadStyle.PlayStation, PadButton.Y) => "△",
        (GamepadStyle.PlayStation, PadButton.LeftShoulder) => "L1",
        (GamepadStyle.PlayStation, PadButton.RightShoulder) => "R1",
        (GamepadStyle.PlayStation, PadButton.Start) => "Options",
        (GamepadStyle.PlayStation, PadButton.Back) => "Share",
        (GamepadStyle.Nintendo, PadButton.A) => "B",
        (GamepadStyle.Nintendo, PadButton.B) => "A",
        (GamepadStyle.Nintendo, PadButton.X) => "Y",
        (GamepadStyle.Nintendo, PadButton.Y) => "X",
        (GamepadStyle.Nintendo, PadButton.LeftShoulder) => "L",
        (GamepadStyle.Nintendo, PadButton.RightShoulder) => "R",
        (GamepadStyle.Nintendo, PadButton.Start) => "+",
        (GamepadStyle.Nintendo, PadButton.Back) => "-",
        (_, PadButton.LeftShoulder) => "LB",
        (_, PadButton.RightShoulder) => "RB",
        (_, PadButton.Start) => "Start",
        (_, PadButton.Back) => "Back",
        (_, PadButton.DPadUp) => "↑",
        (_, PadButton.DPadDown) => "↓",
        (_, PadButton.DPadLeft) => "←",
        (_, PadButton.DPadRight) => "→",
        (_, PadButton.LeftStick) => "L3",
        (_, PadButton.RightStick) => "R3",
        _ => button.ToString(),
    };

    private static string AxisName(PadAxis axis, bool positive, GamepadStyle style) => axis switch
    {
        PadAxis.LeftTrigger => style switch { GamepadStyle.PlayStation => "L2", GamepadStyle.Nintendo => "ZL", _ => "LT" },
        PadAxis.RightTrigger => style switch { GamepadStyle.PlayStation => "R2", GamepadStyle.Nintendo => "ZR", _ => "RT" },
        PadAxis.LeftX => "L" + (positive ? "→" : "←"),
        PadAxis.LeftY => "L" + (positive ? "↓" : "↑"),
        PadAxis.RightX => "R" + (positive ? "→" : "←"),
        _ => "R" + (positive ? "↓" : "↑"),
    };

    /// <summary>The gamepad family a pad's name suggests (browsers and raylib report names, not types).</summary>
    public static GamepadStyle StyleFromName(string? name)
    {
        if (string.IsNullOrEmpty(name)) return GamepadStyle.Xbox;
        string n = name.ToLowerInvariant();
        if (n.Contains("054c") || n.Contains("sony") || n.Contains("playstation") || n.Contains("dualshock") || n.Contains("dualsense") || n.Contains("ps4") || n.Contains("ps5")) return GamepadStyle.PlayStation;
        if (n.Contains("057e") || n.Contains("nintendo") || n.Contains("switch") || n.Contains("joy-con") || n.Contains("pro controller")) return GamepadStyle.Nintendo;
        return GamepadStyle.Xbox;
    }
}

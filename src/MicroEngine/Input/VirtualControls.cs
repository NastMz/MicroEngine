using System.Numerics;
using MicroEngine.Graphics;

namespace MicroEngine.Input;

/// <summary>Which corner or edge of the safe area a touch control is placed from.</summary>
/// <remarks>Offsets point into the screen from the anchor, so the same offset mirrors between left and right anchors.</remarks>
public enum TouchAnchor { BottomLeft, BottomRight, TopLeft, TopRight, BottomCenter, TopCenter }

/// <summary>When on-screen controls are drawn.</summary>
public enum TouchVisibility
{
    /// <summary>On touch-first screens, and elsewhere after the first touch; hidden while a keyboard or gamepad plays, back on the next touch.</summary>
    Auto,
    /// <summary>Drawn on every screen with touch, whatever device plays.</summary>
    Always,
    /// <summary>Never drawn, and touches are ignored.</summary>
    Never,
}

/// <summary>Where a virtual joystick's base is.</summary>
public enum StickPlacement
{
    /// <summary>Always at its anchored position; a touch must start on it.</summary>
    Fixed,
    /// <summary>Appears where the thumb lands inside <see cref="TouchStick.Zone"/>, and follows a thumb that slides past its rim.</summary>
    Floating,
}

/// <summary>How a touch button drives its action.</summary>
public enum TouchButtonMode
{
    /// <summary>Down while a finger holds it.</summary>
    Hold,
    /// <summary>Each tap switches it on or off (an aim lock, a run toggle).</summary>
    Toggle,
}

/// <summary>
/// One on-screen control of a <see cref="TouchLayout"/>. Positions and sizes are density-independent pixels (dp, 1/160
/// inch), so a control keeps its physical size on any screen; the layout shrinks everything when a screen is too small.
/// </summary>
/// <param name="Id">Names the control for <see cref="VirtualControls.SetVisible"/>, scripts and tests.</param>
public abstract record TouchControl(string Id)
{
    /// <summary>The corner or edge of the safe area the control is placed from.</summary>
    public TouchAnchor Anchor { get; init; } = TouchAnchor.BottomLeft;
    /// <summary>Centre of the control from its anchor, in dp, pointing into the screen (x from the anchored side, y from the anchored edge).</summary>
    public Vector2 Offset { get; init; }
    /// <summary>Radius of the drawn control in dp; 0 uses the kind's default.</summary>
    public float Radius { get; init; }
    /// <summary>Shown and touchable when the layout starts (see <see cref="VirtualControls.SetVisible"/>).</summary>
    public bool Visible { get; init; } = true;
    /// <summary>How this control looks (null: its kind's style in the layout). Changes the drawing only, never the hit area.</summary>
    public TouchStyle? Style { get; init; }
    internal abstract float DefaultRadius { get; }
    internal float EffectiveRadius => Radius > 0 ? Radius : DefaultRadius;
    internal abstract IEnumerable<string> ActionNames { get; }
}

/// <summary>
/// A d-pad: discrete directions with no analog value, for games where a precise press matters more than a smooth one
/// (platformers, grid movement, menus). The thumb anywhere around its centre presses the direction it points to.
/// </summary>
public sealed record TouchDPad(string Id = "dpad", string Left = "left", string Right = "right", string Up = "up", string Down = "down") : TouchControl(Id)
{
    /// <summary>True: eight sectors (diagonals press two directions). False: four, only the dominant direction.</summary>
    public bool Diagonals { get; init; } = true;
    /// <summary>Centre area, as a fraction of the radius, where no direction is pressed.</summary>
    public float DeadZone { get; init; } = 0.22f;
    internal override float DefaultRadius => 62;
    internal override IEnumerable<string> ActionNames => [Left, Right, Up, Down];
}

/// <summary>
/// An analog joystick: a 2D value with intensity (<see cref="InputMap.Vector"/>) and, beyond <see cref="Threshold"/>,
/// the four direction actions as well, so digital code keeps working.
/// </summary>
public sealed record TouchStick(string Id = "stick", string Left = "left", string Right = "right", string Up = "up", string Down = "down") : TouchControl(Id)
{
    /// <summary>Whether the stick stays at its anchored position or appears under the thumb (floating, the default).</summary>
    public StickPlacement Placement { get; init; } = StickPlacement.Floating;
    /// <summary>For a floating stick, where a thumb may start it, as fractions (0..1) of the safe area. Default: the lower left.</summary>
    public RectF Zone { get; init; } = new(0, 0.3f, 0.45f, 0.7f);
    /// <summary>Radial dead zone as a fraction of the radius.</summary>
    public float DeadZone { get; init; } = 0.15f;
    /// <summary>Deflection beyond the dead zone at which direction actions go down.</summary>
    public float Threshold { get; init; } = 0.4f;
    /// <summary>Whether the stick presses directions in eight sectors (the default) or four.</summary>
    public StickDirections Directions { get; init; } = StickDirections.Eight;
    internal override float DefaultRadius => 56;
    internal override IEnumerable<string> ActionNames => [Left, Right, Up, Down];
}

/// <summary>A button that presses an action (and optionally more, as one key may: <c>Also = ["confirm"]</c> on a jump button).</summary>
/// <param name="Label">Short text drawn on it ("A", "JUMP"); null draws none.</param>
public sealed record TouchButton(string Id, string Action, string? Label = null) : TouchControl(Id)
{
    /// <summary>Whether the action is down while held (the default) or toggled by each tap.</summary>
    public TouchButtonMode Mode { get; init; } = TouchButtonMode.Hold;
    /// <summary>Further actions the button presses with <see cref="TouchButton.Action"/>, like a key bound to several actions.</summary>
    public IReadOnlyList<string> Also { get; init; } = [];
    internal override float DefaultRadius => 34;
    internal override IEnumerable<string> ActionNames => [Action, .. Also];
}

/// <summary>
/// A game's on-screen controls: which controls, where, and how they look and feel. The same layout is used on phones
/// (SDL), in mobile browsers and in tests; games return theirs from <c>Game.CreateTouchLayout</c>.
/// </summary>
public sealed record TouchLayout
{
    /// <summary>The controls, drawn in order; ids must be unique.</summary>
    public IReadOnlyList<TouchControl> Controls { get; init; } = [];
    /// <summary>Opacity of idle controls, 0..1. Pressed controls are always more opaque.</summary>
    public float Opacity { get; init; } = 0.6f;
    /// <summary>Size factor on top of the screen's density (a player setting).</summary>
    public float Scale { get; init; } = 1;
    /// <summary>The touchable area as a multiple of the drawn radius (overlaps go to the nearest control).</summary>
    public float HitScale { get; init; } = 1.3f;
    /// <summary>A held button stays pressed until its finger is this many touchable radii from its centre.</summary>
    public float SlideTolerance { get; init; } = 1.6f;
    /// <summary>Whether a finger that slides onto another button presses it instead (fire-and-jump rolls); toggle buttons are never slid onto.</summary>
    public bool SlideBetweenButtons { get; init; }
    /// <summary>A short vibration on each press where the device can vibrate.</summary>
    public bool Haptics { get; init; } = true;
    /// <summary>When the controls are drawn (automatic by default).</summary>
    public TouchVisibility Visibility { get; init; } = TouchVisibility.Auto;
    /// <summary>Colour of outlines and labels.</summary>
    public Color Tint { get; init; } = new(240, 244, 255);
    /// <summary>Fill of a pressed control.</summary>
    public Color PressedTint { get; init; } = new(110, 200, 255);
    /// <summary>How buttons look (null: the default style).</summary>
    public TouchStyle? ButtonStyle { get; init; }
    /// <summary>How d-pads look (null: the default style).</summary>
    public TouchStyle? DPadStyle { get; init; }
    /// <summary>How joysticks look (null: the default style).</summary>
    public TouchStyle? StickStyle { get; init; }
    /// <summary>Draws the controls (null: <see cref="DefaultTouchSkin"/>, shaped by the styles). Replace it for a look styles cannot give.</summary>
    public ITouchSkin? Skin { get; init; }

    private static readonly TouchStyle Plain = new();

    /// <summary>The style <paramref name="control"/> is drawn with: its own, else its kind's, else the default.</summary>
    public TouchStyle StyleOf(TouchControl control) => control.Style ?? control switch
    {
        TouchButton => ButtonStyle,
        TouchDPad => DPadStyle,
        _ => StickStyle,
    } ?? Plain;

    /// <summary>A layout with these controls and the default look.</summary>
    public static TouchLayout Of(params TouchControl[] controls) => new() { Controls = controls };

    /// <summary>
    /// The layout for <see cref="InputMap.CreateDefault"/>: a d-pad on the left; confirm (A), cancel (B) and action (C)
    /// on the right; pause at the top right.
    /// </summary>
    public static TouchLayout CreateDefault() => Of(
        new TouchDPad { Offset = new(104, 100) },
        new TouchButton("confirm", "confirm", "A") { Anchor = TouchAnchor.BottomRight, Offset = new(70, 84), Radius = 38 },
        new TouchButton("cancel", "cancel", "B") { Anchor = TouchAnchor.BottomRight, Offset = new(156, 54), Radius = 32 },
        new TouchButton("action", "action", "C") { Anchor = TouchAnchor.BottomRight, Offset = new(146, 140), Radius = 30 },
        new TouchButton("pause", "pause", "II") { Anchor = TouchAnchor.TopRight, Offset = new(34, 30), Radius = 20 });
}

/// <summary>
/// On-screen controls at run time: hit testing, finger tracking, d-pad and joystick values, tap latching, haptics and
/// drawing. It is an <see cref="IActionSource"/> (read by <see cref="InputMap"/> with the keyboard and gamepad) and an
/// <see cref="ITouchOverlay"/> (drawn by the backend over the game). Each finger belongs to the control it started on
/// until it lifts, so two thumbs work independently and a thumb that slides a little stays on its control.
/// </summary>
public sealed class VirtualControls : IActionSource, ITouchOverlay
{
    private const int TapFrames = 8;
    private const long ScriptFingerBase = 1L << 40;

    private sealed class Placed(TouchControl control)
    {
        public TouchControl Control { get; } = control;
        /// <summary>The control's actions, read once (a control's own list is built on every read).</summary>
        public string[] ActionNames { get; } = [.. control.ActionNames];
        public Vector2 Center;
        public float Radius;
        public bool Manual = control.Visible;   // SetVisible
        public bool InContext = true;           // the scene needs one of its actions (SetContext)
        public bool Visible => Manual && InContext;
        // Live state.
        public long? Finger;
        public Vector2 Origin;      // stick base while held
        public Vector2 Raw;         // stick or d-pad deflection, -1..1
        public Vector2 Analog;      // stick value after its dead zone
        public HashSet<InputMap.StickDirection> Directions = [];
        public HashSet<InputMap.StickDirection> Reported = [];
        public int Holders;          // fingers on a button
        public bool Toggled;
        public float Glow;           // 1 while pressed, fades after release
        public bool Pressed => Control switch
        {
            TouchButton { Mode: TouchButtonMode.Toggle } => Toggled,
            TouchButton => Holders > 0,
            _ => Directions.Count > 0,
        };
    }

    private sealed class Finger
    {
        public Placed? Control;
        public Vector2 Position;
        public bool Inside = true;
    }

    private readonly List<Placed> _controls = [];
    private readonly Dictionary<long, Finger> _fingers = [];
    private readonly List<TouchEvent> _events = [];
    private readonly List<(long Finger, string? Target, Vector2 At, bool Lift)> _scripted = [];
    private readonly HashSet<string> _held = new(StringComparer.Ordinal);
    private readonly Dictionary<string, float> _values = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _taps = new(StringComparer.Ordinal);
    // Reused every frame, so reading the controls allocates nothing.
    private readonly HashSet<string> _context = new(StringComparer.Ordinal);
    private readonly List<string> _keys = [];
    private string[] _actions = [];
    private readonly HashSet<string> _sampled = new(StringComparer.Ordinal);
    private TouchLayout _layout;
    private Vector2 _size;
    private RectF _safe;
    private float _density;
    private bool _touchedSinceOtherDevice, _touchFirst, _laidOut;
    private ITouchScreen? _screen;

    /// <summary>Creates the controls for <paramref name="layout"/> (null: <see cref="TouchLayout.CreateDefault"/>). Games use the instance in <see cref="Core.Game.Touch"/>.</summary>
    /// <exception cref="ArgumentException">Two controls share an id.</exception>
    public VirtualControls(TouchLayout? layout = null)
    {
        _layout = layout ?? TouchLayout.CreateDefault();
        Build();
    }

    /// <summary>The layout in use. Setting it (a scene with other controls) releases every finger.</summary>
    public TouchLayout Layout
    {
        get => _layout;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            CancelAll();
            _layout = value;
            Build();
        }
    }

    /// <summary>Always <see cref="InputDevice.Touch"/>.</summary>
    public InputDevice Device => InputDevice.Touch;
    /// <summary>Every action a control of the layout can press.</summary>
    public IEnumerable<string> Actions => _actions;
    /// <summary>Whether the controls are drawn this frame.</summary>
    public bool Shown { get; private set; }
    /// <summary>Whether the screen they were last laid out for is touch-first.</summary>
    public bool TouchFirst => _touchFirst;
    /// <summary>Vibrations requested since start (tests and diagnostics).</summary>
    public int Vibrations { get; private set; }
    /// <summary>Fingers currently on the screen (on a control or not).</summary>
    public int FingerCount => _fingers.Count;
    /// <summary>Pixels per dp used by the current layout, after shrinking to fit the screen.</summary>
    public float PixelsPerDp { get; private set; } = 1;

    private void Build()
    {
        _actions = [.. _layout.Controls.SelectMany(c => c.ActionNames).Distinct(StringComparer.Ordinal)];
        _controls.Clear();
        foreach (var c in _layout.Controls)
        {
            if (_controls.Any(p => p.Control.Id == c.Id)) throw new ArgumentException($"Two touch controls are named '{c.Id}'.");
            _controls.Add(new Placed(c));
        }
        _laidOut = false;
        if (_screen is not null) Relayout();
    }

    /// <summary>Shows or hides one control (hidden ones ignore touches and release their finger).</summary>
    public void SetVisible(string id, bool visible)
    {
        var p = Find(id);
        if (p.Manual == visible) return;
        p.Manual = visible;
        if (!visible) Release(p);
        RecomputeActions();
    }

    /// <summary>Whether the control <paramref name="id"/> is shown and touchable, after <see cref="SetVisible"/> and the scene's context.</summary>
    /// <exception cref="ArgumentException">No control has that id.</exception>
    public bool IsVisible(string id) => Find(id).Visible;

    /// <summary>
    /// The actions the current context needs (the top scene's <see cref="Scenes.Scene.TouchActions"/>, set by the game
    /// every frame): only controls that press at least one of them are shown and touchable; null shows them all. A
    /// dialogue that only advances with "confirm" hides the d-pad over its text, keeps the button that confirms, and
    /// the hidden controls release their fingers. The game frame does not change size with the context.
    /// </summary>
    public void SetContext(IEnumerable<string>? actions)
    {
        // Called every frame: the set is reused rather than built anew.
        HashSet<string>? needed = null;
        if (actions is not null)
        {
            needed = _context;
            needed.Clear();
            if (actions is IList<string> list)
                for (int i = 0; i < list.Count; i++) needed.Add(list[i]); // no enumerator to allocate
            else
                foreach (var a in actions) needed.Add(a);
        }
        bool changed = false;
        foreach (var p in _controls)
        {
            bool inContext = needed is null || Needs(needed, p.ActionNames);
            if (inContext == p.InContext) continue;
            p.InContext = inContext;
            if (!inContext) Release(p);
            changed = true;
        }
        if (changed) RecomputeActions();
    }

    private static bool Needs(HashSet<string> needed, string[] actions)
    {
        foreach (var a in actions)
            if (needed.Contains(a)) return true;
        return false;
    }

    /// <summary>
    /// Whether the game frame is fitted around the controls while they are shown (default true): on screens close to
    /// 16:9, where the letterbox bars cannot hold them, the frame is made as large as it can be without any control
    /// covering it (see <see cref="Frame"/>).
    /// </summary>
    public bool KeepFrameClear { get; set; } = true;

    /// <summary>The smallest frame, as a fraction of the largest, that <see cref="KeepFrameClear"/> may choose; below it controls overlap the edge of the frame instead.</summary>
    public float MinimumFrameFraction { get; set; } = 0.5f;

    /// <summary>The frame chosen by the last <see cref="Frame"/> call, in screen pixels (tests and diagnostics).</summary>
    public RectF LastFrame { get; private set; }

    /// <summary>
    /// Where the backend draws the game frame. Without shown controls it is <see cref="FrameLayout.Fit"/>. With shown
    /// controls that would cover the largest frame, the frame moves to the top of the safe area, or just below a control
    /// anchored at the top (horizontally centred), and takes the largest size at which no control of the layout overlaps it, never below
    /// <see cref="MinimumFrameFraction"/>; a whole-number scale is kept when it is within 90 % of that size, so pixels
    /// stay even when it costs little (pixel scaling only). Controls hidden by context still count, so a dialogue does not resize the game.
    /// </summary>
    public RectF Frame(Vector2 window, RectF safeArea, int virtualWidth, int virtualHeight, CanvasScaling scaling = CanvasScaling.Pixel)
    {
        var full = FrameLayout.Fit(window, safeArea, virtualWidth, virtualHeight, scaling);
        LastFrame = full;
        if (!Shown || !KeepFrameClear || !_laidOut || virtualWidth <= 0 || virtualHeight <= 0) return full;
        var safe = safeArea.Width > 0 && safeArea.Height > 0 ? safeArea : new RectF(0, 0, window.X, window.Y);
        float margin = 4 * PixelsPerDp;
        var obstacles = _controls.Where(p => p.Control.Visible && p.Manual).Select(p => (p.Center, R: p.Radius + margin)).ToList();
        bool Clear(RectF f) => obstacles.All(o => !Overlaps(f, o.Center, o.R));
        if (Clear(full)) return full;
        // Candidate tops: the top of the safe area, or just below a control anchored at the top (a pause button), so a
        // small corner button does not cost the frame its width.
        var tops = new List<float> { safe.Y };
        foreach (var p in _controls)
            if (p.Control.Visible && p.Manual && p.Control.Anchor is TouchAnchor.TopLeft or TouchAnchor.TopRight or TouchAnchor.TopCenter)
                tops.Add(MathF.Ceiling(p.Center.Y + p.Radius + margin));
        tops.Sort();
        RectF Place(float scale, float top)
        {
            float w = virtualWidth * scale, h = virtualHeight * scale;
            return new RectF(MathF.Floor(safe.X + (safe.Width - w) / 2), top, w, h);
        }
        RectF? Fits(float scale)
        {
            foreach (float top in tops)
            {
                var f = Place(scale, top);
                if (f.Bottom <= safe.Bottom + 0.5f && Clear(f)) return f;
            }
            return null;
        }
        float fullScale = full.Width / virtualWidth, lo = 0, hi = fullScale;
        for (int i = 0; i < 30; i++)
        {
            float mid = (lo + hi) / 2;
            if (Fits(mid) is not null) lo = mid; else hi = mid;
        }
        float minimum = fullScale * MinimumFrameFraction, chosen = Math.Max(lo, minimum);
        // A whole-number scale keeps pixels even; take it when it costs little and stays above the minimum.
        float whole = MathF.Floor(chosen);
        if (scaling == CanvasScaling.Pixel && whole >= 1 && whole >= chosen * 0.9f && whole >= minimum) chosen = whole;
        return LastFrame = Fits(chosen) ?? Place(chosen, safe.Y);
    }

    private static bool Overlaps(RectF r, Vector2 c, float radius)
    {
        float x = Math.Clamp(c.X, r.X, r.Right), y = Math.Clamp(c.Y, r.Y, r.Bottom);
        return (c.X - x) * (c.X - x) + (c.Y - y) * (c.Y - y) < radius * radius;
    }
    /// <summary>Whether the control is pressed (a button held or toggled on, a d-pad or stick pushed).</summary>
    public bool IsPressed(string id) => Find(id).Pressed;
    /// <summary>A stick's value after its dead zone, or a d-pad's direction, each axis -1..1.</summary>
    public Vector2 ValueOf(string id)
    {
        var p = Find(id);
        return p.Control is TouchStick ? p.Analog : DirectionVector(p.Directions);
    }

    /// <summary>The control's centre and drawn radius in screen pixels, as last laid out.</summary>
    public (Vector2 Center, float Radius) Geometry(string id)
    {
        var p = Find(id);
        return (p.Center, p.Radius);
    }

    private Placed Find(string id) => _controls.Find(p => p.Control.Id == id) ?? throw new ArgumentException($"No touch control is named '{id}'.", nameof(id));

    /// <summary>Whether a control holds <paramref name="action"/> down; a tap shorter than an update still counts once.</summary>
    public bool IsDown(string action)
    {
        if (_taps.ContainsKey(action))
        {
            _sampled.Add(action);
            return true;
        }
        return _held.Contains(action);
    }

    /// <summary>The action's strength, 0..1: a joystick's deflection, 1 for a pressed button or d-pad direction.</summary>
    public float Value(string action) => _values.TryGetValue(action, out var v) ? v : IsDown(action) ? 1 : 0;

    /// <summary>
    /// A scripted touch, as an input script's <c>touch</c> command: finger <paramref name="finger"/> goes down (or moves)
    /// on control <paramref name="target"/>, displaced by <paramref name="offset"/> radii from its centre, at the next
    /// update. The touch goes through the same hit testing as a real finger.
    /// </summary>
    public void ScriptTouch(int finger, string target, Vector2 offset) => _scripted.Add((ScriptFingerBase + finger, target, offset, false));
    /// <summary>A scripted touch at a point given as fractions (0..1) of the screen.</summary>
    public void ScriptTouchAt(int finger, Vector2 normalized) => _scripted.Add((ScriptFingerBase + finger, null, normalized, false));
    /// <summary>Lifts a scripted finger.</summary>
    public void ScriptLift(int finger) => _scripted.Add((ScriptFingerBase + finger, null, default, true));

    /// <summary>Releases every finger, as when the game loses focus or is suspended. Toggled buttons stay as they are.</summary>
    public void CancelAll()
    {
        _fingers.Clear();
        foreach (var p in _controls) Release(p);
        _taps.Clear();
        RecomputeActions();
    }

    /// <summary>
    /// Called once per frame by the loop, before the steps: lays the controls out for the screen, applies its touch
    /// events and scripted touches, and advances visibility and press animations. <paramref name="otherDevice"/> is the
    /// device that last pressed an action (a keyboard or gamepad hides <see cref="TouchVisibility.Auto"/> controls).
    /// </summary>
    public void Update(ITouchScreen? screen, float dt, InputDevice otherDevice = InputDevice.None)
    {
        foreach (var a in _sampled) _taps.Remove(a);
        _sampled.Clear();
        _keys.Clear();
        _keys.AddRange(_taps.Keys);
        foreach (var a in _keys)
            if (--_taps[a] <= 0) _taps.Remove(a);

        _screen = screen;
        if (screen is null)
        {
            Shown = false;
            return;
        }
        _touchFirst = screen.TouchFirst;
        if (!_laidOut || screen.Size != _size || screen.SafeArea != _safe || screen.PixelsPerDp != _density)
        {
            // A rotation or resize moves every control; fingers held across it would point at the wrong place.
            if (_laidOut && _fingers.Count > 0) CancelAll();
            Relayout();
        }
        _events.Clear();
        screen.ReadTouches(_events);
        foreach (var (finger, target, at, lift) in _scripted)
        {
            if (lift) { _events.Add(new TouchEvent(finger, TouchPhase.Ended, _fingers.TryGetValue(finger, out var f) ? f.Position : default)); continue; }
            Vector2 position;
            if (target is null) position = at * _size;
            else
            {
                var p = Find(target);
                bool holding = p.Finger == finger && p.Control is not TouchButton;
                // A thumb lands on the middle of a stick or d-pad, then pushes: a floating stick starts where it lands.
                if (!holding && p.Control is not TouchButton && at != Vector2.Zero && !_fingers.ContainsKey(finger))
                    _events.Add(new TouchEvent(finger, TouchPhase.Began, p.Center));
                position = (holding ? p.Origin : p.Center) + at * p.Radius;
            }
            bool known = _fingers.ContainsKey(finger) || _events.Any(e => e.Finger == finger && e.Phase == TouchPhase.Began);
            _events.Add(new TouchEvent(finger, known ? TouchPhase.Moved : TouchPhase.Began, position));
        }
        _scripted.Clear();
        if (_layout.Visibility == TouchVisibility.Never) _events.Clear();
        foreach (var e in _events) Handle(e);
        RecomputeActions();

        if (otherDevice is InputDevice.Keyboard or InputDevice.Gamepad && _fingers.Count == 0) _touchedSinceOtherDevice = false;
        if (_events.Any(e => e.Phase == TouchPhase.Began)) _touchedSinceOtherDevice = true;
        Shown = _layout.Visibility switch
        {
            TouchVisibility.Always => true,
            TouchVisibility.Never => false,
            _ => _touchedSinceOtherDevice || (screen.TouchFirst && otherDevice is not (InputDevice.Keyboard or InputDevice.Gamepad)),
        };
        float fade = dt / 0.12f;
        foreach (var p in _controls) p.Glow = p.Pressed ? 1 : Math.Max(0, p.Glow - fade);
    }

    private void Relayout()
    {
        if (_screen is null) return;
        _size = _screen.Size;
        _safe = _screen.SafeArea.Width > 0 && _screen.SafeArea.Height > 0 ? _screen.SafeArea : new RectF(0, 0, _size.X, _size.Y);
        _density = _screen.PixelsPerDp;
        float k = Math.Max(0.1f, _density) * Math.Max(0.1f, _layout.Scale);
        // Shrink when the screen cannot hold the controls at their physical size: each corner's controls keep within
        // about half of the safe area's height and 45 % of its width, so left and right thumbs never overlap.
        foreach (var c in _layout.Controls)
        {
            float r = c.EffectiveRadius * _layout.HitScale;
            float vertical = c.Offset.Y + r, horizontal = c.Anchor is TouchAnchor.BottomCenter or TouchAnchor.TopCenter ? 0 : c.Offset.X + r;
            if (vertical > 0) k = Math.Min(k, _safe.Height * 0.5f / vertical);
            if (horizontal > 0) k = Math.Min(k, _safe.Width * 0.45f / horizontal);
        }
        PixelsPerDp = k;
        foreach (var p in _controls)
        {
            var c = p.Control;
            var o = c.Offset * k;
            p.Radius = c.EffectiveRadius * k;
            p.Center = c.Anchor switch
            {
                TouchAnchor.BottomLeft => new Vector2(_safe.X + o.X, _safe.Bottom - o.Y),
                TouchAnchor.BottomRight => new Vector2(_safe.Right - o.X, _safe.Bottom - o.Y),
                TouchAnchor.TopLeft => new Vector2(_safe.X + o.X, _safe.Y + o.Y),
                TouchAnchor.TopRight => new Vector2(_safe.Right - o.X, _safe.Y + o.Y),
                TouchAnchor.BottomCenter => new Vector2(_safe.Center.X + o.X, _safe.Bottom - o.Y),
                _ => new Vector2(_safe.Center.X + o.X, _safe.Y + o.Y),
            };
        }
        _laidOut = true;
    }

    private void Handle(TouchEvent e)
    {
        switch (e.Phase)
        {
            case TouchPhase.Began:
            {
                if (_fingers.ContainsKey(e.Finger)) { Move(e.Finger, e.Position); break; }
                var finger = new Finger { Position = e.Position };
                _fingers[e.Finger] = finger;
                var target = HitTest(e.Position);
                if (target is null) break;
                finger.Control = target;
                Begin(target, e.Finger, e.Position);
                break;
            }
            case TouchPhase.Moved:
                Move(e.Finger, e.Position);
                break;
            default:
                if (_fingers.Remove(e.Finger, out var f) && f.Control is { } p) End(p, f);
                break;
        }
    }

    private Placed? HitTest(Vector2 at)
    {
        Placed? best = null;
        float bestDistance = float.MaxValue;
        foreach (var p in _controls)
        {
            if (!p.Visible || p.Control is TouchStick { Placement: StickPlacement.Floating }) continue;
            if (p.Control is not TouchButton && p.Finger is not null) continue;
            float d = Vector2.Distance(at, p.Center) / (p.Radius * _layout.HitScale);
            if (d <= 1 && d < bestDistance) (best, bestDistance) = (p, d);
        }
        if (best is not null) return best;
        foreach (var p in _controls)
        {
            if (!p.Visible || p.Finger is not null || p.Control is not TouchStick { Placement: StickPlacement.Floating } stick) continue;
            var zone = new RectF(_safe.X + stick.Zone.X * _safe.Width, _safe.Y + stick.Zone.Y * _safe.Height, stick.Zone.Width * _safe.Width, stick.Zone.Height * _safe.Height);
            if (zone.Contains(at) || Vector2.Distance(at, p.Center) <= p.Radius * _layout.HitScale) return p;
        }
        return null;
    }

    private void Begin(Placed p, long finger, Vector2 at)
    {
        bool was = p.Pressed;
        switch (p.Control)
        {
            case TouchButton b:
                p.Holders++;
                if (b.Mode == TouchButtonMode.Toggle) p.Toggled = !p.Toggled;
                break;
            case TouchStick s:
                p.Finger = finger;
                p.Origin = s.Placement == StickPlacement.Floating ? ClampInside(at, p.Radius) : p.Center;
                Steer(p, at);
                break;
            default:
                p.Finger = finger;
                p.Origin = p.Center;
                Steer(p, at);
                break;
        }
        Pressed(p, was);
    }

    private void Move(long id, Vector2 at)
    {
        if (!_fingers.TryGetValue(id, out var f)) return;
        f.Position = at;
        if (f.Control is not { } p) return;
        bool was = p.Pressed;
        if (p.Control is TouchButton b)
        {
            float hit = p.Radius * _layout.HitScale;
            if (_layout.SlideBetweenButtons)
            {
                var other = _controls.Where(o => o != p && o.Visible && o.Control is TouchButton { Mode: not TouchButtonMode.Toggle } && Vector2.Distance(at, o.Center) <= o.Radius * _layout.HitScale)
                    .OrderBy(o => Vector2.Distance(at, o.Center)).FirstOrDefault();
                if (other is not null && Vector2.Distance(at, other.Center) < Vector2.Distance(at, p.Center))
                {
                    End(p, f);
                    f.Control = other;
                    f.Inside = true;
                    Begin(other, id, at);
                    return;
                }
            }
            bool inside = Vector2.Distance(at, p.Center) <= hit * _layout.SlideTolerance;
            if (inside == f.Inside || b.Mode == TouchButtonMode.Toggle) { f.Inside = inside; return; }
            f.Inside = inside;
            p.Holders += inside ? 1 : -1;
            Pressed(p, was);
            return;
        }
        Steer(p, at);
        Pressed(p, was);
    }

    private void End(Placed p, Finger f)
    {
        if (p.Control is TouchButton)
        {
            if (f.Inside && p.Holders > 0) p.Holders--;
            return;
        }
        Release(p);
    }

    private void Release(Placed p)
    {
        p.Finger = null;
        p.Raw = p.Analog = Vector2.Zero;
        p.Directions.Clear();
        p.Reported.Clear();
        p.Holders = 0;
        foreach (var f in _fingers.Values) if (f.Control == p) f.Control = null;
    }

    private void Steer(Placed p, Vector2 at)
    {
        var raw = (at - p.Origin) / Math.Max(1, p.Radius);
        float length = raw.Length();
        p.Directions.Clear();
        if (p.Control is TouchStick s)
        {
            if (length > 1)
            {
                // A floating stick follows the thumb past its rim; a fixed one clamps.
                if (s.Placement == StickPlacement.Floating) p.Origin = ClampInside(at - raw / length * p.Radius, p.Radius);
                raw = (at - p.Origin) / Math.Max(1, p.Radius);
                if (raw.Length() > 1) raw = Vector2.Normalize(raw);
            }
            p.Raw = raw;
            p.Analog = InputMap.ApplyDeadZone(raw, s.DeadZone);
            foreach (var d in InputMap.Directions(p.Analog, s.Threshold, s.Directions)) p.Directions.Add(d);
            return;
        }
        var pad = (TouchDPad)p.Control;
        p.Raw = length > 1 ? raw / length : raw;
        if (length >= pad.DeadZone)
            foreach (var d in InputMap.Directions(raw, 0, pad.Diagonals ? StickDirections.Eight : StickDirections.Four)) p.Directions.Add(d);
    }

    private void Pressed(Placed p, bool was)
    {
        var newly = new List<string>();
        switch (p.Control)
        {
            case TouchButton b:
                if (!was && p.Pressed) newly.AddRange(p.ActionNames);
                break;
            case TouchDPad d:
                foreach (var dir in p.Directions) if (!p.Reported.Contains(dir)) newly.Add(Name(d.Left, d.Right, d.Up, d.Down, dir));
                break;
            case TouchStick s:
                foreach (var dir in p.Directions) if (!p.Reported.Contains(dir)) newly.Add(Name(s.Left, s.Right, s.Up, s.Down, dir));
                break;
        }
        p.Reported.Clear();
        p.Reported.UnionWith(p.Directions);
        // Taps shorter than a frame still reach a step (as with keys).
        foreach (var a in newly) _taps[a] = TapFrames;
        // A tick on every new press and on each d-pad direction change, so the thumb feels what it pressed; a stick
        // ticks only when it leaves the centre.
        if (newly.Count > 0 && _layout.Haptics && (p.Control is not TouchStick || !was) && _screen is { CanVibrate: true } screen)
        {
            screen.Vibrate(p.Control is TouchButton ? 0.5f : 0.3f, p.Control is TouchButton ? 14 : 9);
            Vibrations++;
        }
    }

    private static string Name(string left, string right, string up, string down, InputMap.StickDirection d) => d switch
    {
        InputMap.StickDirection.Left => left,
        InputMap.StickDirection.Right => right,
        InputMap.StickDirection.Up => up,
        _ => down,
    };

    private void RecomputeActions()
    {
        _held.Clear();
        _values.Clear();
        foreach (var p in _controls)
        {
            if (!p.Visible) continue;
            switch (p.Control)
            {
                case TouchButton b:
                    if (p.Pressed)
                        foreach (var a in p.ActionNames) { _held.Add(a); _values[a] = 1; }
                    break;
                case TouchDPad d:
                    foreach (var dir in p.Directions) { var a = Name(d.Left, d.Right, d.Up, d.Down, dir); _held.Add(a); _values[a] = 1; }
                    break;
                case TouchStick s:
                    Add(s.Left, Math.Max(0, -p.Analog.X), p.Directions.Contains(InputMap.StickDirection.Left));
                    Add(s.Right, Math.Max(0, p.Analog.X), p.Directions.Contains(InputMap.StickDirection.Right));
                    Add(s.Up, Math.Max(0, -p.Analog.Y), p.Directions.Contains(InputMap.StickDirection.Up));
                    Add(s.Down, Math.Max(0, p.Analog.Y), p.Directions.Contains(InputMap.StickDirection.Down));
                    break;
            }
        }

        void Add(string action, float value, bool down)
        {
            if (down) _held.Add(action);
            if (value > _values.GetValueOrDefault(action)) _values[action] = value;
        }
    }

    private Vector2 ClampInside(Vector2 at, float radius)
    {
        float m = radius * 1.05f;
        return new Vector2(
            Math.Clamp(at.X, _safe.X + m, Math.Max(_safe.X + m, _safe.Right - m)),
            Math.Clamp(at.Y, _safe.Y + m, Math.Max(_safe.Y + m, _safe.Bottom - m)));
    }

    private static Vector2 DirectionVector(HashSet<InputMap.StickDirection> d) => new(
        (d.Contains(InputMap.StickDirection.Right) ? 1 : 0) - (d.Contains(InputMap.StickDirection.Left) ? 1 : 0),
        (d.Contains(InputMap.StickDirection.Down) ? 1 : 0) - (d.Contains(InputMap.StickDirection.Up) ? 1 : 0));

    /// <summary>
    /// Draws the shown controls with the layout's skin (<see cref="TouchLayout.Skin"/>, by default
    /// <see cref="DefaultTouchSkin"/> shaped by the layout's styles), each with its state this frame.
    /// </summary>
    public void Draw(IOverlayCanvas canvas)
    {
        if (!Shown) return;
        var skin = _layout.Skin ?? DefaultTouchSkin.Instance;
        foreach (var p in _controls)
        {
            if (!p.Visible) continue;
            bool held = p.Finger is not null;
            var value = p.Control is TouchStick ? p.Analog : DirectionVector(p.Directions);
            skin.Draw(canvas, new TouchControlView(p.Control, _layout.StyleOf(p.Control), p.Center, p.Radius, PixelsPerDp, _layout.Opacity, p.Glow,
                p.Pressed, p.Toggled, held || p.Holders > 0, held ? p.Origin : p.Center, p.Raw, value, p.Directions, _layout));
        }
    }
}

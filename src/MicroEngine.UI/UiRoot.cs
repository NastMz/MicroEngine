using System.Numerics;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Kernel;

namespace MicroEngine.UI;

/// <summary>
/// The actions engine widgets answer to. A member left null (the default) is the input map's own name for it
/// (<see cref="InputMap.Names"/>), read at every update: a game that renames <c>confirm</c> with
/// <see cref="ActionNames"/> keeps working widgets, and a gameplay action that happens to be called <c>confirm</c>
/// never presses a button. Set a member to give the widgets an action of the game's own.
/// </summary>
public sealed record UiActions
{
    /// <summary>The map's names for every action (all members null).</summary>
    public static UiActions Default { get; } = new();

    /// <summary>Moves the focus up; null is <see cref="ActionNames.Up"/>.</summary>
    public string? Up { get; init; }
    /// <summary>Moves the focus down; null is <see cref="ActionNames.Down"/>.</summary>
    public string? Down { get; init; }
    /// <summary>Moves the focus left, or the caret of a text field; null is <see cref="ActionNames.Left"/>.</summary>
    public string? Left { get; init; }
    /// <summary>Moves the focus right, or the caret of a text field; null is <see cref="ActionNames.Right"/>.</summary>
    public string? Right { get; init; }
    /// <summary>Presses the focused button or submits the focused text field; null is <see cref="ActionNames.Confirm"/>.</summary>
    public string? Confirm { get; init; }
    /// <summary>Raises <see cref="UiRoot.Cancelled"/>; null is <see cref="ActionNames.Cancel"/>.</summary>
    public string? Cancel { get; init; }

    /// <summary>The action <paramref name="action"/> is read from on <paramref name="names"/>.</summary>
    public string NameOf(UiAction action, ActionNames names)
    {
        ArgumentNullException.ThrowIfNull(names);
        return action switch
        {
            UiAction.Up => Up ?? names.Up,
            UiAction.Down => Down ?? names.Down,
            UiAction.Left => Left ?? names.Left,
            UiAction.Right => Right ?? names.Right,
            UiAction.Confirm => Confirm ?? names.Confirm,
            _ => Cancel ?? names.Cancel,
        };
    }
}

/// <summary>
/// Shows one widget tree in the UI view and drives it: lays <see cref="Content"/> out in UI units at
/// <see cref="Anchor"/>, keeps the focus, moves it with the engine's direction actions, gives the focused widget confirm
/// and cancel, and, as the highest input layer (<see cref="InputLayerOrder.UI"/>), takes pointer presses that land on
/// the content and, while a text field has the focus, the keyboard. What it takes never reaches gameplay: a press on a
/// button is not a press in the world, and the actions it acts on are consumed (<see cref="InputMap.Consume"/>), so the
/// scene's update, which runs after the root's, does not see them. A scene has one in <c>Scene.Widgets</c>, which the engine
/// updates, draws and routes input to; a root can also be driven by hand (<see cref="Update"/>, <see cref="Draw"/>,
/// <see cref="Handle"/>).
/// </summary>
public sealed class UiRoot : IInputLayer, IStateHashed
{
    private readonly InputMap _input;
    private readonly Queue<InputEvent> _events = new();
    private readonly List<Widget> _focusable = [];
    private Widget? _content;
    private long _pressPointer = -1;
    private Vector2 _size;
    private bool _focusTouched;

    /// <summary>A root reading actions from <paramref name="input"/> and typed text from <paramref name="text"/> (null: text fields cannot be typed in).</summary>
    public UiRoot(InputMap input, TextInput? text = null)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
        TextInput = text;
    }

    /// <summary>The text channel text fields type through, or null.</summary>
    public TextInput? TextInput { get; }

    /// <summary>The widget tree shown; setting it drops the focus, the hover and any press.</summary>
    /// <exception cref="InvalidOperationException">The widget already belongs to another widget or root.</exception>
    public Widget? Content
    {
        get => _content;
        set
        {
            if (ReferenceEquals(value, _content)) return;
            if (value is not null && (value.Parent is not null || value.Root is not null))
                throw new InvalidOperationException("The widget already belongs to another widget or root; remove it there first.");
            Focus(null);
            _content?.SetRoot(null);
            _content = value;
            value?.SetRoot(this);
            Hovered = Pressed = null;
            _pressPointer = -1;
            _focusTouched = false;
        }
    }

    /// <summary>The font widgets measure and draw text with. The engine sets the game's font; it must be set before drawing.</summary>
    public IFont? Font { get; set; }
    /// <summary>The colours (<see cref="UiTheme.Default"/> by default).</summary>
    public UiTheme Theme { get; set; } = UiTheme.Default;
    /// <summary>The actions the widgets answer to (<see cref="UiActions.Default"/>: the input map's names).</summary>
    public UiActions Actions { get; set; } = UiActions.Default;
    /// <summary>The point of the UI view the content is placed at (<see cref="Anchor.Center"/> by default).</summary>
    public Anchor Anchor { get; set; } = Anchor.Center;
    /// <summary>Moves the content from its anchor, in UI units.</summary>
    public Vector2 Offset { get; set; }
    /// <summary>Whether the first focusable widget takes the focus when nothing has it, so a keyboard or a gamepad can use the UI at once (true by default).</summary>
    public bool AutoFocus { get; set; } = true;
    /// <summary>Whether moving the focus past the last widget in a direction continues from the other side (true by default).</summary>
    public bool WrapFocus { get; set; } = true;
    /// <summary>
    /// Maps a pointer event's position (virtual canvas pixels, <see cref="InputEvent.Position"/>) into UI units. The engine
    /// sets it to the game's view mapping; the identity by default (UI units are canvas pixels).
    /// </summary>
    public Func<Vector2, Vector2> PointerToUi { get; set; } = static p => p;
    /// <summary>Maps a UI position into virtual canvas pixels, for the caret an input method shows its candidates by; the identity by default.</summary>
    public Func<Vector2, Vector2> UiToCanvas { get; set; } = static p => p;
    /// <summary>The size of the UI view in UI units: set by every <see cref="Draw"/> from the renderer's size, and by the engine before each update.</summary>
    public Vector2 Size { get => _size; set => _size = value; }
    /// <summary>Seconds the root has been updated.</summary>
    public float Time { get; private set; }

    /// <summary>The widget with the focus, or null.</summary>
    public Widget? Focused { get; private set; }
    /// <summary>The widget under the pointer, or null.</summary>
    public Widget? Hovered { get; private set; }
    /// <summary>The widget a held pointer press started on, or null.</summary>
    public Widget? Pressed { get; private set; }
    /// <summary>Raised when cancel is pressed and no focused widget used it; the press is then consumed (for example to close a menu).</summary>
    public event Action? Cancelled;

    private UiContext Context => new(Font ?? throw new InvalidOperationException("Set UiRoot.Font before laying out or drawing the UI."),
        Theme, UiTheme.Unit(Font), Time);

    /// <summary>The visible widgets of the content in focus and drawing order (depth first).</summary>
    public IEnumerable<Widget> AllWidgets
    {
        get
        {
            if (_content is null) yield break;
            var pending = new Stack<Widget>();
            pending.Push(_content);
            while (pending.Count > 0)
            {
                var w = pending.Pop();
                if (!w.Visible) continue;
                yield return w;
                for (int i = w.Children.Count - 1; i >= 0; i--) pending.Push(w.Children[i]);
            }
        }
    }

    /// <summary>Gives <paramref name="widget"/> the focus (null removes it); false when it cannot take it (not focusable, disabled, hidden or not in this root).</summary>
    public bool Focus(Widget? widget)
    {
        if (widget is not null && (!widget.CanFocus || !ReferenceEquals(widget.Root, this))) return false;
        _focusTouched = true;
        if (ReferenceEquals(widget, Focused)) return true;
        var previous = Focused;
        Focused = widget;
        previous?.OnFocusChanged(false);
        widget?.OnFocusChanged(true);
        return true;
    }

    /// <summary>
    /// Moves the focus to the nearest focusable widget in <paramref name="direction"/> (up, down, left or right) by the
    /// widgets' last layout; past the last one it continues from the other side when <see cref="WrapFocus"/> is on.
    /// Returns whether the focus moved.
    /// </summary>
    public bool MoveFocus(UiAction direction)
    {
        if (direction is UiAction.Confirm or UiAction.Cancel) return false;
        Collect();
        if (_focusable.Count == 0) return false;
        if (Focused is null || !_focusable.Contains(Focused)) return Focus(_focusable[0]);
        var from = Center(Focused.Bounds);
        var axis = direction switch { UiAction.Up => -Vector2.UnitY, UiAction.Down => Vector2.UnitY, UiAction.Left => -Vector2.UnitX, _ => Vector2.UnitX };
        var cross = new Vector2(MathF.Abs(axis.Y), MathF.Abs(axis.X));
        Widget? best = null;
        float bestScore = float.MaxValue;
        foreach (var w in _focusable)
        {
            if (ReferenceEquals(w, Focused)) continue;
            var d = Center(w.Bounds) - from;
            float along = Vector2.Dot(d, axis);
            if (along <= 0.5f) continue;
            float score = along + 2 * MathF.Abs(Vector2.Dot(d, cross));
            if (score < bestScore) (best, bestScore) = (w, score);
        }
        if (best is null && WrapFocus)
        {
            // From the far side: the widget furthest against the direction, nearest across.
            foreach (var w in _focusable)
            {
                if (ReferenceEquals(w, Focused)) continue;
                var d = Center(w.Bounds) - from;
                float score = Vector2.Dot(d, axis) + 2 * MathF.Abs(Vector2.Dot(d, cross));
                if (Vector2.Dot(d, axis) < -0.5f && score < bestScore) (best, bestScore) = (w, score);
            }
        }
        return best is not null && Focus(best);
    }

    private static Vector2 Center(RectF r) => new(r.X + r.Width / 2, r.Y + r.Height / 2);

    private void Collect()
    {
        _focusable.Clear();
        foreach (var w in AllWidgets)
            if (w.CanFocus) _focusable.Add(w);
    }

    /// <summary>Whether <paramref name="uiPoint"/> (UI units) lies on the content, where the root takes the pointer.</summary>
    public bool Contains(Vector2 uiPoint) => _content is { Visible: true } c && c.Bounds.Contains(uiPoint);

    /// <summary>The deepest visible widget under <paramref name="uiPoint"/>, or null.</summary>
    public Widget? WidgetAt(Vector2 uiPoint)
    {
        Widget? hit = null;
        foreach (var w in AllWidgets)
            if (w.Bounds.Contains(uiPoint)) hit = w;
        return hit;
    }

    /// <summary>The focusable, enabled widget under <paramref name="uiPoint"/> (the hit widget or the nearest container of it that can take the focus), or null.</summary>
    private Widget? TargetAt(Vector2 uiPoint)
    {
        for (var w = WidgetAt(uiPoint); w is not null; w = w.Parent)
            if (w.CanFocus) return w;
        return null;
    }

    /// <summary>Measures the content and places it in the UI view (<see cref="Size"/>) at <see cref="Anchor"/>.</summary>
    /// <exception cref="InvalidOperationException"><see cref="Font"/> is not set.</exception>
    public void Layout()
    {
        if (_content is null) return;
        var context = Context;
        var size = _content.Measure(context);
        float x = Anchor switch
        {
            Anchor.TopLeft or Anchor.Left or Anchor.BottomLeft => 0,
            Anchor.Top or Anchor.Center or Anchor.Bottom => MathF.Round((_size.X - size.X) / 2),
            _ => _size.X - size.X,
        };
        float y = Anchor switch
        {
            Anchor.TopLeft or Anchor.Top or Anchor.TopRight => 0,
            Anchor.Left or Anchor.Center or Anchor.Right => MathF.Round((_size.Y - size.Y) / 2),
            _ => _size.Y - size.Y,
        };
        _content.Arrange(new RectF(x + Offset.X, y + Offset.Y, size.X, size.Y), context);
    }

    /// <summary>
    /// Advances the UI by one update: lays it out, applies the pointer events and keys it took since the last update,
    /// updates the widgets, then gives the actions to the focused widget and to focus navigation, consuming those it acts
    /// on. The engine calls it before the scene's own update.
    /// </summary>
    public void Update(float dt)
    {
        Time += dt;
        if (_content is null || !_content.Visible)
        {
            _events.Clear();
            return;
        }
        if (Font is not null) Layout();
        Collect();
        // A focused widget that was disabled or hidden hands the focus on to the first one that can take it.
        if (Focused is not null && !Focused.CanFocus) Focus(_focusable.Count > 0 ? _focusable[0] : null);
        if (Focused is null && AutoFocus && !_focusTouched && _focusable.Count > 0) Focus(_focusable[0]);
        while (_events.TryDequeue(out var e)) Apply(e);
        foreach (var w in AllWidgets.ToList()) w.Update(dt);
        ReadActions();
    }

    private void Apply(in InputEvent e)
    {
        switch (e.Kind)
        {
            case InputEventKind.PointerMove:
                Hovered = WidgetAt(PointerToUi(e.Position));
                break;
            case InputEventKind.PointerDown:
            {
                var p = PointerToUi(e.Position);
                Hovered = WidgetAt(p);
                Pressed = TargetAt(p);
                if (Pressed is not null) Focus(Pressed);
                break;
            }
            case InputEventKind.PointerUp:
            {
                var p = PointerToUi(e.Position);
                var target = TargetAt(p);
                var pressed = Pressed;
                Pressed = null;
                if (pressed is not null && ReferenceEquals(target, pressed) && pressed.IsEnabledInTree) pressed.OnClick();
                break;
            }
            case InputEventKind.PointerCancel:
                Pressed = null;
                break;
            case InputEventKind.KeyDown:
                if (Focused is TextField field && !field.OnKey(e.Key)) Navigate(e.Key, field);
                break;
        }
    }

    private void Navigate(Key key, TextField field)
    {
        switch (key)
        {
            case Key.Up: MoveFocus(UiAction.Up); break;
            case Key.Down or Key.Tab: MoveFocus(UiAction.Down); break;
            case Key.Escape: if (ReferenceEquals(Focused, field)) Focus(null); break;
        }
    }

    private void ReadActions()
    {
        var names = _input.Names;
        foreach (var action in (ReadOnlySpan<UiAction>)[UiAction.Up, UiAction.Down, UiAction.Left, UiAction.Right, UiAction.Confirm, UiAction.Cancel])
        {
            string name = Actions.NameOf(action, names);
            if (!_input.Pressed(name)) continue;
            bool used = Focused is { } focused && focused.IsEnabledInTree && focused.OnAction(action);
            if (!used && action is UiAction.Up or UiAction.Down or UiAction.Left or UiAction.Right) used = MoveFocus(action);
            if (!used && action == UiAction.Cancel && Cancelled is { } cancelled)
            {
                cancelled();
                used = true;
            }
            if (used) _input.Consume(name);
        }
    }

    /// <summary>Lays the content out for <paramref name="ui"/>'s size (UI units) and draws it there.</summary>
    /// <exception cref="InvalidOperationException"><see cref="Font"/> is not set.</exception>
    public void Draw(IRenderer ui)
    {
        ArgumentNullException.ThrowIfNull(ui);
        if (_content is null) return;
        _size = new Vector2(ui.Width, ui.Height);
        Layout();
        _content.Draw(ui, Context);
    }

    /// <summary>
    /// Takes an event as the UI's input layer (see <see cref="IInputLayer"/>): a pointer press on the content, everything
    /// of a press it took, the wheel over the content, and keys while a text field has the focus. Taken events are
    /// applied at the next <see cref="Update"/>. Pointer moves are watched (for the hover) but left to gameplay, and text
    /// goes to the text channel.
    /// </summary>
    public bool Handle(in InputEvent e)
    {
        if (_content is not { Visible: true }) return false;
        switch (e.Kind)
        {
            case InputEventKind.PointerDown:
                if (e.PointerKind == PointerKind.Mouse && e.PointerButton != PointerButton.Primary) return Contains(PointerToUi(e.Position));
                if (!Contains(PointerToUi(e.Position))) return false;
                _pressPointer = e.Pointer;
                _events.Enqueue(e);
                return true;
            case InputEventKind.PointerUp or InputEventKind.PointerCancel:
                if (e.Pointer != _pressPointer) return false;
                _pressPointer = -1;
                _events.Enqueue(e);
                return true;
            case InputEventKind.PointerMove:
                _events.Enqueue(e);
                return e.Pointer == _pressPointer;
            case InputEventKind.Wheel:
                return Contains(PointerToUi(e.Position));
            case InputEventKind.KeyDown or InputEventKind.KeyUp:
                if (Focused is not TextField || !Focused.IsEnabledInTree) return false;
                if (e.Kind == InputEventKind.KeyDown) _events.Enqueue(e);
                return true;
            default:
                return false;
        }
    }

    /// <summary>Adds the focus and the widgets' state (in tree order) to the scene's state hash.</summary>
    public void HashState(StateHasher hash)
    {
        ArgumentNullException.ThrowIfNull(hash);
        int index = -1, i = 0;
        foreach (var w in AllWidgets)
        {
            if (ReferenceEquals(w, Focused)) index = i;
            i++;
        }
        hash.Add(index);
        foreach (var w in AllWidgets) w.HashState(hash);
    }

    internal void StartText(TextField field)
    {
        if (TextInput is null) return;
        var b = field.Bounds;
        var a = UiToCanvas(new Vector2(b.X, b.Y));
        var c = UiToCanvas(new Vector2(b.Right, b.Bottom));
        TextInput.Start(new RectF(a.X, a.Y, c.X - a.X, c.Y - a.Y));
    }

    internal void StopText(TextField field)
    {
        if (Focused is TextField) return; // another field took the focus and the channel with it
        TextInput?.Stop();
    }

    /// <summary>A widget left the tree: it loses the focus, the hover and the press.</summary>
    internal void Forget(Widget widget)
    {
        foreach (var w in Descendants(widget))
        {
            if (ReferenceEquals(Focused, w)) Focus(null);
            if (ReferenceEquals(Hovered, w)) Hovered = null;
            if (ReferenceEquals(Pressed, w)) Pressed = null;
        }
    }

    private static IEnumerable<Widget> Descendants(Widget root)
    {
        yield return root;
        foreach (var child in root.Children)
            foreach (var w in Descendants(child)) yield return w;
    }
}

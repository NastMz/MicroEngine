using System.Numerics;
using MicroEngine.Graphics;
using MicroEngine.Kernel;

namespace MicroEngine.UI;

/// <summary>What a widget is asked to do by the engine's actions (<see cref="UiActions"/>): move, confirm or cancel.</summary>
public enum UiAction
{
    /// <summary>The up action.</summary>
    Up,
    /// <summary>The down action.</summary>
    Down,
    /// <summary>The left action.</summary>
    Left,
    /// <summary>The right action.</summary>
    Right,
    /// <summary>The confirm action: press the focused button, submit the focused text field.</summary>
    Confirm,
    /// <summary>The cancel action.</summary>
    Cancel,
}

/// <summary>The direction a <see cref="Stack"/> places its children in.</summary>
public enum Orientation
{
    /// <summary>Top to bottom.</summary>
    Vertical,
    /// <summary>Left to right.</summary>
    Horizontal,
}

/// <summary>Where something is placed in the space it is given.</summary>
public enum Align
{
    /// <summary>It fills the space.</summary>
    Stretch,
    /// <summary>At the start: the left, or the top.</summary>
    Start,
    /// <summary>Centred.</summary>
    Center,
    /// <summary>At the end: the right, or the bottom.</summary>
    End,
}

/// <summary>A point of the UI view a <see cref="UiRoot"/> places its content at.</summary>
public enum Anchor
{
    /// <summary>The top-left corner.</summary>
    TopLeft,
    /// <summary>The middle of the top edge.</summary>
    Top,
    /// <summary>The top-right corner.</summary>
    TopRight,
    /// <summary>The middle of the left edge.</summary>
    Left,
    /// <summary>The centre.</summary>
    Center,
    /// <summary>The middle of the right edge.</summary>
    Right,
    /// <summary>The bottom-left corner.</summary>
    BottomLeft,
    /// <summary>The middle of the bottom edge.</summary>
    Bottom,
    /// <summary>The bottom-right corner.</summary>
    BottomRight,
}

/// <summary>What widgets measure and draw with: the root's font and theme, the layout unit and the root's clock.</summary>
/// <param name="Font">The font of every widget's text.</param>
/// <param name="Theme">The colours.</param>
/// <param name="Unit">
/// The layout unit in UI units: 1 for the built-in 10-pixel pixel font, proportionally more for a larger font, so margins
/// grow with the text. Widgets express their padding and spacing in it.
/// </param>
/// <param name="Time">Seconds the root has been updated, for blinking carets and other animation.</param>
public readonly record struct UiContext(IFont Font, UiTheme Theme, float Unit, float Time)
{
    /// <summary>The area drawing is restricted to (UI units), or null for none: set by containers that clip their children.</summary>
    public RectF? Clip { get; init; }

    /// <summary>
    /// Restricts drawing to <paramref name="area"/> within the current <see cref="Clip"/>, until the result is disposed,
    /// which restores the current clip. Draw with <paramref name="inner"/>, which carries the new clip, so a clip set
    /// further down nests inside this one. Use it instead of <see cref="IRenderer.SetClip"/>, which cannot nest.
    /// </summary>
    /// <example><code>using (context.ClipTo(renderer, Bounds, out var inner)) content.Draw(renderer, inner);</code></example>
    public UiClip ClipTo(IRenderer renderer, RectF area, out UiContext inner)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        var clip = Clip is { } outer ? outer.Intersection(area) : area;
        renderer.SetClip(clip);
        inner = this with { Clip = clip };
        return new UiClip(renderer, Clip);
    }
}

/// <summary>A clip set by <see cref="UiContext.ClipTo"/>; disposing it restores the clip that was set before.</summary>
public readonly struct UiClip : IDisposable
{
    private readonly IRenderer? _renderer;
    private readonly RectF? _previous;

    internal UiClip(IRenderer renderer, RectF? previous) => (_renderer, _previous) = (renderer, previous);

    /// <summary>Restores the clip that was set before.</summary>
    public void Dispose() => _renderer?.SetClip(_previous);
}

/// <summary>
/// One element of a user interface, laid out in UI units in the UI view (window pixels divided by the presentation's UI
/// scale) by its <see cref="UiRoot"/>: it says how large it wants to be (<see cref="Measure"/>), is given its
/// <see cref="Bounds"/> (<see cref="Arrange"/>) and draws itself there (<see cref="Draw"/>). Focusable widgets take the
/// focus, act on the engine's actions while they have it and on the pointer. Derive from it for a widget of a game's own.
/// </summary>
public abstract class Widget
{
    private UiRoot? _root;

    /// <summary>Where the widget was placed by the last layout, in UI units.</summary>
    public RectF Bounds { get; private set; }
    /// <summary>Whether the widget and its children are drawn, laid out and reachable (true by default).</summary>
    public bool Visible { get; set; } = true;
    /// <summary>Whether the widget and its children accept the focus, the pointer and actions (true by default); disabled widgets draw muted.</summary>
    public bool Enabled { get; set; } = true;
    /// <summary>A fixed width in UI units; null (the default) is the width the widget measures.</summary>
    public float? Width { get; set; }
    /// <summary>A fixed height in UI units; null (the default) is the height the widget measures.</summary>
    public float? Height { get; set; }
    /// <summary>The widget that contains this one, or null for a root's content or a widget not placed yet.</summary>
    public Widget? Parent { get; private set; }
    /// <summary>The root the widget is shown by, or null while it is not part of one.</summary>
    public UiRoot? Root => _root ?? Parent?.Root;
    /// <summary>The widgets this one contains, in focus and drawing order.</summary>
    public virtual IReadOnlyList<Widget> Children => [];
    /// <summary>Whether the widget can take the focus (false by default; buttons and text fields can).</summary>
    public virtual bool Focusable => false;
    /// <summary>
    /// Whether the widget restricts its children to its <see cref="Bounds"/> (false by default): what they draw outside
    /// is cut, and the pointer outside reaches them neither. A scrolling container does.
    /// </summary>
    public virtual bool ClipsChildren => false;
    /// <summary>Whether the widget has its root's focus.</summary>
    public bool IsFocused => Root is { } root && ReferenceEquals(root.Focused, this);
    /// <summary>Whether the pointer is over the widget.</summary>
    public bool IsHovered => Root is { } root && ReferenceEquals(root.Hovered, this);
    /// <summary>Whether a pointer press that started on the widget is held.</summary>
    public bool IsPressed => Root is { } root && ReferenceEquals(root.Pressed, this);

    /// <summary>Whether this widget and every widget containing it are enabled.</summary>
    public bool IsEnabledInTree => Enabled && (Parent?.IsEnabledInTree ?? true);
    /// <summary>Whether this widget and every widget containing it are visible.</summary>
    public bool IsVisibleInTree => Visible && (Parent?.IsVisibleInTree ?? true);
    internal bool CanFocus => Focusable && IsEnabledInTree && IsVisibleInTree;

    internal void SetRoot(UiRoot? root) => _root = root;

    /// <summary>Makes <paramref name="child"/> a child of this widget (containers call it when a child is added).</summary>
    /// <exception cref="InvalidOperationException">The child already belongs to another widget or root.</exception>
    protected void Adopt(Widget child)
    {
        ArgumentNullException.ThrowIfNull(child);
        if ((child.Parent is not null && !ReferenceEquals(child.Parent, this)) || child._root is not null)
            throw new InvalidOperationException("The widget already belongs to another widget or root; remove it there first.");
        child.Parent = this;
    }

    /// <summary>Detaches <paramref name="child"/> from this widget (containers call it when a child is removed).</summary>
    protected void Release(Widget child)
    {
        ArgumentNullException.ThrowIfNull(child);
        if (!ReferenceEquals(child.Parent, this)) return;
        if (Root is { } root) root.Forget(child);
        child.Parent = null;
    }

    /// <summary>The size the widget wants, in UI units: <see cref="Width"/> and <see cref="Height"/> when set, else what it measures; zero while invisible.</summary>
    public Vector2 Measure(UiContext context)
    {
        if (!Visible) return Vector2.Zero;
        var size = MeasureContent(context);
        return new Vector2(Width ?? size.X, Height ?? size.Y);
    }

    /// <summary>Places the widget at <paramref name="bounds"/> (UI units) and its children within it.</summary>
    public void Arrange(RectF bounds, UiContext context)
    {
        Bounds = bounds;
        ArrangeContent(bounds, context);
    }

    /// <summary>Draws the widget in its <see cref="Bounds"/>; nothing while invisible.</summary>
    public void Draw(IRenderer renderer, UiContext context)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        if (!Visible) return;
        if (!ClipsChildren)
        {
            DrawContent(renderer, context);
            return;
        }
        using (context.ClipTo(renderer, Bounds, out var inner)) DrawContent(renderer, inner);
    }

    /// <summary>The size the widget's content needs, in UI units.</summary>
    protected abstract Vector2 MeasureContent(UiContext context);

    /// <summary>Places the children within <paramref name="bounds"/>; nothing by default.</summary>
    protected virtual void ArrangeContent(RectF bounds, UiContext context) { }

    /// <summary>Draws the widget in <see cref="Bounds"/>, and its children.</summary>
    protected abstract void DrawContent(IRenderer renderer, UiContext context);

    /// <summary>Called for an action while the widget has the focus; return true when it acted, which consumes the action.</summary>
    protected internal virtual bool OnAction(UiAction action) => false;

    /// <summary>Called when a pointer press that started on the widget is released on it, after <see cref="OnPointerUp"/>.</summary>
    protected internal virtual void OnClick() { }

    /// <summary>
    /// Called when a pointer press starts on the widget (the focusable widget under the pointer, or its nearest focusable
    /// container). <paramref name="position"/> is in UI units, like <see cref="Bounds"/>. The widget then holds the press:
    /// it receives the press's moves and its release wherever the pointer goes.
    /// </summary>
    protected internal virtual void OnPointerDown(Vector2 position) { }

    /// <summary>Called when the pointer of a press the widget holds moves, inside the widget or not (dragging a slider's handle); <paramref name="position"/> is in UI units.</summary>
    protected internal virtual void OnPointerMove(Vector2 position) { }

    /// <summary>Called when a press the widget holds is released, inside the widget or not; <paramref name="position"/> is in UI units. A release on the widget then also calls <see cref="OnClick"/>.</summary>
    protected internal virtual void OnPointerUp(Vector2 position) { }

    /// <summary>Called when a press the widget holds is cancelled (the system took the touch, the widget left the tree, a container took it as a drag); no release or click follows.</summary>
    protected internal virtual void OnPointerCancel() { }

    /// <summary>
    /// Whether the widget takes a press that started on one of its children once the pointer moves further than a few
    /// units (false by default). The child's press is then cancelled and the widget receives <see cref="OnPointerDown"/>
    /// at the press's start and the moves that follow: a scrolling container dragged by a finger that landed on a button.
    /// A press on the widget itself, where no child takes it, is the widget's from the start.
    /// </summary>
    protected internal virtual bool InterceptsDrag => false;

    /// <summary>
    /// Called for the wheel over the widget (<paramref name="delta"/> in notches: y positive away from the player, x positive
    /// to the right). Return true when the widget used it; otherwise its container is asked, up to the root.
    /// </summary>
    protected internal virtual bool OnWheel(Vector2 delta) => false;

    /// <summary>Called when the widget gains (true) or loses (false) the focus.</summary>
    protected internal virtual void OnFocusChanged(bool focused) { }

    /// <summary>Called once per update of the root, before actions are read.</summary>
    protected internal virtual void Update(float dt) { }

    /// <summary>Adds the widget's state that decides what happens next (a text field's text) to the scene's state hash; nothing by default.</summary>
    protected internal virtual void HashState(StateHasher hash) { }
}

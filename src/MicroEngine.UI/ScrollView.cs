using System.Numerics;
using MicroEngine.Graphics;
using MicroEngine.Kernel;

namespace MicroEngine.UI;

/// <summary>
/// A window onto content longer than the space it has: a long menu, a list of saves, an achievement list, the credits.
/// Give it a fixed <see cref="Widget.Height"/> (or <see cref="Widget.Width"/> when <see cref="Orientation"/> is
/// horizontal); its content keeps the size it measures and is cut at the view's edges. It scrolls to show a widget
/// inside it when that widget takes the focus, with the wheel over it, and with a drag, also one that starts on a button
/// inside it (a finger on a phone). A thin bar on its edge shows where the window is.
/// </summary>
public sealed class ScrollView : Widget
{
    private Widget? _content;
    private float _offset, _max, _line = 10;
    private Vector2 _dragStart;
    private float _dragOffset;
    private Widget? _revealed;

    /// <summary>A view of <paramref name="content"/> (optional).</summary>
    public ScrollView(Widget? content = null) => Content = content;

    /// <summary>The widget scrolled, or null.</summary>
    public Widget? Content
    {
        get => _content;
        set
        {
            if (ReferenceEquals(value, _content)) return;
            if (_content is not null) Release(_content);
            if (value is not null) Adopt(value);
            _content = value;
            _offset = 0;
        }
    }

    /// <summary>The direction the content scrolls in (vertical by default).</summary>
    public Orientation Orientation { get; set; }

    /// <summary>How far the content is scrolled, in UI units, from 0 to <see cref="MaxOffset"/>.</summary>
    public float Offset
    {
        get => _offset;
        set => _offset = Math.Clamp(value, 0, _max);
    }

    /// <summary>How far the content can scroll by the last layout: its length beyond the view's, or 0 when it fits.</summary>
    public float MaxOffset => _max;

    /// <summary>How far one notch of the wheel scrolls, in UI units; null (the default) is three lines of the font.</summary>
    public float? WheelStep { get; set; }

    /// <summary>Whether the bar that shows the scroll position is drawn while the content does not fit (true by default).</summary>
    public bool ShowScrollBar { get; set; } = true;

    /// <inheritdoc/>
    public override IReadOnlyList<Widget> Children => _content is null ? [] : [_content];

    /// <inheritdoc/>
    public override bool ClipsChildren => true;

    /// <summary>A drag inside the view scrolls it, while the content does not fit.</summary>
    protected internal override bool InterceptsDrag => _max > 0;

    private bool Vertical => Orientation == Orientation.Vertical;

    /// <summary>The strip along the edge that holds the scroll bar, beside the content (UI units).</summary>
    private float Gutter(UiContext context) => ShowScrollBar ? 4 * context.Unit : 0;

    /// <summary>The content's size, and the scroll bar's strip when the content is longer than the fixed length.</summary>
    protected override Vector2 MeasureContent(UiContext context)
    {
        var size = _content?.Measure(context) ?? Vector2.Zero;
        if (Vertical && Height is { } h && size.Y > h) size.X += Gutter(context);
        if (!Vertical && Width is { } w && size.X > w) size.Y += Gutter(context);
        return size;
    }

    /// <inheritdoc/>
    protected override void ArrangeContent(RectF bounds, UiContext context)
    {
        _line = context.Font.LineHeight + context.Unit;
        if (_content is null)
        {
            _max = _offset = 0;
            return;
        }
        var size = _content.Measure(context);
        _max = MathF.Max(0, Vertical ? size.Y - bounds.Height : size.X - bounds.Width);
        _offset = Math.Clamp(_offset, 0, _max);
        Place(bounds, size, context);
        // A widget inside that has just taken the focus is scrolled into view, once: the wheel and drags may move it out.
        var focused = Root?.Focused;
        if (focused is null || !Contains(focused))
        {
            _revealed = null;
            return;
        }
        if (ReferenceEquals(focused, _revealed)) return;
        _revealed = focused;
        var f = focused.Bounds;
        float before = Vertical ? bounds.Y - f.Y : bounds.X - f.X;
        float after = Vertical ? f.Bottom - bounds.Bottom : f.Right - bounds.Right;
        if (before > 0) Offset -= before;
        else if (after > 0) Offset += MathF.Min(after, -before);
        else return;
        Place(bounds, size, context);
    }

    private void Place(RectF bounds, Vector2 size, UiContext context)
    {
        float o = MathF.Round(_offset), gutter = _max > 0 ? Gutter(context) : 0;
        _content!.Arrange(Vertical
            ? new RectF(bounds.X, bounds.Y - o, MathF.Max(0, bounds.Width - gutter), size.Y)
            : new RectF(bounds.X - o, bounds.Y, size.X, MathF.Max(0, bounds.Height - gutter)), context);
    }

    private bool Contains(Widget widget)
    {
        for (var w = widget.Parent; w is not null; w = w.Parent)
            if (ReferenceEquals(w, this)) return true;
        return false;
    }

    /// <inheritdoc/>
    protected override void DrawContent(IRenderer renderer, UiContext context)
    {
        _content?.Draw(renderer, context);
        if (!ShowScrollBar || _max <= 0) return;
        float u = context.Unit, room = Vertical ? Bounds.Height : Bounds.Width, length = room + _max;
        float thumb = MathF.Max(4 * u, room * room / length), at = (room - thumb) * (_offset / _max);
        // In the gutter's outer half, so a unit of space separates it from the content.
        var bar = Vertical
            ? new RectF(Bounds.Right - 2 * u, Bounds.Y + at, 2 * u, thumb)
            : new RectF(Bounds.X + at, Bounds.Bottom - 2 * u, thumb, 2 * u);
        renderer.FillRect(bar, context.Theme.Muted);
    }

    /// <summary>Scrolls by the wheel: away from the player goes back towards the start.</summary>
    protected internal override bool OnWheel(Vector2 delta)
    {
        if (_max <= 0) return false;
        float step = WheelStep ?? 3 * _line;
        Offset += (Vertical || delta.X == 0 ? -delta.Y : delta.X) * step;
        return true;
    }

    /// <inheritdoc/>
    protected internal override void OnPointerDown(Vector2 position)
    {
        _dragStart = position;
        _dragOffset = _offset;
    }

    /// <summary>The content follows the pointer.</summary>
    protected internal override void OnPointerMove(Vector2 position) =>
        Offset = _dragOffset - (Vertical ? position.Y - _dragStart.Y : position.X - _dragStart.X);

    /// <inheritdoc/>
    protected internal override void HashState(StateHasher hash) => hash.Add(_offset);
}

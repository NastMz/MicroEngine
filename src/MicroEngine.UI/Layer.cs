using System.Numerics;
using MicroEngine.Graphics;

namespace MicroEngine.UI;

/// <summary>
/// The whole UI view, with each child placed on its own: against an edge or a corner (a banner in a corner, a HUD
/// block), or at a point (a speech bubble over a character, a damage number, a marker that follows something in the
/// world). Children keep the size they measure; later children draw over earlier ones. Where no child is, the pointer
/// passes through to gameplay. Use it as a root's <see cref="UiRoot.Content"/>.
/// </summary>
public sealed class Layer : Widget
{
    private readonly List<Widget> _children = [];
    private readonly Dictionary<Widget, (Anchor Anchor, Vector2 Position, bool AtPoint)> _places = [];

    /// <inheritdoc/>
    public override IReadOnlyList<Widget> Children => _children;

    /// <summary>The layer's empty areas are not the UI's: presses there reach gameplay.</summary>
    public override bool BlocksPointer => false;

    /// <summary>
    /// Adds <paramref name="child"/> with its <paramref name="anchor"/> point on the layer's same point, moved by
    /// <paramref name="offset"/>: <c>Add(banner, Anchor.TopRight, new(-4, 4))</c> puts a banner 4 units from the top-right
    /// corner. Returns the layer.
    /// </summary>
    /// <exception cref="InvalidOperationException">The child already belongs to another widget or root.</exception>
    public Layer Add(Widget child, Anchor anchor = Anchor.TopLeft, Vector2 offset = default)
    {
        Adopt(child);
        if (!_children.Contains(child)) _children.Add(child);
        _places[child] = (anchor, offset, false);
        return this;
    }

    /// <summary>
    /// Adds <paramref name="child"/> with its <paramref name="pivot"/> point at <paramref name="point"/> (UI units from the
    /// layer's top-left): <c>AddAt(bubble, head, Anchor.Bottom)</c> puts a bubble's bottom centre over a head. Returns the
    /// layer.
    /// </summary>
    /// <exception cref="InvalidOperationException">The child already belongs to another widget or root.</exception>
    public Layer AddAt(Widget child, Vector2 point, Anchor pivot = Anchor.Center)
    {
        Adopt(child);
        if (!_children.Contains(child)) _children.Add(child);
        _places[child] = (pivot, point, true);
        return this;
    }

    /// <summary>Places <paramref name="child"/> against the layer's <paramref name="anchor"/> point, as <see cref="Add"/> does.</summary>
    /// <exception cref="ArgumentException">The child is not in this layer.</exception>
    public void Place(Widget child, Anchor anchor, Vector2 offset = default) => _places[Own(child)] = (anchor, offset, false);

    /// <summary>
    /// Moves <paramref name="child"/>'s pivot to <paramref name="point"/>, keeping its pivot (the centre if it was placed
    /// against an edge). Call it every update to follow something that moves.
    /// </summary>
    /// <exception cref="ArgumentException">The child is not in this layer.</exception>
    public void Move(Widget child, Vector2 point)
    {
        var (anchor, _, atPoint) = _places[Own(child)];
        _places[child] = (atPoint ? anchor : Anchor.Center, point, true);
    }

    /// <summary>Removes <paramref name="child"/>; false when it was not in the layer.</summary>
    public bool Remove(Widget child)
    {
        if (!_children.Remove(child)) return false;
        _places.Remove(child);
        Release(child);
        return true;
    }

    private Widget Own(Widget child) =>
        child is not null && _places.ContainsKey(child) ? child : throw new ArgumentException("The widget is not in this layer.", nameof(child));

    /// <summary>The UI view's size: a layer fills it.</summary>
    protected override Vector2 MeasureContent(UiContext context) => Root?.Size ?? Vector2.Zero;

    /// <inheritdoc/>
    protected override void ArrangeContent(RectF bounds, UiContext context)
    {
        foreach (var child in _children)
        {
            if (!child.Visible) continue;
            var size = child.Measure(context);
            var (anchor, position, atPoint) = _places[child];
            var f = Fraction(anchor);
            var at = atPoint
                ? bounds.Position + position - f * size
                : bounds.Position + f * (bounds.Size - size) + position;
            child.Arrange(new RectF(MathF.Round(at.X), MathF.Round(at.Y), size.X, size.Y), context);
        }
    }

    /// <summary>Where <paramref name="anchor"/> lies in a box, as fractions of its width and height.</summary>
    private static Vector2 Fraction(Anchor anchor) => anchor switch
    {
        Anchor.TopLeft => new(0, 0),
        Anchor.Top => new(0.5f, 0),
        Anchor.TopRight => new(1, 0),
        Anchor.Left => new(0, 0.5f),
        Anchor.Center => new(0.5f, 0.5f),
        Anchor.Right => new(1, 0.5f),
        Anchor.BottomLeft => new(0, 1),
        Anchor.Bottom => new(0.5f, 1),
        _ => new(1, 1),
    };

    /// <inheritdoc/>
    protected override void DrawContent(IRenderer renderer, UiContext context)
    {
        foreach (var child in _children) child.Draw(renderer, context);
    }
}

using System.Numerics;
using MicroEngine.Graphics;

namespace MicroEngine.UI;

/// <summary>
/// Children in rows of <see cref="Columns"/> equal cells, left to right then top to bottom: an inventory, a level select,
/// a character roster, a keyboard on screen. Every cell is as large as the largest child; hidden children take no cell.
/// Focus moves between cells by direction, as it does anywhere in the tree.
/// </summary>
public sealed class Grid : Widget
{
    private readonly List<Widget> _children = [];
    private int _columns;

    /// <summary>A grid of <paramref name="columns"/> columns.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="columns"/> is less than 1.</exception>
    public Grid(int columns) => Columns = columns;

    /// <summary>The number of cells in a row, at least 1.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is less than 1.</exception>
    public int Columns
    {
        get => _columns;
        set => _columns = value >= 1 ? value : throw new ArgumentOutOfRangeException(nameof(value), "A grid needs at least one column.");
    }

    /// <summary>Space between cells in UI units, across and down; null (the default) is 4 layout units.</summary>
    public float? Spacing { get; set; }

    /// <summary>How a child sits in its cell (<see cref="Align.Stretch"/> by default: it fills the cell; otherwise it keeps its size, aligned on both axes).</summary>
    public Align Align { get; set; }

    /// <inheritdoc/>
    public override IReadOnlyList<Widget> Children => _children;

    /// <summary>Adds <paramref name="child"/> in the next cell and returns the grid, so grids can be written in one expression.</summary>
    /// <exception cref="InvalidOperationException">The child already belongs to another widget or root.</exception>
    public Grid Add(Widget child)
    {
        Adopt(child);
        if (!_children.Contains(child)) _children.Add(child);
        return this;
    }

    /// <summary>Removes <paramref name="child"/>; the children after it move back one cell. False when it was not in the grid.</summary>
    public bool Remove(Widget child)
    {
        if (!_children.Remove(child)) return false;
        Release(child);
        return true;
    }

    private float SpacingOf(UiContext context) => Spacing ?? 4 * context.Unit;

    /// <summary>The size of one cell (the largest child) and the number of visible children.</summary>
    private (Vector2 Cell, int Shown) Cells(UiContext context)
    {
        var cell = Vector2.Zero;
        int shown = 0;
        foreach (var child in _children)
        {
            if (!child.Visible) continue;
            cell = Vector2.Max(cell, child.Measure(context));
            shown++;
        }
        return (cell, shown);
    }

    /// <inheritdoc/>
    protected override Vector2 MeasureContent(UiContext context)
    {
        var (cell, shown) = Cells(context);
        if (shown == 0) return Vector2.Zero;
        int columns = Math.Min(_columns, shown), rows = (shown + _columns - 1) / _columns;
        float spacing = SpacingOf(context);
        return new Vector2(columns * cell.X + (columns - 1) * spacing, rows * cell.Y + (rows - 1) * spacing);
    }

    /// <inheritdoc/>
    protected override void ArrangeContent(RectF bounds, UiContext context)
    {
        var (cell, _) = Cells(context);
        float spacing = SpacingOf(context);
        int index = 0;
        foreach (var child in _children)
        {
            if (!child.Visible) continue;
            float x = bounds.X + index % _columns * (cell.X + spacing), y = bounds.Y + index / _columns * (cell.Y + spacing);
            index++;
            if (Align == Align.Stretch)
            {
                child.Arrange(new RectF(x, y, cell.X, cell.Y), context);
                continue;
            }
            var size = Vector2.Min(child.Measure(context), cell);
            float shift = Align switch { Align.Center => 0.5f, Align.End => 1f, _ => 0f };
            child.Arrange(new RectF(x + MathF.Round((cell.X - size.X) * shift), y + MathF.Round((cell.Y - size.Y) * shift), size.X, size.Y), context);
        }
    }

    /// <inheritdoc/>
    protected override void DrawContent(IRenderer renderer, UiContext context)
    {
        foreach (var child in _children) child.Draw(renderer, context);
    }
}

using System.Numerics;
using MicroEngine.Graphics;

namespace MicroEngine.UI;

/// <summary>Text that does nothing: a heading, a value, a hint. Lines are separated by <c>\n</c>.</summary>
public sealed class Label : Widget
{
    /// <summary>A label showing <paramref name="text"/>.</summary>
    public Label(string text = "") => Text = text;

    /// <summary>The text; null counts as empty.</summary>
    public string Text { get => _text; set => _text = value ?? ""; }
    private string _text = "";
    /// <summary>The text's colour; null (the default) is the theme's text colour, or its muted colour while disabled.</summary>
    public Color? Color { get; set; }
    /// <summary>Whether the text has the theme's shadow under it (true by default).</summary>
    public bool Shadow { get; set; } = true;

    /// <inheritdoc/>
    protected override Vector2 MeasureContent(UiContext context)
    {
        var lines = Text.Split('\n');
        float width = lines.Max(l => context.Font.MeasureWidth(l));
        return new Vector2(width, lines.Length * context.Font.LineHeight + (lines.Length - 1) * context.Unit);
    }

    /// <inheritdoc/>
    protected override void DrawContent(IRenderer renderer, UiContext context)
    {
        var color = Color ?? (IsEnabledInTree ? context.Theme.Text : context.Theme.Muted);
        float y = Bounds.Y;
        foreach (var line in Text.Split('\n'))
        {
            var at = new Vector2(Bounds.X, y);
            if (Shadow) context.Font.DrawShadowed(renderer, line, at, color, context.Theme.TextShadow, new Vector2(context.Unit));
            else context.Font.Draw(renderer, line, at, color);
            y += context.Font.LineHeight + context.Unit;
        }
    }
}

/// <summary>
/// A button: confirm while it has the focus, or a click (a press and release of the pointer on it), raises
/// <see cref="Clicked"/>. It takes the focus.
/// </summary>
public sealed class Button : Widget
{
    /// <summary>A button labelled <paramref name="text"/>, calling <paramref name="clicked"/> (optional) when clicked.</summary>
    public Button(string text, Action? clicked = null)
    {
        Text = text;
        if (clicked is not null) Clicked += clicked;
    }

    /// <summary>The label; null counts as empty.</summary>
    public string Text { get => _text; set => _text = value ?? ""; }
    private string _text = "";
    /// <summary>Raised when the button is clicked.</summary>
    public event Action? Clicked;
    /// <inheritdoc/>
    public override bool Focusable => true;

    /// <summary>Clicks the button as confirm or the pointer would: raises <see cref="Clicked"/> unless it is disabled. Returns whether it did.</summary>
    public bool Click()
    {
        if (!IsEnabledInTree) return false;
        Clicked?.Invoke();
        return true;
    }

    /// <inheritdoc/>
    protected internal override bool OnAction(UiAction action) => action == UiAction.Confirm && Click();

    /// <inheritdoc/>
    protected internal override void OnClick() => Click();

    /// <inheritdoc/>
    protected override Vector2 MeasureContent(UiContext context) =>
        new(context.Font.MeasureWidth(Text) + 12 * context.Unit, context.Font.LineHeight + 8 * context.Unit);

    /// <inheritdoc/>
    protected override void DrawContent(IRenderer renderer, UiContext context)
    {
        var theme = context.Theme;
        bool enabled = IsEnabledInTree;
        var style = theme.Panel;
        if (IsFocused && enabled) style = style with { Border = theme.Accent };
        if (IsPressed) style = style with { Fill = Color.Lerp(style.Fill, theme.Accent, 0.25f) };
        renderer.Panel(Bounds, style);
        var color = !enabled ? theme.Muted : IsFocused ? theme.Accent : theme.Text;
        var size = context.Font.Measure(Text);
        var at = new Vector2(MathF.Round(Bounds.X + (Bounds.Width - size.X) / 2), MathF.Round(Bounds.Y + (Bounds.Height - context.Font.LineHeight) / 2));
        context.Font.DrawShadowed(renderer, Text, at, color, theme.TextShadow, new Vector2(context.Unit));
    }
}

/// <summary>A box behind one child, with padding around it: the frame of a menu, a dialog or a HUD block.</summary>
public sealed class Panel : Widget
{
    private Widget? _content;

    /// <summary>A panel around <paramref name="content"/> (optional).</summary>
    public Panel(Widget? content = null) => Content = content;

    /// <summary>The widget inside, or null for an empty box.</summary>
    public Widget? Content
    {
        get => _content;
        set
        {
            if (ReferenceEquals(value, _content)) return;
            if (_content is not null) Release(_content);
            if (value is not null) Adopt(value);
            _content = value;
        }
    }

    /// <summary>Space between the edge and the content in UI units; null (the default) is 6 layout units.</summary>
    public float? Padding { get; set; }
    /// <summary>The box's colours; null (the default) is the theme's panel.</summary>
    public PanelStyle? Style { get; set; }

    /// <inheritdoc/>
    public override IReadOnlyList<Widget> Children => _content is null ? [] : [_content];

    private float PaddingOf(UiContext context) => Padding ?? 6 * context.Unit;

    /// <inheritdoc/>
    protected override Vector2 MeasureContent(UiContext context) =>
        (_content?.Measure(context) ?? Vector2.Zero) + new Vector2(2 * PaddingOf(context));

    /// <inheritdoc/>
    protected override void ArrangeContent(RectF bounds, UiContext context)
    {
        float p = PaddingOf(context);
        _content?.Arrange(new RectF(bounds.X + p, bounds.Y + p, MathF.Max(0, bounds.Width - 2 * p), MathF.Max(0, bounds.Height - 2 * p)), context);
    }

    /// <inheritdoc/>
    protected override void DrawContent(IRenderer renderer, UiContext context)
    {
        renderer.Panel(Bounds, Style ?? context.Theme.Panel);
        _content?.Draw(renderer, context);
    }
}

/// <summary>Children placed one after another, top to bottom or left to right, with a space between them: menus, rows of buttons, forms.</summary>
public sealed class Stack : Widget
{
    private readonly List<Widget> _children = [];

    /// <summary>The direction children follow each other (<see cref="Orientation.Vertical"/> by default).</summary>
    public Orientation Orientation { get; set; }
    /// <summary>Space between children in UI units; null (the default) is 4 layout units.</summary>
    public float? Spacing { get; set; }
    /// <summary>How children are placed across the stack (<see cref="Align.Stretch"/> by default: as wide as the stack, or as tall in a row).</summary>
    public Align Align { get; set; }
    /// <inheritdoc/>
    public override IReadOnlyList<Widget> Children => _children;

    /// <summary>Adds <paramref name="child"/> after the others and returns the stack, so stacks can be written in one expression.</summary>
    /// <exception cref="InvalidOperationException">The child already belongs to another widget or root.</exception>
    public Stack Add(Widget child)
    {
        Adopt(child);
        if (!_children.Contains(child)) _children.Add(child);
        return this;
    }

    /// <summary>Removes <paramref name="child"/>; false when it was not in the stack.</summary>
    public bool Remove(Widget child)
    {
        if (!_children.Remove(child)) return false;
        Release(child);
        return true;
    }

    private float SpacingOf(UiContext context) => Spacing ?? 4 * context.Unit;

    /// <inheritdoc/>
    protected override Vector2 MeasureContent(UiContext context)
    {
        float main = 0, cross = 0;
        int shown = 0;
        foreach (var child in _children)
        {
            if (!child.Visible) continue;
            var size = child.Measure(context);
            main += Orientation == Orientation.Vertical ? size.Y : size.X;
            cross = MathF.Max(cross, Orientation == Orientation.Vertical ? size.X : size.Y);
            shown++;
        }
        main += Math.Max(0, shown - 1) * SpacingOf(context);
        return Orientation == Orientation.Vertical ? new Vector2(cross, main) : new Vector2(main, cross);
    }

    /// <inheritdoc/>
    protected override void ArrangeContent(RectF bounds, UiContext context)
    {
        bool vertical = Orientation == Orientation.Vertical;
        float at = vertical ? bounds.Y : bounds.X, room = vertical ? bounds.Width : bounds.Height, spacing = SpacingOf(context);
        foreach (var child in _children)
        {
            if (!child.Visible) continue;
            var size = child.Measure(context);
            float length = vertical ? size.Y : size.X, wanted = vertical ? size.X : size.Y;
            float across = Align == Align.Stretch ? room : MathF.Min(wanted, room);
            float offset = Align switch { Align.Center => MathF.Round((room - across) / 2), Align.End => room - across, _ => 0 };
            child.Arrange(vertical ? new RectF(bounds.X + offset, at, across, length) : new RectF(at, bounds.Y + offset, length, across), context);
            at += length + spacing;
        }
    }

    /// <inheritdoc/>
    protected override void DrawContent(IRenderer renderer, UiContext context)
    {
        foreach (var child in _children) child.Draw(renderer, context);
    }
}

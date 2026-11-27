using System.Numerics;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Localization;

namespace MicroEngine.UI;

/// <summary>
/// Text that does nothing: a heading, a value, a hint, a line of dialogue. Lines are separated by <c>\n</c>; with
/// <see cref="Wrap"/> and a fixed <see cref="Widget.Width"/> the text also breaks between words to fit. Set
/// <see cref="VisibleCharacters"/> to reveal it a little at a time: the lines stay where the whole text puts them.
/// </summary>
public sealed class Label : Widget
{
    private string[] _lines = [""];
    private (string Text, IFont? Font, float Width) _laidOut;

    /// <summary>A label showing <paramref name="text"/>.</summary>
    public Label(string text = "") => Text = text;

    /// <summary>A label showing the text of a key of the game's string tables, in the language in use (<see cref="Localized"/>).</summary>
    public Label(LocalizedText text) => Localized = text;

    /// <summary>
    /// The text. With <see cref="Localized"/> set it is that key's text in the language in use, as of the last layout;
    /// setting it makes the label plain text again. Null counts as empty.
    /// </summary>
    public string Text { get => _text; set { _text = value ?? ""; _localized.Text = null; } }
    private string _text = "";
    private LocalizedField _localized;

    /// <summary>
    /// A key of the game's string tables and the values its message shows: the label shows that text in the language
    /// in use and changes when the language does. Set it again when a value changes
    /// (<c>label.Localized = new("hud.gems", ("count", gems))</c>); the same key and values cost nothing. Null (the
    /// default) is plain <see cref="Text"/>.
    /// </summary>
    public LocalizedText? Localized { get => _localized.Text; set => _localized.Text = value; }
    /// <summary>The text's colour; null (the default) is the theme's text colour, or its muted colour while disabled.</summary>
    public Color? Color { get; set; }
    /// <summary>Whether the text has the theme's shadow under it (true by default).</summary>
    public bool Shadow { get; set; } = true;

    /// <summary>
    /// Whether the text breaks between words so no line is wider than the label's fixed <see cref="Widget.Width"/>
    /// (false by default); a word wider than that on its own is broken between characters. Without a fixed width the
    /// text keeps its lines. The label grows in height to hold the lines.
    /// </summary>
    public bool Wrap { get; set; }

    /// <summary>
    /// How many characters of <see cref="Text"/> are drawn, counted in the text as written (spaces and <c>\n</c>
    /// included); null (the default) draws all of it. The label keeps the size and the lines of its whole text, so a
    /// text revealed a little at a time never moves. Raise it over time for a typewriter effect.
    /// </summary>
    public int? VisibleCharacters { get; set; }

    /// <summary>
    /// Reveals the label's text a character at a time (<see cref="TextReveal"/>): the label starts it whenever its
    /// text changes (a new page, another language) and advances it at every update of its root, on the scene's clock,
    /// so a dialogue sets the text and reads <see cref="TextReveal.Complete"/>. Characters are counted as a reader
    /// counts them, so text in any script is never cut inside one. While it is set it decides what shows, and
    /// <see cref="VisibleCharacters"/> is not read. It is part of the scene's state hash. Null (the default) reveals
    /// nothing by itself.
    /// </summary>
    /// <example>
    /// <code>
    /// var text = new Label { Width = 280, Wrap = true, Reveal = new TextReveal { CharactersPerSecond = 45 } };
    /// text.Reveal.CharacterRevealed += _ =&gt; Audio.TryPlay("blip", 0.3f);
    /// text.Text = "The gems only grow at night.";      // starts the reveal
    /// // on confirm
    /// if (!text.Reveal.Complete) text.Reveal.Finish(); else text.Text = NextPage();
    /// </code>
    /// </example>
    public TextReveal? Reveal { get; set; }

    /// <summary>The lines the text is drawn in, after wrapping, as of the last layout.</summary>
    public IReadOnlyList<string> Lines => _lines;

    /// <summary>
    /// The part of each of <see cref="Lines"/> that shows: all of it, or what <see cref="Reveal"/> or
    /// <see cref="VisibleCharacters"/> lets through. While the label keeps its lines the list is the same one, brought
    /// up to date in place: a line that shows whole is the line itself, a line not reached is empty, and the one line
    /// being revealed is a new string each time another character of it shows. Nothing else is allocated, and nothing
    /// at all while what shows does not change.
    /// </summary>
    public IReadOnlyList<string> VisibleLines
    {
        get
        {
            if (ShownCharacters() is not { } count) return _lines;
            bool sameLines = ReferenceEquals(_visibleOf, _lines) && _visible is not null;
            if (sameLines && _visibleCount == count) return _visible!;
            // The same list while the lines are the same: only the entries that changed are written.
            var visible = sameLines ? _visible! : new string[_lines.Length];
            int at = 0;
            for (int i = 0; i < _lines.Length; i++)
            {
                var line = _lines[i];
                at = StartOf(line, at);
                int shown = Math.Clamp(count - at, 0, line.Length);
                if (shown == line.Length) visible[i] = line;
                else if (shown == 0) visible[i] = "";
                else if (!sameLines || visible[i] is not { } was || was.Length != shown) visible[i] = line[..shown];
                at += line.Length;
            }
            (_visible, _visibleOf, _visibleCount) = (visible, _lines, count);
            return visible;
        }
    }
    private string[]? _visible, _visibleOf;
    private int _visibleCount;

    /// <summary>How many <c>char</c>s of the text show, or null for all: the reveal's, once it is revealing this text, else <see cref="VisibleCharacters"/>.</summary>
    private int? ShownCharacters() =>
        Reveal is { } reveal ? (reveal.Text == _text ? reveal.VisibleLength : 0) : VisibleCharacters;

    /// <inheritdoc/>
    protected internal override void Update(float dt)
    {
        if (Reveal is not { } reveal) return;
        if (reveal.Text != _text) reveal.Start(_text);
        reveal.Update(dt);
    }

    /// <inheritdoc/>
    protected internal override void HashState(StateHasher hash)
    {
        if (Reveal is { } reveal) hash.Add(reveal);
    }

    /// <summary>Where <paramref name="line"/> starts in the text, from <paramref name="from"/>: past the spaces and line breaks a wrap took.</summary>
    private int StartOf(string line, int from)
    {
        while (from < _text.Length && string.CompareOrdinal(_text, from, line, 0, line.Length) != 0) from++;
        return from;
    }

    /// <summary>
    /// Where line <paramref name="line"/> of <see cref="Lines"/> starts horizontally, in UI units, as of the last
    /// draw: the label's left edge, or in a right-to-left language the point that puts the line's end at the label's
    /// right edge.
    /// </summary>
    public float LineStart(int line) => _lineStarts is { } starts && (uint)line < (uint)starts.Length ? starts[line] : Bounds.X;
    private float[]? _lineStarts;

    private void LayOut(UiContext context)
    {
        _localized.Resolve(context, ref _text);
        float width = Wrap && Width is { } w ? w : float.PositiveInfinity;
        if (_laidOut == (_text, context.Font, width)) return;
        _lines = float.IsPositiveInfinity(width) ? _text.Split('\n') : [.. context.Font.Wrap(_text, width)];
        _laidOut = (_text, context.Font, width);
    }

    /// <inheritdoc/>
    protected override Vector2 MeasureContent(UiContext context)
    {
        LayOut(context);
        float width = _lines.Max(l => context.Font.MeasureWidth(l));
        return new Vector2(width, _lines.Length * context.Font.LineHeight + (_lines.Length - 1) * context.Unit);
    }

    /// <inheritdoc/>
    protected override void DrawContent(IRenderer renderer, UiContext context)
    {
        LayOut(context);
        var color = Color is { } own ? context.Fade(own) : IsEnabledInTree ? context.Theme.Text : context.Theme.Muted;
        float y = Bounds.Y;
        var lines = VisibleLines;
        if (_lineStarts is null || _lineStarts.Length != lines.Count) _lineStarts = new float[lines.Count];
        for (int i = 0; i < lines.Count; i++)
        {
            string line = lines[i];
            // A right-to-left line starts from the right edge; revealed a little at a time, it grows leftwards.
            float x = context.RightToLeft ? MathF.Round(Bounds.Right - context.Font.MeasureWidth(line)) : Bounds.X;
            _lineStarts[i] = x;
            var at = new Vector2(x, y);
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

    /// <summary>A button labelled with the text of a key of the game's string tables (<see cref="Localized"/>), calling <paramref name="clicked"/> (optional) when clicked.</summary>
    public Button(LocalizedText text, Action? clicked = null)
    {
        Localized = text;
        if (clicked is not null) Clicked += clicked;
    }

    /// <summary>The label; with <see cref="Localized"/> set, that key's text in the language in use as of the last layout. Setting it makes the label plain text again. Null counts as empty.</summary>
    public string Text { get => _text; set { _text = value ?? ""; _localized.Text = null; } }
    private string _text = "";
    private LocalizedField _localized;
    /// <summary>A key of the game's string tables: the button shows its text in the language in use and changes with it. Null (the default) is plain <see cref="Text"/>.</summary>
    public LocalizedText? Localized { get => _localized.Text; set => _localized.Text = value; }
    /// <summary>Raised when the button is clicked.</summary>
    public event Action? Clicked;

    /// <summary>
    /// Draws the button in the game's own look, from its state (the focus, the pointer, the press, the time it has had
    /// the focus): a menu row that is a bar, an underline and a bobbing cursor, a tab, a card. Null (the default) is
    /// the theme's (<see cref="UiTheme.ButtonSkin"/>), or the built-in box around the label. The button keeps its
    /// behaviour and its size (<see cref="Padding"/>).
    /// </summary>
    public ControlSkin? Skin { get; set; }

    /// <summary>
    /// The room around the label in UI units, each side: X to its left and to its right, Y above and below. Null (the
    /// default) is 6 and 4 layout units, the built-in box's. A skin without a box usually sets it: a row as tall as a
    /// line of text is <c>new(5, 0)</c>.
    /// </summary>
    public Vector2? Padding { get; set; }
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
    protected override Vector2 MeasureContent(UiContext context)
    {
        _localized.Resolve(context, ref _text);
        var padding = Padding ?? new Vector2(6 * context.Unit, 4 * context.Unit);
        return new(context.Font.MeasureWidth(Text) + 2 * padding.X, context.Font.LineHeight + 2 * padding.Y);
    }

    /// <inheritdoc/>
    protected override void DrawContent(IRenderer renderer, UiContext context)
    {
        _localized.Resolve(context, ref _text);
        var theme = context.Theme;
        bool enabled = IsEnabledInTree;
        if ((Skin ?? theme.ButtonSkin) is { } skin)
        {
            skin(renderer, context, new ControlState(Bounds, Text, enabled, IsFocused, IsHovered, IsPressed, FocusTime));
            return;
        }
        var style = theme.Panel;
        if (style.Image is not null)
        {
            // An image of the game's own shows the state by its tint: the accent while focused, darker while pressed.
            var tint = style.ImageTint ?? Color.White;
            if (IsFocused && enabled) tint = Multiply(tint, theme.Accent);
            if (IsPressed) tint = Color.Lerp(tint, new Color(0, 0, 0, tint.A), 0.25f);
            style = style with { ImageTint = tint };
        }
        if (IsFocused && enabled) style = style with { Border = theme.Accent };
        if (IsPressed) style = style with { Fill = Color.Lerp(style.Fill, theme.Accent, 0.25f) };
        renderer.Panel(Bounds, style);
        var color = !enabled ? theme.Muted : IsFocused ? theme.Accent : theme.Text;
        var size = context.Font.Measure(Text);
        var at = new Vector2(MathF.Round(Bounds.X + (Bounds.Width - size.X) / 2), MathF.Round(Bounds.Y + (Bounds.Height - context.Font.LineHeight) / 2));
        context.Font.DrawShadowed(renderer, Text, at, color, theme.TextShadow, new Vector2(context.Unit));
    }

    private static Color Multiply(Color a, Color b) =>
        new((byte)(a.R * b.R / 255), (byte)(a.G * b.G / 255), (byte)(a.B * b.B / 255), (byte)(a.A * b.A / 255));
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

    /// <summary>
    /// Draws the box in the game's own look (a double border, a shadow of its own, a speech bubble's tail) instead of
    /// <see cref="Style"/>; the content is drawn over it as always. Null (the default) is the theme's
    /// (<see cref="UiTheme.PanelSkin"/>), or the panel style.
    /// </summary>
    public ControlSkin? Skin { get; set; }

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
        if ((Skin ?? (Style is null ? context.Theme.PanelSkin : null)) is { } skin)
            skin(renderer, context, new ControlState(Bounds, "", IsEnabledInTree, IsFocused, IsHovered, IsPressed, FocusTime));
        else renderer.Panel(Bounds, Style is { } own ? context.Fade(own) : context.Theme.Panel);
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

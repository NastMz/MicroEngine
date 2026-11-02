using System.Numerics;
using MicroEngine.Graphics;
using MicroEngine.Input;

namespace MicroEngine.UI;

/// <summary>Reveals text character by character. Confirm skips to the end; a second confirm advances.</summary>
public sealed class Typewriter
{
    private float _visible;
    /// <summary>The full text being revealed.</summary>
    public string Text { get; private set; } = "";
    /// <summary>Reveal speed (45 by default).</summary>
    public float CharactersPerSecond { get; set; } = 45f;
    /// <summary>Number of characters revealed so far.</summary>
    public int VisibleCount => Math.Min(Text.Length, (int)_visible);
    /// <summary>Whether the whole text is revealed.</summary>
    public bool Complete => VisibleCount >= Text.Length;
    /// <summary>The revealed part of the text.</summary>
    public string Visible => Text[..VisibleCount];
    /// <summary>Raised for each newly revealed non-space character (for blip sounds).</summary>
    public event Action<char>? Revealed;

    /// <summary>Starts revealing <paramref name="text"/> from the beginning; null counts as empty.</summary>
    public void Start(string text) => Start(text, 0);

    /// <summary>Starts revealing <paramref name="text"/> with its first <paramref name="revealed"/> characters already shown (text laid out again mid-reveal).</summary>
    public void Start(string text, int revealed)
    {
        Text = text ?? "";
        _visible = Math.Clamp(revealed, 0, Text.Length);
    }

    /// <summary>Reveals more characters for <paramref name="dt"/> seconds and raises <see cref="Revealed"/> for each new visible one.</summary>
    public void Update(float dt)
    {
        int before = VisibleCount;
        _visible = MathF.Min(Text.Length, _visible + CharactersPerSecond * dt);
        for (int i = before; i < VisibleCount; i++)
            if (!char.IsWhiteSpace(Text[i])) Revealed?.Invoke(Text[i]);
    }

    /// <summary>Reveals the rest of the text at once, without raising <see cref="Revealed"/>.</summary>
    public void Finish() => _visible = Text.Length;
}

/// <summary>Visual theme for the shared widgets so each game can keep its own identity.</summary>
/// <param name="Panel">Colours of panels and boxes.</param>
/// <param name="Text">Normal text.</param>
/// <param name="TextShadow">The 1px shadow under text.</param>
/// <param name="Accent">Highlights: the selected option, name tags, headings.</param>
/// <param name="Muted">Secondary or disabled text.</param>
public sealed record UiTheme(PanelStyle Panel, Color Text, Color TextShadow, Color Accent, Color Muted)
{
    /// <summary>The built-in dark blue theme with a warm yellow accent.</summary>
    public static readonly UiTheme Default = new(
        new PanelStyle(Color.Hex("1c1b2e"), Color.Hex("f4f1de"), Color.Hex("3a3a5c"), Color.Hex("00000080")),
        Color.Hex("f4f1de"), Color.Hex("0a0a14"), Color.Hex("f2cc60"), Color.Hex("8a8aa8"));
}

/// <summary>
/// A queue of dialogue pages shown in a panel with a typewriter and a speaker name tag. A page longer than
/// <see cref="Lines"/> wrapped lines continues on the next confirm, so no text is ever cut. With <see cref="Prompts"/>
/// set, <c>{action}</c> in a page names the control that presses it on the player's device, and follows a device change
/// while the page is showing. Games call <see cref="Update"/> and <see cref="Draw"/> from their own scene.
/// </summary>
public sealed class DialogueBox
{
    private readonly Queue<(string? Speaker, string Text)> _pages = new();
    private readonly Typewriter _typewriter = new();
    private float _time;
    private string _page = "";
    private IReadOnlyList<string>? _wrapped;
    private int _first, _promptVersion;
    private float _wrapWidth;
    private IFont? _wrapFont;
    /// <summary>Resolves <c>{action}</c> in pages to the player's controls (null: pages are shown as written). Games pass <see cref="Core.Game.Prompts"/>.</summary>
    public ActionPrompts? Prompts { get; set; }
    /// <summary>Colours of the box (<see cref="UiTheme.Default"/> by default).</summary>
    public UiTheme Theme { get; set; } = UiTheme.Default;
    /// <summary>The speaker of the current page, drawn as a name tag; null or empty draws none.</summary>
    public string? Speaker { get; private set; }
    /// <summary>Whether a page is showing.</summary>
    public bool Active { get; private set; }
    /// <summary>Maximum lines drawn at once (3 by default); a page that wraps to more continues on the next confirm.</summary>
    public int Lines { get; set; } = 3;
    /// <summary>The text of the current page as shown (prompts resolved), or empty when closed.</summary>
    public string Text => Active ? Display() : "";
    /// <summary>The wrapped lines in the box now, in full even while they are being revealed (known once the page has been drawn).</summary>
    public IReadOnlyList<string> VisibleLines => Active ? _typewriter.Text.Split('\n') : [];
    /// <summary>Whether the current page has more wrapped lines after the ones showing (known once it has been drawn).</summary>
    public bool HasMore => _wrapped is { } w && _first + Math.Max(1, Lines) < w.Count;
    /// <summary>Raised for each revealed non-space character, for a typing sound.</summary>
    public event Action<char>? Blip { add => _typewriter.Revealed += value; remove => _typewriter.Revealed -= value; }
    /// <summary>Raised when the last page is dismissed.</summary>
    public event Action? Closed;

    /// <summary>Queues pages spoken by <paramref name="speaker"/>; the first one shows at once if the box was closed.</summary>
    public void Show(string? speaker, params string[] pages)
    {
        foreach (var p in pages) _pages.Enqueue((speaker, p));
        if (!Active) Next();
    }

    private void Next()
    {
        if (_pages.Count == 0)
        {
            Active = false;
            Speaker = null;
            Closed?.Invoke();
            return;
        }
        var (speaker, text) = _pages.Dequeue();
        Speaker = speaker;
        _page = text ?? "";
        _wrapped = null;
        _first = 0;
        _promptVersion = Prompts?.Version ?? 0;
        _typewriter.Start(Display());
        Active = true;
    }

    private string Display() => Prompts is { } p ? p.Format(_page) : _page;

    /// <summary>Wraps the page for <paramref name="font"/> and <paramref name="width"/> and shows the lines from <see cref="_first"/>, keeping what was revealed.</summary>
    private void Layout(IFont font, float width)
    {
        int version = Prompts?.Version ?? 0;
        if (_wrapped is not null && font == _wrapFont && width == _wrapWidth && version == _promptVersion) return;
        _wrapped = font.Wrap(Display(), width);
        (_wrapFont, _wrapWidth, _promptVersion) = (font, width, version);
        _first = Math.Min(_first, Math.Max(0, _wrapped.Count - 1));
        ShowChunk(_typewriter.VisibleCount);
    }

    private void ShowChunk(int revealed)
    {
        if (_wrapped is null) return;
        _typewriter.Start(string.Join('\n', _wrapped.Skip(_first).Take(Math.Max(1, Lines))), revealed);
    }

    /// <summary>Returns true while the box consumed input this update.</summary>
    public bool Update(float dt, InputMap input, string confirm = "confirm")
    {
        if (!Active) return false;
        _time += dt;
        _typewriter.Update(dt);
        if (input.Pressed(confirm))
        {
            if (!_typewriter.Complete) _typewriter.Finish();
            else if (HasMore)
            {
                _first += Math.Max(1, Lines);
                ShowChunk(0);
            }
            else Next();
        }
        return true;
    }

    /// <summary>
    /// Draws the current page in <paramref name="area"/> (virtual pixels), wrapped to its width; draws nothing when closed.
    /// Margins grow with the font: the built-in 10-pixel-high pixel font gets the pixel-art layout, a
    /// <see cref="ScalableFont"/> size with 40-pixel lines gets margins four times as wide.
    /// </summary>
    public void Draw(IRenderer r, IFont font, RectF area)
    {
        ArgumentNullException.ThrowIfNull(font);
        if (!Active) return;
        float u = UiUnit(font);
        r.Panel(area, Theme.Panel);
        Layout(font, area.Width - 16 * u);
        int shown = _typewriter.VisibleCount;
        float y = area.Y + 7 * u;
        var shadow = new Vector2(u);
        foreach (var line in _typewriter.Text.Split('\n'))
        {
            var visible = line[..Math.Min(line.Length, Math.Max(0, shown))];
            shown -= line.Length + 1;
            font.DrawShadowed(r, visible, new Vector2(area.X + 8 * u, y), Theme.Text, Theme.TextShadow, shadow);
            y += font.LineHeight + u;
        }
        if (Speaker is { Length: > 0 } name)
        {
            float w = font.MeasureWidth(name) + 10 * u;
            var tag = new RectF(area.X + 6 * u, area.Y - 9 * u, w, 12 * u);
            r.Panel(tag, Theme.Panel with { Fill = Theme.Accent, Highlight = default });
            font.Draw(r, name, new Vector2(tag.X + 5 * u, tag.Y + 2 * u), Theme.Panel.Fill);
        }
        if (_typewriter.Complete && (int)(_time * 3) % 2 == 0)
            font.Draw(r, "▼", new Vector2(area.Right - 12 * u, area.Bottom - 10 * u), Theme.Accent);
    }

    /// <summary>The widgets' layout unit for <paramref name="font"/>: 1 for the built-in pixel font (10-pixel lines), proportionally more for larger fonts, never less than 1.</summary>
    internal static float UiUnit(IFont font) => MathF.Max(1, MathF.Round(font.LineHeight / 10f));
}

/// <summary>A vertical list of options navigated with up/down and chosen with confirm.</summary>
public sealed class SelectionList
{
    private readonly List<string> _options;
    private readonly HashSet<int> _disabled = [];
    /// <summary>The highlighted option; a value outside the options is clamped to the first or the last.</summary>
    public int Index { get => _index; set => _index = Math.Clamp(value, 0, _options.Count - 1); }
    private int _index;
    /// <summary>Whether moving past either end jumps to the other (true by default).</summary>
    public bool Wrap { get; set; } = true;
    /// <summary>The option labels, top to bottom.</summary>
    public IReadOnlyList<string> Options => _options;
    /// <summary>Raised when the highlighted option changes (for a cursor sound).</summary>
    public event Action? Moved;

    /// <summary>Creates a list with the first option highlighted.</summary>
    /// <exception cref="ArgumentException"><paramref name="options"/> is empty.</exception>
    public SelectionList(params string[] options)
    {
        if (options.Length == 0) throw new ArgumentException("At least one option is required.", nameof(options));
        _options = [.. options];
    }

    /// <summary>Replaces the labels, keeping <see cref="Index"/> within range. Disabled indices stay disabled.</summary>
    /// <exception cref="ArgumentException"><paramref name="options"/> is empty.</exception>
    public void SetOptions(IEnumerable<string> options)
    {
        var list = options?.ToList() ?? throw new ArgumentNullException(nameof(options));
        if (list.Count == 0) throw new ArgumentException("At least one option is required.", nameof(options));
        _options.Clear();
        _options.AddRange(list);
        _index = Math.Clamp(_index, 0, _options.Count - 1);
    }

    /// <summary>Enables or disables an option. A disabled option can be highlighted but not chosen, and is drawn in <see cref="UiTheme.Muted"/>.</summary>
    public void SetEnabled(int index, bool enabled) { if (enabled) _disabled.Remove(index); else _disabled.Add(index); }
    /// <summary>Whether the option at <paramref name="index"/> can be chosen.</summary>
    public bool IsEnabled(int index) => !_disabled.Contains(index);

    /// <summary>Returns the chosen index when confirmed, otherwise null.</summary>
    public int? Update(InputMap input, string up = "up", string down = "down", string confirm = "confirm")
    {
        int dir = (input.Pressed(down) ? 1 : 0) - (input.Pressed(up) ? 1 : 0);
        if (dir != 0 && _options.Count > 1)
        {
            int next = Index + dir;
            next = Wrap ? (next + _options.Count) % _options.Count : Math.Clamp(next, 0, _options.Count - 1);
            if (next != Index) { Index = next; Moved?.Invoke(); }
        }
        if (input.Pressed(confirm) && IsEnabled(Index)) return Index;
        return null;
    }

    /// <summary>Draws the options with a cursor beside the highlighted one.</summary>
    /// <param name="r">The renderer.</param>
    /// <param name="font">The font.</param>
    /// <param name="position">Top-left of the list, in virtual pixels.</param>
    /// <param name="theme">Colours.</param>
    /// <param name="time">Seconds, for the cursor's bobbing (for example <see cref="Scenes.Scene.Age"/>).</param>
    /// <param name="lineHeight">Distance between options in pixels; null for the font's line height plus a fifth (12 for the built-in font).</param>
    public void Draw(IRenderer r, IFont font, Vector2 position, UiTheme theme, float time, float? lineHeight = null)
    {
        ArgumentNullException.ThrowIfNull(font);
        float u = DialogueBox.UiUnit(font), step = lineHeight ?? font.LineHeight + 2 * u;
        var shadow = new Vector2(u);
        for (int i = 0; i < _options.Count; i++)
        {
            bool selected = i == Index;
            var color = !IsEnabled(i) ? theme.Muted : selected ? theme.Accent : theme.Text;
            float bob = selected ? MathF.Round(MathX.Wave(time, 2f, 1f) * u) : 0;
            var pos = position + new Vector2((selected ? 10 : 8) * u, i * step);
            if (selected) font.Draw(r, "▶", new Vector2(position.X + bob, pos.Y), theme.Accent);
            font.DrawShadowed(r, _options[i], pos, color, theme.TextShadow, shadow);
        }
    }
}

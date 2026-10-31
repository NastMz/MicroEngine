using System.Numerics;
using MicroEngine.Graphics;
using MicroEngine.Input;

namespace MicroEngine.UI;

/// <summary>Reveals text character by character. Confirm skips to the end; a second confirm advances.</summary>
public sealed class Typewriter
{
    private float _visible;
    public string Text { get; private set; } = "";
    public float CharactersPerSecond { get; set; } = 45f;
    public int VisibleCount => Math.Min(Text.Length, (int)_visible);
    public bool Complete => VisibleCount >= Text.Length;
    public string Visible => Text[..VisibleCount];
    /// <summary>Raised for each newly revealed non-space character (for blip sounds).</summary>
    public event Action<char>? Revealed;

    public void Start(string text)
    {
        Text = text ?? "";
        _visible = 0;
    }

    public void Update(float dt)
    {
        int before = VisibleCount;
        _visible = MathF.Min(Text.Length, _visible + CharactersPerSecond * dt);
        for (int i = before; i < VisibleCount; i++)
            if (!char.IsWhiteSpace(Text[i])) Revealed?.Invoke(Text[i]);
    }

    public void Finish() => _visible = Text.Length;
}

/// <summary>Visual theme for the shared widgets so each game can keep its own identity.</summary>
public sealed record UiTheme(PanelStyle Panel, Color Text, Color TextShadow, Color Accent, Color Muted)
{
    public static readonly UiTheme Default = new(
        new PanelStyle(Color.Hex("1c1b2e"), Color.Hex("f4f1de"), Color.Hex("3a3a5c"), Color.Hex("00000080")),
        Color.Hex("f4f1de"), Color.Hex("0a0a14"), Color.Hex("f2cc60"), Color.Hex("8a8aa8"));
}

/// <summary>
/// A queue of dialogue pages shown in a panel with a typewriter and a speaker name tag.
/// Games call <see cref="Update"/> and <see cref="Draw"/> from their own scene.
/// </summary>
public sealed class DialogueBox
{
    private readonly Queue<(string? Speaker, string Text)> _pages = new();
    private readonly Typewriter _typewriter = new();
    private float _time;
    public UiTheme Theme { get; set; } = UiTheme.Default;
    public string? Speaker { get; private set; }
    public bool Active { get; private set; }
    public int Lines { get; set; } = 3;
    public event Action<char>? Blip { add => _typewriter.Revealed += value; remove => _typewriter.Revealed -= value; }
    /// <summary>Raised when the last page is dismissed.</summary>
    public event Action? Closed;

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
        _typewriter.Start(text);
        Active = true;
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
            else Next();
        }
        return true;
    }

    public void Draw(IRenderer r, BitmapFont font, RectF area)
    {
        if (!Active) return;
        r.Panel(area, Theme.Panel);
        var lines = font.Wrap(_typewriter.Text, (int)area.Width - 16);
        int shown = _typewriter.VisibleCount;
        float y = area.Y + 7;
        foreach (var line in lines.Take(Lines))
        {
            var visible = line[..Math.Min(line.Length, Math.Max(0, shown))];
            shown -= line.Length + 1;
            font.DrawShadowed(r, visible, new Vector2(area.X + 8, y), Theme.Text, Theme.TextShadow);
            y += font.LineHeight + 1;
        }
        if (Speaker is { Length: > 0 } name)
        {
            int w = font.MeasureWidth(name) + 10;
            var tag = new RectF(area.X + 6, area.Y - 9, w, 12);
            r.Panel(tag, Theme.Panel with { Fill = Theme.Accent, Highlight = default });
            font.Draw(r, name, new Vector2(tag.X + 5, tag.Y + 2), Theme.Panel.Fill);
        }
        if (_typewriter.Complete && (int)(_time * 3) % 2 == 0)
            font.Draw(r, "▼", new Vector2(area.Right - 12, area.Bottom - 10), Theme.Accent);
    }
}

/// <summary>A vertical list of options navigated with up/down and chosen with confirm.</summary>
public sealed class SelectionList
{
    private readonly List<string> _options;
    private readonly HashSet<int> _disabled = [];
    public int Index { get; set; }
    public bool Wrap { get; set; } = true;
    public IReadOnlyList<string> Options => _options;
    /// <summary>Raised when the highlighted option changes (for a cursor sound).</summary>
    public event Action? Moved;

    public SelectionList(params string[] options)
    {
        if (options.Length == 0) throw new ArgumentException("At least one option is required.", nameof(options));
        _options = [.. options];
    }

    public void SetOptions(IEnumerable<string> options)
    {
        _options.Clear();
        _options.AddRange(options);
        Index = Math.Clamp(Index, 0, Math.Max(0, _options.Count - 1));
    }

    public void SetEnabled(int index, bool enabled) { if (enabled) _disabled.Remove(index); else _disabled.Add(index); }
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

    public void Draw(IRenderer r, BitmapFont font, Vector2 position, UiTheme theme, float time, int lineHeight = 12)
    {
        for (int i = 0; i < _options.Count; i++)
        {
            bool selected = i == Index;
            var color = !IsEnabled(i) ? theme.Muted : selected ? theme.Accent : theme.Text;
            float bob = selected ? MathF.Round(MathX.Wave(time, 2f, 1f)) : 0;
            var pos = position + new Vector2(selected ? 10 : 8, i * lineHeight);
            if (selected) font.Draw(r, "▶", new Vector2(position.X + bob, pos.Y), theme.Accent);
            font.DrawShadowed(r, _options[i], pos, color, theme.TextShadow);
        }
    }
}

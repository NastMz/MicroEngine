using System.Numerics;
using MicroEngine.Graphics;
using MicroEngine.Input;

namespace MicroEngine.UI;

/// <summary>A vertical list of options navigated with up/down and chosen with confirm.</summary>
[Obsolete("Build menus from widgets: Buttons (or widgets of your own) in a Stack, with UiRoot.FocusChanged for the cursor sound, as the Title menu example does. It is removed in a later SDK release; see the migration notes.")]
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

    /// <summary>
    /// Returns the chosen index when confirmed, otherwise null. Actions left null (the default) are the map's up, down and
    /// confirm actions (<see cref="InputMap.Names"/>). The press that chooses is consumed (<see cref="InputMap.Consume"/>),
    /// so nothing read after the list acts on it too.
    /// </summary>
    public int? Update(InputMap input, string? up = null, string? down = null, string? confirm = null)
    {
        ArgumentNullException.ThrowIfNull(input);
        up ??= input.Names.Up;
        down ??= input.Names.Down;
        confirm ??= input.Names.Confirm;
        int dir = (input.Pressed(down) ? 1 : 0) - (input.Pressed(up) ? 1 : 0);
        if (dir != 0 && _options.Count > 1)
        {
            int next = Index + dir;
            next = Wrap ? (next + _options.Count) % _options.Count : Math.Clamp(next, 0, _options.Count - 1);
            if (next != Index) { Index = next; Moved?.Invoke(); }
        }
        if (input.Pressed(confirm) && IsEnabled(Index))
        {
            input.Consume(confirm);
            return Index;
        }
        return null;
    }

    /// <summary>Draws the options with a cursor beside the highlighted one.</summary>
    /// <param name="r">The renderer.</param>
    /// <param name="font">The font.</param>
    /// <param name="position">Top-left of the list, in virtual pixels.</param>
    /// <param name="theme">Colours.</param>
    /// <param name="time">Seconds, for the cursor's bobbing (for example the scene's <c>Age</c>).</param>
    /// <param name="lineHeight">Distance between options in pixels; null for the font's line height plus a fifth (12 for the built-in font).</param>
    public void Draw(IRenderer r, IFont font, Vector2 position, UiTheme theme, float time, float? lineHeight = null)
    {
        ArgumentNullException.ThrowIfNull(font);
        float u = UiTheme.Unit(font), step = lineHeight ?? font.LineHeight + 2 * u;
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

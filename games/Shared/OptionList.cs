using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.UI;

namespace DemoKit;

/// <summary>
/// A vertical list of options moved with up and down and chosen with confirm: the demos' own copy of the cursor menu the
/// engine used to ship, kept because their menus are drawn their own way around it. A disabled option can be highlighted
/// but not chosen.
/// </summary>
public sealed class OptionList
{
    private readonly List<string> _options;
    private readonly HashSet<int> _disabled = [];
    private int _index;

    /// <summary>Creates a list with the first option highlighted.</summary>
    /// <exception cref="ArgumentException"><paramref name="options"/> is empty.</exception>
    public OptionList(params string[] options)
    {
        if (options.Length == 0) throw new ArgumentException("At least one option is required.", nameof(options));
        _options = [.. options];
    }

    /// <summary>The highlighted option; a value outside the options is clamped to the first or the last.</summary>
    public int Index { get => _index; set => _index = Math.Clamp(value, 0, _options.Count - 1); }
    /// <summary>Whether moving past either end jumps to the other (true by default).</summary>
    public bool Wrap { get; set; } = true;
    /// <summary>The option labels, top to bottom.</summary>
    public IReadOnlyList<string> Options => _options;
    /// <summary>Raised when the highlighted option changes (for a cursor sound).</summary>
    public event Action? Moved;

    /// <summary>Replaces the labels, keeping <see cref="Index"/> within range. Disabled indices stay disabled.</summary>
    public void SetOptions(IEnumerable<string> options)
    {
        var list = options.ToList();
        if (list.Count == 0) throw new ArgumentException("At least one option is required.", nameof(options));
        _options.Clear();
        _options.AddRange(list);
        _index = Math.Clamp(_index, 0, _options.Count - 1);
    }

    /// <summary>Enables or disables an option.</summary>
    public void SetEnabled(int index, bool enabled) { if (enabled) _disabled.Remove(index); else _disabled.Add(index); }
    /// <summary>Whether the option at <paramref name="index"/> can be chosen.</summary>
    public bool IsEnabled(int index) => !_disabled.Contains(index);

    /// <summary>
    /// Returns the chosen index when confirmed, otherwise null. Actions left null are the map's up, down and confirm
    /// actions. The press that chooses is consumed, so nothing read after the list acts on it too.
    /// </summary>
    public int? Update(InputMap input, string? up = null, string? down = null, string? confirm = null)
    {
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

    /// <summary>Draws the options with a bobbing cursor beside the highlighted one.</summary>
    /// <param name="r">The renderer.</param>
    /// <param name="font">The font.</param>
    /// <param name="position">Top-left of the list, in virtual pixels.</param>
    /// <param name="theme">Colours.</param>
    /// <param name="time">Seconds, for the cursor's bobbing.</param>
    /// <param name="lineHeight">Distance between options in pixels; null for the font's line height plus two units.</param>
    public void Draw(IRenderer r, IFont font, Vector2 position, UiTheme theme, float time, float? lineHeight = null)
    {
        float u = MathF.Max(1, MathF.Round(font.LineHeight / 10f)), step = lineHeight ?? font.LineHeight + 2 * u;
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

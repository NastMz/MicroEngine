using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Core;
using MicroEngine.UI;

namespace CourtyardKey.UI;

/// <summary>
/// The game's own copy of the dialogue box the engine used to ship: a queue of dialogue pages shown in a panel with a
/// typewriter and a speaker name tag. A page longer than
/// <see cref="Lines"/> wrapped lines continues on the next confirm, so no text is ever cut. With <see cref="Prompts"/>
/// set, <c>{action}</c> in a page names the control that presses it on the player's device, and follows a device change
/// while the page is showing. Games call <see cref="Update"/> and <see cref="Draw"/> from their own scene.
/// </summary>
public sealed class DialogueBox
{
    private readonly Queue<(string? Speaker, string Text)> _pages = new();
    private readonly DemoKit.Typewriter _typewriter = new();
    private float _time;
    private string _page = "";
    private IReadOnlyList<string>? _wrapped;
    private int _first, _promptVersion;
    private float _wrapWidth;
    private IFont? _wrapFont;
    /// <summary>Resolves <c>{action}</c> in pages to the player's controls (null: pages are shown as written). Games pass <c>Game.Prompts</c>.</summary>
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

    /// <summary>
    /// Returns true while the box is open and takes input this update. <paramref name="confirm"/> advances it; null (the
    /// default) is the map's confirm action (<see cref="InputMap.Names"/>). A confirm the box acted on is consumed
    /// (<see cref="InputMap.Consume"/>), so nothing read after it (the scene under an overlay) acts on the same press.
    /// </summary>
    public bool Update(float dt, InputMap input, string? confirm = null)
    {
        if (!Active) return false;
        ArgumentNullException.ThrowIfNull(input);
        confirm ??= input.Names.Confirm;
        _time += dt;
        _typewriter.Update(dt);
        if (input.Pressed(confirm))
        {
            input.Consume(confirm);
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
        float u = MathF.Max(1, MathF.Round(font.LineHeight / 10f));
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
}

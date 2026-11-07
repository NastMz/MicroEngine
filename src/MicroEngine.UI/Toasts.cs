using System.Numerics;
using MicroEngine.Graphics;

namespace MicroEngine.UI;

/// <summary>One banner of <see cref="Toasts"/>: a heading, a title, an optional description and an optional icon.</summary>
/// <param name="Heading">The first line, in the accent colour ("ACHIEVEMENT UNLOCKED", "SAVED").</param>
/// <param name="Title">The second line.</param>
/// <param name="Description">Up to two wrapped lines in the muted colour; empty draws none.</param>
[Obsolete("Draw banners of your own, from widgets or from Game.DrawAboveScenes. It is removed in a later SDK release; see the migration notes.")]
public sealed record Toast(string Heading, string Title, string Description = "")
{
    /// <summary>A sprite drawn left of the text, or null.</summary>
    public Sprite? Icon { get; init; }
}

/// <summary>
/// Queued banners that slide in at the top of the screen (or the bottom), one at a time: unlocked achievements, saves,
/// notifications. Purely visual: they never read input, so play continues underneath. Call <see cref="Update"/> every
/// update and <see cref="Draw"/> after the scenes.
/// </summary>
[Obsolete("Draw banners of your own, from widgets or from Game.DrawAboveScenes. It is removed in a later SDK release; see the migration notes.")]
public sealed class Toasts
{
    /// <summary>Seconds each banner stays (3.4), and seconds it takes to slide in and out (0.25).</summary>
    public const float ShowTime = 3.4f, SlideTime = 0.25f;
    private readonly Queue<Toast> _queue = new();
    private float _time;

    /// <summary>Colours of the banners.</summary>
    public UiTheme Theme { get; set; } = UiTheme.Default;
    /// <summary>Distance from the top edge when fully shown.</summary>
    public float Top { get; set; } = 4;
    /// <summary>Distance from the bottom edge when fully shown, for games whose HUD sits at the top; null (the default) shows banners at the top.</summary>
    public float? Bottom { get; set; }
    /// <summary>The banner on screen, or null.</summary>
    public Toast? Current { get; private set; }
    /// <summary>Banners waiting behind the current one.</summary>
    public int Pending => _queue.Count;
    /// <summary>Raised when a banner leaves the screen and the next one (or none) takes its place.</summary>
    public event Action? Advanced;

    /// <summary>Queues <paramref name="toast"/>; it shows at once when no other is on screen.</summary>
    public void Show(Toast toast)
    {
        ArgumentNullException.ThrowIfNull(toast);
        _queue.Enqueue(toast);
        if (Current is null) Next();
    }

    /// <summary>Advances the current banner; call it every update.</summary>
    public void Update(float dt)
    {
        if (Current is null) return;
        _time += dt;
        if (_time >= ShowTime)
        {
            Next();
            Advanced?.Invoke();
        }
    }

    private void Next()
    {
        Current = _queue.Count > 0 ? _queue.Dequeue() : null;
        _time = 0;
    }

    /// <summary>Draws the current banner centred at the top of <paramref name="r"/> (or the bottom, see <see cref="Bottom"/>).</summary>
    public void Draw(IRenderer r, IFont font)
    {
        ArgumentNullException.ThrowIfNull(r);
        ArgumentNullException.ThrowIfNull(font);
        if (Current is not { } t) return;
        float icon = t.Icon is { } s ? Math.Max(s.Width, s.Height) + 6 : 0;
        float maxText = Math.Min(r.Width - 16, 240) - 16 - icon;
        var description = t.Description.Length == 0 ? [] : font.Wrap(t.Description, maxText).Take(2).ToList();
        float textWidth = new[] { font.MeasureWidth(t.Heading), font.MeasureWidth(t.Title) }.Concat(description.Select(font.MeasureWidth)).Max();
        float lh = font.LineHeight + 1;
        var size = new Vector2(textWidth + 16 + icon, 8 + lh * (2 + description.Count));
        float shown = Ease.OutCubic(Math.Clamp(MathF.Min(_time, ShowTime - _time) / SlideTime, 0, 1));
        float y0 = Bottom is { } bottom ? MathX.Lerp(r.Height + 2, r.Height - bottom - size.Y, shown) : MathX.Lerp(-size.Y - 2, Top, shown);
        var box = new RectF(MathF.Round((r.Width - size.X) / 2), MathF.Round(y0), size.X, size.Y);
        r.Panel(box, Theme.Panel);
        if (t.Icon is { } sprite) r.Draw(sprite, new Vector2(box.X + 8, MathF.Round(box.Y + (box.Height - sprite.Height) / 2)) + sprite.Origin);
        float x = box.X + 8 + icon, y = box.Y + 5;
        font.DrawShadowed(r, t.Heading, new Vector2(x, y), Theme.Accent, Theme.TextShadow);
        font.DrawShadowed(r, t.Title, new Vector2(x, y + lh), Theme.Text, Theme.TextShadow);
        for (int i = 0; i < description.Count; i++)
            font.Draw(r, description[i], new Vector2(x, y + lh * (2 + i)), Theme.Muted);
    }
}

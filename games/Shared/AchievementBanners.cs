using System.Numerics;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.UI;

namespace DemoKit;

/// <summary>
/// "Achievement unlocked" banners that slide in at the top of the screen (or the bottom), one at a time, queued. Purely
/// visual: they read no input, so play continues underneath. A demo attaches them in <c>Configure</c> and draws them
/// from <c>DrawAboveScenes</c>, so they stay up while scenes change.
/// </summary>
public sealed class AchievementBanners
{
    /// <summary>Seconds each banner stays (3.4), and seconds it takes to slide in and out (0.25).</summary>
    public const float ShowTime = 3.4f, SlideTime = 0.25f;
    private readonly Queue<Achievement> _queue = new();
    private float _time;

    /// <summary>Colours and panel of the banners.</summary>
    public UiTheme Theme { get; set; } = UiTheme.Default;
    /// <summary>The first line of every banner ("ACHIEVEMENT UNLOCKED" by default).</summary>
    public string Heading { get; set; } = "ACHIEVEMENT UNLOCKED";
    /// <summary>Distance from the top edge when fully shown.</summary>
    public float Top { get; set; } = 4;
    /// <summary>Distance from the bottom edge when fully shown; null (the default) shows banners at the top.</summary>
    public float? Bottom { get; set; }
    /// <summary>The banner on screen, or null.</summary>
    public Achievement? Current { get; private set; }
    /// <summary>Banners waiting behind the current one.</summary>
    public int Pending => _queue.Count;

    /// <summary>Shows a banner for every unlock of <paramref name="tracker"/>, advanced by a frame system of <paramref name="builder"/>.</summary>
    public AchievementTracker Attach(EngineBuilder builder, AchievementTracker tracker)
    {
        tracker.Unlocked += Show;
        builder.Schedule.Add(FramePhase.PostFixed, "demo.banners", step => Update((float)step.FixedDelta));
        return tracker;
    }

    /// <summary>Queues a banner for <paramref name="achievement"/>; it shows at once when no other is on screen.</summary>
    public void Show(Achievement achievement)
    {
        _queue.Enqueue(achievement);
        if (Current is null) Next();
    }

    /// <summary>Advances the current banner.</summary>
    public void Update(float dt)
    {
        if (Current is null) return;
        _time += dt;
        if (_time >= ShowTime) Next();
    }

    private void Next()
    {
        Current = _queue.Count > 0 ? _queue.Dequeue() : null;
        _time = 0;
    }

    /// <summary>Draws the current banner centred at the top of <paramref name="r"/> (or the bottom, see <see cref="Bottom"/>).</summary>
    public void Draw(IRenderer r, IFont font)
    {
        if (Current is not { } a) return;
        float icon = a.Icon is { } s ? Math.Max(s.Width, s.Height) + 6 : 0;
        float maxText = Math.Min(r.Width - 16, 240) - 16 - icon;
        var description = a.Description.Length == 0 ? [] : font.Wrap(a.Description, maxText).Take(2).ToList();
        float textWidth = new[] { font.MeasureWidth(Heading), font.MeasureWidth(a.Name) }.Concat(description.Select(font.MeasureWidth)).Max();
        float lh = font.LineHeight + 1;
        var size = new Vector2(textWidth + 16 + icon, 8 + lh * (2 + description.Count));
        float shown = Ease.OutCubic(Math.Clamp(MathF.Min(_time, ShowTime - _time) / SlideTime, 0, 1));
        float y0 = Bottom is { } bottom ? MathX.Lerp(r.Height + 2, r.Height - bottom - size.Y, shown) : MathX.Lerp(-size.Y - 2, Top, shown);
        var box = new RectF(MathF.Round((r.Width - size.X) / 2), MathF.Round(y0), size.X, size.Y);
        r.Panel(box, Theme.Panel);
        if (a.Icon is { } sprite) r.Draw(sprite, new Vector2(box.X + 8, MathF.Round(box.Y + (box.Height - sprite.Height) / 2)) + sprite.Origin);
        float x = box.X + 8 + icon, y = box.Y + 5;
        font.DrawShadowed(r, Heading, new Vector2(x, y), Theme.Accent, Theme.TextShadow);
        font.DrawShadowed(r, a.Name, new Vector2(x, y + lh), Theme.Text, Theme.TextShadow);
        for (int i = 0; i < description.Count; i++)
            font.Draw(r, description[i], new Vector2(x, y + lh * (2 + i)), Theme.Muted);
    }
}

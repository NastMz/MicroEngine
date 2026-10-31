using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Scenes;

namespace MicroEngine.UI;

/// <summary>
/// Queued "achievement unlocked" banners that slide in at the top of the screen, one at a time.
/// Purely visual: they never read input, so play continues underneath. <see cref="Game"/> owns one
/// when achievements are enabled and draws it above every scene.
/// </summary>
public sealed class AchievementToasts
{
    public const float ShowTime = 3.4f, SlideTime = 0.25f;
    private readonly Queue<Achievement> _queue = new();
    private float _time;

    public UiTheme Theme { get; set; } = UiTheme.Default;
    public string Heading { get; set; } = "ACHIEVEMENT UNLOCKED";
    /// <summary>Distance from the top edge when fully shown.</summary>
    public float Top { get; set; } = 4;
    /// <summary>The banner on screen, or null.</summary>
    public Achievement? Current { get; private set; }
    public int Pending => _queue.Count;

    public void Show(Achievement achievement)
    {
        ArgumentNullException.ThrowIfNull(achievement);
        _queue.Enqueue(achievement);
        if (Current is null) Next();
    }

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

    public void Draw(IRenderer r, BitmapFont font)
    {
        if (Current is not { } a) return;
        int icon = a.Icon is { } s ? Math.Max(s.Width, s.Height) + 6 : 0;
        int maxText = Math.Min(r.Width - 16, 240) - 16 - icon;
        var description = font.Wrap(a.Description, maxText).Take(2).ToList();
        int textWidth = new[] { font.MeasureWidth(Heading), font.MeasureWidth(a.Name) }.Concat(description.Select(font.MeasureWidth)).Max();
        int lh = font.LineHeight + 1;
        var size = new Vector2(textWidth + 16 + icon, 8 + lh * (2 + description.Count));
        float shown = Ease.OutCubic(Math.Clamp(MathF.Min(_time, ShowTime - _time) / SlideTime, 0, 1));
        var box = new RectF(MathF.Round((r.Width - size.X) / 2), MathF.Round(MathX.Lerp(-size.Y - 2, Top, shown)), size.X, size.Y);
        r.Panel(box, Theme.Panel);
        if (a.Icon is { } sprite) r.Draw(sprite, new Vector2(box.X + 8, MathF.Round(box.Y + (box.Height - sprite.Height) / 2)) + sprite.Origin);
        float x = box.X + 8 + icon, y = box.Y + 5;
        font.DrawShadowed(r, Heading, new Vector2(x, y), Theme.Accent, Theme.TextShadow);
        font.DrawShadowed(r, a.Name, new Vector2(x, y + lh), Theme.Text, Theme.TextShadow);
        for (int i = 0; i < description.Count; i++)
            font.Draw(r, description[i], new Vector2(x, y + lh * (2 + i)), Theme.Muted);
    }
}

/// <summary>
/// Overlay listing a game's achievements: unlocked ones highlighted, pending ones dimmed, progress bars
/// for progressive goals. Up/down moves the cursor (the list scrolls); any of <see cref="CloseActions"/> closes it.
/// </summary>
public sealed class AchievementsScene(AchievementTracker tracker, UiTheme theme) : Scene
{
    private int _scroll;

    public string Title { get; init; } = "ACHIEVEMENTS";
    public string[] CloseActions { get; init; } = ["cancel", "pause", "confirm"];
    /// <summary>Screen tint drawn over the scene below.</summary>
    public Color Dim { get; init; } = Color.Black.WithAlpha(0.55f);
    public string LockedLabel { get; init; } = "LOCKED";
    public string UnlockedLabel { get; init; } = "UNLOCKED";
    public int Selected { get; private set; }
    /// <summary>Raised when the cursor moves (for a sound).</summary>
    public event Action? Moved;
    /// <summary>Raised when the screen closes (for a sound).</summary>
    public event Action? Closed;

    public override bool IsOverlay => true;

    public override void Update(float dt)
    {
        if (CloseActions.Any(Input.Pressed))
        {
            Closed?.Invoke();
            Game.Scenes.Pop();
            return;
        }
        int dir = (Input.Pressed("down") ? 1 : 0) - (Input.Pressed("up") ? 1 : 0);
        int next = Math.Clamp(Selected + dir, 0, Math.Max(0, tracker.Count - 1));
        if (next != Selected) { Selected = next; Moved?.Invoke(); }
    }

    public override void Draw(IRenderer r)
    {
        r.FillRect(new RectF(0, 0, r.Width, r.Height), Dim);
        var panel = new RectF(8, 8, r.Width - 16, r.Height - 16);
        r.Panel(panel, theme.Panel);
        int lh = Font.LineHeight + 1;
        var items = tracker.All;
        string count = $"{tracker.UnlockedCount}/{tracker.Count}";
        Font.DrawShadowed(r, Title, new Vector2(panel.X + 8, panel.Y + 6), theme.Accent, theme.TextShadow);
        Font.DrawShadowed(r, count, new Vector2(panel.Right - 8 - Font.MeasureWidth(count), panel.Y + 6), theme.Text, theme.TextShadow);

        float rowHeight = lh * 2 + 7, top = panel.Y + 8 + lh + 4;
        int visible = Math.Max(1, (int)((panel.Bottom - 6 - top) / rowHeight));
        _scroll = Math.Clamp(_scroll, Math.Max(0, Selected - visible + 1), Selected);
        for (int i = _scroll; i < Math.Min(items.Count, _scroll + visible); i++)
            DrawRow(r, items[i], new RectF(panel.X + 4, top + (i - _scroll) * rowHeight, panel.Width - 8, rowHeight - 2), i == Selected, lh);
        if (_scroll > 0) Font.Draw(r, "^", new Vector2(panel.Right - 10, top - 4), theme.Accent);
        if (_scroll + visible < items.Count) Font.Draw(r, "v", new Vector2(panel.Right - 10, panel.Bottom - lh), theme.Accent);
    }

    private void DrawRow(IRenderer r, AchievementStatus s, RectF row, bool selected, int lh)
    {
        if (selected) r.DrawRect(row, theme.Accent);
        float x = row.X + 5;
        if (s.Definition.Icon is { } icon)
        {
            r.Draw(icon, new Vector2(x, MathF.Round(row.Y + (row.Height - icon.Height) / 2)) + icon.Origin, s.Unlocked ? null : theme.Muted);
            x += icon.Width + 5;
        }
        string state = s.Unlocked ? UnlockedLabel : s.IsProgressive ? $"{s.Progress}/{s.Goal}" : LockedLabel;
        float stateX = row.Right - 5 - Font.MeasureWidth(state);
        Font.DrawShadowed(r, state, new Vector2(stateX, row.Y + 3), s.Unlocked ? theme.Accent : theme.Muted, theme.TextShadow);
        Font.DrawShadowed(r, Fit(s.Definition.Name, stateX - 6 - x), new Vector2(x, row.Y + 3), s.Unlocked ? theme.Accent : theme.Text, theme.TextShadow);
        Font.Draw(r, Fit(s.Definition.Description, row.Right - 5 - x), new Vector2(x, row.Y + 3 + lh), theme.Muted);
        if (s.IsProgressive && !s.Unlocked)
        {
            var bar = new RectF(x, row.Bottom - 3, row.Right - 5 - x, 2);
            r.FillRect(bar, theme.TextShadow);
            r.FillRect(bar with { Width = MathF.Round(bar.Width * s.Fraction) }, theme.Accent);
        }
    }

    /// <summary>Cuts text with "..." so it fits the given width.</summary>
    private string Fit(string text, float width)
    {
        if (Font.MeasureWidth(text) <= width) return text;
        for (int n = text.Length - 1; n > 0; n--)
        {
            var cut = text[..n].TrimEnd() + "...";
            if (Font.MeasureWidth(cut) <= width) return cut;
        }
        return "";
    }
}

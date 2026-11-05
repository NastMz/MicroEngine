using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Scenes;

namespace MicroEngine.UI;

/// <summary>
/// Queued "achievement unlocked" banners that slide in at the top of the screen, one at a time: the UI's
/// <see cref="Toasts"/> showing achievements. Purely visual: they never read input, so play continues underneath.
/// <see cref="Game"/> owns one when achievements are enabled and draws it above every scene.
/// </summary>
public sealed class AchievementToasts
{
    /// <summary>Seconds each banner stays (3.4), and seconds it takes to slide in and out (0.25).</summary>
    public const float ShowTime = Toasts.ShowTime, SlideTime = Toasts.SlideTime;
    private readonly Queue<Achievement> _queue = new();

    /// <summary>Creates the banners; each one that leaves the screen makes the next achievement current.</summary>
    public AchievementToasts() => Banners.Advanced += () => Current = _queue.Count > 0 ? _queue.Dequeue() : null;

    /// <summary>The banners themselves, a <see cref="Toasts"/> of the UI.</summary>
    public Toasts Banners { get; } = new();
    /// <summary>Colours of the banners.</summary>
    public UiTheme Theme { get => Banners.Theme; set => Banners.Theme = value; }
    /// <summary>The first line of every banner queued from now on ("ACHIEVEMENT UNLOCKED" by default).</summary>
    public string Heading { get; set; } = "ACHIEVEMENT UNLOCKED";
    /// <summary>Distance from the top edge when fully shown.</summary>
    public float Top { get => Banners.Top; set => Banners.Top = value; }
    /// <summary>Distance from the bottom edge when fully shown, for games whose HUD sits at the top; null (the default) shows banners at the top.</summary>
    public float? Bottom { get => Banners.Bottom; set => Banners.Bottom = value; }
    /// <summary>The banner on screen, or null.</summary>
    public Achievement? Current { get; private set; }
    /// <summary>Banners waiting behind the current one.</summary>
    public int Pending => _queue.Count;

    /// <summary>Queues a banner for <paramref name="achievement"/>; it shows at once when no other is on screen.</summary>
    public void Show(Achievement achievement)
    {
        ArgumentNullException.ThrowIfNull(achievement);
        if (Current is null) Current = achievement;
        else _queue.Enqueue(achievement);
        Banners.Show(new Toast(Heading, achievement.Name, achievement.Description) { Icon = achievement.Icon });
    }

    /// <summary>Advances the current banner; the game calls it every update.</summary>
    public void Update(float dt) => Banners.Update(dt);

    /// <summary>Draws the current banner centred at the top of the screen (or the bottom, see <see cref="Bottom"/>); the game calls it after the scenes.</summary>
    public void Draw(IRenderer r, BitmapFont font) => Banners.Draw(r, font);
}

/// <summary>
/// Overlay listing a game's achievements: unlocked ones highlighted, pending ones dimmed, progress bars
/// for progressive goals. Up/down moves the cursor (the list scrolls); any of <see cref="CloseActions"/> closes it.
/// </summary>
public sealed class AchievementsScene(AchievementTracker tracker, UiTheme theme) : Scene
{
    private int _scroll;

    /// <summary>The heading of the list ("ACHIEVEMENTS" by default).</summary>
    public string Title { get; init; } = "ACHIEVEMENTS";
    /// <summary>
    /// Actions that close the screen; null (the default) is the map's cancel, pause and confirm actions. The cursor moves
    /// with the map's up and down actions (<see cref="Input.InputMap.Names"/>).
    /// </summary>
    public string[]? CloseActions { get; init; }
    /// <summary>Screen tint drawn over the scene below.</summary>
    public Color Dim { get; init; } = Color.Black.WithAlpha(0.55f);
    /// <summary>Text shown for locked achievements.</summary>
    public string LockedLabel { get; init; } = "LOCKED";
    /// <summary>Text shown for unlocked achievements.</summary>
    public string UnlockedLabel { get; init; } = "UNLOCKED";
    /// <summary>Index of the highlighted achievement in catalog order.</summary>
    public int Selected { get; private set; }
    /// <summary>Raised when the cursor moves (for a sound).</summary>
    public event Action? Moved;
    /// <summary>Raised when the screen closes (for a sound).</summary>
    public event Action? Closed;

    /// <summary>Always true: the scene below stays visible, dimmed by <see cref="Dim"/>.</summary>
    public override bool IsOverlay => true;

    /// <summary>Moves the cursor, or pops the scene when a close action is pressed.</summary>
    public override void Update(float dt)
    {
        var names = Input.Names;
        // The press that closes the screen is consumed, so the scene it returns to does not act on it as well.
        if ((CloseActions ?? [names.Cancel, names.Pause, names.Confirm]).FirstOrDefault(Input.Pressed) is { } close)
        {
            Input.Consume(close);
            Closed?.Invoke();
            Game.Scenes.Pop();
            return;
        }
        int dir = (Input.Pressed(names.Down) ? 1 : 0) - (Input.Pressed(names.Up) ? 1 : 0);
        int next = Math.Clamp(Selected + dir, 0, Math.Max(0, tracker.Count - 1));
        if (next != Selected) { Selected = next; Moved?.Invoke(); }
    }

    /// <summary>Draws the dimmed backdrop, the panel and the visible part of the list.</summary>
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

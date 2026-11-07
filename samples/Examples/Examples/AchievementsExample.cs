using System.Numerics;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace Examples;

/// <summary>
/// Achievements shown with widgets: a scrolling list of every achievement (unlocked ones lit, progress for the
/// progressive one) and a banner that slides in on each unlock. The tracker keeps the rules and raises
/// <see cref="AchievementTracker.Unlocked"/>; how unlocks look is the game's.
/// </summary>
public sealed class AchievementsExample : Scene
{
    private static readonly Achievement[] Catalog =
    [
        new("first", "FIRST GEM", "Collect a gem."),
        new("five", "GEM HOARDER", "Collect five gems.", Goal: 5),
        new("reset", "FRESH START", "Start over once."),
    ];

    private readonly Dictionary<string, (Label Name, Label State)> _rows = [];
    private readonly Queue<Achievement> _waiting = new();
    private bool _showing;
    private Label _bannerName = null!;

    /// <summary>The achievements; kept in memory, so they start over each visit.</summary>
    public AchievementTracker Tracker { get; } = new(Catalog);
    /// <summary>The unlock banner, for tests.</summary>
    public Panel Banner { get; private set; } = null!;

    public override void Enter()
    {
        Widgets.Content = new Layer().Add(Screen(), Anchor.Center).Add(CreateBanner(), Anchor.Top, new Vector2(0, 14));
        Tracker.Unlocked += a =>
        {
            _waiting.Enqueue(a);                                             // one banner at a time
            if (!_showing) ShowNext();
        };
    }

    #region list
    /// <summary>A row per achievement in a scrolling list, and two buttons that make progress.</summary>
    private Widget Screen()
    {
        var list = new Stack { Spacing = 2, Align = Align.Start };
        foreach (var a in Catalog)
        {
            var row = (Name: new Label(a.Name), State: new Label { Color = UiTheme.Default.Muted });
            _rows[a.Id] = row;
            list.Add(new Panel(new Stack { Spacing = 1, Align = Align.Start }
                .Add(new Stack { Orientation = Orientation.Horizontal, Spacing = 8 }.Add(row.Name).Add(row.State))
                .Add(new Label(a.Description) { Color = UiTheme.Default.Muted })) { Width = 180, Padding = 4 });
        }
        var buttons = new Stack { Orientation = Orientation.Horizontal, Spacing = 6 }
            .Add(new Button("COLLECT A GEM", () => { Tracker.Unlock("first"); Tracker.AddProgress("five"); }))
            .Add(new Button("START OVER", () => Tracker.Unlock("reset")));
        return new Stack { Spacing = 6 }.Add(new ScrollView(list) { Height = 90 }).Add(buttons);
    }

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
        foreach (var s in Tracker.All)
        {
            var (name, state) = _rows[s.Definition.Id];
            name.Color = s.Unlocked ? UiTheme.Default.Accent : null;
            state.Text = s.Unlocked ? "UNLOCKED" : s.IsProgressive ? $"{s.Progress}/{s.Goal}" : "LOCKED";
        }
    }
    #endregion

    #region banner
    /// <summary>The banner: hidden until an unlock slides it in.</summary>
    private Panel CreateBanner()
    {
        _bannerName = new Label();
        return Banner = new Panel(new Stack { Spacing = 1, Align = Align.Start }
            .Add(new Label("ACHIEVEMENT UNLOCKED") { Color = UiTheme.Default.Accent })
            .Add(_bannerName)) { Opacity = 0 };
    }

    private void ShowNext()
    {
        if (!_waiting.TryDequeue(out var achievement)) { _showing = false; return; }
        _showing = true;
        _bannerName.Text = achievement.Name;
        Audio.TryPlay("select", 0.5f);
        Tweens.Add(0.3f, t => { Banner.Offset = new Vector2(0, (t - 1) * 20); Banner.Opacity = t; }, Ease.OutCubic);
        Tweens.Add(0.3f, t => Banner.Opacity = 1 - t, delay: 2f, done: ShowNext);
    }
    #endregion

    public override void Draw(IRenderer r) => Heading.Draw(r, Font, "ACHIEVEMENTS");
}

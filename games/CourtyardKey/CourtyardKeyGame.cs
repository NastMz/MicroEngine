using CourtyardKey.Audio;
using CourtyardKey.Content;
using CourtyardKey.Model;
using CourtyardKey.Scenes;
using CourtyardKey.Visuals;
using MicroEngine.Core;
using MicroEngine.Input;

namespace CourtyardKey;

/// <summary>
/// Courtyard Key: a top-down action-adventure. Wren takes up the elder's sword, crosses the vale,
/// and descends into the Sunken Courtyard to reclaim its Key from the Moss Warden.
/// </summary>
public sealed class CourtyardKeyGame : Game
{
    public const string GameId = "CourtyardKey";
    private readonly string? _start;

    /// <param name="saveDirectory">Where the save slot lives (tests pass a temporary folder).</param>
    /// <param name="start">Optional debug checkpoint name, see <see cref="DebugStart"/>.</param>
    public CourtyardKeyGame(string? saveDirectory = null, string? start = null)
    {
        _start = start;
        Saves = new SaveSlots(new SaveStore(GameId, saveDirectory));
    }

    public override GameSettings Settings { get; } = new("Courtyard Key") { Background = Pal.Ink };
    public Sprites Art { get; private set; } = null!;
    public SaveSlots Saves { get; }
    /// <summary>The adventure scene while one is active (title and ending have none).</summary>
    public PlayScene? Play { get; internal set; }

    protected override InputMap CreateInput() => InputMap.CreateDefault().Bind("action", Key.X);

    /// <summary>
    /// Touch: a floating joystick, because the hero walks in any direction and a light push walks slowly (precise
    /// steps next to pits and blocks); A swings and talks, C throws the boomerang, B closes menus. Pause sits at the
    /// top centre, clear of the hearts and the item slots.
    /// </summary>
    protected override TouchLayout CreateTouchLayout() => TouchLayout.Of(
        new TouchStick { Offset = new(104, 100) },
        new TouchButton("confirm", "confirm", "A") { Anchor = TouchAnchor.BottomRight, Offset = new(72, 92), Radius = 40 },
        new TouchButton("action", "action", "C") { Anchor = TouchAnchor.BottomRight, Offset = new(164, 64), Radius = 34 },
        new TouchButton("cancel", "cancel", "B") { Anchor = TouchAnchor.BottomRight, Offset = new(150, 156), Radius = 26 },
        new TouchButton("pause", "pause", "II") { Anchor = TouchAnchor.TopCenter, Offset = new(0, 28), Radius = 20 });

    protected override void Load()
    {
        Art = Sprites.Build(Graphics, new MicroEngine.Core.ContentFolder());
        SoundBank.Register(Audio);
        MusicBank.Register(Audio);
        UseAchievements(CourtyardAchievements.Catalog, Saves.Store, UI.UiStyle.Theme);
        AchievementToasts!.Top = PlayScene.HudHeight + 3;
        Achievements.Unlocked += _ => Audio.Play("secret", 0.8f);
        if (_start is { Length: > 0 } name)
        {
            if (name == "ending") Scenes.Push(new EndingScene(this, DebugStart.Create("boss")));
            else Scenes.Push(new PlayScene(this, DebugStart.Create(name)));
        }
        else Scenes.Push(new TitleScene(this));
    }

    protected override void Unload() => Art?.Dispose();
}

/// <summary>Named checkpoints for captures and debugging (COURTYARD_START environment variable).</summary>
public static class DebugStart
{
    public static Progress Create(string name)
    {
        var p = new Progress();
        if (name == "village") return p;
        p.HasSword = true;
        p.Gems = 40;
        switch (name)
        {
            case "town": p.Entrance = new Checkpoint(Overworld.Id, 1, 2, 12f, 4.6f, Dir.Down); break;
            case "road": p.Entrance = new Checkpoint(Overworld.Id, 1, 1, 10f, 7.5f, Dir.Up); break;
            case "woods": p.Entrance = new Checkpoint(Overworld.Id, 0, 0, 10f, 7.5f, Dir.Up); break;
            case "ruins": p.Entrance = new Checkpoint(Overworld.Id, 2, 0, 14.5f, 7.5f, Dir.Up); break;
            case "bridge": p.Entrance = new Checkpoint(Overworld.Id, 2, 1, 3f, 5f, Dir.Right); break;
            case "dungeon": p.Entrance = Dungeon.EntranceCheckpoint; break;
            case "blocks":
                p.Entrance = new Checkpoint(Dungeon.Id, 0, 2, 17f, 5f, Dir.Left);
                break;
            case "trial":
                p.SmallKeys = 1;
                p.Flags.Add("door:dungeon:1,2:v");
                p.Entrance = new Checkpoint(Dungeon.Id, 0, 1, 17.5f, 5f, Dir.Left);
                break;
            case "channel":
                p.HasBoomerang = true;
                p.SmallKeys = 1;
                p.Entrance = new Checkpoint(Dungeon.Id, 2, 1, 2f, 5f, Dir.Right);
                break;
            case "boss":
                p.HasBoomerang = true;
                p.HasBossKey = true;
                p.MaxHealth = p.Health = 10;
                p.Entrance = new Checkpoint(Dungeon.Id, 1, 0, 10f, 8.4f, Dir.Up);
                break;
            default: throw new ArgumentException($"Unknown start '{name}'.");
        }
        foreach (var room in new[] { (1, 3), (1, 2), (0, 2), (2, 2), (1, 1), (0, 1), (2, 1), (2, 0), (1, 0) })
            if (name is "boss" or "channel") p.MarkVisited(Dungeon.Id, room.Item1, room.Item2);
        p.MarkVisited(Overworld.Id, 1, 2);
        return p;
    }
}

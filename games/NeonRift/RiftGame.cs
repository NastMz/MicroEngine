using NeonRift.Art;
using NeonRift.Gameplay;
using NeonRift.Scenes;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Input;

namespace NeonRift;

/// <summary>Developer launch options (environment variables), used for captures and debugging.</summary>
public sealed record LaunchOptions(string Start = "title", int Stage = 1, int Checkpoint = 0, WeaponKind Weapon = WeaponKind.Pulse, bool God = false, bool Auto = false)
{
    public static LaunchOptions FromEnvironment()
    {
        var start = Environment.GetEnvironmentVariable("RIFT_START") ?? "title";
        int stage = int.TryParse(Environment.GetEnvironmentVariable("RIFT_STAGE"), out var s) ? s : 1;
        int cp = int.TryParse(Environment.GetEnvironmentVariable("RIFT_CHECKPOINT"), out var c) ? c : 0;
        var weapon = Enum.TryParse<WeaponKind>(Environment.GetEnvironmentVariable("RIFT_WEAPON"), true, out var w) ? w : WeaponKind.Pulse;
        bool god = Environment.GetEnvironmentVariable("RIFT_GOD") == "1";
        bool auto = Environment.GetEnvironmentVariable("RIFT_AUTO") == "1";
        return new LaunchOptions(start, stage, cp, weapon, god, auto);
    }
}

/// <summary>
/// Neon Rift: a run &amp; gun about Vela, a courier who storms Halcyon's rooftops, the drowned grotto
/// beneath them and the lab where the Leviathan dreadnought is built.
/// </summary>
public sealed class RiftGame(LaunchOptions? launch = null, string? contentRoot = null, string? saveDirectory = null) : Game
{
    private readonly LaunchOptions _launch = launch ?? new LaunchOptions();

    public override GameSettings Settings { get; } = new("Neon Rift", 480, 270, 3) { Background = Color.Hex("0d0b1a") };
    internal Assets Art { get; private set; } = null!;
    public Session Session { get; private set; } = new();
    /// <summary>Sound names requested but not registered (should stay empty; checked by tests).</summary>
    public HashSet<string> MissingSounds { get; } = [];
    public LaunchOptions Launch => _launch;
    /// <summary>Where achievements persist (a private folder in tests).</summary>
    public SaveStore Store { get; } = new("NeonRift", saveDirectory);

    protected override InputMap CreateInput() => InputMap.CreateDefault()
        .Bind("jump", [Key.Z, Key.Space, Key.J], [PadButton.A])
        .Bind("fire", [Key.X, Key.K], [PadButton.X, PadButton.B])
        .Bind("lock", [Key.C, Key.L, Key.LeftShift], [PadButton.RightShoulder, PadButton.LeftShoulder]);

    /// <summary>
    /// Touch: a floating joystick for running and eight-way aiming (the thumb rolls between diagonals more easily than
    /// on a d-pad); FIRE and JUMP side by side, and the thumb may roll from one onto the other; LOCK is a toggle, so
    /// standing still to aim needs no third finger.
    /// </summary>
    protected override TouchLayout CreateTouchLayout() => TouchLayout.Of(
        new TouchStick { Offset = new(104, 100), Threshold = 0.35f },
        new TouchButton("fire", "fire", "FIRE") { Anchor = TouchAnchor.BottomRight, Offset = new(72, 92), Radius = 40, Also = ["cancel"] },
        new TouchButton("jump", "jump", "JUMP") { Anchor = TouchAnchor.BottomRight, Offset = new(166, 60), Radius = 36, Also = ["confirm"] },
        new TouchButton("lock", "lock", "LOCK") { Anchor = TouchAnchor.BottomRight, Offset = new(150, 156), Radius = 28, Mode = TouchButtonMode.Toggle },
        new TouchButton("pause", "pause", "II") { Anchor = TouchAnchor.TopRight, Offset = new(30, 30), Radius = 20 }) with
    {
        SlideBetweenButtons = true,
        // Neon signs: cyan outlines, magenta when pressed, as in the game's menus.
        Tint = NeonRift.Scenes.Ui.Cyan,
        PressedTint = NeonRift.Scenes.Ui.Magenta,
    };

    protected override void Load()
    {
        var content = new ContentFolder(contentRoot);
        Art = new Assets(Graphics, content) { Font = Font };
        Sounds.Register(Audio, content);
        UseAchievements(RiftAchievements.Catalog, Store, Ui.Theme);
        AchievementToasts!.Top = 22;   // below the HUD gradient
        Achievements.Unlocked += _ => Sfx("powerup", 0.8f);
        Session = new Session { Weapon = _launch.Weapon };
        int stage = Math.Clamp(_launch.Stage, 1, Content.Stages.Count) - 1;
        Scenes.Push(_launch.Start switch
        {
            "stage" => new PlayScene(this, stage, _launch.Checkpoint),
            "card" => new StageCardScene(this, stage),
            "gameover" => new GameOverScene(this, stage),
            "ending" => new EndingScene(this),
            _ => new TitleScene(this),
        });
    }

    public void NewGame() => Session = new Session();

    /// <summary>Plays a sound by name, tolerating names that are not registered.</summary>
    public void Sfx(string name, float volume = 1, float pitch = 1, float pan = 0.5f)
    {
        if (!Audio.TryPlay(name, volume, pitch, 0.03f, pan)) MissingSounds.Add(name);
    }

    public void Music(string name)
    {
        if (name.Length == 0) Audio.StopMusic(0.4f);
        else if (Audio.HasMusic(name)) Audio.PlayMusic(name, 0.6f);
        else MissingSounds.Add(name);
    }

    protected override void Unload() => Art.Dispose();
}

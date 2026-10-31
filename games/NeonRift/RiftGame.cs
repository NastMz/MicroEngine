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

using NeonRift.Art;
using NeonRift.Gameplay;
using NeonRift.Scenes;
using MicroEngine;
using MicroEngine.Content;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Graphics.Devices;
using MicroEngine.Input;
using MicroEngine.Kernel;

namespace NeonRift;

/// <summary>Developer launch options (the host's developer variables), used for captures and debugging.</summary>
public sealed record LaunchOptions(string Start = "title", int Stage = 1, int Checkpoint = 0, WeaponKind Weapon = WeaponKind.Pulse, bool God = false, bool Auto = false)
{
    /// <summary>
    /// <summary>
    /// How the canvas reaches the window: pixel art (whole factors, nearest) for players. RIFT_PRESENT chooses another for
    /// comparisons at window sizes that are not a whole factor of the canvas: <c>integer</c> (whole factors only),
    /// <c>nearest</c> (fills the window, nearest), <c>smooth</c> (fills the window, linear) or <c>sharp</c> (fills the
    /// window, enlarged by a whole factor first and then linear: even pixels with antialiased edges).
    /// </summary>
    public Presentation Presentation { get; init; } = Presentation.PixelArt;

    private static Presentation PresentationFor(string? word) => word switch
    {
        "integer" => Presentation.PixelArt with { Rounding = ScaleRounding.Integer },
        "nearest" => Presentation.PixelArt with { Rounding = ScaleRounding.Fractional },
        "smooth" => Presentation.PixelArt with { Rounding = ScaleRounding.Fractional, Upscale = SamplerDescription.LinearClamp },
        "sharp" => Presentation.PixelArt with { Rounding = ScaleRounding.Fractional, Upscale = SamplerDescription.LinearClamp, Prescale = Presentation.AutoPrescale },
        _ => Presentation.PixelArt,
    };

    /// <summary>
    /// Reads RIFT_START, RIFT_STAGE, RIFT_CHECKPOINT, RIFT_WEAPON, RIFT_GOD=1, RIFT_AUTO=1 and RIFT_PRESENT through
    /// <paramref name="variable"/> (the composition passes the host's <c>HostEnvironment.Variable</c>: environment
    /// variables on desktop, a phone test's <c>--env</c> values); unset or unreadable ones keep their defaults.
    /// </summary>
    public static LaunchOptions From(Func<string, string?> variable)
    {
        ArgumentNullException.ThrowIfNull(variable);
        var start = variable("RIFT_START") ?? "title";
        int stage = int.TryParse(variable("RIFT_STAGE"), out var s) ? s : 1;
        int cp = int.TryParse(variable("RIFT_CHECKPOINT"), out var c) ? c : 0;
        var weapon = Enum.TryParse<WeaponKind>(variable("RIFT_WEAPON"), true, out var w) ? w : WeaponKind.Pulse;
        bool god = variable("RIFT_GOD") == "1";
        bool auto = variable("RIFT_AUTO") == "1";
        return new LaunchOptions(start, stage, cp, weapon, god, auto) { Presentation = PresentationFor(variable("RIFT_PRESENT")) };
    }
}

/// <summary>What the game keeps between launches beside the achievements: the best score ever reached.</summary>
public sealed record RiftRecord(int HighScore);

/// <summary>
/// Neon Rift: a run &amp; gun about Vela, a courier who storms Halcyon's rooftops, the drowned grotto
/// beneath them and the lab where the Leviathan dreadnought is built.
/// </summary>
public sealed class RiftGame(LaunchOptions? launch = null, string? contentRoot = null, string? saveDirectory = null) : Game
{
    private readonly LaunchOptions _launch = launch ?? new LaunchOptions();

    public override GameSettings Settings { get; } = new("Neon Rift", 480, 270, 3)
    {
        Background = Color.Hex("0d0b1a"),
        Presentation = (launch ?? new LaunchOptions()).Presentation,
    };
    internal Assets Art { get; private set; } = null!;
    public Session Session { get; private set; } = new();
    /// <summary>Sound names requested but not registered (should stay empty; checked by tests).</summary>
    public HashSet<string> MissingSounds { get; } = [];
    public LaunchOptions Launch => _launch;
    /// <summary>A content folder of the game's own (tests); null reads the composition's content.</summary>
    protected override string? ContentRoot => contentRoot;
    /// <summary>Where achievements persist (a private folder in tests).</summary>
    public SaveStore Store { get; } = new("NeonRift", saveDirectory);
    /// <summary>The <see cref="Store"/> slot of the high score (the achievements have their own).</summary>
    public const string RecordSlot = "record";
    private int _savedHighScore;

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

    // Achievements are declared before the engine is composed, so it adds their module.
    public override void Configure(EngineBuilder builder) => Banners.Attach(builder, TrackAchievements(RiftAchievements.Catalog, Store));

    /// <summary>The unlock banners, drawn above every scene.</summary>
    public DemoKit.AchievementBanners Banners { get; } = new() { Theme = Ui.Theme };

    /// <summary>The unlock banners stay up while scenes change.</summary>
    protected override void DrawAboveScenes(MicroEngine.Graphics.IRenderer canvas) => Banners.Draw(canvas, Font);


    protected override void Load()
    {
        // The art bundle is an asset of the game's store, so Game.Assets disposes its atlas and backdrops with the game.
        Art = Assets.Scope.Acquire(new AssetKey(NeonRift.Art.Assets.Kind, "art"), _ => new Assets(Graphics, Assets) { Font = Font });
        Sounds.Register(Audio, Assets);
        Banners.Top = 22;   // below the HUD gradient
        Achievements.Unlocked += _ => Sfx("powerup", 0.8f);
        _savedHighScore = Math.Max(0, Store.Load<RiftRecord>(RecordSlot)?.HighScore ?? 0);
        Session = new Session(_savedHighScore) { Weapon = _launch.Weapon };
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

    /// <summary>Starts a run from zero; the high score carries over (saved first, in case the last run never ended).</summary>
    public void NewGame()
    {
        SaveHighScore();
        Session = new Session(Session.HighScore);
    }

    /// <summary>
    /// Saves the session's high score when it beats the saved one. Called where a run ends or reaches a milestone: a
    /// cleared stage, game over, the title screen (after quitting a run), a new run and closing the game.
    /// </summary>
    public void SaveHighScore()
    {
        if (Session.HighScore > _savedHighScore && Store.Save(RecordSlot, new RiftRecord(Session.HighScore)))
            _savedHighScore = Session.HighScore;
    }

    protected override void Unload() => SaveHighScore();

    /// <summary>
    /// Adds the run's session (lives, continues, score, weapon, deaths). The launch options are fixed for the run and
    /// already decide the first scene; the art, the save store and the list of missing sounds are not simulation state.
    /// </summary>
    public override void HashState(StateHasher hash) => hash.Add(Session);

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
}

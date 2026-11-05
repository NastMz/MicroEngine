using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Kernel;
using MicroEngine.Scenes;
using TrailDuel.Rules;
using TrailDuel.Scenes;
using TrailDuel.Sound;
using TrailDuel.Visuals;

namespace TrailDuel;

/// <summary>
/// Trail Duel: a creature-collecting, turn-based adventure from Sprout Town to the Trail Master.
/// TRAIL_DEBUG (gallery, battle, route, forest, brook, hall, evolve, credits...) jumps ahead for captures; the
/// composition (<see cref="Startup.Configure"/>) reads it from the host and passes it in.
/// </summary>
public sealed class TrailDuelGame : Game
{
    private readonly string? _saveDirectory;
    private readonly string? _debugStart;

    /// <param name="saveDirectory">Where saves and achievements go; the platform's default when null.</param>
    /// <param name="debugStart">A development shortcut (<see cref="DebugStart"/>); null or empty opens the title screen.</param>
    /// <param name="debugEvolve">The species that evolves under the <c>evolve</c> shortcut; Sprigbun when null.</param>
    public TrailDuelGame(string? saveDirectory = null, string? debugStart = null, string? debugEvolve = null)
    {
        _saveDirectory = saveDirectory;
        _debugStart = debugStart;
        DebugEvolve = debugEvolve;
        Saves = new SaveStore("TrailDuel", _saveDirectory);
        // Declared before the engine is composed, so it adds the achievements module.
        UseAchievements(TrailAchievements.Catalog, Saves, Ui.UiKit.Theme);
    }

    private readonly GameSettings _settings = new("Trail Duel") { Background = Color.Hex("2b2140") };
    public override GameSettings Settings => _settings;

    /// <summary>Updates per second (60 by default); every timing follows the loop's step, so tests run other rates.</summary>
    internal int UpdateRate { get => _settings.UpdateRate; init => _settings = _settings with { UpdateRate = value }; }
    /// <summary>The species that evolves under the <c>evolve</c> debug start (TRAIL_EVOLVE), or null for the default.</summary>
    internal string? DebugEvolve { get; }
    internal SpriteAtlas Atlas { get; private set; } = null!;
    internal SaveStore Saves { get; private set; } = null!;
    /// <summary>The adventure in progress (null on the title screen before starting).</summary>
    public GameState State { get; internal set; } = GameState.NewGame();

    /// <summary>
    /// The default actions; the stick moves in four sectors, because the overworld is a grid and menus are lists, so
    /// a slightly diagonal push must not step sideways.
    /// </summary>
    protected override InputMap CreateInput()
    {
        var input = InputMap.CreateDefault();
        input.StickMode = StickDirections.Four;
        return input;
    }

    /// <summary>
    /// Touch: a four-way d-pad for grid steps, menus and the battle grid; A confirms, B cancels and runs while held, C
    /// shows move details in battle, MENU opens the start menu.
    /// </summary>
    protected override TouchLayout CreateTouchLayout() => TouchLayout.Of(
        new TouchDPad { Offset = new(104, 100), Diagonals = false },
        new TouchButton("confirm", "confirm", "A") { Anchor = TouchAnchor.BottomRight, Offset = new(72, 92), Radius = 40 },
        new TouchButton("cancel", "cancel", "B") { Anchor = TouchAnchor.BottomRight, Offset = new(164, 62), Radius = 34 },
        new TouchButton("action", "action", "C") { Anchor = TouchAnchor.BottomRight, Offset = new(150, 156), Radius = 26 },
        new TouchButton("pause", "pause", "MENU") { Anchor = TouchAnchor.TopRight, Offset = new(40, 30), Radius = 26 }) with
    {
        // Rounded squares in the menus' cream and coral, like the boxes of its handheld-style interface.
        Tint = Pal.Cream,
        PressedTint = Pal.Coral,
        ButtonStyle = new TouchStyle { Shape = TouchShape.RoundedSquare, Corner = 0.4f },
        DPadStyle = new TouchStyle { Shape = TouchShape.RoundedSquare, Corner = 0.3f },
    };

    protected override void Load()
    {
        var content = new ContentFolder();
        Atlas = AtlasFactory.Build(Graphics, content);
        SoundBank.Register(Audio);
        MusicBank.Register(Audio, content);
        Audio.MusicVolume = 0.6f;
        Achievements.Unlocked += _ => Audio.Play("sparkle", 0.8f);
        if (_debugStart is { Length: > 0 } debug && DebugStart.Create(this, debug) is { } scene) Scenes.Push(scene);
        else Scenes.Push(new TitleScene(this));
    }

    protected override void Unload() => Atlas.Dispose();

    /// <summary>
    /// State hash (section <c>game</c>): the adventure in progress (<see cref="GameState"/>). Left out: the save folder
    /// and the debug start (launch options, not state; the save folder differs per run), the atlas and the save store
    /// (platform and storage objects). Scenes add their own state.
    /// </summary>
    public override void HashState(StateHasher hash) => hash.Add(State);

    public bool HasSave => Saves.Exists(GameState.SaveSlot);

    public void StartNewGame()
    {
        State = GameState.NewGame();
        Scenes.Reset(new OverworldScene(this, intro: true), Transition.Fade(1.2f, Pal.Ink));
    }

    public void ContinueGame()
    {
        State = GameState.Load(Saves) ?? GameState.NewGame();
        Scenes.Reset(new OverworldScene(this), Transition.Fade(1f, Pal.Ink));
    }

    public void SaveGame() => State.Save(Saves);

    internal Sprite Sprite(string name) => Atlas[name];
}

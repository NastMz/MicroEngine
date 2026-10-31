using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using TrailDuel.Rules;
using TrailDuel.Scenes;
using TrailDuel.Sound;
using TrailDuel.Visuals;

namespace TrailDuel;

/// <summary>
/// Trail Duel: a creature-collecting, turn-based adventure from Sprout Town to the Trail Master.
/// Set TRAIL_DEBUG (gallery, battle, route, forest, brook, hall, evolve, credits) to jump ahead for captures.
/// </summary>
public sealed class TrailDuelGame : Game
{
    private readonly string? _saveDirectory;
    private readonly string? _debugStart;

    public TrailDuelGame(string? saveDirectory = null, string? debugStart = null)
    {
        _saveDirectory = saveDirectory;
        _debugStart = debugStart ?? Environment.GetEnvironmentVariable("TRAIL_DEBUG");
    }

    public override GameSettings Settings { get; } = new("Trail Duel") { Background = Color.Hex("2b2140") };
    internal SpriteAtlas Atlas { get; private set; } = null!;
    internal SaveStore Saves { get; private set; } = null!;
    /// <summary>The adventure in progress (null on the title screen before starting).</summary>
    public GameState State { get; internal set; } = GameState.NewGame();

    protected override void Load()
    {
        Saves = new SaveStore("TrailDuel", _saveDirectory);
        var content = new ContentFolder();
        Atlas = AtlasFactory.Build(Graphics, content);
        SoundBank.Register(Audio);
        MusicBank.Register(Audio, content);
        Audio.MusicVolume = 0.6f;
        UseAchievements(TrailAchievements.Catalog, Saves, Ui.UiKit.Theme);
        Achievements.Unlocked += _ => Audio.Play("sparkle", 0.8f);
        if (_debugStart is { Length: > 0 } debug && DebugStart.Create(this, debug) is { } scene) Scenes.Push(scene);
        else Scenes.Push(new TitleScene(this));
    }

    protected override void Unload() => Atlas.Dispose();

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

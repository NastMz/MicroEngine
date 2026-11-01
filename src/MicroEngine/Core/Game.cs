using MicroEngine.Audio;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace MicroEngine.Core;

/// <summary>
/// Base class for a game. Subclasses declare <see cref="Settings"/>, build content in
/// <see cref="Load"/> and push the first scene. The runner owns the loop.
/// </summary>
public abstract class Game
{
    private IPlatform? _platform;
    private float _achievementFlush;

    public abstract GameSettings Settings { get; }
    public IGraphicsDevice Graphics => Platform.Graphics;
    public AudioMixer Audio { get; private set; } = null!;
    public InputMap Input { get; private set; } = null!;
    /// <summary>The on-screen touch controls (<see cref="CreateTouchLayout"/>), drawn and read wherever the platform has a touch screen.</summary>
    public VirtualControls Touch { get; private set; } = null!;
    public SceneManager Scenes { get; private set; } = null!;
    public BitmapFont Font { get; private set; } = null!;
    /// <summary>Seconds of simulated time since start.</summary>
    public float Time { get; private set; }
    public long UpdateCount { get; private set; }
    /// <summary>Deterministic per-run randomness; seeded by the launcher.</summary>
    public Random Random { get; private set; } = new(1);
    public bool QuitRequested { get; private set; }
    public int Width => Settings.VirtualWidth;
    public int Height => Settings.VirtualHeight;
    private AchievementTracker? _achievements;
    /// <summary>The game's achievements; available after <see cref="UseAchievements"/>.</summary>
    public AchievementTracker Achievements => _achievements ?? throw new InvalidOperationException("Achievements are off: call UseAchievements in Load.");
    public bool HasAchievements => _achievements is not null;
    /// <summary>Unlock banners drawn above every scene (null without achievements).</summary>
    public AchievementToasts? AchievementToasts { get; private set; }
    protected IPlatform Platform => _platform ?? throw new InvalidOperationException("Game is not running.");

    /// <summary>Called once after the platform exists. Create atlases, sounds and push the first scene.</summary>
    protected abstract void Load();
    /// <summary>Called once before the platform is disposed.</summary>
    protected virtual void Unload() { }
    /// <summary>Called when the platform suspends the game (sent to the background), after music paused and achievements were saved.</summary>
    protected virtual void OnSuspended() { }
    /// <summary>Called when a suspended game comes back, before its next update.</summary>
    protected virtual void OnResumed() { }
    /// <summary>Override to change bindings; the default suits most games.</summary>
    protected virtual InputMap CreateInput() => InputMap.CreateDefault();
    /// <summary>
    /// Override to choose the on-screen controls for touch screens: which d-pad, joystick and buttons press which actions,
    /// and where. The default (<see cref="TouchLayout.CreateDefault"/>) matches <see cref="InputMap.CreateDefault"/>.
    /// </summary>
    protected virtual TouchLayout CreateTouchLayout() => TouchLayout.CreateDefault();

    public void Quit() => QuitRequested = true;

    /// <summary>
    /// Turns on achievements, usually from <see cref="Load"/>. Progress persists in the "achievements" slot
    /// of <paramref name="store"/> (the game's own save folder) and every unlock shows a banner styled with
    /// <paramref name="theme"/>. Report progress through <see cref="Achievements"/>.
    /// </summary>
    protected AchievementTracker UseAchievements(IEnumerable<Achievement> catalog, SaveStore store, UiTheme? theme = null)
    {
        _achievements = new AchievementTracker(catalog, store);
        AchievementToasts = new AchievementToasts { Theme = theme ?? UiTheme.Default };
        _achievements.Unlocked += AchievementToasts.Show;
        return _achievements;
    }

    internal void Initialize(IPlatform platform, int seed)
    {
        _platform = platform;
        Random = new Random(seed);
        Input = CreateInput();
        Touch = new VirtualControls(CreateTouchLayout());
        Input.AddSource(Touch);
        if (platform.Touch is { } screen) screen.Overlay = Touch;
        Audio = new AudioMixer(platform.Audio);
        Font = BitmapFont.CreateDefault(platform.Graphics);
        Scenes = new SceneManager(this);
        Load();
        Scenes.ApplyPending();
    }

    internal void UpdateTouch(ITouchScreen? screen, float dt) => Touch.Update(screen, dt, Input.LastDevice);

    internal void Step(float dt)
    {
        Time += dt;
        UpdateCount++;
        Scenes.Update(dt);
        Audio.Update(dt);
        AchievementToasts?.Update(dt);
        if (_achievements is { } achievements && (_achievementFlush += dt) >= 1f)
        {
            _achievementFlush = 0;
            achievements.Flush();
        }
        Input.EndUpdate();
    }

    internal void Render(IRenderer renderer)
    {
        renderer.Clear(Settings.Background);
        Scenes.Draw(renderer);
        AchievementToasts?.Draw(renderer, Font);
    }

    private bool _pausedMusicForSuspension;

    internal void Suspend()
    {
        // Music the game paused itself stays paused after resuming.
        _pausedMusicForSuspension = !Audio.MusicPaused;
        if (_pausedMusicForSuspension) Audio.PauseMusic(true);
        _achievements?.Flush();
        // The system never lifts fingers that were down when the game left.
        Touch.CancelAll();
        OnSuspended();
    }

    internal void Resume()
    {
        if (_pausedMusicForSuspension) Audio.PauseMusic(false);
        _pausedMusicForSuspension = false;
        OnResumed();
    }

    internal void Shutdown()
    {
        Scenes.Clear();
        _achievements?.Flush();
        Unload();
        Font.Dispose();
    }

    /// <summary>Test hook: initializes against a platform without running the loop.</summary>
    public void InitializeForTest(IPlatform platform, int seed = 1) => Initialize(platform, seed);
    /// <summary>Test hook: advances one fixed step after sampling input.</summary>
    public void StepForTest()
    {
        UpdateTouch(Platform.Touch, 1f / Settings.UpdateRate);
        Input.Sample(Platform.Input);
        Step(1f / Settings.UpdateRate);
    }
}

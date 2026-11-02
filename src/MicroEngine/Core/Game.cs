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
/// <remarks>
/// The engine services (<see cref="Graphics"/>, <see cref="Audio"/>, <see cref="Input"/>, <see cref="Scenes"/>,
/// <see cref="Font"/>) exist from <see cref="Load"/> on; reading them in the constructor fails. The simulation advances in
/// fixed steps of <c>1 / UpdateRate</c> seconds; drawing happens once per frame.
/// </remarks>
/// <example>
/// <code>
/// public sealed class MyGame : Game
/// {
///     public override GameSettings Settings { get; } = new("My Game", 320, 180);
///
///     protected override void Load() => Scenes.Push(new TitleScene());
/// }
/// </code>
/// </example>
public abstract class Game
{
    private IPlatform? _platform;
    private float _achievementFlush;

    /// <summary>The title, virtual canvas size, window scale, update rate and background colour of the game. Read once when the game starts; keep it constant.</summary>
    public abstract GameSettings Settings { get; }
    /// <summary>Creates textures from images (<see cref="PixelImage"/>, atlases, fonts). Available from <see cref="Load"/> on; throws <see cref="InvalidOperationException"/> before.</summary>
    public IGraphicsDevice Graphics => Platform.Graphics;
    /// <summary>The mixer that loads and plays sound effects and music. Created before <see cref="Load"/>.</summary>
    public AudioMixer Audio { get; private set; } = null!;
    /// <summary>The input actions, built by <see cref="CreateInput"/> before <see cref="Load"/> and sampled once per update.</summary>
    public InputMap Input { get; private set; } = null!;
    /// <summary>The on-screen touch controls (<see cref="CreateTouchLayout"/>), drawn and read wherever the platform has a touch screen.</summary>
    public VirtualControls Touch { get; private set; } = null!;
    /// <summary>
    /// How to name each action to the player on the device in use (<c>Prompts.Format("Press {confirm}")</c>), from the
    /// bindings of <see cref="Input"/> and the controls of <see cref="Touch"/>. Created with them, before <see cref="Load"/>.
    /// </summary>
    public ActionPrompts Prompts { get; private set; } = null!;
    /// <summary>The scene stack. Push the first scene from <see cref="Load"/>.</summary>
    public SceneManager Scenes { get; private set; } = null!;
    /// <summary>
    /// The game's content files: textures, atlases and fonts loaded from <c>Content/</c>, shared and released by the
    /// engine (<see cref="AssetCache"/>). Created before <see cref="Load"/>; everything it loaded is disposed after
    /// <see cref="Unload"/>.
    /// </summary>
    public AssetCache Assets { get; private set; } = null!;
    /// <summary>
    /// The content folder <see cref="Assets"/> reads; null (the default) for <see cref="ContentFolder.DefaultRoot"/> or the
    /// <c>Content</c> folder next to the executable. Tests override it to point at the game's sources.
    /// </summary>
    protected virtual string? ContentRoot => null;
    /// <summary>The built-in pixel font, created before <see cref="Load"/> and disposed by the engine when the game shuts down.</summary>
    public BitmapFont Font { get; private set; } = null!;
    /// <summary>Seconds of simulated time since start.</summary>
    public float Time { get; private set; }
    /// <summary>Number of fixed updates simulated since start (0 during <see cref="Load"/>).</summary>
    public long UpdateCount { get; private set; }
    /// <summary>
    /// How far the frame being drawn is between the last update and the next one, from 0 to 1: the part of a step of
    /// real time that has passed but not been simulated yet. Updates run at a fixed rate and frames at the display's, so
    /// on a display whose rate is not a multiple of <see cref="GameSettings.UpdateRate"/> (75, 100, 144 Hz...) some
    /// frames show no new update; drawing moving things at
    /// <c>Vector2.Lerp(previousPosition, position, FrameBlend)</c> makes their motion even (drawn up to one update behind,
    /// like every interpolating loop). Always 1 when time advances one step per frame (headless, scripted runs), so
    /// captures show exactly the last update.
    /// </summary>
    public float FrameBlend { get; internal set; }
    /// <summary>
    /// The screen while the game runs: displays, VSync, frame limit, window mode, and the graphics API and GPU in use
    /// (<see cref="GraphicsSettings"/> for the options it starts with). Available from <c>Load</c>.
    /// </summary>
    public GameScreen Screen { get; private set; } = null!;
    /// <summary>Deterministic per-run randomness; seeded by the launcher.</summary>
    public Random Random { get; private set; } = new(1);
    /// <summary>True after <see cref="Quit"/>; the loop stops at the end of the current frame.</summary>
    public bool QuitRequested { get; private set; }
    /// <summary>Width of the virtual canvas in pixels (<see cref="GameSettings.VirtualWidth"/>).</summary>
    public int Width => Settings.VirtualWidth;
    /// <summary>Height of the virtual canvas in pixels (<see cref="GameSettings.VirtualHeight"/>).</summary>
    public int Height => Settings.VirtualHeight;
    private AchievementTracker? _achievements;
    /// <summary>The game's achievements; available after <see cref="UseAchievements"/>.</summary>
    public AchievementTracker Achievements => _achievements ?? throw new InvalidOperationException("Achievements are off: call UseAchievements in Load.");
    /// <summary>Whether <see cref="UseAchievements"/> was called, so <see cref="Achievements"/> can be read without throwing.</summary>
    public bool HasAchievements => _achievements is not null;
    /// <summary>Unlock banners drawn above every scene (null without achievements).</summary>
    public AchievementToasts? AchievementToasts { get; private set; }
    /// <summary>The platform running the game (window, audio, input). Throws <see cref="InvalidOperationException"/> before the game starts.</summary>
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

    /// <summary>Asks the loop to stop. The current frame finishes, then every scene exits and <see cref="Unload"/> runs.</summary>
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

    internal void Initialize(IPlatform platform, int seed) => Initialize(platform, seed, Settings.Graphics);

    internal void Initialize(IPlatform platform, int seed, GraphicsSettings graphics)
    {
        _platform = platform;
        Screen = new GameScreen(platform, graphics);
        Random = new Random(seed);
        Input = CreateInput();
        Touch = new VirtualControls(CreateTouchLayout());
        Input.AddSource(Touch);
        Prompts = new ActionPrompts(Input, Touch);
        if (platform.Touch is { } screen) screen.Overlay = Touch;
        platform.Graphics.DefaultFilter = Settings.DefaultTextureFilter;
        Assets = new AssetCache(platform.Graphics, ContentRoot);
        Audio = new AudioMixer(platform.Audio);
        Font = BitmapFont.CreateDefault(platform.Graphics);
        Scenes = new SceneManager(this);
        Load();
        Scenes.ApplyPending();
    }

    internal void UpdateTouch(ITouchScreen? screen, float dt)
    {
        Touch.SetContext(Scenes.Current?.TouchActions);
        Touch.Update(screen, dt, Input.LastDevice);
    }

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
        // From the first update on, a load can show as a hitch: the cache reports slow ones.
        Assets.Playing = true;
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
        Assets.Dispose();
        Font.Dispose();
    }

    /// <summary>Test hook: initializes against a platform without running the loop.</summary>
    public void InitializeForTest(IPlatform platform, int seed = 1) => Initialize(platform, seed);
    /// <summary>Test hook: advances one fixed step after sampling input.</summary>
    public void StepForTest()
    {
        Assets.Update();
        UpdateTouch(Platform.Touch, 1f / Settings.UpdateRate);
        Input.Sample(Platform.Input);
        Step(1f / Settings.UpdateRate);
    }
}

using MicroEngine.Audio;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Kernel;
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
/// <para>
/// A game is an engine module (<see cref="IEngineModule"/>): its services are registered in the engine's
/// <see cref="Services"/> and its per-frame work runs as named systems of the frame <see cref="Schedule"/>. Override
/// <see cref="Configure"/> to add systems, replace services or change the <see cref="Kernel.TimeSettings"/>.
/// </para>
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
public abstract class Game : IEngineModule
{
    private IPlatform? _platform;
    private Engine? _engine;
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
    internal Engine Engine => _engine ?? throw new InvalidOperationException("Game is not running.");
    /// <summary>
    /// The engine's services: every built-in service (<see cref="InputMap"/>, <see cref="AudioMixer"/>,
    /// <see cref="SceneManager"/>, <see cref="AssetCache"/>, <see cref="VirtualControls"/>, <see cref="ActionPrompts"/>,
    /// <see cref="BitmapFont"/>, <see cref="GameScreen"/>, <see cref="IPlatform"/>, <see cref="System.Random"/>, this
    /// game) and those the game's <see cref="Configure"/> registered, found by the type they were registered as.
    /// Available from <see cref="Load"/> on.
    /// </summary>
    public ServiceRegistry Services => Engine.Services;
    /// <summary>The frame's systems by phase, the built-in ones named <c>engine.*</c>; fixed once the game starts.</summary>
    public FrameSchedule Schedule => Engine.Schedule;
    /// <summary>Engine events: suspension, resumption, quit requests (with veto) and, where the platform reports them, resizing and focus.</summary>
    public IEngineLifecycle Lifecycle => Engine.Lifecycle;
    /// <summary>Where the engine's diagnostics go (the <see cref="ILog"/> service; standard error by default).</summary>
    public ILog Log => Engine.Log;
    /// <summary>The time policy of the run: fixed rate, catch-up limits and whether time follows the wall clock or advances one step per frame.</summary>
    public TimeSettings TimeSettings => Engine.Time;
    /// <summary>The time of the frame being run (<see cref="FrameContext"/>): its delta, the fixed step, the blend and the frame number.</summary>
    public FrameContext Frame => Engine.Clock.Context;

    /// <summary>An optional feature the engine declared (the platform's <see cref="ITouchScreen"/>, its <see cref="IPlatformDiagnostics"/>), if there is one.</summary>
    public bool TryGetCapability<T>([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out T? capability) where T : class
    {
        if (_engine is { } engine) return engine.TryGetCapability(out capability);
        capability = null;
        return false;
    }

    /// <summary>
    /// Called once when the game starts, after the built-in services and systems were registered and before
    /// <see cref="Load"/>: add frame systems (<c>builder.Schedule.Add</c>), register or replace services
    /// (<c>builder.Services</c>), edit settings such as <see cref="Kernel.TimeSettings"/> (<c>builder.Configure</c>) and
    /// subscribe to <c>builder.Lifecycle</c>. A run's own choices (<see cref="RunOptions.TimeMode"/>) are applied after
    /// it. The default does nothing.
    /// </summary>
    public virtual void Configure(EngineBuilder builder) { }

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

    /// <summary>
    /// Composes the engine (the built-in services and systems, then <see cref="Configure"/>, then
    /// <paramref name="host"/>'s additions and the run's <paramref name="mode"/>), resolves the services from it and runs
    /// <see cref="Load"/>.
    /// </summary>
    internal void Initialize(IPlatform platform, int seed, GraphicsSettings graphics, TimeMode? mode = null, Action<EngineBuilder>? host = null)
    {
        _platform = platform;
        var builder = new EngineBuilder();
        builder.Configure<TimeSettings>(time => time with
        {
            FixedRate = Settings.UpdateRate,
            Mode = platform.RealTime ? TimeMode.RealTime : TimeMode.Deterministic,
        });
        builder.Add(new GameModule(this, platform, seed, graphics));
        builder.Add(this);
        host?.Invoke(builder);
        if (mode is { } chosen) builder.Configure<TimeSettings>(time => time with { Mode = chosen });
        var engine = _engine = builder.Build();
        var services = engine.Services;
        Screen = services.Get<GameScreen>();
        Random = services.Get<Random>();
        Input = services.Get<InputMap>();
        Touch = services.Get<VirtualControls>();
        Prompts = services.Get<ActionPrompts>();
        Assets = services.Get<AssetCache>();
        Assets.Log = engine.Log;
        Audio = services.Get<AudioMixer>();
        Font = services.Get<BitmapFont>();
        Scenes = services.Get<SceneManager>();
        Load();
        Scenes.ApplyPending();
    }

    /// <summary>
    /// The game's built-in services and frame systems, registered before the game's own <see cref="Configure"/>. The
    /// services are created in the order the engine always created them; the systems keep the order of a step.
    /// </summary>
    private sealed class GameModule(Game game, IPlatform platform, int seed, GraphicsSettings graphics) : IEngineModule
    {
        public void Configure(EngineBuilder builder)
        {
            var services = builder.Services;
            services.Add(game);
            services.Add(platform);
            services.Add(new GameScreen(platform, graphics));
            services.Add(new Random(seed));
            var input = game.CreateInput();
            var touch = new VirtualControls(game.CreateTouchLayout());
            input.AddSource(touch);
            services.Add(input);
            services.Add(touch);
            services.Add(new ActionPrompts(input, touch));
            if (platform.Touch is { } screen)
            {
                screen.Overlay = touch;
                builder.Capabilities.Add(screen);
            }
            if (platform is IPlatformDiagnostics diagnostics) builder.Capabilities.Add(diagnostics);
            platform.Graphics.DefaultFilter = game.Settings.DefaultTextureFilter;
            services.Add(new AssetCache(platform.Graphics, game.ContentRoot));
            services.Add(new AudioMixer(platform.Audio));
            services.Add(BitmapFont.CreateDefault(platform.Graphics));
            services.Add(new SceneManager(game));

            var schedule = builder.Schedule;
            schedule.Add(FramePhase.BeginFrame, "engine.assets", _ => game.Assets.Update());
            schedule.Add(FramePhase.Input, "engine.touch", frame => game.UpdateTouch(platform.Touch, (float)frame.Delta));
            schedule.Add(FramePhase.PreFixed, "engine.input", _ => game.Input.Sample(platform.Input));
            schedule.Add(FramePhase.FixedUpdate, "engine.time", step =>
            {
                game.Time += (float)step.FixedDelta;
                game.UpdateCount++;
            });
            schedule.Add(FramePhase.FixedUpdate, "engine.scenes", step => game.Scenes.Update((float)step.FixedDelta));
            schedule.Add(FramePhase.PostFixed, "engine.audio", step => game.Audio.Update((float)step.FixedDelta));
            schedule.Add(FramePhase.PostFixed, "engine.achievements", step => game.UpdateAchievements((float)step.FixedDelta));
            schedule.Add(FramePhase.PostFixed, "engine.input.end", _ => game.Input.EndUpdate());
            // From the first update on, a load can show as a hitch: the cache reports slow ones.
            schedule.Add(FramePhase.PostFixed, "engine.assets.playing", _ => game.Assets.Playing = true);
            schedule.Add(FramePhase.Render, "engine.render", _ => game.Render(platform.BeginFrame()));
            schedule.Add(FramePhase.Present, "engine.present", _ => platform.EndFrame());
            // The game's own handling comes first, so later handlers see music paused and progress saved.
            builder.Lifecycle.Suspending += game.Suspend;
            builder.Lifecycle.Resumed += game.Resume;
        }
    }

    internal void UpdateTouch(ITouchScreen? screen, float dt)
    {
        Touch.SetContext(Scenes.Current?.TouchActions);
        Touch.Update(screen, dt, Input.LastDevice);
    }

    private void UpdateAchievements(float dt)
    {
        AchievementToasts?.Update(dt);
        if (_achievements is { } achievements && (_achievementFlush += dt) >= 1f)
        {
            _achievementFlush = 0;
            achievements.Flush();
        }
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
    /// <remarks>
    /// Runs the phases that come before drawing for one fixed step, with a frame that stands for exactly one step:
    /// <see cref="FramePhase.BeginFrame"/>, <see cref="FramePhase.Input"/>, the four fixed phases,
    /// <see cref="FramePhase.Update"/> and <see cref="FramePhase.LateUpdate"/>. The loop's clock does not move.
    /// </remarks>
    public void StepForTest()
    {
        var engine = Engine;
        double step = engine.Time.FixedDelta;
        var frame = new FrameContext(step, step, 1f, UpdateCount, engine.Clock.Frame);
        engine.Run(FramePhase.BeginFrame, frame);
        engine.Run(FramePhase.Input, frame);
        engine.RunFixedStep(frame with { Blend = 0 });
        engine.Run(FramePhase.Update, frame with { FixedStep = UpdateCount });
        engine.Run(FramePhase.LateUpdate, frame with { FixedStep = UpdateCount });
    }
}

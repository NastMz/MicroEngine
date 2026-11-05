using MicroEngine.Audio;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Kernel;
using MicroEngine.Platform;
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
public abstract class Game : IEngineModule, IStateHashed
{
    private IPlatform? _platform;
    private Engine? _engine;

    /// <summary>The title, virtual canvas size, window scale, update rate and background colour of the game. Read once when the game starts; keep it constant.</summary>
    public abstract GameSettings Settings { get; }
    /// <summary>Creates textures from images (<see cref="PixelImage"/>, atlases, fonts). Available from <see cref="Load"/> on; throws <see cref="InvalidOperationException"/> before.</summary>
    public IGraphicsDevice Graphics => Platform.Graphics;
    /// <summary>The mixer that loads and plays sound effects and music. Created before <see cref="Load"/>.</summary>
    public AudioMixer Audio { get; private set; } = null!;
    /// <summary>
    /// The input actions of player 1, built by <see cref="CreateInput"/> before <see cref="Load"/> and sampled once per
    /// update. With no setup player 1 owns every device, so a single-player game reads only this.
    /// </summary>
    public InputMap Input { get; private set; } = null!;
    /// <summary>
    /// The engine's input (<see cref="Input.InputSystem"/>): the local players (<see cref="InputSystem.Players"/>;
    /// player 1's map is <see cref="Input"/>), the devices and their events, the input layers events pass through before
    /// gameplay (<see cref="InputSystem.Layers"/>; the on-screen controls are one), this update's events no layer took
    /// (<see cref="InputSystem.Events"/>) and the text channel (<see cref="InputSystem.Text"/>). Created before
    /// <see cref="Load"/>.
    /// </summary>
    public InputSystem InputSystem { get; private set; } = null!;
    /// <summary>
    /// The on-screen touch controls (<see cref="CreateTouchLayout"/>), available from <see cref="Configure"/> on. They are
    /// an opt-in module: active, drawn and read on a touch-first screen (<see cref="ITouchScreen.TouchFirst"/>: phones,
    /// tablets, touch-only browsers) or once the game sets their <see cref="VirtualControls.Layout"/> (in
    /// <see cref="Configure"/>, <see cref="Load"/> or a scene), on any platform with a touch screen; otherwise inactive,
    /// with no frame system, input layer, action source or overlay (<see cref="VirtualControls.IsActive"/>). Controls
    /// activated after the engine was built join the game at the start of the next frame.
    /// </summary>
    public VirtualControls Touch { get; private set; } = null!;
    /// <summary>
    /// How to name each action to the player on the device in use (<c>Prompts.Format("Press {confirm}")</c>), from the
    /// bindings of <see cref="Input"/> and the controls of <see cref="Touch"/>. Created with them, before <see cref="Load"/>.
    /// </summary>
    public ActionPrompts Prompts { get; private set; } = null!;
    /// <summary>The scene stack. Push the first scene from <see cref="Load"/>.</summary>
    public SceneManager Scenes { get; private set; } = null!;
    /// <summary>
    /// The world view and the UI view, and the mapping between window pixels, canvas pixels and UI units, with the
    /// pointer in each (<see cref="GameViews"/>).
    /// </summary>
    public GameViews Views => _views ??= new GameViews(this);
    private GameViews? _views;
    /// <summary>
    /// The game's content files: textures, atlases and fonts loaded from <c>Content/</c>, shared and released by the
    /// engine (<see cref="AssetCache"/>). Created before <see cref="Load"/>; everything it loaded is disposed after
    /// <see cref="Unload"/>.
    /// </summary>
    public AssetCache Assets { get; private set; } = null!;
    /// <summary>
    /// A content folder of the game's own for <see cref="Assets"/>, which then wins over the composition's content. Null
    /// (the default) reads the content the composition names (<see cref="Hosting.HostBuilder.Content"/>: a macOS bundle's
    /// resources, a phone's unpacked files, a zip archive), or else the <c>Content</c> folder next to the executable
    /// (<see cref="ContentFolder.DefaultRoot"/>). Tests override it to point at the game's sources.
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
    /// <summary>
    /// Deterministic per-run randomness, seeded by the launcher (<see cref="RunOptions.Seed"/>): a
    /// <see cref="SeededRandom"/>, which draws exactly what <c>new Random(seed)</c> draws and whose state is part of the
    /// per-step state hash (<see cref="ComputeStateHash"/>).
    /// </summary>
    public Random Random { get; private set; } = new(1);
    /// <summary>True after <see cref="Quit"/>; the loop stops at the end of the current frame.</summary>
    public bool QuitRequested { get; private set; }
    /// <summary>Width of the virtual canvas in pixels (<see cref="GameSettings.VirtualWidth"/>).</summary>
    public int Width => Settings.VirtualWidth;
    /// <summary>Height of the virtual canvas in pixels (<see cref="GameSettings.VirtualHeight"/>).</summary>
    public int Height => Settings.VirtualHeight;
    /// <summary>
    /// A camera the size of the canvas whose defaults follow the game's presentation
    /// (<see cref="GameSettings.EffectivePresentation"/>): snapped to whole pixels for pixel art, smooth for
    /// high-resolution 2D.
    /// </summary>
    public Camera2D CreateCamera() => new(Width, Height, Settings.EffectivePresentation);
    private AchievementTracker? _achievements;
    /// <summary>The game's achievements; available after <see cref="UseAchievements"/>.</summary>
    public AchievementTracker Achievements => _achievements ?? throw new InvalidOperationException("Achievements are off: call UseAchievements from the game's constructor or Configure.");
    /// <summary>Whether <see cref="UseAchievements"/> was called, so <see cref="Achievements"/> can be read without throwing.</summary>
    public bool HasAchievements => _achievements is not null;
    /// <summary>Unlock banners drawn above every scene (null without achievements).</summary>
    public AchievementToasts? AchievementToasts { get; private set; }
    /// <summary>The platform running the game (window, audio, input). Throws <see cref="InvalidOperationException"/> before the game starts.</summary>
    protected IPlatform Platform => _platform ?? throw new InvalidOperationException("Game is not running.");
    internal Engine Engine => _engine ?? throw new InvalidOperationException("Game is not running.");
    /// <summary>The engine once the game started (also after it shut down); null before.</summary>
    internal Engine? EngineOrNull => _engine;
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

    /// <summary>
    /// An optional feature, if there is one: one the engine declared (<see cref="EngineBuilder.Capabilities"/>), else one
    /// the platform offers (<see cref="IPlatformHost.TryGetCapability{T}"/>: its <see cref="ITouchScreen"/>, its
    /// <see cref="IDisplayControl"/>, its <see cref="IPlatformDiagnostics"/>...). False before the game starts.
    /// </summary>
    public bool TryGetCapability<T>([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out T? capability) where T : class
    {
        if (_engine is { } engine && engine.TryGetCapability(out capability)) return true;
        if (_platform is { } platform) return platform.TryGetCapability(out capability);
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
    /// and where. The default (<see cref="TouchLayout.CreateDefault"/>) matches <see cref="InputMap.CreateDefault"/> and is
    /// used on touch-first screens only. A layout of the game's own, returned without calling the base method, is the
    /// game setting a layout: it activates the controls on any platform with a touch screen (<see cref="Touch"/>).
    /// </summary>
    protected virtual TouchLayout CreateTouchLayout()
    {
        _touchLayoutDeclared = false;
        return TouchLayout.CreateDefault(_inputNames);
    }

    private ActionNames? _inputNames; // the names of the map CreateInput built, for the default touch layout
    private bool _touchLayoutDeclared; // CreateTouchLayout returned the game's own layout

    /// <summary>Asks the loop to stop. The current frame finishes, then every scene exits and <see cref="Unload"/> runs.</summary>
    public void Quit() => QuitRequested = true;

    /// <summary>
    /// Declares the game's achievements: call it from the game's constructor or from <see cref="Configure"/>, before
    /// the engine is composed, so the engine adds the achievements module (the tracker and <paramref name="store"/> as
    /// services, and the <c>engine.achievements</c> system that writes progress). Progress persists in the
    /// "achievements" slot of <paramref name="store"/> (the game's own save folder) and every unlock shows a banner
    /// styled with <paramref name="theme"/>. Report progress through <see cref="Achievements"/>. A game that never calls
    /// it has no achievement service, system or file.
    /// </summary>
    /// <exception cref="InvalidOperationException">The engine was already composed (called from <see cref="Load"/> or later).</exception>
    protected AchievementTracker UseAchievements(IEnumerable<Achievement> catalog, SaveStore store, UiTheme? theme = null)
    {
        if (_engine is not null)
            throw new InvalidOperationException("Achievements are declared before the engine is composed: call UseAchievements from the game's constructor or from Configure, not from Load.");
        ArgumentNullException.ThrowIfNull(store);
        _achievements = new AchievementTracker(catalog, store);
        _achievementStore = store;
        AchievementToasts = new AchievementToasts { Theme = theme ?? UiTheme.Default };
        _achievements.Unlocked += AchievementToasts.Show;
        return _achievements;
    }

    private SaveStore? _achievementStore;

    /// <summary>The modules of what the game declared: saves and achievements when it called <see cref="UseAchievements"/>.</summary>
    private void AddDeclaredModules(EngineBuilder builder)
    {
        if (_achievements is not { } tracker) return;
        if (!builder.HasModule<Saves.SavesModule>() && !builder.Services.Contains<SaveStore>()) builder.Add(new Saves.SavesModule(_achievementStore!));
        var module = new Achievements.AchievementsModule(tracker);
        module.Stepped += AchievementToasts!.Update;
        builder.Add(module);
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
        // A preset the composition states (the headless platform's: deterministic) comes first; the game's Configure and the
        // run's choice win over it.
        platform.TryGetCapability<RunPreset>(out var preset);
        builder.Configure<TimeSettings>(time => time with { FixedRate = Settings.UpdateRate, Mode = preset?.TimeMode ?? time.Mode });
        builder.Add(new GameModule(this, platform, seed, graphics));
        builder.Add(this);
        AddDeclaredModules(builder);
        // A layout set in Configure activates the on-screen controls on any screen.
        if (Touch.IsActive && !_touchScheduled) AddTouchControls(builder, builder.Services.Get<InputMap>(), builder.Services.Get<InputSystem>());
        host?.Invoke(builder);
        if (mode is { } chosen) builder.Configure<TimeSettings>(time => time with { Mode = chosen });
        var engine = _engine = builder.Build();
        var services = engine.Services;
        Screen = services.Get<GameScreen>();
        Random = services.Get<Random>();
        Input = services.Get<InputMap>();
        InputSystem = services.Get<InputSystem>();
        Touch = services.Get<VirtualControls>();
        Prompts = services.Get<ActionPrompts>();
        Assets = services.Get<AssetCache>();
        Assets.Log = engine.Log;
        Audio = services.Get<AudioMixer>();
        // Deterministic runs keep audio state on the simulation's timeline only (songs render when waited for, sounds
        // never re-anchor to the device), so two runs hash and sound the same.
        Audio.Deterministic = engine.Time.Mode == TimeMode.Deterministic;
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
            services.Add<Random>(new SeededRandom(seed));
            var input = game.CreateInput();
            // The full-screen toggle is the engine's own action: a map that never mentions it gets F11 and Alt+Enter; one
            // that unbound or rebound it keeps its choice.
            if (input.GetBindings(input.Names.Fullscreen) is null) input.BindFullscreenDefaults();
            game._inputNames = input.Names;
            // The on-screen controls exist from here on (Configure may set their layout) but stay inactive, costing
            // nothing, until the game sets a layout or the screen is touch-first (AddTouchControls, RouteInput).
            game._touchLayoutDeclared = true;
            var touch = game.Touch = new VirtualControls(game.CreateTouchLayout());
            if (game._touchLayoutDeclared) touch.Activate();
            var system = CreateInputSystem(platform, input);
            services.Add(input);
            services.Add(system);
            services.Add(system.Players);
            services.Add(touch);
            // Keys are named as the player's layout prints them where the platform knows the layout.
            services.Add(new ActionPrompts(input, touch) { KeyLabels = system.KeyLabels });
            // Optional features are found by query, never by casting the platform.
            platform.TryGetCapability<ITouchScreen>(out var screen);
            game._touchScreen = screen;
            platform.Graphics.DefaultFilter = game.Settings.DefaultTextureFilter;
            services.Add(new AssetCache(platform.Graphics, game.ContentRoot));
            services.Add(new AudioMixer(platform.Audio));
            services.Add(BitmapFont.CreateDefault(platform.Graphics));
            services.Add(new SceneManager(game));

            var schedule = builder.Schedule;
            schedule.Add(FramePhase.BeginFrame, "engine.assets", _ => game.Assets.Update());
            schedule.Add(FramePhase.Input, "engine.input.events", frame => game.RouteInput(screen, (float)frame.Delta));
            // A touch-first screen or a layout of the game's own activates the controls now, so their system keeps its place.
            if (touch.IsActive || screen is { TouchFirst: true }) game.AddTouchControls(builder, input, system);
            schedule.Add(FramePhase.PreFixed, "engine.input", _ => game.InputSystem.Sample());
            schedule.Add(FramePhase.PreFixed, "engine.fullscreen", _ =>
            {
                if (game.Input.Pressed(game.Input.Names.Fullscreen)) game.Screen.ToggleFullscreen();
            });
            schedule.Add(FramePhase.FixedUpdate, "engine.time", step =>
            {
                game.Time += (float)step.FixedDelta;
                game.UpdateCount++;
            });
            schedule.Add(FramePhase.FixedUpdate, "engine.scenes", step => game.Scenes.Update((float)step.FixedDelta));
            schedule.Add(FramePhase.PostFixed, "engine.audio", step => game.Audio.Update((float)step.FixedDelta));
            schedule.Add(FramePhase.PostFixed, "engine.input.end", _ => game.InputSystem.EndUpdate());
            // From the first update on, a load can show as a hitch: the cache reports slow ones.
            schedule.Add(FramePhase.PostFixed, "engine.assets.playing", _ => game.Assets.Playing = true);
            schedule.Add(FramePhase.Render, "engine.render", _ => game.Render(platform.BeginFrame()));
            schedule.Add(FramePhase.Present, "engine.present", _ => platform.EndFrame());
            // Audio work of the host frame, outside the simulation step: songs rendering in slices, music decoded ahead,
            // pumped sinks (browsers).
            schedule.Add(FramePhase.EndFrame, "engine.audio.pump", _ => game.Audio.Pump());
            // The game's own handling comes first, so later handlers see music paused and progress saved.
            builder.Lifecycle.Suspending += game.Suspend;
            builder.Lifecycle.Resumed += game.Resume;
            // A window that loses the focus never hears the releases of what was held.
            builder.Lifecycle.FocusChanged += focused => { if (!focused) game.InputSystem.ReleaseAll(); };
        }
    }

    /// <summary>
    /// The engine's input over <paramref name="platform"/>: its event backend and the optional features it offers when
    /// it has one, otherwise its polled source as one device; a host's extra polled sources as devices of their own.
    /// The on-screen controls join as the touch layer when they activate (<see cref="WireTouch"/>).
    /// </summary>
    private static InputSystem CreateInputSystem(IPlatform platform, InputMap input)
    {
        var system = new InputSystem(input);
        platform.TryGetCapability<IInputBackend>(out var backend);
        platform.TryGetCapability<IKeyLabels>(out var labels);
        platform.TryGetCapability<IGamepadRumble>(out var rumble);
        platform.TryGetCapability<ITextInputControl>(out var text);
        system.Connect(backend, labels, rumble, text);
        var polled = platform.Input;
        if (backend is null) system.AddPolled(polled);
        else if (polled is CompositeInput composite)
        {
            // The backend's own polled view is the same device as its events: only the host's additions are read.
            foreach (var source in composite.Sources)
                if (!ReferenceEquals(source, backend)) system.AddPolled(source);
        }
        else if (!ReferenceEquals(polled, backend)) system.AddPolled(polled);
        return system;
    }

    private ITouchScreen? _touchScreen;
    // Whether the on-screen controls are wired in (source, layer, overlay), and whether engine.touch runs them.
    private bool _touchWired, _touchScheduled;

    /// <summary>
    /// Activates the on-screen controls while the engine is composed: wires them in and schedules <c>engine.touch</c>
    /// right after <c>engine.input.events</c>. Called for a touch-first screen, and after <see cref="Configure"/> when it
    /// set a layout.
    /// </summary>
    private void AddTouchControls(EngineBuilder builder, InputMap input, InputSystem system)
    {
        Touch.Activate();
        WireTouch(input, system);
        builder.Schedule.Add(FramePhase.Input, "engine.touch", frame => UpdateTouch(_touchScreen, (float)frame.Delta), after: "engine.input.events");
        _touchScheduled = true;
    }

    /// <summary>The on-screen controls join the game: an action source of <paramref name="input"/>, the touch layer, the screen's overlay.</summary>
    private void WireTouch(InputMap input, InputSystem system)
    {
        input.AddSource(Touch);
        system.Layers.Add(Touch, InputLayerOrder.TouchControls);
        if (_touchScreen is not null) _touchScreen.Overlay = Touch;
        _touchWired = true;
    }

    private readonly List<TouchEvent> _touches = [];

    /// <summary>
    /// The frame's input routing: the backend's events and those sent by scripts and tests, then the screen's touches
    /// (and scripted touches) as pointers in screen and canvas pixels, all through the input layers to gameplay.
    /// </summary>
    /// <remarks>
    /// The safe point where on-screen controls activated after the engine was built (a layout set in <see cref="Load"/>
    /// or by a scene, or a screen that became touch-first) join the game: at the start of the next frame, before its
    /// input is routed. Those controls are then updated here, at the end of the routing, where <c>engine.touch</c> would
    /// have run them.
    /// </remarks>
    internal void RouteInput(ITouchScreen? screen, float dt = 0)
    {
        if (!_touchWired)
        {
            if (screen is { TouchFirst: true }) Touch.Activate();
            if (Touch.IsActive) WireTouch(Input, InputSystem);
        }
        RouteEvents(screen);
        if (_touchWired && !_touchScheduled) UpdateTouch(screen, dt);
    }

    private void RouteEvents(ITouchScreen? screen)
    {
        if (_touchWired)
        {
            Touch.SetContext(Scenes.Current?.TouchActions);
            Touch.BeginFrame(screen);
        }
        InputSystem.BeginFrame();
        if (screen is null) return;
        _touches.Clear();
        screen.ReadTouches(_touches);
        if (_touchWired) Touch.TakeScripted(_touches);
        if (_touches.Count == 0) return;
        var frame = screen.Overlay?.Frame(screen.Size, screen.SafeArea, Settings.VirtualWidth, Settings.VirtualHeight, Settings.Scaling)
            ?? FrameLayout.Fit(screen.Size, screen.SafeArea, Settings.VirtualWidth, Settings.VirtualHeight, Settings.Scaling);
        foreach (var t in _touches)
        {
            var canvas = FrameLayout.ToCanvas(t.Position, frame, Settings.VirtualWidth, Settings.VirtualHeight);
            var e = t.Phase switch
            {
                TouchPhase.Began => InputEvent.PointerDown(DeviceId.TouchScreen, t.Finger, PointerKind.Touch, canvas),
                TouchPhase.Moved => InputEvent.PointerMove(DeviceId.TouchScreen, t.Finger, PointerKind.Touch, canvas),
                TouchPhase.Ended => InputEvent.PointerUp(DeviceId.TouchScreen, t.Finger, PointerKind.Touch, canvas),
                _ => new InputEvent { Kind = InputEventKind.PointerCancel, Device = DeviceId.TouchScreen, Pointer = t.Finger, PointerKind = PointerKind.Touch, Position = canvas },
            };
            InputSystem.Route(e with { ScreenPosition = t.Position });
        }
    }

    internal void UpdateTouch(ITouchScreen? screen, float dt) => Touch.EndFrame(dt, Input.LastDevice);


    internal void Render(IRenderer renderer)
    {
        renderer.Clear(Settings.Background);
        Scenes.Draw(renderer);
        AchievementToasts?.Draw(renderer, Font);
        // The UI view: at the window's resolution where the platform offers it, else in the canvas over the world.
        var ui = TryGetCapability(out IScreenView? screen) ? screen.BeginUi() : renderer;
        Scenes.DrawUi(ui, separate: !ReferenceEquals(ui, renderer));
    }

    private bool _pausedMusicForSuspension;

    internal void Suspend()
    {
        // Music the game paused itself stays paused after resuming.
        _pausedMusicForSuspension = !Audio.MusicPaused;
        if (_pausedMusicForSuspension) Audio.PauseMusic(true);
        _achievements?.Flush();
        // The system never lifts fingers that were down when the game left, nor keys held when it went away.
        Touch.CancelAll();
        InputSystem.ReleaseAll();
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

    /// <summary>
    /// Adds the game's own simulation state to the per-step state hash (<see cref="ComputeStateHash"/>), after the
    /// engine's sections, in a section named <c>game</c>: the fields of this class that decide what happens next
    /// (progress, a session's lives and score...). Leave out what only drawing or the platform uses (caches written while
    /// drawing, textures, save locations). The default adds nothing; scenes add their own state
    /// (<see cref="Scene.HashState"/>).
    /// </summary>
    public virtual void HashState(StateHasher hash) { }

    private readonly StateHasher _stateHasher = new();

    /// <summary>
    /// Hashes the simulation state now (<see cref="StateHasher"/>), in sections: <c>engine.time</c> (update count and
    /// time), <c>engine.random</c> (<see cref="Random"/>), <c>engine.input</c> (<see cref="Input"/> after sampling, then the other players and the pairings when there are any),
    /// <c>engine.audio</c> (the music playing or paused and the volumes), <c>engine.assets</c> (how many assets are
    /// loaded), <c>engine.achievements</c> (when on), <c>engine.scenes</c> (the stack and the transition), one
    /// <c>scene[i]:Type</c> per scene from the bottom (its age, coroutines and tweens, then <see cref="Scene.HashState"/>)
    /// and <c>game</c> (<see cref="HashState"/>). The loop records it after every fixed step of a run given a
    /// <see cref="RunOptions.StateTrace"/>; drawing never changes it.
    /// </summary>
    /// <param name="sections">Whether the result carries each section's hash, to name the state that differs.</param>
    /// <exception cref="InvalidOperationException">The game has not started.</exception>
    public StepHash ComputeStateHash(bool sections = false)
    {
        _ = Engine;
        var hash = _stateHasher;
        hash.Reset();
        hash.Section("engine.time");
        hash.Add(UpdateCount);
        hash.Add(Time);
        hash.Section("engine.random");
        hash.Add(Random as IStateHashed);
        hash.Section("engine.input");
        Input.HashState(hash);
        // Nothing for one player without pairings, so single-player hashes are player 1's map alone.
        InputSystem.HashState(hash);
        // Logical music state only. In browsers songs still render in wall-clock slices, so this section can differ
        // there until music state is independent of rendering.
        hash.Section("engine.audio");
        hash.Add(Audio.CurrentMusic);
        hash.Add(Audio.MusicPaused);
        hash.Add(Audio.MusicVolume);
        hash.Add(Audio.SfxVolume);
        hash.Section("engine.assets");
        hash.Add(Assets.Count);
        if (_achievements is { } achievements)
        {
            hash.Section("engine.achievements");
            foreach (var status in achievements.All)
            {
                hash.Add(status.Id);
                hash.Add(status.Progress);
                hash.Add(status.Unlocked);
            }
        }
        Scenes.HashState(hash);
        hash.Section("game");
        HashState(hash);
        return new StepHash(UpdateCount, hash.Value, sections ? hash.Sections : []);
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

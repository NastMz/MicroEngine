using MicroEngine.Core.Graphics;
using MicroEngine.Core.Input;
using MicroEngine.Core.Logging;
using MicroEngine.Core.Profiling;
using MicroEngine.Core.Scenes;
using MicroEngine.Core.Time;

namespace MicroEngine.Core.Engine;

/// <summary>
/// Core game engine that manages the main loop, scenes, and system lifecycle.
/// Implements fixed timestep for deterministic updates and variable render rate.
/// </summary>
public sealed class GameEngine
{
    private const string LOG_CATEGORY = "Engine";

    private readonly EngineConfiguration _configuration;
    private readonly ILogger _logger;
    private readonly GameTime _gameTime;
    private readonly PrecisionTimer _timer;
    private readonly SceneManager _sceneManager;
    private readonly IWindow _window;
    private readonly IRenderer2D _renderer;
    private readonly IInputBackend _inputBackend;
    private readonly MemoryProfiler? _memoryProfiler;

    private EngineState _state;
    private double _accumulator;
    private int _frameCount;

    /// <summary>
    /// Gets the current state of the engine.
    /// </summary>
    public EngineState State => _state;

    /// <summary>
    /// Gets the engine configuration.
    /// </summary>
    public EngineConfiguration Configuration => _configuration;

    /// <summary>
    /// Gets the game time information.
    /// </summary>
    public GameTime Time => _gameTime;

    /// <summary>
    /// Gets the scene manager.
    /// </summary>
    public SceneManager SceneManager => _sceneManager;

    /// <summary>
    /// Gets or sets whether the engine should exit.
    /// </summary>
    public bool ShouldExit { get; set; }

    /// <summary>
    /// Gets the memory profiler, if enabled.
    /// </summary>
    public MemoryProfiler? MemoryProfiler => _memoryProfiler;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameEngine"/> class.
    /// </summary>
    /// <param name="configuration">Engine configuration.</param>
    /// <param name="window">The window manager.</param>
    /// <param name="renderer">The 2D renderer.</param>
    /// <param name="inputBackend">The input backend.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="transitionEffect">Optional scene transition effect.</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    public GameEngine(
        EngineConfiguration configuration,
        IWindow window,
        IRenderer2D renderer,
        IInputBackend inputBackend,
        ILogger logger,
        ISceneTransitionEffect? transitionEffect = null)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _inputBackend = inputBackend ?? throw new ArgumentNullException(nameof(inputBackend));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _configuration.Validate();

        _gameTime = new GameTime(_configuration.FixedTimeStep);
        _timer = new PrecisionTimer();
        _sceneManager = new SceneManager(transitionEffect);

        // Initialize memory profiler if enabled
        if (_configuration.EnableMemoryProfiling)
        {
            _memoryProfiler = new MemoryProfiler { MaxSnapshots = 500 };
            _logger.Info(LOG_CATEGORY, $"Memory profiling enabled (snapshot every {_configuration.MemorySnapshotInterval} frames)");
        }

        _state = EngineState.NotInitialized;
        _accumulator = 0.0;
        _frameCount = 0;
        ShouldExit = false;

        _logger.Info(LOG_CATEGORY, "Engine instance created");
    }

    /// <summary>
    /// Initializes the engine with a scene context.
    /// </summary>
    /// <param name="sceneContext">The scene context containing all engine services.</param>
    /// <exception cref="ArgumentNullException">Thrown when sceneContext is null.</exception>
    public void Initialize(SceneContext sceneContext)
    {
        if (sceneContext == null)
        {
            throw new ArgumentNullException(nameof(sceneContext));
        }

        if (_state != EngineState.NotInitialized)
        {
            _logger.Warn(LOG_CATEGORY, $"Initialize called but engine is in state: {_state}");
            return;
        }

        _state = EngineState.Initializing;
        _logger.Info(LOG_CATEGORY, "Initializing engine...");

        _sceneManager.Initialize(sceneContext);

        _state = EngineState.Running;
        _logger.Info(LOG_CATEGORY, "Engine initialized successfully");
    }

    /// <summary>
    /// Runs the main game loop.
    /// This method blocks until the engine exits.
    /// </summary>
    public void Run()
    {
        if (_state != EngineState.Running)
        {
            _logger.Error(LOG_CATEGORY, $"Cannot run engine in state: {_state}");
            return;
        }

        _logger.Info(LOG_CATEGORY, "Starting main loop");
        _timer.Restart();

        while (!ShouldExit && !_window.ShouldClose && _state == EngineState.Running)
        {
            ProcessFrame();
        }

        _logger.Info(LOG_CATEGORY, "Main loop ended");
    }

    /// <summary>
    /// Processes a single frame of the game loop.
    /// Implements fixed timestep for updates and variable render rate.
    /// </summary>
    private void ProcessFrame()
    {
        // Update input state
        _inputBackend.Update();

        float deltaTime = _timer.GetDeltaTime();
        _accumulator += deltaTime;

        int fixedUpdateCount = 0;
        float fixedDelta = _configuration.FixedTimeStep;

        // Fixed timestep updates (physics, deterministic logic)
        while (_accumulator >= fixedDelta && fixedUpdateCount < _configuration.MaxFixedUpdatesPerFrame)
        {
            _gameTime.Update(fixedDelta);
            FixedUpdate(fixedDelta);

            _accumulator -= fixedDelta;
            fixedUpdateCount++;
        }

        // Prevent spiral of death
        if (fixedUpdateCount >= _configuration.MaxFixedUpdatesPerFrame)
        {
            _logger.Warn(LOG_CATEGORY, $"Frame took too long, skipped {_accumulator / fixedDelta:F2} fixed updates");
            _accumulator = 0.0;
        }

        // Variable timestep update (input, animations, non-deterministic logic)
        Update(deltaTime);

        // Render (uncapped or V-synced)
        _renderer.BeginFrame();
        Render();
        _renderer.EndFrame();

        // Memory profiling (capture snapshot at configured interval)
        _frameCount++;
        if (_memoryProfiler != null && _frameCount % _configuration.MemorySnapshotInterval == 0)
        {
            _memoryProfiler.CaptureSnapshot(forceGC: false);
        }
    }

    /// <summary>
    /// Fixed timestep update for deterministic logic (physics, game logic).
    /// </summary>
    /// <param name="fixedDeltaTime">The fixed delta time.</param>
    private void FixedUpdate(float fixedDeltaTime)
    {
        _sceneManager.FixedUpdate(fixedDeltaTime);
    }

    /// <summary>
    /// Variable timestep update for non-deterministic logic (input, animations).
    /// </summary>
    /// <param name="deltaTime">The frame delta time.</param>
    private void Update(float deltaTime)
    {
        _sceneManager.Update(deltaTime);
    }

    /// <summary>
    /// Renders the current frame.
    /// </summary>
    private void Render()
    {
        _sceneManager.Render();
    }

    /// <summary>
    /// Pauses the engine.
    /// </summary>
    public void Pause()
    {
        if (_state == EngineState.Running)
        {
            _state = EngineState.Paused;
            _logger.Info(LOG_CATEGORY, "Engine paused");
        }
    }

    /// <summary>
    /// Resumes the engine from paused state.
    /// </summary>
    public void Resume()
    {
        if (_state == EngineState.Paused)
        {
            _state = EngineState.Running;
            _logger.Info(LOG_CATEGORY, "Engine resumed");
        }
    }

    /// <summary>
    /// Shuts down the engine and cleans up resources.
    /// </summary>
    public void Shutdown()
    {
        if (_state == EngineState.Stopped || _state == EngineState.ShuttingDown)
        {
            return;
        }

        _state = EngineState.ShuttingDown;
        _logger.Info(LOG_CATEGORY, "Shutting down engine...");

        _sceneManager.Shutdown();
        _timer.Stop();

        _state = EngineState.Stopped;
        _logger.Info(LOG_CATEGORY, "Engine stopped");
    }
}

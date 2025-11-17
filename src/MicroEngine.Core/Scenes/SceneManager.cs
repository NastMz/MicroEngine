using MicroEngine.Core.Logging;

namespace MicroEngine.Core.Scenes;

/// <summary>
/// Manages scene lifecycle, transitions, and updates.
/// </summary>
public sealed class SceneManager
{
    private const string LOG_CATEGORY = "SceneManager";

    private readonly ILogger _logger;
    private readonly Dictionary<string, IScene> _scenes;
    private IScene? _currentScene;
    private IScene? _nextScene;
    private bool _isTransitioning;

    /// <summary>
    /// Gets the currently active scene.
    /// </summary>
    public IScene? CurrentScene => _currentScene;

    /// <summary>
    /// Gets whether a scene transition is in progress.
    /// </summary>
    public bool IsTransitioning => _isTransitioning;

    /// <summary>
    /// Initializes a new instance of the <see cref="SceneManager"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public SceneManager(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _scenes = new Dictionary<string, IScene>();
        _currentScene = null;
        _nextScene = null;
        _isTransitioning = false;
    }

    /// <summary>
    /// Initializes the scene manager.
    /// </summary>
    internal void Initialize()
    {
        _logger.Info(LOG_CATEGORY, "Scene manager initialized");
    }

    /// <summary>
    /// Registers a scene with the manager.
    /// </summary>
    /// <param name="scene">The scene to register.</param>
    /// <exception cref="ArgumentNullException">Thrown when scene is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a scene with the same name already exists.</exception>
    public void RegisterScene(IScene scene)
    {
        if (scene == null)
        {
            throw new ArgumentNullException(nameof(scene));
        }

        if (_scenes.ContainsKey(scene.Name))
        {
            throw new InvalidOperationException($"Scene '{scene.Name}' is already registered");
        }

        _scenes.Add(scene.Name, scene);
        _logger.Info(LOG_CATEGORY, $"Registered scene: {scene.Name}");
    }

    /// <summary>
    /// Unregisters a scene from the manager.
    /// </summary>
    /// <param name="sceneName">The name of the scene to unregister.</param>
    /// <returns>True if the scene was unregistered, false otherwise.</returns>
    public bool UnregisterScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return false;
        }

        if (_currentScene?.Name == sceneName)
        {
            _logger.Warn(LOG_CATEGORY, $"Cannot unregister active scene: {sceneName}");
            return false;
        }

        if (_scenes.Remove(sceneName))
        {
            _logger.Info(LOG_CATEGORY, $"Unregistered scene: {sceneName}");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Loads and activates a scene by name.
    /// </summary>
    /// <param name="sceneName">The name of the scene to load.</param>
    /// <exception cref="ArgumentException">Thrown when scene name is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when scene is not registered or transition is in progress.</exception>
    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            throw new ArgumentException("Scene name cannot be null or empty", nameof(sceneName));
        }

        if (_isTransitioning)
        {
            throw new InvalidOperationException("Cannot load scene while a transition is in progress");
        }

        if (!_scenes.TryGetValue(sceneName, out IScene? scene))
        {
            throw new InvalidOperationException($"Scene '{sceneName}' is not registered");
        }

        _logger.Info(LOG_CATEGORY, $"Loading scene: {sceneName}");
        _nextScene = scene;
        _isTransitioning = true;
    }

    /// <summary>
    /// Performs the scene transition if one is pending.
    /// </summary>
    private void ProcessTransition()
    {
        if (!_isTransitioning || _nextScene == null)
        {
            return;
        }

        // Unload current scene
        if (_currentScene != null)
        {
            _logger.Info(LOG_CATEGORY, $"Unloading scene: {_currentScene.Name}");
            _currentScene.OnUnload();
        }

        // Load new scene
        _currentScene = _nextScene;
        _nextScene = null;

        _logger.Info(LOG_CATEGORY, $"Activating scene: {_currentScene.Name}");
        _currentScene.OnLoad();

        _isTransitioning = false;
    }

    /// <summary>
    /// Updates scenes with fixed timestep.
    /// </summary>
    /// <param name="fixedDeltaTime">The fixed delta time.</param>
    internal void FixedUpdate(float fixedDeltaTime)
    {
        ProcessTransition();
        _currentScene?.OnFixedUpdate(fixedDeltaTime);
    }

    /// <summary>
    /// Updates scenes with variable timestep.
    /// </summary>
    /// <param name="deltaTime">The frame delta time.</param>
    internal void Update(float deltaTime)
    {
        _currentScene?.OnUpdate(deltaTime);
    }

    /// <summary>
    /// Renders the current scene.
    /// </summary>
    internal void Render()
    {
        _currentScene?.OnRender();
    }

    /// <summary>
    /// Shuts down the scene manager and unloads all scenes.
    /// </summary>
    internal void Shutdown()
    {
        if (_currentScene != null)
        {
            _logger.Info(LOG_CATEGORY, $"Unloading current scene: {_currentScene.Name}");
            _currentScene.OnUnload();
            _currentScene = null;
        }

        _scenes.Clear();
        _logger.Info(LOG_CATEGORY, "Scene manager shut down");
    }
}

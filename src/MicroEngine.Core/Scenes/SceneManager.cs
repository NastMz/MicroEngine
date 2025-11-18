using MicroEngine.Core.Logging;

namespace MicroEngine.Core.Scenes;

/// <summary>
/// Manages scene lifecycle and navigation using a stack-based approach.
/// Supports pushing, popping, and replacing scenes with automatic lifecycle management.
/// </summary>
public sealed class SceneManager
{
    private const string LOG_CATEGORY = "SceneManager";

    private readonly Stack<Scene> _sceneStack = new();
    private readonly ILogger _logger;
    private Scene? _pendingScene;
    private SceneTransition _pendingTransition;

    /// <summary>
    /// Gets the currently active scene.
    /// </summary>
    public Scene? CurrentScene => _sceneStack.Count > 0 ? _sceneStack.Peek() : null;

    /// <summary>
    /// Gets the number of scenes in the stack.
    /// </summary>
    public int SceneCount => _sceneStack.Count;

    /// <summary>
    /// Initializes a new instance of the <see cref="SceneManager"/> class.
    /// </summary>
    /// <param name="logger">Logger for scene transitions.</param>
    public SceneManager(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initializes the scene manager.
    /// </summary>
    public void Initialize()
    {
        _logger.Info(LOG_CATEGORY, "Scene manager initialized");
    }

    /// <summary>
    /// Pushes a new scene onto the stack, pausing the current scene.
    /// The new scene becomes active and will receive updates.
    /// </summary>
    /// <param name="scene">The scene to push.</param>
    public void PushScene(Scene scene)
    {
        if (scene == null)
        {
            throw new ArgumentNullException(nameof(scene));
        }

        _pendingScene = scene;
        _pendingTransition = SceneTransition.Push;
    }

    /// <summary>
    /// Pops the current scene from the stack, returning to the previous scene.
    /// If only one scene remains, does nothing.
    /// </summary>
    public void PopScene()
    {
        if (_sceneStack.Count <= 1)
        {
            _logger.Warn(LOG_CATEGORY, "Cannot pop last scene from stack");
            return;
        }

        _pendingTransition = SceneTransition.Pop;
    }

    /// <summary>
    /// Replaces the current scene with a new scene without affecting the stack.
    /// Useful for transitions between scenes of the same "level" (e.g., menu to menu).
    /// </summary>
    /// <param name="scene">The scene to replace with.</param>
    public void ReplaceScene(Scene scene)
    {
        if (scene == null)
        {
            throw new ArgumentNullException(nameof(scene));
        }

        _pendingScene = scene;
        _pendingTransition = SceneTransition.Replace;
    }

    /// <summary>
    /// Processes any pending scene transitions.
    /// </summary>
    private void ProcessPendingTransitions()
    {
        if (_pendingTransition == SceneTransition.None)
        {
            return;
        }

        switch (_pendingTransition)
        {
            case SceneTransition.Push:
                ProcessPushTransition();
                break;

            case SceneTransition.Pop:
                ProcessPopTransition();
                break;

            case SceneTransition.Replace:
                ProcessReplaceTransition();
                break;
        }

        _pendingTransition = SceneTransition.None;
    }

    private void ProcessPushTransition()
    {
        if (_pendingScene == null)
        {
            return;
        }

        _logger.Info(LOG_CATEGORY, $"Pushing scene: {_pendingScene.Name}");

        // Current scene remains in stack but won't receive updates
        _sceneStack.Push(_pendingScene);
        _pendingScene.OnLoad();

        _pendingScene = null;
    }

    private void ProcessPopTransition()
    {
        if (_sceneStack.Count == 0)
        {
            return;
        }

        var currentScene = _sceneStack.Pop();
        _logger.Info(LOG_CATEGORY, $"Popping scene: {currentScene.Name}");
        currentScene.OnUnload();

        // Resume previous scene (if any)
        if (_sceneStack.Count > 0)
        {
            _logger.Info(LOG_CATEGORY, $"Resuming scene: {CurrentScene!.Name}");
        }
    }

    private void ProcessReplaceTransition()
    {
        if (_pendingScene == null)
        {
            return;
        }

        // Unload current scene
        if (_sceneStack.Count > 0)
        {
            var oldScene = _sceneStack.Pop();
            _logger.Info(LOG_CATEGORY, $"Replacing scene: {oldScene.Name} → {_pendingScene.Name}");
            oldScene.OnUnload();
        }
        else
        {
            _logger.Info(LOG_CATEGORY, $"Loading initial scene: {_pendingScene.Name}");
        }

        // Push new scene
        _sceneStack.Push(_pendingScene);
        _pendingScene.OnLoad();

        _pendingScene = null;
    }

    /// <summary>
    /// Updates scenes with fixed timestep.
    /// </summary>
    /// <param name="fixedDeltaTime">The fixed delta time.</param>
    public void FixedUpdate(float fixedDeltaTime)
    {
        CurrentScene?.OnFixedUpdate(fixedDeltaTime);
    }

    /// <summary>
    /// Updates scenes with variable timestep.
    /// </summary>
    /// <param name="deltaTime">The frame delta time.</param>
    public void Update(float deltaTime)
    {
        ProcessPendingTransitions();
        CurrentScene?.OnUpdate(deltaTime);
    }

    /// <summary>
    /// Renders the current scene.
    /// </summary>
    public void Render()
    {
        CurrentScene?.OnRender();
    }

    /// <summary>
    /// Shuts down the scene manager and unloads all scenes.
    /// </summary>
    public void Shutdown()
    {
        _logger.Info(LOG_CATEGORY, $"Clearing {_sceneStack.Count} scene(s) from stack");

        while (_sceneStack.Count > 0)
        {
            var scene = _sceneStack.Pop();
            _logger.Info(LOG_CATEGORY, $"Unloading scene: {scene.Name}");
            scene.OnUnload();
        }

        _pendingScene = null;
        _pendingTransition = SceneTransition.None;

        _logger.Info(LOG_CATEGORY, "Scene manager shut down");
    }

    private enum SceneTransition
    {
        None,
        Push,
        Pop,
        Replace
    }
}

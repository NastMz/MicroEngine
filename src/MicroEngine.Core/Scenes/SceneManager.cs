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
    private ISceneTransitionEffect? _transitionEffect;
    private SceneContext _sceneContext = null!;
    private ILogger _logger = null!;

    private Scene? _pendingScene;
    private SceneParameters? _pendingParameters;
    private SceneTransition _pendingTransition;
    private TransitionState _transitionState;

    /// <summary>
    /// Gets the currently active scene.
    /// </summary>
    public Scene? CurrentScene => _sceneStack.Count > 0 ? _sceneStack.Peek() : null;

    /// <summary>
    /// Gets the number of scenes in the stack.
    /// </summary>
    public int SceneCount => _sceneStack.Count;

    /// <summary>
    /// Gets a value indicating whether a transition is currently in progress.
    /// </summary>
    public bool IsTransitioning => _transitionState != TransitionState.None;

    /// <summary>
    /// Initializes a new instance of the <see cref="SceneManager"/> class.
    /// </summary>
    /// <param name="transitionEffect">Optional transition effect (e.g., fade). If null, no transitions are used.</param>
    public SceneManager(ISceneTransitionEffect? transitionEffect = null)
    {
        _transitionEffect = transitionEffect;
        _transitionState = TransitionState.None;
    }

    /// <summary>
    /// Initializes the scene manager with the scene context.
    /// </summary>
    /// <param name="context">The scene context providing access to engine services.</param>
    public void Initialize(SceneContext context)
    {
        _sceneContext = context ?? throw new ArgumentNullException(nameof(context));
        _logger = context.Logger;
        _logger.Info(LOG_CATEGORY, "Scene manager initialized");
    }

    /// <summary>
    /// Changes the transition effect used for scene changes.
    /// </summary>
    /// <param name="transitionEffect">The new transition effect to use. Can be null to disable transitions.</param>
    public void SetTransition(ISceneTransitionEffect? transitionEffect)
    {
        _transitionEffect = transitionEffect;
        _transitionEffect?.Reset();
        _logger?.Info(LOG_CATEGORY, $"Transition effect changed to: {transitionEffect?.GetType().Name ?? "None"}");
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
        _pendingParameters = null;
        _pendingTransition = SceneTransition.Push;
    }

    /// <summary>
    /// Pushes a new scene onto the stack with parameters.
    /// The new scene becomes active and will receive updates and parameters.
    /// </summary>
    /// <param name="scene">The scene to push.</param>
    /// <param name="parameters">Parameters to pass to the new scene.</param>
    public void PushScene(Scene scene, SceneParameters parameters)
    {
        if (scene == null)
        {
            throw new ArgumentNullException(nameof(scene));
        }

        _pendingScene = scene;
        _pendingParameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
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
        _pendingParameters = null;
        _pendingTransition = SceneTransition.Replace;
    }

    /// <summary>
    /// Replaces the current scene with a new scene with parameters.
    /// </summary>
    /// <param name="scene">The scene to replace with.</param>
    /// <param name="parameters">Parameters to pass to the new scene.</param>
    public void ReplaceScene(Scene scene, SceneParameters parameters)
    {
        if (scene == null)
        {
            throw new ArgumentNullException(nameof(scene));
        }

        _pendingScene = scene;
        _pendingParameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        _pendingTransition = SceneTransition.Replace;
    }

    /// <summary>
    /// Processes any pending scene transitions.
    /// </summary>
    private void ProcessPendingTransitions()
    {
        // Handle transition states
        if (_transitionState == TransitionState.FadingOut && _transitionEffect != null)
        {
            if (_transitionEffect.IsComplete)
            {
                // Fade out complete, perform actual scene change
                ExecuteSceneChange();
                
                // Start fade in
                if (_transitionEffect != null)
                {
                    _transitionEffect.Start(fadeOut: false);
                    _transitionState = TransitionState.FadingIn;
                }
                else
                {
                    _transitionState = TransitionState.None;
                }
            }
            return;
        }

        if (_transitionState == TransitionState.FadingIn && _transitionEffect != null)
        {
            if (_transitionEffect.IsComplete)
            {
                _transitionState = TransitionState.None;
            }
            return;
        }

        // Start new transition if requested
        if (_pendingTransition == SceneTransition.None)
        {
            return;
        }

        // Start fade out if transition effect is enabled
        if (_transitionEffect != null)
        {
            _transitionEffect.Start(fadeOut: true);
            _transitionState = TransitionState.FadingOut;
        }
        else
        {
            // No transition effect, execute immediately
            ExecuteSceneChange();
            _pendingTransition = SceneTransition.None;
        }
    }

    private void ExecuteSceneChange()
    {
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
        _pendingScene.SetSceneManager(this);
        
        // Call appropriate OnLoad overload based on whether parameters were provided
        if (_pendingParameters != null)
        {
            _pendingScene.OnLoad(_sceneContext, _pendingParameters);
            _pendingParameters = null;
        }
        else
        {
            _pendingScene.OnLoad(_sceneContext);
        }

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
        _pendingScene.SetSceneManager(this);
        
        // Call appropriate OnLoad overload based on whether parameters were provided
        if (_pendingParameters != null)
        {
            _pendingScene.OnLoad(_sceneContext, _pendingParameters);
            _pendingParameters = null;
        }
        else
        {
            _pendingScene.OnLoad(_sceneContext);
        }

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
        
        // Update transition effect if active
        if (_transitionState != TransitionState.None && _transitionEffect != null)
        {
            _transitionEffect.Update(deltaTime);
        }
        
        // Only update current scene when not transitioning (or fade in is happening)
        if (_transitionState == TransitionState.None || _transitionState == TransitionState.FadingIn)
        {
            CurrentScene?.OnUpdate(deltaTime);
        }
    }

    /// <summary>
    /// Renders the current scene.
    /// </summary>
    public void Render()
    {
        CurrentScene?.OnRender();
        
        // Render transition overlay on top
        if (_transitionState != TransitionState.None && _transitionEffect != null)
        {
            _transitionEffect.Render();
        }
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

    private enum TransitionState
    {
        None,
        FadingOut,
        FadingIn
    }
}

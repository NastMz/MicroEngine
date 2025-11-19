using MicroEngine.Core;
using MicroEngine.Core.Engine;
using MicroEngine.Core.Graphics;
using MicroEngine.Core.Input;
using MicroEngine.Core.Logging;
using MicroEngine.Core.Math;
using MicroEngine.Core.Scenes;
using MicroEngine.Game.Scenes.Demos;

namespace MicroEngine.Game.Scenes;

/// <summary>
/// Main menu scene that provides navigation to all demo scenes.
/// Supports multiple transition effects for scene changes.
/// </summary>
public sealed class MainMenuScene : Scene
{
    private IInputBackend _inputBackend = null!;
    private IRenderBackend2D _renderBackend = null!;
    private ILogger _logger = null!;
    private readonly SceneManager _sceneManager;
    private readonly SceneCache _sceneCache;
    private readonly FadeTransition _fadeTransition;
    private readonly SlideTransition _slideTransition;
    private readonly WipeTransition _wipeTransition;
    private readonly ZoomTransition _zoomTransition;

    private const int MENU_X = 250;
    private const int MENU_Y = 80;
    private const int LINE_HEIGHT = 28;

    private string _currentTransition = "Fade";
    private string _lastCacheInfo = "Cache: No activity yet";
    private int _preloadedScenes;
    private bool _isPreloading;
    private CancellationTokenSource? _preloadCancellation;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainMenuScene"/> class.
    /// </summary>
    public MainMenuScene(
        SceneManager sceneManager,
        SceneCache sceneCache,
        FadeTransition fadeTransition,
        SlideTransition slideTransition,
        WipeTransition wipeTransition,
        ZoomTransition zoomTransition)
        : base("MainMenu")
    {
        _sceneManager = sceneManager;
        _sceneCache = sceneCache;
        _fadeTransition = fadeTransition;
        _slideTransition = slideTransition;
        _wipeTransition = wipeTransition;
        _zoomTransition = zoomTransition;
    }

    /// <inheritdoc/>
    public override void OnLoad(SceneContext context)
    {
        base.OnLoad(context);
        _inputBackend = context.InputBackend;
        _renderBackend = context.RenderBackend;
        _logger = context.Logger;
        _logger.Info("MainMenu", "Main menu loaded");

        // Subscribe to preload events
        _sceneCache.ScenePreloaded += OnScenePreloaded;

        // Start background preloading of demo scenes
        _ = PreloadDemoScenesAsync();
    }

    /// <inheritdoc/>
    public override void OnUnload()
    {
        // Unsubscribe from events
        _sceneCache.ScenePreloaded -= OnScenePreloaded;

        // Cancel any ongoing preloads
        _preloadCancellation?.Cancel();
        _preloadCancellation?.Dispose();
        _preloadCancellation = null;

        _logger.Info("MainMenu", "Main menu unloaded");
        
        base.OnUnload();
    }

    private async Task PreloadDemoScenesAsync()
    {
        _preloadCancellation = new CancellationTokenSource();
        _isPreloading = true;
        _preloadedScenes = 0;

        try
        {
            _logger.Info("MainMenu", "Starting background preload of demo scenes...");

            var preloadRequests = new[]
            {
                ("EcsBasicsDemo", (Func<IScene>)(() => new EcsBasicsDemo())),
                ("GraphicsDemo", (Func<IScene>)(() => new GraphicsDemo())),
                ("PhysicsDemo", (Func<IScene>)(() => new PhysicsDemo())),
                ("InputDemo", (Func<IScene>)(() => new InputDemo())),
                ("TilemapDemo", (Func<IScene>)(() => new TilemapDemo())),
                ("AudioDemo", (Func<IScene>)(() => new AudioDemo()))
            };

            await _sceneCache.PreloadMultipleAsync(preloadRequests, _preloadCancellation.Token);

            _logger.Info("MainMenu", $"Background preload completed: {_preloadedScenes} scenes ready");
        }
        catch (OperationCanceledException)
        {
            _logger.Info("MainMenu", "Scene preloading cancelled");
        }
        catch (Exception ex)
        {
            _logger.Error("MainMenu", $"Error during scene preloading: {ex.Message}");
        }
        finally
        {
            _isPreloading = false;
        }
    }

    private void OnScenePreloaded(object? sender, ScenePreloadedEventArgs e)
    {
        if (e.Success)
        {
            _preloadedScenes++;
            _logger.Debug("MainMenu", $"Preloaded: {e.SceneKey} ({_preloadedScenes}/6)");
        }
        else
        {
            _logger.Warn("MainMenu", $"Failed to preload {e.SceneKey}: {e.Exception?.Message}");
        }
    }

    /// <inheritdoc/>
    public override void OnUpdate(float deltaTime)
    {
        // Demo selection
        if (_inputBackend.IsKeyPressed(Key.One))
        {
            LoadDemo<EcsBasicsDemo>();
        }
        else if (_inputBackend.IsKeyPressed(Key.Two))
        {
            LoadDemo<GraphicsDemo>();
        }
        else if (_inputBackend.IsKeyPressed(Key.Three))
        {
            LoadDemo<PhysicsDemo>();
        }
        else if (_inputBackend.IsKeyPressed(Key.Four))
        {
            // Demonstrate scene parameter passing with caching
            var parameters = SceneParameters.Create()
                .Add("fromMenu", true)
                .Add("welcomeMessage", "Welcome from Main Menu!")
                .Build();
            
            LoadDemo<InputDemo>(parameters);
        }
        else if (_inputBackend.IsKeyPressed(Key.Five))
        {
            LoadDemo<TilemapDemo>();
        }
        else if (_inputBackend.IsKeyPressed(Key.Six))
        {
            LoadDemo<AudioDemo>();
        }

        // Transition effect selection
        if (_inputBackend.IsKeyPressed(Key.F6))
        {
            _currentTransition = "Fade";
            _sceneManager.SetTransition(_fadeTransition);
            _logger.Info("MainMenu", "Transition changed to: Fade");
        }
        else if (_inputBackend.IsKeyPressed(Key.F7))
        {
            _currentTransition = "Slide";
            _sceneManager.SetTransition(_slideTransition);
            _logger.Info("MainMenu", "Transition changed to: Slide");
        }
        else if (_inputBackend.IsKeyPressed(Key.F8))
        {
            _currentTransition = "Wipe";
            _sceneManager.SetTransition(_wipeTransition);
            _logger.Info("MainMenu", "Transition changed to: Wipe");
        }
        else if (_inputBackend.IsKeyPressed(Key.F9))
        {
            _currentTransition = "Zoom";
            _sceneManager.SetTransition(_zoomTransition);
            _logger.Info("MainMenu", "Transition changed to: Zoom");
        }
    }

    /// <inheritdoc/>
    public override void OnRender()
    {
        _renderBackend.Clear(new Color(20, 20, 30, 255));

        var titlePos = new Vector2(MENU_X, MENU_Y);
        _renderBackend.DrawText(EngineInfo.FullName, titlePos, 24, Color.White);

        var subtitlePos = new Vector2(MENU_X, MENU_Y + 30);
        _renderBackend.DrawText("Demo Showcase", subtitlePos, 20, new Color(180, 180, 180, 255));

        var separatorPos = new Vector2(MENU_X - 50, MENU_Y + 70);
        _renderBackend.DrawText("═══════════════════════════════════════", separatorPos, 16, new Color(100, 100, 100, 255));

        // Scene cache status - Top right corner
        var cacheX = 600;
        var cacheY = 20;
        _renderBackend.DrawText("Scene Cache", new Vector2(cacheX, cacheY), 14, new Color(200, 200, 200, 255));
        
        cacheY += 22;
        _renderBackend.DrawText(_lastCacheInfo, new Vector2(cacheX, cacheY), 11, new Color(255, 200, 100, 255));
        
        cacheY += 18;
        var statusColor = _isPreloading ? new Color(100, 255, 100, 255) : new Color(150, 150, 150, 255);
        var statusText = _isPreloading ? $"Preloading... {_preloadedScenes}/6" : $"Stored: {_sceneCache.Count}/{_sceneCache.MaxCacheSize}";
        _renderBackend.DrawText(statusText, new Vector2(cacheX, cacheY), 11, statusColor);
        
        if (_sceneCache.Count > 0)
        {
            cacheY += 18;
            var cachedKeys = string.Join(", ", _sceneCache.GetCachedKeys());
            _renderBackend.DrawText($"[{cachedKeys}]", new Vector2(cacheX, cacheY), 9, new Color(100, 150, 200, 255));
        }

        var optionY = MENU_Y + 110;
        _renderBackend.DrawText("Select a demo:", new Vector2(MENU_X, optionY), 18, new Color(200, 200, 200, 255));

        optionY += LINE_HEIGHT + 10;
        _renderBackend.DrawText("[1] ECS Basics", new Vector2(MENU_X + 20, optionY), 16, new Color(100, 200, 255, 255));

        optionY += LINE_HEIGHT;
        _renderBackend.DrawText("[2] Graphics & Camera", new Vector2(MENU_X + 20, optionY), 16, new Color(100, 200, 255, 255));

        optionY += LINE_HEIGHT;
        _renderBackend.DrawText("[3] Physics & Collisions", new Vector2(MENU_X + 20, optionY), 16, new Color(100, 200, 255, 255));

        optionY += LINE_HEIGHT;
        _renderBackend.DrawText("[4] Input Mapping", new Vector2(MENU_X + 20, optionY), 16, new Color(100, 200, 255, 255));

        optionY += LINE_HEIGHT;
        _renderBackend.DrawText("[5] Tilemap System", new Vector2(MENU_X + 20, optionY), 16, new Color(100, 200, 255, 255));

        optionY += LINE_HEIGHT;
        _renderBackend.DrawText("[6] Audio System", new Vector2(MENU_X + 20, optionY), 16, new Color(100, 200, 255, 255));

        optionY += LINE_HEIGHT + 10;
        _renderBackend.DrawText("[X] Exit (close window)", new Vector2(MENU_X + 20, optionY), 16, new Color(255, 100, 100, 255));

        var bottomSeparatorPos = new Vector2(MENU_X - 50, optionY + 40);
        _renderBackend.DrawText("═══════════════════════════════════════", bottomSeparatorPos, 16, new Color(100, 100, 100, 255));

        // Transition effect selection
        var transitionY = optionY + 80;
        _renderBackend.DrawText("Scene Transitions:", new Vector2(MENU_X, transitionY), 18, new Color(200, 200, 200, 255));

        transitionY += LINE_HEIGHT + 5;
        var fadeColor = _currentTransition == "Fade" ? new Color(100, 255, 100, 255) : new Color(150, 150, 150, 255);
        _renderBackend.DrawText($"[F6] Fade {(_currentTransition == "Fade" ? "✓" : "")}", new Vector2(MENU_X + 20, transitionY), 14, fadeColor);

        transitionY += 25;
        var slideColor = _currentTransition == "Slide" ? new Color(100, 255, 100, 255) : new Color(150, 150, 150, 255);
        _renderBackend.DrawText($"[F7] Slide {(_currentTransition == "Slide" ? "✓" : "")}", new Vector2(MENU_X + 20, transitionY), 14, slideColor);

        transitionY += 25;
        var wipeColor = _currentTransition == "Wipe" ? new Color(100, 255, 100, 255) : new Color(150, 150, 150, 255);
        _renderBackend.DrawText($"[F8] Wipe {(_currentTransition == "Wipe" ? "✓" : "")}", new Vector2(MENU_X + 20, transitionY), 14, wipeColor);

        transitionY += 25;
        var zoomColor = _currentTransition == "Zoom" ? new Color(100, 255, 100, 255) : new Color(150, 150, 150, 255);
        _renderBackend.DrawText($"[F9] Zoom {(_currentTransition == "Zoom" ? "✓" : "")}", new Vector2(MENU_X + 20, transitionY), 14, zoomColor);
    }

    private void LoadDemo<T>(SceneParameters? parameters = null) where T : Scene, new()
    {
        var sceneName = typeof(T).Name;
        var wasInCache = _sceneCache.Contains(sceneName);
        
        var demo = _sceneCache.GetOrCreate(sceneName, () => new T());
        
        _lastCacheInfo = wasInCache 
            ? $"Cache HIT: {sceneName} (reused)"
            : $"Cache MISS: {sceneName} (new)";
        
        if (parameters != null)
        {
            PushScene(demo, parameters);
        }
        else
        {
            PushScene(demo);
        }
        
        _logger.Info("MainMenu", $"Loading {sceneName} - {_lastCacheInfo}");
    }
}

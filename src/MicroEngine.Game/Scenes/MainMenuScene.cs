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
        };            await _sceneCache.PreloadMultipleAsync(preloadRequests, _preloadCancellation.Token);

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

        // Center-aligned title section
        var centerX = 425f; // Screen width / 2
        var layout = new TextLayoutHelper(_renderBackend, startX: centerX - 150, startY: 40, defaultLineHeight: 30);
        
        var titleColor = Color.White;
        var subtitleColor = new Color(180, 200, 220, 255);
        var separatorColor = new Color(80, 90, 110, 255);
        var sectionColor = new Color(200, 220, 240, 255);
        var optionColor = new Color(120, 180, 255, 255);
        var exitColor = new Color(255, 120, 120, 255);
        var dimColor = new Color(160, 160, 160, 255);
        var cacheHeaderColor = new Color(180, 200, 220, 255);

        // Title (centered)
        _renderBackend.DrawText(EngineInfo.FullName, new Vector2(centerX - 140, 40), 26, titleColor);
        _renderBackend.DrawText("Demo Showcase", new Vector2(centerX - 80, 75), 18, subtitleColor);

        // Decorative separator
        _renderBackend.DrawText("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", new Vector2(150, 110), 14, separatorColor);

        // Scene cache status - Compact top-right corner
        var cacheLayout = new TextLayoutHelper(_renderBackend, startX: 620, startY: 15, defaultLineHeight: 16);
        cacheLayout.DrawText("Scene Cache", 12, cacheHeaderColor)
                   .AddSpacing(2);
        
        var statusColor = _isPreloading ? new Color(100, 255, 150, 255) : new Color(140, 160, 180, 255);
        var statusText = _isPreloading ? $"Loading {_preloadedScenes}/6..." : $"{_sceneCache.Count}/{_sceneCache.MaxCacheSize} cached";
        cacheLayout.DrawText(statusText, 10, statusColor);
        
        if (_sceneCache.Count > 0 && !_isPreloading)
        {
            cacheLayout.DrawText(_lastCacheInfo, 9, new Color(255, 200, 100, 255));
        }

        // Demo selection section
        layout.SetX(280)
              .SetY(145)
              .DrawText("Available Demos", 20, sectionColor)
              .AddSpacing(15)
              .SetX(300);

        // Demo options with better spacing
        layout.DrawText("[1] ECS Basics", 16, optionColor, customLineHeight: 26)
              .DrawText("[2] Graphics & Camera", 16, optionColor, customLineHeight: 26)
              .DrawText("[3] Physics & Collisions", 16, optionColor, customLineHeight: 26)
              .DrawText("[4] Input Mapping", 16, optionColor, customLineHeight: 26)
              .DrawText("[5] Tilemap System", 16, optionColor, customLineHeight: 26)
              .DrawText("[6] Audio System", 16, optionColor, customLineHeight: 26)
              .AddSpacing(15)
              .DrawText("[X] Exit", 16, exitColor);

        // Decorative separator
        _renderBackend.DrawText("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", new Vector2(150, 425), 14, separatorColor);

        // Transition effects section
        layout.SetX(280)
              .SetY(455)
              .DrawText("Scene Transitions", 20, sectionColor)
              .AddSpacing(15);

        // Transition options in a grid layout
        var transY = layout.CurrentY;
        var col1X = 300f;
        var col2X = 480f;
        
        var fadeColor = _currentTransition == "Fade" ? new Color(100, 255, 150, 255) : dimColor;
        var slideColor = _currentTransition == "Slide" ? new Color(100, 255, 150, 255) : dimColor;
        var wipeColor = _currentTransition == "Wipe" ? new Color(100, 255, 150, 255) : dimColor;
        var zoomColor = _currentTransition == "Zoom" ? new Color(100, 255, 150, 255) : dimColor;

        _renderBackend.DrawText($"[F6] Fade {(_currentTransition == "Fade" ? "●" : "○")}", new Vector2(col1X, transY), 14, fadeColor);
        _renderBackend.DrawText($"[F7] Slide {(_currentTransition == "Slide" ? "●" : "○")}", new Vector2(col2X, transY), 14, slideColor);
        
        _renderBackend.DrawText($"[F8] Wipe {(_currentTransition == "Wipe" ? "●" : "○")}", new Vector2(col1X, transY + 25), 14, wipeColor);
        _renderBackend.DrawText($"[F9] Zoom {(_currentTransition == "Zoom" ? "●" : "○")}", new Vector2(col2X, transY + 25), 14, zoomColor);
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

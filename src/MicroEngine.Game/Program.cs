using MicroEngine.Backend.Raylib;
using MicroEngine.Backend.Raylib.Resources;
using MicroEngine.Core.Logging;
using MicroEngine.Core.Resources;
using MicroEngine.Core.Scenes;
using MicroEngine.Core.State;
using MicroEngine.Game.Scenes;

namespace MicroEngine.Game;

internal static class Program
{
    private static SceneManager? _sceneManager;
    private static Core.Input.IInputBackend? _inputBackend;
    private static Core.Graphics.IRenderBackend2D? _renderBackend;
    private static Core.Time.ITimeService? _timeService;
    private static ILogger? _logger;
    private static ResourceCache<ITexture>? _textureCache;

    private static void Main(string[] args)
    {
        _logger = new ConsoleLogger(LogLevel.Info);
        _logger.Info("Game", "MicroEngine Demo Showcase Starting...");

        _renderBackend = new RaylibRenderBackend();
        _inputBackend = new RaylibInputBackend();
        _timeService = new Core.Time.TimeService(targetFPS: 60);

        // Configure MSAA before window initialization
        _renderBackend.AntiAliasing = Core.Graphics.AntiAliasingMode.MSAA4X;

        // Initialize texture resource cache
        var textureLoader = new RaylibTextureLoader();
        _textureCache = new ResourceCache<ITexture>(textureLoader, _logger);

        _renderBackend.Initialize(800, 600, "MicroEngine - Demo Showcase v0.4.9");

        try
        {
            // Create all transition effects
            var fadeTransition = new FadeTransition(_renderBackend, duration: 0.25f);
            var slideTransition = new SlideTransition(_renderBackend, SlideDirection.Left, duration: 0.5f);
            var wipeTransition = new WipeTransition(_renderBackend, WipeDirection.LeftToRight, duration: 0.4f);
            var zoomTransition = new ZoomTransition(_renderBackend, ZoomMode.ZoomOut, duration: 0.5f);
            
            // Create SceneManager first (it will receive context after creation)
            _sceneManager = new SceneManager(fadeTransition);
            
            // Create global game state for persistent data
            var gameState = new GameState();
            
            // Create scene context with all engine services (no SceneManager to avoid circular dependency)
            var sceneContext = new SceneContext(
                _renderBackend,
                _inputBackend,
                _timeService,
                _logger,
                _textureCache,
                gameState
            );

            // Initialize SceneManager with context
            _sceneManager.Initialize(sceneContext);

            // Load initial scene (MainMenu) with access to SceneManager and transitions
            var mainMenu = new MainMenuScene(
                _sceneManager,
                fadeTransition,
                slideTransition,
                wipeTransition,
                zoomTransition
            );
            _sceneManager.ReplaceScene(mainMenu);

            _logger.Info("Game", "Main menu running... Press 1-5 to select demo, ESC to exit");

            while (!_renderBackend.ShouldClose)
            {
                _timeService.Update();
                var deltaTime = _timeService.DeltaTime;

                _inputBackend.Update();
                _sceneManager.Update(deltaTime);

                _renderBackend.BeginFrame();
                _sceneManager.Render();
                _renderBackend.EndFrame();
            }

            _sceneManager.Shutdown();
            _logger.Info("Game", "Demo showcase shut down successfully");
        }
        catch (Exception ex)
        {
            _logger.Fatal("Game", "Fatal error occurred", ex);
        }
        finally
        {
            _renderBackend.Shutdown();
        }

        _logger.Info("Game", "Goodbye!");
    }
}





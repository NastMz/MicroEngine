using MicroEngine.Backend.Raylib;
using MicroEngine.Backend.Raylib.Resources;
using MicroEngine.Core.Logging;
using MicroEngine.Core.Resources;
using MicroEngine.Core.Scenes;
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
            // Create fade transition effect
            var fadeTransition = new FadeTransition(_renderBackend, duration: 0.25f);
            
            // Create SceneManager first (it will receive context after creation)
            _sceneManager = new SceneManager(fadeTransition);
            
            // Create scene context with all engine services including SceneManager
            var sceneContext = new SceneContext(
                _renderBackend,
                _inputBackend,
                _timeService,
                _logger,
                _textureCache,
                _sceneManager
            );

            // Initialize SceneManager with context
            _sceneManager.Initialize(sceneContext);

            // Load initial scene (MainMenu)
            var mainMenu = new MainMenuScene();
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





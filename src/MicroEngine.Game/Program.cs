using MicroEngine.Backend.Raylib;
using MicroEngine.Core.Logging;
using MicroEngine.Core.Scenes;
using MicroEngine.Game.Scenes;

namespace MicroEngine.Game;

internal static class Program
{
    private static SceneManager? _sceneManager;
    private static Core.Input.IInputBackend? _inputBackend;
    private static Core.Graphics.IRenderBackend? _renderBackend;
    private static ILogger? _logger;

    public static SceneManager SceneManager => _sceneManager ?? throw new InvalidOperationException("SceneManager not initialized");
    public static Core.Input.IInputBackend InputBackend => _inputBackend ?? throw new InvalidOperationException("InputBackend not initialized");
    public static Core.Graphics.IRenderBackend RenderBackend => _renderBackend ?? throw new InvalidOperationException("RenderBackend not initialized");
    public static ILogger Logger => _logger ?? throw new InvalidOperationException("Logger not initialized");

    private static void Main(string[] args)
    {
        _logger = new ConsoleLogger(LogLevel.Info);
        _logger.Info("Game", "MicroEngine Demo Showcase Starting...");

        _renderBackend = new RaylibRenderBackend();
        _inputBackend = new RaylibInputBackend();

        _renderBackend.Initialize(800, 600, "MicroEngine - Demo Showcase v0.4.9");
        _renderBackend.SetTargetFPS(60);

        try
        {
            _sceneManager = new SceneManager(_logger);
            _sceneManager.Initialize();

            // Load initial scene (MainMenu)
            var mainMenu = new MainMenuScene();
            _sceneManager.ReplaceScene(mainMenu);

            _logger.Info("Game", "Main menu running... Press 1-5 to select demo, ESC to exit");

            while (!_renderBackend.ShouldClose)
            {
                var deltaTime = _renderBackend.GetDeltaTime();

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





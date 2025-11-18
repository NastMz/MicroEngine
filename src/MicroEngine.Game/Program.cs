using MicroEngine.Backend.Raylib;
using MicroEngine.Core.Logging;
using MicroEngine.Game.Scenes;

namespace MicroEngine.Game;

internal static class Program
{
    private static void Main(string[] args)
    {
        // Create logger with Info level
        var logger = new ConsoleLogger(LogLevel.Info);
        logger.Info("Game", "MicroEngine Resource Demo Starting...");

        // Create Raylib backends
        var renderBackend = new RaylibRenderBackend();
        var inputBackend = new RaylibInputBackend();
        var audioBackend = new RaylibAudioBackend();

        // Initialize backends
        renderBackend.Initialize(1280, 720, "MicroEngine Resource Demo");
        renderBackend.SetTargetFPS(60);
        audioBackend.Initialize();

        try
        {
            // Create resource demo scene
            var resourceDemo = new ResourceDemoScene(logger, renderBackend, inputBackend, audioBackend);
            resourceDemo.OnLoad();

            logger.Info("Game", "Resource Demo running... Press ESC to exit");

            // Main loop
            while (!renderBackend.ShouldClose)
            {
                var deltaTime = renderBackend.GetDeltaTime();

                // Update scene
                resourceDemo.OnFixedUpdate(deltaTime);

                // Render frame
                renderBackend.BeginFrame();
                resourceDemo.OnRender();
                renderBackend.EndFrame();
            }

            resourceDemo.OnUnload();
            logger.Info("Game", "Resource Demo shut down successfully");
        }
        catch (Exception ex)
        {
            logger.Fatal("Game", "Fatal error occurred", ex);
        }
        finally
        {
            audioBackend.Shutdown();
            renderBackend.Shutdown();
        }

        logger.Info("Game", "Goodbye!");
    }
}




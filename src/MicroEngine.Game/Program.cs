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
        logger.Info("Game", "MicroEngine Sprite Batch Demo Starting...");

        // Create Raylib backends
        var renderBackend = new RaylibRenderBackend();
        var inputBackend = new RaylibInputBackend();

        // Initialize backends
        renderBackend.Initialize(1280, 720, "MicroEngine Sprite Batch Demo");
        renderBackend.SetTargetFPS(60);

        try
        {
            // Create sprite batch demo scene
            var demo = new SpriteBatchDemoScene(logger, inputBackend, renderBackend);
            demo.OnLoad();

            logger.Info("Game", "Sprite Batch Demo running... Press ESC to exit");

            // Main loop
            while (!renderBackend.ShouldClose)
            {
                var deltaTime = renderBackend.GetDeltaTime();

                // Update input
                inputBackend.Update();

                // Update scene
                demo.OnUpdate(deltaTime);

                // Render frame
                renderBackend.BeginFrame();
                demo.OnRender();
                renderBackend.EndFrame();
            }

            demo.OnUnload();
            logger.Info("Game", "Sprite Batch Demo shut down successfully");
        }
        catch (Exception ex)
        {
            logger.Fatal("Game", "Fatal error occurred", ex);
        }
        finally
        {
            renderBackend.Shutdown();
        }

        logger.Info("Game", "Goodbye!");
    }
}




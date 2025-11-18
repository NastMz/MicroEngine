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
        logger.Info("Game", "MicroEngine Camera Demo Starting...");

        // Create Raylib backends
        var renderBackend = new RaylibRenderBackend();
        var inputBackend = new RaylibInputBackend();

        // Initialize backends
        renderBackend.Initialize(1280, 720, "MicroEngine Camera Demo");
        renderBackend.SetTargetFPS(60);

        try
        {
            // Create camera demo scene
            var cameraDemo = new CameraDemoScene(logger, inputBackend, renderBackend);
            cameraDemo.OnLoad();

            logger.Info("Game", "Camera Demo running... Press ESC to exit");

            // Main loop
            while (!renderBackend.ShouldClose)
            {
                var deltaTime = renderBackend.GetDeltaTime();

                // Update input
                inputBackend.Update();

                // Update scene
                cameraDemo.OnUpdate(deltaTime);

                // Render frame
                renderBackend.BeginFrame();
                cameraDemo.OnRender();
                renderBackend.EndFrame();
            }

            cameraDemo.OnUnload();
            logger.Info("Game", "Camera Demo shut down successfully");
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




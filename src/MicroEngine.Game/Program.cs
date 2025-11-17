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
        logger.Info("Game", "MicroEngine Visual Demo Starting...");

        // Create Raylib backends
        var renderBackend = new RaylibRenderBackend();
        var inputBackend = new RaylibInputBackend();

        // Initialize window
        renderBackend.Initialize(1280, 720, "MicroEngine Visual Demo");
        renderBackend.SetTargetFPS(60);

        try
        {
            // Create visual demo scene
            var visualScene = new VisualDemoScene(logger, renderBackend, inputBackend);
            visualScene.OnLoad();

            logger.Info("Game", "Visual Demo running... Press ESC to exit");

            // Main loop
            while (!renderBackend.ShouldClose)
            {
                var deltaTime = renderBackend.GetDeltaTime();

                // Update scene (ECS systems update here via World.Update in OnFixedUpdate)
                visualScene.OnFixedUpdate(deltaTime);

                // Render frame
                renderBackend.BeginFrame();
                visualScene.OnRender();
                renderBackend.EndFrame();
            }

            visualScene.OnUnload();
            logger.Info("Game", "Visual Demo shut down successfully");
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




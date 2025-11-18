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
        logger.Info("Game", "MicroEngine Component Helpers Demo Starting...");

        // Create Raylib backends
        var renderBackend = new RaylibRenderBackend();
        var inputBackend = new RaylibInputBackend();

        // Initialize backends
        renderBackend.Initialize(800, 600, "MicroEngine - Component Helpers Demo");
        renderBackend.SetTargetFPS(60);

        try
        {
            // Create component helpers demo scene
            var demo = new ComponentHelpersDemoScene(inputBackend, renderBackend, logger);
            demo.OnLoad();

            logger.Info("Game", "Component Helpers Demo running... Press ESC to exit");

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
            logger.Info("Game", "Component Helpers Demo shut down successfully");
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




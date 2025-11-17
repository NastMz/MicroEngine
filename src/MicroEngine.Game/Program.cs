using MicroEngine.Core.Engine;
using MicroEngine.Core.Logging;
using MicroEngine.Game.Scenes;

namespace MicroEngine.Game;

internal static class Program
{
    private static void Main(string[] args)
    {
        // Create logger
        var logger = new ConsoleLogger(LogLevel.Info);
        logger.Info("Game", "MicroEngine Demo Starting...");

        // Create engine configuration
        var config = new EngineConfiguration
        {
            TargetFPS = 60,
            FixedTimeStep = 1f / 60f,
            WindowTitle = "MicroEngine Demo",
            WindowWidth = 1280,
            WindowHeight = 720
        };

        try
        {
            // Create and initialize engine
            var engine = new GameEngine(config, logger);
            engine.Initialize();

            // Create and register demo scene
            var demoScene = new DemoScene(logger);
            engine.SceneManager.RegisterScene(demoScene);
            engine.SceneManager.LoadScene("DemoScene");

            logger.Info("Game", "Engine running... (will run for 3 seconds)");

            // Set up auto-exit after 3 seconds
            var exitTimer = new System.Timers.Timer(3000);
            exitTimer.Elapsed += (sender, e) =>
            {
                engine.ShouldExit = true;
                exitTimer.Stop();
            };
            exitTimer.Start();

            // Run the engine (this blocks until ShouldExit becomes true)
            engine.Run();

            engine.Shutdown();

            logger.Info("Game", "Engine shut down successfully");
        }
        catch (Exception ex)
        {
            logger.Fatal("Game", "Fatal error occurred", ex);
            return;
        }

        logger.Info("Game", "Goodbye!");
    }
}




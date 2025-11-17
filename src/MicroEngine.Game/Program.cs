using MicroEngine.Core.Engine;
using MicroEngine.Core.Logging;
using MicroEngine.Game.Scenes;

namespace MicroEngine.Game;

internal static class Program
{
    private static void Main(string[] args)
    {
        // Create logger with Debug level to see ECS systems
        var logger = new ConsoleLogger(LogLevel.Debug);
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

            // Create and load ECS demo scene
            var demoScene = new EcsDemoScene(logger);
            engine.SceneManager.RegisterScene(demoScene);
            engine.SceneManager.LoadScene("ECS Demo");

            logger.Info("Game", "Engine running... (will run for 5 seconds)");

            // Set up auto-exit after 5 seconds
            var exitTimer = new System.Timers.Timer(5000);
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




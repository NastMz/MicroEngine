using MicroEngine.Backend.Raylib;
using MicroEngine.Core.Logging;
using MicroEngine.Core.Scenes;
using MicroEngine.Game.Scenes;
using MicroEngine.Game.Scenes.Demos;

namespace MicroEngine.Game;

internal static class Program
{
    public static string? RequestedScene { get; set; }

    private static void Main(string[] args)
    {
        var logger = new ConsoleLogger(LogLevel.Info);
        logger.Info("Game", "MicroEngine Demo Showcase Starting...");

        var renderBackend = new RaylibRenderBackend();
        var inputBackend = new RaylibInputBackend();

        renderBackend.Initialize(800, 600, "MicroEngine - Demo Showcase");
        renderBackend.SetTargetFPS(60);

        try
        {
            Scene currentScene = new MainMenuScene(inputBackend, renderBackend, logger);
            currentScene.OnLoad();

            logger.Info("Game", "Main menu running... Press 1-5 to select demo, ESC to exit");

            while (!renderBackend.ShouldClose)
            {
                var deltaTime = renderBackend.GetDeltaTime();

                inputBackend.Update();
                currentScene.OnUpdate(deltaTime);

                if (RequestedScene != null)
                {
                    currentScene.OnUnload();
                    currentScene = CreateScene(RequestedScene, inputBackend, renderBackend, logger);
                    currentScene.OnLoad();
                    RequestedScene = null;
                }

                renderBackend.BeginFrame();
                currentScene.OnRender();
                renderBackend.EndFrame();
            }

            currentScene.OnUnload();
            logger.Info("Game", "Demo showcase shut down successfully");
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

    private static Scene CreateScene(string sceneName, object inputBackend, object renderBackend, ILogger logger)
    {
        return sceneName switch
        {
            "MainMenu" => new MainMenuScene((Core.Input.IInputBackend)inputBackend, (Core.Graphics.IRenderBackend)renderBackend, logger),
            "EcsBasics" => new EcsBasicsDemo((Core.Input.IInputBackend)inputBackend, (Core.Graphics.IRenderBackend)renderBackend, logger),
            "Graphics" => new GraphicsDemo((Core.Input.IInputBackend)inputBackend, (Core.Graphics.IRenderBackend)renderBackend, logger),
            "Physics" => new PhysicsDemo((Core.Input.IInputBackend)inputBackend, (Core.Graphics.IRenderBackend)renderBackend, logger),
            "Input" => new InputDemo((Core.Input.IInputBackend)inputBackend, (Core.Graphics.IRenderBackend)renderBackend, logger),
            "Tilemap" => new TilemapDemo((Core.Input.IInputBackend)inputBackend, (Core.Graphics.IRenderBackend)renderBackend, logger),
            _ => new MainMenuScene((Core.Input.IInputBackend)inputBackend, (Core.Graphics.IRenderBackend)renderBackend, logger)
        };
    }
}





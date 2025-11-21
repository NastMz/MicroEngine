using MicroEngine.Backend.Aether;
using MicroEngine.Backend.Raylib;
using MicroEngine.Backend.Raylib.Resources;
using MicroEngine.Core.DependencyInjection;
using MicroEngine.Core.ECS.Systems;
using MicroEngine.Core.Engine;
using MicroEngine.Core.Events;
using MicroEngine.Core.Logging;
using MicroEngine.Core.Physics;
using MicroEngine.Core.Resources;
using MicroEngine.Core.Scenes;
using MicroEngine.Core.State;
using MicroEngine.Game.Scenes;

namespace MicroEngine.Game;

internal static class Program
{
    private static void Main(string[] args)
    {
        var logger = new ConsoleLogger(LogLevel.Info);
        logger.Info("Game", "MicroEngine Demo Showcase Starting...");

        var renderBackend = new RaylibRenderBackend();
        var inputBackend = new RaylibInputBackend();
        var audioBackend = new RaylibAudioBackend();

        // Configure MSAA before window initialization
        renderBackend.AntiAliasing = Core.Graphics.AntiAliasingMode.MSAA4X;

        // Initialize texture resource cache
        var textureLoader = new RaylibTextureLoader();
        var textureCache = new ResourceCache<ITexture>(textureLoader, logger);

        // Initialize audio resource cache
        var audioLoader = new RaylibAudioClipLoader();
        var audioCache = new ResourceCache<IAudioClip>(audioLoader, logger);

        renderBackend.Initialize(850, 600, $"{EngineInfo.FullName} - Demo Showcase");
        audioBackend.Initialize();

        try
        {
            // Create all transition effects
            var fadeTransition = new FadeTransition(renderBackend, renderBackend, duration: 0.25f);
            var slideTransition = new SlideTransition(renderBackend, renderBackend, SlideDirection.Left, duration: 0.5f);
            var wipeTransition = new WipeTransition(renderBackend, renderBackend, WipeDirection.LeftToRight, duration: 0.4f);
            var zoomTransition = new ZoomTransition(renderBackend, renderBackend, ZoomMode.ZoomOut, duration: 0.5f);

            // Create engine configuration with fixed timestep for deterministic physics
            var engineConfig = new EngineConfiguration
            {
                FixedTimeStep = 1f / 60f, // 60 updates per second (16.67ms)
                MaxFixedUpdatesPerFrame = 5 // Prevent spiral of death
            };

            // Create GameEngine with backends (rendering is uncapped/V-synced)
            var engine = new GameEngine(
                engineConfig,
                renderBackend, // IWindow
                renderBackend, // IRenderer2D
                inputBackend,
                logger,
                fadeTransition
            );

            // Create scene cache for demo scene reuse (max 5 scenes)
            var sceneCache = new SceneCache(maxCacheSize: 5);

            // Create global game state for persistent data
            var gameState = new GameState();

            // Create TimeService for FPS tracking (not for limiting - engine handles timing)
            var timeService = new Core.Time.TimeService(targetFPS: 0);

            // Create DI container and register services
            var serviceContainer = new ServiceContainer();

            // Register singleton services (shared across entire application)
            serviceContainer.RegisterSingleton(logger);
            serviceContainer.RegisterSingleton(renderBackend); // IWindow + IRenderer2D
            serviceContainer.RegisterSingleton(inputBackend);
            serviceContainer.RegisterSingleton(timeService);
            serviceContainer.RegisterSingleton(textureCache);
            serviceContainer.RegisterSingleton(audioCache);
            serviceContainer.RegisterSingleton(audioBackend); // IAudioDevice + ISoundPlayer + IMusicPlayer
            serviceContainer.RegisterSingleton(gameState);

            // Register scoped services (per scene load)
            serviceContainer.RegisterScoped<EventBus>(s => new EventBus());
            serviceContainer.RegisterScoped<PhysicsBackendSystem>(s =>
            {
                var backend = new AetherPhysicsBackend();
                return new PhysicsBackendSystem(backend);
            });

            // Create scene context with all engine services
            var sceneContext = new SceneContext(
                renderBackend, // IWindow
                renderBackend, // IRenderer2D
                inputBackend,
                timeService,
                logger,
                textureCache,
                audioCache,
                audioBackend, // IAudioDevice
                audioBackend, // ISoundPlayer
                audioBackend, // IMusicPlayer
                gameState,
                serviceContainer
            );

            // Initialize engine with scene context
            engine.Initialize(sceneContext);

            // Load initial scene (MainMenu) with access to SceneManager, transitions, and scene cache
            var mainMenu = new MainMenuScene(
                engine.SceneManager,
                sceneCache,
                fadeTransition,
                slideTransition,
                wipeTransition,
                zoomTransition
            );
            engine.SceneManager.ReplaceScene(mainMenu);

            logger.Info("Game", "Main menu running... Press 1-5 to select demo, ESC to exit");

            // Run game loop (fixed timestep for updates, variable rate for rendering)
            engine.Run();

            engine.Shutdown();
            logger.Info("Game", "Demo showcase shut down successfully");
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





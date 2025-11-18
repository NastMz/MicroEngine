using MicroEngine.Core.ECS;
using MicroEngine.Core.ECS.Components;
using MicroEngine.Core.ECS.Helpers;
using MicroEngine.Core.Graphics;
using MicroEngine.Core.Input;
using MicroEngine.Core.Logging;
using MicroEngine.Core.Math;
using MicroEngine.Core.Scenes;

namespace MicroEngine.Game.Scenes.Demos;

/// <summary>
/// Demonstrates ECS fundamentals and Phase 3 entity creation patterns.
/// Shows EntityBuilder and EntityFactory usage with simple colored entities.
/// </summary>
public sealed class EcsBasicsDemo : Scene
{
    private readonly IInputBackend _inputBackend;
    private readonly IRenderBackend _renderBackend;
    private readonly ILogger _logger;

    private const string SCENE_NAME = "EcsBasicsDemo";

    /// <summary>
    /// Initializes a new instance of the <see cref="EcsBasicsDemo"/> class.
    /// </summary>
    public EcsBasicsDemo(IInputBackend inputBackend, IRenderBackend renderBackend, ILogger logger)
        : base(SCENE_NAME)
    {
        _inputBackend = inputBackend ?? throw new ArgumentNullException(nameof(inputBackend));
        _renderBackend = renderBackend ?? throw new ArgumentNullException(nameof(renderBackend));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public override void OnLoad()
    {
        base.OnLoad();
        _logger.Info(SCENE_NAME, "Demo loaded - showcasing EntityBuilder and EntityFactory");

        CreateDemoEntities();

        _logger.Info(SCENE_NAME, $"Created {World.EntityCount} entities");
    }

    /// <inheritdoc/>
    public override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);

        if (_inputBackend.IsKeyPressed(Key.Escape))
        {
            Program.RequestedScene = "MainMenu";
        }
        else if (_inputBackend.IsKeyPressed(Key.R))
        {
            ResetScene();
        }
    }

    /// <inheritdoc/>
    public override void OnRender()
    {
        _renderBackend.Clear(new Color(30, 30, 40, 255));

        RenderEntities();
        RenderUI();
    }

    /// <inheritdoc/>
    public override void OnUnload()
    {
        base.OnUnload();
        _logger.Info(SCENE_NAME, "Demo unloaded");
    }

    private void CreateDemoEntities()
    {
        // Blue player entity using EntityBuilder
        new EntityBuilder(World)
            .WithName("Player")
            .WithTransform(new Vector2(400, 300))
            .Build();

        // Red enemy entities using EntityFactory
        var enemyPositions = new[] { new Vector2(200, 150), new Vector2(600, 150), new Vector2(200, 450), new Vector2(600, 450) };
        foreach (var pos in enemyPositions)
        {
            EntityFactory.CreateEnemy(World, pos);
        }

        // Yellow collectibles using EntityFactory
        var collectiblePositions = new[] { new Vector2(100, 100), new Vector2(700, 100), new Vector2(400, 100), new Vector2(100, 500), new Vector2(700, 500), new Vector2(400, 500) };
        foreach (var pos in collectiblePositions)
        {
            EntityFactory.CreateCollectible(World, pos);
        }

        // Gray obstacles using EntityBuilder
        var obstaclePositions = new[] { new Vector2(300, 200), new Vector2(500, 200), new Vector2(300, 400), new Vector2(500, 400) };
        foreach (var pos in obstaclePositions)
        {
            new EntityBuilder(World).WithName("Obstacle").WithTransform(pos).Build();
        }

        _logger.Debug(SCENE_NAME, "Entities created using modern Phase 3 patterns");
    }

    private void ResetScene()
    {
        _logger.Info(SCENE_NAME, "Resetting scene...");

        var entities = World.GetEntitiesWith<TransformComponent>().ToList();
        foreach (var entity in entities)
        {
            World.DestroyEntity(entity);
        }

        CreateDemoEntities();
    }

    private void RenderEntities()
    {
        foreach (var entity in World.GetEntitiesWith<TransformComponent>())
        {
            var transform = World.GetComponent<TransformComponent>(entity);
            var name = World.GetEntityName(entity) ?? "Unknown";
            var color = GetColorByName(name);
            var size = GetSizeByName(name);

            _renderBackend.DrawCircle(transform.Position, size, color);
        }
    }

    private void RenderUI()
    {
        _renderBackend.DrawText("ECS Basics - EntityBuilder & EntityFactory", new Vector2(10, 10), 18, Color.White);
        _renderBackend.DrawText($"Entities: {World.EntityCount}", new Vector2(10, 35), 16, new Color(200, 200, 200, 255));
        _renderBackend.DrawText("[R] Reset | [ESC] Back to Menu", new Vector2(10, 580), 14, new Color(150, 150, 150, 255));
    }

    private static Color GetColorByName(string name) => name switch
    {
        "Player" => new Color(100, 150, 255, 255),
        "Enemy" => new Color(255, 100, 100, 255),
        "Obstacle" => new Color(120, 120, 120, 255),
        "Collectible" => new Color(255, 220, 100, 255),
        "Platform" => new Color(100, 200, 100, 255),
        _ => Color.White
    };

    private static float GetSizeByName(string name) => name switch
    {
        "Player" => 16f,
        "Enemy" => 14f,
        "Obstacle" => 25f,
        "Collectible" => 8f,
        "Platform" => 20f,
        _ => 10f
    };
}



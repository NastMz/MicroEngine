using MicroEngine.Core.ECS;
using MicroEngine.Core.ECS.Components;
using MicroEngine.Core.ECS.Helpers;
using MicroEngine.Core.ECS.Systems;
using MicroEngine.Core.Graphics;
using MicroEngine.Core.Input;
using MicroEngine.Core.Logging;
using MicroEngine.Core.Math;
using MicroEngine.Core.Scenes;

namespace MicroEngine.Game.Scenes.Demos;

/// <summary>
/// Demonstrates ECS fundamentals and Phase 3 entity creation patterns.
/// Shows EntityBuilder and EntityFactory usage with interactive entities.
/// Features: player movement, enemy patrol, collectible animations, and labels.
/// </summary>
public sealed class EcsBasicsDemo : Scene
{
    private IInputBackend _inputBackend = null!;
    private IRenderer2D _renderer = null!;
    private ILogger _logger = null!;
    private readonly MovementSystem _movementSystem;

    private const string SCENE_NAME = "EcsBasicsDemo";
    private const float PLAYER_SPEED = 200f;
    private const float ENEMY_SPEED = 50f;
    private const float COLLECTIBLE_BOUNCE_SPEED = 2f;
    private const float COLLECTIBLE_BOUNCE_HEIGHT = 10f;
    private const string ENTITY_NAME_PLAYER = "Player";
    private const string ENTITY_NAME_ENEMY = "Enemy";
    private const string ENTITY_NAME_OBSTACLE = "Obstacle";
    private const string ENTITY_NAME_COLLECTIBLE = "Collectible";

    private Entity _playerEntity;
    private readonly List<Entity> _enemyEntities = new();
    private readonly List<Entity> _collectibleEntities = new();
    private readonly Dictionary<Entity, float> _enemyDirections = new();
    private readonly Dictionary<Entity, float> _collectiblePhases = new();
    private float _time;
    private int _collectedCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="EcsBasicsDemo"/> class.
    /// </summary>
    public EcsBasicsDemo()
        : base(SCENE_NAME)
    {
        _movementSystem = new MovementSystem();
    }

    /// <inheritdoc/>
    public override void OnLoad(SceneContext context)
    {
        base.OnLoad(context);
        _inputBackend = context.InputBackend;
        _renderer = context.Renderer;
        _logger = context.Logger;
        _logger.Info(SCENE_NAME, "Demo loaded - showcasing EntityBuilder and EntityFactory with interactivity");

        CreateDemoEntities();

        _logger.Info(SCENE_NAME, $"Created {World.EntityCount} entities - use WASD/Arrows to move player");
    }

    /// <inheritdoc/>
    public override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);

        _time += deltaTime;

        if (_inputBackend.IsKeyPressed(Key.Escape))
        {
            PopScene();
        }
        else if (_inputBackend.IsKeyPressed(Key.R))
        {
            ResetScene();
        }

        UpdatePlayerMovement(deltaTime);
        UpdateEnemyPatrol(deltaTime);
        UpdateCollectibleAnimations(deltaTime);
        CheckCollections();
    }

    /// <inheritdoc/>
    public override void OnRender()
    {
        _renderer.Clear(new Color(30, 30, 40, 255));

        RenderEntities();
        RenderLabels();
        RenderUI();
    }

    /// <inheritdoc/>
    public override void OnUnload()
    {
        base.OnUnload();
        _logger?.Info(SCENE_NAME, "Demo unloaded");
    }

    private void CreateDemoEntities()
    {
        _enemyEntities.Clear();
        _collectibleEntities.Clear();
        _enemyDirections.Clear();
        _collectiblePhases.Clear();
        _collectedCount = 0;

        // Blue player entity using EntityBuilder (centered)
        _playerEntity = new EntityBuilder(World)
            .WithName(ENTITY_NAME_PLAYER)
            .WithTransform(new Vector2(400, 300))
            .Build();

        // Red enemy entities using EntityFactory (patrol horizontally)
        var enemyPositions = new[] 
        { 
            new Vector2(200, 150), 
            new Vector2(600, 150), 
            new Vector2(200, 450), 
            new Vector2(600, 450) 
        };
        
        foreach (var pos in enemyPositions)
        {
            var enemy = EntityFactory.CreateEnemy(World, pos);
            _enemyEntities.Add(enemy);
            _enemyDirections[enemy] = 1f; // Start moving right
        }

        // Yellow collectibles using EntityFactory (bounce animation)
        var collectiblePositions = new[] 
        { 
            new Vector2(100, 100), 
            new Vector2(700, 100), 
            new Vector2(400, 100), 
            new Vector2(100, 500), 
            new Vector2(700, 500), 
            new Vector2(400, 500) 
        };
        
        for (int i = 0; i < collectiblePositions.Length; i++)
        {
            var collectible = EntityFactory.CreateCollectible(World, collectiblePositions[i]);
            _collectibleEntities.Add(collectible);
            _collectiblePhases[collectible] = i * MathF.PI / 3; // Stagger animation phases
        }

        // Gray obstacles using EntityBuilder (static)
        var obstaclePositions = new[] 
        { 
            new Vector2(300, 200), 
            new Vector2(500, 200), 
            new Vector2(300, 400), 
            new Vector2(500, 400) 
        };
        
        foreach (var pos in obstaclePositions)
        {
            new EntityBuilder(World).WithName(ENTITY_NAME_OBSTACLE).WithTransform(pos).Build();
        }

        _logger.Debug(SCENE_NAME, "Interactive entities created using modern Phase 3 patterns");
    }

    private void ClearEntities()
    {
        // Clear tracking lists
        _enemyEntities.Clear();
        _collectibleEntities.Clear();
        _enemyDirections.Clear();
        _collectiblePhases.Clear();
        _collectedCount = 0;

        // Destroy all entities
        var entities = World.GetEntitiesWith<TransformComponent>().ToList();
        foreach (var entity in entities)
        {
            World.DestroyEntity(entity);
        }

        // Force process destruction queue before creating new entities
        World.Update(0f);
    }

    private void ResetScene()
    {
        _logger.Info(SCENE_NAME, "Resetting scene...");

        ClearEntities();

        _time = 0f;
        CreateDemoEntities();
    }

    private void UpdatePlayerMovement(float deltaTime)
    {
        if (!World.IsEntityValid(_playerEntity))
        {
            return;
        }

        var movementX = 0f;
        var movementY = 0f;

        // WASD and Arrow keys support
        if (_inputBackend.IsKeyDown(Key.W) || _inputBackend.IsKeyDown(Key.Up))
        {
            movementY -= 1f;
        }
        if (_inputBackend.IsKeyDown(Key.S) || _inputBackend.IsKeyDown(Key.Down))
        {
            movementY += 1f;
        }
        if (_inputBackend.IsKeyDown(Key.A) || _inputBackend.IsKeyDown(Key.Left))
        {
            movementX -= 1f;
        }
        if (_inputBackend.IsKeyDown(Key.D) || _inputBackend.IsKeyDown(Key.Right))
        {
            movementX += 1f;
        }

        const float EPSILON = 0.0001f;
        if (MathF.Abs(movementX) > EPSILON || MathF.Abs(movementY) > EPSILON)
        {
            // Normalize diagonal movement
            var length = MathF.Sqrt(movementX * movementX + movementY * movementY);
            if (length > EPSILON)
            {
                movementX /= length;
                movementY /= length;
            }

            var offset = new Vector2(
                movementX * PLAYER_SPEED * deltaTime,
                movementY * PLAYER_SPEED * deltaTime
            );

            _movementSystem.Translate(World, _playerEntity, offset);

            // Clamp to screen bounds
            ref var transform = ref World.GetComponent<TransformComponent>(_playerEntity);
            transform.Position = new Vector2(
                MathF.Max(20f, MathF.Min(780f, transform.Position.X)),
                MathF.Max(20f, MathF.Min(580f, transform.Position.Y))
            );
        }
    }

    private void UpdateEnemyPatrol(float deltaTime)
    {
        foreach (var enemy in _enemyEntities.ToList())
        {
            if (!World.IsEntityValid(enemy))
            {
                continue;
            }

            ref var transform = ref World.GetComponent<TransformComponent>(enemy);
            var direction = _enemyDirections[enemy];

            // Move horizontally
            var offset = new Vector2(direction * ENEMY_SPEED * deltaTime, 0f);
            _movementSystem.Translate(World, enemy, offset);

            // Reverse direction at bounds (100px padding from edges)
            if (transform.Position.X < 100f || transform.Position.X > 700f)
            {
                _enemyDirections[enemy] *= -1f;
            }
        }
    }

    private void UpdateCollectibleAnimations(float deltaTime)
    {
        foreach (var collectible in _collectibleEntities.ToList())
        {
            if (!World.IsEntityValid(collectible))
            {
                continue;
            }

            // Bounce animation using sine wave
            var phase = _collectiblePhases[collectible];
            _collectiblePhases[collectible] = phase + COLLECTIBLE_BOUNCE_SPEED * deltaTime;
        }
    }

    private void CheckCollections()
    {
        if (!World.IsEntityValid(_playerEntity))
        {
            return;
        }

        var playerTransform = World.GetComponent<TransformComponent>(_playerEntity);
        const float COLLECTION_DISTANCE = 30f; // Increased from 20f (player radius 16 + collectible radius 8 + padding)

        foreach (var collectible in _collectibleEntities.ToList())
        {
            if (!World.IsEntityValid(collectible))
            {
                continue;
            }

            var collectibleTransform = World.GetComponent<TransformComponent>(collectible);
            var dx = playerTransform.Position.X - collectibleTransform.Position.X;
            var dy = playerTransform.Position.Y - collectibleTransform.Position.Y;
            var distanceSquared = dx * dx + dy * dy;

            if (distanceSquared < COLLECTION_DISTANCE * COLLECTION_DISTANCE)
            {
                _logger.Debug(SCENE_NAME, $"Collecting entity {collectible.Id} at distance {MathF.Sqrt(distanceSquared):F1}px");
                World.DestroyEntity(collectible);
                _collectibleEntities.Remove(collectible);
                _collectiblePhases.Remove(collectible);
                _collectedCount++;
                _logger.Info(SCENE_NAME, $"Collectible collected! Total: {_collectedCount}/6");
            }
        }
    }

    private void RenderEntities()
    {
        foreach (var entity in World.GetEntitiesWith<TransformComponent>())
        {
            // Skip entities marked for destruction
            if (!World.IsEntityValid(entity))
            {
                continue;
            }

            var transform = World.GetComponent<TransformComponent>(entity);
            var name = World.GetEntityName(entity) ?? "Unknown";
            var color = GetColorByName(name);
            var size = GetSizeByName(name);

            // Apply bounce offset for collectibles
            var renderPos = transform.Position;
            if (name == ENTITY_NAME_COLLECTIBLE && _collectiblePhases.TryGetValue(entity, out var phase))
            {
                var bounceOffset = MathF.Sin(phase) * COLLECTIBLE_BOUNCE_HEIGHT;
                renderPos = new Vector2(renderPos.X, renderPos.Y + bounceOffset);
            }

            _renderer.DrawCircle(renderPos, size, color);

            // Draw trail for player (simple effect)
            if (entity.Equals(_playerEntity))
            {
                var trailColor = new Color(100, 150, 255, 80);
                _renderer.DrawCircle(renderPos, size + 4, trailColor);
            }
        }
    }

    private void RenderLabels()
    {
        foreach (var entity in World.GetEntitiesWith<TransformComponent>())
        {
            // Skip entities marked for destruction
            if (!World.IsEntityValid(entity))
            {
                continue;
            }

            var transform = World.GetComponent<TransformComponent>(entity);
            var name = World.GetEntityName(entity) ?? "Unknown";

            // Apply bounce offset for collectibles (match entity rendering)
            var labelPos = transform.Position;
            if (name == ENTITY_NAME_COLLECTIBLE && _collectiblePhases.TryGetValue(entity, out var phase))
            {
                var bounceOffset = MathF.Sin(phase) * COLLECTIBLE_BOUNCE_HEIGHT;
                labelPos = new Vector2(labelPos.X, labelPos.Y + bounceOffset);
            }

            // Draw label above entity
            var labelOffset = new Vector2(-20f, -30f);
            var textPos = new Vector2(labelPos.X + labelOffset.X, labelPos.Y + labelOffset.Y);
            _renderer.DrawText(name, textPos, 12, new Color(220, 220, 220, 200));
        }
    }

    private void RenderUI()
    {
        var layout = new TextLayoutHelper(startX: 10, startY: 10, defaultLineHeight: 20);
        var infoColor = new Color(200, 200, 200, 255);
        var dimColor = new Color(180, 180, 180, 255);
        var controlsColor = new Color(150, 150, 150, 255);

        layout.DrawText(_renderer, "ECS Basics - Interactive Demo", 20, Color.White)
              .AddSpacing(5)
              .DrawText(_renderer, $"Entities: {World.EntityCount} | Collected: {_collectedCount}/6", 16, infoColor)
              .DrawText(_renderer, "Features: EntityBuilder, EntityFactory, MovementSystem", 14, dimColor);

        layout.SetY(580)
              .DrawText(_renderer, "[WASD/Arrows] Move | [R] Reset | [ESC] Menu", 14, controlsColor);
    }

    private static Color GetColorByName(string name) => name switch
    {
        ENTITY_NAME_PLAYER => new Color(100, 150, 255, 255),
        ENTITY_NAME_ENEMY => new Color(255, 100, 100, 255),
        ENTITY_NAME_OBSTACLE => new Color(120, 120, 120, 255),
        ENTITY_NAME_COLLECTIBLE => new Color(255, 220, 100, 255),
        _ => Color.White
    };

    private static float GetSizeByName(string name) => name switch
    {
        ENTITY_NAME_PLAYER => 16f,
        ENTITY_NAME_ENEMY => 14f,
        ENTITY_NAME_OBSTACLE => 25f,
        ENTITY_NAME_COLLECTIBLE => 8f,
        _ => 10f
    };
}




using MicroEngine.Core.ECS;
using MicroEngine.Core.ECS.Components;
using MicroEngine.Core.Graphics;
using MicroEngine.Core.Input;
using MicroEngine.Core.Logging;
using MicroEngine.Core.Math;
using MicroEngine.Core.Physics;
using MicroEngine.Core.Scenes;

namespace MicroEngine.Game.Scenes.Demos;

/// <summary>
/// Demonstrates collision filtering with CollisionMatrix in a simple platformer.
/// Shows how enemies pass through each other but collide with player and obstacles.
/// </summary>
public sealed class CollisionFilteringDemo : Scene
{
    private IInputBackend _inputBackend = null!;
    private IRenderer2D _renderer = null!;
    private ILogger _logger = null!;

    private const string SCENE_NAME = "CollisionFilteringDemo";
    private const float PLAYER_SPEED = 200f;
    private const float ENEMY_SPEED = 100f;

    // Collision layers
    private static readonly CollisionLayer PlayerLayer = new(0, "Player");
    private static readonly CollisionLayer EnemyLayer = new(1, "Enemy");
    private static readonly CollisionLayer ObstacleLayer = new(2, "Obstacle");
    private static readonly CollisionLayer GroundLayer = new(3, "Ground");

    private readonly CollisionMatrix _collisionMatrix = new();
    private readonly Dictionary<Entity, CollisionLayer> _entityLayers = new();
    private readonly Dictionary<Entity, float> _enemyDirections = new();
    private readonly List<string> _collisionLog = new();

    private Entity _player;
    private readonly List<Entity> _enemies = new();
    private readonly List<Entity> _obstacles = new();
    private Entity _ground;

    /// <summary>
    /// Initializes a new instance of the <see cref="CollisionFilteringDemo"/> class.
    /// </summary>
    public CollisionFilteringDemo()
        : base(SCENE_NAME)
    {
    }

    /// <inheritdoc/>
    public override void OnLoad(SceneContext context)
    {
        base.OnLoad(context);
        _inputBackend = context.InputBackend;
        _renderer = context.Renderer;
        _logger = context.Logger;
        _logger.Info(SCENE_NAME, "Collision Filtering demo loaded - platformer with collision filtering");

        ConfigureCollisionMatrix();
        CreateEntities();
    }

    private void ConfigureCollisionMatrix()
    {
        // Player collides with: Ground, Obstacles, Enemies
        _collisionMatrix.SetCollision(PlayerLayer, GroundLayer, true);
        _collisionMatrix.SetCollision(PlayerLayer, ObstacleLayer, true);
        _collisionMatrix.SetCollision(PlayerLayer, EnemyLayer, true);

        // Enemy collides with: Ground, Obstacles (but NOT other enemies)
        _collisionMatrix.SetCollision(EnemyLayer, GroundLayer, true);
        _collisionMatrix.SetCollision(EnemyLayer, ObstacleLayer, true);
        _collisionMatrix.SetCollision(EnemyLayer, EnemyLayer, false); // Enemies pass through each other!

        // Obstacles collide with everything
        _collisionMatrix.SetCollision(ObstacleLayer, GroundLayer, true);

        _logger.Info(SCENE_NAME, "Collision matrix configured - enemies pass through each other");
    }

    private void CreateEntities()
    {
        // Create ground platform
        _ground = World.CreateEntity("Ground");
        World.AddComponent(_ground, new TransformComponent { Position = new Vector2(400, 550) });
        _entityLayers[_ground] = GroundLayer;

        // Create player
        _player = World.CreateEntity("Player");
        World.AddComponent(_player, new TransformComponent { Position = new Vector2(100, 500) });
        _entityLayers[_player] = PlayerLayer;

        // Create obstacles first to define the arena
        // Obstacles at 200 and 600 create a 400px wide central arena
        // Ground is at 550. Obstacle height 60 (half 30). Pos Y = 550 - 30 = 520 to touch ground.
        var obstacle1 = World.CreateEntity("Obstacle1");
        World.AddComponent(obstacle1, new TransformComponent { Position = new Vector2(200, 520) });
        _entityLayers[obstacle1] = ObstacleLayer;
        _obstacles.Add(obstacle1);

        var obstacle2 = World.CreateEntity("Obstacle2");
        World.AddComponent(obstacle2, new TransformComponent { Position = new Vector2(600, 520) });
        _entityLayers[obstacle2] = ObstacleLayer;
        _obstacles.Add(obstacle2);

        // Create enemies
        // Enemy 1: Starts at 300, moves Right -> <- Meets Enemy 2
        // Lower enemies to Y=530 to be closer to ground (Player is at 530)
        var enemy1 = World.CreateEntity("Enemy1");
        World.AddComponent(enemy1, new TransformComponent { Position = new Vector2(300, 530) });
        _entityLayers[enemy1] = EnemyLayer;
        _enemyDirections[enemy1] = 1f; // Move Right
        _enemies.Add(enemy1);

        // Enemy 2: Starts at 500, moves Left -> <- Meets Enemy 1
        var enemy2 = World.CreateEntity("Enemy2");
        World.AddComponent(enemy2, new TransformComponent { Position = new Vector2(500, 530) });
        _entityLayers[enemy2] = EnemyLayer;
        _enemyDirections[enemy2] = -1f; // Move Left
        _enemies.Add(enemy2);

        // Enemy 3: Outside the arena (Right side)
        var enemy3 = World.CreateEntity("Enemy3");
        World.AddComponent(enemy3, new TransformComponent { Position = new Vector2(700, 530) });
        _entityLayers[enemy3] = EnemyLayer;
        _enemyDirections[enemy3] = -1f;
        _enemies.Add(enemy3);

        _logger.Info(SCENE_NAME, "Entities created: 1 player, 3 enemies, 2 obstacles");
    }

    /// <inheritdoc/>
    public override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);

        if (_inputBackend.IsKeyPressed(Key.Escape))
        {
            PopScene();
            return;
        }

        if (_inputBackend.IsKeyPressed(Key.C))
        {
            _collisionLog.Clear();
            _logger.Info(SCENE_NAME, "Collision log cleared");
        }

        // Move player
        MovePlayer(deltaTime);

        // Move enemies
        MoveEnemies(deltaTime);

        // Check collisions
        CheckCollisions();
    }

    private const float GRAVITY = 900f;
    private const float JUMP_FORCE = -450f;
    private float _playerVelocityY = 0f;
    private bool _isGrounded = false;

    private void MovePlayer(float deltaTime)
    {
        if (!World.IsEntityValid(_player))
        {
            return;
        }

        ref var transform = ref World.GetComponent<TransformComponent>(_player);
        
        // 1. Horizontal Movement
        var movementX = 0f;
        if (_inputBackend.IsKeyDown(Key.Left))
        {
            movementX -= PLAYER_SPEED * deltaTime;
        }

        if (_inputBackend.IsKeyDown(Key.Right))
        {
            movementX += PLAYER_SPEED * deltaTime;
        }

        // Apply X movement
        transform.Position = new Vector2(transform.Position.X + movementX, transform.Position.Y);
        
        // Clamp to screen bounds
        transform.Position = new Vector2(
            MathF.Max(30f, MathF.Min(770f, transform.Position.X)),
            transform.Position.Y
        );

        // Check X Collisions (Obstacles)
        ResolveObstacleCollisions(ref transform, true);

        // 2. Vertical Movement (Gravity + Jump)
        if (_inputBackend.IsKeyPressed(Key.Space) && _isGrounded)
        {
            _playerVelocityY = JUMP_FORCE;
            _isGrounded = false;
            // Add simple jump sound effect if available or just log
            // _logger.Debug(SCENE_NAME, "Jump!");
        }

        _playerVelocityY += GRAVITY * deltaTime;
        transform.Position = new Vector2(transform.Position.X, transform.Position.Y + _playerVelocityY * deltaTime);

        // Check Y Collisions (Ground + Obstacles)
        _isGrounded = false; // Assume falling until proven otherwise
        
        // Ground collision
        if (transform.Position.Y >= 530f) // Ground is at 550, player radius 20
        {
            transform.Position = new Vector2(transform.Position.X, 530f);
            _playerVelocityY = 0;
            _isGrounded = true;
        }

        ResolveObstacleCollisions(ref transform, false);
    }

    private void ResolveObstacleCollisions(ref TransformComponent playerTransform, bool isXAxis)
    {
        var playerLayer = _entityLayers[_player];
        var playerRadius = 20f;

        foreach (var obstacle in _obstacles)
        {
            if (!World.IsEntityValid(obstacle))
            {
                continue;
            }

            var obstacleTransform = World.GetComponent<TransformComponent>(obstacle);
            var obstacleLayer = _entityLayers[obstacle];
            
            // Obstacle bounds (Center based, 30x60 size)
            var obsHalfW = 15f;
            var obsHalfH = 30f;
            var obsLeft = obstacleTransform.Position.X - obsHalfW;
            var obsRight = obstacleTransform.Position.X + obsHalfW;
            var obsTop = obstacleTransform.Position.Y - obsHalfH;
            var obsBottom = obstacleTransform.Position.Y + obsHalfH;

            // Simple AABB check for player (treating player as 40x40 box for stability)
            var pLeft = playerTransform.Position.X - playerRadius;
            var pRight = playerTransform.Position.X + playerRadius;
            var pTop = playerTransform.Position.Y - playerRadius;
            var pBottom = playerTransform.Position.Y + playerRadius;

            if (pRight > obsLeft && pLeft < obsRight && pBottom > obsTop && pTop < obsBottom)
            {
                if (isXAxis)
                {
                    // Resolve X
                    if (playerTransform.Position.X < obstacleTransform.Position.X)
                    {
                        playerTransform.Position = new Vector2(obsLeft - playerRadius, playerTransform.Position.Y);
                    }
                    else
                    {
                        playerTransform.Position = new Vector2(obsRight + playerRadius, playerTransform.Position.Y);
                    }
                }
                else
                {
                    // Resolve Y
                    if (_playerVelocityY > 0) // Falling
                    {
                        playerTransform.Position = new Vector2(playerTransform.Position.X, obsTop - playerRadius);
                        _playerVelocityY = 0;
                        _isGrounded = true;
                    }
                    else if (_playerVelocityY < 0) // Jumping up
                    {
                        playerTransform.Position = new Vector2(playerTransform.Position.X, obsBottom + playerRadius);
                        _playerVelocityY = 0;
                    }
                }
            }
        }
    }

    private void CheckCollisions()
    {
        // Only check Player vs Enemies and Enemy vs Obstacles here
        // Player vs Obstacles/Ground is handled in MovePlayer for physics stability

        if (World.IsEntityValid(_player))
        {
            var playerTransform = World.GetComponent<TransformComponent>(_player);
            var playerLayer = _entityLayers[_player];

            // Player vs Enemies
            foreach (var enemy in _enemies)
            {
                if (!World.IsEntityValid(enemy))
                {
                    continue;
                }

                var enemyTransform = World.GetComponent<TransformComponent>(enemy);
                var enemyLayer = _entityLayers[enemy];

                if (_collisionMatrix.CanCollide(playerLayer, enemyLayer))
                {
                    if (CheckCircleCollision(playerTransform.Position, 20f, enemyTransform.Position, 18f))
                    {
                        var enemyName = World.GetEntityName(enemy) ?? "Enemy";
                        AddToLog($"Player hit {enemyName}!");
                    }
                }
            }
        }

        // Check enemy collisions
        foreach (var enemy in _enemies)
        {
            if (!World.IsEntityValid(enemy))
            {
                continue;
            }

            ref var enemyTransform = ref World.GetComponent<TransformComponent>(enemy);
            var enemyLayer = _entityLayers[enemy];

            // Enemy vs Obstacles
            foreach (var obstacle in _obstacles)
            {
                if (!World.IsEntityValid(obstacle))
                {
                    continue;
                }

                var obstacleTransform = World.GetComponent<TransformComponent>(obstacle);
                var obstacleLayer = _entityLayers[obstacle];

                if (_collisionMatrix.CanCollide(enemyLayer, obstacleLayer))
                {
                    // Simple AABB/Circle check
                    var obsHalfW = 15f;
                    var obsHalfH = 30f;
                    
                    // Check if enemy is inside obstacle bounds + radius
                    if (enemyTransform.Position.X + 18f > obstacleTransform.Position.X - obsHalfW &&
                        enemyTransform.Position.X - 18f < obstacleTransform.Position.X + obsHalfW &&
                        enemyTransform.Position.Y + 18f > obstacleTransform.Position.Y - obsHalfH &&
                        enemyTransform.Position.Y - 18f < obstacleTransform.Position.Y + obsHalfH)
                    {
                        // Bounce enemy
                        _enemyDirections[enemy] *= -1f;
                        // Move out
                        var dir = enemyTransform.Position.X < obstacleTransform.Position.X ? -1f : 1f;
                        enemyTransform.Position = new Vector2(enemyTransform.Position.X + dir * 5f, enemyTransform.Position.Y);
                    }
                }
            }
        }

        // Check enemy vs enemy (should NOT collide due to collision matrix)
        for (int i = 0; i < _enemies.Count; i++)
        {
            if (!World.IsEntityValid(_enemies[i]))
            {
                continue;
            }

            var enemy1Transform = World.GetComponent<TransformComponent>(_enemies[i]);
            var enemy1Layer = _entityLayers[_enemies[i]];

            for (int j = i + 1; j < _enemies.Count; j++)
            {
                if (!World.IsEntityValid(_enemies[j]))
                {
                    continue;
                }

                var enemy2Transform = World.GetComponent<TransformComponent>(_enemies[j]);
                var enemy2Layer = _entityLayers[_enemies[j]];

                // Check if they overlap (they will, but collision matrix says they shouldn't collide)
                if (CheckCircleCollision(enemy1Transform.Position, 18f, enemy2Transform.Position, 18f))
                {
                    var canCollide = _collisionMatrix.CanCollide(enemy1Layer, enemy2Layer);
                    if (!canCollide)
                    {
                        // They're overlapping but NOT colliding - this is what we want to demonstrate!
                        // Don't log this, it's expected behavior
                    }
                }
            }
        }
    }

    private void MoveEnemies(float deltaTime)
    {
        foreach (var enemy in _enemies)
        {
            if (!World.IsEntityValid(enemy))
            {
                continue;
            }

            ref var transform = ref World.GetComponent<TransformComponent>(enemy);
            var direction = _enemyDirections[enemy];

            transform.Position = new Vector2(
                transform.Position.X + direction * ENEMY_SPEED * deltaTime,
                transform.Position.Y
            );

            // Bounce off screen edges
            if (transform.Position.X < 50f || transform.Position.X > 750f)
            {
                _enemyDirections[enemy] *= -1f;
            }
        }
    }

    private bool CheckCircleCollision(Vector2 pos1, float radius1, Vector2 pos2, float radius2)
    {
        var dx = pos1.X - pos2.X;
        var dy = pos1.Y - pos2.Y;
        var distSq = dx * dx + dy * dy;
        var radiusSum = radius1 + radius2;
        return distSq < radiusSum * radiusSum;
    }

    private void AddToLog(string message)
    {
        _collisionLog.Insert(0, message);
        if (_collisionLog.Count > 5)
        {
            _collisionLog.RemoveAt(5);
        }
        _logger.Debug(SCENE_NAME, message);
    }

    /// <inheritdoc/>
    public override void OnRender()
    {
        _renderer.Clear(new Color(20, 25, 30, 255));

        RenderEntities();
        RenderUI();
    }

    /// <inheritdoc/>
    public override void OnUnload()
    {
        base.OnUnload();
        _logger?.Info(SCENE_NAME, "Collision Filtering demo unloaded");
    }

    private void RenderEntities()
    {
        // Draw ground
        if (World.IsEntityValid(_ground))
        {
            var groundTransform = World.GetComponent<TransformComponent>(_ground);
            _renderer.DrawRectangle(
                new Vector2(0, groundTransform.Position.Y),
                new Vector2(800, 50),
                new Color(80, 80, 80, 255)
            );
        }

        // Draw obstacles
        foreach (var obstacle in _obstacles)
        {
            if (!World.IsEntityValid(obstacle))
            {
                continue;
            }

            var transform = World.GetComponent<TransformComponent>(obstacle);
            var obstaclePos = new Vector2(transform.Position.X - 15, transform.Position.Y - 30);
            _renderer.DrawRectangle(obstaclePos, new Vector2(30, 60), new Color(100, 100, 100, 255));
            _renderer.DrawText("OBS", new Vector2(transform.Position.X - 12, transform.Position.Y - 8), 10, Color.White);
        }

        // Draw enemies
        foreach (var enemy in _enemies)
        {
            if (!World.IsEntityValid(enemy))
            {
                continue;
            }

            var transform = World.GetComponent<TransformComponent>(enemy);
            _renderer.DrawCircle(transform.Position, 18, new Color(255, 100, 100, 255));
            _renderer.DrawText("E", new Vector2(transform.Position.X - 5, transform.Position.Y - 6), 12, Color.White);
        }

        // Draw player
        if (World.IsEntityValid(_player))
        {
            var playerTransform = World.GetComponent<TransformComponent>(_player);
            _renderer.DrawCircle(playerTransform.Position, 20, new Color(100, 150, 255, 255));
            _renderer.DrawText("P", new Vector2(playerTransform.Position.X - 5, playerTransform.Position.Y - 6), 12, Color.White);
        }
    }

    private void RenderUI()
    {
        var layout = new TextLayoutHelper(startX: 10, startY: 10, defaultLineHeight: 20);
        var infoColor = new Color(200, 200, 200, 255);
        var dimColor = new Color(150, 150, 150, 255);

        layout.DrawText(_renderer, "Collision Filtering Demo", 20, Color.White)
              .AddSpacing(5)
              .DrawText(_renderer, "Simple platformer demonstrating collision layers", 12, dimColor)
              .AddSpacing(10)
              .DrawText(_renderer, "How it works:", 16, Color.White)
              .DrawText(_renderer, "- Player collides with enemies and obstacles", 12, dimColor)
              .DrawText(_renderer, "- Enemies pass through each other!", 12, new Color(255, 200, 100, 255))
              .DrawText(_renderer, "- Enemies bounce off obstacles and screen edges", 12, dimColor);

        // Collision Matrix
        layout.AddSpacing(10)
              .DrawText(_renderer, "Collision Matrix:", 16, Color.White)
              .DrawText(_renderer, "What collides:", 12, new Color(100, 255, 100, 255))
              .DrawText(_renderer, "  Player with Enemies, Obstacles", 11, dimColor)
              .DrawText(_renderer, "  Enemies with Obstacles", 11, dimColor)
              .AddSpacing(5)
              .DrawText(_renderer, "What does NOT collide:", 12, new Color(255, 100, 100, 255))
              .DrawText(_renderer, "  Enemies with Enemies (pass through!)", 11, dimColor);

        // Collision Log
        layout.AddSpacing(10)
              .DrawText(_renderer, "Collision Log:", 16, Color.White);

        if (_collisionLog.Count == 0)
        {
            layout.DrawText(_renderer, "No collisions yet", 12, dimColor);
        }
        else
        {
            foreach (var logEntry in _collisionLog)
            {
                layout.DrawText(_renderer, logEntry, 12, new Color(255, 200, 100, 255));
            }
        }

        // Legend
        layout.AddSpacing(10)
              .DrawText(_renderer, "Legend:", 16, Color.White)
              .DrawText(_renderer, "[P] Player (Blue) - You control this", 12, new Color(100, 150, 255, 255))
              .DrawText(_renderer, "[E] Enemy (Red) - Move automatically", 12, new Color(255, 100, 100, 255))
              .DrawText(_renderer, "[OBS] Obstacle (Gray) - Static", 12, new Color(100, 100, 100, 255));

        // Controls
        layout.SetY(500)
              .DrawText(_renderer, "Controls:", 16, Color.White)
              .DrawText(_renderer, "[Left/Right] Move | [SPACE] Jump", 14, dimColor)
              .DrawText(_renderer, "Watch: Enemies pass through each other!", 14, new Color(255, 200, 100, 255))
              .DrawText(_renderer, "[C] Clear Log | [ESC] Menu", 14, dimColor);
    }
}

using MicroEngine.Core.ECS;
using MicroEngine.Core.ECS.Components;
using MicroEngine.Core.ECS.Systems;
using MicroEngine.Core.Graphics;
using MicroEngine.Core.Input;
using MicroEngine.Core.Logging;
using MicroEngine.Core.Math;
using MicroEngine.Core.Scenes;

namespace MicroEngine.Game.Scenes.Demos;

/// <summary>
/// Demonstrates physics simulation with gravity, collisions, and continuous collision detection.
/// Showcases RigidBody, Colliders, and collision resolution.
/// </summary>
public sealed class PhysicsDemo : Scene
{
    private IInputBackend _inputBackend = null!;
    private IRenderBackend2D _renderBackend = null!;
    private ILogger _logger = null!;

    private World _world = null!;
    private PhysicsSystem _physicsSystem = null!;
    private CollisionSystem _collisionSystem = null!;

    private Entity _ground;
    private readonly List<Entity> _balls = new List<Entity>();
    private const int MAX_BALLS = 10;
    private float _spawnTimer = 0f;
    private const float SPAWN_INTERVAL = 0.5f;

    /// <summary>
    /// Initializes a new instance of the <see cref="PhysicsDemo"/> class.
    /// </summary>
    public PhysicsDemo()
        : base("PhysicsDemo")
    {
    }

    /// <inheritdoc/>
    public override void OnLoad(SceneContext context)
    {
        base.OnLoad(context);
        _inputBackend = context.InputBackend;
        _renderBackend = context.RenderBackend;
        _logger = context.Logger;

        // Initialize ECS
        _world = new World();
        _physicsSystem = new PhysicsSystem(new Vector2(0, 980f)); // Downward gravity
        _collisionSystem = new CollisionSystem();

        _world.RegisterSystem(_physicsSystem);
        _world.RegisterSystem(_collisionSystem);

        // Create ground (static platform)
        _ground = _world.CreateEntity();
        _world.AddComponent(_ground, new TransformComponent { Position = new Vector2(400, 550) });
        _world.AddComponent(_ground, new ColliderComponent 
        { 
            Shape = ColliderShape.Rectangle, 
            Size = new Vector2(600, 20),
            Enabled = true
        });

        // Ground visual marker (for rendering)
        _world.AddComponent(_ground, new RenderComponent 
        { 
            Color = new Color(100, 100, 100, 255),
            Shape = RenderShape.Rectangle
        });

        _logger.Info("PhysicsDemo", "Physics demo loaded - click to spawn balls");
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

        // Auto-spawn balls
        _spawnTimer += deltaTime;
        if (_spawnTimer >= SPAWN_INTERVAL && _balls.Count < MAX_BALLS)
        {
            SpawnBall();
            _spawnTimer = 0f;
        }

        // Manual spawn with mouse click
        if (_inputBackend.IsMouseButtonPressed(MouseButton.Left) && _balls.Count < MAX_BALLS)
        {
            var mousePos = _inputBackend.GetMousePosition();
            SpawnBallAt(mousePos);
        }

        // Update physics
        _physicsSystem.Update(_world, deltaTime);

        // Remove balls that fell off screen
        for (int i = _balls.Count - 1; i >= 0; i--)
        {
            var transform = _world.GetComponent<TransformComponent>(_balls[i]);
            if (transform.Position.Y > 700)
            {
                _world.DestroyEntity(_balls[i]);
                _balls.RemoveAt(i);
            }
        }
    }

    /// <inheritdoc/>
    public override void OnRender()
    {
        _renderBackend.Clear(new Color(20, 20, 30, 255));

        // Render ground
        var groundTransform = _world.GetComponent<TransformComponent>(_ground);
        var groundCollider = _world.GetComponent<ColliderComponent>(_ground);
        _renderBackend.DrawRectangle(
            new Vector2(groundTransform.Position.X - groundCollider.Size.X / 2, groundTransform.Position.Y - groundCollider.Size.Y / 2),
            groundCollider.Size,
            new Color(100, 100, 100, 255)
        );

        // Render balls
        foreach (var ball in _balls)
        {
            var transform = _world.GetComponent<TransformComponent>(ball);
            var collider = _world.GetComponent<ColliderComponent>(ball);
            var render = _world.GetComponent<RenderComponent>(ball);

            _renderBackend.DrawCircle(transform.Position, collider.Size.X, render.Color);
        }

        // UI
        _renderBackend.DrawText("Physics Demo - Continuous Collision Detection", new Vector2(20, 20), 20, Color.White);
        _renderBackend.DrawText($"Balls: {_balls.Count}/{MAX_BALLS}", new Vector2(20, 50), 16, new Color(180, 180, 180, 255));
        _renderBackend.DrawText("[Click] Spawn Ball | [ESC] Back to Menu", new Vector2(10, 580), 14, new Color(150, 150, 150, 255));
    }

    /// <inheritdoc/>
    public override void OnUnload()
    {
        base.OnUnload();
        _logger.Info("PhysicsDemo", "Physics demo unloaded");
    }

    private void SpawnBall()
    {
        var random = new Random();
        var x = random.Next(100, 700);
        SpawnBallAt(new Vector2(x, 50));
    }

    private void SpawnBallAt(Vector2 position)
    {
        var ball = _world.CreateEntity();
        var random = new Random();

        _world.AddComponent(ball, new TransformComponent { Position = position });
        _world.AddComponent(ball, new ColliderComponent 
        { 
            Shape = ColliderShape.Circle, 
            Size = new Vector2(15, 15),
            Enabled = true
        });
        _world.AddComponent(ball, new RigidBodyComponent 
        { 
            Mass = 1.0f,
            Velocity = new Vector2(random.Next(-50, 50), 0),
            UseGravity = true,
            GravityScale = 1.0f,
            Drag = 0.01f,
            Restitution = 0.6f,
            UseContinuousCollision = true
        });

        var color = new Color(
            (byte)random.Next(100, 255),
            (byte)random.Next(100, 255),
            (byte)random.Next(100, 255),
            255
        );

        _world.AddComponent(ball, new RenderComponent 
        { 
            Color = color,
            Shape = RenderShape.Circle
        });

        _balls.Add(ball);
    }
}

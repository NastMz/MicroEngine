using MicroEngine.Backend.Aether;
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
/// Demonstrates physics simulation with gravity, collisions, and realistic dynamics.
/// Uses Aether.Physics2D backend for professional-grade physics.
/// </summary>
public sealed class PhysicsDemo : Scene
{
    private IInputBackend _inputBackend = null!;
    private IRenderBackend2D _renderBackend = null!;
    private ILogger _logger = null!;

    private World _world = null!;
    private PhysicsBackendSystem _physicsSystem = null!;

    private Entity _ground;
    private readonly List<Entity> _balls;
    private const int MAX_BALLS = 20;
    
    // Drag functionality
    private Entity? _draggedBall;
    private Vector2 _dragOffset;

    /// <summary>
    /// Initializes a new instance of the <see cref="PhysicsDemo"/> class.
    /// </summary>
    public PhysicsDemo()
        : base("PhysicsDemo")
    {
        _balls = new List<Entity>();
        _draggedBall = null;
        _dragOffset = Vector2.Zero;
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
        
        // Create physics backend system with Aether
        var aetherBackend = new AetherPhysicsBackend();
        _physicsSystem = new PhysicsBackendSystem(aetherBackend);
        _physicsSystem.Initialize(gravity: 750f); // Downward gravity
        
        _world.RegisterSystem(_physicsSystem);

        // Create ground (static platform)
        _ground = _world.CreateEntity();
        _world.AddComponent(_ground, new TransformComponent { Position = new Vector2(400, 550) });
        _world.AddComponent(_ground, new ColliderComponent 
        { 
            Shape = ColliderShape.Rectangle, 
            Size = new Vector2(600, 20),
            Enabled = true
        });
        _world.AddComponent(_ground, new RigidBodyComponent
        {
            Mass = 0f, // Infinite mass (static)
            UseGravity = false,
            Restitution = 0.3f, // Some bounce
            Drag = 0.5f // Ground friction
        });
        _world.AddComponent(_ground, new RenderComponent 
        { 
            Color = new Color(100, 100, 100, 255),
            Shape = RenderShape.Rectangle
        });

        // Create physics body for ground
        _physicsSystem.CreateBodyForEntity(_world, _ground);

        _logger.Info("PhysicsDemo", "Physics demo loaded - click to spawn balls");
    }

    /// <inheritdoc/>
    public override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);

        // Early exit if not loaded yet (can happen during scene preloading)
        if (_inputBackend == null)
        {
            return;
        }

        if (_inputBackend.IsKeyPressed(Key.Escape))
        {
            PopScene();
            return;
        }

        var mousePos = _inputBackend.GetMousePosition();

        // Handle mouse input for dragging and spawning
        if (_inputBackend.IsMouseButtonPressed(MouseButton.Left))
        {
            // Check if clicking on a ball to drag
            Entity? clickedBall = null;
            foreach (var ball in _balls)
            {
                var transform = _world.GetComponent<TransformComponent>(ball);
                var collider = _world.GetComponent<ColliderComponent>(ball);
                var radius = collider.Size.X;
                
                var dx = mousePos.X - transform.Position.X;
                var dy = mousePos.Y - transform.Position.Y;
                var distanceSquared = dx * dx + dy * dy;
                
                if (distanceSquared <= radius * radius)
                {
                    clickedBall = ball;
                    break;
                }
            }

            if (clickedBall.HasValue)
            {
                // Start dragging
                _draggedBall = clickedBall.Value;
                ref var rigidBody = ref _world.GetComponent<RigidBodyComponent>(_draggedBall.Value);
                var transform = _world.GetComponent<TransformComponent>(_draggedBall.Value);
                
                _dragOffset = new Vector2(
                    mousePos.X - transform.Position.X,
                    mousePos.Y - transform.Position.Y
                );
                
                // Make kinematic during drag
                rigidBody.IsKinematic = true;
                rigidBody.Velocity = Vector2.Zero;
            }
            else if (_balls.Count < MAX_BALLS)
            {
                // Spawn new ball
                SpawnBallAt(mousePos);
            }
        }

        // Handle dragging
        if (_draggedBall.HasValue && _inputBackend.IsMouseButtonDown(MouseButton.Left))
        {
            ref var transform = ref _world.GetComponent<TransformComponent>(_draggedBall.Value);
            
            // Move to mouse position
            var desiredPos = new Vector2(mousePos.X - _dragOffset.X, mousePos.Y - _dragOffset.Y);
            transform.Position = desiredPos;
        }
        else if (_draggedBall.HasValue)
        {
            // Released the ball - restore dynamic physics
            ref var rigidBody = ref _world.GetComponent<RigidBodyComponent>(_draggedBall.Value);
            rigidBody.IsKinematic = false;
            rigidBody.Velocity = Vector2.Zero; // Drop with zero velocity
            
            _draggedBall = null;
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
        // Early exit if not loaded yet (can happen during scene preloading)
        if (_renderBackend == null)
        {
            return;
        }

        _renderBackend.Clear(new Color(20, 25, 35, 255));

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
        _renderBackend.DrawText("Physics Demo - Realistic Dynamics", new Vector2(20, 20), 20, Color.White);
        _renderBackend.DrawText($"Balls: {_balls.Count}/{MAX_BALLS}", new Vector2(20, 50), 16, new Color(180, 180, 180, 255));
        _renderBackend.DrawText("[Click] Spawn Ball | [Drag] Move Ball | [ESC] Back to Menu", new Vector2(10, 580), 14, new Color(150, 150, 150, 255));
    }

    /// <inheritdoc/>
    public override void OnUnload()
    {
        // Clean up physics bodies
        if (_physicsSystem != null)
        {
            foreach (var ball in _balls)
            {
                _physicsSystem.DestroyBodyForEntity(_world, ball);
            }
            _physicsSystem.DestroyBodyForEntity(_world, _ground);
            _physicsSystem.Shutdown();
        }
        base.OnUnload();
        _logger?.Info("PhysicsDemo", "Physics demo unloaded");
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
            Velocity = new Vector2(random.Next(-30, 30), 0),
            UseGravity = true,
            GravityScale = 1.0f,
            Drag = 0.05f, // Air resistance
            Restitution = 0.7f, // Better bounce
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

        // Create physics body
        _physicsSystem.CreateBodyForEntity(_world, ball);

        _balls.Add(ball);
    }
}

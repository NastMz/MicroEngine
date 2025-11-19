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
/// Demonstrates physics simulation using IPhysicsBackend with Aether.Physics2D.
/// Shows realistic rigid body dynamics, stacking, and proper collision resolution.
/// </summary>
public sealed class PhysicsBackendDemo : Scene
{
    private IInputBackend _inputBackend = null!;
    private IRenderBackend2D _renderBackend = null!;
    private ILogger _logger = null!;

    private World _world = null!;
    private PhysicsBackendSystem _physicsSystem = null!;

    private Entity _ground;
    private readonly List<Entity> _balls = new();
    private const int MAX_BALLS = 20;

    private Entity? _draggedBall;
    private Vector2 _dragOffset;

    /// <summary>
    /// Initializes a new instance of the <see cref="PhysicsBackendDemo"/> class.
    /// </summary>
    public PhysicsBackendDemo()
        : base("PhysicsBackendDemo")
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
        
        // Create physics backend system with Aether
        var aetherBackend = new AetherPhysicsBackend();
        _physicsSystem = new PhysicsBackendSystem(aetherBackend);
        _physicsSystem.Initialize(gravity: 750f); // Downward gravity
        
        _world.RegisterSystem(_physicsSystem);

        // Create ground (static platform)
        _ground = _world.CreateEntity();
        _world.AddComponent(_ground, new TransformComponent 
        { 
            Position = new Vector2(400, 550) 
        });
        _world.AddComponent(_ground, new ColliderComponent 
        { 
            Shape = ColliderShape.Rectangle, 
            Size = new Vector2(600, 20),
            Enabled = true
        });
        _world.AddComponent(_ground, new RigidBodyComponent
        {
            Mass = 0f, // Static body
            UseGravity = false,
            Restitution = 0.3f,
            Drag = 0.5f
        });
        _world.AddComponent(_ground, new RenderComponent 
        { 
            Color = new Color(100, 100, 100, 255),
            Shape = RenderShape.Rectangle
        });

        // Create physics body for ground
        _physicsSystem.CreateBodyForEntity(_world, _ground);

        _logger.Info("PhysicsBackendDemo", "Physics backend demo loaded - Click to spawn balls, Drag to move them");
    }

    /// <inheritdoc/>
    public override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);

        if (_inputBackend == null)
        {
            return;
        }

        if (_inputBackend.IsKeyPressed(Key.Escape))
        {
            PopScene();
            return;
        }

        // Handle mouse input for spawning and dragging
        HandleMouseInput();

        // Update world systems
        _world.Update(deltaTime);

        // Update ball count display
        var ballCount = _balls.Count;
        _logger.Debug("PhysicsBackendDemo", $"Balls: {ballCount}/{MAX_BALLS}");
    }

    /// <inheritdoc/>
    public override void OnRender()
    {
        base.OnRender();

        if (_renderBackend == null)
        {
            return;
        }

        _renderBackend.Clear(new Color(30, 30, 40, 255));

        // Render ground
        var groundTransform = _world.GetComponent<TransformComponent>(_ground);
        var groundCollider = _world.GetComponent<ColliderComponent>(_ground);
        var groundRender = _world.GetComponent<RenderComponent>(_ground);

        _renderBackend.DrawRectangle(
            new Vector2(
                groundTransform.Position.X - groundCollider.Size.X / 2,
                groundTransform.Position.Y - groundCollider.Size.Y / 2
            ),
            groundCollider.Size,
            groundRender.Color
        );

        // Render balls
        foreach (var ball in _balls)
        {
            if (!_world.HasComponent<TransformComponent>(ball) ||
                !_world.HasComponent<ColliderComponent>(ball) ||
                !_world.HasComponent<RenderComponent>(ball))
            {
                continue;
            }

            var transform = _world.GetComponent<TransformComponent>(ball);
            var collider = _world.GetComponent<ColliderComponent>(ball);
            var render = _world.GetComponent<RenderComponent>(ball);

            if (collider.Shape == ColliderShape.Circle)
            {
                _renderBackend.DrawCircle(
                    transform.Position,
                    collider.Size.X,
                    render.Color
                );
            }
        }

        // Instructions
        _renderBackend.DrawText("Physics Demo - Advanced Features", new Vector2(10, 10), 20, Color.White);
        _renderBackend.DrawText("[Click] Spawn Ball | [Drag] Move Ball | [ESC] Back to Menu", new Vector2(10, 40), 16, new Color(200, 200, 200, 255));
        _renderBackend.DrawText($"Balls: {_balls.Count}/{MAX_BALLS}", new Vector2(10, 560), 16, new Color(200, 200, 200, 255));
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
    }

    private void HandleMouseInput()
    {
        var mousePos = _inputBackend.GetMousePosition();

        // Start dragging
        if (_inputBackend.IsMouseButtonPressed(MouseButton.Left) && _draggedBall == null)
        {
            // Try to grab a ball
            foreach (var ball in _balls)
            {
                var transform = _world.GetComponent<TransformComponent>(ball);
                var collider = _world.GetComponent<ColliderComponent>(ball);
                var radius = collider.Size.X;

                var dx = mousePos.X - transform.Position.X;
                var dy = mousePos.Y - transform.Position.Y;
                var distSq = dx * dx + dy * dy;

                if (distSq <= radius * radius)
                {
                    _draggedBall = ball;
                    _dragOffset = new Vector2(
                        transform.Position.X - mousePos.X,
                        transform.Position.Y - mousePos.Y
                    );

                    // Make ball kinematic during drag
                    ref var rigidBody = ref _world.GetComponent<RigidBodyComponent>(ball);
                    rigidBody.IsKinematic = true;
                    rigidBody.Velocity = Vector2.Zero;
                    break;
                }
            }

            // No ball grabbed, spawn new one
            if (_draggedBall == null && _balls.Count < MAX_BALLS)
            {
                SpawnBall(mousePos);
            }
        }

        // During drag
        if (_inputBackend.IsMouseButtonDown(MouseButton.Left) && _draggedBall != null)
        {
            var desiredPos = new Vector2(
                mousePos.X + _dragOffset.X,
                mousePos.Y + _dragOffset.Y
            );

            ref var transform = ref _world.GetComponent<TransformComponent>(_draggedBall.Value);
            transform.Position = desiredPos;
        }

        // End drag
        if (_inputBackend.IsMouseButtonReleased(MouseButton.Left) && _draggedBall != null)
        {
            // Restore dynamic behavior
            ref var rigidBody = ref _world.GetComponent<RigidBodyComponent>(_draggedBall.Value);
            rigidBody.IsKinematic = false;

            _draggedBall = null;
        }
    }

    private void SpawnBall(Vector2 position)
    {
        var random = new Random();
        var ball = _world.CreateEntity();

        _world.AddComponent(ball, new TransformComponent 
        { 
            Position = position 
        });

        _world.AddComponent(ball, new ColliderComponent
        {
            Shape = ColliderShape.Circle,
            Size = new Vector2(15f, 15f), // radius
            Enabled = true
        });

        _world.AddComponent(ball, new RigidBodyComponent
        {
            Mass = 1.0f,
            Velocity = new Vector2(random.Next(-30, 31), random.Next(-30, 31)),
            UseGravity = true,
            Drag = 0.05f,
            Restitution = 0.7f, // Bouncy
            IsKinematic = false
        });

        _world.AddComponent(ball, new RenderComponent
        {
            Color = new Color(
                (byte)random.Next(100, 256),
                (byte)random.Next(100, 256),
                (byte)random.Next(100, 256),
                255
            ),
            Shape = RenderShape.Circle
        });

        // Create physics body
        _physicsSystem.CreateBodyForEntity(_world, ball);

        _balls.Add(ball);
    }
}

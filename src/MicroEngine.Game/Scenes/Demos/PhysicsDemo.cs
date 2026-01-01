using MicroEngine.Backend.Aether;
using Microsoft.Extensions.DependencyInjection;
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
    private IRenderer2D _renderer = null!;
    private ILogger _logger = null!;

    private PhysicsBackendSystem _physicsSystem = null!;
    private DragSystem _dragSystem = null!;

    private Entity _ground;
    private readonly List<Entity> _balls;
    private const int MAX_BALLS = 20;

    /// <summary>
    /// Initializes a new instance of the <see cref="PhysicsDemo"/> class.
    /// </summary>
    public PhysicsDemo()
        : base("PhysicsDemo")
    {
        _balls = new List<Entity>();
    }

    /// <inheritdoc/>
    public override void OnLoad(SceneContext context)
    {
        base.OnLoad(context);
        _inputBackend = context.InputBackend;
        _renderer = context.Renderer;
        _logger = context.Logger;

        // Get physics system from DI container
        _physicsSystem = context.Services.GetService<PhysicsBackendSystem>();
        _physicsSystem.Initialize(gravity: 750f); // Downward gravity
        
        // Create drag system
        _dragSystem = new DragSystem();
        
        World.RegisterSystem(_physicsSystem);

        // Create ground (static platform)
        _ground = World.CreateEntity();
        World.AddComponent(_ground, new TransformComponent { Position = new Vector2(400, 550) });
        World.AddComponent(_ground, new ColliderComponent 
        { 
            Shape = ColliderShape.Rectangle, 
            Size = new Vector2(600, 20),
            Enabled = true
        });
        World.AddComponent(_ground, new RigidBodyComponent
        {
            Mass = 0f, // Infinite mass (static)
            UseGravity = false,
            Restitution = 0.3f, // Some bounce
            Drag = 0.5f // Ground friction
        });
        World.AddComponent(_ground, new RenderComponent 
        { 
            Color = new Color(100, 100, 100, 255),
            Shape = RenderShape.Rectangle
        });

        // Create physics body for ground
        _physicsSystem.CreateBodyForEntity(World, _ground);

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

        // Translate input to drag commands
        if (_inputBackend.IsMouseButtonPressed(MouseButton.Left))
        {
            // Check if clicking on a draggable entity
            bool foundDraggable = false;
            var draggables = World.GetEntitiesWith<DraggableComponent>();

            foreach (var entity in draggables)
            {
                var transform = World.GetComponent<TransformComponent>(entity);
                var collider = World.GetComponent<ColliderComponent>(entity);

                if (IsPointInCollider(mousePos, transform, collider))
                {
                    ref var draggable = ref World.GetComponent<DraggableComponent>(entity);
                    draggable.StartDragRequested = true;
                    draggable.DragPosition = mousePos;
                    foundDraggable = true;
                    break;
                }
            }

            // If no draggable clicked, spawn new ball
            if (!foundDraggable && _balls.Count < MAX_BALLS)
            {
                SpawnBallAt(mousePos);
            }
        }

        // Update drag position
        if (_inputBackend.IsMouseButtonDown(MouseButton.Left))
        {
            var draggables = World.GetEntitiesWith<DraggableComponent>();
            foreach (var entity in draggables)
            {
                ref var draggable = ref World.GetComponent<DraggableComponent>(entity);
                if (draggable.IsDragging)
                {
                    draggable.DragPosition = mousePos;
                }
            }
        }

        // Stop drag
        if (_inputBackend.IsMouseButtonReleased(MouseButton.Left))
        {
            var draggables = World.GetEntitiesWith<DraggableComponent>();
            foreach (var entity in draggables)
            {
                ref var draggable = ref World.GetComponent<DraggableComponent>(entity);
                if (draggable.IsDragging)
                {
                    draggable.StopDragRequested = true;
                }
            }
        }

        // Process drag commands via system
        _dragSystem.Update(World, deltaTime);

        // Update physics
        _physicsSystem.Update(World, deltaTime);

        // Remove balls that fell off screen
        for (int i = _balls.Count - 1; i >= 0; i--)
        {
            var transform = World.GetComponent<TransformComponent>(_balls[i]);
            if (transform.Position.Y > 700)
            {
                World.DestroyEntity(_balls[i]);
                _balls.RemoveAt(i);
            }
        }
    }

    /// <inheritdoc/>
    public override void OnRender()
    {
        // Early exit if not loaded yet (can happen during scene preloading)
        if (_renderer == null)
        {
            return;
        }

        _renderer.Clear(new Color(20, 25, 35, 255));

        // Render ground
        var groundTransform = World.GetComponent<TransformComponent>(_ground);
        var groundCollider = World.GetComponent<ColliderComponent>(_ground);
        _renderer.DrawRectangle(
            new Vector2(groundTransform.Position.X - groundCollider.Size.X / 2, groundTransform.Position.Y - groundCollider.Size.Y / 2),
            groundCollider.Size,
            new Color(100, 100, 100, 255)
        );

        // Render balls
        foreach (var ball in _balls)
        {
            var transform = World.GetComponent<TransformComponent>(ball);
            var collider = World.GetComponent<ColliderComponent>(ball);
            var render = World.GetComponent<RenderComponent>(ball);

            _renderer.DrawCircle(transform.Position, collider.Size.X, render.Color);
        }

        // UI
        var layout = new TextLayoutHelper(_renderer, startX: 20, startY: 20, defaultLineHeight: 20);
        var infoColor = new Color(180, 180, 180, 255);
        var controlsColor = new Color(150, 150, 150, 255);

        layout.DrawText("Physics Demo - Realistic Dynamics", 20, Color.White)
              .AddSpacing(10)
              .DrawText($"Balls: {_balls.Count}/{MAX_BALLS}", 16, infoColor);

        layout.SetY(580)
              .DrawText("[Click] Spawn Ball | [Drag] Move Ball | [ESC] Back to Menu", 14, controlsColor);
    }

    /// <inheritdoc/>
    public override void OnUnload()
    {
        base.OnUnload();
        _logger?.Info("PhysicsDemo", "Physics demo unloaded");
    }

    private void SpawnBallAt(Vector2 position)
    {
        var ball = World.CreateEntity();
        var random = new Random();

        World.AddComponent(ball, new TransformComponent { Position = position });
        World.AddComponent(ball, new ColliderComponent 
        { 
            Shape = ColliderShape.Circle, 
            Size = new Vector2(15, 15),
            Enabled = true
        });
        World.AddComponent(ball, new RigidBodyComponent 
        { 
            Mass = 1.0f,
            Velocity = new Vector2(random.Next(-30, 30), 0),
            UseGravity = true,
            Drag = 0.05f, // Air resistance
            Restitution = 0.7f // Better bounce
        });

        var color = new Color(
            (byte)random.Next(100, 255),
            (byte)random.Next(100, 255),
            (byte)random.Next(100, 255),
            255
        );

        World.AddComponent(ball, new RenderComponent 
        { 
            Color = color,
            Shape = RenderShape.Circle
        });

        // Add DraggableComponent for drag interaction
        World.AddComponent(ball, new DraggableComponent
        {
            IsDragging = false,
            MakeKinematicOnDrag = true, // Physics bodies should be kinematic when dragged
            DragOffset = Vector2.Zero,
            StartDragRequested = false,
            DragPosition = Vector2.Zero,
            StopDragRequested = false
        });

        // Create physics body
        _physicsSystem.CreateBodyForEntity(World, ball);

        _balls.Add(ball);
    }

    private static bool IsPointInCollider(Vector2 point, TransformComponent transform, ColliderComponent collider)
    {
        if (collider.Shape == ColliderShape.Circle)
        {
            var dx = point.X - transform.Position.X;
            var dy = point.Y - transform.Position.Y;
            var distanceSquared = dx * dx + dy * dy;
            var radius = collider.Size.X;
            return distanceSquared <= radius * radius;
        }
        else if (collider.Shape == ColliderShape.Rectangle)
        {
            var halfWidth = collider.Size.X / 2f;
            var halfHeight = collider.Size.Y / 2f;
            return point.X >= transform.Position.X - halfWidth &&
                   point.X <= transform.Position.X + halfWidth &&
                   point.Y >= transform.Position.Y - halfHeight &&
                   point.Y <= transform.Position.Y + halfHeight;
        }
        return false;
    }
}


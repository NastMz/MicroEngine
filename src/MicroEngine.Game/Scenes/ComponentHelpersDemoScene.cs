using MicroEngine.Core.ECS;
using MicroEngine.Core.ECS.Components;
using MicroEngine.Core.ECS.Systems;
using MicroEngine.Core.Graphics;
using MicroEngine.Core.Input;
using MicroEngine.Core.Logging;
using MicroEngine.Core.Math;
using MicroEngine.Core.Scenes;

namespace MicroEngine.Game.Scenes;

/// <summary>
/// Demonstration scene showcasing PURE ECS architecture:
/// - Components are DATA ONLY (no methods)
/// - Systems contain ALL LOGIC
/// - PhysicsSystem handles forces, velocity, gravity
/// - MovementSystem handles translations
/// - CollisionSystem handles overlap detection
/// </summary>
public sealed class ComponentHelpersDemoScene : Scene
{
    private const string SCENE_NAME = "ComponentHelpersDemo";
    private const float JUMP_FORCE = -450f;
    private const float MOVE_SPEED = 200f;

    private readonly IInputBackend _inputBackend;
    private readonly IRenderBackend _renderBackend;
    private readonly ILogger _logger;
    private readonly World _world;

    // Systems (where ALL logic lives)
    private readonly Core.ECS.Systems.PhysicsSystem _physicsSystem;
    private readonly Core.ECS.Systems.MovementSystem _movementSystem;
    private readonly Core.ECS.Systems.CollisionSystem _collisionSystem;

    // Entities
    private Entity _player;
    private Entity _ground;
    private readonly List<Entity> _obstacles;
    private Entity _collectible;

    // Game state
    private int _score;
    private bool _showCollisionEffect;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentHelpersDemoScene"/> class.
    /// </summary>
    /// <param name="inputBackend">The input backend.</param>
    /// <param name="renderBackend">The render backend.</param>
    /// <param name="logger">The logger.</param>
    public ComponentHelpersDemoScene(IInputBackend inputBackend, IRenderBackend renderBackend, ILogger logger)
        : base(SCENE_NAME)
    {
        _inputBackend = inputBackend ?? throw new ArgumentNullException(nameof(inputBackend));
        _renderBackend = renderBackend ?? throw new ArgumentNullException(nameof(renderBackend));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _world = new World();
        _obstacles = new List<Entity>();

        // Create systems
        _physicsSystem = new Core.ECS.Systems.PhysicsSystem(new Vector2(0f, 980f));
        _movementSystem = new Core.ECS.Systems.MovementSystem();
        _collisionSystem = new Core.ECS.Systems.CollisionSystem();

        // Create entities with PURE DATA components
        CreatePlayer();
        CreateGround();
        CreateObstacles();
        CreateCollectible();

        _score = 0;
        _showCollisionEffect = false;
    }

    private void CreatePlayer()
    {
        _player = _world.CreateEntity();
        _world.AddComponent(_player, new TransformComponent
        {
            Position = new Vector2(100f, 300f),
            Rotation = 0f,
            Scale = new Vector2(1f, 1f),
            Origin = Vector2.Zero
        });
        _world.AddComponent(_player, new SpriteComponent
        {
            Tint = Color.Blue,
            Visible = true
        });
        _world.AddComponent(_player, new RigidBodyComponent
        {
            Velocity = Vector2.Zero,
            Acceleration = Vector2.Zero,
            Mass = 1f,
            Drag = 0f,
            GravityScale = 1f,
            IsKinematic = false,
            UseGravity = true
        });
        _world.AddComponent(_player, new ColliderComponent
        {
            Shape = ColliderShape.Rectangle,
            Size = new Vector2(32f, 32f),
            Offset = Vector2.Zero,
            IsTrigger = false,
            Enabled = true,
            LayerMask = -1
        });
    }

    private void CreateGround()
    {
        _ground = _world.CreateEntity();
        _world.AddComponent(_ground, new TransformComponent
        {
            Position = new Vector2(400f, 550f),
            Rotation = 0f,
            Scale = new Vector2(1f, 1f),
            Origin = Vector2.Zero
        });
        _world.AddComponent(_ground, new SpriteComponent
        {
            Tint = Color.Green,
            Visible = true
        });
        _world.AddComponent(_ground, new ColliderComponent
        {
            Shape = ColliderShape.Rectangle,
            Size = new Vector2(800f, 50f),
            Offset = Vector2.Zero,
            IsTrigger = false,
            Enabled = true,
            LayerMask = -1
        });
    }

    private void CreateObstacles()
    {
        _obstacles.Add(CreateObstacle(new Vector2(300f, 480f), Color.Red));
        _obstacles.Add(CreateObstacle(new Vector2(500f, 450f), new Color(255, 165, 0, 255)));
        _obstacles.Add(CreateObstacle(new Vector2(700f, 480f), Color.Red));
    }

    private Entity CreateObstacle(Vector2 position, Color color)
    {
        var obstacle = _world.CreateEntity();
        _world.AddComponent(obstacle, new TransformComponent
        {
            Position = position,
            Rotation = 0f,
            Scale = new Vector2(1f, 1f),
            Origin = Vector2.Zero
        });
        _world.AddComponent(obstacle, new SpriteComponent
        {
            Tint = color,
            Visible = true
        });
        _world.AddComponent(obstacle, new ColliderComponent
        {
            Shape = ColliderShape.Rectangle,
            Size = new Vector2(50f, 70f),
            Offset = Vector2.Zero,
            IsTrigger = false,
            Enabled = true,
            LayerMask = -1
        });
        return obstacle;
    }

    private void CreateCollectible()
    {
        _collectible = _world.CreateEntity();
        _world.AddComponent(_collectible, new TransformComponent
        {
            Position = new Vector2(600f, 400f),
            Rotation = 0f,
            Scale = new Vector2(1f, 1f),
            Origin = Vector2.Zero
        });
        _world.AddComponent(_collectible, new SpriteComponent
        {
            Tint = Color.Yellow,
            Visible = true
        });
        _world.AddComponent(_collectible, new ColliderComponent
        {
            Shape = ColliderShape.Circle,
            Size = new Vector2(16f, 16f),
            Offset = Vector2.Zero,
            IsTrigger = true,
            Enabled = true,
            LayerMask = -1
        });
    }

    /// <inheritdoc/>
    public override void OnLoad()
    {
        base.OnLoad();
        _logger.Info(SCENE_NAME, "Scene loaded using PURE ECS architecture with Systems");
    }

    /// <inheritdoc/>
    public override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);

        // Handle input -> uses SYSTEMS, not component methods
        HandleInput();

        // Physics system updates ALL entities automatically
        _physicsSystem.Update(_world, deltaTime);

        // Collision detection using CollisionSystem
        CheckCollisions();

        // Visual feedback
        if (_showCollisionEffect)
        {
            _showCollisionEffect = false;
        }
    }

    private void HandleInput()
    {
        // Jump: apply impulse using PhysicsSystem
        if (_inputBackend.IsKeyPressed(Key.Space))
        {
            var playerTransform = _world.GetComponent<TransformComponent>(_player);
            
            // Check if on ground using CollisionSystem
            var groundTransform = _world.GetComponent<TransformComponent>(_ground);
            var playerCollider = _world.GetComponent<ColliderComponent>(_player);
            var groundCollider = _world.GetComponent<ColliderComponent>(_ground);
            
            bool onGround = _collisionSystem.CheckOverlap(
                playerCollider, playerTransform.Position,
                groundCollider, groundTransform.Position);

            if (onGround)
            {
                _physicsSystem.ApplyImpulse(_world, _player, new Vector2(0f, JUMP_FORCE));
            }
        }

        // Move left/right: apply force using PhysicsSystem
        float horizontalInput = 0f;
        if (_inputBackend.IsKeyDown(Key.Left) || _inputBackend.IsKeyDown(Key.A))
        {
            horizontalInput = -1f;
        }
        if (_inputBackend.IsKeyDown(Key.Right) || _inputBackend.IsKeyDown(Key.D))
        {
            horizontalInput = 1f;
        }

        if (horizontalInput != 0f)
        {
            _physicsSystem.ApplyForce(_world, _player, new Vector2(horizontalInput * MOVE_SPEED, 0f));
        }
    }

    private void CheckCollisions()
    {
        var playerTransform = _world.GetComponent<TransformComponent>(_player);
        var playerCollider = _world.GetComponent<ColliderComponent>(_player);

        // Check ground collision
        var groundTransform = _world.GetComponent<TransformComponent>(_ground);
        var groundCollider = _world.GetComponent<ColliderComponent>(_ground);
        
        if (_collisionSystem.CheckOverlap(playerCollider, playerTransform.Position, 
                                           groundCollider, groundTransform.Position))
        {
            // Stop vertical velocity on ground contact
            var rigidBody = _world.GetComponent<RigidBodyComponent>(_player);
            if (rigidBody.Velocity.Y > 0)
            {
                _world.AddComponent(_player, new RigidBodyComponent
                {
                    Velocity = new Vector2(rigidBody.Velocity.X, 0f),
                    Acceleration = rigidBody.Acceleration,
                    Mass = rigidBody.Mass,
                    Drag = rigidBody.Drag,
                    GravityScale = rigidBody.GravityScale,
                    IsKinematic = rigidBody.IsKinematic,
                    UseGravity = rigidBody.UseGravity
                });

                // Position correction
                var bounds = _collisionSystem.GetBounds(playerCollider, playerTransform.Position);
                var groundBounds = _collisionSystem.GetBounds(groundCollider, groundTransform.Position);
                
                _world.AddComponent(_player, new TransformComponent
                {
                    Position = new Vector2(playerTransform.Position.X, groundBounds.Y - bounds.Height / 2),
                    Rotation = playerTransform.Rotation,
                    Scale = playerTransform.Scale,
                    Origin = playerTransform.Origin
                });
            }
        }

        // Check obstacle collisions
        foreach (var obstacle in _obstacles)
        {
            var obstacleTransform = _world.GetComponent<TransformComponent>(obstacle);
            var obstacleCollider = _world.GetComponent<ColliderComponent>(obstacle);

            if (_collisionSystem.CheckOverlap(playerCollider, playerTransform.Position,
                                               obstacleCollider, obstacleTransform.Position))
            {
                _showCollisionEffect = true;
            }
        }

        // Check collectible collision
        if (_world.HasComponent<ColliderComponent>(_collectible))
        {
            var collectTransform = _world.GetComponent<TransformComponent>(_collectible);
            var collectCollider = _world.GetComponent<ColliderComponent>(_collectible);

            if (_collisionSystem.CheckOverlap(playerCollider, playerTransform.Position,
                                               collectCollider, collectTransform.Position))
            {
                _score++;
                _world.RemoveComponent<ColliderComponent>(_collectible);
                _logger.Info(SCENE_NAME, $"Collectible gathered! Score: {_score}");
            }
        }
    }

    /// <inheritdoc/>
    public override void OnRender()
    {
        base.OnRender();

        // Render ground
        RenderEntity(_ground, new Vector2(800f, 50f));

        // Render obstacles
        foreach (var obstacle in _obstacles)
        {
            RenderEntity(obstacle, new Vector2(50f, 70f));
        }

        // Render collectible
        if (_world.HasComponent<SpriteComponent>(_collectible))
        {
            RenderEntity(_collectible, new Vector2(32f, 32f));
        }

        // Render player (with collision effect)
        var playerSprite = _world.GetComponent<SpriteComponent>(_player);
        var effectTint = _showCollisionEffect ? Color.Red : playerSprite.Tint;
        
        var tempSprite = playerSprite;
        tempSprite.Tint = effectTint;
        _world.AddComponent(_player, tempSprite);
        
        RenderEntity(_player, new Vector2(32f, 32f));
        
        _world.AddComponent(_player, playerSprite); // Restore

        // Render UI
        _renderBackend.DrawText($"Score: {_score}", new Vector2(10f, 10f), 20, Color.White);
        _renderBackend.DrawText("Arrow Keys / AD: Move", new Vector2(10f, 40f), 16, Color.White);
        _renderBackend.DrawText("Space: Jump", new Vector2(10f, 60f), 16, Color.White);
        _renderBackend.DrawText("ESC: Back to Menu", new Vector2(10f, 80f), 16, Color.White);
        _renderBackend.DrawText($"Player Pos: {_world.GetComponent<TransformComponent>(_player).Position}", 
                                 new Vector2(10f, 110f), 14, new Color(200, 200, 200, 255));
    }

    private void RenderEntity(Entity entity, Vector2 size)
    {
        var transform = _world.GetComponent<TransformComponent>(entity);
        var sprite = _world.GetComponent<SpriteComponent>(entity);

        if (!sprite.Visible)
        {
            return;
        }

        _renderBackend.DrawRectangle(
            new Vector2(transform.Position.X - size.X / 2, transform.Position.Y - size.Y / 2),
            size,
            sprite.Tint);
    }

    /// <inheritdoc/>
    public override void OnUnload()
    {
        base.OnUnload();
        _logger.Info(SCENE_NAME, "Scene unloaded");
    }
}

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
    private const float JUMP_FORCE = -800f;
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
    private bool _isGrounded;

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
            Drag = 5f,
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
        // Jump: apply impulse using PhysicsSystem (only if grounded)
        if (_inputBackend.IsKeyPressed(Key.Space) && _isGrounded)
        {
            _physicsSystem.ApplyImpulse(_world, _player, new Vector2(0f, JUMP_FORCE));
        }

        // Move left/right: set horizontal velocity directly
        ref var rigidBody = ref _world.GetComponent<RigidBodyComponent>(_player);
        
        float horizontalInput = 0f;
        if (_inputBackend.IsKeyDown(Key.Left) || _inputBackend.IsKeyDown(Key.A))
        {
            horizontalInput = -1f;
        }
        if (_inputBackend.IsKeyDown(Key.Right) || _inputBackend.IsKeyDown(Key.D))
        {
            horizontalInput = 1f;
        }

        // Set horizontal velocity directly (preserve vertical velocity)
        rigidBody.Velocity = new Vector2(horizontalInput * MOVE_SPEED, rigidBody.Velocity.Y);
    }

    private void CheckCollisions()
    {
        // Get CURRENT positions after physics update
        ref var playerTransform = ref _world.GetComponent<TransformComponent>(_player);
        var playerCollider = _world.GetComponent<ColliderComponent>(_player);

        // Check ground collision
        var groundTransform = _world.GetComponent<TransformComponent>(_ground);
        var groundCollider = _world.GetComponent<ColliderComponent>(_ground);
        
        // Get bounds for debugging
        var playerBounds = _collisionSystem.GetBounds(playerCollider, playerTransform.Position);
        var groundBounds = _collisionSystem.GetBounds(groundCollider, groundTransform.Position);
        
        // Check if player bottom is at or below ground top (with some tolerance)
        float playerBottom = playerBounds.Y + playerBounds.Height;
        float groundTop = groundBounds.Y;
        
        // Horizontal overlap check
        bool horizontalOverlap = playerBounds.X < groundBounds.X + groundBounds.Width &&
                                 playerBounds.X + playerBounds.Width > groundBounds.X;
        
        // Vertical collision check - player is on or penetrating ground
        bool verticalCollision = playerBottom >= groundTop && playerTransform.Position.Y < groundTransform.Position.Y + 50;
        
        bool touchingGround = horizontalOverlap && verticalCollision;
        _isGrounded = touchingGround;
        
        if (touchingGround)
        {
            ref var rigidBody = ref _world.GetComponent<RigidBodyComponent>(_player);
            
            // Stop vertical movement
            rigidBody.Velocity = new Vector2(rigidBody.Velocity.X, 0f);
            rigidBody.Acceleration = new Vector2(rigidBody.Acceleration.X, 0f);
            
            // Position player on top of ground
            playerTransform.Position = new Vector2(
                playerTransform.Position.X, 
                groundTop - playerCollider.Size.Y / 2);
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
                ref var collectSprite = ref _world.GetComponent<SpriteComponent>(_collectible);
                collectSprite.Visible = false;
                _logger.Info(SCENE_NAME, $"Collectible gathered! Score: {_score}");
            }
        }
    }

    /// <inheritdoc/>
    public override void OnRender()
    {
        base.OnRender();

        // Clear screen
        _renderBackend.Clear(Color.Black);

        // Render ground
        RenderEntity(_ground);

        // Render obstacles
        foreach (var obstacle in _obstacles)
        {
            RenderEntity(obstacle);
        }

        // Render collectible (only if it still has collider)
        if (_world.HasComponent<ColliderComponent>(_collectible))
        {
            RenderEntity(_collectible);
        }

        // Render player (with collision effect)
        ref var playerSprite = ref _world.GetComponent<SpriteComponent>(_player);
        var originalTint = playerSprite.Tint;
        
        if (_showCollisionEffect)
        {
            playerSprite.Tint = Color.Red;
        }
        
        RenderEntity(_player);
        
        playerSprite.Tint = originalTint; // Restore

        // Render UI
        _renderBackend.DrawText($"Score: {_score}", new Vector2(10f, 10f), 20, Color.White);
        _renderBackend.DrawText("Arrow Keys / AD: Move", new Vector2(10f, 40f), 16, Color.White);
        _renderBackend.DrawText("Space: Jump", new Vector2(10f, 60f), 16, Color.White);
        _renderBackend.DrawText("ESC: Back to Menu", new Vector2(10f, 80f), 16, Color.White);
        _renderBackend.DrawText($"Player Pos: {_world.GetComponent<TransformComponent>(_player).Position}", 
                                 new Vector2(10f, 110f), 14, new Color(200, 200, 200, 255));
    }

    private void RenderEntity(Entity entity)
    {
        var transform = _world.GetComponent<TransformComponent>(entity);
        var sprite = _world.GetComponent<SpriteComponent>(entity);
        var collider = _world.GetComponent<ColliderComponent>(entity);

        if (!sprite.Visible)
        {
            return;
        }

        // Use collider size for rendering
        // For circles, Size.X is the radius, so we need diameter for rendering
        var size = collider.Shape == ColliderShape.Circle 
            ? new Vector2(collider.Size.X * 2, collider.Size.X * 2) 
            : collider.Size;
        
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

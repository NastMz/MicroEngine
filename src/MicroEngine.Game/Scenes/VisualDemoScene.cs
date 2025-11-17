using MicroEngine.Backend.Raylib;
using MicroEngine.Core.ECS;
using MicroEngine.Core.Graphics;
using MicroEngine.Core.Input;
using MicroEngine.Core.Logging;
using MicroEngine.Core.Math;
using MicroEngine.Core.Scenes;

namespace MicroEngine.Game.Scenes;

/// <summary>
/// Visual demo scene showcasing ECS with Raylib rendering and input.
/// </summary>
public class VisualDemoScene : Scene
{
    private readonly ILogger _logger;
    private readonly IRenderBackend _renderer;
    private readonly IInputBackend _input;
    private RenderSystem? _renderSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="VisualDemoScene"/> class.
    /// </summary>
    public VisualDemoScene(ILogger logger, IRenderBackend renderer, IInputBackend input)
        : base("Visual Demo")
    {
        _logger = logger;
        _renderer = renderer;
        _input = input;
    }

    /// <inheritdoc/>
    public override void OnLoad()
    {
        base.OnLoad();

        _logger.Info("Scene", "Visual Demo Scene loaded");

        RegisterSystems();
        CreateEntities();
    }

    private void RegisterSystems()
    {
        World.RegisterSystem(new VisualMovementSystem());
        World.RegisterSystem(new PlayerInputSystem(_input));
        World.RegisterSystem(new BoundsCheckSystem(_renderer.WindowWidth, _renderer.WindowHeight));
        
        // Store RenderSystem separately to call it between BeginFrame/EndFrame
        _renderSystem = new RenderSystem(_renderer);
    }

    private void CreateEntities()
    {
        CreatePlayer();
        CreateEnemies(5);
        CreateUI();

        _logger.Info("Scene", $"Created {World.EntityCount} visual entities");
    }

    private void CreatePlayer()
    {
        var player = World.CreateEntity("Player");

        World.AddComponent(player, new VisualPositionComponent { X = 400f, Y = 300f });
        World.AddComponent(player, new VisualVelocityComponent { X = 0f, Y = 0f });
        World.AddComponent(player, new SizeComponent { Width = 30f, Height = 30f });
        World.AddComponent(player, new ColorComponent { R = 0, G = 255, B = 0, A = 255 });
        World.AddComponent(player, new PlayerTag());
    }

    private void CreateEnemies(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var enemy = World.CreateEntity($"Enemy_{i}");

            var x = Random.Shared.Next(50, _renderer.WindowWidth - 50);
            var y = Random.Shared.Next(50, _renderer.WindowHeight - 50);
            var vx = (Random.Shared.NextSingle() - 0.5f) * 100f;
            var vy = (Random.Shared.NextSingle() - 0.5f) * 100f;

            World.AddComponent(enemy, new VisualPositionComponent { X = x, Y = y });
            World.AddComponent(enemy, new VisualVelocityComponent { X = vx, Y = vy });
            World.AddComponent(enemy, new SizeComponent { Width = 20f, Height = 20f });
            World.AddComponent(enemy, new ColorComponent { R = 255, G = 0, B = 0, A = 255 });
        }
    }

    private void CreateUI()
    {
        var ui = World.CreateEntity("UI");
        World.AddComponent(ui, new UITag());
    }

    /// <inheritdoc/>
    public override void OnRender()
    {
        // Execute only the RenderSystem (between BeginFrame and EndFrame)
        _renderSystem?.Update(World, 0f);
    }

    /// <inheritdoc/>
    public override void OnUnload()
    {
        _logger.Info("Scene", "Visual Demo Scene unloaded");
        base.OnUnload();
    }

    #region Components

    #pragma warning disable CS1591, IDE0044, S1104

    public struct VisualPositionComponent : IComponent
    {
        public float X;
        public float Y;
    }

    public struct VisualVelocityComponent : IComponent
    {
        public float X;
        public float Y;
    }

    public struct SizeComponent : IComponent
    {
        public float Width;
        public float Height;
    }

    public struct ColorComponent : IComponent
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;
    }

    public struct PlayerTag : IComponent { }
    public struct UITag : IComponent { }

    #pragma warning restore CS1591, IDE0044, S1104

    #endregion

    #region Systems

#pragma warning disable CS1591

    public class VisualMovementSystem : ISystem
    {
        public void Update(World world, float deltaTime)
        {
            foreach (var entity in world.GetEntitiesWith<VisualVelocityComponent>())
            {
                if (!world.HasComponent<VisualPositionComponent>(entity))
                {
                    continue;
                }

                ref var pos = ref world.GetComponent<VisualPositionComponent>(entity);
                ref var vel = ref world.GetComponent<VisualVelocityComponent>(entity);

                pos.X += vel.X * deltaTime;
                pos.Y += vel.Y * deltaTime;
            }
        }
    }

    public class PlayerInputSystem : ISystem
    {
        private readonly IInputBackend _input;
        private const float PLAYER_SPEED = 200f;

        public PlayerInputSystem(IInputBackend input)
        {
            _input = input;
        }

        public void Update(World world, float deltaTime)
        {
            foreach (var entity in world.GetEntitiesWith<PlayerTag>())
            {
                ref var vel = ref world.GetComponent<VisualVelocityComponent>(entity);

                vel.X = 0f;
                vel.Y = 0f;

                if (_input.IsKeyDown(Key.Right) || _input.IsKeyDown(Key.D))
                {
                    vel.X = PLAYER_SPEED;
                }
                if (_input.IsKeyDown(Key.Left) || _input.IsKeyDown(Key.A))
                {
                    vel.X = -PLAYER_SPEED;
                }
                if (_input.IsKeyDown(Key.Down) || _input.IsKeyDown(Key.S))
                {
                    vel.Y = PLAYER_SPEED;
                }
                if (_input.IsKeyDown(Key.Up) || _input.IsKeyDown(Key.W))
                {
                    vel.Y = -PLAYER_SPEED;
                }
            }
        }
    }

    public class BoundsCheckSystem : ISystem
    {
        private readonly float _width;
        private readonly float _height;

        public BoundsCheckSystem(float width, float height)
        {
            _width = width;
            _height = height;
        }

        public void Update(World world, float deltaTime)
        {
            foreach (var entity in world.GetEntitiesWith<VisualPositionComponent>())
            {
                if (!world.HasComponent<VisualVelocityComponent>(entity) ||
                    !world.HasComponent<SizeComponent>(entity))
                {
                    continue;
                }

                ref var pos = ref world.GetComponent<VisualPositionComponent>(entity);
                ref var vel = ref world.GetComponent<VisualVelocityComponent>(entity);
                ref var size = ref world.GetComponent<SizeComponent>(entity);

                if (pos.X < 0 || pos.X + size.Width > _width)
                {
                    vel.X = -vel.X;
                    pos.X = Math.Clamp(pos.X, 0f, _width - size.Width);
                }

                if (pos.Y < 0 || pos.Y + size.Height > _height)
                {
                    vel.Y = -vel.Y;
                    pos.Y = Math.Clamp(pos.Y, 0f, _height - size.Height);
                }
            }
        }
    }

    public class RenderSystem : ISystem
    {
        private readonly IRenderBackend _renderer;

        public RenderSystem(IRenderBackend renderer)
        {
            _renderer = renderer;
        }

        public void Update(World world, float deltaTime)
        {
            _renderer.Clear(Color.Black);

            foreach (var entity in world.GetEntitiesWith<VisualPositionComponent>())
            {
                if (!world.HasComponent<SizeComponent>(entity) ||
                    !world.HasComponent<ColorComponent>(entity) ||
                    world.HasComponent<UITag>(entity))
                {
                    continue;
                }

                ref var pos = ref world.GetComponent<VisualPositionComponent>(entity);
                ref var size = ref world.GetComponent<SizeComponent>(entity);
                ref var col = ref world.GetComponent<ColorComponent>(entity);

                var color = new Color(col.R, col.G, col.B, col.A);
                _renderer.DrawRectangle(
                    new Vector2(pos.X, pos.Y),
                    new Vector2(size.Width, size.Height),
                    color
                );
            }

            RenderUI(world);
        }

        private void RenderUI(World world)
        {
            var fps = _renderer.GetFPS();
            var entityCount = world.GetAllEntities().Count();

            _renderer.DrawText($"FPS: {fps}", new Vector2(10, 10), 20, Color.White);
            _renderer.DrawText($"Entities: {entityCount}", new Vector2(10, 35), 20, Color.White);
            _renderer.DrawText("WASD/Arrows to move", new Vector2(10, 60), 20, Color.LightGray);
        }
    }

#pragma warning restore CS1591

    #endregion
}

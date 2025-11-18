using MicroEngine.Core.Graphics;
using MicroEngine.Core.Input;
using MicroEngine.Core.Logging;
using MicroEngine.Core.Math;
using MicroEngine.Core.Scenes;

namespace MicroEngine.Game.Scenes;

/// <summary>
/// Demonstrates sprite batching system with performance comparison.
/// Shows difference between batched vs non-batched rendering.
/// </summary>
public class SpriteBatchDemoScene : Scene
{
    private const int SPRITE_COUNT = 1000;
    private const float SPRITE_SIZE = 16f;
    private const float MOVE_SPEED = 100f;

    private readonly ILogger _logger;
    private readonly IInputBackend _input;
    private readonly IRenderBackend _render;

    private readonly List<SpriteData> _sprites;
    private ISpriteBatch? _spriteBatch;
    private bool _useBatching = true;
    private SpriteSortMode _sortMode = SpriteSortMode.Texture;
    private readonly System.Random _random;

    private struct SpriteData
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Color Tint;
        public float Rotation;
        public float RotationSpeed;
        public float LayerDepth;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpriteBatchDemoScene"/> class.
    /// </summary>
    public SpriteBatchDemoScene(
        ILogger logger,
        IInputBackend input,
        IRenderBackend render)
        : base("SpriteBatchDemo")
    {
        _logger = logger;
        _input = input;
        _render = render;
        _sprites = new List<SpriteData>(SPRITE_COUNT);
        _random = new System.Random(42);
    }

    /// <inheritdoc/>
    public override void OnLoad()
    {
        _logger.Info(Name, "=== Sprite Batch Demo Scene ===");
        _logger.Info(Name, $"Spawning {SPRITE_COUNT} sprites...");
        _logger.Info(Name, "Controls:");
        _logger.Info(Name, "  B - Toggle batching ON/OFF");
        _logger.Info(Name, "  S - Cycle sort modes");
        _logger.Info(Name, "  R - Respawn sprites");
        _logger.Info(Name, "  ESC - Exit");

        // Create sprite batch
        _spriteBatch = new SpriteBatch(_render, SPRITE_COUNT);

        // Spawn sprites
        SpawnSprites();

        _logger.Info(Name, $"Scene loaded - Batching: {(_useBatching ? "ON" : "OFF")}, Sort: {_sortMode}");
    }

    private void SpawnSprites()
    {
        _sprites.Clear();

        for (var i = 0; i < SPRITE_COUNT; i++)
        {
            var sprite = new SpriteData
            {
                Position = new Vector2(
                    _random.Next(0, _render.WindowWidth),
                    _random.Next(0, _render.WindowHeight)),
                Velocity = new Vector2(
                    (_random.NextSingle() - 0.5f) * MOVE_SPEED * 2f,
                    (_random.NextSingle() - 0.5f) * MOVE_SPEED * 2f),
                Tint = new Color(
                    (byte)_random.Next(100, 256),
                    (byte)_random.Next(100, 256),
                    (byte)_random.Next(100, 256),
                    255),
                Rotation = _random.NextSingle() * 360f,
                RotationSpeed = (_random.NextSingle() - 0.5f) * 180f,
                LayerDepth = _random.NextSingle()
            };

            _sprites.Add(sprite);
        }

        _logger.Info(Name, $"Spawned {_sprites.Count} sprites");
    }

    /// <inheritdoc/>
    public override void OnUpdate(float deltaTime)
    {
        // Handle input
        if (_input.IsKeyPressed(Key.Escape))
        {
            _logger.Info(Name, "Exiting sprite batch demo");
            return;
        }

        if (_input.IsKeyPressed(Key.B))
        {
            _useBatching = !_useBatching;
            _logger.Info(Name, $"Batching: {(_useBatching ? "ON" : "OFF")}");
        }

        if (_input.IsKeyPressed(Key.S))
        {
            CycleSortMode();
        }

        if (_input.IsKeyPressed(Key.R))
        {
            SpawnSprites();
        }

        // Update sprites
        for (var i = 0; i < _sprites.Count; i++)
        {
            var sprite = _sprites[i];

            // Move
            sprite.Position += sprite.Velocity * deltaTime;

            // Bounce off walls
            if (sprite.Position.X < 0 || sprite.Position.X > _render.WindowWidth)
            {
                sprite.Velocity = new Vector2(-sprite.Velocity.X, sprite.Velocity.Y);
                sprite.Position = new Vector2(
                    System.Math.Clamp(sprite.Position.X, 0, _render.WindowWidth),
                    sprite.Position.Y);
            }

            if (sprite.Position.Y < 0 || sprite.Position.Y > _render.WindowHeight)
            {
                sprite.Velocity = new Vector2(sprite.Velocity.X, -sprite.Velocity.Y);
                sprite.Position = new Vector2(
                    sprite.Position.X,
                    System.Math.Clamp(sprite.Position.Y, 0, _render.WindowHeight));
            }

            // Rotate
            sprite.Rotation += sprite.RotationSpeed * deltaTime;

            _sprites[i] = sprite;
        }
    }

    /// <inheritdoc/>
    public override void OnRender()
    {
        _render.Clear(Color.DarkGray);

        // Render sprites
        if (_useBatching && _spriteBatch != null)
        {
            RenderWithBatching();
        }
        else
        {
            RenderWithoutBatching();
        }

        // Render UI
        RenderUI();
    }

    private void RenderWithBatching()
    {
        if (_spriteBatch == null) return;

        _spriteBatch.Begin(_sortMode);

        foreach (var sprite in _sprites)
        {
            // For demo purposes, we draw simple colored rectangles
            // In real usage, you'd use actual textures
            _render.DrawRectangle(
                sprite.Position,
                new Vector2(SPRITE_SIZE, SPRITE_SIZE),
                sprite.Tint);
        }

        _spriteBatch.End();
    }

    private void RenderWithoutBatching()
    {
        foreach (var sprite in _sprites)
        {
            _render.DrawRectangle(
                sprite.Position,
                new Vector2(SPRITE_SIZE, SPRITE_SIZE),
                sprite.Tint);
        }
    }

    private void RenderUI()
    {
        const int LINE_HEIGHT = 22;
        var y = 10;

        // Title
        _render.DrawText("MicroEngine - Sprite Batch Demo", new Vector2(10, y), 20, Color.Yellow);
        y += LINE_HEIGHT + 10;

        // Stats
        _render.DrawText($"Sprites: {_sprites.Count}", new Vector2(10, y), 16, Color.White);
        y += LINE_HEIGHT;
        _render.DrawText($"Batching: {(_useBatching ? "ON" : "OFF")} [B to toggle]", new Vector2(10, y), 16, Color.White);
        y += LINE_HEIGHT;
        _render.DrawText($"Sort Mode: {_sortMode} [S to cycle]", new Vector2(10, y), 16, Color.White);
        y += LINE_HEIGHT;
        _render.DrawText("R: Respawn | ESC: Exit", new Vector2(10, y), 16, Color.LightGray);

        // FPS (top right)
        _render.DrawText($"FPS: {_render.GetFPS()}", new Vector2(_render.WindowWidth - 100, 10), 18, Color.Green);

        // Performance note
        y = _render.WindowHeight - 60;
        _render.DrawText("Note: Batching reduces draw calls for better performance", new Vector2(10, y), 14, Color.Yellow);
        y += 18;
        _render.DrawText("with many sprites sharing the same texture.", new Vector2(10, y), 14, Color.Yellow);
    }

    private void CycleSortMode()
    {
        _sortMode = _sortMode switch
        {
            SpriteSortMode.Deferred => SpriteSortMode.Texture,
            SpriteSortMode.Texture => SpriteSortMode.BackToFront,
            SpriteSortMode.BackToFront => SpriteSortMode.FrontToBack,
            SpriteSortMode.FrontToBack => SpriteSortMode.Immediate,
            SpriteSortMode.Immediate => SpriteSortMode.Deferred,
            _ => SpriteSortMode.Deferred
        };

        _logger.Info(Name, $"Sort mode changed to: {_sortMode}");
    }
}

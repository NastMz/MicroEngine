using MicroEngine.Core.Graphics;
using MicroEngine.Core.Input;
using MicroEngine.Core.Logging;
using MicroEngine.Core.Math;
using MicroEngine.Core.Resources;
using MicroEngine.Core.Scenes;

namespace MicroEngine.Game.Scenes.Demos;

/// <summary>
/// Graphics demo showcasing camera controls, sprite rendering, and resource management.
/// Demonstrates Camera2D, real texture loading, sprite atlas, and camera movement.
/// </summary>
public sealed class GraphicsDemo : Scene
{
    private readonly IInputBackend _inputBackend;
    private readonly IRenderBackend _renderBackend;
    private readonly ILogger _logger;
    private readonly ResourceCache<ITexture> _textureCache;
    private readonly Random _random;

    private Camera2D _camera = null!;
    private readonly List<SpriteEntity> _sprites;
    private readonly List<LoadedSprite> _loadedSprites;
    private TextureFilter _currentFilter;

    private const float CAMERA_SPEED = 300f;
    private const float ZOOM_SPEED = 1f;
    private const int WORLD_SIZE = 2000;
    private const int GRID_SPACING = 100;

    private static readonly string[] SPRITE_FILES = ["player.png", "enemy.png", "coin.png", "star.png", "box.png", "heart.png"];

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphicsDemo"/> class.
    /// </summary>
    public GraphicsDemo()
        : base("GraphicsDemo")
    {
        _inputBackend = Program.InputBackend;
        _renderBackend = Program.RenderBackend;
        _logger = Program.Logger;
        _textureCache = Program.TextureCache;
        _random = new Random();
        _sprites = new List<SpriteEntity>();
        _loadedSprites = new List<LoadedSprite>();
    }

    /// <inheritdoc/>
    public override void OnLoad()
    {
        base.OnLoad();
        _logger.Info("GraphicsDemo", "Graphics demo loaded");

        // Initialize camera at center with proper screen offset
        var screenCenterX = _renderBackend.WindowWidth / 2f;
        var screenCenterY = _renderBackend.WindowHeight / 2f;

        _camera = new Camera2D
        {
            Position = new Vector2(WORLD_SIZE / 2f, WORLD_SIZE / 2f),
            Offset = new Vector2(screenCenterX, screenCenterY),
            Rotation = 0f,
            Zoom = 1f
        };

        _logger.Info("GraphicsDemo", $"Camera initialized at ({_camera.Position.X}, {_camera.Position.Y}) with offset ({_camera.Offset.X}, {_camera.Offset.Y})");

        // Load sprite textures
        LoadAssets();

        // Generate random sprites across the world
        GenerateSprites(50);
    }

    /// <inheritdoc/>
    public override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);

        // Exit
        if (_inputBackend.IsKeyPressed(Key.Escape))
        {
            Program.SceneManager.PopScene();
            return;
        }

        // Camera movement (WASD)
        float movementX = 0f;
        float movementY = 0f;
        if (_inputBackend.IsKeyDown(Key.W)) { movementY -= 1f; }
        if (_inputBackend.IsKeyDown(Key.S)) { movementY += 1f; }
        if (_inputBackend.IsKeyDown(Key.A)) { movementX -= 1f; }
        if (_inputBackend.IsKeyDown(Key.D)) { movementX += 1f; }

        if (System.Math.Abs(movementX) > 0.01f || System.Math.Abs(movementY) > 0.01f)
        {
            var movement = new Vector2(movementX, movementY);
            float length = (float)System.Math.Sqrt(movement.X * movement.X + movement.Y * movement.Y);
            if (length > 0f)
            {
                movement = new Vector2(movement.X / length, movement.Y / length);
            }

            _camera.Position = new Vector2(
                _camera.Position.X + movement.X * CAMERA_SPEED * deltaTime / _camera.Zoom,
                _camera.Position.Y + movement.Y * CAMERA_SPEED * deltaTime / _camera.Zoom);
        }

        // Camera zoom (Q/E)
        if (_inputBackend.IsKeyDown(Key.Q))
        {
            _camera.Zoom = System.Math.Max(0.25f, _camera.Zoom - ZOOM_SPEED * deltaTime);
        }

        if (_inputBackend.IsKeyDown(Key.E))
        {
            _camera.Zoom = System.Math.Min(4f, _camera.Zoom + ZOOM_SPEED * deltaTime);
        }

        // Reset camera (R)
        if (_inputBackend.IsKeyPressed(Key.R))
        {
            _camera.Position = new Vector2(WORLD_SIZE / 2f, WORLD_SIZE / 2f);
            _camera.Zoom = 1f;
            _camera.Rotation = 0f;
        }

        // Regenerate sprites (Space)
        if (_inputBackend.IsKeyPressed(Key.Space))
        {
            GenerateSprites(50);
        }

        // Texture filtering controls (F1-F4)
        if (_inputBackend.IsKeyPressed(Key.F1))
        {
            SetTextureFilter(TextureFilter.Point);
        }

        if (_inputBackend.IsKeyPressed(Key.F2))
        {
            SetTextureFilter(TextureFilter.Bilinear);
        }

        if (_inputBackend.IsKeyPressed(Key.F3))
        {
            SetTextureFilter(TextureFilter.Trilinear);
        }

        if (_inputBackend.IsKeyPressed(Key.F4))
        {
            SetTextureFilter(TextureFilter.Anisotropic16X);
        }

        // Generate mipmaps (M key)
        if (_inputBackend.IsKeyPressed(Key.M))
        {
            GenerateAllMipmaps();
        }

        // Update sprite animations (rotate)
        foreach (var sprite in _sprites)
        {
            sprite.Rotation += sprite.RotationSpeed * deltaTime;
        }
    }

    /// <inheritdoc/>
    public override void OnRender()
    {
        _renderBackend.Clear(new Color(20, 20, 30, 255));

        // Begin camera mode
        _renderBackend.BeginCamera2D(_camera);

        // Draw grid
        DrawGrid();

        // Draw world border
        DrawWorldBorder();

        // DEBUG: Draw camera center marker
        _renderBackend.DrawRectangle(
            new Vector2(_camera.Position.X - 50, _camera.Position.Y - 5),
            new Vector2(100, 10),
            new Color(255, 0, 0, 255));
        _renderBackend.DrawRectangle(
            new Vector2(_camera.Position.X - 5, _camera.Position.Y - 50),
            new Vector2(10, 100),
            new Color(255, 0, 0, 255));

        // Draw sprites using loaded textures
        foreach (var sprite in _sprites)
        {
            if (sprite.SpriteIndex >= 0 && sprite.SpriteIndex < _loadedSprites.Count)
            {
                var loadedSprite = _loadedSprites[sprite.SpriteIndex];
                var textureWidth = loadedSprite.Texture.Width;
                var textureHeight = loadedSprite.Texture.Height;

                var sourceRect = new Rectangle(0, 0, textureWidth, textureHeight);
                var destRect = new Rectangle(
                    sprite.Position.X,
                    sprite.Position.Y,
                    textureWidth * sprite.Scale,
                    textureHeight * sprite.Scale);

                var origin = new Vector2(textureWidth / 2f * sprite.Scale, textureHeight / 2f * sprite.Scale);

                _renderBackend.DrawTexturePro(
                    loadedSprite.Texture,
                    sourceRect,
                    destRect,
                    origin,
                    sprite.Rotation,
                    Color.White);
            }
        }

        // End camera mode
        _renderBackend.EndCamera2D();

        // UI (screen space)
        DrawUI();
    }

    /// <inheritdoc/>
    public override void OnUnload()
    {
        base.OnUnload();
        _logger.Info("GraphicsDemo", "Graphics demo unloaded");
    }

    private void LoadAssets()
    {
        _loadedSprites.Clear();

        foreach (var spriteFile in SPRITE_FILES)
        {
            try
            {
                var texture = _textureCache.Load($"assets/textures/{spriteFile}");
                _loadedSprites.Add(new LoadedSprite { Name = spriteFile, Texture = texture });
                _logger.Info("GraphicsDemo", $"Loaded sprite: {spriteFile}");
            }
            catch (Exception ex)
            {
                _logger.Warn("GraphicsDemo", $"Failed to load {spriteFile}: {ex.Message}");
            }
        }

        if (_loadedSprites.Count == 0)
        {
            _logger.Error("GraphicsDemo", "No sprites loaded. Demo will run without sprites.");
        }
        else
        {
            _logger.Info("GraphicsDemo", $"Successfully loaded {_loadedSprites.Count} sprites");
        }
    }

    private void GenerateSprites(int count)
    {
        _sprites.Clear();

        if (_loadedSprites.Count == 0)
        {
            _logger.Warn("GraphicsDemo", "Cannot generate sprites: no textures loaded");
            return;
        }

        // Generate sprites around camera center for better visibility
        var centerX = WORLD_SIZE / 2f;
        var centerY = WORLD_SIZE / 2f;
        var spawnRadius = 1200f; // Spawn within wider area for better filter comparison

        for (int i = 0; i < count; i++)
        {
            // Varied scales to showcase filtering: 0.5x, 1x, 2x, 4x
            float scale;
            switch (i % 4)
            {
                case 0:
                    scale = 4f;
                    break;
                case 1:
                    scale = 2f;
                    break;
                case 2:
                    scale = 1f;
                    break;
                default:
                    scale = 0.5f;
                    break;
            }

            // Random position around center
            var angle = (float)(_random.NextDouble() * System.Math.PI * 2);
            var distance = (float)(_random.NextDouble() * spawnRadius);
            var offsetX = (float)(System.Math.Cos(angle) * distance);
            var offsetY = (float)(System.Math.Sin(angle) * distance);

            var sprite = new SpriteEntity
            {
                Position = new Vector2(centerX + offsetX, centerY + offsetY),
                SpriteIndex = _random.Next(_loadedSprites.Count),
                Rotation = (float)(_random.NextDouble() * 360), // Random rotation
                RotationSpeed = (float)(_random.NextDouble() * 60 - 30),
                Scale = scale
            };

            _sprites.Add(sprite);
        }

        _logger.Info("GraphicsDemo", $"Generated {count} sprites (first sprite at {_sprites[0].Position.X:F0}, {_sprites[0].Position.Y:F0})");
    }

    private void DrawGrid()
    {
        // Draw grid using simple rectangles
        var gridColor = new Color(50, 50, 50, 255);
        for (int x = 0; x < WORLD_SIZE; x += GRID_SPACING)
        {
            for (int y = 0; y < WORLD_SIZE; y += GRID_SPACING)
            {
                _renderBackend.DrawRectangle(new Vector2(x, y), new Vector2(2, 2), gridColor);
            }
        }
    }

    private void DrawWorldBorder()
    {
        // Draw world bounds as thick border
        var borderColor = new Color(100, 100, 150, 255);
        const float BORDER_THICKNESS = 4f;

        // Top
        _renderBackend.DrawRectangle(
            new Vector2(0, 0),
            new Vector2(WORLD_SIZE, BORDER_THICKNESS),
            borderColor);

        // Bottom
        _renderBackend.DrawRectangle(
            new Vector2(0, WORLD_SIZE - BORDER_THICKNESS),
            new Vector2(WORLD_SIZE, BORDER_THICKNESS),
            borderColor);

        // Left
        _renderBackend.DrawRectangle(
            new Vector2(0, 0),
            new Vector2(BORDER_THICKNESS, WORLD_SIZE),
            borderColor);

        // Right
        _renderBackend.DrawRectangle(
            new Vector2(WORLD_SIZE - BORDER_THICKNESS, 0),
            new Vector2(BORDER_THICKNESS, WORLD_SIZE),
            borderColor);
    }

    private void DrawUI()
    {
        const int UI_X = 10;
        int uiY = 10;
        const int LINE_HEIGHT = 20;
        var textColor = new Color(200, 200, 200, 255);
        var titleColor = new Color(100, 200, 255, 255);

        _renderBackend.DrawText("Graphics & Camera Demo", new Vector2(UI_X, uiY), 20, titleColor);
        uiY += LINE_HEIGHT + 10;

        _renderBackend.DrawText($"Camera: ({_camera.Position.X:F0}, {_camera.Position.Y:F0})", new Vector2(UI_X, uiY), 14, textColor);
        uiY += LINE_HEIGHT;

        _renderBackend.DrawText($"Zoom: {_camera.Zoom:F2}x", new Vector2(UI_X, uiY), 14, textColor);
        uiY += LINE_HEIGHT;

        _renderBackend.DrawText($"Sprites: {_sprites.Count}", new Vector2(UI_X, uiY), 14, textColor);
        uiY += LINE_HEIGHT;

        _renderBackend.DrawText($"Textures Loaded: {_loadedSprites.Count}/{SPRITE_FILES.Length}", new Vector2(UI_X, uiY), 14, textColor);
        uiY += LINE_HEIGHT;

        _renderBackend.DrawText($"Texture Filter: {_currentFilter}", new Vector2(UI_X, uiY), 14, textColor);
        uiY += LINE_HEIGHT;

        var mipmapCount = _loadedSprites.Count(s => s.Texture.HasMipmaps);
        var totalMipmapLevels = _loadedSprites.Sum(s => s.Texture.MipmapCount);
        _renderBackend.DrawText($"Mipmaps: {mipmapCount}/{_loadedSprites.Count} ({totalMipmapLevels} levels)", new Vector2(UI_X, uiY), 14, textColor);
        uiY += LINE_HEIGHT + 10;

        var controlsColor = new Color(150, 150, 150, 255);
        var highlightColor = new Color(255, 255, 100, 255);

        _renderBackend.DrawText("Controls:", new Vector2(UI_X, uiY), 14, titleColor);
        uiY += LINE_HEIGHT;

        _renderBackend.DrawText("[WASD] Move Camera", new Vector2(UI_X, uiY), 12, controlsColor);
        uiY += LINE_HEIGHT;

        _renderBackend.DrawText("[Q/E] Zoom Out/In", new Vector2(UI_X, uiY), 12, controlsColor);
        uiY += LINE_HEIGHT;

        _renderBackend.DrawText("[R] Reset Camera", new Vector2(UI_X, uiY), 12, controlsColor);
        uiY += LINE_HEIGHT;

        _renderBackend.DrawText("[SPACE] Regenerate Sprites", new Vector2(UI_X, uiY), 12, controlsColor);
        uiY += LINE_HEIGHT + 5;

        _renderBackend.DrawText("[F1] Point (pixel art, sharp pixels)", new Vector2(UI_X, uiY), 12, controlsColor);
        uiY += LINE_HEIGHT;

        _renderBackend.DrawText("[F2] Bilinear (smooth, blurry when zoomed out)", new Vector2(UI_X, uiY), 12, controlsColor);
        uiY += LINE_HEIGHT;

        _renderBackend.DrawText("[F3] Trilinear (crisp at any zoom, needs mipmaps)", new Vector2(UI_X, uiY), 12, controlsColor);
        uiY += LINE_HEIGHT;

        _renderBackend.DrawText("[F4] Anisotropic (best for rotated textures)", new Vector2(UI_X, uiY), 12, controlsColor);
        uiY += LINE_HEIGHT;

        _renderBackend.DrawText("[M] Generate Mipmaps (REQUIRED for F3/F4)", new Vector2(UI_X, uiY), 12, controlsColor);
        uiY += LINE_HEIGHT + 5;

        _renderBackend.DrawText("[ESC] Back to Menu", new Vector2(UI_X, uiY), 12, controlsColor);
        uiY += LINE_HEIGHT + 10;

        // TIP: How to see the difference
        _renderBackend.DrawText("TIP: Zoom OUT (Q) and compare filters:", new Vector2(UI_X, uiY), 13, highlightColor);
        uiY += LINE_HEIGHT;

        _renderBackend.DrawText("  F1=Pixelated, F2=Blurry, F3=Sharp (with mipmaps)", new Vector2(UI_X, uiY), 11, highlightColor);
    }

    private void SetTextureFilter(TextureFilter filter)
    {
        _currentFilter = filter;

        foreach (var loadedSprite in _loadedSprites)
        {
            loadedSprite.Texture.Filter = filter;
        }

        _logger.Info("GraphicsDemo", $"Texture filter changed to: {filter}");
    }

    private void GenerateAllMipmaps()
    {
        var generated = 0;

        foreach (var loadedSprite in _loadedSprites)
        {
            if (!loadedSprite.Texture.HasMipmaps)
            {
                loadedSprite.Texture.GenerateMipmaps();
                generated++;
            }
        }

        if (generated > 0)
        {
            _logger.Info("GraphicsDemo", $"Generated mipmaps for {generated} texture(s)");
        }
        else
        {
            _logger.Info("GraphicsDemo", "All textures already have mipmaps");
        }
    }

    private sealed class SpriteEntity
    {
        public Vector2 Position { get; set; }
        public int SpriteIndex { get; set; }
        public float Rotation { get; set; }
        public float RotationSpeed { get; set; }
        public float Scale { get; set; }
    }

    private sealed class LoadedSprite
    {
        public string Name { get; set; } = string.Empty;
        public ITexture Texture { get; set; } = null!;
    }
}

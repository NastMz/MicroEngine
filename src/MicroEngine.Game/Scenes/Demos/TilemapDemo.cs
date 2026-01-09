using MicroEngine.Core.ECS;
using MicroEngine.Core.ECS.Components;
using MicroEngine.Core.ECS.Systems;
using MicroEngine.Core.Graphics;
using MicroEngine.Core.Input;
using MicroEngine.Core.Logging;
using MicroEngine.Core.Math;
using MicroEngine.Core.Resources;
using MicroEngine.Core.Scenes;

namespace MicroEngine.Game.Scenes.Demos;

/// <summary>
/// Demonstrates the Tilemap and TilemapRenderer systems with procedurally generated terrain.
/// Shows sprite atlas-based tile rendering, viewport culling, and camera movement.
/// </summary>
public sealed class TilemapDemo : Scene
{
    private IInputBackend _inputBackend = null!;
    private IRenderer2D _renderer = null!;
    private IWindow _window = null!;
    private ILogger _logger = null!;
    private ResourceCache<ITexture> _textureCache = null!;

    // ECS for camera control
    private CameraControllerSystem _cameraSystem = null!;
    private Entity _cameraEntity;
    private Camera2D _camera = null!;

    // Tilemap system
    private Tilemap _tilemap = null!;
    private SpriteBatch _spriteBatch = null!;

    // Sprite atlas for tiles
    private SpriteAtlas _tilesAtlas = null!;

    private const int TILE_SIZE = 32;
    private const int GRID_WIDTH = 25;
    private const int GRID_HEIGHT = 19;
    private const float CAMERA_SPEED = 200f;

    // Tile IDs (0 = empty, 1 = grass, 2 = water, 3 = dirt, 4 = stone)
    private const int TILE_EMPTY = 0;
    private const int TILE_GRASS = 1;
    private const int TILE_WATER = 2;
    private const int TILE_DIRT = 3;
    private const int TILE_STONE = 4;

    /// <summary>
    /// Initializes a new instance of the <see cref="TilemapDemo"/> class.
    /// </summary>
    public TilemapDemo()
        : base("TilemapDemo")
    {
    }

    /// <inheritdoc/>
    public override void OnLoad(SceneContext context)
    {
        base.OnLoad(context);
        _inputBackend = context.InputBackend;
        _renderer = context.Renderer;
        _window = context.Window;
        _logger = context.Logger;
        _textureCache = context.TextureCache;
        
        
        // Initialize camera system
        _cameraSystem = new CameraControllerSystem();

        // Calculate screen center for camera offset
        var screenCenterX = _window.Width / 2f;
        var screenCenterY = _window.Height / 2f;

        // Initialize camera with centered viewport
        _camera = new Camera2D
        {
            Position = Vector2.Zero,
            Offset = new Vector2(screenCenterX, screenCenterY),
            Rotation = 0f,
            Zoom = 1f
        };

        // Create camera entity with CameraComponent
        _cameraEntity = World.CreateEntity();
        World.AddComponent(_cameraEntity, new CameraComponent
        {
            Camera = _camera,
            MovementSpeed = CAMERA_SPEED,
            ZoomSpeed = 0f,
            MinZoom = 1f,
            MaxZoom = 1f,
            DefaultPosition = Vector2.Zero,
            MoveDirection = Vector2.Zero,
            ZoomDelta = 0f,
            ResetRequested = false
        });

        // Initialize tilemap system
        _tilemap = new Tilemap(GRID_WIDTH, GRID_HEIGHT, TILE_SIZE, TILE_SIZE);
        _spriteBatch = new SpriteBatch(_renderer);
        
        // Load tile textures
        LoadTileTextures();
        
        // Generate procedural tilemap
        GenerateProceduralTilemap();
        
        _logger.Info("TilemapDemo", $"Tilemap demo loaded: {GRID_WIDTH}x{GRID_HEIGHT} tiles, 4 tile types");
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

        // Translate input to camera commands
        ref var cam = ref World.GetComponent<CameraComponent>(_cameraEntity);

        // Movement direction (WASD or Arrow keys)
        float moveX = 0f, moveY = 0f;
        if (_inputBackend.IsKeyDown(Key.W) || _inputBackend.IsKeyDown(Key.Up)) { moveY -= 1f; }
        if (_inputBackend.IsKeyDown(Key.S) || _inputBackend.IsKeyDown(Key.Down)) { moveY += 1f; }
        if (_inputBackend.IsKeyDown(Key.A) || _inputBackend.IsKeyDown(Key.Left)) { moveX -= 1f; }
        if (_inputBackend.IsKeyDown(Key.D) || _inputBackend.IsKeyDown(Key.Right)) { moveX += 1f; }
        cam.MoveDirection = new Vector2(moveX, moveY);

        // Reset request (R)
        if (_inputBackend.IsKeyPressed(Key.R))
        {
            cam.ResetRequested = true;
            _logger.Info("TilemapDemo", "Camera reset to origin");
        }

        // Process camera commands via system
        _cameraSystem.Update(World, deltaTime);

        // Update local camera reference
        _camera = cam.Camera;

        // Regenerate tilemap
        if (_inputBackend.IsKeyPressed(Key.Space))
        {
            GenerateProceduralTilemap();
            _logger.Info("TilemapDemo", "Tilemap regenerated");
        }
    }

    /// <inheritdoc/>
    public override void OnRender()
    {
        // Early exit if not loaded yet
        if (_renderer == null || _tilemap == null)
        {
            return;
        }

        _renderer.Clear(new Color(40, 60, 80, 255));

        // Begin camera mode for world-space rendering
        _renderer.BeginCamera2D(_camera);

        // Begin sprite batch for tilemap rendering
        _spriteBatch.Begin(SpriteSortMode.Deferred);

        // Manual tilemap rendering with culling
        RenderTilemap();

        // End sprite batch
        _spriteBatch.End();

        // End camera mode
        _renderer.EndCamera2D();

        // UI Overlay
        var layout = new TextLayoutHelper(startX: 20, startY: 20, defaultLineHeight: 20);
        var infoColor = new Color(200, 200, 200, 255);
        var controlsColor = new Color(180, 180, 180, 255);
        var dimColor = new Color(150, 150, 150, 255);

        layout.DrawText(_renderer, "Tilemap Demo - Tilemap System", 20, Color.White)
              .AddSpacing(10)
              .DrawText(_renderer, $"Camera: ({_camera.Position.X:F0}, {_camera.Position.Y:F0})", 14, infoColor)
              .DrawText(_renderer, $"Tiles: {GRID_WIDTH}x{GRID_HEIGHT} ({_tilemap.TotalTileCount} total)", 14, infoColor);
        
        // Calculate visible bounds manually
        var visibleBounds = _camera.GetVisibleBounds(_window.Width, _window.Height);
        var (startX, startY) = _tilemap.WorldToTile(new Vector2(visibleBounds.X, visibleBounds.Y));
        var (endX, endY) = _tilemap.WorldToTile(new Vector2(visibleBounds.X + visibleBounds.Width, visibleBounds.Y + visibleBounds.Height));
        startX = System.Math.Max(0, startX);
        startY = System.Math.Max(0, startY);
        endX = System.Math.Min(_tilemap.Width, endX + 1);
        endY = System.Math.Min(_tilemap.Height, endY + 1);
        var visibleTiles = (endX - startX) * (endY - startY);
        layout.DrawText(_renderer, $"Visible Tiles: {visibleTiles} (culling active)", 14, infoColor);

        layout.SetY(510)
              .DrawText(_renderer, "[WASD/Arrows] Move Camera", 14, controlsColor)
              .DrawText(_renderer, "[SPACE] Regenerate | [R] Reset Camera", 14, controlsColor)
              .DrawText(_renderer, "[ESC] Back to Menu", 14, dimColor);

        // Legend
        DrawLegend();
    }

    /// <inheritdoc/>
    public override void OnUnload()
    {
        base.OnUnload();
        _logger?.Info("TilemapDemo", "Tilemap demo unloaded");
    }

    private void LoadTileTextures()
    {
        // Load the tile atlas texture
        var atlasTexture = _textureCache.Load("assets/textures/tiles_atlas.png");
        
        // Create a grid-based sprite atlas (2x2 grid of 32x32 tiles)
        _tilesAtlas = SpriteAtlas.CreateGrid(
            atlasTexture,
            frameWidth: TILE_SIZE,
            frameHeight: TILE_SIZE,
            spacing: 0,
            margin: 0,
            namePrefix: "tile");
        
        // Manually rename regions to match tile types
        // Grid layout: [0]=grass, [1]=water, [2]=dirt, [3]=stone
        var regions = _tilesAtlas.GetRegionNames().ToList();
        var regionsList = new List<(string oldName, string newName)>
        {
            ("tile_0", "grass"),
            ("tile_1", "water"),
            ("tile_2", "dirt"),
            ("tile_3", "stone")
        };
        
        // Re-add regions with proper names
        foreach (var (oldName, newName) in regionsList)
        {
            if (_tilesAtlas.TryGetRegion(oldName, out var region))
            {
                _tilesAtlas.RemoveRegion(oldName);
                _tilesAtlas.AddRegion(newName, region);
            }
        }
        
        _logger.Info("TilemapDemo", $"Tile atlas loaded with {_tilesAtlas.RegionCount} regions");
    }

    private void RenderTilemap()
    {
        // Calculate visible bounds for culling
        var visibleBounds = _camera.GetVisibleBounds(_window.Width, _window.Height);
        var (startX, startY) = _tilemap.WorldToTile(new Vector2(visibleBounds.X, visibleBounds.Y));
        var (endX, endY) = _tilemap.WorldToTile(new Vector2(visibleBounds.X + visibleBounds.Width, visibleBounds.Y + visibleBounds.Height));

        // Clamp to tilemap bounds
        startX = System.Math.Max(0, startX);
        startY = System.Math.Max(0, startY);
        endX = System.Math.Min(_tilemap.Width, endX + 1);
        endY = System.Math.Min(_tilemap.Height, endY + 1);

        // Render only visible tiles
        for (int y = startY; y < endY; y++)
        {
            for (int x = startX; x < endX; x++)
            {
                int tileId = _tilemap.GetTile(x, y);

                // Skip empty tiles
                if (tileId == TILE_EMPTY)
                {
                    continue;
                }

                // Calculate world position
                var worldPos = _tilemap.TileToWorld(x, y);

                // Get region name from tile ID
                string? regionName = tileId switch
                {
                    TILE_GRASS => "grass",
                    TILE_WATER => "water",
                    TILE_DIRT => "dirt",
                    TILE_STONE => "stone",
                    _ => null
                };

                // Draw tile sprite from atlas
                if (regionName != null && _tilesAtlas.TryGetRegion(regionName, out var region))
                {
                    _spriteBatch.DrawRegion(
                        _tilesAtlas.Texture,
                        worldPos,
                        region,
                        tint: Color.White,
                        rotation: 0f,
                        scale: Vector2.One,
                        layerDepth: 0f);
                }
            }
        }
    }

    private void GenerateProceduralTilemap()
    {
        var random = new Random();
        
        for (int x = 0; x < GRID_WIDTH; x++)
        {
            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                // Simple procedural generation
                var noise = random.NextDouble();
                
                if (noise < 0.15) // 15% water
                {
                    _tilemap.SetTile(x, y, TILE_WATER);
                }
                else if (noise < 0.30) // 15% dirt
                {
                    _tilemap.SetTile(x, y, TILE_DIRT);
                }
                else if (noise < 0.40) // 10% stone
                {
                    _tilemap.SetTile(x, y, TILE_STONE);
                }
                else // 60% grass
                {
                    _tilemap.SetTile(x, y, TILE_GRASS);
                }
            }
        }
    }

    private static Color GetTileColor(int tileId)
    {
        return tileId switch
        {
            TILE_GRASS => new Color(80, 160, 60, 255),   // Grass - green
            TILE_WATER => new Color(50, 100, 200, 255),  // Water - blue
            TILE_DIRT => new Color(140, 90, 50, 255),    // Dirt - brown
            TILE_STONE => new Color(120, 120, 130, 255), // Stone - gray
            _ => Color.White
        };
    }

    private void DrawLegend()
    {
        const int LEGEND_X = 620;
        const int BOX_SIZE = 16;
        const int LINE_HEIGHT = 22;

        var layout = new TextLayoutHelper(startX: LEGEND_X, startY: 20, defaultLineHeight: LINE_HEIGHT);
        var legendColor = new Color(200, 200, 200, 255);

        layout.DrawText(_renderer, "Tile Types:", 14, legendColor)
              .AddSpacing(5);

        DrawLegendItem(ref layout, TILE_GRASS, "Grass");
        DrawLegendItem(ref layout, TILE_WATER, "Water");
        DrawLegendItem(ref layout, TILE_DIRT, "Dirt");
        DrawLegendItem(ref layout, TILE_STONE, "Stone");

        // Helper method to draw legend item
        void DrawLegendItem(ref TextLayoutHelper l, int tileId, string name)
        {
            _renderer.DrawRectangle(new Vector2(LEGEND_X, l.CurrentY), new Vector2(BOX_SIZE, BOX_SIZE), GetTileColor(tileId));
            _renderer.DrawText(name, new Vector2(LEGEND_X + BOX_SIZE + 8, l.CurrentY + 2), 12, Color.White);
            l.AddSpacing(LINE_HEIGHT);
        }
    }
}



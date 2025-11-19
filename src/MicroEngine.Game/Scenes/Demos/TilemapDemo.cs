using MicroEngine.Core.Graphics;
using MicroEngine.Core.Input;
using MicroEngine.Core.Logging;
using MicroEngine.Core.Math;
using MicroEngine.Core.Scenes;

namespace MicroEngine.Game.Scenes.Demos;

/// <summary>
/// Demonstrates tilemap concept with procedurally generated grid.
/// Shows tile-based rendering and camera movement.
/// </summary>
public sealed class TilemapDemo : Scene
{
    private IInputBackend _inputBackend = null!;
    private IRenderBackend2D _renderBackend = null!;
    private ILogger _logger = null!;

    private const int TILE_SIZE = 32;
    private const int GRID_WIDTH = 25;
    private const int GRID_HEIGHT = 19;
    private Vector2 _cameraOffset;
    private const float CAMERA_SPEED = 200f;
    
    // Simple procedural tilemap (0 = grass, 1 = water, 2 = dirt, 3 = stone)
    private readonly int[,] _tiles;

    /// <summary>
    /// Initializes a new instance of the <see cref="TilemapDemo"/> class.
    /// </summary>
    public TilemapDemo()
        : base("TilemapDemo")
    {
        _cameraOffset = Vector2.Zero;
        _tiles = new int[GRID_WIDTH, GRID_HEIGHT];
    }

    /// <inheritdoc/>
    public override void OnLoad(SceneContext context)
    {
        base.OnLoad(context);
        _inputBackend = context.InputBackend;
        _renderBackend = context.RenderBackend;
        _logger = context.Logger;
        
        GenerateProceduralTilemap();
        _logger.Info("TilemapDemo", "Tilemap demo loaded with procedural generation");
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

        // Camera movement with WASD
        var movementX = 0f;
        var movementY = 0f;
        
        if (_inputBackend.IsKeyDown(Key.W) || _inputBackend.IsKeyDown(Key.Up))
        {
            movementY -= CAMERA_SPEED * deltaTime;
        }
        if (_inputBackend.IsKeyDown(Key.S) || _inputBackend.IsKeyDown(Key.Down))
        {
            movementY += CAMERA_SPEED * deltaTime;
        }
        if (_inputBackend.IsKeyDown(Key.A) || _inputBackend.IsKeyDown(Key.Left))
        {
            movementX -= CAMERA_SPEED * deltaTime;
        }
        if (_inputBackend.IsKeyDown(Key.D) || _inputBackend.IsKeyDown(Key.Right))
        {
            movementX += CAMERA_SPEED * deltaTime;
        }

        _cameraOffset = new Vector2(
            _cameraOffset.X + movementX,
            _cameraOffset.Y + movementY
        );

        // Reset camera
        if (_inputBackend.IsKeyPressed(Key.R))
        {
            _cameraOffset = Vector2.Zero;
            _logger.Info("TilemapDemo", "Camera reset to origin");
        }

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
        // Early exit if not loaded yet (can happen during scene preloading)
        if (_renderBackend == null)
        {
            return;
        }

        _renderBackend.Clear(new Color(40, 60, 80, 255));

        // Render tilemap
        for (int x = 0; x < GRID_WIDTH; x++)
        {
            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                var tileX = x * TILE_SIZE - _cameraOffset.X;
                var tileY = y * TILE_SIZE - _cameraOffset.Y;

                // Culling - only render visible tiles
                if (tileX + TILE_SIZE < 0 || tileX > 800 || tileY + TILE_SIZE < 0 || tileY > 600)
                {
                    continue;
                }

                var tileType = _tiles[x, y];
                
                // Draw tile with texture-like details
                DrawTile(tileX, tileY, tileType);
            }
        }

        // UI Overlay
        _renderBackend.DrawText("Tilemap Demo - Procedural Tiles", new Vector2(20, 20), 20, Color.White);
        _renderBackend.DrawText($"Camera: ({_cameraOffset.X:F0}, {_cameraOffset.Y:F0})", new Vector2(20, 50), 14, new Color(200, 200, 200, 255));
        _renderBackend.DrawText("[WASD/Arrows] Move Camera", new Vector2(20, 510), 14, new Color(180, 180, 180, 255));
        _renderBackend.DrawText("[SPACE] Regenerate | [R] Reset Camera", new Vector2(20, 535), 14, new Color(180, 180, 180, 255));
        _renderBackend.DrawText("[ESC] Back to Menu", new Vector2(20, 560), 14, new Color(150, 150, 150, 255));

        // Legend
        DrawLegend();
    }

    /// <inheritdoc/>
    public override void OnUnload()
    {
        base.OnUnload();
        _logger?.Info("TilemapDemo", "Tilemap demo unloaded");
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
                    _tiles[x, y] = 1;
                }
                else if (noise < 0.30) // 15% dirt
                {
                    _tiles[x, y] = 2;
                }
                else if (noise < 0.40) // 10% stone
                {
                    _tiles[x, y] = 3;
                }
                else // 60% grass
                {
                    _tiles[x, y] = 0;
                }
            }
        }
    }

    private static Color GetTileColor(int tileType)
    {
        return tileType switch
        {
            0 => new Color(80, 160, 60, 255),   // Grass - green
            1 => new Color(50, 100, 200, 255),  // Water - blue
            2 => new Color(140, 90, 50, 255),   // Dirt - brown
            3 => new Color(120, 120, 130, 255), // Stone - gray
            _ => Color.White
        };
    }

    private void DrawTile(float x, float y, int tileType)
    {
        const int size = TILE_SIZE;
        var baseColor = GetTileColor(tileType);
        
        // Draw base tile
        _renderBackend.DrawRectangle(
            new Vector2(x, y),
            new Vector2(size - 1, size - 1),
            baseColor
        );

        // Add visual details based on tile type
        switch (tileType)
        {
            case 0: // Grass - add darker stripes
                var grassDark = new Color(
                    (byte)(baseColor.R * 0.8f),
                    (byte)(baseColor.G * 0.8f),
                    (byte)(baseColor.B * 0.8f),
                    255
                );
                _renderBackend.DrawRectangle(new Vector2(x + 4, y + 2), new Vector2(2, 6), grassDark);
                _renderBackend.DrawRectangle(new Vector2(x + 12, y + 8), new Vector2(2, 6), grassDark);
                _renderBackend.DrawRectangle(new Vector2(x + 22, y + 4), new Vector2(2, 6), grassDark);
                break;

            case 1: // Water - add lighter waves
                var waterLight = new Color(
                    (byte)System.Math.Min(255, baseColor.R * 1.3f),
                    (byte)System.Math.Min(255, baseColor.G * 1.3f),
                    (byte)System.Math.Min(255, baseColor.B * 1.3f),
                    255
                );
                _renderBackend.DrawRectangle(new Vector2(x + 2, y + 8), new Vector2(8, 2), waterLight);
                _renderBackend.DrawRectangle(new Vector2(x + 18, y + 16), new Vector2(10, 2), waterLight);
                break;

            case 2: // Dirt - add darker spots
                var dirtDark = new Color(
                    (byte)(baseColor.R * 0.7f),
                    (byte)(baseColor.G * 0.7f),
                    (byte)(baseColor.B * 0.7f),
                    255
                );
                _renderBackend.DrawRectangle(new Vector2(x + 6, y + 6), new Vector2(4, 4), dirtDark);
                _renderBackend.DrawRectangle(new Vector2(x + 20, y + 12), new Vector2(4, 4), dirtDark);
                _renderBackend.DrawRectangle(new Vector2(x + 12, y + 20), new Vector2(4, 4), dirtDark);
                break;

            case 3: // Stone - add lighter cracks
                var stoneLight = new Color(
                    (byte)System.Math.Min(255, baseColor.R * 1.2f),
                    (byte)System.Math.Min(255, baseColor.G * 1.2f),
                    (byte)System.Math.Min(255, baseColor.B * 1.2f),
                    255
                );
                _renderBackend.DrawRectangle(new Vector2(x + 8, y + 4), new Vector2(12, 1), stoneLight);
                _renderBackend.DrawRectangle(new Vector2(x + 4, y + 16), new Vector2(16, 1), stoneLight);
                _renderBackend.DrawRectangle(new Vector2(x + 12, y + 24), new Vector2(8, 1), stoneLight);
                break;
        }
    }

    private void DrawLegend()
    {
        const int legendX = 620;
        const int legendY = 20;
        const int boxSize = 16;
        const int lineHeight = 22;

        _renderBackend.DrawText("Tile Types:", new Vector2(legendX, legendY), 14, new Color(200, 200, 200, 255));

        var y = legendY + 25;

        // Grass
        _renderBackend.DrawRectangle(new Vector2(legendX, y), new Vector2(boxSize, boxSize), GetTileColor(0));
        _renderBackend.DrawText("Grass", new Vector2(legendX + boxSize + 8, y + 2), 12, Color.White);
        y += lineHeight;

        // Water
        _renderBackend.DrawRectangle(new Vector2(legendX, y), new Vector2(boxSize, boxSize), GetTileColor(1));
        _renderBackend.DrawText("Water", new Vector2(legendX + boxSize + 8, y + 2), 12, Color.White);
        y += lineHeight;

        // Dirt
        _renderBackend.DrawRectangle(new Vector2(legendX, y), new Vector2(boxSize, boxSize), GetTileColor(2));
        _renderBackend.DrawText("Dirt", new Vector2(legendX + boxSize + 8, y + 2), 12, Color.White);
        y += lineHeight;

        // Stone
        _renderBackend.DrawRectangle(new Vector2(legendX, y), new Vector2(boxSize, boxSize), GetTileColor(3));
        _renderBackend.DrawText("Stone", new Vector2(legendX + boxSize + 8, y + 2), 12, Color.White);
    }
}

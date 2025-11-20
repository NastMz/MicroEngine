using MicroEngine.Core.ECS;
using MicroEngine.Core.ECS.Components;
using MicroEngine.Core.Graphics;
using MicroEngine.Core.Input;
using MicroEngine.Core.Logging;
using MicroEngine.Core.Math;
using MicroEngine.Core.Savegame;
using MicroEngine.Core.Scenes;

namespace MicroEngine.Game.Scenes.Demos;

/// <summary>
/// Demonstrates savegame system with entity position saving and loading.
/// Shows ISavegameManager usage, metadata handling, and file operations.
/// </summary>
public sealed class SaveLoadDemo : Scene
{
    private IInputBackend _inputBackend = null!;
    private IRenderBackend2D _renderBackend = null!;
    private ILogger _logger = null!;
    private ISavegameManager _savegameManager = null!;

    private const string SCENE_NAME = "SaveLoadDemo";
    private const string SAVE_FILE_NAME = "demo_save";
    private const float DRAG_THRESHOLD = 20f;

    private readonly List<Entity> _entities = new();
    private Entity? _draggedEntity;
    private Vector2 _dragOffset;
    private string _statusMessage = "";
    private float _statusTimer;
    private SaveMetadata? _currentSaveMetadata;

    /// <summary>
    /// Initializes a new instance of the <see cref="SaveLoadDemo"/> class.
    /// </summary>
    public SaveLoadDemo()
        : base(SCENE_NAME)
    {
    }

    /// <inheritdoc/>
    public override void OnLoad(SceneContext context)
    {
        base.OnLoad(context);
        _inputBackend = context.InputBackend;
        _renderBackend = context.RenderBackend;
        _logger = context.Logger;
        _savegameManager = new SavegameManager("./Saves");
        _logger.Info(SCENE_NAME, "Save/Load demo loaded - drag entities and save/load their positions");

        CreateDemoEntities();
        LoadSaveMetadata();
    }

    /// <inheritdoc/>
    public override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);

        // Update status message timer
        if (_statusTimer > 0)
        {
            _statusTimer -= deltaTime;
        }

        // Handle input
        if (_inputBackend.IsKeyPressed(Key.Escape))
        {
            PopScene();
        }
        else if (_inputBackend.IsKeyPressed(Key.F5))
        {
            SaveGame();
        }
        else if (_inputBackend.IsKeyPressed(Key.F6))
        {
            LoadGame();
        }
        else if (_inputBackend.IsKeyPressed(Key.F7))
        {
            DeleteSave();
        }
        else if (_inputBackend.IsKeyPressed(Key.R))
        {
            ResetEntities();
        }

        HandleDragging();
    }

    /// <inheritdoc/>
    public override void OnRender()
    {
        _renderBackend.Clear(new Color(25, 30, 35, 255));

        RenderEntities();
        RenderUI();
    }

    /// <inheritdoc/>
    public override void OnUnload()
    {
        base.OnUnload();
        _logger?.Info(SCENE_NAME, "Save/Load demo unloaded");
    }

    private void CreateDemoEntities()
    {
        _entities.Clear();

        // Create 6 draggable entities in a grid pattern
        var colors = new[]
        {
            new Color(255, 100, 100, 255), // Red
            new Color(100, 255, 100, 255), // Green
            new Color(100, 100, 255, 255), // Blue
            new Color(255, 255, 100, 255), // Yellow
            new Color(255, 100, 255, 255), // Magenta
            new Color(100, 255, 255, 255)  // Cyan
        };

        var positions = new[]
        {
            new Vector2(200, 150),
            new Vector2(400, 150),
            new Vector2(600, 150),
            new Vector2(200, 350),
            new Vector2(400, 350),
            new Vector2(600, 350)
        };

        for (int i = 0; i < 6; i++)
        {
            var entity = World.CreateEntity($"Box{i + 1}");
            World.AddComponent(entity, new TransformComponent { Position = positions[i] });
            _entities.Add(entity);
        }

        _logger.Info(SCENE_NAME, $"Created {_entities.Count} draggable entities");
    }

    private void HandleDragging()
    {
        var mousePos = _inputBackend.GetMousePosition();
        var mousePressed = _inputBackend.IsMouseButtonPressed(MouseButton.Left);
        var mouseDown = _inputBackend.IsMouseButtonDown(MouseButton.Left);
        var mouseReleased = _inputBackend.IsMouseButtonReleased(MouseButton.Left);

        // Start dragging
        if (mousePressed && _draggedEntity == null)
        {
            foreach (var entity in _entities)
            {
                if (!World.IsEntityValid(entity))
                {
                    continue;
                }

                var transform = World.GetComponent<TransformComponent>(entity);
                var dx = mousePos.X - transform.Position.X;
                var dy = mousePos.Y - transform.Position.Y;
                var distance = MathF.Sqrt(dx * dx + dy * dy);

                if (distance < DRAG_THRESHOLD)
                {
                    _draggedEntity = entity;
                    _dragOffset = new Vector2(dx, dy);
                    break;
                }
            }
        }

        // Update dragged entity position
        if (mouseDown && _draggedEntity != null && World.IsEntityValid(_draggedEntity.Value))
        {
            ref var transform = ref World.GetComponent<TransformComponent>(_draggedEntity.Value);
            transform.Position = new Vector2(
                mousePos.X - _dragOffset.X,
                mousePos.Y - _dragOffset.Y
            );

            // Clamp to screen bounds
            transform.Position = new Vector2(
                MathF.Max(30f, MathF.Min(770f, transform.Position.X)),
                MathF.Max(30f, MathF.Min(570f, transform.Position.Y))
            );
        }

        // Stop dragging
        if (mouseReleased)
        {
            _draggedEntity = null;
        }
    }

    private void SaveGame()
    {
        var gameState = new GameSaveData
        {
            EntityPositions = new Dictionary<string, Vector2>()
        };

        foreach (var entity in _entities)
        {
            if (!World.IsEntityValid(entity))
            {
                continue;
            }

            var name = World.GetEntityName(entity) ?? $"Entity{entity.Id}";
            var transform = World.GetComponent<TransformComponent>(entity);
            gameState.EntityPositions[name] = transform.Position;
        }

        var result = _savegameManager.Save(gameState, SAVE_FILE_NAME, "Demo Save");

        if (result.Success)
        {
            _statusMessage = $"✓ Saved {gameState.EntityPositions.Count} entities";
            _statusTimer = 3f;
            LoadSaveMetadata();
            _logger.Info(SCENE_NAME, $"Game saved successfully to {result.FilePath}");
        }
        else
        {
            _statusMessage = $"✗ Save failed: {result.ErrorMessage}";
            _statusTimer = 3f;
            _logger.Error(SCENE_NAME, $"Failed to save game: {result.ErrorMessage}");
        }
    }

    private void LoadGame()
    {
        var result = _savegameManager.Load<GameSaveData>(SAVE_FILE_NAME);

        if (result.Success && result.Data != null)
        {
            int loadedCount = 0;

            foreach (var entity in _entities)
            {
                if (!World.IsEntityValid(entity))
                {
                    continue;
                }

                var name = World.GetEntityName(entity) ?? $"Entity{entity.Id}";
                if (result.Data.EntityPositions.TryGetValue(name, out var position))
                {
                    ref var transform = ref World.GetComponent<TransformComponent>(entity);
                    transform.Position = position;
                    loadedCount++;
                }
            }

            _statusMessage = $"✓ Loaded {loadedCount} entities";
            _statusTimer = 3f;
            LoadSaveMetadata();
            _logger.Info(SCENE_NAME, $"Game loaded successfully: {loadedCount} entities restored");
        }
        else
        {
            _statusMessage = result.ErrorMessage ?? "✗ No save file found";
            _statusTimer = 3f;
            _logger.Warn(SCENE_NAME, $"Failed to load game: {result.ErrorMessage}");
        }
    }

    private void DeleteSave()
    {
        if (_savegameManager.Exists(SAVE_FILE_NAME))
        {
            var deleted = _savegameManager.Delete(SAVE_FILE_NAME);
            if (deleted)
            {
                _statusMessage = "✓ Save file deleted";
                _statusTimer = 3f;
                _currentSaveMetadata = null;
                _logger.Info(SCENE_NAME, "Save file deleted successfully");
            }
            else
            {
                _statusMessage = "✗ Failed to delete save";
                _statusTimer = 3f;
                _logger.Error(SCENE_NAME, "Failed to delete save file");
            }
        }
        else
        {
            _statusMessage = "✗ No save file to delete";
            _statusTimer = 3f;
        }
    }

    private void ResetEntities()
    {
        // Destroy all existing entities
        var entitiesToDestroy = World.GetEntitiesWith<TransformComponent>().ToList();
        foreach (var entity in entitiesToDestroy)
        {
            World.DestroyEntity(entity);
        }
        
        // Force process destruction queue
        World.Update(0f);
        
        CreateDemoEntities();
        _statusMessage = "✓ Entities reset to default positions";
        _statusTimer = 3f;
        _logger.Info(SCENE_NAME, "Entities reset");
    }

    private void LoadSaveMetadata()
    {
        _currentSaveMetadata = _savegameManager.GetMetadata(SAVE_FILE_NAME);
    }

    private void RenderEntities()
    {
        for (int i = 0; i < _entities.Count; i++)
        {
            var entity = _entities[i];
            if (!World.IsEntityValid(entity))
            {
                continue;
            }

            var transform = World.GetComponent<TransformComponent>(entity);
            var name = World.GetEntityName(entity) ?? "Unknown";

            // Entity color
            var color = i switch
            {
                0 => new Color(255, 100, 100, 255), // Red
                1 => new Color(100, 255, 100, 255), // Green
                2 => new Color(100, 100, 255, 255), // Blue
                3 => new Color(255, 255, 100, 255), // Yellow
                4 => new Color(255, 100, 255, 255), // Magenta
                _ => new Color(100, 255, 255, 255)  // Cyan
            };

            // Highlight if being dragged
            if (_draggedEntity.HasValue && entity.Equals(_draggedEntity.Value))
            {
                var highlightSize = new Vector2(45, 45);
                var highlightPos = new Vector2(transform.Position.X - 22.5f, transform.Position.Y - 22.5f);
                _renderBackend.DrawRectangle(highlightPos, highlightSize, new Color(255, 255, 255, 100));
            }

            // Draw entity as rectangle (position is center, so offset by half size)
            var rectSize = new Vector2(40, 40);
            var rectPos = new Vector2(transform.Position.X - 20f, transform.Position.Y - 20f);
            _renderBackend.DrawRectangle(rectPos, rectSize, color);

            // Draw label
            var labelPos = new Vector2(transform.Position.X - 15, transform.Position.Y - 30);
            _renderBackend.DrawText(name, labelPos, 12, Color.White);
        }
    }

    private void RenderUI()
    {
        var layout = new TextLayoutHelper(_renderBackend, startX: 10, startY: 10, defaultLineHeight: 20);
        var infoColor = new Color(200, 200, 200, 255);
        var dimColor = new Color(150, 150, 150, 255);

        layout.DrawText("Save/Load System Demo", 20, Color.White)
              .AddSpacing(5)
              .DrawText($"Entities: {_entities.Count(e => World.IsEntityValid(e))}", 16, infoColor);

        // Save metadata
        if (_currentSaveMetadata != null)
        {
            layout.AddSpacing(10)
                  .DrawText("Save File Info:", 16, Color.White)
                  .DrawKeyValue("Name", _currentSaveMetadata.SaveName ?? "N/A", 14, dimColor, infoColor)
                  .DrawKeyValue("Created", _currentSaveMetadata.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"), 14, dimColor, infoColor)
                  .DrawKeyValue("Modified", _currentSaveMetadata.LastModified.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"), 14, dimColor, infoColor);
        }
        else
        {
            layout.AddSpacing(10)
                  .DrawText("No save file found", 14, dimColor);
        }

        // Status message
        if (_statusTimer > 0)
        {
            layout.AddSpacing(10)
                  .DrawText(_statusMessage, 16, new Color(100, 255, 100, 255));
        }

        // Controls
        layout.SetY(520)
              .DrawText("Controls:", 16, Color.White)
              .DrawText("[Click + Drag] Move entities", 14, dimColor)
              .DrawText("[F5] Save | [F6] Load | [F7] Delete Save", 14, dimColor)
              .DrawText("[R] Reset | [ESC] Menu", 14, dimColor);
    }

    /// <summary>
    /// Game save data structure.
    /// </summary>
    private sealed class GameSaveData
    {
        public Dictionary<string, Vector2> EntityPositions { get; set; } = new();
    }
}

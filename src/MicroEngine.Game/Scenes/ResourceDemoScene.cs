using MicroEngine.Backend.Raylib;
using MicroEngine.Backend.Raylib.Resources;
using MicroEngine.Core.Audio;
using MicroEngine.Core.Graphics;
using MicroEngine.Core.Input;
using MicroEngine.Core.Logging;
using MicroEngine.Core.Math;
using MicroEngine.Core.Resources;
using MicroEngine.Core.Scenes;

namespace MicroEngine.Game.Scenes;

/// <summary>
/// Demo scene showcasing resource loading (textures, fonts, audio) and sprite rendering.
/// </summary>
public class ResourceDemoScene : Scene
{
    private const string SCENE_CATEGORY = "ResourceDemo";
    private const float SPRITE_MOVE_SPEED = 200f;
    private const float ROTATION_SPEED = 90f;
    private const int FONT_SIZE = 18;
    private const int LINE_HEIGHT = 22;

    private readonly ILogger _logger;
    private readonly IRenderBackend _renderer;
    private readonly IInputBackend _input;
    private readonly IAudioBackend _audio;

    // Resource caches
    private readonly ResourceCache<ITexture> _textureCache;
    private readonly ResourceCache<IFont> _fontCache;
    private readonly ResourceCache<IAudioClip> _audioCache;

    private Vector2 _playerPosition;
    private float _playerRotation;
    private readonly List<EnemySprite> _enemies;
    private int _soundPlayCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceDemoScene"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="renderer">Render backend.</param>
    /// <param name="input">Input backend.</param>
    /// <param name="audio">Audio backend.</param>
    public ResourceDemoScene(
        ILogger logger,
        IRenderBackend renderer,
        IInputBackend input,
        IAudioBackend audio)
        : base("Resource Demo")
    {
        _logger = logger;
        _renderer = renderer;
        _input = input;
        _audio = audio;
        
        // Create resource caches with Raylib loaders
        _textureCache = new ResourceCache<ITexture>(new RaylibTextureLoader(), logger);
        _fontCache = new ResourceCache<IFont>(new RaylibFontLoader(), logger);
        _audioCache = new ResourceCache<IAudioClip>(new RaylibAudioClipLoader(), logger);
        
        _enemies = [];
        _playerPosition = new Vector2(640f, 360f);
        _playerRotation = 0f;
        _soundPlayCount = 0;
    }

    /// <inheritdoc/>
    public override void OnLoad()
    {
        base.OnLoad();

        _logger.Info(SCENE_CATEGORY, "=== Resource Demo Scene ===");
        _logger.Info(SCENE_CATEGORY, "Demonstrating ResourceCache<T> with reference counting");

        LoadDemoResources();
        CreateEnemies();

        _logger.Info(SCENE_CATEGORY, "Scene loaded - Controls: WASD=Move, Q/E=Rotate, SPACE=Sound, M=Music");
    }

    private void LoadDemoResources()
    {
        _logger.Info(SCENE_CATEGORY, "");
        _logger.Info(SCENE_CATEGORY, "--- RESOURCE LOADING DEMO ---");
        
        _logger.Info(SCENE_CATEGORY, "ResourceCache usage example:");
        _logger.Info(SCENE_CATEGORY, "  var texture = textureCache.Load(\"player.png\");  // Loads & caches");
        _logger.Info(SCENE_CATEGORY, "  var texture2 = textureCache.Load(\"player.png\"); // Reuses cached (refcount++)");
        _logger.Info(SCENE_CATEGORY, "  textureCache.Unload(\"player.png\");             // Decrements refcount");
        _logger.Info(SCENE_CATEGORY, "  textureCache.Unload(\"player.png\");             // Refcount=0, disposes resource");
        _logger.Info(SCENE_CATEGORY, "");
        _logger.Info(SCENE_CATEGORY, "Supported formats:");
        _logger.Info(SCENE_CATEGORY, "  Textures: PNG, JPG, BMP, TGA, GIF");
        _logger.Info(SCENE_CATEGORY, "  Fonts: TTF, OTF (with configurable size)");
        _logger.Info(SCENE_CATEGORY, "  Audio: WAV, OGG, MP3, FLAC (auto-streaming for large files)");
        _logger.Info(SCENE_CATEGORY, "");
    }

    private void CreateEnemies()
    {
        for (int i = 0; i < 5; i++)
        {
            var x = Random.Shared.Next(100, _renderer.WindowWidth - 100);
            var y = Random.Shared.Next(100, _renderer.WindowHeight - 100);
            var vx = (Random.Shared.NextSingle() - 0.5f) * 100f;
            var vy = (Random.Shared.NextSingle() - 0.5f) * 100f;

            _enemies.Add(new EnemySprite
            {
                Position = new Vector2(x, y),
                Velocity = new Vector2(vx, vy),
                Rotation = Random.Shared.NextSingle() * 360f
            });
        }

        _logger.Info(SCENE_CATEGORY, $"Created {_enemies.Count} sprites (simulating texture rendering)");
    }

    /// <inheritdoc/>
    public override void OnFixedUpdate(float fixedDeltaTime)
    {
        base.OnFixedUpdate(fixedDeltaTime);

        HandleInput(fixedDeltaTime);
        UpdateEnemies(fixedDeltaTime);
    }

    private void HandleInput(float deltaTime)
    {
        HandleMovementInput(deltaTime);
        HandleRotationInput(deltaTime);
        HandleAudioDemoInput();
    }

    private void HandleMovementInput(float deltaTime)
    {
        float moveDirX = 0f;
        float moveDirY = 0f;

        if (_input.IsKeyDown(Key.W) || _input.IsKeyDown(Key.Up))
        {
            moveDirY -= 1f;
        }

        if (_input.IsKeyDown(Key.S) || _input.IsKeyDown(Key.Down))
        {
            moveDirY += 1f;
        }

        if (_input.IsKeyDown(Key.A) || _input.IsKeyDown(Key.Left))
        {
            moveDirX -= 1f;
        }

        if (_input.IsKeyDown(Key.D) || _input.IsKeyDown(Key.Right))
        {
            moveDirX += 1f;
        }

        var moveDir = new Vector2(moveDirX, moveDirY);
        if (moveDir.SqrMagnitude > 0f)
        {
            moveDir = moveDir.Normalized;
            _playerPosition += moveDir * SPRITE_MOVE_SPEED * deltaTime;
        }

        _playerPosition = new Vector2(
            System.Math.Clamp(_playerPosition.X, 0f, _renderer.WindowWidth),
            System.Math.Clamp(_playerPosition.Y, 0f, _renderer.WindowHeight));
    }

    private void HandleRotationInput(float deltaTime)
    {
        if (_input.IsKeyDown(Key.Q))
        {
            _playerRotation -= ROTATION_SPEED * deltaTime;
        }

        if (_input.IsKeyDown(Key.E))
        {
            _playerRotation += ROTATION_SPEED * deltaTime;
        }
    }

    private void HandleAudioDemoInput()
    {
        if (_input.IsKeyPressed(Key.Space))
        {
            _soundPlayCount++;
            _logger.Info(SCENE_CATEGORY, $"[DEMO] Sound effect played ({_soundPlayCount} times)");
            _logger.Info(SCENE_CATEGORY, "       Real usage: audioCache.Load(\"jump.wav\") -> audioBackend.PlaySound()");
        }

        if (_input.IsKeyPressed(Key.M))
        {
            _logger.Info(SCENE_CATEGORY, "[DEMO] Music toggle simulated");
            _logger.Info(SCENE_CATEGORY, "       Real usage: audioCache.Load(\"music.ogg\") -> audioBackend.PlayMusic()");
        }
    }

    private void UpdateEnemies(float deltaTime)
    {
        for (int i = 0; i < _enemies.Count; i++)
        {
            UpdateEnemy(_enemies[i], deltaTime);
        }
    }

    private void UpdateEnemy(EnemySprite enemy, float deltaTime)
    {
        enemy.Position += enemy.Velocity * deltaTime;
        enemy.Rotation += 45f * deltaTime;

        var newVelocity = enemy.Velocity;
        var newPosition = enemy.Position;

        if (enemy.Position.X < 0f || enemy.Position.X > _renderer.WindowWidth)
        {
            newVelocity = new Vector2(-newVelocity.X, newVelocity.Y);
            newPosition = new Vector2(
                System.Math.Clamp(enemy.Position.X, 0f, _renderer.WindowWidth),
                enemy.Position.Y);
        }

        if (enemy.Position.Y < 0f || enemy.Position.Y > _renderer.WindowHeight)
        {
            newVelocity = new Vector2(newVelocity.X, -newVelocity.Y);
            newPosition = new Vector2(
                enemy.Position.X,
                System.Math.Clamp(enemy.Position.Y, 0f, _renderer.WindowHeight));
        }

        enemy.Velocity = newVelocity;
        enemy.Position = newPosition;
    }

    /// <inheritdoc/>
    public override void OnRender()
    {
        _renderer.Clear(new Color(30, 30, 40, 255));

        RenderEnemies();
        RenderPlayer();
        RenderUI();
    }

    private void RenderEnemies()
    {
        foreach (var enemy in _enemies)
        {
            _renderer.DrawRectangle(
                new Vector2(enemy.Position.X - 10f, enemy.Position.Y - 10f),
                new Vector2(20f, 20f),
                Color.Red);
            
            _renderer.DrawRectangleLines(
                new Vector2(enemy.Position.X - 10f, enemy.Position.Y - 10f),
                new Vector2(20f, 20f),
                Color.White,
                1f);
        }
    }

    private void RenderPlayer()
    {
        _renderer.DrawRectangle(
            new Vector2(_playerPosition.X - 15f, _playerPosition.Y - 15f),
            new Vector2(30f, 30f),
            Color.Green);
        
        _renderer.DrawRectangleLines(
            new Vector2(_playerPosition.X - 15f, _playerPosition.Y - 15f),
            new Vector2(30f, 30f),
            Color.White,
            2f);
    }

    private void RenderUI()
    {
        RenderTitle();
        RenderControls();
        RenderResourceStats();
        RenderPlayerStats();
    }

    private void RenderTitle()
    {
        _renderer.DrawText(
            "MicroEngine - Resource System Demo",
            new Vector2(10, 10),
            FONT_SIZE + 4,
            Color.Yellow);
    }

    private void RenderControls()
    {
        int x = 10;
        int y = 45;

        _renderer.DrawText("Controls:", new Vector2(x, y), FONT_SIZE, Color.Cyan);
        y += LINE_HEIGHT;
        _renderer.DrawText("WASD/Arrows - Move Player", new Vector2(x + 10, y), FONT_SIZE - 2, Color.White);
        y += LINE_HEIGHT;
        _renderer.DrawText("Q/E - Rotate", new Vector2(x + 10, y), FONT_SIZE - 2, Color.White);
        y += LINE_HEIGHT;
        _renderer.DrawText($"SPACE - Play Sound ({_soundPlayCount} played)", new Vector2(x + 10, y), FONT_SIZE - 2, Color.White);
        y += LINE_HEIGHT;
        _renderer.DrawText("M - Music Demo", new Vector2(x + 10, y), FONT_SIZE - 2, Color.White);
        y += LINE_HEIGHT;
        _renderer.DrawText("ESC - Exit", new Vector2(x + 10, y), FONT_SIZE - 2, Color.White);
    }

    private void RenderResourceStats()
    {
        int x = 10;
        int y = _renderer.WindowHeight - 250;

        _renderer.DrawText("Resource System Status:", new Vector2(x, y), FONT_SIZE, Color.Cyan);
        y += LINE_HEIGHT;

        // Cache stats
        _renderer.DrawText(
            $"TextureCache: {_textureCache.CachedCount} loaded, {_textureCache.TotalMemoryUsage} bytes",
            new Vector2(x + 10, y),
            FONT_SIZE - 2,
            Color.LightGray);
        y += LINE_HEIGHT;

        _renderer.DrawText(
            $"FontCache: {_fontCache.CachedCount} loaded, {_fontCache.TotalMemoryUsage} bytes",
            new Vector2(x + 10, y),
            FONT_SIZE - 2,
            Color.LightGray);
        y += LINE_HEIGHT;

        _renderer.DrawText(
            $"AudioCache: {_audioCache.CachedCount} loaded, {_audioCache.TotalMemoryUsage} bytes",
            new Vector2(x + 10, y),
            FONT_SIZE - 2,
            Color.LightGray);
        y += LINE_HEIGHT * 2;

        // Supported formats
        _renderer.DrawText("Supported Formats:", new Vector2(x, y), FONT_SIZE, Color.Cyan);
        y += LINE_HEIGHT;
        _renderer.DrawText("Textures: PNG, JPG, BMP, TGA, GIF", new Vector2(x + 10, y), FONT_SIZE - 2, Color.LightGray);
        y += LINE_HEIGHT;
        _renderer.DrawText("Fonts: TTF, OTF", new Vector2(x + 10, y), FONT_SIZE - 2, Color.LightGray);
        y += LINE_HEIGHT;
        _renderer.DrawText("Audio: WAV, OGG, MP3, FLAC", new Vector2(x + 10, y), FONT_SIZE - 2, Color.LightGray);
        y += LINE_HEIGHT * 2;

        // Usage hint
        _renderer.DrawText("Add files to assets/ folder to test", new Vector2(x, y), FONT_SIZE - 2, Color.Yellow);
    }

    private void RenderPlayerStats()
    {
        // FPS in top right
        _renderer.DrawText(
            $"FPS: {_renderer.GetFPS()}",
            new Vector2(_renderer.WindowWidth - 80, 10),
            FONT_SIZE,
            Color.Green);

        // Player position in bottom right
        _renderer.DrawText(
            $"Player: ({_playerPosition.X:F0}, {_playerPosition.Y:F0}) | Rot: {_playerRotation:F0}°",
            new Vector2(_renderer.WindowWidth - 300, _renderer.WindowHeight - 25),
            FONT_SIZE - 2,
            Color.White);

        // Enemy count
        _renderer.DrawText(
            $"Sprites: {_enemies.Count + 1}",
            new Vector2(_renderer.WindowWidth - 120, _renderer.WindowHeight - 25),
            FONT_SIZE - 2,
            Color.LightGray);
    }

    /// <inheritdoc/>
    public override void OnUnload()
    {
        _logger.Info(SCENE_CATEGORY, "");
        _logger.Info(SCENE_CATEGORY, "--- RESOURCE CLEANUP ---");
        _logger.Info(SCENE_CATEGORY, "Disposing resource caches (auto-unloads all cached resources)");
        
        _textureCache.Dispose();
        _fontCache.Dispose();
        _audioCache.Dispose();
        
        _logger.Info(SCENE_CATEGORY, "Resource Demo Scene unloaded");
        base.OnUnload();
    }

    private sealed class EnemySprite
    {
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public float Rotation { get; set; }
    }
}

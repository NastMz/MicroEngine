using System.Linq;
using System.Text.Json;
using MicroEngine.Core.ECS;
using MicroEngine.Core.ECS.Components;
using MicroEngine.Core.Events;
using MicroEngine.Core.Graphics;
using MicroEngine.Core.Input;
using MicroEngine.Core.Logging;
using MicroEngine.Core.Math;
using MicroEngine.Core.Resources;
using MicroEngine.Core.Scenes;
using MicroEngine.Core.Audio;
using MicroEngine.Game.Scenes.Demos.Zelda.Systems;
using Microsoft.Extensions.DependencyInjection;

namespace MicroEngine.Game.Scenes.Demos.Zelda;

/// <summary>
/// Zelda-style action RPG demo scene with combat, AI enemies, and collision detection.
/// </summary>
public sealed class ZeldaScene : Scene
{
    private IInputBackend _input = null!;
    private IRenderer2D _renderer = null!;
    private ILogger _logger = null!;
    private ResourceCache<ITexture> _textureCache = null!;
    private ResourceCache<IAudioClip> _audioCache = null!;
    private ISoundPlayer _soundPlayer = null!;
    private EventBus _eventBus = null!;

    private CombatSystem _combatSystem = null!;
    private RenderSystem _renderSystem = null!;
    private IAudioClip _swordClip = null!;
    private IAudioClip _hitClip = null!;

    private Camera2D _camera = null!;
    private Entity _playerEntity;
    private CachedQuery? _enemyQuery;
    private IWindow _window = null!;

    private string _statusMessage = "MINI-ZELDA STRESS TEST";
    private Color _statusColor = ZeldaConstants.COLOR_UI_TEXT;
    private bool _gameOver;
    private bool _victory;

    private const int HERO_SIZE = ZeldaConstants.HERO_SPRITE_ORIGINAL_SIZE;

    // Map Data
    private int[,] _map = null!;
    private readonly Dictionary<int, bool> _tilePassability = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ZeldaScene"/> class.
    /// </summary>
    public ZeldaScene() : base("ZeldaDemo") 
    {
    }

    private void LoadMap()
    {
        try 
        {
            // Load AssetManifest to get passability
            string manifestPath = ZeldaConstants.ASSET_MANIFEST_PATH;
            if (File.Exists(manifestPath))
            {
                string manifestJson = File.ReadAllText(manifestPath);
                using var doc = JsonDocument.Parse(manifestJson);
                var assets = doc.RootElement.GetProperty("assets");
                foreach (var asset in assets.EnumerateArray())
                {
                    if (asset.GetProperty("assetName").GetString() == ZeldaConstants.TILES_ASSET_NAME)
                    {
                        var tiles = asset.GetProperty("tiles").EnumerateArray().ToList();
                        
                        // Floor and Details
                        _tilePassability[ZeldaConstants.TILE_ID_FLOOR_STONE_VAR1] = GetPassable(tiles, "floor_stone_var1");
                        _tilePassability[ZeldaConstants.TILE_ID_FLOOR_STONE_VAR2] = GetPassable(tiles, "floor_stone_var2");
                        _tilePassability[ZeldaConstants.TILE_ID_FLOOR_CRACKED] = GetPassable(tiles, "floor_cracked_var1");
                        
                        // Walls
                        _tilePassability[ZeldaConstants.TILE_ID_WALL_TOP] = GetPassable(tiles, "wall_brick_top_var1");
                        _tilePassability[ZeldaConstants.TILE_ID_WALL_MID] = GetPassable(tiles, "wall_brick_mid_var1");
                        _tilePassability[ZeldaConstants.TILE_ID_WALL_BASE] = GetPassable(tiles, "wall_brick_base_var1");
                        _tilePassability[ZeldaConstants.TILE_ID_WALL_TOP_LEFT] = GetPassable(tiles, "wall_corner_top_left");
                        _tilePassability[ZeldaConstants.TILE_ID_WALL_TOP_RIGHT] = GetPassable(tiles, "wall_corner_top_right");
                        
                        // Obstacles
                        _tilePassability[ZeldaConstants.TILE_ID_PILLAR] = GetPassable(tiles, "pillar_stone");
                        _tilePassability[ZeldaConstants.TILE_ID_HOLE_CENTER] = GetPassable(tiles, "hole_void_center");
                        _tilePassability[ZeldaConstants.TILE_ID_HOLE_TOP] = GetPassable(tiles, "hole_top_mid");
                        _tilePassability[ZeldaConstants.TILE_ID_HOLE_BOT] = GetPassable(tiles, "hole_bot_mid");
                        _tilePassability[ZeldaConstants.TILE_ID_HOLE_LEFT] = GetPassable(tiles, "hole_mid_left");
                        _tilePassability[ZeldaConstants.TILE_ID_HOLE_RIGHT] = GetPassable(tiles, "hole_mid_right");
                        break;
                    }
                }
            }

            // Load Map Layout
            if (File.Exists(ZeldaConstants.MAP_JSON_PATH))
            {
                string mapJson = File.ReadAllText(ZeldaConstants.MAP_JSON_PATH);
                using var doc = JsonDocument.Parse(mapJson);
                int width = doc.RootElement.GetProperty("width").GetInt32();
                int height = doc.RootElement.GetProperty("height").GetInt32();
                var data = doc.RootElement.GetProperty("data").EnumerateArray().Select(x => x.GetInt32()).ToArray();

                _map = new int[height, width];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = y * width + x;
                        _map[y, x] = index < data.Length ? data[index] : ZeldaConstants.TILE_ID_FLOOR_STONE_VAR1;
                    }
                }
                _logger.Info(ZeldaConstants.LOG_ZELDA, $"{ZeldaConstants.MSG_LOADING_MAP}{width}x{height}");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ZeldaConstants.LOG_ZELDA, $"{ZeldaConstants.MSG_ERROR_LOADING_MAP}{ex.Message}");
            // Fallback: Generate empty map with walls
            for (int y = 0; y < ZeldaConstants.MAP_HEIGHT; y++)
            {
                for (int x = 0; x < ZeldaConstants.MAP_WIDTH; x++)
                {
                    if (x == 0 || x == ZeldaConstants.MAP_WIDTH - 1 || y == 0 || y == ZeldaConstants.MAP_HEIGHT - 1)
                    {
                        _map[y, x] = ZeldaConstants.TILE_ID_WALL_TOP;
                    }
                }
            }
        }
    }

    private Vector2 GetRandomPassableTile(Random rand)
    {
        for (int attempt = 0; attempt < ZeldaConstants.SPAWN_MAX_ATTEMPTS; attempt++)
        {
            int tx = rand.Next(ZeldaConstants.MAP_WIDTH);
            int ty = rand.Next(ZeldaConstants.MAP_HEIGHT);

            if (IsPassable(new Vector2(tx * ZeldaConstants.TILE_SIZE + ZeldaConstants.TILE_SIZE / 2f, ty * ZeldaConstants.TILE_SIZE + ZeldaConstants.TILE_SIZE / 2f), 0))
            {
                return new Vector2(tx * ZeldaConstants.TILE_SIZE + ZeldaConstants.TILE_SIZE / 2f, ty * ZeldaConstants.TILE_SIZE + ZeldaConstants.TILE_SIZE / 2f);
            }
        }
        return new Vector2(ZeldaConstants.SPAWN_FALLBACK_TILE_X * ZeldaConstants.TILE_SIZE + ZeldaConstants.TILE_SIZE / 2f, ZeldaConstants.SPAWN_FALLBACK_TILE_Y * ZeldaConstants.TILE_SIZE + ZeldaConstants.TILE_SIZE / 2f);
    }

    private bool GetPassable(List<JsonElement> tiles, string id)
    {
        var tile = tiles.FirstOrDefault(t => t.GetProperty("id").GetString() == id);
        return tile.ValueKind != JsonValueKind.Undefined && tile.GetProperty("passable").GetBoolean();
    }

    /// <summary>
    /// Checks if a position with given radius is passable (not blocked by walls).
    /// </summary>
    /// <param name="worldPos">World position to check.</param>
    /// <param name="radius">Collision radius.</param>
    /// <returns>True if passable, false otherwise.</returns>
    public bool IsPassable(Vector2 worldPos, float radius = ZeldaConstants.PLAYER_COLLISION_RADIUS)
    {
        // Check 4 corners of a bounding box for better collision feel
        return IsPointPassable(new Vector2(worldPos.X - radius, worldPos.Y - radius)) &&
               IsPointPassable(new Vector2(worldPos.X + radius, worldPos.Y - radius)) &&
               IsPointPassable(new Vector2(worldPos.X - radius, worldPos.Y + radius)) &&
               IsPointPassable(new Vector2(worldPos.X + radius, worldPos.Y + radius));
    }

    /// <summary>
    /// Plays a sound effect.
    /// </summary>
    /// <param name="clip">Audio clip to play.</param>
    public void PlaySound(IAudioClip clip)
    {
        _soundPlayer?.PlaySound(clip);
    }

    /// <summary>
    /// Gets the sword swing audio clip.
    /// </summary>
    public IAudioClip SwordClip => _swordClip;
    
    /// <summary>
    /// Gets the hit impact audio clip.
    /// </summary>
    public IAudioClip HitClip => _hitClip;

    /// <summary>
    /// Checks if a single point is passable.
    /// </summary>
    /// <param name="pos">Point to check.</param>
    /// <returns>True if passable, false otherwise.</returns>
    public bool IsPointPassable(Vector2 pos)
    {
        int tx = (int)(pos.X / ZeldaConstants.TILE_SIZE);
        int ty = (int)(pos.Y / ZeldaConstants.TILE_SIZE);

        if (tx < 0 || tx >= ZeldaConstants.MAP_WIDTH || ty < 0 || ty >= ZeldaConstants.MAP_HEIGHT)
        {
            return false;
        }

        int tileType = _map[ty, tx];
        return _tilePassability.TryGetValue(tileType, out bool passable) && passable;
    }

    /// <inheritdoc/>
    public override void OnLoad(SceneContext context)
    {
        base.OnLoad(context);

        _input = context.InputBackend;
        _renderer = context.Renderer;
        _logger = context.Logger;
        _textureCache = context.TextureCache;
        _audioCache = context.AudioCache;
        _soundPlayer = context.SoundPlayer;
        _eventBus = context.Services.GetRequiredService<EventBus>();

        _window = context.Window;
        _camera = new Camera2D
        {
            Position = new Vector2(ZeldaConstants.CAMERA_INITIAL_X, ZeldaConstants.CAMERA_INITIAL_Y),
            Offset = new Vector2(_window.Width / 2f, _window.Height / 2f),
            Rotation = 0f,
            Zoom = 1.0f
        };

        _eventBus.Subscribe<GameStateEvent>(OnGameStateChanged);

        // Register systems in the World. They will be resolved via DI through context.Services
        World.RegisterSystem<AnimationSystem>();
        World.RegisterSystem<RenderSystem>();
        World.RegisterSystem<CombatSystem>();
        World.RegisterSystem<PlayerSystem>();
        World.RegisterSystem<EnemyAISystem>();
        World.RegisterSystem<AudioSystem>();
        
        // Retrieve systems if we need direct interaction (like debug toggles or specialized rendering)
        _combatSystem = World.GetSystem<CombatSystem>();
        _renderSystem = World.GetSystem<RenderSystem>();

        _enemyQuery = World.CreateCachedQuery(typeof(EnemyComponent), typeof(TransformComponent));

        // Load Audio
        _swordClip = _audioCache.Load(ZeldaConstants.SWORD_SFX_PATH);
        _hitClip = _audioCache.Load(ZeldaConstants.HIT_SFX_PATH);

        LoadMap();

        // Spawn Map entity for systems to consume via MapComponent
        var mapEntity = World.CreateEntity(ZeldaConstants.ENTITY_MAP);
        World.AddComponent(mapEntity, new MapComponent 
        { 
            Tiles = _map, 
            Width = ZeldaConstants.MAP_WIDTH, 
            Height = ZeldaConstants.MAP_HEIGHT 
        });

        SpawnPlayer();
        SpawnEnemies();
        
        _logger.Info(ZeldaConstants.LOG_ZELDA, ZeldaConstants.MSG_SCENE_READY);
    }

    private void SpawnPlayer()
    {
        var heroTexture = _textureCache.Load(ZeldaConstants.HERO_TEXTURE_PATH);
        _playerEntity = World.CreateEntity(ZeldaConstants.ENTITY_PLAYER);

        float heroScale = ZeldaConstants.HERO_DRAW_SIZE / ZeldaConstants.HERO_SPRITE_ORIGINAL_SIZE;

        // Find a safe spot for the player (Start Room: top-left area in JSON, which is top-left in world)
        Vector2 playerStart = new Vector2(ZeldaConstants.SPAWN_FALLBACK_TILE_X * ZeldaConstants.TILE_SIZE, ZeldaConstants.SPAWN_FALLBACK_TILE_Y * ZeldaConstants.TILE_SIZE);
        
        World.AddComponent(_playerEntity, new TransformComponent
        {
            Position = playerStart,
            Scale = new Vector2(heroScale, heroScale),
            // Origin at the feet (centered horizontally, near bottom vertically)
            Origin = new Vector2(ZeldaConstants.HERO_DRAW_SIZE / 2f, ZeldaConstants.HERO_DRAW_SIZE * ZeldaConstants.PLAYER_PIVOT_Y_FACTOR) 
        });

        World.AddComponent(_playerEntity, new SpriteComponent
        {
            Texture = heroTexture,
            Visible = true,
            Layer = 10,
            Tint = Color.White,
            Region = new SpriteRegion(new Rectangle(0, 0, ZeldaConstants.HERO_SPRITE_ORIGINAL_SIZE, ZeldaConstants.HERO_SPRITE_ORIGINAL_SIZE))
        });

        var clips = new List<AnimationClip>
        {
            CreateHeroClip(ZeldaConstants.CLIP_WALK_DOWN, 0),
            CreateHeroClip(ZeldaConstants.CLIP_WALK_UP, 1),
            CreateHeroClip(ZeldaConstants.CLIP_WALK_LEFT, 2),
            CreateHeroClip(ZeldaConstants.CLIP_WALK_RIGHT, 3),
            CreateHeroAttackClip(ZeldaConstants.CLIP_ATTACK_DOWN, 4, 0),
            CreateHeroAttackClip(ZeldaConstants.CLIP_ATTACK_UP, 4, 1),
            CreateHeroAttackClip(ZeldaConstants.CLIP_ATTACK_LEFT, 4, 2),
            CreateHeroAttackClip(ZeldaConstants.CLIP_ATTACK_RIGHT, 4, 3)
        };

        World.AddComponent(_playerEntity, new AnimatorComponent
        {
            Clips = clips,
            CurrentClipName = ZeldaConstants.CLIP_WALK_DOWN,
            IsPlaying = false,
            Speed = 1.0f
        });

        World.AddComponent(_playerEntity, new PlayerComponent
        {
            Speed = ZeldaConstants.PLAYER_SPEED,
            AttackDuration = ZeldaConstants.PLAYER_ATTACK_DURATION,
            State = PlayerState.Idle
        });

        World.AddComponent(_playerEntity, new HealthComponent
        {
            Current = ZeldaConstants.PLAYER_MAX_HEALTH,
            Max = ZeldaConstants.PLAYER_MAX_HEALTH
        });

        World.AddComponent(_playerEntity, new AudioComponent
        {
            AttackClip = _swordClip,
            HitClip = _hitClip
        });
    }

    private AnimationClip CreateHeroClip(string name, int row)
    {
        var frames = new List<SpriteRegion>();
        for (int i = 0; i < 4; i++)
        {
            frames.Add(new SpriteRegion(new Rectangle(i * ZeldaConstants.HERO_SPRITE_ORIGINAL_SIZE, row * ZeldaConstants.HERO_SPRITE_ORIGINAL_SIZE, ZeldaConstants.HERO_SPRITE_ORIGINAL_SIZE, ZeldaConstants.HERO_SPRITE_ORIGINAL_SIZE)));
        }
        return new AnimationClip(name, frames, ZeldaConstants.ANIM_FRAME_TIME_HERO, true);
    }

    private AnimationClip CreateHeroAttackClip(string name, int row, int col)
    {
        var frames = new List<SpriteRegion>
        {
            new SpriteRegion(new Rectangle(col * ZeldaConstants.HERO_SPRITE_ORIGINAL_SIZE, row * ZeldaConstants.HERO_SPRITE_ORIGINAL_SIZE, ZeldaConstants.HERO_SPRITE_ORIGINAL_SIZE, ZeldaConstants.HERO_SPRITE_ORIGINAL_SIZE))
        };
        // Fully synchronize animation with state duration
        return new AnimationClip(name, frames, ZeldaConstants.PLAYER_ATTACK_DURATION, false);
    }

    private void SpawnEnemies()
    {
        var blobTexture = _textureCache.Load(ZeldaConstants.BLOB_TEXTURE_PATH);
        var rand = new Random();

        for (int i = 0; i < ZeldaConstants.SLIME_COUNT; i++)
        {
            var enemy = World.CreateEntity($"{ZeldaConstants.ENTITY_ENEMY_PREFIX}{i}");
            Vector2 pos = GetRandomPassableTile(rand);

            float slimeDrawSize = ZeldaConstants.TILE_SIZE * ZeldaConstants.ENEMY_SCALE;
            World.AddComponent(enemy, new TransformComponent { 
                Position = pos, 
                Scale = new Vector2(ZeldaConstants.ENEMY_SCALE, ZeldaConstants.ENEMY_SCALE), 
                // Pivot at the base of the slime
                Origin = new Vector2(slimeDrawSize / 2f, slimeDrawSize * ZeldaConstants.ENEMY_PIVOT_Y_FACTOR) 
            });
            World.AddComponent(enemy, new SpriteComponent
            {
                Texture = blobTexture,
                Visible = true,
                Layer = 10,
                Tint = Color.White,
                Region = new SpriteRegion(new Rectangle(0, 0, ZeldaConstants.TILE_SIZE, ZeldaConstants.TILE_SIZE))
            });

            var frames = new List<SpriteRegion>();
            for (int f = 0; f < 4; f++)
            {
                frames.Add(new SpriteRegion(new Rectangle(f * ZeldaConstants.TILE_SIZE, 0, ZeldaConstants.TILE_SIZE, ZeldaConstants.TILE_SIZE)));
            }
            var idleClip = new AnimationClip(ZeldaConstants.CLIP_ENEMY_IDLE, frames, ZeldaConstants.ANIM_FRAME_TIME_ENEMY, true);

            World.AddComponent(enemy, new AnimatorComponent
            {
                Clips = new List<AnimationClip> { idleClip },
                CurrentClipName = ZeldaConstants.CLIP_ENEMY_IDLE,
                IsPlaying = true,
                Speed = 1.0f
            });

            World.AddComponent(enemy, new EnemyComponent
            {
                Speed = ZeldaConstants.ENEMY_SPEED,
                DetectionRadius = ZeldaConstants.ENEMY_DETECTION_RADIUS
            });

            World.AddComponent(enemy, new HealthComponent { Current = ZeldaConstants.ENEMY_MAX_HEALTH, Max = ZeldaConstants.ENEMY_MAX_HEALTH });

            World.AddComponent(enemy, new AudioComponent
            {
                HitClip = _hitClip
            });
        }
    }

    private void OnGameStateChanged(GameStateEvent e)
    {
        _statusMessage = e.Message;
        _gameOver = e.IsGameOver;
        
        if (e.Message.Contains(ZeldaConstants.MSG_VICTORY_KEY))
        {
            _victory = true;
            _statusColor = ZeldaConstants.COLOR_UI_VICTORY;
        }
        else if (_gameOver)
        {
            _statusColor = ZeldaConstants.COLOR_UI_GAMEOVER;
        }
    }

    /// <inheritdoc/>
    public override void OnUpdate(float deltaTime)
    {
        if (_input.IsKeyPressed(Key.Escape))
        {
            PopScene();
            return;
        }

        if (_input.IsKeyPressed(Key.H))
        {
            _combatSystem.ShowDebug = !_combatSystem.ShowDebug;
        }

        if ((_gameOver || _victory) && _input.IsKeyPressed(Key.R))
        {
            ReplaceScene(new ZeldaScene());
            return;
        }

        if (World.IsEntityValid(_playerEntity))
        {
            var transform = World.GetComponent<TransformComponent>(_playerEntity);
            _camera.Position = Vector2.Lerp(_camera.Position, transform.Position, ZeldaConstants.CAMERA_LERP_FACTOR * deltaTime);
        }

        // NO LONGER CALLING SYSTEMS MANUALLY
        // World.Update(deltaTime) is called automatically by Scene.OnFixedUpdate or base logic
        // but since ZeldaScene doesn't use FixedUpdate for everything, we might want to call 
        // World.Update(deltaTime) here if we want systems to run every frame.
        // Actually, Scene.cs calls World.Update in OnFixedUpdate.
        // For this demo, let's call it here too to maintain frame-rate independent logic if needed.
        World.Update(deltaTime);

        base.OnUpdate(deltaTime);
    }

    /// <inheritdoc/>
    public override void OnRender()
    {
        _renderer.Clear(ZeldaConstants.COLOR_BACKGROUND);

        _renderer.BeginCamera2D(_camera);

        var tilesTexture = _textureCache.Load(ZeldaConstants.TILES_TEXTURE_PATH);
        for (int y = 0; y < ZeldaConstants.MAP_HEIGHT; y++)
        {
            for (int x = 0; x < ZeldaConstants.MAP_WIDTH; x++)
            {
                int tileType = _map[y, x];

                // Aesthetics: Draw floor under transparent pillars
                if (tileType == ZeldaConstants.TILE_ID_PILLAR)
                {
                    _renderer.DrawTexturePro(tilesTexture, new Rectangle(0, 0, ZeldaConstants.TILE_SIZE, ZeldaConstants.TILE_SIZE),
                        new Rectangle(x * ZeldaConstants.TILE_SIZE, y * ZeldaConstants.TILE_SIZE, ZeldaConstants.TILE_SIZE, ZeldaConstants.TILE_SIZE), Vector2.Zero, 0f, Color.White);
                }

                Rectangle source = tileType switch
                {
                    ZeldaConstants.TILE_ID_FLOOR_STONE_VAR2 => new Rectangle(1 * ZeldaConstants.TILE_SIZE, 0, ZeldaConstants.TILE_SIZE, ZeldaConstants.TILE_SIZE),
                    ZeldaConstants.TILE_ID_WALL_TOP => new Rectangle(5 * ZeldaConstants.TILE_SIZE, 0, ZeldaConstants.TILE_SIZE, ZeldaConstants.TILE_SIZE),
                    ZeldaConstants.TILE_ID_PILLAR => new Rectangle(3 * ZeldaConstants.TILE_SIZE, 4 * ZeldaConstants.TILE_SIZE, ZeldaConstants.TILE_SIZE, ZeldaConstants.TILE_SIZE),
                    ZeldaConstants.TILE_ID_HOLE_CENTER => new Rectangle(1 * ZeldaConstants.TILE_SIZE, 5 * ZeldaConstants.TILE_SIZE, ZeldaConstants.TILE_SIZE, ZeldaConstants.TILE_SIZE),
                    ZeldaConstants.TILE_ID_FLOOR_CRACKED => new Rectangle(3 * ZeldaConstants.TILE_SIZE, 0, ZeldaConstants.TILE_SIZE, ZeldaConstants.TILE_SIZE),
                    ZeldaConstants.TILE_ID_WALL_MID => new Rectangle(5 * ZeldaConstants.TILE_SIZE, 1 * ZeldaConstants.TILE_SIZE, ZeldaConstants.TILE_SIZE, ZeldaConstants.TILE_SIZE),
                    ZeldaConstants.TILE_ID_WALL_BASE => new Rectangle(5 * ZeldaConstants.TILE_SIZE, 2 * ZeldaConstants.TILE_SIZE, ZeldaConstants.TILE_SIZE, ZeldaConstants.TILE_SIZE),
                    ZeldaConstants.TILE_ID_WALL_TOP_LEFT => new Rectangle(0 * ZeldaConstants.TILE_SIZE, 3 * ZeldaConstants.TILE_SIZE, ZeldaConstants.TILE_SIZE, ZeldaConstants.TILE_SIZE),
                    ZeldaConstants.TILE_ID_WALL_TOP_RIGHT => new Rectangle(1 * ZeldaConstants.TILE_SIZE, 3 * ZeldaConstants.TILE_SIZE, ZeldaConstants.TILE_SIZE, ZeldaConstants.TILE_SIZE),
                    ZeldaConstants.TILE_ID_HOLE_TOP => new Rectangle(1 * ZeldaConstants.TILE_SIZE, 4 * ZeldaConstants.TILE_SIZE, ZeldaConstants.TILE_SIZE, ZeldaConstants.TILE_SIZE),
                    ZeldaConstants.TILE_ID_HOLE_BOT => new Rectangle(1 * ZeldaConstants.TILE_SIZE, 6 * ZeldaConstants.TILE_SIZE, ZeldaConstants.TILE_SIZE, ZeldaConstants.TILE_SIZE),
                    ZeldaConstants.TILE_ID_HOLE_LEFT => new Rectangle(0 * ZeldaConstants.TILE_SIZE, 5 * ZeldaConstants.TILE_SIZE, ZeldaConstants.TILE_SIZE, ZeldaConstants.TILE_SIZE),
                    ZeldaConstants.TILE_ID_HOLE_RIGHT => new Rectangle(2 * ZeldaConstants.TILE_SIZE, 5 * ZeldaConstants.TILE_SIZE, ZeldaConstants.TILE_SIZE, ZeldaConstants.TILE_SIZE),
                    _ => new Rectangle(0, 0, ZeldaConstants.TILE_SIZE, ZeldaConstants.TILE_SIZE)
                };

                _renderer.DrawTexturePro(tilesTexture, source, 
                    new Rectangle(x * ZeldaConstants.TILE_SIZE, y * ZeldaConstants.TILE_SIZE, ZeldaConstants.TILE_SIZE, ZeldaConstants.TILE_SIZE), Vector2.Zero, 0f, Color.White);
            }
        }

        _renderSystem.Update(World, 0f); 

        // HITBOX VISUALIZATION
        if (_combatSystem.ShowDebug)
        {
            if (World.IsEntityValid(_playerEntity))
            {
                var pComp = World.GetComponent<PlayerComponent>(_playerEntity);
                
                // Show proximity radius at body center
                _renderer.DrawCircleLines(_combatSystem.LastPlayerBodyCenter, _combatSystem.PlayerProximityRadius, ZeldaConstants.COLOR_DEBUG_HITBOX_PLAYER);
                
                if (pComp.State == PlayerState.Attacking)
                {
                    _renderer.DrawCircleLines(_combatSystem.LastAttackPoint, _combatSystem.LastAttackRadius, ZeldaConstants.COLOR_DEBUG_HITBOX_ATTACK);
                    _renderer.DrawCircle(_combatSystem.LastAttackPoint, 4f, ZeldaConstants.COLOR_DEBUG_HITBOX_ATTACK);
                }
            }

            // Enemy Hitboxes centered on body
            if (_enemyQuery != null)
            {
                float slimeDrawSize = ZeldaConstants.TILE_SIZE * ZeldaConstants.ENEMY_SCALE;
                foreach (var enemy in _enemyQuery.Entities)
                {
                    if (!World.IsEntityValid(enemy))
                    {
                        continue;
                    }
                    
                    var eTrans = World.GetComponent<TransformComponent>(enemy);
                    Vector2 eBodyCenter = eTrans.Position + new Vector2(0, -slimeDrawSize * ZeldaConstants.ENEMY_BODY_CENTER_Y_FACTOR);
                    _renderer.DrawCircleLines(eBodyCenter, ZeldaConstants.ENEMY_HITBOX_RADIUS, ZeldaConstants.COLOR_DEBUG_HITBOX_ENEMY);
                }
            }
        }

        _renderer.EndCamera2D();

        // UI Overlay
        var layout = new TextLayoutHelper(startX: ZeldaConstants.UI_MARGIN, startY: ZeldaConstants.UI_MARGIN, defaultLineHeight: ZeldaConstants.UI_LINE_HEIGHT);
        layout.DrawText(_renderer, _statusMessage, ZeldaConstants.FONT_SIZE_TITLE, _statusColor);
        
        // --- RESTORED CONTROLS GUIDE ---
        layout.DrawText(_renderer, "WASD: MOVE | SPACE: ATTACK", ZeldaConstants.FONT_SIZE_GUIDE, ZeldaConstants.COLOR_UI_TEXT);
        layout.DrawText(_renderer, "H: TOGGLE HITBOXES | R: RESTART", ZeldaConstants.FONT_SIZE_GUIDE, ZeldaConstants.COLOR_UI_TEXT);
        layout.DrawText(_renderer, $"ENEMIES REMAINING: {(_enemyQuery?.Count ?? 0)}", ZeldaConstants.FONT_SIZE_GUIDE, ZeldaConstants.COLOR_UI_COUNTER);
        
        if (World.IsEntityValid(_playerEntity))
        {
            var hp = World.GetComponent<HealthComponent>(_playerEntity);
            _renderer.DrawText($"HEART CONTAINER: {hp.Current}/{hp.Max}", new Vector2(ZeldaConstants.UI_MARGIN, ZeldaConstants.UI_HEART_CONTAINER_Y), ZeldaConstants.FONT_SIZE_HUD, ZeldaConstants.COLOR_UI_HEALTH);
        }
        
        if (_gameOver || _victory)
        {
            _renderer.DrawRectangle(Vector2.Zero, new Vector2(_window.Width, _window.Height), ZeldaConstants.COLOR_UI_OVERLAY);
            string bigMsg = _victory ? ZeldaConstants.MSG_VICTORY : ZeldaConstants.MSG_GAME_OVER;
            Color bigColor = _victory ? ZeldaConstants.COLOR_UI_VICTORY : ZeldaConstants.COLOR_UI_GAMEOVER;
            
            _renderer.DrawText(bigMsg, new Vector2(ZeldaConstants.UI_END_SCREEN_X, ZeldaConstants.UI_END_SCREEN_Y), ZeldaConstants.FONT_SIZE_END_SCREEN_LARGE, bigColor);
            _renderer.DrawText(ZeldaConstants.MSG_RESTART_HINT, new Vector2(ZeldaConstants.UI_END_SCREEN_X - 10f, ZeldaConstants.UI_RESTART_HINT_Y), ZeldaConstants.FONT_SIZE_HUD, ZeldaConstants.COLOR_UI_TEXT);
        }
    }

    /// <inheritdoc/>
    public override void OnUnload()
    {
        _eventBus.Unsubscribe<GameStateEvent>(OnGameStateChanged);
        base.OnUnload();
    }
}


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

public sealed class ZeldaScene : Scene
{
    private IInputBackend _input = null!;
    private IRenderer2D _renderer = null!;
    private ILogger _logger = null!;
    private ResourceCache<ITexture> _textureCache;
    private ResourceCache<IAudioClip> _audioCache;
    private ISoundPlayer _soundPlayer;
    private EventBus _eventBus;

    private AnimationSystem _animationSystem = null!;
    private RenderSystem _renderSystem = null!;
    private CombatSystem _combatSystem;
    private PlayerSystem _playerSystem = null!;
    private EnemyAISystem _enemyAISystem = null!;
    private IAudioClip _swordClip;
    private IAudioClip _hitClip;

    private Camera2D _camera = null!;
    private Entity _playerEntity;
    private CachedQuery? _enemyQuery;

    private string _statusMessage = "MINI-ZELDA STRESS TEST";
    private Color _statusColor = ZeldaConstants.COLOR_UI_TEXT;
    private bool _gameOver;
    private bool _victory;

    private const int HERO_SIZE = 214;

    // Map Data
    private int[,] _map;
    private readonly Dictionary<int, bool> _tilePassability = new();

    public ZeldaScene() : base("ZeldaDemo") 
    {
    }

    private void LoadMap()
    {
        try 
        {
            // Load AssetManifest to get passability
            string manifestPath = "assets/textures/AssetManifest.json";
            if (File.Exists(manifestPath))
            {
                string manifestJson = File.ReadAllText(manifestPath);
                using var doc = JsonDocument.Parse(manifestJson);
                var assets = doc.RootElement.GetProperty("assets");
                foreach (var asset in assets.EnumerateArray())
                {
                    if (asset.GetProperty("assetName").GetString() == "dungeon_tiles")
                    {
                        var tiles = asset.GetProperty("tiles").EnumerateArray().ToList();
                        
                        // Floor and Details
                        _tilePassability[0] = GetPassable(tiles, "floor_stone_var1");
                        _tilePassability[1] = GetPassable(tiles, "floor_stone_var2");
                        _tilePassability[5] = GetPassable(tiles, "floor_cracked_var1");
                        
                        // Walls
                        _tilePassability[2] = GetPassable(tiles, "wall_brick_top_var1");
                        _tilePassability[6] = GetPassable(tiles, "wall_brick_mid_var1");
                        _tilePassability[7] = GetPassable(tiles, "wall_brick_base_var1");
                        _tilePassability[8] = GetPassable(tiles, "wall_corner_top_left");
                        _tilePassability[9] = GetPassable(tiles, "wall_corner_top_right");
                        
                        // Obstacles
                        _tilePassability[3] = GetPassable(tiles, "pillar_stone");
                        _tilePassability[4] = GetPassable(tiles, "hole_void_center");
                        _tilePassability[10] = GetPassable(tiles, "hole_top_mid");
                        _tilePassability[11] = GetPassable(tiles, "hole_bot_mid");
                        _tilePassability[12] = GetPassable(tiles, "hole_mid_left");
                        _tilePassability[13] = GetPassable(tiles, "hole_mid_right");
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
                        _map[y, x] = index < data.Length ? data[index] : 0;
                    }
                }
                _logger.Info("Zelda", $"Loaded static map: {width}x{height}");
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Zelda", $"Error loading map: {ex.Message}");
            // Fallback: Generate empty map with walls
            for (int y = 0; y < ZeldaConstants.MAP_HEIGHT; y++)
                for (int x = 0; x < ZeldaConstants.MAP_WIDTH; x++)
                    if (x == 0 || x == 39 || y == 0 || y == 39) _map[y, x] = 2;
        }
    }

    private Vector2 GetRandomPassableTile(Random rand)
    {
        for (int attempt = 0; attempt < 100; attempt++)
        {
            int tx = rand.Next(ZeldaConstants.MAP_WIDTH);
            int ty = rand.Next(ZeldaConstants.MAP_HEIGHT);

            if (IsPassable(new Vector2(tx * ZeldaConstants.TILE_SIZE + 16, ty * ZeldaConstants.TILE_SIZE + 16), 0))
            {
                return new Vector2(tx * ZeldaConstants.TILE_SIZE + 16, ty * ZeldaConstants.TILE_SIZE + 16);
            }
        }
        return new Vector2(4 * ZeldaConstants.TILE_SIZE + 16, 4 * ZeldaConstants.TILE_SIZE + 16);
    }

    private bool GetPassable(List<JsonElement> tiles, string id)
    {
        var tile = tiles.FirstOrDefault(t => t.GetProperty("id").GetString() == id);
        return tile.ValueKind != JsonValueKind.Undefined && tile.GetProperty("passable").GetBoolean();
    }

    public bool IsPassable(Vector2 worldPos, float radius = 8f)
    {
        // Check 4 corners of a bounding box for better collision feel
        return IsPointPassable(new Vector2(worldPos.X - radius, worldPos.Y - radius)) &&
               IsPointPassable(new Vector2(worldPos.X + radius, worldPos.Y - radius)) &&
               IsPointPassable(new Vector2(worldPos.X - radius, worldPos.Y + radius)) &&
               IsPointPassable(new Vector2(worldPos.X + radius, worldPos.Y + radius));
    }

    public void PlaySound(IAudioClip clip)
    {
        _soundPlayer?.PlaySound(clip);
    }

    public IAudioClip SwordClip => _swordClip;
    public IAudioClip HitClip => _hitClip;

    public bool IsPointPassable(Vector2 pos)
    {
        int tx = (int)(pos.X / ZeldaConstants.TILE_SIZE);
        int ty = (int)(pos.Y / ZeldaConstants.TILE_SIZE);

        if (tx < 0 || tx >= ZeldaConstants.MAP_WIDTH || ty < 0 || ty >= ZeldaConstants.MAP_HEIGHT) return false;

        int tileType = _map[ty, tx];
        return _tilePassability.TryGetValue(tileType, out bool passable) && passable;
    }

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

        _camera = new Camera2D
        {
            Position = new Vector2(400, 300),
            Offset = new Vector2(context.Window.Width / 2f, context.Window.Height / 2f),
            Rotation = 0f,
            Zoom = 1.0f
        };

        _eventBus.Subscribe<ZeldaGameStateEvent>(OnGameStateChanged);

        _animationSystem = new AnimationSystem();
        _renderSystem = new RenderSystem(_renderer);
        _combatSystem = new CombatSystem(_eventBus, _logger, this);
        
        // Create systems that will be called manually since DI is not set up
        _playerSystem = new PlayerSystem(_input, _eventBus, _logger, this);
        _enemyAISystem = new EnemyAISystem(this);

        _enemyQuery = World.CreateCachedQuery(typeof(EnemyComponent), typeof(TransformComponent));

        // Load Audio
        _swordClip = _audioCache.Load(ZeldaConstants.SWORD_SFX_PATH);
        _hitClip = _audioCache.Load(ZeldaConstants.HIT_SFX_PATH);

        LoadMap();
        SpawnPlayer();
        SpawnEnemies();
        
        _logger.Info("Zelda", "Scene loaded successfully. Camera and Systems ready.");
    }

    private void SpawnPlayer()
    {
        var heroTexture = _textureCache.Load(ZeldaConstants.HERO_TEXTURE_PATH);
        _playerEntity = World.CreateEntity("Link");

        float heroScale = ZeldaConstants.HERO_DRAW_SIZE / ZeldaConstants.HERO_SPRITE_ORIGINAL_SIZE;

        // Find a safe spot for the player (Start Room: top-left area in JSON, which is top-left in world)
        Vector2 playerStart = new Vector2(4 * ZeldaConstants.TILE_SIZE, 4 * ZeldaConstants.TILE_SIZE);
        
        World.AddComponent(_playerEntity, new TransformComponent
        {
            Position = playerStart,
            Scale = new Vector2(heroScale, heroScale),
            // Origin at the feet (centered horizontally, near bottom vertically)
            Origin = new Vector2(ZeldaConstants.HERO_DRAW_SIZE / 2f, ZeldaConstants.HERO_DRAW_SIZE * 0.9f) 
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
            CurrentClipName = "walk_down",
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
    }

    private AnimationClip CreateHeroClip(string name, int row)
    {
        var frames = new List<SpriteRegion>();
        for (int i = 0; i < 4; i++)
        {
            frames.Add(new SpriteRegion(new Rectangle(i * ZeldaConstants.HERO_SPRITE_ORIGINAL_SIZE, row * ZeldaConstants.HERO_SPRITE_ORIGINAL_SIZE, ZeldaConstants.HERO_SPRITE_ORIGINAL_SIZE, ZeldaConstants.HERO_SPRITE_ORIGINAL_SIZE)));
        }
        return new AnimationClip(name, frames, 0.12f, true);
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
            var enemy = World.CreateEntity($"Slime_{i}");
            Vector2 pos = GetRandomPassableTile(rand);

            float slimeDrawSize = ZeldaConstants.TILE_SIZE * ZeldaConstants.ENEMY_SCALE;
            World.AddComponent(enemy, new TransformComponent { 
                Position = pos, 
                Scale = new Vector2(ZeldaConstants.ENEMY_SCALE, ZeldaConstants.ENEMY_SCALE), 
                // Pivot at the base of the slime
                Origin = new Vector2(slimeDrawSize / 2f, slimeDrawSize * 0.85f) 
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
            for (int f = 0; f < 4; f++) frames.Add(new SpriteRegion(new Rectangle(f * ZeldaConstants.TILE_SIZE, 0, ZeldaConstants.TILE_SIZE, ZeldaConstants.TILE_SIZE)));
            var idleClip = new AnimationClip(ZeldaConstants.CLIP_ENEMY_IDLE, frames, 0.15f, true);

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
        }
    }

    private void OnGameStateChanged(ZeldaGameStateEvent e)
    {
        _statusMessage = e.Message;
        _gameOver = e.IsGameOver;
        
        if (e.Message.Contains("WIN"))
        {
            _victory = true;
            _statusColor = ZeldaConstants.COLOR_UI_VICTORY;
        }
        else if (_gameOver)
        {
            _statusColor = ZeldaConstants.COLOR_UI_GAMEOVER;
        }
    }

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
            _camera.Position = Vector2.Lerp(_camera.Position, transform.Position, 8.0f * deltaTime);
        }

        // Call systems manually since DI is not configured
        _playerSystem.Update(World, deltaTime);
        _enemyAISystem.Update(World, deltaTime);
        _combatSystem.Update(World, deltaTime);
        _animationSystem.Update(World, deltaTime);

        base.OnUpdate(deltaTime);
    }

    public override void OnRender()
    {
        _renderer.Clear(new Color(20, 25, 20, 255));

        _renderer.BeginCamera2D(_camera);

        var tilesTexture = _textureCache.Load(ZeldaConstants.TILES_TEXTURE_PATH);
        for (int y = 0; y < ZeldaConstants.MAP_HEIGHT; y++)
        {
            for (int x = 0; x < ZeldaConstants.MAP_WIDTH; x++)
            {
                int tileType = _map[y, x];

                // Aesthetics: Draw floor under transparent pillars
                if (tileType == 3)
                {
                    _renderer.DrawTexturePro(tilesTexture, new Rectangle(0, 0, 32, 32),
                        new Rectangle(x * ZeldaConstants.TILE_SIZE, y * ZeldaConstants.TILE_SIZE, ZeldaConstants.TILE_SIZE, ZeldaConstants.TILE_SIZE), Vector2.Zero, 0f, Color.White);
                }

                Rectangle source = tileType switch
                {
                    1 => new Rectangle(1 * 32, 0, 32, 32),    // floor_stone_var2
                    2 => new Rectangle(5 * 32, 0, 32, 32),    // wall_brick_top_var1
                    3 => new Rectangle(3 * 32, 4 * 32, 32, 32), // pillar_stone
                    4 => new Rectangle(1 * 32, 5 * 32, 32, 32), // hole_void_center
                    5 => new Rectangle(3 * 32, 0, 32, 32),    // floor_cracked_var1
                    6 => new Rectangle(5 * 32, 1 * 32, 32, 32), // wall_brick_mid_var1
                    7 => new Rectangle(5 * 32, 2 * 32, 32, 32), // wall_brick_base_var1
                    8 => new Rectangle(0 * 32, 3 * 32, 32, 32), // wall_corner_top_left
                    9 => new Rectangle(1 * 32, 3 * 32, 32, 32), // wall_corner_top_right
                    10 => new Rectangle(1 * 32, 4 * 32, 32, 32), // hole_top_mid
                    11 => new Rectangle(1 * 32, 6 * 32, 32, 32), // hole_bot_mid
                    12 => new Rectangle(0 * 32, 5 * 32, 32, 32), // hole_mid_left
                    13 => new Rectangle(2 * 32, 5 * 32, 32, 32), // hole_mid_right
                    _ => new Rectangle(0, 0, 32, 32)      // floor_stone_var1
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
                    if (!World.IsEntityValid(enemy)) continue;
                    
                    var eTrans = World.GetComponent<TransformComponent>(enemy);
                    Vector2 eBodyCenter = eTrans.Position + new Vector2(0, -slimeDrawSize * 0.4f);
                    _renderer.DrawCircleLines(eBodyCenter, 16f, ZeldaConstants.COLOR_DEBUG_HITBOX_ENEMY);
                }
            }
        }

        _renderer.EndCamera2D();

        // UI Overlay
        var layout = new TextLayoutHelper(startX: 20, startY: 20, defaultLineHeight: 25);
        layout.DrawText(_renderer, _statusMessage, 28, _statusColor);
        
        // --- RESTORED CONTROLS GUIDE ---
        layout.DrawText(_renderer, "WASD: MOVE | SPACE: ATTACK", 16, ZeldaConstants.COLOR_UI_TEXT);
        layout.DrawText(_renderer, "H: TOGGLE HITBOXES | R: RESTART", 16, ZeldaConstants.COLOR_UI_TEXT);
        layout.DrawText(_renderer, $"ENEMIES REMAINING: {(_enemyQuery?.Count ?? 0)}", 16, ZeldaConstants.COLOR_UI_COUNTER);
        
        if (World.IsEntityValid(_playerEntity))
        {
            var hp = World.GetComponent<HealthComponent>(_playerEntity);
            _renderer.DrawText($"HEART CONTAINER: {hp.Current}/{hp.Max}", new Vector2(20, 550), 20, ZeldaConstants.COLOR_UI_HEALTH);
        }
        
        if (_gameOver || _victory)
        {
            _renderer.DrawRectangle(Vector2.Zero, new Vector2(850, 600), new Color(0, 0, 0, 150));
            string bigMsg = _victory ? ZeldaConstants.MSG_VICTORY : ZeldaConstants.MSG_GAME_OVER;
            Color bigColor = _victory ? ZeldaConstants.COLOR_UI_VICTORY : ZeldaConstants.COLOR_UI_GAMEOVER;
            
            _renderer.DrawText(bigMsg, new Vector2(320, 250), 40, bigColor);
            _renderer.DrawText(ZeldaConstants.MSG_RESTART_HINT, new Vector2(310, 320), 20, ZeldaConstants.COLOR_UI_TEXT);
        }
    }

    public override void OnUnload()
    {
        _eventBus.Unsubscribe<ZeldaGameStateEvent>(OnGameStateChanged);
        base.OnUnload();
    }
}


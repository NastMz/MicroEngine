using MicroEngine.Core.Audio;
using MicroEngine.Core.ECS;
using MicroEngine.Core.ECS.Components;
using MicroEngine.Core.Events;
using MicroEngine.Core.Math;
using MicroEngine.Core.Graphics;
using MicroEngine.Core.Logging;
using System;

namespace MicroEngine.Game.Scenes.Demos.Zelda.Systems;

/// <summary>
/// Coordinates combat interactions between the player and enemies, applying damage and knockback.
/// </summary>
public class CombatSystem : ISystem, IDisposable
{
    private readonly EventBus _eventBus;
    private readonly ILogger _logger;
    private readonly ISoundPlayer _soundPlayer;
    private CachedQuery? _enemyQuery;
    private CachedQuery? _playerQuery;
    private bool _isSubscribed;
    private World? _currentWorld;
    private CachedQuery? _mapQuery;

    /// <summary>
    /// Gets or sets a value indicating whether debug combat visuals are shown.
    /// </summary>
    public bool ShowDebug { get; set; } = false;

    /// <summary>
    /// Gets the last calculated attack point for debugging.
    /// </summary>
    public Vector2 LastAttackPoint { get; private set; }

    /// <summary>
    /// Gets the last attack radius used for hit detection.
    /// </summary>
    public float LastAttackRadius { get; private set; } = ZeldaConstants.ATTACK_HIT_THRESHOLD;

    /// <summary>
    /// Gets the proximity radius used to detect near hits between player and enemies.
    /// </summary>
    public float PlayerProximityRadius { get; private set; } = ZeldaConstants.PROXIMITY_HIT_THRESHOLD;

    /// <summary>
    /// Gets the last computed center position of the player's body for collision checks.
    /// </summary>
    public Vector2 LastPlayerBodyCenter { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CombatSystem"/> class.
    /// </summary>
    public CombatSystem(EventBus eventBus, ILogger logger, ISoundPlayer soundPlayer)
    {
        _eventBus = eventBus;
        _logger = logger;
        _soundPlayer = soundPlayer;
    }

    /// <summary>
    /// Updates combat interactions, applies damage, and processes invulnerability timers.
    /// </summary>
    public void Update(World world, float deltaTime)
    {
        _currentWorld = world;
        if (!_isSubscribed)
        {
            _eventBus.Subscribe<DamageEvent>(OnDamage);
            _isSubscribed = true;
        }

        _enemyQuery ??= world.CreateCachedQuery(typeof(EnemyComponent), typeof(TransformComponent), typeof(HealthComponent));
        _playerQuery ??= world.CreateCachedQuery(typeof(PlayerComponent), typeof(TransformComponent), typeof(HealthComponent));

        if (_playerQuery.Count == 0)
        {
            return;
        }

        var playerEntity = _playerQuery.Entities[0];
        ref var playerHealth = ref world.GetComponent<HealthComponent>(playerEntity);
        ref var playerTransform = ref world.GetComponent<TransformComponent>(playerEntity);
        ref var player = ref world.GetComponent<PlayerComponent>(playerEntity);

        // Stop all combat if player is dead
        if (playerHealth.Current <= 0)
        {
            return;
        }

        // 1. Enemy hitting Player
        // Calculate body centers for fair damage detection (pivots are at the feet)
        Vector2 playerBodyCenter = playerTransform.Position + new Vector2(0, -ZeldaConstants.HERO_DRAW_SIZE * 0.5f);
        LastPlayerBodyCenter = playerBodyCenter;
        
        foreach (var enemyEntity in _enemyQuery.Entities)
        {
            if (!world.IsEntityValid(enemyEntity))
            {
                continue;
            }
            
            var enemyTransform = world.GetComponent<TransformComponent>(enemyEntity);
            float slimeDrawSize = ZeldaConstants.TILE_SIZE * ZeldaConstants.ENEMY_SCALE;
            Vector2 enemyBodyCenter = enemyTransform.Position + new Vector2(0, -slimeDrawSize * ZeldaConstants.ENEMY_BODY_CENTER_Y_FACTOR);

            if (Vector2.Distance(playerBodyCenter, enemyBodyCenter) < ZeldaConstants.PROXIMITY_HIT_THRESHOLD)
            {
                if (playerHealth.InvulnerabilityTimer <= 0)
                {
                    _eventBus.Queue<DamageEvent>(e =>
                    {
                        e.TargetEntity = playerEntity;
                        e.DamageAmount = ZeldaConstants.ENEMY_DAMAGE;
                        e.AttackerEntity = enemyEntity;
                    });
                }
            }
        }

        // 2. Player -> Enemy combat (only when player is attacking)
        if (player.State == PlayerState.Attacking)
        {
            var animator = world.GetComponent<AnimatorComponent>(playerEntity);
            Vector2 attackOffset = Vector2.Zero;
            float swordReach = ZeldaConstants.SWORD_REACH; 
            
            string clipName = animator.CurrentClipName ?? "";
            if (clipName.Contains("up"))
            {
                attackOffset = new Vector2(0, -swordReach);
            }
            else if (clipName.Contains("down"))
            {
                attackOffset = new Vector2(0, swordReach);
            }
            else if (clipName.Contains("left"))
            {
                attackOffset = new Vector2(-swordReach, 0);
            }
            else if (clipName.Contains("right"))
            {
                attackOffset = new Vector2(swordReach, 0);
            }

            // Calculate attack start point from the body center calculated in step 1
            Vector2 attackPoint = playerBodyCenter + attackOffset;
            LastAttackPoint = attackPoint;
            LastAttackRadius = ZeldaConstants.ATTACK_HIT_THRESHOLD;
            PlayerProximityRadius = ZeldaConstants.PROXIMITY_HIT_THRESHOLD;

            foreach (var enemyEntity in _enemyQuery.Entities)
            {
                if (!world.IsEntityValid(enemyEntity))
                {
                    continue;
                }
                
                var enemyTransform = world.GetComponent<TransformComponent>(enemyEntity);
                ref var enemyHealth = ref world.GetComponent<HealthComponent>(enemyEntity);

                float slimeDrawSize = ZeldaConstants.TILE_SIZE * ZeldaConstants.ENEMY_SCALE;
                Vector2 enemyBodyCenter = enemyTransform.Position + new Vector2(0, -slimeDrawSize * ZeldaConstants.ENEMY_BODY_CENTER_Y_FACTOR);

                // Use body centers for both parties for more "natural" feeling hits
                float distToAttackPoint = Vector2.Distance(attackPoint, enemyBodyCenter);
                float distToPlayer = Vector2.Distance(playerBodyCenter, enemyBodyCenter);

                if (enemyHealth.InvulnerabilityTimer <= 0 && 
                   (distToAttackPoint < ZeldaConstants.ATTACK_HIT_THRESHOLD || distToPlayer < ZeldaConstants.PROXIMITY_HIT_THRESHOLD))
                {
                    _logger.Info(ZeldaConstants.LOG_COMBAT, $"Player hit enemy {enemyEntity.Id}. DistToAP: {distToAttackPoint:F1}, DistToP: {distToPlayer:F1}");
                    _eventBus.Queue<DamageEvent>(e =>
                    {
                        e.TargetEntity = enemyEntity;
                        e.DamageAmount = ZeldaConstants.SWORD_DAMAGE;
                        e.AttackerEntity = playerEntity;
                    });
                }
            }
        }

        // 3. Update timers
        if (playerHealth.InvulnerabilityTimer > 0)
        {
            playerHealth.InvulnerabilityTimer -= deltaTime;
            if (playerHealth.InvulnerabilityTimer <= 0)
            {
                ref var sprite = ref world.GetComponent<SpriteComponent>(playerEntity);
                sprite.Tint = Color.White;
            }
        }

        foreach (var enemyEntity in _enemyQuery.Entities)
        {
            if (!world.IsEntityValid(enemyEntity))
            {
                continue;
            }
            
            ref var enemyHealth = ref world.GetComponent<HealthComponent>(enemyEntity);
            if (enemyHealth.InvulnerabilityTimer > 0)
            {
                enemyHealth.InvulnerabilityTimer -= deltaTime;
                if (enemyHealth.InvulnerabilityTimer <= 0)
                {
                    ref var sprite = ref world.GetComponent<SpriteComponent>(enemyEntity);
                    sprite.Tint = Color.White;
                }
            }
        }

        _eventBus.ProcessEvents();

        // 4. Check for Victory
        if (_enemyQuery.Count == 0 && playerHealth.Current > 0)
        {
            _eventBus.Queue<GameStateEvent>(e =>
            {
                e.Message = ZeldaConstants.MSG_VICTORY;
                e.IsGameOver = true;
            });
        }
    }

    private void OnDamage(DamageEvent e)
    {
        if (_currentWorld == null || !_currentWorld.IsEntityValid(e.TargetEntity))
        {
            return;
        }

        if (_currentWorld.HasComponent<HealthComponent>(e.TargetEntity) && 
            _currentWorld.HasComponent<SpriteComponent>(e.TargetEntity))
        {
            ref var health = ref _currentWorld.GetComponent<HealthComponent>(e.TargetEntity);
            ref var sprite = ref _currentWorld.GetComponent<SpriteComponent>(e.TargetEntity);
            
            health.Current -= e.DamageAmount;
            health.InvulnerabilityTimer = ZeldaConstants.INVULNERABILITY_DURATION;

            _logger.Info(ZeldaConstants.LOG_COMBAT, $"Entity {e.TargetEntity.Id} took {e.DamageAmount} damage. HP: {health.Current}");

            // Flash red
            sprite.Tint = ZeldaConstants.COLOR_DAMAGE;

            // Play hit sound if target has audio component
            if (_currentWorld.HasComponent<AudioComponent>(e.TargetEntity))
            {
                var audio = _currentWorld.GetComponent<AudioComponent>(e.TargetEntity);
                if (audio.HitClip != null)
                {
                    _eventBus.Queue<PlaySoundEvent>(evt => evt.Clip = audio.HitClip);
                }
            }

            // Simple Knockback
            if (_currentWorld.HasComponent<TransformComponent>(e.TargetEntity) && 
                _currentWorld.HasComponent<TransformComponent>(e.AttackerEntity))
            {
                ref var targetTrans = ref _currentWorld.GetComponent<TransformComponent>(e.TargetEntity);
                var attackerTrans = _currentWorld.GetComponent<TransformComponent>(e.AttackerEntity);
                
                Vector2 knockDir = (targetTrans.Position - attackerTrans.Position).Normalized;
                Vector2 nextPos = targetTrans.Position + knockDir * ZeldaConstants.KNOCKBACK_FORCE;
                
                // Only apply knockback if the destination is passable
                _mapQuery ??= _currentWorld.CreateCachedQuery(typeof(MapComponent));
                if (_mapQuery.Count > 0)
                {
                    var mapEntity = _mapQuery.Entities[0];
                    var map = _currentWorld.GetComponent<MapComponent>(mapEntity);
                    if (IsPassable(map, nextPos, ZeldaConstants.ENEMY_COLLISION_RADIUS))
                    {
                        targetTrans.Position = nextPos;
                    }
                }
            }

            if (health.Current <= 0)
            {
                if (_currentWorld.HasComponent<PlayerComponent>(e.TargetEntity))
                {
                    _eventBus.Queue<GameStateEvent>(evt =>
                    {
                        evt.Message = ZeldaConstants.MSG_GAME_OVER;
                        evt.IsGameOver = true;
                    });
                    sprite.Visible = false;
                }
                else
                {
                    _currentWorld.DestroyEntity(e.TargetEntity);
                }
            }
        }
    }

    /// <summary>
    /// Releases event subscriptions held by the combat system.
    /// </summary>
    public void Dispose()
    {
        _eventBus.Unsubscribe<DamageEvent>(OnDamage);
    }

    private bool IsPassable(MapComponent map, Vector2 worldPos, float radius)
    {
        Vector2[] points = 
        {
            worldPos + new Vector2(-radius, -radius),
            worldPos + new Vector2(radius, -radius),
            worldPos + new Vector2(-radius, radius),
            worldPos + new Vector2(radius, radius)
        };

        foreach (var p in points)
        {
            int tx = (int)(p.X / ZeldaConstants.TILE_SIZE);
            int ty = (int)(p.Y / ZeldaConstants.TILE_SIZE);
            if (!map.IsPassable(tx, ty)) 
            {
                return false;
            }
        }
        return true;
    }
}

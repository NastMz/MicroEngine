using MicroEngine.Core.ECS;
using MicroEngine.Core.ECS.Components;
using MicroEngine.Core.Events;
using MicroEngine.Core.Math;
using MicroEngine.Core.Graphics;
using MicroEngine.Core.Logging;

namespace MicroEngine.Game.Scenes.Demos.Zelda.Systems;

public class CombatSystem : ISystem, IDisposable
{
    private readonly EventBus _eventBus;
    private readonly ILogger _logger;
    private CachedQuery? _enemyQuery;
    private CachedQuery? _playerQuery;
    private bool _isSubscribed;
    private World? _currentWorld;
    private readonly ZeldaScene _scene;
    public bool ShowDebug { get; set; } = false;
    public Vector2 LastAttackPoint { get; private set; }
    public float LastAttackRadius { get; private set; } = ZeldaConstants.ATTACK_HIT_THRESHOLD;
    public float PlayerProximityRadius { get; private set; } = ZeldaConstants.PROXIMITY_HIT_THRESHOLD;
    public Vector2 LastPlayerBodyCenter { get; private set; }

    public CombatSystem(EventBus eventBus, ILogger logger, ZeldaScene scene)
    {
        _eventBus = eventBus;
        _logger = logger;
        _scene = scene;
    }

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

        if (_playerQuery.Count == 0) return;

        var playerEntity = _playerQuery.Entities[0];
        ref var playerHealth = ref world.GetComponent<HealthComponent>(playerEntity);
        ref var playerTransform = ref world.GetComponent<TransformComponent>(playerEntity);
        ref var player = ref world.GetComponent<PlayerComponent>(playerEntity);

        // Stop all combat if player is dead
        if (playerHealth.Current <= 0) return;

        // 1. Enemy hitting Player
        // Calculate body centers for fair damage detection (pivots are at the feet)
        Vector2 playerBodyCenter = playerTransform.Position + new Vector2(0, -ZeldaConstants.HERO_DRAW_SIZE * 0.5f);
        LastPlayerBodyCenter = playerBodyCenter;
        
        foreach (var enemyEntity in _enemyQuery.Entities)
        {
            var enemyTransform = world.GetComponent<TransformComponent>(enemyEntity);
            float slimeDrawSize = ZeldaConstants.TILE_SIZE * ZeldaConstants.ENEMY_SCALE;
            Vector2 enemyBodyCenter = enemyTransform.Position + new Vector2(0, -slimeDrawSize * 0.4f);

            if (Vector2.Distance(playerBodyCenter, enemyBodyCenter) < ZeldaConstants.PROXIMITY_HIT_THRESHOLD)
            {
                if (playerHealth.InvulnerabilityTimer <= 0)
                {
                    _eventBus.Queue(new DamageEvent(playerEntity, ZeldaConstants.ENEMY_DAMAGE, enemyEntity));
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
            if (clipName.Contains("up")) attackOffset = new Vector2(0, -swordReach);
            else if (clipName.Contains("down")) attackOffset = new Vector2(0, swordReach);
            else if (clipName.Contains("left")) attackOffset = new Vector2(-swordReach, 0);
            else if (clipName.Contains("right")) attackOffset = new Vector2(swordReach, 0);

            // Calculate attack start point from the body center calculated in step 1
            Vector2 attackPoint = playerBodyCenter + attackOffset;
            LastAttackPoint = attackPoint;
            LastAttackRadius = ZeldaConstants.ATTACK_HIT_THRESHOLD;
            PlayerProximityRadius = ZeldaConstants.PROXIMITY_HIT_THRESHOLD;

            foreach (var enemyEntity in _enemyQuery.Entities)
            {
                var enemyTransform = world.GetComponent<TransformComponent>(enemyEntity);
                ref var enemyHealth = ref world.GetComponent<HealthComponent>(enemyEntity);

                float slimeDrawSize = ZeldaConstants.TILE_SIZE * ZeldaConstants.ENEMY_SCALE;
                Vector2 enemyBodyCenter = enemyTransform.Position + new Vector2(0, -slimeDrawSize * 0.4f);

                // Use body centers for both parties for more "natural" feeling hits
                float distToAttackPoint = Vector2.Distance(attackPoint, enemyBodyCenter);
                float distToPlayer = Vector2.Distance(playerBodyCenter, enemyBodyCenter);

                if (enemyHealth.InvulnerabilityTimer <= 0 && 
                   (distToAttackPoint < ZeldaConstants.ATTACK_HIT_THRESHOLD || distToPlayer < ZeldaConstants.PROXIMITY_HIT_THRESHOLD))
                {
                    _logger.Info("Combat", $"Player hit enemy {enemyEntity.Id}. DistToAP: {distToAttackPoint:F1}, DistToP: {distToPlayer:F1}");
                    _eventBus.Queue(new DamageEvent(enemyEntity, ZeldaConstants.SWORD_DAMAGE, playerEntity));
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
            _eventBus.Publish(new ZeldaGameStateEvent(ZeldaConstants.MSG_VICTORY, true));
        }
    }

    private void OnDamage(DamageEvent e)
    {
        if (_currentWorld == null || !_currentWorld.IsEntityValid(e.TargetEntity)) return;

        if (_currentWorld.HasComponent<HealthComponent>(e.TargetEntity) && 
            _currentWorld.HasComponent<SpriteComponent>(e.TargetEntity))
        {
            ref var health = ref _currentWorld.GetComponent<HealthComponent>(e.TargetEntity);
            ref var sprite = ref _currentWorld.GetComponent<SpriteComponent>(e.TargetEntity);
            
            health.Current -= e.DamageAmount;
            health.InvulnerabilityTimer = ZeldaConstants.INVULNERABILITY_DURATION;

            _logger.Info("Combat", $"Entity {e.TargetEntity.Id} took {e.DamageAmount} damage. HP: {health.Current}");

            // Flash red
            sprite.Tint = ZeldaConstants.COLOR_DAMAGE;

            // Simple Knockback
            if (_currentWorld.HasComponent<TransformComponent>(e.TargetEntity) && 
                _currentWorld.HasComponent<TransformComponent>(e.AttackerEntity))
            {
                ref var targetTrans = ref _currentWorld.GetComponent<TransformComponent>(e.TargetEntity);
                var attackerTrans = _currentWorld.GetComponent<TransformComponent>(e.AttackerEntity);
                
                Vector2 knockDir = (targetTrans.Position - attackerTrans.Position).Normalized;
                Vector2 nextPos = targetTrans.Position + knockDir * ZeldaConstants.KNOCKBACK_FORCE;
                
                // Only apply knockback if the destination is passable
                if (_scene.IsPassable(nextPos, 10f))
                {
                    targetTrans.Position = nextPos;
                }
            }

            if (health.Current <= 0)
            {
                if (_currentWorld.HasComponent<PlayerComponent>(e.TargetEntity))
                {
                    _eventBus.Publish(new ZeldaGameStateEvent(ZeldaConstants.MSG_GAME_OVER, true));
                    sprite.Visible = false;
                }
                else
                {
                    _currentWorld.DestroyEntity(e.TargetEntity);
                }
            }
        }
    }

    public void Dispose()
    {
        _eventBus.Unsubscribe<DamageEvent>(OnDamage);
    }
}

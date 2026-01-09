using MicroEngine.Core.ECS;
using MicroEngine.Core.ECS.Components;
using MicroEngine.Core.Math;

namespace MicroEngine.Game.Scenes.Demos.Zelda.Systems;

/// <summary>
/// Drives enemy AI behavior to chase the player and animate movement.
/// </summary>
public class EnemyAISystem : ISystem
{
    private CachedQuery? _enemyQuery;
    private CachedQuery? _playerQuery;
    private CachedQuery? _mapQuery;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnemyAISystem"/> class.
    /// </summary>
    public EnemyAISystem()
    {
    }

    /// <summary>
    /// Updates enemy movement and animation towards the player target.
    /// </summary>
    public void Update(World world, float deltaTime)
    {
        _enemyQuery ??= world.CreateCachedQuery(typeof(EnemyComponent), typeof(TransformComponent), typeof(AnimatorComponent));
        _playerQuery ??= world.CreateCachedQuery(typeof(PlayerComponent), typeof(TransformComponent));
        _mapQuery ??= world.CreateCachedQuery(typeof(MapComponent));

        if (_playerQuery.Count == 0 || _mapQuery.Count == 0)
        {
            return;
        }

        var mapEntity = _mapQuery.Entities[0];
        var map = world.GetComponent<MapComponent>(mapEntity);

        // Target the closest player or first player
        var playerEntity = _playerQuery.Entities[0];
        var playerTransform = world.GetComponent<TransformComponent>(playerEntity);
        var playerPos = playerTransform.Position;

        foreach (var entity in _enemyQuery.Entities)
        {
            if (!world.IsEntityValid(entity))
            {
                continue;
            }
            
            ref var enemy = ref world.GetComponent<EnemyComponent>(entity);
            ref var transform = ref world.GetComponent<TransformComponent>(entity);
            ref var animator = ref world.GetComponent<AnimatorComponent>(entity);

            float distance = Vector2.Distance(transform.Position, playerPos);

            if (distance < ZeldaConstants.ENEMY_DETECTION_RADIUS && distance > 10f)
            {
                Vector2 direction = (playerPos - transform.Position).Normalized;
                
                // Collision checks for enemies with radius
                Vector2 nextPosX = transform.Position + new Vector2(direction.X * enemy.Speed * deltaTime, 0);
                if (IsPassable(map, nextPosX, ZeldaConstants.ENEMY_COLLISION_RADIUS))
                {
                    transform.Position = nextPosX;
                }
                
                Vector2 nextPosY = transform.Position + new Vector2(0, direction.Y * enemy.Speed * deltaTime);
                if (IsPassable(map, nextPosY, ZeldaConstants.ENEMY_COLLISION_RADIUS))
                {
                    transform.Position = nextPosY;
                }
                
                animator.IsPlaying = true;
                animator.CurrentClipName = ZeldaConstants.CLIP_ENEMY_IDLE; 
            }
            else if (distance <= 10f)
            {
                // Attack player logic (e.g., publish damage event)
            }
            else
            {
                // Idle behavior
                animator.IsPlaying = true;
                animator.CurrentClipName = ZeldaConstants.CLIP_ENEMY_IDLE;
            }
        }
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

using MicroEngine.Core.ECS;
using MicroEngine.Core.ECS.Components;
using MicroEngine.Core.Math;

namespace MicroEngine.Game.Scenes.Demos.Zelda.Systems;

public class EnemyAISystem : ISystem
{
    private readonly ZeldaScene _scene;
    private CachedQuery? _enemyQuery;
    private CachedQuery? _playerQuery;

    public EnemyAISystem(ZeldaScene scene)
    {
        _scene = scene;
    }

    public void Update(World world, float deltaTime)
    {
        _enemyQuery ??= world.CreateCachedQuery(typeof(EnemyComponent), typeof(TransformComponent), typeof(AnimatorComponent));
        _playerQuery ??= world.CreateCachedQuery(typeof(PlayerComponent), typeof(TransformComponent));

        if (_playerQuery.Count == 0) return;

        // Target the closest player or first player
        var playerEntity = _playerQuery.Entities[0];
        var playerTransform = world.GetComponent<TransformComponent>(playerEntity);
        var playerPos = playerTransform.Position;

        foreach (var entity in _enemyQuery.Entities)
        {
            ref var enemy = ref world.GetComponent<EnemyComponent>(entity);
            ref var transform = ref world.GetComponent<TransformComponent>(entity);
            ref var animator = ref world.GetComponent<AnimatorComponent>(entity);

            float distance = Vector2.Distance(transform.Position, playerPos);

            if (distance < ZeldaConstants.ENEMY_DETECTION_RADIUS && distance > 10f)
            {
                Vector2 direction = (playerPos - transform.Position).Normalized;
                
                // Collision checks for enemies with radius
                Vector2 nextPosX = transform.Position + new Vector2(direction.X * enemy.Speed * deltaTime, 0);
                if (_scene.IsPassable(nextPosX, 8f)) transform.Position = nextPosX;
                
                Vector2 nextPosY = transform.Position + new Vector2(0, direction.Y * enemy.Speed * deltaTime);
                if (_scene.IsPassable(nextPosY, 8f)) transform.Position = nextPosY;
                
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
}

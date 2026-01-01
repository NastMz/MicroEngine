using MicroEngine.Core.ECS;
using MicroEngine.Core.ECS.Components;
using MicroEngine.Core.Input;
using MicroEngine.Core.Events;
using MicroEngine.Core.Math;
using MicroEngine.Core.Logging;

namespace MicroEngine.Game.Scenes.Demos.Zelda.Systems;

public class PlayerSystem : ISystem
{
    private readonly IInputBackend _input;
    private readonly EventBus _eventBus;
    private readonly ILogger _logger;
    private readonly ZeldaScene _scene;
    private CachedQuery? _playerQuery;
    private bool _isControlDisabled;

    public PlayerSystem(IInputBackend input, EventBus eventBus, ILogger logger, ZeldaScene scene)
    {
        _input = input;
        _eventBus = eventBus;
        _logger = logger;
        _scene = scene;
        _eventBus.Subscribe<ZeldaGameStateEvent>(e => _isControlDisabled = e.IsGameOver);
    }

    public void Update(World world, float deltaTime)
    {
        _playerQuery ??= world.CreateCachedQuery(typeof(PlayerComponent), typeof(TransformComponent), typeof(AnimatorComponent), typeof(HealthComponent));

        foreach (var entity in _playerQuery.Entities)
        {
            if (!world.IsEntityValid(entity)) continue;

            ref var player = ref world.GetComponent<PlayerComponent>(entity);
            ref var transform = ref world.GetComponent<TransformComponent>(entity);
            ref var animator = ref world.GetComponent<AnimatorComponent>(entity);
            ref var health = ref world.GetComponent<HealthComponent>(entity);

            if (health.Current <= 0 || _isControlDisabled)
            {
                animator.IsPlaying = false;
                continue;
            }

            // --- 1. Handle Input Latching (Robustness for FixedUpdate) ---
            bool spaceDown = _input.IsKeyDown(Key.Space) || _input.IsKeyDown(Key.J);
            bool attackTriggered = false;

            if (spaceDown)
            {
                if (!player.AttackInputLatched)
                {
                    attackTriggered = true;
                    player.AttackInputLatched = true;
                }
            }
            else
            {
                player.AttackInputLatched = false;
            }

            // --- 2. Handle Attacking State ---
            if (player.State == PlayerState.Attacking)
            {
                // If the attack animation has finished, transition back to idle
                if (!animator.IsPlaying)
                {
                    player.State = PlayerState.Idle;
                    animator.CurrentClipName = animator.CurrentClipName?.Replace("attack", "walk") ?? ZeldaConstants.CLIP_WALK_DOWN;
                    animator.IsPlaying = false;
                    animator.CurrentFrame = 0;
                }
                else
                {
                    // Block input and movement while attacking
                    continue; 
                }
            }

            if (attackTriggered)
            {
                player.State = PlayerState.Attacking;
                player.AttackTimer = player.AttackDuration;
                
                string currentClip = animator.CurrentClipName ?? ZeldaConstants.CLIP_WALK_DOWN;
                string dirSuffix = "down";
                if (currentClip.Contains("up")) dirSuffix = "up";
                else if (currentClip.Contains("left")) dirSuffix = "left";
                else if (currentClip.Contains("right")) dirSuffix = "right";
                
                _logger.Info("Player", $"Attack Triggered! Dir: {dirSuffix}");
                animator.CurrentClipName = $"attack_{dirSuffix}";
                animator.IsPlaying = true;
                animator.CurrentFrame = 0;
                animator.FrameTimer = 0;
                _scene.PlaySound(_scene.SwordClip);
                continue; 
            }

            // --- 4. Handle Movement ---
            Vector2 movement = Vector2.Zero;
            bool moved = false;

            if (_input.IsKeyDown(Key.W) || _input.IsKeyDown(Key.Up)) { movement += new Vector2(0, -1); moved = true; }
            if (_input.IsKeyDown(Key.S) || _input.IsKeyDown(Key.Down)) { movement += new Vector2(0, 1); moved = true; }
            if (_input.IsKeyDown(Key.A) || _input.IsKeyDown(Key.Left)) { movement += new Vector2(-1, 0); moved = true; }
            if (_input.IsKeyDown(Key.D) || _input.IsKeyDown(Key.Right)) { movement += new Vector2(1, 0); moved = true; }

            if (moved)
            {
                movement = movement.Normalized;
                
                // Try move X with collision radius
                Vector2 nextPosX = transform.Position + new Vector2(movement.X * player.Speed * deltaTime, 0);
                if (_scene.IsPassable(nextPosX, 10f)) { transform.Position = nextPosX; }
                
                // Try move Y with collision radius
                Vector2 nextPosY = transform.Position + new Vector2(0, movement.Y * player.Speed * deltaTime);
                if (_scene.IsPassable(nextPosY, 10f)) { transform.Position = nextPosY; }
                
                player.State = PlayerState.Walking;

                if (MathF.Abs(movement.X) > MathF.Abs(movement.Y))
                {
                    animator.CurrentClipName = movement.X > 0 ? ZeldaConstants.CLIP_WALK_RIGHT : ZeldaConstants.CLIP_WALK_LEFT;
                }
                else
                {
                    animator.CurrentClipName = movement.Y > 0 ? ZeldaConstants.CLIP_WALK_DOWN : ZeldaConstants.CLIP_WALK_UP;
                }
                animator.IsPlaying = true;
            }
            else
            {
                player.State = PlayerState.Idle;
                animator.IsPlaying = false; 
            }
        }
    }
}

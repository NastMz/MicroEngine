using MicroEngine.Core.ECS;
using MicroEngine.Core.ECS.Components;

namespace MicroEngine.Game.Scenes.Demos.Zelda.Systems;

/// <summary>
/// Updates animator components and applies the current sprite region for each frame.
/// </summary>
public class AnimationSystem : ISystem
{
    private CachedQuery? _animQuery;

    /// <summary>
    /// Advances animation clips and updates sprite regions.
    /// </summary>
    public void Update(World world, float deltaTime)
    {
        _animQuery ??= world.CreateCachedQuery(typeof(AnimatorComponent), typeof(SpriteComponent));

        foreach (var entity in _animQuery.Entities)
        {
            if (!world.IsEntityValid(entity))
            {
                continue;
            }
            ref var animator = ref world.GetComponent<AnimatorComponent>(entity);
            ref var sprite = ref world.GetComponent<SpriteComponent>(entity);

            if (animator.Clips == null || animator.Clips.Count == 0 || string.IsNullOrEmpty(animator.CurrentClipName))
            {
                continue;
            }

            string currentClipName = animator.CurrentClipName;
            var clip = animator.Clips.Find(c => c.Name == currentClipName);
            if (clip == null)
            {
                continue;
            }

            if (animator.IsPlaying)
            {
                animator.FrameTimer += deltaTime * animator.Speed;
                if (animator.FrameTimer >= clip.FrameDuration)
                {
                    animator.FrameTimer = 0;
                    animator.CurrentFrame++;

                    if (animator.CurrentFrame >= clip.Frames.Count)
                    {
                        if (clip.Loop)
                        {
                            animator.CurrentFrame = 0;
                        }
                        else
                        {
                            animator.CurrentFrame = clip.Frames.Count - 1;
                            animator.IsPlaying = false;
                            animator.IsFinished = true;
                        }
                    }
                }
            }

            sprite.Region = clip.Frames[animator.CurrentFrame];
        }
    }
}

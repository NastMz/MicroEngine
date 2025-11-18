using MicroEngine.Core.Graphics;

namespace MicroEngine.Core.ECS.Components;

/// <summary>
/// Component for animation data.
/// Pure data component - contains no logic.
/// Logic is handled by AnimationSystem.
/// </summary>
public struct AnimatorComponent : IComponent
{
    /// <summary>
    /// Gets or sets the list of animation clips.
    /// </summary>
    public List<AnimationClip>? Clips { get; set; }
    
    /// <summary>
    /// Gets or sets the currently playing clip name.
    /// </summary>
    public string? CurrentClipName { get; set; }

    /// <summary>
    /// Gets or sets the current frame index.
    /// </summary>
    public int CurrentFrame { get; set; }

    /// <summary>
    /// Gets or sets the frame timer.
    /// </summary>
    public float FrameTimer { get; set; }

    /// <summary>
    /// Gets or sets whether the animator is playing.
    /// </summary>
    public bool IsPlaying { get; set; }

    /// <summary>
    /// Gets or sets the playback speed multiplier.
    /// </summary>
    public float Speed { get; set; }

    /// <summary>
    /// Gets or sets whether the current animation has finished playing.
    /// </summary>
    public bool IsFinished { get; set; }
}

/// <summary>
/// Represents a sequence of sprite frames that make up an animation.
/// This is a data class, not a component.
/// </summary>
public sealed class AnimationClip
{
    /// <summary>
    /// Gets the name of the animation clip.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the list of sprite regions (frames) in this animation.
    /// </summary>
    public IReadOnlyList<SpriteRegion> Frames { get; }

    /// <summary>
    /// Gets the duration of each frame in seconds.
    /// </summary>
    public float FrameDuration { get; }

    /// <summary>
    /// Gets whether the animation should loop.
    /// </summary>
    public bool Loop { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AnimationClip"/> class.
    /// </summary>
    /// <param name="name">The name of the animation.</param>
    /// <param name="frames">The sprite regions (frames).</param>
    /// <param name="frameDuration">Duration of each frame in seconds.</param>
    /// <param name="loop">Whether the animation should loop.</param>
    public AnimationClip(string name, IReadOnlyList<SpriteRegion> frames, float frameDuration, bool loop = true)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Animation name cannot be null or empty", nameof(name));
        }

        if (frames == null || frames.Count == 0)
        {
            throw new ArgumentException("Animation must have at least one frame", nameof(frames));
        }

        if (frameDuration <= 0)
        {
            throw new ArgumentException("Frame duration must be positive", nameof(frameDuration));
        }

        Name = name;
        Frames = frames;
        FrameDuration = frameDuration;
        Loop = loop;
    }

    /// <summary>
    /// Creates an animation clip with frames per second.
    /// </summary>
    /// <param name="name">The name of the animation.</param>
    /// <param name="frames">The sprite regions (frames).</param>
    /// <param name="framesPerSecond">Frames per second (FPS).</param>
    /// <param name="loop">Whether the animation should loop.</param>
    /// <returns>A new animation clip.</returns>
    public static AnimationClip FromFPS(string name, IReadOnlyList<SpriteRegion> frames, float framesPerSecond, bool loop = true)
    {
        if (framesPerSecond <= 0)
        {
            throw new ArgumentException("Frames per second must be positive", nameof(framesPerSecond));
        }

        var frameDuration = 1f / framesPerSecond;
        return new AnimationClip(name, frames, frameDuration, loop);
    }

    /// <summary>
    /// Gets the total duration of the animation in seconds.
    /// </summary>
    public float GetTotalDuration()
    {
        return Frames.Count * FrameDuration;
    }
}

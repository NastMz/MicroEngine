using System.Numerics;
using MicroEngine.Kernel;

namespace MicroEngine.Graphics;

/// <summary>An effect played by <see cref="SpriteEffects.Play"/>: asks whether it still plays, moves it or stops it. The default value is no effect.</summary>
public readonly record struct SpriteEffect
{
    internal SpriteEffect(long id) => Id = id;
    internal long Id { get; }
}

/// <summary>
/// One-shot sprite animations: "play this clip once here, after a delay, mirrored, tinted, then go away". Impacts,
/// puffs of smoke, sparkles and explosions are each one call, with no object to keep and nothing to remove. A scene
/// has one in <c>Scene.Effects</c>, stepped and cleared with the scene; a game whose rules live outside a scene makes
/// its own and calls <see cref="Update"/>.
/// </summary>
/// <remarks>
/// <para>
/// An effect shows nothing during its delay, then the clip's frames at the clip's own timing (or at the rate given),
/// once: a looping clip plays one pass. It ends on the update its time reaches the clip's length. Effects draw in the
/// order they were played; into a <see cref="SortedRenderer"/> each draws at its own layer and depth.
/// </para>
/// <para>
/// Playing, stepping and drawing allocate nothing once the pool is as large as the most effects alive at once (it
/// grows by itself).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // when the shot lands
/// Effects.Play(atlas.Clip("explosion"), hit.Position);
/// Effects.Play(atlas.Clip("spark"), hit.Position + new Vector2(6, -4), delay: 0.08f, flipX: true, tint: Color.Yellow);
///
/// // in Draw
/// Effects.Draw(renderer);
/// </code>
/// </example>
public sealed class SpriteEffects : IStateHashed
{
    private struct Playing
    {
        public long Id;
        public AnimationClip Clip;
        public Vector2 At;
        public float Time, Delay;
        public float Rate;        // frames per second; 0 plays the clip's own timing
        public bool FlipX;
        public Color Tint;
        public int Layer;
        public float Depth;
    }

    private Playing[] _playing;
    private int _count;
    private long _nextId = 1;

    /// <summary>Creates an empty player.</summary>
    /// <param name="capacity">How many effects it holds before it grows.</param>
    public SpriteEffects(int capacity = 32)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        _playing = new Playing[Math.Max(capacity, 1)];
    }

    /// <summary>How many effects are waiting or playing.</summary>
    public int Count => _count;
    /// <summary>Whether any effect is waiting or playing: a turn-based rule waits for it before going on.</summary>
    public bool Busy => _count > 0;
    /// <summary>
    /// Whether effects are drawn on whole pixels (their position rounded), as pixel art on a low-resolution canvas wants.
    /// False by default: an effect is drawn where it was played.
    /// </summary>
    public bool SnapToPixels { get; set; }

    /// <summary>Plays <paramref name="clip"/> once with its anchor at <paramref name="at"/>, then removes it.</summary>
    /// <param name="clip">The frames and their timing (<see cref="SpriteAtlas.Clip(string, bool?)"/>).</param>
    /// <param name="at">Where the frames' anchor goes, in the coordinates the effects are drawn in.</param>
    /// <param name="delay">Seconds before it shows; a negative value, an infinite one or one that is not a number is none.</param>
    /// <param name="flipX">Mirrors it around its anchor.</param>
    /// <param name="tint">Colour multiplier; null for white (unchanged).</param>
    /// <param name="framesPerSecond">Plays it at this rate instead of the clip's own timing.</param>
    /// <param name="layer">Its layer in a <see cref="SortedRenderer"/> block.</param>
    /// <param name="depth">Its depth within the layer in a <see cref="SortedRenderer"/> block.</param>
    /// <returns>The effect, to ask about, move or stop; most callers ignore it.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="framesPerSecond"/> is not a positive number.</exception>
    public SpriteEffect Play(AnimationClip clip, Vector2 at, float delay = 0, bool flipX = false, Color? tint = null, float? framesPerSecond = null, int layer = 0, float depth = 0)
    {
        ArgumentNullException.ThrowIfNull(clip);
        if (framesPerSecond is { } rate && (!(rate > 0) || float.IsInfinity(rate))) throw new ArgumentOutOfRangeException(nameof(framesPerSecond), rate, "An effect's rate is positive.");
        if (_count == _playing.Length) Array.Resize(ref _playing, _playing.Length * 2);
        long id = _nextId++;
        _playing[_count++] = new Playing
        {
            // A delay that never ends would keep whoever waits on Busy waiting for ever.
            Id = id, Clip = clip, At = at, Delay = delay > 0 && float.IsFinite(delay) ? delay : 0, FlipX = flipX, Tint = tint ?? Color.White,
            // A clip with a fixed rate is counted in frames, exactly as (int)(time * rate).
            Rate = framesPerSecond ?? (clip.HasFrameDurations ? 0 : clip.FramesPerSecond), Layer = layer, Depth = float.IsFinite(depth) ? depth : 0,
        };
        return new SpriteEffect(id);
    }

    /// <summary>Whether <paramref name="effect"/> is still waiting or playing.</summary>
    public bool IsPlaying(SpriteEffect effect) => Find(effect) >= 0;

    /// <summary>Moves <paramref name="effect"/> to <paramref name="at"/>: an effect that follows something is moved to it each update.</summary>
    /// <returns>False when the effect has ended.</returns>
    public bool Move(SpriteEffect effect, Vector2 at)
    {
        int i = Find(effect);
        if (i < 0) return false;
        _playing[i].At = at;
        return true;
    }

    /// <summary>Removes <paramref name="effect"/> now.</summary>
    /// <returns>False when the effect had already ended.</returns>
    public bool Stop(SpriteEffect effect)
    {
        int i = Find(effect);
        if (i < 0) return false;
        Array.Copy(_playing, i + 1, _playing, i, _count - i - 1);
        _playing[--_count] = default;
        return true;
    }

    /// <summary>Removes every effect.</summary>
    public void Clear()
    {
        Array.Clear(_playing, 0, _count);
        _count = 0;
    }

    /// <summary>
    /// Advances the effects by <paramref name="dt"/> seconds: a waiting effect counts its delay down, a playing one
    /// its time up, and those that reached the end of their clip are removed. A step that is not a time (negative,
    /// infinite, not a number) is ignored: time does not run backwards and every effect ends.
    /// </summary>
    public void Update(float dt)
    {
        if (!(dt > 0) || float.IsInfinity(dt)) return;
        int kept = 0;
        for (int i = 0; i < _count; i++)
        {
            ref var p = ref _playing[i];
            if (p.Delay > 0) p.Delay -= dt;
            else p.Time += dt;
            bool ended = p.Rate > 0 ? p.Time * p.Rate >= p.Clip.Frames.Count : p.Time >= p.Clip.Duration;
            if (ended) continue;
            if (kept != i) _playing[kept] = p;
            kept++;
        }
        Array.Clear(_playing, kept, _count - kept);
        _count = kept;
    }

    /// <summary>
    /// Draws the effects that are showing, in the order they were played. Into a <see cref="SortedRenderer"/> block
    /// each is drawn at its own layer and depth, and the block's layer and depth are left as they were.
    /// </summary>
    public void Draw(IRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        var sorted = renderer as SortedRenderer;
        int layer = sorted?.Layer ?? 0;
        float depth = sorted?.Depth ?? 0;
        for (int i = 0; i < _count; i++)
        {
            ref var p = ref _playing[i];
            if (p.Delay > 0) continue;
            var frames = p.Clip.Frames;
            int index = p.Rate > 0 ? Math.Clamp((int)(p.Time * p.Rate), 0, frames.Count - 1) : p.Clip.FrameIndexOnce(p.Time);
            var frame = frames[index];
            var at = SnapToPixels ? new Vector2(MathF.Round(p.At.X), MathF.Round(p.At.Y)) : p.At;
            sorted?.At(p.Layer, p.Depth);
            renderer.Draw(frame.Texture, frame.Source, at - (p.FlipX ? new Vector2(frame.Source.Width - frame.Origin.X, frame.Origin.Y) : frame.Origin), p.Tint, new DrawOptions(FlipX: p.FlipX));
        }
        sorted?.At(layer, depth);
    }

    /// <summary>
    /// Adds the effects to a state hash: how many there are and, for each, its time, its delay and its length. A rule
    /// that waits for an effect (<see cref="Busy"/>, <see cref="IsPlaying"/>) makes them part of what happens next.
    /// </summary>
    public void HashState(StateHasher hash)
    {
        ArgumentNullException.ThrowIfNull(hash);
        hash.Add(_count);
        for (int i = 0; i < _count; i++)
        {
            ref var p = ref _playing[i];
            hash.Add(p.Time);
            hash.Add(p.Delay);
            hash.Add(p.Rate);
            hash.Add(p.Clip.Frames.Count);
        }
    }

    private int Find(SpriteEffect effect)
    {
        if (effect.Id == 0) return -1;
        for (int i = 0; i < _count; i++)
            if (_playing[i].Id == effect.Id) return i;
        return -1;
    }
}

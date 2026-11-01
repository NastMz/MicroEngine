using System.Numerics;

namespace MicroEngine.Graphics;

/// <summary>How a burst of particles looks and moves. Ranges are (min, max).</summary>
public sealed record ParticleSpec
{
    /// <summary>Colour at birth (white by default).</summary>
    public Color StartColor { get; init; } = Color.White;
    /// <summary>Colour at death, blended linearly from <see cref="StartColor"/> (transparent white by default).</summary>
    public Color EndColor { get; init; } = Color.White.WithAlpha((byte)0);
    /// <summary>Lifetime range in seconds (0.3 to 0.6 by default).</summary>
    public (float Min, float Max) Life { get; init; } = (0.3f, 0.6f);
    /// <summary>Initial speed range in pixels per second (20 to 60 by default).</summary>
    public (float Min, float Max) Speed { get; init; } = (20f, 60f);
    /// <summary>Emission angle range in radians (0 = right, -PI/2 = up).</summary>
    public (float Min, float Max) Angle { get; init; } = (0, MathF.Tau);
    /// <summary>Size in pixels at birth and at death (2 to 1 by default). Particles smaller than 0.2 pixels are not drawn.</summary>
    public (float Start, float End) Size { get; init; } = (2f, 1f);
    /// <summary>Downward acceleration in pixels per second squared (0 by default; negative values rise).</summary>
    public float Gravity { get; init; }
    /// <summary>Velocity multiplier per second (1 = no drag).</summary>
    public float Drag { get; init; } = 1f;
    /// <summary>Draws circles instead of pixel-aligned squares.</summary>
    public bool Round { get; init; }
    /// <summary>Random spread of the spawn position around the emit point.</summary>
    public float Spread { get; init; }
}

/// <summary>A pooled particle system for dust, sparks, confetti, smoke and the like.</summary>
public sealed class ParticleSystem
{
    private struct Particle
    {
        public Vector2 Position, Velocity;
        public float Age, Life;
        public ParticleSpec Spec;
    }

    private readonly Particle[] _particles;
    private int _count;
    private readonly Random _random;

    /// <summary>Creates an empty system.</summary>
    /// <param name="capacity">Maximum number of live particles; emissions beyond it are dropped.</param>
    /// <param name="seed">Seed of the system's own random numbers, so effects repeat exactly.</param>
    public ParticleSystem(int capacity = 512, int seed = 3)
    {
        _particles = new Particle[capacity];
        _random = new Random(seed);
    }

    /// <summary>Number of live particles.</summary>
    public int Count => _count;
    /// <summary>When true, particles are drawn with additive blending (glows, sparks).</summary>
    public bool Additive { get; set; }

    /// <summary>Spawns particles with random values within the ranges of <paramref name="spec"/>.</summary>
    /// <param name="at">Spawn point in the coordinates the system is drawn in.</param>
    /// <param name="spec">How the particles look and move; kept by reference, so do not change it while its particles live.</param>
    /// <param name="count">Number of particles; those beyond the capacity are dropped.</param>
    /// <param name="inheritVelocity">Velocity added to each particle, in pixels per second (for example the emitter's).</param>
    public void Emit(Vector2 at, ParticleSpec spec, int count = 1, Vector2 inheritVelocity = default)
    {
        ArgumentNullException.ThrowIfNull(spec);
        for (int i = 0; i < count && _count < _particles.Length; i++)
        {
            float angle = Range(spec.Angle.Min, spec.Angle.Max);
            float speed = Range(spec.Speed.Min, spec.Speed.Max);
            var offset = spec.Spread > 0 ? new Vector2(Range(-spec.Spread, spec.Spread), Range(-spec.Spread, spec.Spread)) : Vector2.Zero;
            _particles[_count++] = new Particle
            {
                Position = at + offset,
                Velocity = MathX.FromAngle(angle, speed) + inheritVelocity,
                Life = Range(spec.Life.Min, spec.Life.Max),
                Spec = spec,
            };
        }
    }

    private float Range(float min, float max) => min + (float)_random.NextDouble() * (max - min);

    /// <summary>Ages and moves every particle by <paramref name="dt"/> seconds and removes those past their life.</summary>
    public void Update(float dt)
    {
        for (int i = 0; i < _count; i++)
        {
            ref var p = ref _particles[i];
            p.Age += dt;
            if (p.Age >= p.Life)
            {
                _particles[i] = _particles[--_count];
                i--;
                continue;
            }
            p.Velocity.Y += p.Spec.Gravity * dt;
            if (p.Spec.Drag != 1f) p.Velocity *= MathF.Pow(p.Spec.Drag, dt);
            p.Position += p.Velocity * dt;
        }
    }

    /// <summary>Draws every live particle. With <see cref="Additive"/>, the blend mode is set to additive and back to <see cref="BlendMode.Alpha"/> afterwards.</summary>
    public void Draw(IRenderer renderer)
    {
        if (Additive) renderer.SetBlendMode(BlendMode.Additive);
        for (int i = 0; i < _count; i++)
        {
            ref var p = ref _particles[i];
            float t = p.Age / p.Life;
            var color = Color.Lerp(p.Spec.StartColor, p.Spec.EndColor, t);
            float size = MathX.Lerp(p.Spec.Size.Start, p.Spec.Size.End, t);
            if (size <= 0.2f) continue;
            if (p.Spec.Round) renderer.FillCircle(p.Position, size / 2f, color);
            else renderer.FillRect(new RectF(MathF.Round(p.Position.X - size / 2), MathF.Round(p.Position.Y - size / 2), MathF.Max(1, MathF.Round(size)), MathF.Max(1, MathF.Round(size))), color);
        }
        if (Additive) renderer.SetBlendMode(BlendMode.Alpha);
    }

    /// <summary>Removes every particle.</summary>
    public void Clear() => _count = 0;
}

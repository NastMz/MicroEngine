using System.Numerics;

namespace MicroEngine.Graphics;

/// <summary>How a burst of particles looks and moves. Ranges are (min, max).</summary>
public sealed record ParticleSpec
{
    public Color StartColor { get; init; } = Color.White;
    public Color EndColor { get; init; } = Color.White.WithAlpha((byte)0);
    public (float Min, float Max) Life { get; init; } = (0.3f, 0.6f);
    public (float Min, float Max) Speed { get; init; } = (20f, 60f);
    /// <summary>Emission angle range in radians (0 = right, -PI/2 = up).</summary>
    public (float Min, float Max) Angle { get; init; } = (0, MathF.Tau);
    public (float Start, float End) Size { get; init; } = (2f, 1f);
    public float Gravity { get; init; }
    /// <summary>Velocity multiplier per second (1 = no drag).</summary>
    public float Drag { get; init; } = 1f;
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

    public ParticleSystem(int capacity = 512, int seed = 3)
    {
        _particles = new Particle[capacity];
        _random = new Random(seed);
    }

    public int Count => _count;
    /// <summary>When true, particles are drawn with additive blending (glows, sparks).</summary>
    public bool Additive { get; set; }

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

    public void Clear() => _count = 0;
}

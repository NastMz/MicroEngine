using System.Numerics;
using MeadowRun.Art;
using MeadowRun.Content;
using MicroEngine;
using MicroEngine.Graphics;

namespace MeadowRun.Gameplay;

/// <summary>A floating score/label that rises and fades.</summary>
internal sealed class Popup(string text, Vector2 position, Color color)
{
    public string Text { get; } = text;
    public Vector2 Position = position;
    public Color Color { get; } = color;
    public float Age;
}

internal sealed partial class Stage
{
    private static readonly ParticleSpec DustSpec = new()
    {
        StartColor = Color.Hex("fffaf0e0"), EndColor = Color.Hex("e3bf8a00"),
        Life = (0.25f, 0.45f), Speed = (10, 34), Angle = (-MathF.PI, 0), Size = (3, 1), Drag = 0.05f, Gravity = -20, Round = true,
    };
    private static readonly ParticleSpec SparkSpec = new()
    {
        StartColor = Color.Hex("fff3a3"), EndColor = Color.Hex("f5a52400"),
        Life = (0.2f, 0.45f), Speed = (30, 90), Size = (2, 0.5f), Drag = 0.02f,
    };
    private static readonly ParticleSpec AmbientSpec = new()
    {
        Life = (2.5f, 4f), Speed = (4, 12), Angle = (-MathF.PI * 0.8f, -MathF.PI * 0.2f), Size = (1.5f, 1f),
    };

    private readonly List<Popup> _popups = [];
    private readonly Dictionary<Color, ParticleSpec> _burstSpecs = [];
    private float _ambient;

    /// <summary>Soft dust, smoke and debris drawn with alpha blending.</summary>
    public ParticleSystem Particles { get; } = new(700, 11);
    /// <summary>Sparks, glints and embers drawn additively.</summary>
    public ParticleSystem Glints { get; } = new(500, 12) { Additive = true };
    public IReadOnlyList<Popup> Popups => _popups;

    private void UpdateEffects(float dt)
    {
        Particles.Update(dt);
        Glints.Update(dt);
        for (int i = _popups.Count - 1; i >= 0; i--)
        {
            var p = _popups[i];
            p.Age += dt;
            p.Position.Y -= MathF.Max(0, 30 - p.Age * 40) * dt;
            if (p.Age > 0.9f) _popups.RemoveAt(i);
        }
    }

    public void Popup(string text, Vector2 at, Color color) => _popups.Add(new Popup(text, at, color));

    public void Dust(Vector2 feet, float direction, int count = 2)
    {
        var spec = direction == 0 ? DustSpec : DustSpec with { Angle = direction > 0 ? (-MathF.PI * 0.45f, 0) : (-MathF.PI, -MathF.PI * 0.55f) };
        Particles.Emit(feet + new Vector2(0, -2), spec, count);
    }

    public void LandPuff(Vector2 feet, float strength)
    {
        int count = 3 + (int)(strength * 6);
        Particles.Emit(feet + new Vector2(-3, -1), DustSpec with { Angle = (-MathF.PI, -MathF.PI * 0.85f), Speed = (20, 40 + strength * 30) }, count);
        Particles.Emit(feet + new Vector2(3, -1), DustSpec with { Angle = (-MathF.PI * 0.15f, 0), Speed = (20, 40 + strength * 30) }, count);
    }

    public void Puff(Vector2 at, Color color)
        => Particles.Emit(at, DustSpec with { StartColor = color, EndColor = color.WithAlpha((byte)0), Angle = (0, MathF.Tau), Speed = (10, 40) }, 6);

    public void Sparkle(Vector2 at, Color color) => Glints.Emit(at, SparkSpec with { StartColor = color }, 5);

    public void Burst(Vector2 at, Color color, int count)
    {
        if (!_burstSpecs.TryGetValue(color, out var spec))
            _burstSpecs[color] = spec = SparkSpec with { StartColor = color, EndColor = color.WithAlpha((byte)0), Speed = (40, 140), Life = (0.3f, 0.7f), Gravity = 160, Size = (3, 1) };
        Glints.Emit(at, spec, count);
    }

    /// <summary>Ground shockwave from the boss slam: dust racing both ways along the floor.</summary>
    public void Shockwave(Vector2 feet)
    {
        Particles.Emit(feet, DustSpec with { Angle = (-MathF.PI, -MathF.PI * 0.95f), Speed = (80, 160), Life = (0.4f, 0.6f), Size = (5, 2) }, 14);
        Particles.Emit(feet, DustSpec with { Angle = (-MathF.PI * 0.05f, 0), Speed = (80, 160), Life = (0.4f, 0.6f), Size = (5, 2) }, 14);
    }

    /// <summary>Biome atmosphere: pollen in the meadow, motes in the caves, wind in the sky, embers in the castle.</summary>
    private void EmitAmbient(float dt)
    {
        _ambient += dt;
        float interval = Level.Biome switch { Biome.Castle => 0.09f, Biome.Caves => 0.14f, _ => 0.2f };
        while (_ambient > interval)
        {
            _ambient -= interval;
            var view = Camera.View;
            float rx = Hash01(Clock * 13.1f), ry = Hash01(Clock * 7.7f + 3);
            var at = new Vector2(view.X + rx * view.Width, view.Y + ry * view.Height);
            switch (Level.Biome)
            {
                case Biome.Meadow:
                    Glints.Emit(at, AmbientSpec with { StartColor = Pal.Butter.WithAlpha(0.7f), EndColor = Pal.Butter.WithAlpha(0f) });
                    break;
                case Biome.Caves:
                    Glints.Emit(at, AmbientSpec with { StartColor = (rx > 0.5f ? Pal.Aqua : Pal.Lilac).WithAlpha(0.8f), EndColor = Pal.Violet.WithAlpha(0f), Angle = (-MathF.PI * 0.6f, -MathF.PI * 0.4f) });
                    break;
                case Biome.Sky:
                    Particles.Emit(at, AmbientSpec with { StartColor = Pal.Cream.WithAlpha(0.45f), EndColor = Pal.Cream.WithAlpha(0f), Angle = (MathF.PI - 0.05f, MathF.PI + 0.05f), Speed = (60, 90), Life = (0.8f, 1.4f), Size = (1, 1) });
                    break;
                case Biome.Castle:
                    Glints.Emit(new Vector2(at.X, view.Bottom), AmbientSpec with { StartColor = Pal.Flame, EndColor = Pal.Rust.WithAlpha(0f), Speed = (15, 40), Angle = (-MathF.PI * 0.6f, -MathF.PI * 0.4f), Life = (2, 3.5f) });
                    break;
            }
        }
    }

    private static float Hash01(float v)
    {
        float s = MathF.Sin(v * 12.9898f) * 43758.5453f;
        return s - MathF.Floor(s);
    }
}

using CourtyardKey.Visuals;
using MicroEngine;
using MicroEngine.Graphics;

namespace CourtyardKey.World;

/// <summary>Shared particle recipes, so every hit, cut and spark in the game feels related.</summary>
public static class Fx
{
    public static readonly ParticleSpec Leaves = new()
    {
        StartColor = Pal.Leaf[3], EndColor = Pal.Leaf[1].WithAlpha((byte)0), Life = (0.35f, 0.7f), Speed = (30, 80),
        Angle = (-MathF.PI, 0), Size = (2, 1), Gravity = 140, Drag = 0.2f, Spread = 4,
    };

    public static readonly ParticleSpec Shards = new()
    {
        StartColor = Color.Hex("b8703e"), EndColor = Color.Hex("5a2e22"), Life = (0.3f, 0.6f), Speed = (40, 100),
        Angle = (-MathF.PI, 0), Size = (2, 2), Gravity = 260, Drag = 0.3f, Spread = 3,
    };

    public static readonly ParticleSpec Dust = new()
    {
        StartColor = Pal.Cream.WithAlpha((byte)200), EndColor = Pal.Cream.WithAlpha((byte)0), Life = (0.25f, 0.45f), Speed = (8, 22),
        Angle = (-MathF.PI, 0), Size = (2, 1), Drag = 0.1f, Spread = 3,
    };

    public static readonly ParticleSpec Rubble = new()
    {
        StartColor = Pal.Stone[3], EndColor = Pal.Stone[1].WithAlpha((byte)0), Life = (0.4f, 0.8f), Speed = (40, 110),
        Angle = (-MathF.PI, 0), Size = (3, 1), Gravity = 240, Drag = 0.3f, Spread = 6,
    };

    public static readonly ParticleSpec Splash = new()
    {
        StartColor = Pal.Water[4], EndColor = Pal.Water[3].WithAlpha((byte)0), Life = (0.25f, 0.45f), Speed = (30, 70),
        Angle = (-MathF.PI * 0.9f, -MathF.PI * 0.1f), Size = (2, 1), Gravity = 220, Spread = 3,
    };

    // Additive (glow) particles
    public static readonly ParticleSpec Ember = new()
    {
        StartColor = Pal.Ember[3], EndColor = Pal.Ember[1].WithAlpha((byte)0), Life = (0.5f, 1.0f), Speed = (6, 18),
        Angle = (-MathF.PI * 0.65f, -MathF.PI * 0.35f), Size = (2, 1), Gravity = -10, Spread = 2,
    };

    public static readonly ParticleSpec Sparkle = new()
    {
        StartColor = Pal.Gold[3], EndColor = Pal.Gold[2].WithAlpha((byte)0), Life = (0.3f, 0.6f), Speed = (20, 60),
        Size = (2, 1), Drag = 0.1f, Spread = 2,
    };

    public static readonly ParticleSpec SpiritMote = new()
    {
        StartColor = Pal.Spirit[3], EndColor = Pal.Spirit[1].WithAlpha((byte)0), Life = (0.5f, 1.1f), Speed = (10, 40),
        Size = (2, 1), Drag = 0.3f, Spread = 4,
    };

    public static readonly ParticleSpec HitSpark = new()
    {
        StartColor = Pal.White, EndColor = Pal.Gold[2].WithAlpha((byte)0), Life = (0.12f, 0.25f), Speed = (60, 140),
        Size = (2, 1), Drag = 0.05f,
    };

    public static readonly ParticleSpec Firefly = new()
    {
        StartColor = Color.Hex("d8ff8a"), EndColor = Color.Hex("6ad04a00"), Life = (1.5f, 3f), Speed = (3, 10),
        Size = (2, 2), Drag = 0.8f, Spread = 1,
    };

    // Ambient (normal blend)
    public static readonly ParticleSpec Pollen = new()
    {
        StartColor = Pal.Gold[3].WithAlpha((byte)200), EndColor = Pal.White.WithAlpha((byte)0), Life = (3f, 5f), Speed = (4, 10),
        Angle = (-0.4f, 0.4f), Size = (1, 1), Gravity = -2,
    };

    public static readonly ParticleSpec FallingLeaf = new()
    {
        StartColor = Pal.Leaf[3], EndColor = Pal.Grass[2].WithAlpha((byte)0), Life = (3f, 5f), Speed = (8, 18),
        Angle = (0.6f, 1.6f), Size = (2, 2), Gravity = 4,
    };

    public static readonly ParticleSpec Mote = new()
    {
        StartColor = Pal.Stone[5].WithAlpha((byte)150), EndColor = Pal.Stone[4].WithAlpha((byte)0), Life = (3f, 6f), Speed = (2, 6),
        Size = (1, 1), Gravity = -1,
    };

    public static readonly ParticleSpec Spray = new()
    {
        StartColor = Pal.Water[4].WithAlpha((byte)170), EndColor = Pal.White.WithAlpha((byte)0), Life = (1.5f, 3f), Speed = (3, 9),
        Angle = (-MathF.PI, 0), Size = (1, 1), Gravity = -3,
    };
}

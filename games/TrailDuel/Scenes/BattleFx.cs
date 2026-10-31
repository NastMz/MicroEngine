using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using TrailDuel.Rules;
using TrailDuel.Visuals;

namespace TrailDuel.Scenes;

/// <summary>Particle recipes for attacks, statuses and stat changes, one look per element.</summary>
internal static class BattleFx
{
    private static ParticleSpec Spec(string start, string end, (float, float) life, (float, float) speed, (float, float) size,
        float gravity = 0, float drag = 1, (float, float)? angle = null, float spread = 0, bool round = true) => new()
    {
        StartColor = Color.Hex(start), EndColor = Color.Hex(end), Life = life, Speed = speed, Size = size,
        Gravity = gravity, Drag = drag, Angle = angle ?? (0, MathF.Tau), Spread = spread, Round = round,
    };

    private static readonly ParticleSpec Flame = Spec("fde88a", "e8403020", (0.4f, 0.8f), (20, 60), (7, 2), gravity: -90, drag: 0.4f, spread: 7);
    private static readonly ParticleSpec FlameCore = Spec("fff6d0", "f0802a40", (0.3f, 0.5f), (5, 25), (6, 2), gravity: -60, spread: 5);
    private static readonly ParticleSpec Bubble = Spec("e2f6ff", "5ea2e000", (0.4f, 0.8f), (15, 45), (4, 2), gravity: -40, drag: 0.5f, spread: 8);
    private static readonly ParticleSpec Splash = Spec("93ccf2", "3f7cc600", (0.3f, 0.6f), (40, 90), (3, 1), gravity: 160, angle: (-MathF.PI * 0.9f, -MathF.PI * 0.1f));
    private static readonly ParticleSpec Leaf = Spec("a2dc6c", "37884200", (0.5f, 0.9f), (30, 80), (3, 2), gravity: 30, drag: 0.4f, spread: 10, round: false);
    private static readonly ParticleSpec Petal = Spec("f8a0b4", "e06a8c00", (0.5f, 0.9f), (30, 70), (3, 2), gravity: 40, drag: 0.5f, spread: 12, round: false);
    private static readonly ParticleSpec Spark = Spec("fff6a8", "f8dc4a00", (0.12f, 0.3f), (70, 150), (2, 1), drag: 0.2f, round: false);
    private static readonly ParticleSpec Rock = Spec("c6c0d2", "4c465800", (0.4f, 0.7f), (40, 100), (4, 3), gravity: 260, angle: (-MathF.PI * 0.95f, -MathF.PI * 0.05f), round: false, spread: 6);
    private static readonly ParticleSpec Dust = Spec("e6d4b0", "a8805000", (0.3f, 0.6f), (20, 50), (5, 2), drag: 0.3f, spread: 8);
    private static readonly ParticleSpec Wind = Spec("ffffff", "c8ecff00", (0.3f, 0.6f), (80, 140), (2, 1), drag: 0.6f, angle: (-0.3f, 0.3f), round: false, spread: 14);
    private static readonly ParticleSpec Impact = Spec("ffffff", "fbf5e600", (0.12f, 0.25f), (60, 120), (3, 1), drag: 0.2f, round: false);
    private static readonly ParticleSpec Heal = Spec("d8ffd0", "58c06000", (0.5f, 0.9f), (10, 30), (3, 1), gravity: -50, angle: (-MathF.PI, 0), spread: 14);
    private static readonly ParticleSpec Up = Spec("b8e4fa", "4a88d000", (0.4f, 0.7f), (40, 70), (2, 2), angle: (-MathF.PI / 2 - 0.1f, -MathF.PI / 2 + 0.1f), spread: 14, round: false);
    private static readonly ParticleSpec Down = Spec("f8a0a0", "c0403000", (0.4f, 0.7f), (40, 70), (2, 2), angle: (MathF.PI / 2 - 0.1f, MathF.PI / 2 + 0.1f), spread: 14, round: false);
    private static readonly ParticleSpec Poison = Spec("d8a0f0", "8a40b000", (0.4f, 0.8f), (10, 35), (4, 1), gravity: -40, spread: 10);
    private static readonly ParticleSpec Sleep = Spec("ffffff", "c0c0e000", (0.6f, 1f), (10, 20), (3, 2), gravity: -20, angle: (-MathF.PI * 0.7f, -MathF.PI * 0.3f), spread: 6);
    private static readonly ParticleSpec Star = Spec("fff0a8", "f2c25c00", (0.4f, 0.7f), (30, 70), (3, 1), gravity: 40, angle: (-MathF.PI, 0), round: false);

    /// <summary>The flourish at the target when a move connects.</summary>
    public static void Attack(ParticleSystem ps, ElementType type, Vector2 at, bool big)
    {
        int n = big ? 2 : 1;
        switch (type)
        {
            case ElementType.Flame: ps.Emit(at, Flame, 26 * n); ps.Emit(at, FlameCore, 10 * n); break;
            case ElementType.Tide: ps.Emit(at, Bubble, 18 * n); ps.Emit(at, Splash, 14 * n); break;
            case ElementType.Leaf: ps.Emit(at, Leaf, 16 * n); ps.Emit(at, Petal, 8 * n); break;
            case ElementType.Volt: ps.Emit(at, Spark, 30 * n); ps.Emit(at, Impact, 6); break;
            case ElementType.Stone: ps.Emit(at + new Vector2(0, -10), Rock, 14 * n); ps.Emit(at, Dust, 10); break;
            case ElementType.Gale: ps.Emit(at - new Vector2(20, 0), Wind, 24 * n); ps.Emit(at, Dust, 6); break;
            default: ps.Emit(at, Impact, 12 * n); ps.Emit(at, Dust, 6); break;
        }
    }

    /// <summary>The projectile trail drawn between attacker and target for special moves.</summary>
    public static void Trail(ParticleSystem ps, ElementType type, Vector2 at)
    {
        var spec = type switch
        {
            ElementType.Flame => Flame,
            ElementType.Tide => Bubble,
            ElementType.Leaf => Leaf,
            ElementType.Volt => Spark,
            ElementType.Stone => Dust,
            ElementType.Gale => Wind,
            _ => Impact,
        };
        ps.Emit(at, spec, 5);
    }

    public static void Hit(ParticleSystem ps, Vector2 at) => ps.Emit(at, Impact, 10);
    public static void Healing(ParticleSystem ps, Vector2 at) => ps.Emit(at, Heal, 20);
    public static void StatChange(ParticleSystem ps, Vector2 at, bool up) => ps.Emit(at + new Vector2(0, up ? 10 : -16), up ? Up : Down, 16);
    public static void Stars(ParticleSystem ps, Vector2 at) => ps.Emit(at, Star, 14);

    public static void Status(ParticleSystem ps, StatusCondition status, Vector2 at)
    {
        switch (status)
        {
            case StatusCondition.Burn: ps.Emit(at, Flame, 18); break;
            case StatusCondition.Poison: ps.Emit(at, Poison, 16); break;
            case StatusCondition.Sleep: ps.Emit(at + new Vector2(10, -16), Sleep, 4); break;
        }
    }

    public static Color Tint(ElementType type) => Pal.TypeColor(type);
}

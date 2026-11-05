using System.Numerics;

namespace NeonRift.Gameplay;

public enum WeaponKind { Pulse, Spread, Lance, Rapid }

/// <summary>The effect a shot leaves where it hits: a strip of the atlas played once at <paramref name="Fps"/>, scaled.</summary>
public readonly record struct ImpactSpec(string Sprite, float Fps = 20, float Scale = 1)
{
    /// <summary>The hero pack's own small spark (the Pulse blaster's impact).</summary>
    public static readonly ImpactSpec Spark = new("shot_hit");
}

/// <summary>How a weapon fires: cadence, bullet count and fan, speed, damage and whether it pierces.</summary>
public sealed record WeaponSpec(
    WeaponKind Kind,
    char Letter,
    string Label,
    float Cooldown,
    int Pellets,
    float FanDegrees,
    float Speed,
    int Damage,
    bool Piercing,
    string Sprite,
    string Sound,
    int MaxOnScreen)
{
    /// <summary>What the weapon's shots leave where they hit a wall or an enemy.</summary>
    public ImpactSpec Impact { get; init; } = ImpactSpec.Spark;

    public static readonly IReadOnlyDictionary<WeaponKind, WeaponSpec> All = new Dictionary<WeaponKind, WeaponSpec>
    {
        // Default blaster: honest single shots.
        [WeaponKind.Pulse] = new(WeaponKind.Pulse, 'P', "PULSE", 0.13f, 1, 0, 360, 1, false, "shot", "shot", 5),
        // Spread: a five-way fan that covers diagonals; the signature crowd-clearer. Its star-shaped pellets burst into stars.
        [WeaponKind.Spread] = new(WeaponKind.Spread, 'S', "SCATTER", 0.26f, 5, 13, 300, 1, false, "crossed", "spread", 15)
        {
            Impact = new("hit2", 28, 0.5f),
        },
        // Lance: slow, piercing beams that hit everything in a line for heavy damage, with a wide ring burst.
        [WeaponKind.Lance] = new(WeaponKind.Lance, 'L', "LANCE", 0.42f, 1, 0, 420, 3, true, "waveform", "laser", 3)
        {
            Impact = new("hit4", 24, 0.75f),
        },
        // Rapid: a stream of light bolts with a little jitter, each popping a small ring.
        [WeaponKind.Rapid] = new(WeaponKind.Rapid, 'R', "RAPID", 0.075f, 1, 0, 380, 1, false, "bolt", "rapid", 9)
        {
            Impact = new("hit1", 28, 0.5f),
        },
    };

    public static WeaponSpec Of(WeaponKind kind) => All[kind];

    public static WeaponKind? FromLetter(char letter) => All.Values.FirstOrDefault(w => w.Letter == letter)?.Kind;

    /// <summary>Directions of every pellet for one trigger pull towards <paramref name="aim"/> (unit vector).</summary>
    public IEnumerable<Vector2> Directions(Vector2 aim, Random random)
    {
        float baseAngle = MathF.Atan2(aim.Y, aim.X);
        if (Kind == WeaponKind.Rapid) baseAngle += ((float)random.NextDouble() - 0.5f) * 0.07f;
        for (int i = 0; i < Pellets; i++)
        {
            float offset = Pellets == 1 ? 0 : (i - (Pellets - 1) / 2f) * FanDegrees * MathF.PI / 180f;
            yield return new Vector2(MathF.Cos(baseAngle + offset), MathF.Sin(baseAngle + offset));
        }
    }
}

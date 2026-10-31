using System.Numerics;

namespace NeonRift.Gameplay;

public enum WeaponKind { Pulse, Spread, Lance, Rapid }

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
    public static readonly IReadOnlyDictionary<WeaponKind, WeaponSpec> All = new Dictionary<WeaponKind, WeaponSpec>
    {
        // Default blaster: honest single shots.
        [WeaponKind.Pulse] = new(WeaponKind.Pulse, 'P', "PULSE", 0.13f, 1, 0, 360, 1, false, "shot", "shot", 5),
        // Spread: a five-way fan that covers diagonals; the signature crowd-clearer.
        [WeaponKind.Spread] = new(WeaponKind.Spread, 'S', "SCATTER", 0.26f, 5, 13, 300, 1, false, "crossed", "spread", 15),
        // Lance: slow, piercing beams that hit everything in a line for heavy damage.
        [WeaponKind.Lance] = new(WeaponKind.Lance, 'L', "LANCE", 0.42f, 1, 0, 420, 3, true, "waveform", "laser", 3),
        // Rapid: a stream of light bolts with a little jitter.
        [WeaponKind.Rapid] = new(WeaponKind.Rapid, 'R', "RAPID", 0.075f, 1, 0, 380, 1, false, "bolt", "rapid", 9),
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

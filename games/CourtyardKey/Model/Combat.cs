using System.Numerics;

namespace CourtyardKey.Model;

/// <summary>The rule set for hits: damage tables, invulnerability, knockback and shield blocking.</summary>
public static class Combat
{
    /// <summary>Hero invulnerability after taking a hit, in seconds.</summary>
    public const float HeroInvulnerability = 1.1f;
    /// <summary>Enemy invulnerability after a sword hit; short so combos feel responsive.</summary>
    public const float EnemyInvulnerability = 0.3f;
    public const float HeroKnockbackSpeed = 190f;
    public const float EnemyKnockbackSpeed = 210f;
    public const float KnockbackTime = 0.16f;
    public const int SwordDamage = 1;
    public const float BoomerangStun = 1.8f;

    /// <summary>Unit vector pushing the victim away from the attacker; straight down when they overlap exactly.</summary>
    public static Vector2 KnockbackDirection(Vector2 attacker, Vector2 victim)
    {
        var d = victim - attacker;
        if (d.LengthSquared() < 0.0001f) return new Vector2(0, 1);
        // Snap to the dominant axis: classic top-down knockback is cardinal and readable.
        return DirExtensions.FromVector(d).ToVector();
    }

    /// <summary>
    /// True when a shield held toward <paramref name="shieldFacing"/> blocks an attack arriving from
    /// <paramref name="attackerPosition"/>: the attacker must be in front, within a 90 degree cone.
    /// Stunned shield bearers never block.
    /// </summary>
    public static bool ShieldBlocks(Dir shieldFacing, Vector2 bearerPosition, Vector2 attackerPosition, bool stunned)
    {
        if (stunned) return false;
        var toAttacker = attackerPosition - bearerPosition;
        if (toAttacker.LengthSquared() < 0.0001f) return true;
        var facing = shieldFacing.ToVector();
        float cos = Vector2.Dot(Vector2.Normalize(toAttacker), facing);
        return cos > 0.7071f - 0.0001f;
    }
}

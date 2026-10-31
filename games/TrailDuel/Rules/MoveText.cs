namespace TrailDuel.Rules;

/// <summary>
/// Player-facing descriptions of moves, written from the same data and rules the battle uses, so what a
/// move card says is exactly what the move does.
/// </summary>
public static class MoveText
{
    public static string Category(MoveData move) => move.Category.ToString();

    /// <summary>Power, or "-" for moves that deal no direct damage.</summary>
    public static string Power(MoveData move) => move.IsDamaging ? move.Power.ToString() : "-";

    /// <summary>Hit chance, or "-" for moves that never miss.</summary>
    public static string Accuracy(MoveData move) => move.NeverMisses ? "-" : move.Accuracy + "%";

    /// <summary>How the move's type fares against a foe of <paramref name="foe"/> type; null when neutral or not an attack.</summary>
    public static string? Matchup(MoveData move, ElementType foe)
    {
        if (!move.IsDamaging) return null;
        float e = TypeChart.Effectiveness(move.Type, foe);
        return e == 0 ? "No effect" : e > 1 ? "Super effective" : e < 1 ? "Not very effective" : null;
    }

    /// <summary>The flavour sentence followed by every rule the move adds; plain attacks say so.</summary>
    public static string Describe(MoveData move)
    {
        string effects = Effects(move);
        if (!move.IsDamaging) return effects;
        string flavour = Flavour(move.Description);
        return effects.Length == 0 ? $"{flavour} No added effect." : $"{flavour} {effects}";
    }

    /// <summary>What the move does beyond its damage, one sentence per rule.</summary>
    public static string Effects(MoveData move)
    {
        var parts = new List<string>();
        if (move.Priority > 0) parts.Add("Strikes before ordinary moves.");
        if (move.HighCrit) parts.Add($"High critical-hit rate (1 in {Formulas.CritChanceDenominator(move)} instead of 1 in 16).");
        bool damaging = move.IsDamaging;
        // Damaging moves only trigger their effect with a chance above zero; status moves always do.
        string? chance = damaging ? (move.EffectChance > 0 ? $"{move.EffectChance}% chance to " : null) : "";
        switch (move.Effect)
        {
            case MoveEffect.Burn when chance is not null:
                parts.Add(damaging
                    ? $"{chance}burn the foe (it loses 1/8 of its max HP each turn and its physical attacks deal half damage). Flame types can't be burned."
                    : "Burns the foe: it loses 1/8 of its max HP each turn and its physical attacks deal half damage. Flame types can't be burned. Fails if the foe already has a status.");
                break;
            case MoveEffect.Poison when chance is not null:
                parts.Add(damaging
                    ? $"{chance}poison the foe (it loses 1/8 of its max HP each turn)."
                    : "Poisons the foe: it loses 1/8 of its max HP each turn. Fails if the foe already has a status.");
                break;
            case MoveEffect.Sleep when chance is not null:
                parts.Add(damaging
                    ? $"{chance}put the foe to sleep for 1 to 3 turns."
                    : "Puts the foe to sleep for 1 to 3 turns. Fails if the foe already has a status.");
                break;
            case MoveEffect.RaiseSelf when chance is not null:
                parts.Add(damaging ? $"{chance}raise your {move.Stat} by {Stages(move)}." : $"Raises your {move.Stat} by {Stages(move)}.");
                break;
            case MoveEffect.LowerFoe when chance is not null:
                parts.Add(damaging ? $"{chance}lower the foe's {move.Stat} by {Stages(move)}." : $"Lowers the foe's {move.Stat} by {Stages(move)}.");
                break;
            case MoveEffect.HealSelf:
                parts.Add("Restores half of your max HP. Fails at full HP.");
                break;
            case MoveEffect.Drain:
                parts.Add("Heals you by half the damage dealt.");
                break;
        }
        return string.Join(" ", parts);
    }

    private static string Stages(MoveData move) => move.Stages == 1 ? "1 stage" : $"{move.Stages} stages";

    private static string Flavour(string description)
    {
        int end = description.IndexOf(". ", StringComparison.Ordinal);
        return end < 0 ? description : description[..(end + 1)];
    }
}

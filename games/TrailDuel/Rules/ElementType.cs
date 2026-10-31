namespace TrailDuel.Rules;

/// <summary>The seven elemental types of the Trail Duel world.</summary>
public enum ElementType { Normal, Flame, Tide, Leaf, Volt, Stone, Gale }

/// <summary>Type effectiveness chart: attacker type against defender type.</summary>
public static class TypeChart
{
    // Rows: attacking type, columns: defending type, in enum order.
    // N = Normal, F = Flame, T = Tide, L = Leaf, V = Volt, S = Stone, G = Gale
    private static readonly float[,] Chart =
    {
        //        N     F     T     L     V     S     G
        /*N*/ { 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 0.5f, 1.0f },
        /*F*/ { 1.0f, 0.5f, 0.5f, 2.0f, 1.0f, 0.5f, 1.0f },
        /*T*/ { 1.0f, 2.0f, 0.5f, 0.5f, 1.0f, 2.0f, 1.0f },
        /*L*/ { 1.0f, 0.5f, 2.0f, 0.5f, 1.0f, 2.0f, 0.5f },
        /*V*/ { 1.0f, 1.0f, 2.0f, 0.5f, 0.5f, 0.0f, 2.0f },
        /*S*/ { 1.0f, 2.0f, 1.0f, 1.0f, 2.0f, 1.0f, 2.0f },
        /*G*/ { 1.0f, 1.0f, 1.0f, 2.0f, 0.5f, 0.5f, 1.0f },
    };

    public static float Effectiveness(ElementType attack, ElementType defend) => Chart[(int)attack, (int)defend];

    public static string Name(ElementType type) => type.ToString().ToUpperInvariant();

    /// <summary>The battle message for an effectiveness multiplier, or null for neutral hits.</summary>
    public static string? Message(float multiplier) => multiplier switch
    {
        0f => "It had no effect...",
        > 1f => "It's super effective!",
        < 1f => "It's not very effective...",
        _ => null,
    };
}

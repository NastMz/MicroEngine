namespace TrailDuel.Ui;

/// <summary>
/// The numbers screens show every frame (levels, HP and PP pairs, stats), each built once and kept, so drawing
/// produces no garbage. The text is exactly what the plain formatting gives.
/// </summary>
internal static class NumberTexts
{
    private const int MaxKept = 4096;
    private static readonly string?[] Numbers = new string?[1000];
    private static readonly string?[] Levels = new string?[128];
    private static readonly string?[] Counts = new string?[1000];
    private static readonly Dictionary<long, string> Ratios = [];
    private static readonly Dictionary<int, string> Amounts = [];

    /// <summary><paramref name="n"/> as text.</summary>
    public static string Of(int n) => (uint)n < (uint)Numbers.Length ? Numbers[n] ??= n.ToString() : n.ToString();

    /// <summary>"Lv" followed by <paramref name="level"/>.</summary>
    public static string Level(int level) => (uint)level < (uint)Levels.Length ? Levels[level] ??= "Lv" + level : "Lv" + level;

    /// <summary>"×" followed by <paramref name="count"/>, as items in the bag are counted.</summary>
    public static string Count(int count) => (uint)count < (uint)Counts.Length ? Counts[count] ??= "×" + count : "×" + count;

    /// <summary>"<paramref name="a"/>/<paramref name="b"/>", as HP and PP are shown.</summary>
    public static string Ratio(int a, int b)
    {
        long key = ((long)a << 32) | (uint)b;
        if (!Ratios.TryGetValue(key, out var text))
        {
            if (Ratios.Count >= MaxKept) Ratios.Clear();
            Ratios[key] = text = $"{a}/{b}";
        }
        return text;
    }

    /// <summary>"$" followed by <paramref name="amount"/>, as money and prices are shown.</summary>
    public static string Money(int amount)
    {
        if (!Amounts.TryGetValue(amount, out var text))
        {
            if (Amounts.Count >= MaxKept) Amounts.Clear();
            Amounts[amount] = text = $"${amount}";
        }
        return text;
    }
}

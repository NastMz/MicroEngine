namespace MeadowRun.Content;

/// <summary>
/// An authored level: ASCII rows plus metadata. Legend (tiles): <c>#</c> ground, <c>=</c> hard block,
/// <c>B</c> brick, <c>M</c> multi-coin brick, <c>?</c> coin block, <c>P</c> power block, <c>U</c> 1-up block,
/// <c>H</c> hidden 1-up block, <c>-</c> one-way platform, <c>^</c> spikes, <c>~</c> lava, <c>%</c> castle back wall.
/// Entities: <c>S</c> start, <c>G</c> goal pole, <c>K</c> checkpoint, <c>o</c> coin, <c>j</c> spring,
/// <c>w</c> bumblet, <c>t</c> thornback, <c>h</c> hopkin, <c>f</c> mothwing, <c>p</c> spitpod,
/// <c>_</c> horizontal platform, <c>I</c> vertical platform, <c>v</c> falling platform, <c>F</c> fire bar,
/// <c>X</c> boss, <c>*</c> torch / light.
/// </summary>
public sealed record LevelDef(int Number, string Name, string Subtitle, Biome Biome, int Time, string Music, string[] Rows)
{
    public bool IsBoss => Rows.Any(r => r.Contains('X'));
}

/// <summary>Maps level characters to tile ids.</summary>
public static class Legend
{
    public static readonly IReadOnlyDictionary<char, int> Tiles = new Dictionary<char, int>
    {
        ['#'] = Tile.Ground,
        ['='] = Tile.HardBlock,
        ['F'] = Tile.HardBlock,
        ['B'] = Tile.Brick,
        ['M'] = Tile.MultiCoinBrick,
        ['?'] = Tile.CoinBlock,
        ['P'] = Tile.PowerBlock,
        ['U'] = Tile.LifeBlock,
        ['H'] = Tile.HiddenLife,
        ['-'] = Tile.OneWay,
        ['^'] = Tile.Spikes,
        ['~'] = Tile.Lava,
        ['%'] = Tile.BackWall,
    };

    /// <summary>Characters that spawn entities instead of tiles.</summary>
    public const string Entities = "SGKojwthfp_IvFX*";
}

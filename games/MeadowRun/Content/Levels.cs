namespace MeadowRun.Content;

/// <summary>
/// The campaign. Levels are authored as 15-row ASCII chunks joined left to right, so each section
/// (a lesson, a challenge, a breather) can be designed and read on its own. See <see cref="LevelDef"/> for the legend.
/// </summary>
public static partial class Levels
{
    public const int Rows = 14;

    private static IReadOnlyList<LevelDef>? _all;

    /// <summary>All levels in campaign order. Built lazily: chunk tables live in other partial files.</summary>
    public static IReadOnlyList<LevelDef> All => _all ??= Build();

    private static LevelDef[] Build() =>
    [
        new(1, "Sunny Meadow", "Where the Sunstone shone", Biome.Meadow, 300, "meadow", Join(MeadowChunks)),
        new(2, "Crystal Caves", "Down where the light hides", Biome.Caves, 320, "caves", Join(CaveChunks)),
        new(3, "Cloud Heights", "Above the wind", Biome.Sky, 320, "sky", Join(SkyChunks)),
        new(4, "Ember Castle", "The Mole King's halls", Biome.Castle, 340, "castle", Castleize(Join(CastleChunks))),
        new(5, "Throne of Cinders", "Face the Mole King", Biome.Castle, 400, "castle", Castleize(Join(ThroneChunks))),
    ];

    /// <summary>Concatenates chunks horizontally; every chunk must have <see cref="Rows"/> rows of equal width.</summary>
    public static string[] Join(IEnumerable<string[]> chunks)
    {
        var rows = new System.Text.StringBuilder[Rows];
        for (int i = 0; i < Rows; i++) rows[i] = new();
        int index = 0;
        foreach (var chunk in chunks)
        {
            if (chunk.Length != Rows) throw new FormatException($"Chunk {index} has {chunk.Length} rows; expected {Rows}.");
            int width = chunk[0].Length;
            for (int r = 0; r < Rows; r++)
            {
                if (chunk[r].Length != width) throw new FormatException($"Chunk {index}, row {r} is {chunk[r].Length} wide; expected {width}.");
                rows[r].Append(chunk[r]);
            }
            index++;
        }
        return rows.Select(r => r.ToString()).ToArray();
    }

    private static readonly string[][] MeadowChunks =
    [
        // Start: a coin block to learn bumping, a power-up row, the first walker on flat ground.
        [
            "..........................",
            "..........................",
            "..........................",
            "..........................",
            "..........................",
            "..........................",
            "...............ooo........",
            "..........................",
            "..........?...B?BPB.......",
            "..........................",
            "..........................",
            "...S...............w......",
            "##########################",
            "##########################",
        ],
        // Rolling hills: rising steps to jump over, walkers in the valleys.
        [
            "..............................",
            "..............................",
            "..............................",
            "..............................",
            "..............................",
            "..............................",
            "..............................",
            "..............................",
            "..................ooo.........",
            "...........###...o...o........",
            "......##...###................",
            "......##...###......w....w....",
            "##############################",
            "##############################",
        ],
        // First pits, each traced by a coin arc that shows the jump.
        [
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            ".......ooo......ooo.....",
            "......o...o....o...o....",
            "........................",
            "........................",
            "........................",
            "######...#######....####",
            "######...#######....####",
        ],
        // Jump-through planks climbing to a coin reward; a hopper waits at the bottom.
        [
            "..............................",
            "..............................",
            "......................ooo.....",
            ".....................-----....",
            "..............................",
            "..............ooo.............",
            ".............-----............",
            "..............................",
            "......ooo.....................",
            ".....-----....................",
            "..............................",
            "...........................h..",
            "##############################",
            "##############################",
        ],
        // Checkpoint, then a spring that only reaches the sky bridge when jump is held.
        [
            "..........................",
            "..........................",
            "..........................",
            "......oooooooo............",
            "......========............",
            "..............o...........",
            "...............o..........",
            "................o.........",
            "..........................",
            "..........................",
            "..........................",
            "..K..j...........t........",
            "##########################",
            "##########################",
        ],
        // Brick gallery: bricks break once Pip has bloomed; a coin-rich brick and a hidden life.
        [
            "..............................",
            "..............................",
            "..............................",
            "..............................",
            "..............................",
            "..............................",
            "...........BBBMBBBB...........",
            "..............................",
            "....H.....B?B.....B?B.........",
            "..............................",
            "..............................",
            ".............w..w.....w.......",
            "##############################",
            "##############################",
        ],
        // The final staircase and the goal pole: jump from the top step to grab it high.
        [
            "............................",
            "............................",
            "............................",
            "............................",
            ".........=..................",
            "........==..................",
            ".......===..................",
            "......====..................",
            ".....=====..................",
            "....======..................",
            "...=======..................",
            "..========.....G............",
            "############################",
            "############################",
        ],
    ];
}

using TrailDuel.Rules;
using TrailDuel.Visuals;

namespace TrailDuel.World;

/// <summary>What a map cell is. Collision and drawing both derive from the kind.</summary>
public enum TileKind : byte
{
    Void, Grass, Flowers, TallGrass, Path, Tree, Pine, Water, Fence, Ledge, Rock, Bush,
    Floor, Wall, Mat, Bookshelf, Bed, Table, Plant, Counter, Tv, Kitchen, Altar, Pedestal, Rug, Shelf, Machine, Statue, Carpet,
}

public static class Tiles
{
    /// <summary>Map legend shared by every map. Outdoor and indoor characters never overlap.</summary>
    public static readonly IReadOnlyDictionary<char, TileKind> Legend = new Dictionary<char, TileKind>
    {
        ['.'] = TileKind.Grass, [','] = TileKind.Flowers, [';'] = TileKind.TallGrass, ['='] = TileKind.Path,
        ['T'] = TileKind.Tree, ['P'] = TileKind.Pine, ['W'] = TileKind.Water, ['F'] = TileKind.Fence, ['v'] = TileKind.Ledge,
        ['R'] = TileKind.Rock, ['b'] = TileKind.Bush,
        ['_'] = TileKind.Floor, ['#'] = TileKind.Wall, ['m'] = TileKind.Mat, ['B'] = TileKind.Bookshelf, ['d'] = TileKind.Bed,
        ['t'] = TileKind.Table, ['p'] = TileKind.Plant, ['c'] = TileKind.Counter, ['x'] = TileKind.Tv, ['k'] = TileKind.Kitchen,
        ['a'] = TileKind.Altar, ['o'] = TileKind.Pedestal, ['r'] = TileKind.Rug, ['S'] = TileKind.Shelf, ['M'] = TileKind.Machine,
        ['u'] = TileKind.Statue, ['C'] = TileKind.Carpet,
    };

    public static bool IsSolid(TileKind k) => k switch
    {
        TileKind.Void or TileKind.Tree or TileKind.Pine or TileKind.Water or TileKind.Fence or TileKind.Rock or TileKind.Bush
            or TileKind.Wall or TileKind.Bookshelf or TileKind.Bed or TileKind.Table or TileKind.Plant or TileKind.Counter
            or TileKind.Tv or TileKind.Kitchen or TileKind.Altar or TileKind.Pedestal or TileKind.Shelf or TileKind.Machine
            or TileKind.Statue => true,
        _ => false,
    };
}

public enum NpcRole { Talk, Mom, Mentor, Keeper, Clerk, Rival, Gift }

public sealed record TrainerDef(string Name, (string Species, int Level)[] Team, int Prize, string[] Intro, string DefeatLine, string[] After,
    int Sight = 4, bool Smart = true, bool IsMaster = false, bool IsRival = false)
{
    public TrainerInfo ToInfo() => new(Name, Team.Select(t => Creature.Create(t.Species, t.Level)).ToList(), Prize, DefeatLine, Smart, IsMaster);
}

/// <summary>
/// An NPC. <see cref="ShowIf"/>/<see cref="HideIf"/> are story flags controlling presence; trainers use
/// <c>trainer:{Id}</c> as their defeat flag.
/// </summary>
public sealed record NpcDef(string Id, int X, int Y, Direction Facing, CharacterLook Look, string[] Lines,
    NpcRole Role = NpcRole.Talk, bool Wander = false, TrainerDef? Trainer = null, string? ShowIf = null, string? HideIf = null, string? Speaker = null)
{
    public string DefeatFlag => "trainer:" + Id;
}

public sealed record BuildingDef(int X, int Y, int W, int H, BuildingStyle Style, int DoorX, bool Locked = false);
public sealed record WarpDef(int X, int Y, string To, int ToX, int ToY, Direction Facing);
public sealed record SignDef(int X, int Y, params string[] Pages);
public sealed record TriggerDef(int X, int Y, string Id);
public sealed record ItemBallDef(int X, int Y, string ItemId, int Count)
{
    public string Flag => $"item:{X},{Y}:{ItemId}";
}

public sealed record EncounterSlot(string Species, int MinLevel, int MaxLevel, int Weight);

public sealed record EncounterTable(int RatePercent, params EncounterSlot[] Slots)
{
    public Creature Roll(Random rng)
    {
        int total = Slots.Sum(s => s.Weight);
        int pick = rng.Next(total);
        foreach (var s in Slots)
        {
            if (pick < s.Weight) return Creature.Create(s.Species, rng.Next(s.MinLevel, s.MaxLevel + 1));
            pick -= s.Weight;
        }
        throw new InvalidOperationException("Encounter table is empty.");
    }
}

public enum Backdrop { Meadow, Forest, Hall, Indoor }

/// <summary>A map: text rows plus everything placed on them.</summary>
public sealed class MapDef
{
    /// <summary>Side of a map tile in pixels: one grid step, and the size of every overworld tile sprite.</summary>
    public const int TileSize = 16;

    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string[] Rows { get; init; }
    public bool Indoor { get; init; }
    public string Music { get; init; } = "town";
    public Backdrop Backdrop { get; init; } = Backdrop.Meadow;
    public List<BuildingDef> Buildings { get; init; } = [];
    public List<WarpDef> Warps { get; init; } = [];
    public List<NpcDef> Npcs { get; init; } = [];
    public List<SignDef> Signs { get; init; } = [];
    public List<TriggerDef> Triggers { get; init; } = [];
    public List<ItemBallDef> Items { get; init; } = [];
    public EncounterTable? Encounters { get; init; }

    /// <summary>The widest row, measured once (the map is drawn and walked every frame).</summary>
    public int Width => _width ??= Rows.Max(r => r.Length);
    private int? _width;
    public int Height => Rows.Length;

    public TileKind KindAt(int x, int y)
    {
        if (y < 0 || y >= Rows.Length || x < 0 || x >= Rows[y].Length) return TileKind.Void;
        return Tiles.Legend.TryGetValue(Rows[y][x], out var k) ? k : TileKind.Void;
    }
}

using CourtyardKey.Model;

namespace CourtyardKey.Content;

public enum Item { None, Sword, Boomerang, SmallKey, BossKey, HeartContainer, Gems, CourtyardKey }

/// <summary>How a room reacts to the hero: shutters and hidden chests are driven by its rule.</summary>
public enum RoomRule
{
    None,
    /// <summary>Shutters close behind the hero until every enemy is defeated.</summary>
    Ambush,
    /// <summary>A push block resting on the floor plate solves the room.</summary>
    Plate,
    /// <summary>Striking the crystal switch solves the room.</summary>
    Crystal,
    /// <summary>The boss fight: shutters close until the boss falls.</summary>
    Boss,
}

/// <summary>Ambient particles that give each place its mood.</summary>
public enum Ambience { None, Pollen, Leaves, Fireflies, Dust, Spray }

public enum NpcLook { Elder, Merchant, Girl, Kid, Fisher, Hermit }

/// <summary>What happens after an NPC finishes talking.</summary>
public enum TalkEffect { None, GiveSword, OpenShop }

public sealed record Conversation(string[] Pages, TalkEffect Effect = TalkEffect.None);

/// <summary>An NPC placed at tile coordinates (feet position). Talk picks lines from the current progress.</summary>
public sealed record NpcDef(string Id, NpcLook Look, string Name, float TileX, float TileY, Func<Progress, Conversation> Talk, Dir Facing = Dir.Down);

public sealed record ChestDef(Item Item, int Amount = 1);

public sealed class RoomDef
{
    public required string Name { get; init; }
    public required string[] Map { get; init; }
    public Tile Base { get; init; } = Tile.Grass;
    public RoomRule Rule { get; init; }
    public Ambience Ambience { get; init; }
    /// <summary>0 = daylight, 1 = pitch black. Torches and the hero's glow cut through it.</summary>
    public float Darkness { get; init; }
    public IReadOnlyList<NpcDef> Npcs { get; init; } = [];
    /// <summary>Contents for every 'C'/'c' marker, in reading order.</summary>
    public IReadOnlyList<ChestDef> Chests { get; init; } = [];
    /// <summary>Text for every 'S' marker, in reading order.</summary>
    public IReadOnlyList<string> Signs { get; init; } = [];
    /// <summary>The hero cannot leave through any edge until the sword is found (the opening village).</summary>
    public bool LeaveNeedsSword { get; init; }
}

/// <summary>Moves the hero to another area when they leave a room through an edge or step on stairs.</summary>
public sealed record AreaLink(int RoomX, int RoomY, Dir? Edge, Checkpoint Target);

public sealed class AreaDef
{
    public const int RoomTilesX = 20, RoomTilesY = 10, TileSize = 16;
    public const int RoomPixelWidth = RoomTilesX * TileSize, RoomPixelHeight = RoomTilesY * TileSize;

    public required string Id { get; init; }
    public required string Name { get; init; }
    public required bool IsDungeon { get; init; }
    public required string Music { get; init; }
    public required RoomDef?[,] Rooms { get; init; }
    public IReadOnlyList<AreaLink> Links { get; init; } = [];
    public int Width => Rooms.GetLength(0);
    public int Height => Rooms.GetLength(1);

    public RoomDef? Room(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height ? Rooms[x, y] : null;

    /// <summary>Builds the room grid from rows of optional rooms (row-major, as laid out in source).</summary>
    public static RoomDef?[,] Grid(params RoomDef?[][] rows)
    {
        var grid = new RoomDef?[rows[0].Length, rows.Length];
        for (int y = 0; y < rows.Length; y++)
            for (int x = 0; x < rows[y].Length; x++) grid[x, y] = rows[y][x];
        return grid;
    }
}

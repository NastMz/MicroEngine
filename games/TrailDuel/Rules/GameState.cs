using MicroEngine.Core;

namespace TrailDuel.Rules;

public enum Direction { Down, Up, Left, Right }

public static class DirectionX
{
    public static (int X, int Y) Delta(this Direction d) => d switch
    {
        Direction.Down => (0, 1),
        Direction.Up => (0, -1),
        Direction.Left => (-1, 0),
        _ => (1, 0),
    };

    public static Direction Opposite(this Direction d) => d switch
    {
        Direction.Down => Direction.Up,
        Direction.Up => Direction.Down,
        Direction.Left => Direction.Right,
        _ => Direction.Left,
    };
}

/// <summary>
/// Everything that persists between sessions: position, party, bag, money and story flags.
/// Serialized as-is by <see cref="SaveStore"/>.
/// </summary>
public sealed class GameState
{
    public const string PlayerName = "Tavi";
    public const string RivalName = "Wren";
    public const string SaveSlot = "adventure";

    public string MapId { get; set; } = "home";
    public int X { get; set; } = 7;
    public int Y { get; set; } = 3;
    public Direction Facing { get; set; } = Direction.Down;
    public List<Creature> Party { get; set; } = [];
    public List<Creature> Box { get; set; } = [];
    public Bag Bag { get; set; } = new();
    public int Money { get; set; } = 1500;
    public HashSet<string> Flags { get; set; } = new(StringComparer.Ordinal);
    public HashSet<string> Seen { get; set; } = new(StringComparer.Ordinal);
    public HashSet<string> Caught { get; set; } = new(StringComparer.Ordinal);
    public string StarterId { get; set; } = "";
    public string RespawnMap { get; set; } = "home";
    public int RespawnX { get; set; } = 4;
    public int RespawnY { get; set; } = 6;
    public double PlaySeconds { get; set; }
    public int BattlesWon { get; set; }
    public int Badges => Flags.Contains("master") ? 1 : 0;

    public bool Has(string flag) => Flags.Contains(flag);
    public void Set(string flag) => Flags.Add(flag);

    /// <summary>A fresh adventure in the player's bedroom.</summary>
    public static GameState NewGame()
    {
        var s = new GameState();
        s.Bag.Add(ItemDex.Potion.Id, 2);
        return s;
    }

    public void HealParty()
    {
        foreach (var c in Party) c.FullHeal();
    }

    /// <summary>Adds a creature to the party, or the box when the party is full.</summary>
    public bool AddCreature(Creature c)
    {
        Seen.Add(c.SpeciesId);
        Caught.Add(c.SpeciesId);
        if (Party.Count < Battle.MaxParty) { Party.Add(c); return true; }
        Box.Add(c);
        return false;
    }

    /// <summary>The rival picks the starter whose type beats the player's.</summary>
    public static string RivalStarterFor(string playerStarter) => playerStarter switch
    {
        "emberpup" => "puddlepip",
        "puddlepip" => "sprigbun",
        _ => "emberpup",
    };

    public bool AnyHealthy => Party.Any(c => !c.Fainted);

    /// <summary>After losing every creature: lose half the money, heal and return to the last rest point.</summary>
    public int WhiteOut()
    {
        int lost = Money / 2;
        Money -= lost;
        HealParty();
        MapId = RespawnMap;
        X = RespawnX;
        Y = RespawnY;
        Facing = Direction.Down;
        return lost;
    }

    public void Save(SaveStore store) => store.Save(SaveSlot, this);
    public static GameState? Load(SaveStore store) => store.Load<GameState>(SaveSlot);
}

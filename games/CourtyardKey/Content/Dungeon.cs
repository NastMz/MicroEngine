using CourtyardKey.Model;

namespace CourtyardKey.Content;

/// <summary>
/// The Sunken Courtyard: nine chambers on a 3x4 grid. Two small keys open two locks, the wind
/// boomerang reaches the crystal switch, and the boss key opens the Warden's arena.
/// </summary>
public static class Dungeon
{
    public const string Id = "dungeon";

    public static readonly Checkpoint EntranceCheckpoint = new(Id, 1, 3, 10f, 8.2f, Dir.Up);

    public static AreaDef Create() => new()
    {
        Id = Id,
        Name = "The Sunken Courtyard",
        IsDungeon = true,
        Music = "dungeon",
        Rooms = AreaDef.Grid(
            [null, BossArena(), BossKeyVault()],
            [WindTrial(), PillarHall(), ChannelRoom()],
            [BlockRoom(), Courtyard(), AmbushRoom()],
            [null, Entrance(), null]),
        Links = [new AreaLink(1, 3, Dir.Down, Overworld.DungeonExit)],
    };

    private const float Dark = 0.74f;

    private static RoomDef Entrance() => new()
    {
        Name = "Entrance Hall",
        Base = Tile.Floor,
        Ambience = Ambience.Dust,
        Darkness = Dark,
        Map =
        [
            "####T####DD####T####",
            "#........++........#",
            "#..s.....++.....s..#",
            "#........++........#",
            "#...i....++....i...#",
            "#........++........#",
            "#..s.....++.....s..#",
            "#........++........#",
            "#...p....++....p...#",
            "#########EE#########",
        ],
    };

    private static RoomDef Courtyard() => new()
    {
        Name = "Sunken Courtyard",
        Base = Tile.Floor,
        Ambience = Ambience.Dust,
        Darkness = Dark - 0.08f,
        Map =
        [
            "####T####LL####T####",
            "#~~~.....++.....~~~#",
            "#~~~..g..++..g..~~~#",
            "#.......s..s.......#",
            "D.......+..+.......D",
            "D.......+..+.......D",
            "#.......s..s...g...#",
            "#~~~.....++.....~~~#",
            "#~~~.p...++...p.~~~#",
            "#########DD#########",
        ],
    };

    private static RoomDef BlockRoom() => new()
    {
        Name = "Hall of Weights",
        Base = Tile.Floor,
        Ambience = Ambience.Dust,
        Darkness = Dark,
        Rule = RoomRule.Plate,
        Map =
        [
            "#####T######T#######",
            "#..................#",
            "#.sssss............#",
            "#.s_..s......c.....#",
            "#.s.....B..........D",
            "#.s...s............D",
            "#.sssss.......B....#",
            "#..................#",
            "#..p............p..#",
            "####################",
        ],
        Chests = [new ChestDef(Item.SmallKey)],
    };

    private static RoomDef AmbushRoom() => new()
    {
        Name = "Bell Chamber",
        Base = Tile.Floor,
        Ambience = Ambience.Dust,
        Darkness = Dark,
        Rule = RoomRule.Ambush,
        Map =
        [
            "####T#########T#####",
            "#.p..............p.#",
            "#....v........v....#",
            "#.....s......s.....#",
            "X.........c........#",
            "X..................#",
            "#.....s......s..z..#",
            "#.......v..........#",
            "#.p..............p.#",
            "####################",
        ],
        Chests = [new ChestDef(Item.SmallKey)],
    };

    private static RoomDef PillarHall() => new()
    {
        Name = "Pillar Hall",
        Base = Tile.Floor,
        Ambience = Ambience.Dust,
        Darkness = Dark,
        Map =
        [
            "####T####KK####T####",
            "#..................#",
            "#..|...|....|...|..#",
            "#.........k........#",
            "D..................L",
            "D..................L",
            "#..|...|....|...|..#",
            "#....g........g....#",
            "#..................#",
            "#########LL#########",
        ],
    };

    private static RoomDef WindTrial() => new()
    {
        Name = "Trial of Wind",
        Base = Tile.Floor,
        Ambience = Ambience.Dust,
        Darkness = Dark,
        Rule = RoomRule.Ambush,
        Map =
        [
            "####T#########T#####",
            "#..................#",
            "#..i............i..#",
            "#.......g..g.......#",
            "#.........c........X",
            "#....k.............X",
            "#..................#",
            "#..i............i..#",
            "#..p............p..#",
            "####################",
        ],
        Chests = [new ChestDef(Item.Boomerang)],
    };

    private static RoomDef ChannelRoom() => new()
    {
        Name = "Flooded Gallery",
        Base = Tile.Floor,
        Ambience = Ambience.Dust,
        Darkness = Dark,
        Rule = RoomRule.Crystal,
        Map =
        [
            "####T####XX####T####",
            "#..................#",
            "#..~~~~~.....~~~~~.#",
            "#..~...~..z..~...~.#",
            "L..~.G.~.....~.*.~.#",
            "L..~~~~~.....~~~~~.#",
            "#..................#",
            "#..p..........z..p.#",
            "#..................#",
            "####################",
        ],
    };

    private static RoomDef BossKeyVault() => new()
    {
        Name = "Warden's Vault",
        Base = Tile.Floor,
        Ambience = Ambience.Dust,
        Darkness = Dark,
        Map =
        [
            "####T#########T#####",
            "#.......sCs........#",
            "#..................#",
            "#...k..........k...#",
            "#.....~~....~~.....#",
            "#.....~~....~~.....#",
            "#.........z........#",
            "#..i............i..#",
            "#..................#",
            "#########DD#########",
        ],
        Chests = [new ChestDef(Item.BossKey)],
    };

    private static RoomDef BossArena() => new()
    {
        Name = "Warden's Court",
        Base = Tile.Floor,
        Ambience = Ambience.Dust,
        Darkness = Dark + 0.05f,
        Rule = RoomRule.Boss,
        Map =
        [
            "####T####T#T####T###",
            "#..................#",
            "#.i..............i.#",
            "#.......++++.......#",
            "#.......+O++.......#",
            "#.......++P+.......#",
            "#.......++++.......#",
            "#.i..............i.#",
            "#..................#",
            "#########XX#########",
        ],
    };
}

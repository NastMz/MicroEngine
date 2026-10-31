using CourtyardKey.Content;
using CourtyardKey.Model;
using CourtyardKey.World;
using MicroEngine.World;

namespace CourtyardKey.Tests;

/// <summary>Lints the hand-authored maps so a typo can never strand the hero.</summary>
[TestClass]
public sealed class ContentTests
{
    private static IEnumerable<AreaDef> Areas() => [Overworld.Create(), Dungeon.Create()];

    private static IEnumerable<(AreaDef Area, int X, int Y, RoomDef Room)> Rooms()
    {
        foreach (var a in Areas())
            for (int x = 0; x < a.Width; x++)
                for (int y = 0; y < a.Height; y++)
                    if (a.Room(x, y) is { } r) yield return (a, x, y, r);
    }

    [TestMethod]
    public void EveryRoomIsTwentyByTenAndParses()
    {
        foreach (var (a, x, y, r) in Rooms())
        {
            Assert.AreEqual(Room.Hgt, r.Map.Length, $"{a.Id} {x},{y} height");
            foreach (var row in r.Map) Assert.AreEqual(Room.W, row.Length, $"{a.Id} {x},{y} row '{row}'");
            _ = new Room(a, x, y);
        }
    }

    [TestMethod]
    public void MarkersHaveTheirData()
    {
        foreach (var (a, x, y, r) in Rooms())
        {
            var room = new Room(a, x, y);
            int chests = room.Placements.Count(p => p.Marker is Marker.Chest or Marker.HiddenChest);
            Assert.AreEqual(r.Chests.Count, chests, $"{a.Id} {x},{y} chest contents");
            Assert.AreEqual(r.Signs.Count, room.Placements.Count(p => p.Marker == Marker.Sign), $"{a.Id} {x},{y} sign texts");
            if (room.Placements.Any(p => p.Marker == Marker.HiddenChest))
                Assert.AreNotEqual(RoomRule.None, r.Rule, $"{a.Id} {x},{y}: hidden chests need a rule that reveals them");
        }
    }

    [TestMethod]
    public void TreeAndHouseFootprintsAreMarkedCompletely()
    {
        foreach (var (a, x, y, r) in Rooms().Where(t => !t.Area.IsDungeon))
            for (int ty = 0; ty < Room.Hgt; ty++)
                for (int tx = 0; tx < Room.W; tx++)
                {
                    char c = r.Map[ty][tx];
                    if (c == 'T')
                        foreach (var (dx, dy) in new[] { (1, 0), (0, 1), (1, 1) })
                            Assert.AreEqual('t', r.Map[ty + dy][tx + dx], $"{x},{y} tree at {tx},{ty}");
                    if (c == 'A')
                        for (int dy = 0; dy < 3; dy++)
                            for (int dx = 0; dx < 4; dx++)
                                if (dx + dy > 0) Assert.AreEqual('h', r.Map[ty + dy][tx + dx], $"{x},{y} house at {tx},{ty}");
                }
    }

    [TestMethod]
    public void OpenEdgesLineUpWithTheNeighbouringRoom()
    {
        foreach (var area in Areas())
        {
            var map = new AreaMap(area);
            for (int x = 0; x < area.Width; x++)
                for (int y = 0; y < area.Height; y++)
                {
                    if (map.Pristine(x, y) is not { } room) continue;
                    // Right edge against the left edge of the east neighbour, bottom against top of the south one.
                    if (map.Pristine(x + 1, y) is { } east)
                        for (int ty = 0; ty < Room.Hgt; ty++)
                            Assert.AreEqual(Walkable(room, Room.W - 1, ty), Walkable(east, 0, ty), $"{area.Id} {x},{y} east edge row {ty}");
                    if (map.Pristine(x, y + 1) is { } south)
                        for (int tx = 0; tx < Room.W; tx++)
                            Assert.AreEqual(Walkable(room, tx, Room.Hgt - 1), Walkable(south, tx, 0), $"{area.Id} {x},{y} south edge column {tx}");
                }
        }
    }

    private static bool Walkable(Room room, int x, int y)
    {
        var flags = TileFlagsTable.For(room.TileAt(x, y));
        return (flags & (TileFlags.Solid | TileFlags.Water)) == 0;
    }

    [TestMethod]
    public void EveryRoomIsReachableFromItsAreaEntrance()
    {
        foreach (var area in Areas())
        {
            var map = new AreaMap(area);
            var start = area.IsDungeon ? (1, 3) : (1, 2);
            var seen = new HashSet<(int, int)> { start };
            var queue = new Queue<(int X, int Y)>([start]);
            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                var room = map.Pristine(x, y)!;
                foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                {
                    if (map.Pristine(x + dx, y + dy) is null || seen.Contains((x + dx, y + dy))) continue;
                    bool open = Enumerable.Range(0, dx != 0 ? Room.Hgt : Room.W).Any(i =>
                        dx == 1 ? Walkable(room, Room.W - 1, i) : dx == -1 ? Walkable(room, 0, i) : dy == 1 ? Walkable(room, i, Room.Hgt - 1) : Walkable(room, i, 0));
                    if (!open) continue;
                    seen.Add((x + dx, y + dy));
                    queue.Enqueue((x + dx, y + dy));
                }
            }
            int total = 0;
            for (int x = 0; x < area.Width; x++)
                for (int y = 0; y < area.Height; y++)
                    if (map.Exists(x, y)) total++;
            Assert.AreEqual(total, seen.Count, $"{area.Id}: unreachable rooms");
        }
    }

    [TestMethod]
    public void LockedDoorsMatchOnBothSidesAndKeysSuffice()
    {
        var area = Dungeon.Create();
        int locks = 0;
        for (int x = 0; x < area.Width; x++)
            for (int y = 0; y < area.Height; y++)
            {
                if (area.Room(x, y) is not { } r) continue;
                if (area.Room(x, y - 1) is { } north && r.Map[0].Contains('L'))
                {
                    Assert.IsTrue(north.Map[Room.Hgt - 1].Contains('L'), $"lock between {x},{y} and north");
                    locks++;
                }
                if (area.Room(x + 1, y) is { } east && r.Map.Any(row => row[^1] == 'L'))
                {
                    Assert.IsTrue(east.Map.Any(row => row[0] == 'L'), $"lock between {x},{y} and east");
                    locks++;
                }
            }
        int keys = Rooms().Where(t => t.Area.IsDungeon).Sum(t => t.Room.Chests.Count(c => c.Item == Item.SmallKey));
        Assert.AreEqual(2, locks);
        Assert.AreEqual(locks, keys, "Every small-key lock has exactly one key.");
    }
}

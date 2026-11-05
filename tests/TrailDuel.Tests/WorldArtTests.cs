using TrailDuel.Rules;
using TrailDuel.Visuals;
using TrailDuel.World;

namespace TrailDuel.Tests;

/// <summary>
/// The overworld art keeps to the map's rules: buildings stay inside their footprint with the door on the door tile,
/// and no tall crown rises over a tile someone can walk on (it is drawn after that row's characters).
/// </summary>
[TestClass]
public sealed class WorldArtTests
{
    [TestMethod]
    public void EveryBuildingDrawingFitsItsFootprintAndOpenDoorsLeadSomewhere()
    {
        var state = GameState.NewGame();
        foreach (var map in Maps.Every)
        {
            var runtime = new MapRuntime(map, state);
            foreach (var b in map.Buildings)
            {
                string name = $"{map.Id} building at {b.X},{b.Y}";
                Assert.IsTrue(BuildingArt.Fits(b), $"{name}: the drawing {BuildingArt.Bounds(b)} leaves its {b.W}x{b.H} footprint");
                int doorX = b.X + b.DoorX, doorY = b.Y + b.H - 1;
                if (b.Locked)
                {
                    Assert.IsTrue(runtime.TileSolid(doorX, doorY), $"{name}: a locked door is solid");
                    Assert.IsNotNull(runtime.LockedDoorAt(doorX, doorY), $"{name}: the locked door answers on its tile");
                    continue;
                }
                Assert.IsFalse(runtime.TileSolid(doorX, doorY), $"{name}: the door tile is open");
                Assert.IsNotNull(runtime.WarpAt(doorX, doorY), $"{name}: the door tile warps inside");
                Assert.IsFalse(runtime.TileSolid(doorX, doorY + 1), $"{name}: the step in front of the door is walkable");
            }
        }
    }

    [TestMethod]
    public void TallCrownsNeverRiseOverWalkableTiles()
    {
        int tall = 0, low = 0;
        foreach (var map in Maps.Every)
            for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                if (TreeLayout.At(map, x, y) is not { } tree) continue;
                if (tree.Low) { low++; continue; }
                tall++;
                // A centred crown spans half a tile on each side; a leaning one a whole tile on its side.
                int from = tree.Shift > 0 ? x : x - 1, to = tree.Shift < 0 ? x : x + 1;
                for (int cx = from; cx <= to; cx++)
                    Assert.IsFalse(TreeLayout.Walkable(map, cx, y - 1), $"{map.Id}: the tree at {x},{y} rises over walkable {cx},{y - 1}");
            }
        Assert.IsTrue(tall > 100 && low > 20, $"woods use both trees ({tall} tall, {low} low)");
    }

    [TestMethod]
    public void TreesStandOnEveryTreeTileExceptStaggeredGapsInAWood()
    {
        foreach (var map in Maps.Every)
            for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                if (map.KindAt(x, y) is not (TileKind.Tree or TileKind.Pine)) continue;
                bool gap = TreeLayout.Gap(map, x, y);
                Assert.AreEqual(!gap, TreeLayout.At(map, x, y) is not null, $"{map.Id} tree tile {x},{y}");
            }
    }
}

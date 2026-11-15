using System.Numerics;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Navigation;
using MicroEngine.World;
using Pathfinding;

namespace Samples.Tests;

/// <summary>The pathfinding sample, played headlessly: every agent finds its way and none walks into a wall.</summary>
[TestClass]
public sealed class PathfindingTests
{
    private static (PathfindingGame Game, RoomScene Room) Start()
    {
        var game = new PathfindingGame();
        game.InitializeForTest(HeadlessPlatform.For(game.Settings));
        return (game, (RoomScene)game.Scenes.Current!);
    }

    private static Vector2 Cell(RoomScene room, int x, int y) => room.Nav.CenterOf(room.Nav.NodeOf(x, y));

    /// <summary>Steps until <paramref name="done"/> or <paramref name="limit"/> steps, checking after each that nobody is inside a solid tile.</summary>
    private static int StepUntil(PathfindingGame game, RoomScene room, Func<bool> done, int limit)
    {
        int steps = 0;
        for (; steps < limit && !done(); steps++)
        {
            game.StepForTest();
            foreach (var walker in room.Swarm.Append(room.Player).Append(room.Chaser).Append(room.Patrol))
                foreach (var corner in new[] { new Vector2(walker.Bounds.Left + 0.01f, walker.Bounds.Top + 0.01f), new Vector2(walker.Bounds.Right - 0.01f, walker.Bounds.Bottom - 0.01f) })
                    Assert.AreEqual(TileFlags.None, room.Tiles.FlagsAt((int)(corner.X / RoomScene.Tile), (int)(corner.Y / RoomScene.Tile)) & TileFlags.Solid, $"step {steps}: a walker at {walker.Center} is inside a wall");
        }
        return steps;
    }

    [TestMethod]
    public void A_click_sends_the_unit_around_the_walls_to_the_exact_point()
    {
        var (game, room) = Start();
        var target = Cell(room, 16, 8) + new Vector2(3, -2);
        Assert.AreEqual(PathStatus.Found, room.SendPlayerTo(target));
        Assert.IsGreaterThan(1, room.Player.Route.Count, "the room's walls are between them: the route turns");
        Assert.IsLessThan(room.Player.Cells.Count - 1, room.Player.Route.Count, "and it has fewer legs than cells");
        int steps = StepUntil(game, room, () => room.Player.Route.Count == 0, 900);
        Assert.IsLessThan(900, steps);
        Assert.IsLessThan(0.01f, Vector2.Distance(target, room.Player.Center));

        Assert.AreEqual(PathStatus.Unreachable, room.SendPlayerTo(new Vector2(4, 4)), "a click on a wall");
        StepUntil(game, room, () => room.Player.Route.Count == 0, 900);
        Assert.AreEqual(Cell(room, 1, 1), room.Player.Center, "the unit goes to the nearest cell it can stand on");
    }

    [TestMethod]
    public void The_chaser_goes_around_the_mud_reaches_the_player_and_follows_it()
    {
        var (game, room) = Start();
        game.StepForTest();
        Assert.IsFalse(room.Chaser.Cells.Any(node => room.Nav.CostAt(room.Nav.CellOf(node).X, room.Nav.CellOf(node).Y) > 1), "going around the mud is cheaper");
        StepUntil(game, room, () => Vector2.Distance(room.Chaser.Center, room.Player.Center) <= RoomScene.CatchDistance, 600);
        Assert.IsLessThanOrEqualTo(RoomScene.CatchDistance, Vector2.Distance(room.Chaser.Center, room.Player.Center));
        int searches = room.Chaser.Searches;
        Assert.AreEqual(1, searches, "the player stood still: one search was enough");

        room.SendPlayerTo(Cell(room, 10, 8));
        StepUntil(game, room, () => room.Player.Route.Count == 0, 600);
        StepUntil(game, room, () => Vector2.Distance(room.Chaser.Center, room.Player.Center) <= RoomScene.CatchDistance, 900);
        Assert.IsLessThanOrEqualTo(RoomScene.CatchDistance, Vector2.Distance(room.Chaser.Center, room.Player.Center));
        Assert.IsGreaterThan(searches, room.Chaser.Searches);
    }

    [TestMethod]
    public void The_patrol_takes_the_door_and_searches_again_only_when_the_door_closes_on_its_path()
    {
        var (game, room) = Start();
        game.StepForTest();
        CollectionAssert.Contains(room.Patrol.Cells, room.DoorCell, "the door is the short way between the posts");
        Assert.AreEqual(1, room.Patrol.Searches);
        int through = room.Patrol.Cells.Count;

        Assert.IsTrue(room.SetDoor(true));
        game.StepForTest();
        Assert.AreEqual(2, room.Patrol.Searches, "its path was closed");
        CollectionAssert.DoesNotContain(room.Patrol.Cells, room.DoorCell);
        Assert.IsGreaterThan(through + 8, room.Patrol.Cells.Count, "the way around is longer");

        Assert.IsTrue(room.SetDoor(false));
        game.StepForTest();
        Assert.AreEqual(2, room.Patrol.Searches, "the door opened elsewhere: its path is still good");
        int steps = StepUntil(game, room, () => Vector2.Distance(room.Patrol.Center, room.Posts[1]) < 0.01f, 1200);
        Assert.IsLessThan(1200, steps, "it arrives at the other post");
        game.StepForTest();
        CollectionAssert.Contains(room.Patrol.Cells, room.DoorCell, "and comes back through the open door");
    }

    [TestMethod]
    public void The_door_stays_open_while_somebody_stands_in_it()
    {
        var (game, room) = Start();
        StepUntil(game, room, () => room.Patrol.Bounds.Intersects(room.DoorBounds), 600);
        Assert.IsFalse(room.SetDoor(true));
        Assert.IsFalse(room.DoorClosed);
    }

    [TestMethod]
    public void Platforms_are_climbed_by_the_jump_and_left_by_the_drops()
    {
        var level = new Platforms();
        var finder = new PathFinder();
        CollectionAssert.AreEqual(
            new[] { (level.GroundMiddle, Platforms.Walk), (level.LedgeLeft, Platforms.Jump), (level.LedgeRight, Platforms.Walk) },
            level.Steps(finder, level.GroundRight, level.LedgeRight), "under the ledge's right end there is no way up: back to the jump");
        CollectionAssert.AreEqual(new[] { (level.GroundRight, Platforms.Drop) }, level.Steps(finder, level.LedgeRight, level.GroundRight));
        level.Graph.SetEnabled(level.LedgeLeft, false);
        Assert.IsEmpty(level.Steps(finder, level.GroundLeft, level.LedgeRight), "the ledge's left end fell: nothing reaches the ledge");
    }

    [TestMethod]
    public void A_board_of_hexagons_is_searched_by_the_same_finder()
    {
        var board = new HexBoard(7, 7);
        var finder = new PathFinder();
        int from = board.NodeOf(0, 0), to = board.NodeOf(4, 4);
        Assert.AreEqual(PathStatus.Found, finder.Find(board, from, to));
        Assert.AreEqual(6, finder.Cost, "two hexagons across and four down, two of them shared");
        Assert.HasCount(7, finder.Path);

        // A wall across row 2 with its gap at the far right.
        for (int column = 0; column < 6; column++) board.Block(column, 2);
        Assert.AreEqual(PathStatus.Found, finder.Find(board, from, to));
        CollectionAssert.Contains(finder.Path.ToArray(), board.NodeOf(6, 2), "through the gap");
        Assert.AreEqual(10, finder.Cost);
        board.Block(6, 2);
        Assert.AreEqual(PathStatus.Unreachable, finder.Find(board, from, to));
    }

    [TestMethod]
    public void The_swarm_gathers_at_the_player_by_reading_one_field()
    {
        var (game, room) = Start();
        Assert.HasCount(3, room.Swarm);
        StepUntil(game, room, () => room.Swarm.All(bug => Vector2.Distance(bug.Center, room.Player.Center) <= RoomScene.CatchDistance), 1500);
        foreach (var bug in room.Swarm)
            Assert.IsLessThanOrEqualTo(RoomScene.CatchDistance, Vector2.Distance(bug.Center, room.Player.Center));
        Assert.AreEqual(0, room.Swarm.Sum(bug => bug.Searches), "no bug searched a path of its own");
    }
}

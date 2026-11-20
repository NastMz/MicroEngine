using System.Numerics;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Navigation;
using MicroEngine.Scenes;
using MicroEngine.Testing;
using Pathfinding;

namespace Samples.Tests;

/// <summary>
/// The pathfinding sample, played headlessly: the room where every agent finds its way, the group order and the tower
/// defence. In every step no body is in a wall and no two bodies of a crowd are on top of each other.
/// </summary>
[TestClass]
public sealed class PathfindingTests
{
    private static (PathfindingGame Game, T Scene) Start<T>() where T : Scene, new()
    {
        var game = new PathfindingGame();
        game.InitializeForTest(HeadlessPlatform.For(game.Settings));
        if (game.Scenes.Current is not T)
        {
            game.Scenes.Replace(new T());
            game.StepForTest();
        }
        return (game, (T)game.Scenes.Current!);
    }

    private static Vector2 Cell(NavGrid grid, int x, int y) => grid.CenterOf(grid.NodeOf(x, y));

    /// <summary>Steps until <paramref name="done"/> or <paramref name="limit"/> steps, checking the bodies after each.</summary>
    private static int StepUntil(PathfindingGame game, NavAgents agents, Func<bool> done, int limit)
    {
        int steps = 0;
        for (; steps < limit && !done(); steps++)
        {
            game.StepForTest();
            var grid = agents.Grid;
            for (int i = 0; i < agents.Count; i++)
            {
                var agent = agents[i];
                float r = agent.Radius - 0.02f;
                foreach (var corner in new[] { new Vector2(-r, -r), new Vector2(r, r), new Vector2(-r, r), new Vector2(r, -r) })
                {
                    var cell = grid.CellAt(agent.Position + corner);
                    Assert.IsTrue(grid.IsWalkable(cell.X, cell.Y), $"step {steps}: the agent at {agent.Position} is inside the wall of {cell}");
                }
                for (int j = i + 1; j < agents.Count; j++)
                {
                    float least = MathF.Min(agent.Radius, agents[j].Radius);
                    float gap = Vector2.Distance(agent.Position, agents[j].Position) - agent.Radius - agents[j].Radius;
                    Assert.IsGreaterThan(-0.25f * least, gap, $"step {steps}: two bodies overlap by {-gap} at {agent.Position}");
                }
            }
        }
        return steps;
    }

    // ------------------------------------------------------------------ the room

    [TestMethod]
    public void A_click_sends_the_unit_around_the_walls_to_the_exact_point()
    {
        var (game, room) = Start<RoomScene>();
        var target = Cell(room.Nav, 16, 8) + new Vector2(3, -2);
        room.SendPlayerTo(target);
        game.StepForTest();
        Assert.AreEqual(PathStatus.Found, room.Player.Path);
        Assert.IsGreaterThan(1, room.Player.Route.Count, "the room's walls are between them: the route turns");
        int steps = StepUntil(game, room.Agents, () => room.Player.Status == NavAgentStatus.Arrived, 900);
        Assert.IsLessThan(900, steps);
        Assert.IsLessThan(0.01f, Vector2.Distance(target, room.Player.Position));

        room.SendPlayerTo(new Vector2(4, 4));
        game.StepForTest();
        Assert.AreEqual(PathStatus.Unreachable, room.Player.Path, "a click on a wall");
        StepUntil(game, room.Agents, () => room.Player.Status == NavAgentStatus.Arrived, 900);
        Assert.AreEqual(room.Nav.NodeOf(1, 1), room.Nav.NodeAt(room.Player.Position), "the unit goes to the nearest cell it can stand on");
    }

    [TestMethod]
    public void The_chaser_reaches_the_player_and_follows_it()
    {
        var (game, room) = Start<RoomScene>();
        float reach = RoomScene.CatchDistance + 2 * RoomScene.Radius;
        StepUntil(game, room.Agents, () => room.Chaser.Status == NavAgentStatus.Arrived, 600);
        Assert.AreEqual(NavAgentStatus.Arrived, room.Chaser.Status);
        Assert.IsLessThanOrEqualTo(reach, Vector2.Distance(room.Chaser.Position, room.Player.Position));

        room.SendPlayerTo(Cell(room.Nav, 10, 8));
        StepUntil(game, room.Agents, () => room.Player.Status == NavAgentStatus.Arrived, 600);
        Assert.AreEqual(NavAgentStatus.Arrived, room.Player.Status);
        StepUntil(game, room.Agents, () => room.Chaser.Status == NavAgentStatus.Arrived, 900);
        Assert.IsLessThanOrEqualTo(reach, Vector2.Distance(room.Chaser.Position, room.Player.Position), "it went after the player");
    }

    [TestMethod]
    public void The_patrol_takes_the_door_and_goes_around_when_the_door_closes_on_its_path()
    {
        var (game, room) = Start<RoomScene>();
        bool throughDoor = false;
        StepUntil(game, room.Agents, () => throughDoor |= room.Nav.NodeAt(room.Patrol.Position) == room.DoorCell, 300);
        Assert.IsTrue(throughDoor, "the door is the short way between the posts");
        StepUntil(game, room.Agents, () => Vector2.Distance(room.Patrol.Position, room.Posts[1]) < 0.01f, 600);
        StepUntil(game, room.Agents, () => !room.Patrol.Position.Equals(room.Posts[1]) && room.Patrol.Status == NavAgentStatus.Moving && !room.Agents.Occupied(room.DoorBounds), 120);

        // On its way back the door closes ahead of it: it goes around, and still gets to its post.
        Assert.IsTrue(room.SetDoor(true));
        int searches = room.Agents.Searches;
        game.StepForTest();
        Assert.IsGreaterThan(searches, room.Agents.Searches, "its path was closed: it searched again without being told");
        throughDoor = false;
        int steps = StepUntil(game, room.Agents, () =>
        {
            throughDoor |= room.Nav.NodeAt(room.Patrol.Position) == room.DoorCell;
            return Vector2.Distance(room.Patrol.Position, room.Posts[0]) < 0.01f;
        }, 1500);
        Assert.IsLessThan(1500, steps, "it arrives at the other post the long way");
        Assert.IsFalse(throughDoor);
        Assert.IsGreaterThan(250, steps, "which is much longer than the door");
    }

    [TestMethod]
    public void The_door_stays_open_while_somebody_stands_in_it()
    {
        var (game, room) = Start<RoomScene>();
        StepUntil(game, room.Agents, () => room.Agents.Occupied(room.DoorBounds), 600);
        Assert.IsFalse(room.SetDoor(true));
        Assert.IsFalse(room.DoorClosed);
    }

    [TestMethod]
    public void The_swarm_gathers_at_the_player_by_reading_one_field()
    {
        var (game, room) = Start<RoomScene>();
        Assert.HasCount(3, room.Swarm);
        // Only the patrol and the chaser search; the bugs read the field.
        StepUntil(game, room.Agents, () => room.Swarm.All(bug => bug.Status == NavAgentStatus.Arrived), 1500);
        foreach (var bug in room.Swarm)
        {
            Assert.AreEqual(NavAgentStatus.Arrived, bug.Status);
            Assert.IsLessThanOrEqualTo(RoomScene.Tile * 1.5f, Vector2.Distance(bug.Position, room.Player.Position), "a bug stopped far from the player");
        }

        // The player walks off: the field is computed again and the swarm follows.
        room.SendPlayerTo(Cell(room.Nav, 5, 2));
        StepUntil(game, room.Agents, () => room.Player.Status == NavAgentStatus.Arrived, 600);
        StepUntil(game, room.Agents, () => room.Swarm.All(bug => bug.Status == NavAgentStatus.Arrived), 1500);
        foreach (var bug in room.Swarm)
            Assert.IsLessThanOrEqualTo(RoomScene.Tile * 1.5f, Vector2.Distance(bug.Position, room.Player.Position));
    }

    // ------------------------------------------------------------------ the group order

    [TestMethod]
    public void A_group_order_sends_every_unit_and_they_gather_around_the_point()
    {
        var (game, field) = Start<SkirmishScene>();
        Assert.HasCount(12, field.Units);
        var large = field.Units.Where(unit => unit.Radius > SkirmishScene.Tile / 2).ToList();
        Assert.HasCount(2, large);

        var point = Cell(field.Nav, 15, 2);
        field.Order(point);
        int narrowGap = field.Nav.NodeOf(8, 3);
        bool largeInNarrowGap = false, smallInNarrowGap = false;
        int steps = StepUntil(game, field.Agents, () =>
        {
            foreach (var unit in field.Units)
            {
                if (field.Nav.NodeAt(unit.Position) != narrowGap) continue;
                if (unit.Radius > SkirmishScene.Tile / 2) largeInNarrowGap = true; else smallInNarrowGap = true;
            }
            return field.Units.All(unit => unit.Status == NavAgentStatus.Arrived);
        }, 2400);
        Assert.IsLessThan(2400, steps, "every unit ends its order");
        Assert.IsTrue(smallInNarrowGap, "the small units take the gap of one cell");
        Assert.IsFalse(largeInNarrowGap, "the large ones go the wide way");
        foreach (var unit in field.Units)
        {
            Assert.AreEqual(PathStatus.Found, unit.Path);
            Assert.IsLessThan(60, Vector2.Distance(unit.Position, point), $"a unit stopped at {unit.Position}");
        }
        Assert.IsTrue(field.Units.Any(unit => Vector2.Distance(unit.Position, point) < 6), "one stands on the point");
    }

    // ------------------------------------------------------------------ the tower defence

    [TestMethod]
    public void A_wave_reaches_the_keep_around_the_towers_and_a_tower_that_would_wall_it_in_is_refused()
    {
        var (game, field) = Start<DefenceScene>();
        StepUntil(game, field.Agents, () => field.Creeps.Count >= 3, 300);
        Assert.IsGreaterThanOrEqualTo(3, field.Creeps.Count, "creeps leave the gate one after another");

        // A wall of towers across the field, built while the wave walks.
        for (int y = 1; y <= 8; y++) Assert.IsTrue(field.Toggle(new NavCell(10, y)), $"a tower at row {y}");
        Assert.HasCount(8, field.Towers);
        Assert.IsFalse(field.Toggle(new NavCell(10, 9)), "the last gap: the creeps would have no way to the keep");
        Assert.AreEqual(new NavCell(10, 9), field.Refused);
        Assert.IsTrue(field.Nav.IsWalkable(10, 9));
        Assert.IsFalse(field.Toggle(new NavCell(0, 0)), "a wall of the field");
        var under = field.Nav.CellAt(field.Creeps[0].Position);
        Assert.IsFalse(field.Toggle(under), "a creep stands there");
        Assert.HasCount(8, field.Towers);

        int steps = StepUntil(game, field.Agents, () => field.Reached >= 6, 3000);
        Assert.IsLessThan(3000, steps, "the wave goes through the gap left and reaches the keep");
        Assert.IsTrue(field.Creeps.All(creep => creep.Path != PathStatus.Unreachable));

        // A tower taken away opens the short way again.
        Assert.IsTrue(field.Toggle(new NavCell(10, 5)));
        Assert.HasCount(7, field.Towers);
        Assert.IsTrue(field.Nav.IsWalkable(10, 5));
    }

    // ------------------------------------------------------------------ by hand, and the other spaces

    [TestMethod]
    public void A_courier_moved_by_hand_walks_the_route_it_was_given()
    {
        var grid = new NavGrid(12, 6, 16);
        for (int y = 0; y < 5; y++) grid.SetWalkable(6, y, false);
        var courier = new Courier(grid, new PathFinder(), Cell(grid, 2, 1), radius: 5, speed: 60);
        var target = Cell(grid, 9, 1) + new Vector2(2, 3);
        Assert.AreEqual(PathStatus.Found, courier.Plan(target));
        Assert.IsGreaterThan(1, courier.Route.Count);
        Assert.IsTrue(grid.IsOpen(courier.Cells));
        for (int step = 0; step < 600 && courier.Route.Count > 0; step++)
        {
            courier.Step(1 / 60f);
            Assert.IsTrue(grid.IsWalkable(grid.CellAt(courier.Position).X, grid.CellAt(courier.Position).Y));
        }
        Assert.IsLessThan(0.01f, Vector2.Distance(target, courier.Position));

        grid.SetWalkable(6, 5, false);
        Assert.IsFalse(grid.IsOpen(courier.Cells), "the way it came is closed now");
        Assert.AreEqual(PathStatus.Unreachable, courier.Plan(Cell(grid, 2, 1)));
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

    // ------------------------------------------------------------------ the whole sample

    [TestMethod]
    public void The_scripted_tour_of_the_three_scenes_repeats_exactly()
    {
        var script = StateTraceRuns.Script("samples/Pathfinding/scripts/tour.script");
        var run = StateTraceRuns.Run(new PathfindingGame(), script);
        Assert.IsNull(run.Result.Error);
        StateTraceRuns.AssertIdentical(run.Trace, StateTraceRuns.Run(new PathfindingGame(), script).Trace, "the pathfinding tour");
        StateTraceRuns.AssertDrawingChangesNothing(run, "the pathfinding tour");
    }
}

using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Kernel;
using MicroEngine.Physics;
using MicroEngine.Physics.Tiles;
using MicroEngine.Scenes;

namespace MicroEngine.Navigation.Tests;

/// <summary>Agents that follow their routes, keep apart, arrive, and search again when the map changes.</summary>
[TestClass]
public sealed class NavAgentsTests
{
    private const float Dt = 1 / 60f;
    private const int Cell = 16;

    /// <summary>A grid of 16-unit cells from text rows; '#' is a wall.</summary>
    private static NavGrid Grid(params string[] rows)
    {
        var grid = new NavGrid(rows[0].Length, rows.Length, Cell);
        for (int y = 0; y < rows.Length; y++)
            for (int x = 0; x < rows[y].Length; x++)
                if (rows[y][x] == '#') grid.SetWalkable(x, y, false);
        return grid;
    }

    private static Vector2 At(NavGrid grid, int x, int y) => grid.CenterOf(grid.NodeOf(x, y));

    private static readonly string[] Yard =
    [
        "####################",
        "#......#...........#",
        "#......#...........#",
        "#......#...........#",
        "#..................#",
        "#......#...........#",
        "#......#...........#",
        "#......#...........#",
        "#......#...........#",
        "####################",
    ];

    /// <summary>Steps until <paramref name="done"/>, checking after each step that no body is in a wall and no two bodies overlap.</summary>
    private static int Run(NavAgents agents, Func<bool> done, int limit, bool apart = true)
    {
        int steps = 0;
        for (; steps < limit && !done(); steps++)
        {
            agents.Step(Dt);
            Check(agents, steps, apart);
        }
        return steps;
    }

    private static void Check(NavAgents agents, int step, bool apart)
    {
        var grid = agents.Grid;
        for (int i = 0; i < agents.Count; i++)
        {
            var agent = agents[i];
            float r = agent.Radius - 0.02f;
            foreach (var corner in new[] { new Vector2(-r, -r), new Vector2(r, -r), new Vector2(-r, r), new Vector2(r, r) })
            {
                var cell = grid.CellAt(agent.Position + corner);
                Assert.IsTrue(grid.IsWalkable(cell.X, cell.Y), $"step {step}: agent {i} at {agent.Position} is in the wall of cell {cell}");
            }
            for (int j = i + 1; apart && j < agents.Count; j++)
            {
                // Bodies are moved apart every step; pressed together in a passage they may overlap by a small part of a radius.
                float gap = Vector2.Distance(agent.Position, agents[j].Position) - agent.Radius - agents[j].Radius;
                Assert.IsGreaterThan(-0.2f * MathF.Min(agent.Radius, agents[j].Radius), gap, $"step {step}: agents {i} and {j} overlap by {-gap}");
            }
        }
    }

    private static ulong Hash(NavAgents agents)
    {
        var hash = new StateHasher();
        agents.HashState(hash);
        return hash.Value;
    }

    [TestMethod]
    public void An_agent_walks_around_the_walls_to_the_exact_point_and_arrives()
    {
        var grid = Grid(Yard);
        var agents = new NavAgents(grid);
        var agent = agents.Add(At(grid, 2, 2), new NavAgentSettings(radius: 5, speed: 80));
        Assert.AreEqual(NavAgentStatus.Idle, agent.Status);
        Assert.AreEqual(1, agents.Count);
        Assert.AreSame(agent, agents[0]);

        var target = At(grid, 15, 7) + new Vector2(3, -2);
        agent.MoveTo(target);
        Assert.AreEqual(NavAgentStatus.Planning, agent.Status, "the route is searched by the next step");
        Assert.AreEqual(PathStatus.Searching, agent.Path);
        Assert.AreEqual(target, agent.Destination);
        agents.Step(Dt);
        Assert.AreEqual(NavAgentStatus.Moving, agent.Status);
        Assert.AreEqual(PathStatus.Found, agent.Path);
        Assert.IsGreaterThan(1, agent.Route.Count, "the wall is between them: the route turns at its gap");
        Assert.AreEqual(target, agent.Route[^1]);
        Assert.AreEqual(80, agent.Velocity.Length(), 0.01f);

        int steps = Run(agents, () => agent.Status == NavAgentStatus.Arrived, 600);
        Assert.IsLessThan(600, steps);
        Assert.AreEqual(target, agent.Position, "alone, an agent stops on the point asked for");
        Assert.AreEqual(Vector2.Zero, agent.Velocity);
        Assert.IsEmpty(agent.Route);
        float straight = Vector2.Distance(At(grid, 2, 2), target);
        Assert.IsLessThan(straight * 1.35f / 80 / Dt, steps, "it walks at its speed along a short route");

        agent.MoveTo(target);
        agents.Step(Dt);
        Assert.AreEqual(NavAgentStatus.Arrived, agent.Status, "asked for where it already is");
        agent.Stop();
        Assert.AreEqual(NavAgentStatus.Idle, agent.Status);
    }

    [TestMethod]
    public void A_point_that_cannot_be_reached_sends_the_agent_as_near_as_it_gets()
    {
        var grid = Grid(
            "##########",
            "#....#...#",
            "#....#...#",
            "#....#...#",
            "##########");
        var agents = new NavAgents(grid);
        var agent = agents.Add(At(grid, 1, 1), new NavAgentSettings(5, 80));
        agent.MoveTo(At(grid, 7, 2));
        agents.Step(Dt);
        Assert.AreEqual(PathStatus.Unreachable, agent.Path);
        Assert.AreEqual(NavAgentStatus.Moving, agent.Status);
        Assert.AreEqual(1, agents.Searches);
        Assert.IsLessThan(12, agents.Expanded, "the search knew where to stop: it did not look at the whole room");
        Run(agents, () => agent.Status == NavAgentStatus.Arrived, 300);
        Assert.AreEqual(At(grid, 4, 2), agent.Position, "the cell of its room nearest the point");
        Assert.AreEqual(At(grid, 7, 2), agent.Destination, "what was asked is remembered");

        agent.MoveTo(new Vector2(-50, 20));
        agents.Step(Dt);
        Assert.AreEqual(PathStatus.Unreachable, agent.Path, "a point outside the grid");
        Run(agents, () => agent.Status == NavAgentStatus.Arrived, 300);
        Assert.AreEqual(At(grid, 1, 1), agent.Position);
    }

    [TestMethod]
    public void An_agent_searches_again_only_when_a_change_of_the_map_closes_its_path()
    {
        var grid = Grid(Yard);
        var agents = new NavAgents(grid);
        var agent = agents.Add(At(grid, 2, 2), new NavAgentSettings(5, 80));
        agent.MoveTo(At(grid, 15, 2));
        agents.Step(Dt);
        Assert.AreEqual(1, agents.Searches);

        grid.SetWalkable(17, 7, false);
        Run(agents, () => false, 3);
        Assert.AreEqual(1, agents.Searches, "a wall far from its path costs it nothing");

        grid.SetWalkable(7, 4, false);
        agents.Step(Dt);
        Assert.AreEqual(2, agents.Searches, "the gap closed");
        Assert.AreEqual(PathStatus.Unreachable, agent.Path);
        Run(agents, () => agent.Status == NavAgentStatus.Arrived, 300);
        Assert.AreEqual(6, grid.CellAt(agent.Position).X, "it waits by the wall, as near as it gets");

        grid.SetWalkable(7, 4, true);
        agents.Step(Dt);
        Assert.AreEqual(3, agents.Searches, "a way opened: an agent that could not reach its point tries again");
        Assert.AreEqual(PathStatus.Found, agent.Path);
        Run(agents, () => agent.Status == NavAgentStatus.Arrived, 600);
        Assert.AreEqual(At(grid, 15, 2), agent.Position);
    }

    [TestMethod]
    public void Costly_ground_is_gone_around_and_slows_only_those_it_is_set_to_slow()
    {
        var grid = Grid(
            "############",
            "#..........#",
            "#..........#",
            "#..........#",
            "############");
        for (int x = 4; x <= 6; x++) grid.SetCost(x, 2, 3);
        var agents = new NavAgents(grid);
        var walker = agents.Add(At(grid, 1, 2), new NavAgentSettings(5, 60) { SlowedByCost = true });
        walker.MoveTo(At(grid, 10, 2));
        bool inMud = false;
        int around = Run(agents, () =>
        {
            var cell = grid.CellAt(walker.Position);
            inMud |= grid.CostAt(cell.X, cell.Y) > 1;
            return walker.Status == NavAgentStatus.Arrived;
        }, 600);
        Assert.IsFalse(inMud, "the way around the mud is cheaper than the way through");

        // Mud everywhere on the way: it is crossed, at a third of the speed by the one it slows.
        for (int y = 1; y <= 3; y++)
            for (int x = 4; x <= 6; x++) grid.SetCost(x, y, 3);
        var hover = agents.Add(At(grid, 10, 1), new NavAgentSettings(5, 60) { Collides = false });
        walker.Settings = walker.Settings with { Collides = false };
        walker.MoveTo(At(grid, 1, 2));
        hover.MoveTo(At(grid, 1, 1));
        int steps = 0;
        float slowest = 60, hoverSlowest = 60;
        for (; steps < 900 && !(walker.Status == NavAgentStatus.Arrived && hover.Status == NavAgentStatus.Arrived); steps++)
        {
            agents.Step(Dt);
            if (walker.Status == NavAgentStatus.Moving) slowest = MathF.Min(slowest, walker.Velocity.Length());
            if (hover.Status == NavAgentStatus.Moving) hoverSlowest = MathF.Min(hoverSlowest, hover.Velocity.Length());
        }
        Assert.IsLessThan(900, steps);
        Assert.AreEqual(20, slowest, 0.1f, "a cell that costs three is walked at a third of the speed");
        Assert.AreEqual(60, hoverSlowest, 0.1f, "unless the ground does not slow it");
    }

    [TestMethod]
    public void Following_a_moving_target_searches_once_per_cell_the_target_enters()
    {
        var grid = Grid(Yard);
        var agents = new NavAgents(grid);
        var chaser = agents.Add(At(grid, 2, 2), new NavAgentSettings(5, 60));
        var target = At(grid, 12, 6) + new Vector2(-6, 0);
        for (int step = 0; step < 20; step++)
        {
            target.X += 0.5f;      // ten units in all: it stays in its cell
            chaser.MoveTo(target);
            agents.Step(Dt);
        }
        Assert.AreEqual(1, agents.Searches, "the target moved inside one cell");
        Assert.AreEqual(target, chaser.Route[^1], "and the route ends where the target is now");

        target.X += 12;
        chaser.MoveTo(target);
        agents.Step(Dt);
        Assert.AreEqual(2, agents.Searches, "it entered the next cell");
        Assert.AreEqual(NavAgentStatus.Moving, chaser.Status);
    }

    [TestMethod]
    public void A_group_sent_to_one_point_gathers_around_it_without_stacking()
    {
        var grid = Grid(Yard);
        var agents = new NavAgents(grid);
        var settings = new NavAgentSettings(radius: 5, speed: 70);
        var group = new List<NavAgent>();
        for (int y = 1; y <= 8; y += 2)
            for (int x = 1; x <= 5; x += 2)
                group.Add(agents.Add(At(grid, x, y), settings));
        var point = At(grid, 14, 5);
        foreach (var unit in group) unit.MoveTo(point);

        int steps = Run(agents, () => group.TrueForAll(unit => unit.Status == NavAgentStatus.Arrived), 1500);
        Assert.IsLessThan(1500, steps, "every unit of the group ends its order");
        // Twelve bodies of radius 5 packed around a point: all within a few bodies of it.
        foreach (var unit in group) Assert.IsLessThan(40, Vector2.Distance(unit.Position, point), $"a unit stopped at {unit.Position}");
        Assert.IsTrue(group.Exists(unit => Vector2.Distance(unit.Position, point) < 6), "one of them stands on the point");
        foreach (var unit in group) Assert.AreEqual(Vector2.Zero, unit.Velocity);
        Assert.IsFalse(group.Exists(unit => unit.IsBlocked));
    }

    [TestMethod]
    public void Two_groups_that_meet_in_a_passage_get_past_each_other()
    {
        var grid = Grid(
            "######################",
            "#....................#",
            "#....................#",
            "#....................#",
            "#....................#",
            "#....................#",
            "######################");
        var agents = new NavAgents(grid);
        var settings = new NavAgentSettings(radius: 5, speed: 60);
        var all = new List<(NavAgent Unit, Vector2 Goal)>();
        for (int i = 0; i < 4; i++)
        {
            all.Add((agents.Add(At(grid, 1, 1 + i), settings), At(grid, 20, 1 + i)));
            all.Add((agents.Add(At(grid, 20, 1 + i), settings), At(grid, 1, 1 + i)));
        }
        foreach (var (unit, goal) in all) unit.MoveTo(goal);
        int steps = Run(agents, () => all.TrueForAll(a => a.Unit.Status == NavAgentStatus.Arrived), 1200);
        Assert.IsLessThan(1200, steps, "nobody is left pushing against somebody else");
        foreach (var (unit, goal) in all) Assert.IsLessThan(1f, Vector2.Distance(unit.Position, goal));
    }

    [TestMethod]
    public void A_body_wider_than_a_cell_goes_around_the_gap_and_never_touches_a_wall()
    {
        var grid = Grid(
            "####################",
            "#......#...........#",
            "#......#...........#",
            "#......#...........#",
            "#..................#",
            "#......#...........#",
            "#......#...........#",
            "#......#...........#",
            "#..................#",
            "#..................#",
            "#..................#",
            "####################");
        var agents = new NavAgents(grid);
        var small = agents.Add(At(grid, 3, 3), new NavAgentSettings(5, 60) { Collides = false });
        var large = agents.Add(At(grid, 3, 3), new NavAgentSettings(20, 60) { Collides = false });
        small.MoveTo(At(grid, 12, 3));
        large.MoveTo(At(grid, 12, 3));
        agents.Step(Dt);
        Assert.AreEqual(PathStatus.Found, large.Path);
        bool throughGap = false, largeThroughGap = false;
        int steps = Run(agents, () =>
        {
            throughGap |= grid.NodeAt(small.Position) == grid.NodeOf(7, 4);
            largeThroughGap |= grid.CellAt(large.Position).X == 7 && grid.CellAt(large.Position).Y < 8;
            return small.Status == NavAgentStatus.Arrived && large.Status == NavAgentStatus.Arrived;
        }, 1200, apart: false);
        Assert.IsLessThan(1200, steps);
        Assert.IsTrue(throughGap, "the small one takes the gap");
        Assert.IsFalse(largeThroughGap, "the large one the wide way under the wall");
        Assert.AreEqual(At(grid, 12, 3), large.Position);
    }

    [TestMethod]
    public void Searches_spread_over_steps_end_in_the_same_routes()
    {
        static (NavAgents Agents, List<NavAgent> Units) Order(int budget)
        {
            var grid = Grid(Yard);
            var agents = new NavAgents(grid) { SearchBudget = budget };
            var units = new List<NavAgent>();
            for (int i = 0; i < 6; i++) units.Add(agents.Add(At(grid, 1 + i % 3 * 2, 1 + i), new NavAgentSettings(5, 70)));
            foreach (var unit in units) unit.MoveTo(At(grid, 16, 2 + units.IndexOf(unit)));
            return (agents, units);
        }

        var (whole, wholeUnits) = Order(int.MaxValue);
        whole.Step(Dt);
        Assert.IsTrue(wholeUnits.TrueForAll(unit => unit.Status == NavAgentStatus.Moving), "with no limit every route is known in the first step");

        var (sliced, slicedUnits) = Order(10);
        Assert.AreEqual(10, sliced.SearchBudget);
        sliced.Step(Dt);
        Assert.AreEqual(NavAgentStatus.Planning, slicedUnits[^1].Status, "ten nodes a step: the last unit waits its turn");
        Assert.AreEqual(Vector2.Zero, slicedUnits[^1].Velocity, "standing where it is");
        int steps = Run(sliced, () => slicedUnits.TrueForAll(unit => unit.Status != NavAgentStatus.Planning), 200);
        Assert.IsGreaterThan(5, steps);
        Assert.IsLessThan(200, steps);
        Assert.AreEqual(whole.Expanded, sliced.Expanded, "the same work, divided");
        Run(sliced, () => slicedUnits.TrueForAll(unit => unit.Status == NavAgentStatus.Arrived), 1200);
        Run(whole, () => wholeUnits.TrueForAll(unit => unit.Status == NavAgentStatus.Arrived), 1200);
        for (int i = 0; i < 6; i++) Assert.IsLessThan(1f, Vector2.Distance(wholeUnits[i].Position, slicedUnits[i].Position), "and the same places in the end");
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => sliced.SearchBudget = 0);
    }

    [TestMethod]
    public void A_swarm_reads_one_field_and_gathers_without_stacking()
    {
        var grid = Grid(Yard);
        var agents = new NavAgents(grid);
        var field = new DistanceField(grid);
        field.Compute(grid.NodeOf(15, 4));
        var swarm = new List<NavAgent>();
        for (int y = 1; y <= 8; y++)
            for (int x = 1; x <= 3; x++)
                swarm.Add(agents.Add(At(grid, x, y), new NavAgentSettings(3, 50)));
        foreach (var bug in swarm) bug.Follow(field);
        agents.Step(Dt);
        Assert.AreEqual(0, agents.Searches, "nobody searched: the field is the route");
        Assert.IsTrue(swarm.TrueForAll(bug => bug.Status == NavAgentStatus.Moving));

        int steps = Run(agents, () => swarm.TrueForAll(bug => bug.Status == NavAgentStatus.Arrived), 2000);
        Assert.IsLessThan(2000, steps);
        var goal = At(grid, 15, 4);
        foreach (var bug in swarm) Assert.IsLessThan(36, Vector2.Distance(bug.Position, goal));

        var other = new DistanceField(Grid(Yard));
        Assert.ThrowsExactly<ArgumentException>(() => swarm[0].Follow(other), "a field of another grid");
    }

    private static readonly string[] Field =
    [
        "################",
        "#..............#",
        "#..............#",
        "G..............K",
        "#..............#",
        "#..............#",
        "################",
    ];

    [TestMethod]
    public void A_wave_walks_to_the_keep_around_towers_built_while_it_walks()
    {
        var grid = Grid(Field);
        var agents = new NavAgents(grid);
        Vector2 gate = At(grid, 0, 3), keep = At(grid, 15, 3);
        var creeps = new List<NavAgent>();
        int arrived = 0;

        RectF Tile(int x, int y) => new(x * Cell, y * Cell, Cell, Cell);
        bool Build(int x, int y)
        {
            // The game's rule: nobody stands there, no creep is walled in, and the gate keeps a way to the keep.
            if (!agents.CanBlock(Tile(x, y)) || !agents.Regions(4).StaysConnected(Tile(x, y), grid.NodeAt(gate), grid.NodeAt(keep))) return false;
            grid.SetBlocked(Tile(x, y), true);
            return true;
        }

        for (int step = 0; step < 2400 && arrived < 8; step++)
        {
            if (step % 30 == 0 && creeps.Count + arrived < 8)
            {
                var creep = agents.Add(gate, new NavAgentSettings(4, 45));
                creep.MoveTo(keep);
                creeps.Add(creep);
            }
            // A wall of towers across the field, built while the wave walks; the last one would close it.
            if (step == 40) Assert.IsTrue(Build(8, 1));
            if (step == 41) Assert.IsTrue(Build(8, 2));
            if (step == 42) Assert.IsTrue(Build(8, 3), "on the creeps' way: they go around");
            if (step == 43) Assert.IsTrue(Build(8, 4));
            if (step == 44) Assert.IsFalse(Build(8, 5), "the last gap: the wave would be walled in");
            if (step == 45) Assert.IsFalse(Build(0, 3), "the gate itself");
            if (step == 46)
            {
                var first = grid.CellAt(creeps[0].Position);
                Assert.IsFalse(Build(first.X, first.Y), "a creep stands there");
                Assert.IsTrue(agents.Occupied(Tile(first.X, first.Y)));
                Assert.IsFalse(agents.Occupied(Tile(14, 1)));
            }
            agents.Step(Dt);
            Check(agents, step, apart: true);
            for (int i = creeps.Count - 1; i >= 0; i--)
            {
                if (creeps[i].Status != NavAgentStatus.Arrived) continue;
                Assert.IsLessThan(12f, Vector2.Distance(creeps[i].Position, keep), "a creep stopped short of the keep");
                Assert.IsTrue(agents.Remove(creeps[i]));
                creeps.RemoveAt(i);
                arrived++;
            }
        }
        Assert.AreEqual(8, arrived, "the whole wave reaches the keep through the gap left");
        Assert.AreEqual(0, agents.Count);
        Assert.IsTrue(grid.IsWalkable(8, 5));
    }

    [TestMethod]
    public void The_same_orders_give_the_same_state_every_run()
    {
        static List<ulong> Play(bool nudge)
        {
            var grid = Grid(Yard);
            var agents = new NavAgents(grid) { SearchBudget = 40 };
            var units = new List<NavAgent>();
            for (int i = 0; i < 10; i++) units.Add(agents.Add(At(grid, 1 + i % 5, 1 + i / 5 * 3), new NavAgentSettings(4 + i % 3, 50 + 5 * i)));
            var hashes = new List<ulong>();
            for (int step = 0; step < 400; step++)
            {
                if (step == 0) foreach (var unit in units) unit.MoveTo(At(grid, 13, 4));
                if (step == 120) grid.SetWalkable(7, 4, false);
                if (step == 150) grid.SetWalkable(7, 4, true);
                if (step == 200) agents.Remove(units[3]);
                if (step == 210 && nudge) units[0].Position += new Vector2(0.01f, 0);
                agents.Step(Dt);
                hashes.Add(Hash(agents));
            }
            return hashes;
        }

        var first = Play(nudge: false);
        CollectionAssert.AreEqual(first, Play(nudge: false));
        var nudged = Play(nudge: true);
        CollectionAssert.AreEqual(first.Take(210).ToList(), nudged.Take(210).ToList());
        Assert.AreNotEqual(first[215], nudged[215], "a hundredth of a unit is a different state");
        Assert.AreNotEqual(first[0], first[1], "the state changes while they walk");
    }

    [TestMethod]
    public void Stepping_a_crowd_allocates_nothing_once_it_has_walked()
    {
        var grid = Grid(Yard);
        var agents = new NavAgents(grid);
        var units = new List<NavAgent>();
        for (int i = 0; i < 24; i++) units.Add(agents.Add(At(grid, 1 + i % 6, 1 + i / 6 * 2), new NavAgentSettings(4, 60)));
        Vector2[] points = [At(grid, 14, 5), At(grid, 3, 4), At(grid, 16, 2), At(grid, 4, 7)];
        void Round(int steps)
        {
            for (int step = 0; step < steps; step++)
            {
                if (step % 150 == 0) foreach (var unit in units) unit.MoveTo(points[step / 150 % points.Length]);
                if (step % 150 == 75) grid.SetWalkable(12, 4, !grid.IsWalkable(12, 4));
                agents.Step(Dt);
            }
        }
        Round(1800);
        long before = GC.GetAllocatedBytesForCurrentThread();
        Round(600);
        Assert.AreEqual(0, GC.GetAllocatedBytesForCurrentThread() - before, "bytes allocated by 600 steps of 24 agents with orders, searches and a changing map");
    }

    [TestMethod]
    public void In_a_scene_the_agents_are_stepped_with_it_and_are_part_of_its_state()
    {
        var game = new YardGame();
        game.InitializeForTest(HeadlessPlatform.For(game.Settings));
        var scene = (YardScene)game.Scenes.Current!;
        ulong before = game.ComputeStateHash().Hash;
        game.StepForTest();
        Assert.AreEqual(NavAgentStatus.Moving, scene.Unit.Status, "the scene stepped the system: nothing was called by hand");
        Assert.AreNotEqual(before, game.ComputeStateHash().Hash, "the agents are in the game's state hash");
        var at = scene.Unit.Position;
        game.StepForTest();
        Assert.AreNotEqual(at, scene.Unit.Position);
        game.Scenes.Pop();
        game.StepForTest();
        Assert.AreEqual(0, scene.Agents.Count, "the system ends with its scene");
        Assert.ThrowsExactly<InvalidOperationException>(() => scene.Unit.MoveTo(Vector2.Zero));
    }

    private sealed class YardGame : Game
    {
        public override GameSettings Settings { get; } = new("Yard");
        protected override void Load() => Scenes.Push(new YardScene());
    }

    private sealed class YardScene : Scene
    {
        public NavAgents Agents = null!;
        public NavAgent Unit = null!;

        public override void Enter()
        {
            var grid = Grid(Yard);
            Agents = Systems.Add(new NavAgents(grid));
            Unit = Agents.Add(At(grid, 2, 2), new NavAgentSettings(5, 80));
            Unit.MoveTo(At(grid, 15, 7));
        }

        public override void Update(float dt) { }
        public override void Draw(MicroEngine.Graphics.IRenderer r) { }
    }

    [TestMethod]
    public void An_agent_moved_by_the_game_is_steered_and_told_where_it_is()
    {
        var grid = Grid(Yard);
        // The same walls as bodies of a physics world, and a character the game moves through it.
        var world = new TilePhysicsWorld2D();
        var level = world.CreateBody(default, BodyDescription.Static(Vector2.Zero));
        for (int y = 0; y < grid.Height; y++)
            for (int x = 0; x < grid.Width; x++)
                if (!grid.IsWalkable(x, y)) world.AddShape(level, ShapeDescription.Box(new Vector2(Cell, Cell), new Vector2(x * Cell + Cell / 2, y * Cell + Cell / 2)));
        var hero = TopDownController2D.Create(world, default, At(grid, 2, 2), new Vector2(10, 10), new TopDownMotion { MoveSpeed = 80 });

        var agents = new NavAgents(grid);
        var agent = agents.Add(hero.Position, new NavAgentSettings(5, 80) { MovedByGame = true });
        var target = At(grid, 15, 7);
        agent.MoveTo(target);
        int steps = 0;
        for (; steps < 900 && agent.Status != NavAgentStatus.Arrived; steps++)
        {
            agents.Step(Dt);
            var before = agent.Position;
            Assert.AreEqual(before, hero.Position, "the system does not move it");
            hero.Step(Dt, agent.DesiredVelocity / hero.Motion.MoveSpeed);
            agent.Position = hero.Position;
        }
        Assert.IsLessThan(900, steps);
        Assert.IsLessThan(0.5f, Vector2.Distance(hero.Position, target));
        Assert.AreEqual(Vector2.Zero, agent.DesiredVelocity);
    }

    [TestMethod]
    public void Agents_are_added_removed_asked_for_and_refused_with_reasons()
    {
        var grid = Grid(Yard);
        var agents = new NavAgents(grid);
        var a = agents.Add(At(grid, 2, 2), new NavAgentSettings(5, 60)) ;
        var b = agents.Add(At(grid, 3, 2), new NavAgentSettings(5, 60));
        var far = agents.Add(At(grid, 15, 7), new NavAgentSettings(5, 60));
        a.Tag = "first";

        var found = new List<NavAgent>();
        Assert.AreEqual(2, agents.Query(At(grid, 2, 2), 12, found));
        CollectionAssert.AreEquivalent(new[] { a, b }, found);
        Assert.AreEqual("first", found.Find(agent => agent == a)!.Tag);
        Assert.AreEqual(1, agents.Query(At(grid, 15, 7), 0, found), "a body that reaches the point");
        Assert.HasCount(3, found, "results are added to the list");
        Assert.AreEqual(0, agents.Query(At(grid, 10, 5), 20, found));

        Assert.IsTrue(agents.Contains(b));
        Assert.IsTrue(agents.Remove(b));
        Assert.IsFalse(agents.Remove(b));
        Assert.IsFalse(agents.Contains(b));
        Assert.AreEqual(2, agents.Count);
        Assert.AreSame(far, agents[1]);
        Assert.ThrowsExactly<InvalidOperationException>(() => b.MoveTo(Vector2.Zero), "an agent that left the system takes no orders");

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => agents.Add(new Vector2(float.NaN, 0), new NavAgentSettings(5, 60)));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new NavAgentSettings(-1, 60));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new NavAgentSettings(5, 0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => a.Settings = a.Settings with { Weight = 0 });
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => a.MoveTo(new Vector2(float.PositiveInfinity, 0)));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => agents.Step(-1));

        a.Settings = a.Settings with { Speed = 20 };
        a.MoveTo(At(grid, 5, 2));
        agents.Step(Dt);
        Assert.AreEqual(20, a.Velocity.Length(), 0.01f, "settings may change while it walks");

        agents.Clear();
        Assert.AreEqual(0, agents.Count);
        Assert.IsFalse(agents.Contains(a));
        agents.Step(Dt);
    }

    [TestMethod]
    public void An_agent_kept_from_its_point_says_so_and_keeps_trying()
    {
        var grid = Grid(
            "#########",
            "#.......#",
            "####.####",
            "#.......#",
            "#########");
        var agents = new NavAgents(grid);
        // A body that does not move stands in the only gap; a second one wants to cross.
        var guard = agents.Add(At(grid, 4, 2), new NavAgentSettings(7.5f, 60) { Weight = 1000 });
        var walker = agents.Add(At(grid, 1, 1), new NavAgentSettings(5, 60));
        walker.MoveTo(At(grid, 1, 3));
        Run(agents, () => walker.IsBlocked, 600);
        Assert.IsTrue(walker.IsBlocked, "it made no way for a while");
        Assert.AreEqual(NavAgentStatus.Moving, walker.Status, "being blocked is not the end of the order: the game decides");
        Assert.AreEqual(1, grid.CellAt(walker.Position).Y);

        agents.Remove(guard);
        int steps = Run(agents, () => walker.Status == NavAgentStatus.Arrived, 600);
        Assert.IsLessThan(600, steps, "the way cleared");
        Assert.IsFalse(walker.IsBlocked);
    }
}

/// <summary>The steering rules the agents are made of, for games that move things their own way.</summary>
[TestClass]
public sealed class SteeringTests
{
    [TestMethod]
    public void Seek_heads_for_the_target_at_full_speed_and_arrive_slows_down_near_it()
    {
        Assert.AreEqual(new Vector2(0, 50), Steering.Seek(new Vector2(10, 10), new Vector2(10, 90), 50));
        Assert.AreEqual(Vector2.Zero, Steering.Seek(new Vector2(3, 3), new Vector2(3, 3), 50), "already there");
        Assert.AreEqual(new Vector2(0, -50), Steering.Flee(new Vector2(10, 10), new Vector2(10, 90), 50));

        Assert.AreEqual(new Vector2(50, 0), Steering.Arrive(Vector2.Zero, new Vector2(100, 0), 50, slowingDistance: 40));
        Assert.AreEqual(new Vector2(25, 0), Steering.Arrive(new Vector2(80, 0), new Vector2(100, 0), 50, slowingDistance: 40));
        Assert.AreEqual(new Vector2(50, 0), Steering.Arrive(new Vector2(80, 0), new Vector2(100, 0), 50, slowingDistance: 0), "no slowing distance: full speed to the end");
        Assert.AreEqual(Vector2.Zero, Steering.Arrive(new Vector2(100, 0), new Vector2(100, 0), 50, 40));
    }

    [TestMethod]
    public void Separation_pushes_away_from_a_neighbour_harder_the_nearer_it_is()
    {
        var far = Steering.Separate(Vector2.Zero, 5, new Vector2(30, 0), 5, distance: 10);
        Assert.AreEqual(Vector2.Zero, far, "beyond the distance kept");
        var near = Steering.Separate(Vector2.Zero, 5, new Vector2(15, 0), 5, distance: 10);
        Assert.AreEqual(new Vector2(-0.5f, 0), near, "half the distance left: half the push, away from the neighbour");
        var touching = Steering.Separate(Vector2.Zero, 5, new Vector2(0, 8), 5, distance: 10);
        Assert.AreEqual(new Vector2(0, -1), touching, "bodies that touch push in full");
        Assert.AreEqual(1, Steering.Separate(new Vector2(4, 4), 5, new Vector2(4, 4), 5, 10).Length(), 1e-5f, "on the same spot a direction is still chosen");
    }

    [TestMethod]
    public void Avoidance_turns_aside_from_a_body_it_would_run_into_and_ignores_the_others()
    {
        // Head-on: each turns to its own side, so they pass.
        var mine = Steering.Avoid(Vector2.Zero, new Vector2(50, 0), 5, new Vector2(60, 0), new Vector2(-50, 0), 5, horizon: 1);
        var theirs = Steering.Avoid(new Vector2(60, 0), new Vector2(-50, 0), 5, Vector2.Zero, new Vector2(50, 0), 5, horizon: 1);
        Assert.AreNotEqual(0, mine.Y);
        Assert.AreEqual(0, mine.X, 1e-5f, "the push is across the way, not against it");
        Assert.AreEqual(-mine.Y, theirs.Y, 1e-5f, "the other turns the other way in the world: both to their own right");

        Assert.AreEqual(Vector2.Zero, Steering.Avoid(Vector2.Zero, new Vector2(50, 0), 5, new Vector2(60, 40), new Vector2(50, 0), 5, 1), "walking side by side");
        Assert.AreEqual(Vector2.Zero, Steering.Avoid(Vector2.Zero, new Vector2(50, 0), 5, new Vector2(-60, 0), new Vector2(-50, 0), 5, 1), "moving apart");
        Assert.AreEqual(Vector2.Zero, Steering.Avoid(Vector2.Zero, new Vector2(50, 0), 5, new Vector2(500, 0), new Vector2(-50, 0), 5, 1), "too far ahead to matter yet");

        // A body crossing from the right, ahead: turn away from where it will be.
        var crossing = Steering.Avoid(Vector2.Zero, new Vector2(50, 0), 5, new Vector2(30, 28), new Vector2(0, -50), 5, 1);
        Assert.AreNotEqual(Vector2.Zero, crossing);
        Assert.IsLessThanOrEqualTo(1f, crossing.Length());
    }

    [TestMethod]
    public void A_body_slides_along_the_walls_of_a_grid_and_never_enters_them()
    {
        var grid = new NavGrid(6, 4, 10);
        grid.SetWalkable(3, 1, false);
        // Straight into the wall: stopped at its face.
        Assert.AreEqual(new Vector2(26, 15), grid.Slide(new Vector2(15, 15), new Vector2(30, 0), 4));
        // Diagonally into it: stopped across, free along.
        Assert.AreEqual(new Vector2(26, 21), grid.Slide(new Vector2(20, 15), new Vector2(20, 6), 4));
        // Open ground.
        Assert.AreEqual(new Vector2(25, 32), grid.Slide(new Vector2(15, 30), new Vector2(10, 2), 4));
        // The grid's edge is a wall.
        Assert.AreEqual(new Vector2(4, 36), grid.Slide(new Vector2(15, 30), new Vector2(-40, 40), 4));
        // A point slides too.
        Assert.AreEqual(new Vector2(36, 10), grid.Slide(new Vector2(41, 5), new Vector2(-5, 30), 0));
        // A body caught in a wall that appeared walks out of it.
        grid.SetWalkable(1, 1, false);
        Assert.AreEqual(new Vector2(5, 15), grid.Slide(new Vector2(15, 15), new Vector2(-10, 0), 4));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => grid.Slide(Vector2.Zero, Vector2.Zero, -1));
    }
}

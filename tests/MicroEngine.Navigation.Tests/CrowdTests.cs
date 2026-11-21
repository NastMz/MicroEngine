using System.Numerics;
using MicroEngine.Kernel;

namespace MicroEngine.Navigation.Tests;

/// <summary>
/// Crowds under pressure: many agents through one gap, bodies wider than a cell, long steps. Every order ends, nobody
/// reports having arrived on the wrong side of a wall, and bodies overlap no more than the guide says.
/// </summary>
[TestClass]
public sealed class CrowdTests
{
    private const float Dt = 1 / 60f;
    private static readonly Vector2 Goal = new(45.5f, 20.5f);

    /// <summary>A 60 x 40 grid of unit cells with a wall down column 30 and a gap of <paramref name="gapRows"/> rows at row 20 (none for 0).</summary>
    private static NavGrid Wall(int gapRows)
    {
        var grid = new NavGrid(60, 40);
        int top = 20 - gapRows / 2;
        for (int y = 0; y < 40; y++)
            if (y < top || y >= top + gapRows) grid.SetWalkable(30, y, false);
        return grid;
    }

    /// <summary>A block of agents left of the wall, in rows of <paramref name="columns"/>.</summary>
    private static (NavAgents Agents, List<NavAgent> Units) Crowd(NavGrid grid, int count, float radius, float speed, int columns = 20, float spacing = 1)
    {
        var agents = new NavAgents(grid);
        var units = new List<NavAgent>();
        var settings = new NavAgentSettings(radius, speed);
        for (int i = 0; i < count; i++)
            units.Add(agents.Add(new Vector2(4.5f + i % columns * spacing, 20.5f - count / columns * spacing / 2 + i / columns * spacing), settings));
        return (agents, units);
    }

    /// <summary>The largest overlap of two bodies, as a part of the sum of their radii.</summary>
    private static float Overlap(NavAgents agents)
    {
        float worst = 0;
        for (int i = 0; i < agents.Count; i++)
            for (int j = i + 1; j < agents.Count; j++)
            {
                float reach = agents[i].Radius + agents[j].Radius;
                worst = MathF.Max(worst, (reach - Vector2.Distance(agents[i].Position, agents[j].Position)) / reach);
            }
        return worst;
    }

    private static ulong Hash(NavAgents agents)
    {
        var hash = new StateHasher();
        agents.HashState(hash);
        return hash.Value;
    }

    [TestMethod]
    public void Followers_of_a_field_all_pass_a_gap_of_one_cell_and_none_arrives_before_it()
    {
        var grid = Wall(1);
        var (agents, units) = Crowd(grid, 200, 0.3f, 4);
        var field = new DistanceField(grid, 0.3f);
        field.Compute(grid.NodeAt(Goal));
        foreach (var unit in units) unit.Follow(field);
        int steps = 0;
        for (; steps < 180 * 60 && !units.TrueForAll(unit => unit.Status == NavAgentStatus.Arrived); steps++)
        {
            agents.Step(Dt);
            foreach (var unit in units)
                if (unit.Status == NavAgentStatus.Arrived) Assert.IsGreaterThan(31, unit.Position.X, $"step {steps}: a follower stopped at {unit.Position}, before the wall");
        }
        Assert.IsLessThan(180 * 60, steps, $"every follower ends its order; {units.Count(unit => unit.Position.X > 31)} of 200 crossed");
        foreach (var unit in units) Assert.IsLessThan(10, Vector2.Distance(unit.Position, Goal), "the swarm gathers around the goal: two hundred such bodies fill a disc of radius five");
    }

    [TestMethod]
    public void Slow_followers_on_open_ground_gather_as_closely_as_agents_sent_to_the_point()
    {
        float Farthest(bool follow)
        {
            var grid = new NavGrid(60, 40);
            var (agents, units) = Crowd(grid, 60, 0.3f, 1);
            var field = new DistanceField(grid);
            field.Compute(grid.NodeAt(Goal));
            foreach (var unit in units)
                if (follow) unit.Follow(field); else unit.MoveTo(Goal);
            for (int step = 0; step < 90 * 60 && !units.TrueForAll(unit => unit.Status == NavAgentStatus.Arrived); step++) agents.Step(Dt);
            Assert.IsTrue(units.TrueForAll(unit => unit.Status == NavAgentStatus.Arrived));
            return units.Max(unit => Vector2.Distance(unit.Position, Goal));
        }

        float sent = Farthest(follow: false), followed = Farthest(follow: true);
        Assert.IsLessThan(7, sent, "sixty bodies of radius 0.3 fit in a disc of radius three; they come from one side");
        Assert.IsLessThan(7, followed, "and followers of a field stop as near as those");
    }

    [TestMethod]
    public void A_group_of_wide_bodies_sent_behind_a_closed_wall_all_end_their_order_beside_it()
    {
        foreach (float radius in new[] { 0.8f, 1.2f })
        {
            var grid = Wall(0);
            var (agents, units) = Crowd(grid, 40, radius, 3, columns: 8, spacing: 2.6f);
            foreach (var unit in units) unit.MoveTo(Goal);
            int steps = 0;
            for (; steps < 120 * 60 && !units.TrueForAll(unit => unit.Status == NavAgentStatus.Arrived); steps++) agents.Step(Dt);
            Assert.IsLessThan(120 * 60, steps, $"radius {radius}: {units.Count(unit => unit.Status == NavAgentStatus.Arrived)} of 40 arrived; the rest push for ever");
            Assert.IsTrue(units.TrueForAll(unit => unit.Path == PathStatus.Unreachable));
            Assert.IsFalse(units.Exists(unit => unit.IsBlocked));
        }
    }

    [TestMethod]
    public void Wide_bodies_pass_the_narrowest_gap_they_fit_one_by_one()
    {
        foreach (var (radius, count) in new[] { (1.2f, 40), (1.5f, 10) })
        {
            var grid = Wall(3);
            var (agents, units) = Crowd(grid, count, radius, 3, columns: 8, spacing: 2 * radius + 0.2f);
            foreach (var unit in units) unit.MoveTo(Goal);
            int steps = 0;
            for (; steps < 180 * 60 && !units.TrueForAll(unit => unit.Status == NavAgentStatus.Arrived); steps++) agents.Step(Dt);
            int crossed = units.Count(unit => unit.Position.X > 31);
            Assert.AreEqual(count, crossed, $"radius {radius}: only {crossed} of {count} got through a gap three cells wide");
            Assert.IsLessThan(180 * 60, steps, $"radius {radius}: every one ends its order");
        }
    }

    [TestMethod]
    public void Bodies_too_wide_for_a_gap_all_end_their_order_before_it()
    {
        var grid = Wall(2);
        var (agents, units) = Crowd(grid, 40, 0.8f, 3, columns: 8, spacing: 2.6f);
        foreach (var unit in units) unit.MoveTo(Goal);
        int steps = 0;
        for (; steps < 180 * 60 && !units.TrueForAll(unit => unit.Status == NavAgentStatus.Arrived); steps++) agents.Step(Dt);
        Assert.IsLessThan(180 * 60, steps, $"{units.Count(unit => unit.Status == NavAgentStatus.Arrived)} of 40 ended their order: the others wait for a place beyond the gap");
        Assert.IsTrue(units.TrueForAll(unit => unit.Position.X < 30 && unit.Path == PathStatus.Unreachable));
    }

    [TestMethod]
    public void Two_streams_head_on_in_a_passage_one_cell_wide_squeeze_past_each_other()
    {
        var grid = new NavGrid(60, 40);
        for (int x = 24; x < 36; x++)
            for (int y = 0; y < 40; y++)
                if (y != 20) grid.SetWalkable(x, y, false);
        var agents = new NavAgents(grid);
        var settings = new NavAgentSettings(0.3f, 4);
        var all = new List<(NavAgent Unit, Vector2 To)>();
        for (int i = 0; i < 20; i++)
        {
            Vector2 left = new(10.5f + i % 5 * 2, 16.5f + i / 5 * 2), right = new(40.5f + i % 5 * 2, 16.5f + i / 5 * 2);
            all.Add((agents.Add(left, settings), right));
            all.Add((agents.Add(right, settings), left));
        }
        foreach (var (unit, to) in all) unit.MoveTo(to);
        float worst = 0;
        int steps = 0;
        for (; steps < 180 * 60 && !all.TrueForAll(one => one.Unit.Status == NavAgentStatus.Arrived); steps++)
        {
            agents.Step(Dt);
            worst = MathF.Max(worst, Overlap(agents));
        }
        Console.WriteLine($"two streams of 20 head-on, passage one cell wide: worst overlap {worst:P0}; all arrived after {steps} steps");
        Assert.IsLessThan(180 * 60, steps);
        foreach (var (unit, to) in all) Assert.IsLessThan(0.05f, Vector2.Distance(unit.Position, to));
        Assert.IsLessThan(PressedLimit, worst, "two bodies do not fit side by side in it: they pass by overlapping, by this much at most");
    }

    [TestMethod]
    public void A_crowd_squeezed_through_one_cell_overlaps_by_a_small_part_of_its_radii()
    {
        var grid = Wall(1);
        var (agents, units) = Crowd(grid, 400, 0.3f, 4);
        foreach (var unit in units) unit.MoveTo(Goal);
        float worst = 0;
        int over = 0, longest = 0, run = 0, steps = 0;
        for (; steps < 240 * 60 && !units.TrueForAll(unit => unit.Status == NavAgentStatus.Arrived); steps++)
        {
            agents.Step(Dt);
            float now = Overlap(agents);
            worst = MathF.Max(worst, now);
            run = now > OverlapLimit ? run + 1 : 0;
            if (now > OverlapLimit) over++;
            longest = Math.Max(longest, run);
        }
        Console.WriteLine($"400 agents, one-cell gap: worst overlap {worst:P0} of the two radii; {over} steps over {OverlapLimit:P0}, longest run {longest}; all arrived after {steps} steps");
        Assert.IsLessThan(240 * 60, steps, "the whole crowd gets through and ends its order");
        Assert.IsLessThan(PressedLimit, worst, "pressed at the gap");
        Assert.IsLessThan(PressedSteps, longest, $"and not for long: steps in a row over {OverlapLimit:P0}");
    }

    [TestMethod]
    public void A_step_longer_than_a_body_does_not_put_bodies_on_top_of_each_other()
    {
        var grid = new NavGrid(60, 40);
        var (agents, units) = Crowd(grid, 60, 0.25f, 40);
        foreach (var unit in units) unit.MoveTo(Goal);
        float worst = 0;
        int over = 0;
        for (int step = 0; step < 40 * 60; step++)
        {
            agents.Step(Dt);
            float now = Overlap(agents);
            worst = MathF.Max(worst, now);
            if (now > OverlapLimit) over++;
        }
        Console.WriteLine($"radius 0.25, speed 40 (2.7 radii a step): worst overlap {worst:P0}; {over} steps over {OverlapLimit:P0}");
        Assert.IsTrue(units.TrueForAll(unit => unit.Status == NavAgentStatus.Arrived));
        Assert.IsLessThan(PressedLimit, worst);
        Assert.IsLessThan(PressedSteps, over);
    }

    // What the guide states: bodies pressed together overlap by less than this part of their two radii, and by more
    // than a tenth for fewer steps in a row than this.
    private const float OverlapLimit = 0.10f, PressedLimit = 0.25f;
    private const int PressedSteps = 60;

    [TestMethod]
    public void A_wall_on_cells_the_agent_already_passed_costs_no_search()
    {
        var grid = new NavGrid(40, 30);
        var agents = new NavAgents(grid);
        var agent = agents.Add(new Vector2(2.5f, 2.5f), new NavAgentSettings(0.3f, 4));
        var goal = new Vector2(36.5f, 16.5f);
        agent.MoveTo(goal);
        agents.Step(Dt);
        var finder = new PathFinder();
        finder.Find(grid, grid.NodeAt(new Vector2(2.5f, 2.5f)), grid.NodeAt(goal));
        var path = finder.Path.ToArray();
        for (int step = 0; step < 120; step++) agents.Step(Dt);         // two seconds: eight cells along, off the path's cells
        Assert.AreEqual(1, agents.Searches);

        int behind = 0;
        foreach (int node in path)
        {
            var cell = grid.CellOf(node);
            if (cell.X > 5 || cell.Y > 5 || node == grid.NodeAt(agent.Position)) continue;
            grid.SetWalkable(cell.X, cell.Y, false);
            behind++;
        }
        Assert.IsGreaterThan(2, behind, "cells of its path, behind it");
        agents.Step(Dt);
        Assert.AreEqual(1, agents.Searches, "what closed is behind it: its way on is as open as before");

        // A wall across the leg it walks, on cells its searched path never used: it sees that at once.
        var ahead = Vector2.Lerp(agent.Position, agent.Route[0], 0.5f);       // where the straight leg is furthest from the cells searched
        var across = grid.CellAt(ahead);
        Assert.IsFalse(Array.Exists(path, node => grid.CellOf(node).X == across.X && Math.Abs(grid.CellOf(node).Y - across.Y) <= 1), "the wall is off the searched path");
        for (int y = across.Y - 1; y <= across.Y + 1; y++) grid.SetWalkable(across.X, y, false);
        agents.Step(Dt);
        Assert.AreEqual(2, agents.Searches, "its straight leg is closed: it searches at once instead of walking into the wall");
        for (int step = 0; step < 30 * 60 && agent.Status != NavAgentStatus.Arrived; step++) agents.Step(Dt);
        Assert.AreEqual(goal, agent.Position);
    }

    [TestMethod]
    public void The_state_hash_tells_settings_and_fields_apart()
    {
        var grid = Wall(2);
        NavAgent One(out NavAgents agents)
        {
            agents = new NavAgents(grid);
            return agents.Add(new Vector2(5.5f, 5.5f), new NavAgentSettings(0.3f, 4));
        }

        var a = One(out var first);
        var b = One(out var second);
        Assert.AreEqual(Hash(first), Hash(second));
        b.Settings = new NavAgentSettings(0.45f, 9) { Weight = 5, Collides = false };
        Assert.AreNotEqual(Hash(first), Hash(second), "another body, speed and weight");
        b.Settings = a.Settings with { ArrivalTolerance = 1 };
        Assert.AreNotEqual(Hash(first), Hash(second), "another tolerance");
        b.Settings = a.Settings;
        Assert.AreEqual(Hash(first), Hash(second));

        var north = new DistanceField(grid);
        north.Compute(grid.NodeOf(50, 5));
        var south = new DistanceField(grid);
        south.Compute(grid.NodeOf(50, 35));
        a.Follow(north);
        b.Follow(south);
        Assert.AreNotEqual(Hash(first), Hash(second), "fields with different goals");
        var again = new DistanceField(grid);
        again.Compute(grid.NodeOf(50, 5));
        b.Follow(again);
        Assert.AreEqual(Hash(first), Hash(second), "two fields that hold the same are the same state");
        again.Compute(grid.NodeOf(50, 5), maxDistance: 10);
        Assert.AreNotEqual(Hash(first), Hash(second), "a field that knows less");
    }

    [TestMethod]
    public void No_leg_of_a_route_touches_a_wall_for_bodies_up_to_a_cell_wide()
    {
        var finder = new PathFinder();
        var route = new List<Vector2>();
        int checkedLegs = 0, offCentre = 0;
        for (int seed = 0; seed < 60; seed++)
        {
            var grid = Maps.Random(seed, 24, 18, 0.22, costs: false);
            var random = new Random(seed);
            float radius = new[] { 0.2f, 0.35f, 0.5f }[seed % 3];
            for (int i = 0; i < 40; i++)
            {
                int start = random.Next(grid.NodeCount), goal = random.Next(grid.NodeCount);
                if (!grid.IsWalkable(start % 24, start / 24)) continue;
                // An end point anywhere in its cell, not only at the centre.
                var to = grid.CenterOf(goal) + new Vector2((float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f) * 0.98f;
                var from = grid.CenterOf(start);
                if (grid.FindPath(finder, from, to, route, radius) != PathStatus.Found) continue;
                if (route[^1] != grid.CenterOf(goal)) offCentre++;
                var at = from;
                foreach (var point in route)
                {
                    Assert.IsTrue(grid.HasLineOfSight(at, point, radius), $"seed {seed}, radius {radius}: the leg from {at} to {point} of the route from {from} to {to} touches a wall");
                    at = point;
                    checkedLegs++;
                }
            }
        }
        Assert.IsGreaterThan(1000, checkedLegs);
        Assert.IsGreaterThan(300, offCentre);
    }
}

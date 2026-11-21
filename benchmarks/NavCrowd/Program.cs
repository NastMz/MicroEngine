using System.Diagnostics;
using System.Globalization;
using MicroEngine.Navigation;

// Times a crowd of agents (NAV-2): what a step of NavAgents costs as the number of agents grows.
//
//   NavCrowd [--size N] [--walls COUNT] [--steps S] [--budget NODES]
//
// Builds an N x N grid (128 by default) of 16-unit cells with COUNT single-cell walls (1500) from a fixed seed, puts
// 100, 250, 500, 1000 and 2000 agents of radius 5 on free cells, orders them all to the middle of the map and steps
// the system S times (900) at 60 steps a second. Reports the mean and the worst step after the first six, how many
// agents still waited for their route after six steps (the search budget at work) and the bytes allocated by the
// last third of the run.
int size = 128, walls = 1500, steps = 900, budget = 4096;
for (int i = 0; i < args.Length; i++)
    switch (args[i])
    {
        case "--size": size = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
        case "--walls": walls = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
        case "--steps": steps = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
        case "--budget": budget = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
    }

Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"NavCrowd: {size} x {size} cells, {walls} walls, {steps} steps, {budget} nodes a step"));
foreach (int count in new[] { 100, 250, 500, 1000, 2000 })
{
    var grid = new NavGrid(size, size, 16);
    var random = new Random(3);
    for (int i = 0; i < walls; i++) grid.SetWalkable(1 + random.Next(size - 2), 1 + random.Next(size - 2), false);
    var agents = new NavAgents(grid) { SearchBudget = budget };
    var units = new List<NavAgent>();
    while (units.Count < count)
    {
        int x = random.Next(size), y = random.Next(size);
        if (grid.IsWalkable(x, y)) units.Add(agents.Add(grid.CenterOf(grid.NodeOf(x, y)), new NavAgentSettings(radius: 5, speed: 60)));
    }
    var goal = grid.CenterOf(grid.NodeOf(size / 2, size / 2));
    foreach (var unit in units) unit.MoveTo(goal);

    double worst = 0, total = 0;
    int waiting = 0;
    long allocated = 0, before = 0;
    for (int step = 0; step < steps; step++)
    {
        if (step == steps * 2 / 3) before = GC.GetAllocatedBytesForCurrentThread();
        long started = Stopwatch.GetTimestamp();
        agents.Step(1 / 60f);
        double ms = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        if (step == 5) waiting = units.Count(unit => unit.Status == NavAgentStatus.Planning);
        if (step < 6) continue;
        worst = Math.Max(worst, ms);
        total += ms;
    }
    allocated = GC.GetAllocatedBytesForCurrentThread() - before;
    Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
        $"{count,5} agents: mean {total / (steps - 6):0.000} ms, worst {worst:0.00} ms a step; waiting after 6 steps {waiting}; searches {agents.Searches}; arrived {units.Count(unit => unit.Status == NavAgentStatus.Arrived)}; allocated in the last third {allocated} B"));
}

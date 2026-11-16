using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using MicroEngine.Navigation;
using MicroEngine.World;

// Times pathfinding on grids (NAV-1's measurements):
//
//   NavBench [--size N] [--walls PERCENT] [--queries Q] [--runs R]
//
// Builds an N × N tile map (1000 × 1000 by default) with walls scattered over PERCENT of its cells (20) from a fixed
// seed, reads it into a NavGrid and times: reading the tiles, Q searches (64) between cells far apart, the same with
// the route straightened for an agent with a size, the search for a goal that is walled in (the worst case: every
// reachable cell is looked at) with and without a search budget, and a distance field over the whole map and over a
// radius. Each timing is the median of R runs (5) after warm-up runs; allocations are measured on this thread.
int size = 1000, walls = 20, queries = 64, runs = 5;
for (int i = 0; i < args.Length; i++)
    switch (args[i])
    {
        case "--size": size = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
        case "--walls": walls = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
        case "--queries": queries = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
        case "--runs": runs = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
    }

var random = new Random(12345);
var layer = new TileLayer(size, size);
for (int y = 0; y < size; y++)
    for (int x = 0; x < size; x++)
        if (random.Next(100) < walls) layer[x, y] = 1;
// A walled-in cell near the far corner: the goal no search can reach.
int sealedX = size - 3, sealedY = size - 3;
for (int dy = -1; dy <= 1; dy++)
    for (int dx = -1; dx <= 1; dx++) layer[sealedX + dx, sealedY + dy] = dx == 0 && dy == 0 ? 0 : 1;
var tiles = new TileGrid(layer, 16, new Dictionary<int, TileFlags> { [1] = TileFlags.Solid });

Console.WriteLine($"# NavBench: {size} × {size} cells, {walls}% walls, {queries} queries, .NET {Environment.Version}, {runs} runs (median)");
Console.WriteLine();
Console.WriteLine("| Measurement | Median | Allocated |");
Console.WriteLine("| --- | ---: | ---: |");

NavGrid grid = null!;
Row("Read the tile map into a grid (`NavGrid.FromTiles`)", () => grid = NavGrid.FromTiles(tiles));
Row("Read it again after tiles changed (`ReadTiles`)", () => grid.ReadTiles(tiles));

// Pairs of walkable cells at least half the map apart, joined by a path.
var finder = new PathFinder();
var pairs = new List<(int From, int To)>();
while (pairs.Count < queries)
{
    int a = random.Next(grid.NodeCount), b = random.Next(grid.NodeCount);
    var (ca, cb) = (grid.CellOf(a), grid.CellOf(b));
    if (!grid.IsWalkable(ca.X, ca.Y) || !grid.IsWalkable(cb.X, cb.Y)) continue;
    if (Math.Abs(ca.X - cb.X) + Math.Abs(ca.Y - cb.Y) < size / 2) continue;
    if (finder.Find(grid, a, b) == PathStatus.Found) pairs.Add((a, b));
}

long expanded = 0, steps = 0;
foreach (var (from, to) in pairs) { finder.Find(grid, from, to); expanded += finder.Expanded; steps += finder.Path.Count; }
Row(Text($"A* between cells far apart, each of {queries} (mean {steps / queries:N0} cells long, {expanded / queries:N0} cells looked at)"), () =>
{
    foreach (var (from, to) in pairs) finder.Find(grid, from, to);
}, perItem: queries);

var route = new List<Vector2>(4096);
long legs = 0;
foreach (var (from, to) in pairs) { grid.FindPath(finder, grid.CenterOf(from), grid.CenterOf(to), route, 6); legs += route.Count; }
Row(Text($"The same with the route straightened for a 12-unit agent (`FindPath`, mean {legs / queries:N0} legs)"), () =>
{
    foreach (var (from, to) in pairs) grid.FindPath(finder, grid.CenterOf(from), grid.CenterOf(to), route, 6);
}, perItem: queries);

int start = pairs[0].From, walledIn = grid.NodeOf(sealedX, sealedY);
finder.Find(grid, start, walledIn);
Row(Text($"A* to a goal that is walled in ({finder.Expanded:N0} cells looked at)"), () => finder.Find(grid, start, walledIn));
finder.MaxExpansions = 20_000;
Row("The same with `MaxExpansions = 20000`", () => finder.Find(grid, start, walledIn));
finder.MaxExpansions = int.MaxValue;

var field = new DistanceField(grid);
Row("Distance field over the whole map (`DistanceField.Compute`)", () => field.Compute(start));
Row("Distance field up to 64 cells from the goal", () => field.Compute(start, 64));

int agents = 1000;
var cells = Enumerable.Range(0, agents).Select(_ => random.Next(grid.NodeCount)).ToArray();
field.Compute(start);
Row(Text($"The next cell of {agents:N0} agents from the field (`Next`)"), () =>
{
    foreach (int cell in cells) field.Next(cell);
}, "µs", 1000);

long memory = (long)grid.NodeCount * (sizeof(bool) + sizeof(float));
Console.WriteLine();
Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Memory: grid {memory / (1024.0 * 1024):0.0} MB (a flag and a cost per cell), finder {(long)grid.NodeCount * 24 / (1024.0 * 1024):0.0} MB once it has searched this map, field {(long)grid.NodeCount * 8 / (1024.0 * 1024):0.0} MB."));

static string Text(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);

void Row(string name, Action action, string unit = "ms", double scale = 1, int perItem = 1)
{
    // Warm up until the code is compiled at its final tier: at least three runs and a fifth of a second.
    var warm = Stopwatch.StartNew();
    for (int i = 0; i < 3 || warm.ElapsedMilliseconds < 200; i++) action();
    var times = new List<double>();
    long allocated = 0;
    for (int i = 0; i < runs; i++)
    {
        long before = GC.GetAllocatedBytesForCurrentThread();
        var watch = Stopwatch.StartNew();
        action();
        times.Add(watch.Elapsed.TotalMilliseconds * scale / perItem);
        allocated = GC.GetAllocatedBytesForCurrentThread() - before;
    }
    times.Sort();
    Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"| {name} | {times[times.Count / 2]:0.###} {unit} | {allocated / 1024.0:0} KB |"));
}

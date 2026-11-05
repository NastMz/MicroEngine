using System.Globalization;
using MicroEngine.Physics;
using MicroEngine.Physics.Box2D;
using MicroEngine.Physics.Tiles;
using PhysicsSubstitution;

// The only place that knows the backends: everything else talks to IPhysicsWorld2D.
//   dotnet run --project samples/PhysicsSubstitution [tiles|box2d|both]
static IPhysicsWorld2D Make(string backend) => backend switch
{
    "tiles" => new TilePhysicsWorld2D(),
    // Box2D in the engine's pixel space: 16 pixels (a tile) to the metre, y down.
    "box2d" => new Box2DPhysicsWorld2D(new Box2DWorldOptions { UnitsPerMeter = 16, YDown = true }),
    _ => throw new ArgumentException($"Unknown backend '{backend}': tiles or box2d."),
};

string which = args.Length > 0 ? args[0] : "both";
var backends = which == "both" ? new[] { "tiles", "box2d" } : [which];
var inv = CultureInfo.InvariantCulture;
foreach (var backend in backends)
{
    if (backend == "box2d" && !Box2DPhysicsWorld2D.IsSupported)
    {
        Console.WriteLine("box2d: the native library of this machine is missing (src/MicroEngine.Physics.Box2D/native/build.sh).");
        continue;
    }
    var world = Make(backend);
    var game = new Platformer(world);
    var trace = game.Play();
    Console.WriteLine(string.Create(inv, $"{backend}: {trace.Count} steps, end at ({trace[^1].X:0.###}, {trace[^1].Y:0.###}), " +
        $"coins {game.Collected.Count}/{game.Coins.Count} at steps [{string.Join(", ", game.Collected.Select(c => c.Step))}], goal at step {game.GoalStep}"));
    foreach (var (part, at, move) in game.Parts)
        Console.WriteLine(string.Create(inv, $"  {part,-28} ({at.X,7:0.###}, {at.Y,7:0.###}) ground {move.OnGround,-5} wall {move.HitWall}"));
    try
    {
        var crates = Crates.Run(Make(backend));
        Console.WriteLine(string.Create(inv, $"{backend}: crates at {string.Join(" ", crates.Select(c => string.Create(inv, $"({c.Position.X:0.#}, {c.Position.Y:0.#}, {c.Rotation:0.##})")))}"));
    }
    catch (PhysicsCapabilityException e)
    {
        Console.WriteLine($"{backend}: crates refused: {e.Message}");
    }
    (world as IDisposable)?.Dispose();
}

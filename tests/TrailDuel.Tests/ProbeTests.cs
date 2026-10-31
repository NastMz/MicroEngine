using TrailDuel.Rules;
namespace TrailDuel.Tests;

[TestClass]
public sealed class ProbeTests
{
    [TestMethod]
    public void Probe()
    {
        using var h = new Harness("town");
        h.Step(30);
        h.State.MapId = h.State.MapId;
        Console.WriteLine($"{h.State.MapId} {h.State.X},{h.State.Y}");
        h.Hold("right", 6 * 15);
        h.Hold("up", 50);
        Console.WriteLine($"{h.State.MapId} {h.State.X},{h.State.Y} {h.State.Facing}");
        h.Step(60);
        Console.WriteLine($"{h.State.MapId} {h.State.X},{h.State.Y} {h.State.Facing}");
    }
}

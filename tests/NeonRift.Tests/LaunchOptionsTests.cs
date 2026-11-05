using MicroEngine.Core;
using MicroEngine.Hosting;
using NeonRift.Gameplay;

namespace NeonRift.Tests;

/// <summary>
/// The developer launch options come through the composition's normal path: the host's developer variables
/// (<see cref="HostEnvironment.Variable"/>), which are a phone test's <c>--env</c> values on phones, never straight from
/// the process environment.
/// </summary>
[TestClass]
public sealed class LaunchOptionsTests
{
    private static RiftGame Compose(Dictionary<string, string> variables)
    {
        var environment = new HostEnvironment(HostKind.Test) { Variables = name => variables.GetValueOrDefault(name) };
        var host = GameHost.Create(environment, LaunchRequest.Parse([]), Startup.Configure);
        return (RiftGame)host.CreateGame();
    }

    [TestMethod]
    public void The_composition_reads_every_option_from_the_hosts_variables()
    {
        var game = Compose(new()
        {
            ["RIFT_START"] = "stage", ["RIFT_STAGE"] = "3", ["RIFT_CHECKPOINT"] = "2", ["RIFT_WEAPON"] = "lance",
            ["RIFT_GOD"] = "1", ["RIFT_AUTO"] = "1",
        });
        Assert.AreEqual(new LaunchOptions("stage", 3, 2, WeaponKind.Lance, God: true, Auto: true), game.Launch);
    }

    [TestMethod]
    public void Without_variables_the_game_starts_at_the_title_with_the_defaults()
    {
        var game = Compose([]);
        Assert.AreEqual(new LaunchOptions(), game.Launch);
    }

    [TestMethod]
    public void Unreadable_values_keep_their_defaults()
    {
        var options = LaunchOptions.From(name => name switch { "RIFT_STAGE" => "two", "RIFT_WEAPON" => "laser", "RIFT_GOD" => "yes", _ => null });
        Assert.AreEqual(new LaunchOptions(), options);
    }
}

using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Graphics.Devices;
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

    [TestMethod]
    public void The_presentation_stays_pixel_art_unless_a_comparison_asks_for_another()
    {
        Assert.AreEqual(Presentation.PixelArt, Compose([]).Settings.Presentation, "what players get");
        Assert.AreEqual(Presentation.PixelArt, Compose(new() { ["RIFT_PRESENT"] = "crt" }).Settings.Presentation, "an unknown word keeps the default");

        var sharp = Compose(new() { ["RIFT_PRESENT"] = "sharp" }).Settings.Presentation;
        Assert.AreEqual(ScaleRounding.Fractional, sharp.Rounding);
        Assert.AreEqual(SamplerDescription.LinearClamp, sharp.Upscale);
        Assert.AreEqual(Presentation.AutoPrescale, sharp.Prescale);
        Assert.AreEqual(SamplerDescription.PointClamp, sharp.DefaultSampler, "the art itself stays nearest");

        var smooth = Compose(new() { ["RIFT_PRESENT"] = "smooth" }).Settings.Presentation;
        Assert.AreEqual((ScaleRounding.Fractional, SamplerDescription.LinearClamp, 1), (smooth.Rounding, smooth.Upscale, smooth.Prescale));
        var nearest = Compose(new() { ["RIFT_PRESENT"] = "nearest" }).Settings.Presentation;
        Assert.AreEqual((ScaleRounding.Fractional, SamplerDescription.PointClamp), (nearest.Rounding, nearest.Upscale));
        Assert.AreEqual(ScaleRounding.Integer, Compose(new() { ["RIFT_PRESENT"] = "integer" }).Settings.Presentation.Rounding);
    }
}

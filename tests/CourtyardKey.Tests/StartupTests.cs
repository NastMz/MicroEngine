using MicroEngine.Core;
using MicroEngine.Hosting;

namespace CourtyardKey.Tests;

/// <summary>
/// The composition every build calls (<see cref="Startup.Configure"/>) reads its developer options (a phone test's
/// <c>--env</c>, a page's or a shell's variables) from the host's environment, not from the process.
/// </summary>
[TestClass]
public sealed class StartupTests
{
    private static CourtyardKeyGame Compose(string? start, string saves)
    {
        var variables = new Dictionary<string, string?> { ["COURTYARD_START"] = start, ["COURTYARD_SAVE_DIR"] = saves };
        var environment = new HostEnvironment(HostKind.Test) { Variables = name => variables.GetValueOrDefault(name) };
        return (CourtyardKeyGame)GameHost.Create(environment, new LaunchRequest(), Startup.Configure).CreateGame();
    }

    private static T Started<T>(string? start, Func<CourtyardKeyGame, T> read)
    {
        string saves = Path.Combine(Path.GetTempPath(), "courtyard-startup", Guid.NewGuid().ToString("N"));
        try
        {
            var game = Compose(start, saves);
            game.InitializeForTest(HeadlessPlatform.For(game.Settings));
            game.StepForTest();
            return read(game);
        }
        finally
        {
            if (Directory.Exists(saves)) Directory.Delete(saves, recursive: true);
        }
    }

    [TestMethod]
    public void TheStartComesFromTheHostsVariables() =>
        Assert.AreEqual("overworld:1,1", Started("road", g => g.Play?.Room.Key), "COURTYARD_START=road starts on the road");

    [TestMethod]
    public void WithoutAStartTheGameOpensOnTheTitle() =>
        Assert.IsInstanceOfType<Scenes.TitleScene>(Started(null, g => g.Scenes.Current));

    [TestMethod]
    public void ABlankStartIsNoStart() =>
        Assert.IsInstanceOfType<Scenes.TitleScene>(Started("  ", g => g.Scenes.Current), "a blank variable is unset, as for every host option");
}

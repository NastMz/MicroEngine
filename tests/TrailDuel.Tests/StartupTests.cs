using MicroEngine.Core;
using MicroEngine.Hosting;
using TrailDuel.Scenes;

namespace TrailDuel.Tests;

/// <summary>
/// The developer options reach the game through the host's variables (<c>HostEnvironment</c>), the path every host
/// fills: the process environment on desktop, the page's query in browsers, the launch's <c>env</c> option on phones.
/// </summary>
[TestClass]
public sealed class StartupTests
{
    private static TrailDuelGame Compose(string saves, params (string Name, string Value)[] variables)
    {
        var values = variables.ToDictionary(v => v.Name, v => v.Value);
        values["TRAIL_SAVE_DIR"] = saves;
        var environment = new HostEnvironment(HostKind.Test) { Variables = name => values.GetValueOrDefault(name) };
        var host = GameHost.Create(environment, new LaunchRequest(), Startup.Configure);
        return (TrailDuelGame)host.CreateGame();
    }

    private static string Saves() => Path.Combine(Path.GetTempPath(), "trailduel-startup-" + Guid.NewGuid().ToString("N"));

    [TestMethod]
    public void TheDebugStartIsReadFromTheHostsVariables()
    {
        string saves = Saves();
        try
        {
            var game = Compose(saves, ("TRAIL_DEBUG", "town"));
            game.InitializeForTest(new HeadlessPlatform(), 3);
            game.StepForTest();
            var world = Assert.IsInstanceOfType<OverworldScene>(game.Scenes.Current);
            Assert.AreEqual("sprout", world.Map.Def.Id);
            Assert.AreEqual((11, 9), (world.Player.X, world.Player.Y));
            Assert.AreEqual(saves, game.Saves.Directory, "TRAIL_SAVE_DIR comes the same way");
        }
        finally
        {
            if (Directory.Exists(saves)) Directory.Delete(saves, recursive: true);
        }
    }

    [TestMethod]
    public void TheEvolvingSpeciesIsReadFromTheHostsVariables()
    {
        string saves = Saves();
        try
        {
            var game = Compose(saves, ("TRAIL_DEBUG", "evolve"), ("TRAIL_EVOLVE", "puddlepip"));
            game.InitializeForTest(new HeadlessPlatform(), 3);
            game.StepForTest();
            Assert.IsInstanceOfType<EvolutionScene>(game.Scenes.Current);
            Assert.AreEqual("puddlepip", game.State.Party[0].SpeciesId);
        }
        finally
        {
            if (Directory.Exists(saves)) Directory.Delete(saves, recursive: true);
        }
    }

    [TestMethod]
    public void WithoutVariablesTheGameOpensOnTheTitleScreen()
    {
        string saves = Saves();
        try
        {
            var game = Compose(saves);
            game.InitializeForTest(new HeadlessPlatform(), 3);
            game.StepForTest();
            Assert.IsInstanceOfType<TitleScene>(game.Scenes.Current);
        }
        finally
        {
            if (Directory.Exists(saves)) Directory.Delete(saves, recursive: true);
        }
    }
}

using MeadowRun.Art;
using MeadowRun.Scenes;
using MicroEngine.Core;
using MicroEngine.Hosting;

namespace MeadowRun.Tests;

/// <summary>
/// The developer launch options (MEADOW_START, MEADOW_LEVEL, MEADOW_CHECKPOINT, MEADOW_FORM) come from the host's
/// variables, the ones a phone test passes with <c>--env</c> and a page with its query, not from the process.
/// </summary>
[TestClass]
public sealed class LaunchOptionsTests
{
    private static HostEnvironment Host(Dictionary<string, string> variables) =>
        new(HostKind.Test) { Variables = name => variables.GetValueOrDefault(name) };

    [TestMethod]
    public void OptionsAreReadFromTheHostVariables()
    {
        var options = LaunchOptions.From(Host(new()
        {
            ["MEADOW_START"] = "level",
            ["MEADOW_LEVEL"] = "3",
            ["MEADOW_CHECKPOINT"] = "1",
            ["MEADOW_FORM"] = "EMBER",
        }));
        Assert.AreEqual(new LaunchOptions("level", 3, 1, PipForm.Ember), options);
    }

    [TestMethod]
    public void MissingOrUnreadableOptionsKeepTheirDefaults()
    {
        var options = LaunchOptions.From(Host(new() { ["MEADOW_LEVEL"] = "three", ["MEADOW_FORM"] = "giant" }));
        Assert.AreEqual(new LaunchOptions(), options);
    }

    /// <summary>The composition every build calls passes the host's variables to the game: it starts where they say.</summary>
    [TestMethod]
    public void TheCompositionStartsWhereTheHostVariablesSay()
    {
        string save = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "meadowrun-tests", Guid.NewGuid().ToString("N"))).FullName;
        try
        {
            var host = GameHost.Create(Host(new()
            {
                ["MEADOW_START"] = "level",
                ["MEADOW_LEVEL"] = "2",
                ["MEADOW_FORM"] = "bloom",
                ["MEADOW_SAVE_DIR"] = save,
            }), new LaunchRequest(), Startup.Configure);
            var game = (MeadowGame)host.CreateGame();
            game.InitializeForTest(new HeadlessPlatform());
            game.StepForTest();
            var play = game.Scenes.Current as PlayScene;
            Assert.IsNotNull(play, $"started in {game.Scenes.Current?.GetType().Name}");
            Assert.AreEqual(1, play.LevelIndex);
            Assert.AreEqual(PipForm.Bloom, play.Stage.Player.Form);
            Assert.AreEqual(save, game.Store.Directory);
        }
        finally
        {
            Directory.Delete(save, recursive: true);
        }
    }
}

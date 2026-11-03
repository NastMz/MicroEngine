using MicroEngine.Core;

namespace MicroEngine.Tests;

[TestClass]
public sealed class GameLauncherTests
{
    [TestMethod]
    public void Report_is_off_unless_asked()
    {
        Assert.IsFalse(LaunchArguments.Parse([]).Report);
        Assert.IsTrue(LaunchArguments.Parse(["--report", "--mute"]).Report);
    }

    [TestMethod]
    public void Report_names_the_platform_and_whether_audio_is_available()
    {
        using var platform = new HeadlessPlatform();
        CollectionAssert.AreEqual(new[] { "platform: headless", "audio: unavailable" }, GameLauncher.Report(HeadlessPlatform.Profile, platform).ToArray());
    }

    [TestMethod]
    public void Report_adds_what_a_platform_says_about_itself()
    {
        using var platform = new DescribedPlatform();
        CollectionAssert.AreEqual(new[] { "platform: headless (probe renderer)", "audio: unavailable" }, GameLauncher.Report(HeadlessPlatform.Profile, platform).ToArray());
    }

    [TestMethod]
    public void Existing_arguments_parse_as_before()
    {
        var parsed = LaunchArguments.Parse(["--hidden", "--mute", "--frames", "5", "--platform", "headless"]);
        Assert.IsTrue(parsed.Hidden);
        Assert.IsTrue(parsed.Mute);
        Assert.AreEqual(5, parsed.Frames);
        Assert.AreEqual("headless", parsed.Platform);
        Assert.IsFalse(parsed.Report);
        Assert.ThrowsExactly<FormatException>(() => LaunchArguments.Parse(["--reports"]));
    }

    [TestMethod]
    public void A_platform_whose_native_library_cannot_load_is_an_error_not_a_crash()
    {
        var catalog = new PlatformCatalog().Add(new PlatformProfile("native", "a library that cannot load",
            _ => throw new DllNotFoundException("version `GLIBC_2.99' not found (required by libSDL3.so)")));
        Assert.AreEqual(1, GameLauncher.Run(new EmptyGame(), ["--frames", "1"], catalog));
    }

    private sealed class EmptyGame : Game
    {
        public override GameSettings Settings { get; } = new("Empty", 8, 8);
        protected override void Load() { }
    }

    private sealed class DescribedPlatform : TestPlatform
    {
        public override string ToString() => "probe renderer";
    }
}

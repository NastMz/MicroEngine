using System.Reflection;
using MicroEngine.Core;

// This test assembly declares an identity the way the MicroEngine build targets write one into a game.
[assembly: AssemblyMetadata("MicroEngine.ApplicationTitle", "Identity Test")]
[assembly: AssemblyMetadata("MicroEngine.ApplicationId", "com.example.identitytest")]
[assembly: AssemblyMetadata("MicroEngine.Version", "2.1.0")]
[assembly: AssemblyMetadata("MicroEngine.Orientation", "portrait")]

namespace MicroEngine.Tests;

[TestClass]
public sealed class AppIdentityTests
{
    [TestMethod]
    public void The_identity_written_by_the_build_is_read_from_the_game_assembly()
    {
        var identity = AppIdentity.Of(new EmptyGame());
        Assert.IsNotNull(identity);
        Assert.AreEqual("Identity Test", identity.Title);
        Assert.AreEqual("com.example.identitytest", identity.Id);
        Assert.AreEqual("2.1.0", identity.Version);
        Assert.AreEqual(AppOrientation.Portrait, identity.Orientation);
        Assert.IsNull(identity.Icon);
    }

    [TestMethod]
    public void An_assembly_without_an_identity_has_none() => Assert.IsNull(AppIdentity.Of(typeof(object).Assembly));

    [TestMethod]
    public void The_launcher_hands_the_identity_to_the_platform()
    {
        PlatformRequest? seen = null;
        var catalog = new PlatformCatalog().Add(new PlatformProfile("probe", "records the request", request =>
        {
            seen = request;
            return new HeadlessPlatform();
        }));
        GameLauncher.Run(new EmptyGame(), ["--frames", "1"], catalog);
        Assert.AreEqual("Identity Test", seen?.Identity?.Title);
    }

    private sealed class EmptyGame : Game
    {
        public override GameSettings Settings { get; } = new("Empty", 8, 8);
        protected override void Load() { }
    }
}

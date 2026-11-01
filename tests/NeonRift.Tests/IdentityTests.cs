using MicroEngine.Core;

namespace NeonRift.Tests;

/// <summary>The demo's identity comes from its project file through the engine's build targets.</summary>
[TestClass]
public sealed class IdentityTests
{
    [TestMethod]
    public void The_game_carries_its_name_id_and_icon()
    {
        var identity = AppIdentity.Of(typeof(RiftGame).Assembly);
        Assert.IsNotNull(identity);
        Assert.AreEqual("Neon Rift", identity.Title);
        Assert.AreEqual("dev.microengine.neonrift", identity.Id);
        Assert.AreEqual(AppOrientation.Landscape, identity.Orientation);
        Assert.AreEqual(1024, identity.Icon?.Width);
        Assert.AreEqual(1024, identity.Icon?.Height);
    }
}

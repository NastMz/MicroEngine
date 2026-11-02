using MeadowRun.Scenes;

namespace MeadowRun.Tests;

/// <summary>HUD numbers are formatted once per change, not once per frame.</summary>
[TestClass]
public sealed class HudTextTests
{
    [TestMethod]
    public void LabelsFormatLikeTheHudAndReuseTheTextWhileTheValueHolds()
    {
        var lives = new NumberLabel("×", 2);
        string first = lives.For(3);
        Assert.AreEqual("×03", first);
        Assert.AreSame(first, lives.For(3), "an unchanged value reuses its text");
        Assert.AreEqual("×04", lives.For(4));
        Assert.AreEqual("0001250", new NumberLabel(digits: 7).For(1250));
        Assert.AreEqual("0", new NumberLabel().For(0), "the first value is formatted even when it is the default");
    }
}

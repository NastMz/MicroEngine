using NeonRift.Scenes;

namespace NeonRift.Tests;

[TestClass]
public sealed class NumberTextTests
{
    [TestMethod]
    public void Formats_again_only_when_a_number_changes()
    {
        int calls = 0;
        var text = new NumberText((a, b) => { calls++; return $"{a}:{b}"; });
        Assert.AreEqual("1200:3", text.Get(1200, 3));
        Assert.AreSame(text.Get(1200, 3), text.Get(1200, 3), "Same numbers reuse the same string.");
        Assert.AreEqual(1, calls);
        Assert.AreEqual("1200:2", text.Get(1200, 2));
        Assert.AreEqual("1300:2", text.Get(1300, 2));
        Assert.AreEqual(3, calls);
    }
}

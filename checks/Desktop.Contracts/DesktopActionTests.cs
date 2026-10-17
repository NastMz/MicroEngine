using MicroEngine.Desktop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Raylib_cs;

namespace Desktop.Contracts;

[TestClass]
public sealed class DesktopActionTests
{
    [TestMethod]
    [DataRow(KeyboardKey.Left, "left")]
    [DataRow(KeyboardKey.A, "left")]
    [DataRow(KeyboardKey.Right, "right")]
    [DataRow(KeyboardKey.D, "right")]
    [DataRow(KeyboardKey.Space, "jump")]
    [DataRow(KeyboardKey.Up, "up")]
    [DataRow(KeyboardKey.W, "up")]
    [DataRow(KeyboardKey.Down, "down")]
    [DataRow(KeyboardKey.S, "down")]
    public void PhysicalAliasesProduceOneAction(KeyboardKey key, string action)
    {
        var result = DesktopActions.Read(k => k == key, k => k == key);
        CollectionAssert.AreEqual(new[] { action }, result.Held);
        CollectionAssert.AreEqual(new[] { action }, result.Pressed);
    }

    [TestMethod]
    public void AliasCoalescingAndLegacyFirstOrderAreStable()
    {
        var result = DesktopActions.Read(_ => true, _ => true);
        CollectionAssert.AreEqual(new[] { "left", "right", "jump", "up", "down" }, result.Held);
        CollectionAssert.AreEqual(result.Held, result.Pressed);
        result = DesktopActions.Read(k => k == KeyboardKey.Left || k == KeyboardKey.Space, _ => false);
        CollectionAssert.AreEqual(new[] { "left", "jump" }, result.Held); Assert.AreEqual(0, result.Pressed.Length);
    }

    [TestMethod]
    public void UnknownKeyAndEmptyQueriesDoNotEmitOrQueryTwice()
    {
        var calls = new Dictionary<(KeyboardKey, bool), int>();
        bool Query(KeyboardKey key, bool edge)
        {
            calls[(key, edge)] = calls.GetValueOrDefault((key, edge)) + 1;
            return key == KeyboardKey.F1;
        }
        var result = DesktopActions.Read(k => Query(k, false), k => Query(k, true));
        Assert.AreEqual(0, result.Held.Length); Assert.AreEqual(0, result.Pressed.Length);
        Assert.AreEqual(18, calls.Count); Assert.IsTrue(calls.Values.All(count => count == 1));
    }
}

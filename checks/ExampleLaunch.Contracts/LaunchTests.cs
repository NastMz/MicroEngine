using MicroEngine.Examples;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExampleLaunch.Contracts;

[TestClass]
public sealed class LaunchTests
{
    [TestMethod]
    public void EmptyArgumentsKeepInteractiveDefaults()
    {
        Assert.IsTrue(LaunchOptions.TryParse([], out var options));
        Assert.AreEqual(new LaunchOptions(0, false), options);
    }

    [TestMethod]
    public void SmokeBoundariesAndExistingIntegerSyntaxArePreserved()
    {
        foreach (var (text, limit) in new[] { ("1", 1), ("600", 600), (" +20 ", 20), ("020", 20) })
        {
            Assert.IsTrue(LaunchOptions.TryParse(["--smoke", text], out var options));
            Assert.AreEqual(new LaunchOptions(limit, false), options);
        }
    }

    [TestMethod]
    public void FailureFlagWorksAloneAndInBothOrders()
    {
        Assert.IsTrue(LaunchOptions.TryParse(["--fail-update"], out var failure));
        Assert.AreEqual(new LaunchOptions(0, true), failure);
        foreach (var args in new[] { new[] { "--smoke", "20", "--fail-update" }, new[] { "--fail-update", "--smoke", "20" } })
        {
            Assert.IsTrue(LaunchOptions.TryParse(args, out var options));
            Assert.AreEqual(new LaunchOptions(20, true), options);
        }
    }

    [TestMethod]
    public void InvalidArgumentsAreRejectedWithAtomicDefaultOutput()
    {
        string[][] cases = [
            ["--unknown"], ["--Smoke", "20"], ["--FAIL-UPDATE"], ["--smoke"],
            ["--smoke", "0"], ["--smoke", "601"], ["--smoke", "-1"],
            ["--smoke", "x"], ["--smoke", "1.5"], ["--smoke", "2147483648"],
            ["--smoke", "20", "--smoke", "20"], ["--fail-update", "--fail-update"],
            ["--fail-update", "--smoke", "0"], ["--smoke", "20", "unexpected"], ["--smoke", ""], [""]
        ];
        foreach (var args in cases)
        {
            Assert.IsFalse(LaunchOptions.TryParse(args, out var options), string.Join(' ', args));
            Assert.AreEqual(default(LaunchOptions), options);
        }
    }

    [TestMethod]
    public void NullArgumentsAreApiMisuse()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => LaunchOptions.TryParse(null!, out _));
    }
}

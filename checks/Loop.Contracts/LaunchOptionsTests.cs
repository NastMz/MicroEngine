using Microsoft.VisualStudio.TestTools.UnitTesting;
using TurnExplorer;

namespace Loop.Contracts;

[TestClass]
public sealed class LaunchOptionsTests
{
    [TestMethod]
    public void ExistingFlagsRetainPreviousGrammar()
    {
        Assert.IsTrue(LaunchOptions.TryParse([], out var empty));
        Assert.AreEqual(new LaunchOptions(0, false, null, null), empty);
        Assert.IsTrue(LaunchOptions.TryParse(["--fail-update", "--smoke", "600"], out var valid));
        Assert.AreEqual(new LaunchOptions(600, true, null, null), valid);
        foreach (var args in new[] { new[] { "--unknown" }, ["--smoke", "0"], ["--smoke", "NaN"],
            ["--smoke", "601"], ["--smoke", "1", "--smoke", "2"], ["--smoke"], ["--fail-update", "--fail-update"] })
        {
            Assert.IsFalse(LaunchOptions.TryParse(args, out var invalid));
            Assert.IsNull(invalid);
        }
    }

    [TestMethod]
    public void OptionalPathsComposeInEitherOrderWithoutMutatingArguments()
    {
        string[] args = ["--save-new", "new checkpoint.bin", "--smoke", "1", "--load", "old checkpoint.bin", "--fail-update"];
        var before = (string[])args.Clone();
        Assert.IsTrue(LaunchOptions.TryParse(args, out var options));
        Assert.AreEqual(new LaunchOptions(1, true, "old checkpoint.bin", "new checkpoint.bin"), options);
        CollectionAssert.AreEqual(before, args);
        Assert.IsTrue(LaunchOptions.TryParse(["--load", "same", "--save-new", "same"], out var same));
        Assert.AreEqual(new LaunchOptions(0, false, "same", "same"), same);
        Assert.IsTrue(LaunchOptions.TryParse(["--save-new", "relative"], out var save));
        Assert.AreEqual("relative", save!.SaveNewPath);
    }

    [TestMethod]
    public void MissingBlankDuplicateAndFlagValuesFailWithNoOptions()
    {
        foreach (var flag in new[] { "--load", "--save-new" })
        foreach (var args in new[] { new[] { flag }, [flag, ""], [flag, "   "], [flag, "--smoke", "1"], [flag, "a", flag, "b"] })
        {
            Assert.IsFalse(LaunchOptions.TryParse(args, out var invalid));
            Assert.IsNull(invalid);
        }
        Assert.ThrowsExactly<ArgumentNullException>(() => LaunchOptions.TryParse(null!, out _));
    }
}

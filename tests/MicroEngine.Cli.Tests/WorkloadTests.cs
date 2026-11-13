namespace MicroEngine.Cli.Tests;

/// <summary>Which workload the tool names for browser packages, given the .NET SDK that builds the game.</summary>
[TestClass]
public sealed class WorkloadTests
{
    [TestMethod]
    [DataRow("10.0.404", "wasm-tools")]
    [DataRow("11.0.100", "wasm-tools-net10")]
    [DataRow("12.0.100-preview.1", "wasm-tools-net10")]
    public void A_newer_SDK_builds_a_net10_game_for_browsers_with_the_net10_workload(string sdk, string workload) =>
        Assert.AreEqual(workload, Doctor.WebWorkload(Doctor.Major(sdk)));

    [TestMethod]
    public void A_version_that_is_not_one_has_major_zero() => Assert.AreEqual(0, Doctor.Major(""));
}

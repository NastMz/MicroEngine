using MicroEngine.Hosting;

namespace MicroEngine.Cli.Tests;

/// <summary>
/// The phone hosts' launch (Hosts/mobile/MobileLaunch.cs, compiled into the Android and iOS hosts and linked here): a
/// test's <c>--env</c> pairs reach the game as its host's developer variables, never as the process environment.
/// </summary>
[TestClass]
public sealed class MobileLaunchTests
{
    [TestMethod]
    public void The_launch_pairs_are_the_hosts_variables_and_the_process_environment_is_untouched()
    {
        string name = "MICROENGINE_TEST_MOBILE_" + Guid.NewGuid().ToString("N");
        var launch = new MobileLaunch(null, null, MobileLaunch.ParseEnvironment($"{name}=road; MEADOW_FORM=ember"), "captures");

        var host = launch.CreateEnvironment(HostKind.Android);

        Assert.AreEqual(HostKind.Android, host.Kind);
        Assert.AreEqual("road", host.Variable(name));
        Assert.AreEqual("ember", host.Variable("MEADOW_FORM"));
        Assert.IsNull(Environment.GetEnvironmentVariable(name), "the process environment is not written");
    }

    [TestMethod]
    public void A_name_the_launch_does_not_set_is_read_from_the_process()
    {
        string name = "MICROENGINE_TEST_MOBILE_" + Guid.NewGuid().ToString("N");
        Environment.SetEnvironmentVariable(name, "from-process");
        try
        {
            var host = new MobileLaunch(null, null, MobileLaunch.ParseEnvironment(""), "captures").CreateEnvironment(HostKind.IOS);
            Assert.AreEqual(HostKind.IOS, host.Kind);
            Assert.AreEqual("from-process", host.Variable(name));
            Assert.IsNull(host.Variable("MICROENGINE_TEST_MOBILE_UNSET_" + Guid.NewGuid().ToString("N")));
        }
        finally { Environment.SetEnvironmentVariable(name, null); }
    }

    [TestMethod]
    public void Pairs_parse_as_before()
    {
        var pairs = MobileLaunch.ParseEnvironment("A=1;B=x=y; ;C=");
        CollectionAssert.AreEquivalent(new[] { "A", "B", "C" }, pairs.Keys.ToArray());
        Assert.AreEqual("x=y", pairs["B"]);
        Assert.AreEqual("", pairs["C"]);
        Assert.AreEqual(0, MobileLaunch.ParseEnvironment(null).Count);
        Assert.ThrowsExactly<FormatException>(() => MobileLaunch.ParseEnvironment("=1"));
    }
}

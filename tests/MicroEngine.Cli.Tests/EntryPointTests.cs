using MicroEngine.Hosting;

namespace MicroEngine.Cli.Tests;

/// <summary>An entry point the tool must find: public, static, void, one HostBuilder.</summary>
public static class GoodEntry
{
    public static void Configure(HostBuilder host) { }
}

/// <summary>An entry point on a public nested type.</summary>
public static class Outer
{
    public static class Inner
    {
        public static void Configure(HostBuilder host) { }
    }

    internal static class Hidden
    {
        public static void Configure(HostBuilder host) { }
    }
}

/// <summary>Methods named Configure that are not entry points.</summary>
public sealed class NotEntries
{
    public void Configure(HostBuilder host) { }
    public static int Configure(HostBuilder host, int extra) => extra;
    public static void Configure(object host) { }
    internal static void Configure(string host) { }
}

internal static class InternalEntry
{
    public static void Configure(HostBuilder host) { }
}

[TestClass]
public sealed class EntryPointTests
{
    [TestMethod]
    public void Finds_the_public_static_Configure_methods_that_take_a_HostBuilder()
    {
        var found = EntryPoint.Find(typeof(GoodEntry).Assembly.Location);
        CollectionAssert.AreEquivalent(new[] { "global::MicroEngine.Cli.Tests.GoodEntry.Configure", "global::MicroEngine.Cli.Tests.Outer.Inner.Configure" }, found.ToArray(),
            string.Join(", ", found));
    }

    [TestMethod]
    public void Finds_a_demos_entry_point_in_its_built_assembly()
    {
        // The demo's Program.cs hands Startup.Configure to the desktop launcher; the browser and phone hosts call the same.
        var found = EntryPoint.Find(typeof(CourtyardKey.Startup).Assembly.Location);
        CollectionAssert.AreEqual(new[] { "global::CourtyardKey.Startup.Configure" }, found.ToArray());
    }
}

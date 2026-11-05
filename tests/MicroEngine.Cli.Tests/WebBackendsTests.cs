namespace MicroEngine.Cli.Tests;

/// <summary>The platform a web package runs on (<c>microengine package web --web-backend</c>).</summary>
[TestClass]
public sealed class WebBackendsTests
{
    [TestMethod]
    public void No_name_is_the_default_backend() => Assert.AreEqual(WebBackends.Default, WebBackends.Parse(null));

    [TestMethod]
    public void Names_are_read_ignoring_case()
    {
        Assert.AreEqual(WebBackends.Sdl, WebBackends.Parse("SDL"));
        Assert.AreEqual(WebBackends.Raylib, WebBackends.Parse("raylib"));
    }

    [TestMethod]
    public void An_unknown_name_is_a_usage_error_that_lists_the_backends()
    {
        var e = Assert.ThrowsExactly<CliException>(() => WebBackends.Parse("glfw"));
        Assert.AreEqual(2, e.ExitCode);
        Assert.AreEqual("unknown web backend 'glfw'. Web backends: sdl, raylib.", e.Message);
    }

    [TestMethod]
    public void A_package_request_runs_on_the_default_backend_unless_it_names_one()
    {
        Assert.AreEqual(WebBackends.Default, new PackageRequest(null!, PackageTarget.Find("web"), "out").WebBackend);
        Assert.AreEqual(WebBackends.Sdl, new PackageRequest(null!, PackageTarget.Find("web"), "out") { WebBackend = WebBackends.Sdl }.WebBackend);
    }
}

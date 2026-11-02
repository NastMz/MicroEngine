using MicroEngine.Core;

namespace MicroEngine.Tests;

/// <summary>PERF-1: which platform a launch gets when one cannot start, and the GPU preference option.</summary>
[TestClass]
[DoNotParallelize] // redirects the console
public sealed class PlatformSelectionTests
{
    private sealed class EmptyGame : Game
    {
        public override GameSettings Settings { get; } = new("Empty", 8, 8);
        protected override void Load() => Scenes.Push(new Idle());
        private sealed class Idle : MicroEngine.Scenes.Scene
        {
            public override void Update(float dt) { }
            public override void Draw(MicroEngine.Graphics.IRenderer renderer) { }
        }
    }

    private static PlatformProfile Broken(string name) =>
        new(name, "cannot start here", _ => throw new InvalidOperationException($"{name} has no display"));

    private static PlatformProfile Working(string name, List<string> started) =>
        new(name, "starts", request => { started.Add(name); return new HeadlessPlatform(); });

    private static (int Code, string Errors) Run(PlatformCatalog catalog, params string[] args)
    {
        var errors = new StringWriter();
        var previousError = Console.Error;
        var previousOut = Console.Out;
        Console.SetError(errors);
        Console.SetOut(new StringWriter());
        try { return (GameLauncher.Run(new EmptyGame(), args, catalog), errors.ToString()); }
        finally
        {
            Console.SetError(previousError);
            Console.SetOut(previousOut);
        }
    }

    [TestMethod]
    public void Without_a_named_platform_the_next_one_starts_and_the_failure_is_reported()
    {
        var started = new List<string>();
        var catalog = new PlatformCatalog().Add(Broken("first")).Add(Working("second", started)).Add(Working("third", started));
        var (code, errors) = Run(catalog, "--frames", "2");
        Assert.AreEqual(0, code, errors);
        CollectionAssert.AreEqual(new[] { "second" }, started);
        StringAssert.Contains(errors, "platform: first could not start (first has no display); trying second");
    }

    [TestMethod]
    public void A_named_platform_that_cannot_start_is_an_error_without_fallback()
    {
        var started = new List<string>();
        var catalog = new PlatformCatalog().Add(Broken("first")).Add(Working("second", started));
        var (code, errors) = Run(catalog, "--platform", "first", "--frames", "2");
        Assert.AreEqual(1, code);
        Assert.AreEqual(0, started.Count);
        StringAssert.Contains(errors, "error: first has no display");
    }

    [TestMethod]
    public void When_every_platform_fails_the_last_error_is_reported()
    {
        var catalog = new PlatformCatalog().Add(Broken("first")).Add(new PlatformProfile("native", "no library",
            _ => throw new DllNotFoundException("libSDL3.so not found")));
        var (code, errors) = Run(catalog, "--frames", "2");
        Assert.AreEqual(1, code);
        StringAssert.Contains(errors, "trying native");
        StringAssert.Contains(errors, "error: platform 'native' could not load its native library. libSDL3.so not found");
    }

    [TestMethod]
    public void Fallbacks_skip_headless_and_unsupported_platforms()
    {
        var started = new List<string>();
        var catalog = new PlatformCatalog()
            .Add(new PlatformProfile("phone-only", "", _ => new HeadlessPlatform()) { CheckSupport = () => "not here" })
            .Add(Working("window", started))
            .Add(HeadlessPlatform.Profile);
        CollectionAssert.AreEqual(new[] { "window" }, GameLauncher.Fallbacks(catalog).Select(p => p.Name).ToArray());
        var headlessOnly = new PlatformCatalog().Add(HeadlessPlatform.Profile);
        CollectionAssert.AreEqual(new[] { "headless" }, GameLauncher.Fallbacks(headlessOnly).Select(p => p.Name).ToArray(),
            "a catalog with nothing else keeps its default");
    }

    [TestMethod]
    public void Gpu_preference_reaches_the_platform_request()
    {
        Assert.AreEqual(GpuPreference.Automatic, LaunchArguments.Parse([]).Gpu);
        Assert.AreEqual(GpuPreference.HighPerformance, LaunchArguments.Parse(["--gpu", "high-performance"]).Gpu);
        Assert.AreEqual(GpuPreference.PowerSaving, LaunchArguments.Parse(["--gpu", "power-saving"]).Gpu);
        Assert.ThrowsExactly<FormatException>(() => LaunchArguments.Parse(["--gpu", "nvidia"]));

        PlatformRequest? seen = null;
        var catalog = new PlatformCatalog().Add(new PlatformProfile("probe", "", request => { seen = request; return new HeadlessPlatform(); }));
        var (code, errors) = Run(catalog, "--gpu", "power-saving", "--frames", "1");
        Assert.AreEqual(0, code, errors);
        Assert.AreEqual(GpuPreference.PowerSaving, seen?.GpuPreference);
    }
}

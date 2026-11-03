using MicroEngine.Kernel;

namespace MicroEngine.Tests.Kernel;

/// <summary>The builder, the typed service registry, modules, settings and capabilities.</summary>
[TestClass]
public sealed class CompositionTests
{
    private interface IGreeter { string Greet(); }
    private sealed class English : IGreeter { public string Greet() => "hello"; }
    private sealed class Spanish : IGreeter { public string Greet() => "hola"; }

    private sealed class GreeterModule(IGreeter greeter) : IEngineModule
    {
        public int Configured { get; private set; }
        public void Configure(EngineBuilder builder)
        {
            Configured++;
            builder.Services.Add(greeter);
            builder.Schedule.Add(FramePhase.Update, "greeter.update", _ => { });
        }
    }

    private sealed record AudioSettings
    {
        public float Volume { get; init; } = 1f;
        public int Voices { get; init; } = 32;
    }

    [TestMethod]
    public void Services_are_found_by_the_type_they_were_registered_as()
    {
        var services = new ServiceRegistry();
        var greeter = new English();
        services.Add<IGreeter>(greeter);
        Assert.AreSame(greeter, services.Get<IGreeter>());
        Assert.IsTrue(services.TryGet<IGreeter>(out var found));
        Assert.AreSame(greeter, found);
        Assert.IsFalse(services.TryGet<English>(out _), "registered as the interface, not as its class");
        Assert.IsTrue(services.Contains<IGreeter>());
        Assert.AreEqual(1, services.Count);
    }

    [TestMethod]
    public void Adding_a_second_service_of_a_type_fails_and_replace_swaps_it()
    {
        var services = new ServiceRegistry();
        var english = new English();
        services.Add<IGreeter>(english);
        var error = Assert.ThrowsExactly<InvalidOperationException>(() => services.Add<IGreeter>(new Spanish()));
        StringAssert.Contains(error.Message, "IGreeter");
        StringAssert.Contains(error.Message, "Replace");
        Assert.AreSame(english, services.Get<IGreeter>(), "the failed add changed nothing");
        var spanish = new Spanish();
        Assert.AreSame(english, services.Replace<IGreeter>(spanish));
        Assert.AreEqual("hola", services.Get<IGreeter>().Greet());
        Assert.IsNull(new ServiceRegistry().Replace<IGreeter>(spanish), "replacing nothing returns null");
    }

    [TestMethod]
    public void A_missing_service_fails_with_its_name_and_try_get_says_no()
    {
        var services = new ServiceRegistry();
        var error = Assert.ThrowsExactly<InvalidOperationException>(() => services.Get<IGreeter>());
        StringAssert.Contains(error.Message, "No IGreeter service is registered");
        Assert.IsFalse(services.TryGet<IGreeter>(out var none));
        Assert.IsNull(none);
        services.Add<IGreeter>(new English());
        Assert.IsTrue(services.Remove<IGreeter>());
        Assert.IsFalse(services.Remove<IGreeter>());
        Assert.ThrowsExactly<ArgumentNullException>(() => services.Add<IGreeter>(null!));
    }

    [TestMethod]
    public void Modules_configure_when_added_and_once_per_type()
    {
        var module = new GreeterModule(new English());
        var builder = new EngineBuilder().Add(module);
        Assert.AreEqual(1, module.Configured);
        Assert.IsTrue(builder.HasModule<GreeterModule>());
        Assert.IsTrue(builder.Schedule.Contains("greeter.update"));
        var error = Assert.ThrowsExactly<InvalidOperationException>(() => builder.Add(new GreeterModule(new Spanish())));
        StringAssert.Contains(error.Message, "GreeterModule");
        var engine = builder.Build();
        Assert.AreEqual("hello", engine.Services.Get<IGreeter>().Greet());
    }

    [TestMethod]
    public void Settings_start_from_their_defaults_and_each_edit_sees_the_previous_one()
    {
        var builder = new EngineBuilder();
        Assert.AreEqual(new AudioSettings(), builder.Settings<AudioSettings>());
        builder.Configure<AudioSettings>(s => s with { Volume = 0.5f })
               .Configure<AudioSettings>(s => s with { Voices = s.Voices * 2 });
        Assert.AreEqual(new AudioSettings { Volume = 0.5f, Voices = 64 }, builder.Build().Services.Get<AudioSettings>());
        Assert.ThrowsExactly<InvalidOperationException>(() => new EngineBuilder().Configure<AudioSettings>(_ => null!));
    }

    [TestMethod]
    public void Build_registers_the_defaults_nobody_registered_and_happens_once()
    {
        var builder = new EngineBuilder();
        var engine = builder.Build();
        Assert.AreEqual(new TimeSettings(), engine.Time);
        Assert.AreSame(engine.Time, engine.Services.Get<TimeSettings>());
        Assert.IsInstanceOfType<ConsoleLog>(engine.Log);
        Assert.AreSame(engine.Lifecycle, engine.Services.Get<IEngineLifecycle>());
        Assert.AreSame(engine.Clock, engine.Services.Get<FrameClock>());
        Assert.AreSame(engine, engine.Services.Get<Engine>());
        Assert.IsTrue(engine.Schedule.IsFrozen);
        Assert.ThrowsExactly<InvalidOperationException>(() => builder.Build());
        Assert.ThrowsExactly<InvalidOperationException>(() => builder.Add(new GreeterModule(new English())));
        Assert.ThrowsExactly<InvalidOperationException>(() => builder.Configure<TimeSettings>(t => t));
    }

    [TestMethod]
    public void Registered_log_and_time_settings_are_kept()
    {
        var log = new ConsoleLog(new StringWriter());
        var builder = new EngineBuilder().Configure<TimeSettings>(t => t with { FixedRate = 120, Mode = TimeMode.Deterministic });
        builder.Services.Add<ILog>(log);
        var engine = builder.Build();
        Assert.AreSame(log, engine.Log);
        Assert.AreEqual(120, engine.Clock.Settings.FixedRate);
        Assert.AreEqual(TimeMode.Deterministic, engine.Clock.Mode);
    }

    [TestMethod]
    public void Invalid_time_settings_fail_the_build_naming_the_setting()
    {
        foreach (var (settings, name) in new[]
        {
            (new TimeSettings { FixedRate = 0 }, "FixedRate"),
            (new TimeSettings { MaxStepsPerFrame = 0 }, "MaxStepsPerFrame"),
            (new TimeSettings { MaxFrameDelta = 0 }, "MaxFrameDelta"),
            (new TimeSettings { MaxFrameDelta = double.NaN }, "MaxFrameDelta"),
        })
        {
            var builder = new EngineBuilder().Configure<TimeSettings>(_ => settings);
            var error = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => builder.Build());
            Assert.AreEqual(name, error.ParamName);
        }
    }

    [TestMethod]
    public void Capabilities_are_queried_by_type_never_cast()
    {
        var builder = new EngineBuilder();
        var greeter = new English();
        builder.Capabilities.Add<IGreeter>(greeter);
        var engine = builder.Build();
        Assert.IsTrue(engine.TryGetCapability<IGreeter>(out var found));
        Assert.AreSame(greeter, found);
        Assert.IsFalse(engine.TryGetCapability<English>(out _));
        Assert.ThrowsExactly<InvalidOperationException>(() => engine.Capabilities.Add<IGreeter>(new Spanish()));
        engine.Capabilities.Replace<IGreeter>(new Spanish());
        Assert.IsTrue(engine.Capabilities.Has<IGreeter>());
        Assert.IsTrue(engine.Capabilities.Remove<IGreeter>());
        Assert.IsFalse(engine.TryGetCapability<IGreeter>(out _));
    }

    [TestMethod]
    [DoNotParallelize] // redirects the console
    public void Console_log_writes_category_and_message_above_its_level()
    {
        var text = new StringWriter();
        var log = new ConsoleLog(text) { MinimumLevel = LogLevel.Warning };
        log.Write(LogLevel.Info, "assets", "dropped");
        log.Write(LogLevel.Warning, "assets", "slow load");
        log.Write(LogLevel.Error, "platform", "failed");
        Assert.AreEqual($"assets: slow load{Environment.NewLine}platform: failed{Environment.NewLine}", text.ToString());

        var redirected = new StringWriter();
        var previous = Console.Error;
        Console.SetError(redirected);
        try { new ConsoleLog().Write(LogLevel.Info, "kernel", "to the current error stream"); }
        finally { Console.SetError(previous); }
        Assert.AreEqual($"kernel: to the current error stream{Environment.NewLine}", redirected.ToString());
    }
}

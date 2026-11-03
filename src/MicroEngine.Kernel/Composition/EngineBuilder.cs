using System.Diagnostics.CodeAnalysis;

namespace MicroEngine.Kernel;

/// <summary>
/// A part of the engine or of a game that registers itself: services, frame systems, capabilities, settings and
/// lifecycle handlers, all from <see cref="Configure"/>. Rendering, input, audio, scenes and the game itself are modules.
/// </summary>
public interface IEngineModule
{
    /// <summary>Registers what the module provides on <paramref name="builder"/>; called once, when the module is added.</summary>
    void Configure(EngineBuilder builder);
}

/// <summary>
/// Composes an <see cref="Engine"/>: modules are added in order, each registering services, frame systems and
/// capabilities; settings records are edited; <see cref="Build"/> then fixes the schedule and the time policy.
/// Nothing is discovered by reflection.
/// </summary>
/// <example>
/// <code>
/// var builder = new EngineBuilder()
///     .Configure&lt;TimeSettings&gt;(time => time with { FixedRate = 120 })
///     .Add(new PhysicsModule());
/// builder.Schedule.Add(FramePhase.Update, "mygame.camera", frame => camera.Follow(frame.Blend));
/// Engine engine = builder.Build();
/// </code>
/// </example>
public sealed class EngineBuilder
{
    private readonly HashSet<Type> _modules = [];
    private bool _built;

    /// <summary>The services modules register and read (<see cref="ServiceRegistry"/>).</summary>
    public ServiceRegistry Services { get; } = new();
    /// <summary>The frame systems, by phase (<see cref="FrameSchedule"/>).</summary>
    public FrameSchedule Schedule { get; } = new();
    /// <summary>The optional features declared so far (<see cref="CapabilityRegistry"/>).</summary>
    public CapabilityRegistry Capabilities { get; } = new();
    /// <summary>The engine's events; modules may subscribe while they are configured.</summary>
    public EngineLifecycle Lifecycle { get; } = new();

    /// <summary>Adds <paramref name="module"/> and runs its <see cref="IEngineModule.Configure"/> now.</summary>
    /// <returns>This builder, to chain calls.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="module"/> is null.</exception>
    /// <exception cref="InvalidOperationException">A module of the same type was already added, or the engine was built.</exception>
    public EngineBuilder Add<TModule>(TModule module) where TModule : IEngineModule
    {
        ArgumentNullException.ThrowIfNull(module);
        ThrowIfBuilt();
        if (!_modules.Add(module.GetType()))
            throw new InvalidOperationException($"A {module.GetType().Name} module was already added.");
        module.Configure(this);
        return this;
    }

    /// <summary>Whether a module of type <typeparamref name="TModule"/> was added.</summary>
    public bool HasModule<TModule>() where TModule : IEngineModule => _modules.Contains(typeof(TModule));

    /// <summary>
    /// The settings record <typeparamref name="TSettings"/> as configured so far: the registered service, or a new
    /// instance with its defaults when none was registered.
    /// </summary>
    public TSettings Settings<TSettings>() where TSettings : class, new() =>
        Services.TryGet<TSettings>(out var settings) ? settings : new TSettings();

    /// <summary>
    /// Edits the settings record <typeparamref name="TSettings"/>: <paramref name="edit"/> receives the current value
    /// (<see cref="Settings{TSettings}"/>) and returns the new one, which becomes the registered service. Settings are
    /// values: the engine reads them once, when it is built.
    /// </summary>
    /// <returns>This builder, to chain calls.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="edit"/> is null.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="edit"/> returned null, or the engine was built.</exception>
    public EngineBuilder Configure<TSettings>(Func<TSettings, TSettings> edit) where TSettings : class, new()
    {
        ArgumentNullException.ThrowIfNull(edit);
        ThrowIfBuilt();
        var edited = edit(Settings<TSettings>()) ?? throw new InvalidOperationException($"Editing {typeof(TSettings).Name} returned null.");
        Services.Replace(edited);
        return this;
    }

    /// <summary>
    /// Builds the engine, once. Registers the defaults of what nobody registered (<see cref="TimeSettings"/>, a
    /// <see cref="ConsoleLog"/> as <see cref="ILog"/>, the lifecycle as <see cref="IEngineLifecycle"/>), resolves and
    /// fixes the frame schedule and creates the loop's <see cref="FrameClock"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The engine was already built, or the schedule's order cannot be resolved (a cycle, or a constraint naming a missing system or one in another phase).</exception>
    /// <exception cref="ArgumentOutOfRangeException">A <see cref="TimeSettings"/> value is out of range.</exception>
    public Engine Build()
    {
        ThrowIfBuilt();
        var time = Settings<TimeSettings>();
        var clock = new FrameClock(time);
        Schedule.Freeze();
        if (!Services.Contains<TimeSettings>()) Services.Add(time);
        if (!Services.Contains<ILog>()) Services.Add<ILog>(new ConsoleLog());
        if (!Services.Contains<IEngineLifecycle>()) Services.Add<IEngineLifecycle>(Lifecycle);
        Services.Replace(clock);
        _built = true;
        var engine = new Engine(Services, Schedule, Capabilities, Lifecycle, clock);
        Services.Replace(engine);
        return engine;
    }

    private void ThrowIfBuilt()
    {
        if (_built) throw new InvalidOperationException("The engine was already built; configure it before calling Build.");
    }
}

/// <summary>
/// A composed engine: its services, its fixed frame schedule, its clock, its events and its capabilities. A loop (the
/// game runner, a host, a test) drives it frame by frame: it advances <see cref="Clock"/> and runs the phases in order.
/// </summary>
public sealed class Engine
{
    internal Engine(ServiceRegistry services, FrameSchedule schedule, CapabilityRegistry capabilities, EngineLifecycle lifecycle, FrameClock clock)
    {
        Services = services;
        Schedule = schedule;
        Capabilities = capabilities;
        Lifecycle = lifecycle;
        Clock = clock;
        Log = services.Get<ILog>();
    }

    /// <summary>The services registered by the modules.</summary>
    public ServiceRegistry Services { get; }
    /// <summary>The frame systems; fixed (<see cref="FrameSchedule.IsFrozen"/>).</summary>
    public FrameSchedule Schedule { get; }
    /// <summary>The declared optional features.</summary>
    public CapabilityRegistry Capabilities { get; }
    /// <summary>The engine's events; the loop and hosts raise them.</summary>
    public EngineLifecycle Lifecycle { get; }
    /// <summary>The loop's clock, following the engine's <see cref="TimeSettings"/>.</summary>
    public FrameClock Clock { get; }
    /// <summary>The engine's log (<see cref="ILog"/> service).</summary>
    public ILog Log { get; }
    /// <summary>The time policy the engine was built with.</summary>
    public TimeSettings Time => Clock.Settings;

    /// <summary>The capability declared as <typeparamref name="T"/>, if there is one.</summary>
    public bool TryGetCapability<T>([NotNullWhen(true)] out T? capability) where T : class => Capabilities.TryGet(out capability);

    /// <summary>Runs the systems of <paramref name="phase"/> with <paramref name="context"/>.</summary>
    public void Run(FramePhase phase, in FrameContext context) => Schedule.Run(phase, context);

    /// <summary>Runs the four fixed phases, in order, for one step: <see cref="FramePhase.PreFixed"/>, <see cref="FramePhase.FixedUpdate"/>, <see cref="FramePhase.Physics"/>, <see cref="FramePhase.PostFixed"/>.</summary>
    public void RunFixedStep(in FrameContext step)
    {
        Schedule.Run(FramePhase.PreFixed, step);
        Schedule.Run(FramePhase.FixedUpdate, step);
        Schedule.Run(FramePhase.Physics, step);
        Schedule.Run(FramePhase.PostFixed, step);
    }
}

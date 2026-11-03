using System.Diagnostics.CodeAnalysis;
using MicroEngine.Audio;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Kernel;
using MicroEngine.Platform;

namespace MicroEngine.Tests;

/// <summary>An event sink that ignores what a host delivers, for tests that only pump.</summary>
internal sealed class IgnoredEvents : IEngineEventSink
{
    public void Resized(SurfaceResized size) { }
    public void FocusChanged(bool focused) { }
    public void Suspending() { }
    public void Resumed() { }
    public void QuitRequested() { }
}

/// <summary>
/// A platform for tests that need to control one aspect of a host: a headless platform underneath, a clock the test
/// may set by hand (<see cref="Now"/>), extra capabilities (<see cref="Capabilities"/>) and members to override. It
/// forwards every member it does not change, the capability query included, so it keeps the headless platform's
/// deterministic <see cref="RunPreset"/> unless it stands for a real-time host (<c>realTime</c>), which states none.
/// </summary>
internal abstract class TestPlatform : IPlatform, IMonotonicClock
{
    private double? _now;

    protected TestPlatform(HeadlessPlatform? inner = null, bool realTime = false)
    {
        Inner = inner ?? new HeadlessPlatform();
        if (realTime) Capabilities.Add(new RunPreset());
    }

    /// <summary>The headless platform underneath.</summary>
    public HeadlessPlatform Inner { get; }
    /// <summary>Capabilities this platform declares in addition to (and in place of) the headless platform's.</summary>
    public CapabilityRegistry Capabilities { get; } = new();

    private bool _suspended;

    /// <summary>Sends the platform to the background (true) or brings it back (false), delivered at the next pump.</summary>
    public bool Suspended
    {
        get => _suspended;
        set
        {
            _suspended = value;
            if (value) Inner.Suspend();
            else Inner.Resume();
        }
    }

    /// <summary>The clock: the headless platform's, until the test sets it.</summary>
    public double Now { get => _now ?? Inner.Clock.Now; set => _now = value; }
    public IMonotonicClock Clock => this;
    public virtual IGraphicsDevice Graphics => Inner.Graphics;
    public virtual IAudioDevice Audio => Inner.Audio;
    public virtual IInputSource Input => Inner.Input;
    public virtual bool PumpEvents(IEngineEventSink sink) => Inner.PumpEvents(sink);
    public virtual ISurface? MainSurface => Inner.MainSurface;
    public virtual bool CanBlock => true;
    public bool TryGetCapability<T>([NotNullWhen(true)] out T? capability) where T : class =>
        Capabilities.TryGet(out capability) || Inner.TryGetCapability(out capability);
    public virtual IRenderer BeginFrame() => Inner.BeginFrame();
    public virtual void EndFrame() => Inner.EndFrame();
    public virtual void SaveScreenshot(string path) => Inner.SaveScreenshot(path);
    public void ToggleFullscreen() { }
    public Color Letterbox { get; set; }
    public virtual void Dispose() => Inner.Dispose();
}

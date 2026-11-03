using MicroEngine.Kernel;
using MicroEngine.Platform;

namespace MicroEngine.NativeChecks;

/// <summary>An event sink that ignores what a host delivers, for checks that only pump a platform's events.</summary>
internal sealed class IgnoredEvents : IEngineEventSink
{
    public static IgnoredEvents Instance { get; } = new();
    public void Resized(SurfaceResized size) { }
    public void FocusChanged(bool focused) { }
    public void Suspending() { }
    public void Resumed() { }
    public void QuitRequested() { }
}

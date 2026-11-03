using MicroEngine.Conformance.Graphics;
using MicroEngine.Graphics.Devices;
using MicroEngine.Raylib;
using Rl = Raylib_cs.Raylib;

namespace MicroEngine.Desktop.Tests;

/// <summary>
/// raylib's window as the surface of a device under test. raylib has one window per process: the first surface opens it
/// (hidden, 64 × 48) and the last one closes it; surfaces created while it is open share it.
/// </summary>
public sealed unsafe class RaylibTestSurface : ITestSurface
{
    private static int _open;
    private bool _disposed;

    public RaylibTestSurface()
    {
        if (_open++ > 0) return;
        Rl.SetTraceLogLevel(Raylib_cs.TraceLogLevel.Warning);
        Rl.SetConfigFlags(Raylib_cs.ConfigFlags.HiddenWindow);
        Rl.InitWindow(64, 48, "graphics device conformance");
        if (!Rl.IsWindowReady())
        {
            _open = 0;
            throw new InvalidOperationException("raylib could not open a window: no display, or no usable OpenGL");
        }
    }

    public nint NativeWindow => (nint)Rl.GetWindowHandle();
    public (int Width, int Height) PixelSize => (Rl.GetRenderWidth(), Rl.GetRenderHeight());
    public float PixelDensity => 1;
    public bool IsMinimized => Rl.IsWindowMinimized();

    public event Action? Resized { add { } remove { } }
    public event Action? Lost { add { } remove { } }

    /// <summary>raylib cannot minimise a hidden window, and showing one changes its flags for every later test.</summary>
    public bool TrySetMinimized(bool minimized) => false;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (--_open == 0) Rl.CloseWindow();
    }
}

/// <summary>The device conformance suite on raylib's OpenGL layer (desktop).</summary>
[TestClass]
[TestCategory("Native")]
public sealed class RaylibDeviceConformance : DeviceConformance
{
    protected override string ExpectedBackend => RaylibDevice.BackendName;
    protected override string? ExpectedApi => "opengl";
    protected override GraphicsFeatures ExpectedFeatures => GraphicsFeatures.RenderTargets | GraphicsFeatures.AddressRepeat
        | GraphicsFeatures.AddressMirror | GraphicsFeatures.SeparateFilters | GraphicsFeatures.CustomBlend;

    protected override ITestSurface CreateSurface(int width, int height)
    {
        if (OperatingSystem.IsMacOS()) Assert.Inconclusive("windows can only be opened on the main thread on macOS; see web/tests/desktop.mjs");
        try { return new RaylibTestSurface(); }
        catch (InvalidOperationException e)
        {
            Native.Require(false, e.Message);
            throw;
        }
    }

    protected override IGraphicsDevice CreateDevice(ITestSurface surface) => new RaylibDeviceFactory().Create(surface, new GraphicsDeviceOptions());
}

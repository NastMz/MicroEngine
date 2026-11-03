using MicroEngine.Conformance.Graphics;
using MicroEngine.Graphics.Devices;
using MicroEngine.Sdl;
using SDL;
using static SDL.SDL3;

namespace MicroEngine.Desktop.Tests;

/// <summary>A hidden SDL window as the surface of a graphics device under test.</summary>
public sealed unsafe class SdlTestSurface : ITestSurface
{
    private SDL_Window* _window;

    public SdlTestSurface(int width, int height)
    {
        if (!SDL_InitSubSystem(SDL_InitFlags.SDL_INIT_VIDEO)) throw new InvalidOperationException($"SDL cannot start its video subsystem here: {SDL_GetError()}");
        _window = SDL_CreateWindow("graphics device conformance", width, height, SDL_WindowFlags.SDL_WINDOW_HIDDEN);
        if (_window == null)
        {
            SDL_QuitSubSystem(SDL_InitFlags.SDL_INIT_VIDEO);
            throw new InvalidOperationException($"SDL cannot open a window here: {SDL_GetError()}");
        }
    }

    public nint NativeWindow => (nint)_window;

    public (int Width, int Height) PixelSize
    {
        get
        {
            int w, h;
            SDL_GetWindowSizeInPixels(_window, &w, &h);
            return (w, h);
        }
    }

    public float PixelDensity => SDL_GetWindowPixelDensity(_window);

    public bool IsMinimized
    {
        get
        {
            var (w, h) = PixelSize;
            return (SDL_GetWindowFlags(_window) & SDL_WindowFlags.SDL_WINDOW_MINIMIZED) != 0 || w == 0 || h == 0;
        }
    }

    public event Action? Resized { add { } remove { } }
    public event Action? Lost { add { } remove { } }

    /// <summary>Shows and minimises the window (restores and hides it again); false when the window system does not minimise it.</summary>
    public bool TrySetMinimized(bool minimized)
    {
        if (minimized)
        {
            SDL_ShowWindow(_window);
            SDL_MinimizeWindow(_window);
        }
        else
        {
            SDL_RestoreWindow(_window);
            SDL_HideWindow(_window);
        }
        SDL_SyncWindow(_window);
        SDL_Event e;
        while (SDL_PollEvent(&e)) { }
        return IsMinimized == minimized;
    }

    public void Dispose()
    {
        if (_window == null) return;
        SDL_DestroyWindow(_window);
        _window = null;
        SDL_QuitSubSystem(SDL_InitFlags.SDL_INIT_VIDEO);
    }
}

/// <summary>The device conformance suite on SDL_Renderer: SDL's default driver, then each driver this system has.</summary>
public abstract class SdlDeviceConformance(string? driver) : DeviceConformance
{
    /// <summary>The tier-0 features every GPU driver of SDL_Renderer declares.</summary>
    internal const GraphicsFeatures GpuDriverFeatures = GraphicsFeatures.RenderTargets | GraphicsFeatures.AddressRepeat | GraphicsFeatures.CustomBlend;

    protected override string ExpectedBackend => SdlRendererDevice.BackendName;
    protected override string? ExpectedApi => driver;
    protected override GraphicsFeatures ExpectedFeatures => GpuDriverFeatures;

    /// <summary>Whether a device that cannot open here fails under MICROENGINE_NATIVE_TESTS=1 (the default driver) or is only skipped.</summary>
    protected virtual bool Required => driver is null;

    protected override ITestSurface CreateSurface(int width, int height)
    {
        if (OperatingSystem.IsMacOS()) Assert.Inconclusive("windows can only be opened on the main thread on macOS; see web/tests/desktop.mjs");
        try { return new SdlTestSurface(width, height); }
        catch (InvalidOperationException e)
        {
            Native.Require(false, e.Message);
            throw;
        }
    }

    protected override IGraphicsDevice CreateDevice(ITestSurface surface)
    {
        if (driver is not null && !SdlPlatform.RenderDrivers.Contains(driver))
            Assert.Inconclusive($"SDL has no '{driver}' render driver here ({string.Join(", ", SdlPlatform.RenderDrivers)})");
        try { return new SdlRendererBackendFactory().Create(surface, new GraphicsDeviceOptions(Api: driver)); }
        catch (InvalidOperationException e)
        {
            string why = $"SDL_Renderer '{driver ?? "default"}' cannot start here: {e.Message}";
            if (Required) Native.Require(false, why);
            Assert.Inconclusive(why);
            throw;
        }
    }
}

[TestClass]
[TestCategory("Native")]
public sealed class SdlDefaultDeviceConformance() : SdlDeviceConformance(null);

[TestClass]
[TestCategory("Native")]
public sealed class SdlDirect3D11DeviceConformance() : SdlDeviceConformance("direct3d11");

[TestClass]
[TestCategory("Native")]
public sealed class SdlDirect3D12DeviceConformance() : SdlDeviceConformance("direct3d12");

[TestClass]
[TestCategory("Native")]
public sealed class SdlOpenGLDeviceConformance() : SdlDeviceConformance("opengl");

[TestClass]
[TestCategory("Native")]
public sealed class SdlVulkanDeviceConformance() : SdlDeviceConformance("vulkan");

[TestClass]
[TestCategory("Native")]
public sealed class SdlGpuDriverDeviceConformance() : SdlDeviceConformance("gpu");

/// <summary>
/// SDL's software renderer: a diagnostic device that does not claim conformance. It runs the suite with two named
/// tolerances: rotated-edge coverage (its own polygon rasterizer, not the pixel-centre rule: 33 of 1,595 pixels measured
/// on Windows) and blending (truncating 8-bit integer steps: 1.2/255 below the formula measured for Multiply). It
/// declares no custom blend (its blend probe fails).
/// </summary>
[TestClass]
[TestCategory("Native")]
public sealed class SdlSoftwareDeviceConformance() : SdlDeviceConformance("software")
{
    protected override GraphicsFeatures ExpectedFeatures => GraphicsFeatures.RenderTargets | GraphicsFeatures.AddressRepeat;
    protected override int CoverageTolerance => 40;
    protected override int BlendTolerance => 2;
}

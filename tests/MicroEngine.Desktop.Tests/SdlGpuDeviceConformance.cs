using MicroEngine.Conformance.Graphics;
using MicroEngine.Graphics.Devices;
using MicroEngine.Graphics.SdlGpu;

namespace MicroEngine.Desktop.Tests;

/// <summary>
/// The device conformance suite on the tier-1 SDL_GPU device: SDL's default GPU driver, then Direct3D 12 and Vulkan
/// where this system has them. Category <c>Gpu</c>: CI runs these on Linux with Mesa's software Vulkan (lavapipe), so
/// <c>MICROENGINE_GPU_ADAPTER</c>, when set, names a substring the reported adapter must contain (the proof that the
/// run used the adapter it claims). <c>MICROENGINE_GPU_LOW_POWER=1</c> asks SDL for the integrated adapter.
/// </summary>
public abstract class SdlGpuConformance(string? api) : DeviceConformance
{
    protected override string ExpectedBackend => SdlGpuDevice.BackendName;
    protected override string? ExpectedApi => api;
    protected override GraphicsTier ExpectedTier => GraphicsTier.Tier1;
    protected override GraphicsFeatures ExpectedFeatures => SdlGpuDevice.DeclaredFeatures;

    /// <summary>Whether a device that cannot open here fails under MICROENGINE_NATIVE_TESTS=1 (the default driver) or is only skipped.</summary>
    protected virtual bool Required => api is null;

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
        var options = new GraphicsDeviceOptions(Api: api, PreferLowPower: Environment.GetEnvironmentVariable("MICROENGINE_GPU_LOW_POWER") == "1");
        IGraphicsDevice device;
        try { device = new SdlGpuBackendFactory().Create(surface, options); }
        catch (Exception e) when (e is InvalidOperationException or NotSupportedException)
        {
            string why = $"SDL_GPU '{api ?? "default"}' cannot start here: {e.Message}";
            if (Required) Native.Require(false, why);
            Assert.Inconclusive(why);
            throw;
        }
        if (Environment.GetEnvironmentVariable("MICROENGINE_GPU_ADAPTER") is { Length: > 0 } expected
            && device.Capabilities.Adapter?.Contains(expected, StringComparison.OrdinalIgnoreCase) != true)
        {
            var reported = device.Capabilities.Adapter;
            device.Dispose();
            Assert.Fail($"MICROENGINE_GPU_ADAPTER={expected}, but SDL_GPU chose \"{reported}\"");
        }
        return device;
    }

    [TestMethod]
    public void Identity_NamesTheAdapterAndDriverSdlChose()
    {
        var device = (SdlGpuDevice)Device;
        Console.WriteLine($"sdl-gpu api={Caps.ApiName} adapter=\"{Caps.Adapter}\" driver=\"{device.DriverName}\" version=\"{device.DriverVersion}\" info=\"{device.DriverInfo}\" shaders={Caps.ShaderFormats} present={string.Join(",", Caps.PresentModes)}");
        Assert.IsFalse(string.IsNullOrEmpty(Caps.Adapter), "SDL_GPU names the adapter");
        Assert.AreNotEqual(ShaderFormat.None, Caps.ShaderFormats);
        Assert.AreEqual(PresentMode.VSync, device.SwapchainPresentMode, "the present mode is set on the swapchain explicitly");
        var depth = string.Join(",", new[] { TextureFormat.D16Unorm, TextureFormat.D24UnormS8, TextureFormat.D32Float }.Where(f => Caps.Supports(f, TextureUsage.DepthStencil)));
        Console.WriteLine($"depth formats: {depth}");
    }

    [TestMethod]
    public void Present_CopiesTheSurfaceTextureToTheSwapchainOncePerShownFrame()
    {
        var device = (SdlGpuDevice)Device;
        var before = device.SurfaceCopies;
        Assert.IsTrue(Device.TryAcquireSurfaceTarget(out var surface));
        Painter.Paint(surface, [], clear: Opaque(10, 20, 30));
        Device.Present();
        // A hidden window may have no swapchain texture this frame (SDL returns none while it is occluded): at most one copy.
        Assert.IsTrue(device.SurfaceCopies - before is 0 or 1, $"{device.SurfaceCopies - before} copies for one present");
    }

    [TestMethod]
    public void Offscreen_WithoutASurface_RendersAndReadsBack()
    {
        using var offscreen = new SdlGpuBackendFactory().Create(null, new GraphicsDeviceOptions(Api: api));
        Assert.IsFalse(offscreen.TryAcquireSurfaceTarget(out var none));
        Assert.IsNull(none);
        Assert.IsEmpty(offscreen.Capabilities.PresentModes, "no surface, no present modes");
        using var painter = new Painter(offscreen);
        var target = offscreen.CreateRenderTarget(new RenderTargetDescription(4, 4));
        painter.Paint(target, [], clear: Opaque(32, 64, 96));
        var request = offscreen.ReadPixels(target, target.Bounds);
        request.Wait();
        CollectionAssert.AreEqual(new byte[] { 32, 64, 96, 255 }, request.Pixels[..4].ToArray());
    }
}

[TestClass]
[TestCategory("Native")]
[TestCategory("Gpu")]
public sealed class SdlGpuDefaultDeviceConformance() : SdlGpuConformance(null);

/// <summary>An offscreen SDL_GPU device in a process where nothing else started SDL (no window, no video subsystem).</summary>
[TestClass]
[TestCategory("Native")]
[TestCategory("Gpu")]
public sealed class SdlGpuOffscreenTests
{
    [TestMethod]
    public void Offscreen_WithoutAnySdlWindow_StartsRendersAndReadsBack()
    {
        var factory = new SdlGpuBackendFactory();
        if (factory.UnsupportedReason(null) is { } reason) Native.Require(false, $"SDL_GPU cannot start here: {reason}");
        using var device = factory.Create(null, new GraphicsDeviceOptions());
        using var painter = new Painter(device);
        var target = device.CreateRenderTarget(new RenderTargetDescription(4, 4));
        painter.Paint(target, [], clear: PremultipliedColor.FromBytes(32, 64, 96, 255));
        var request = device.ReadPixels(target, target.Bounds);
        request.Wait();
        CollectionAssert.AreEqual(new byte[] { 32, 64, 96, 255 }, request.Pixels[..4].ToArray());
    }
}

[TestClass]
[TestCategory("Native")]
[TestCategory("Gpu")]
public sealed class SdlGpuDirect3D12DeviceConformance() : SdlGpuConformance("direct3d12")
{
    protected override bool Required => false;
}

[TestClass]
[TestCategory("Native")]
[TestCategory("Gpu")]
public sealed class SdlGpuVulkanDeviceConformance() : SdlGpuConformance("vulkan")
{
    // Required where the CI job installs a Vulkan driver (lavapipe) and says so.
    protected override bool Required => Environment.GetEnvironmentVariable("MICROENGINE_GPU_ADAPTER") is { Length: > 0 };
}

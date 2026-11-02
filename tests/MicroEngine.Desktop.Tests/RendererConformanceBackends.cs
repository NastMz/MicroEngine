using MicroEngine.Core;
using MicroEngine.Raylib;
using MicroEngine.Sdl;

namespace MicroEngine.Desktop.Tests;

/// <summary>The headless platform with the CPU reference rasterizer: no window, so it runs everywhere, macOS included.</summary>
[TestClass]
public sealed class HeadlessReferenceRendererConformance : RendererConformance
{
    protected override IPlatform? Open(Game game, PlatformRequest request) => HeadlessPlatform.For(request.Settings, rasterize: true);
}

/// <summary>Opens a real desktop backend for the conformance suite; real windows, hidden.</summary>
public abstract class NativeRendererConformance : RendererConformance
{
    /// <summary>The backend's catalog entry.</summary>
    protected abstract PlatformProfile Profile { get; }

    /// <summary>
    /// Whether a backend that cannot open here fails under MICROENGINE_NATIVE_TESTS=1 (the default backends) or is only
    /// skipped (a render driver this system may lack).
    /// </summary>
    protected virtual bool Required => true;

    [TestInitialize]
    public void NotOnMacOS()
    {
        if (OperatingSystem.IsMacOS()) Assert.Inconclusive("windows can only be opened on the main thread on macOS; see web/tests/desktop.mjs");
    }

    protected override IPlatform? Open(Game game, PlatformRequest request)
    {
        try { return Profile.Create(request); }
        catch (InvalidOperationException e)
        {
            string why = $"{Profile.Name} cannot open here: {e.Message}";
            if (Required) Native.Require(false, why);
            Assert.Inconclusive(why);
            return null;
        }
    }
}

/// <summary>An SDL render driver, skipped when this system does not have it.</summary>
public abstract class SdlDriverConformance(string driver) : NativeRendererConformance
{
    protected override PlatformProfile Profile => SdlPlatform.CreateProfile(driver);
    protected override bool Required => false;

    [TestInitialize]
    public void DriverAvailable()
    {
        if (!SdlPlatform.RenderDrivers.Contains(driver)) Assert.Inconclusive($"SDL has no '{driver}' render driver here ({string.Join(", ", SdlPlatform.RenderDrivers)})");
    }
}

[TestClass]
[TestCategory("Native")]
public sealed class SdlRendererConformance : NativeRendererConformance
{
    protected override PlatformProfile Profile => SdlPlatform.Profile;
}

[TestClass]
[TestCategory("Native")]
public sealed class SdlDirect3D11RendererConformance() : SdlDriverConformance("direct3d11");

[TestClass]
[TestCategory("Native")]
public sealed class SdlDirect3D12RendererConformance() : SdlDriverConformance("direct3d12");

[TestClass]
[TestCategory("Native")]
public sealed class SdlOpenGLRendererConformance() : SdlDriverConformance("opengl");

[TestClass]
[TestCategory("Native")]
public sealed class SdlVulkanRendererConformance() : SdlDriverConformance("vulkan");

[TestClass]
[TestCategory("Native")]
public sealed class SdlGpuRendererConformance() : SdlDriverConformance("gpu");

/// <summary>
/// SDL's software renderer declares two sources of variation: it rounds blends down (±1/255) and rasterizes circles,
/// lines and triangles with its own algorithm (measured up to +30 % coverage on circles, 0.6× to 2× on lines).
/// </summary>
[TestClass]
[TestCategory("Native")]
public sealed class SdlSoftwareRendererConformance() : SdlDriverConformance("software")
{
    protected override Variation Allowed => Variation.Quantization | Variation.ShapeCoverage;
}

[TestClass]
[TestCategory("Native")]
public sealed class RaylibRendererConformance : NativeRendererConformance
{
    protected override PlatformProfile Profile => RaylibPlatform.Profile;
}

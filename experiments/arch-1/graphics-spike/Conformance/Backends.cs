using Arch1.Graphics.Tier0Sdl;
using Arch1.Graphics.Tier1SdlGpu;

namespace Arch1.Graphics.Conformance;

/// <summary>Tier 0: SDL_Renderer with its default driver (Direct3D 11 on Windows, per P1).</summary>
[TestClass]
public sealed class Tier0SdlRendererDirect3D11 : DeviceConformance
{
    protected override IGraphicsBackendFactory Factory { get; } = new SdlRendererBackendFactory();
    protected override string? Api => null; // the default driver
    protected override string ExpectedApi => "direct3d11";
    protected override GraphicsTier ExpectedTier => GraphicsTier.Tier0;
    protected override GraphicsFeatures ExpectedFeatures => GraphicsFeatures.RenderTargets | GraphicsFeatures.AddressRepeat | GraphicsFeatures.CustomBlend;
}

/// <summary>Tier 1: pure SDL_GPU on Direct3D 12.</summary>
[TestClass]
public sealed class Tier1SdlGpuDirect3D12 : DeviceConformance
{
    protected override IGraphicsBackendFactory Factory { get; } = new SdlGpuBackendFactory();
    protected override string? Api => "direct3d12";
    protected override string ExpectedApi => "direct3d12";
    protected override GraphicsTier ExpectedTier => GraphicsTier.Tier1;
    protected override GraphicsFeatures ExpectedFeatures => Tier1Features;

    internal const GraphicsFeatures Tier1Features = GraphicsFeatures.RenderTargets | GraphicsFeatures.AddressRepeat | GraphicsFeatures.AddressMirror
        | GraphicsFeatures.CustomBlend | GraphicsFeatures.Shaders | GraphicsFeatures.DepthBuffer | GraphicsFeatures.Instancing | GraphicsFeatures.SeparateFilters;
}

/// <summary>Tier 1: pure SDL_GPU on Vulkan.</summary>
[TestClass]
public sealed class Tier1SdlGpuVulkan : DeviceConformance
{
    protected override IGraphicsBackendFactory Factory { get; } = new SdlGpuBackendFactory();
    protected override string? Api => "vulkan";
    protected override string ExpectedApi => "vulkan";
    protected override GraphicsTier ExpectedTier => GraphicsTier.Tier1;
    protected override GraphicsFeatures ExpectedFeatures => Tier1SdlGpuDirect3D12.Tier1Features;
}

/// <summary>Tier 0 on SDL_Renderer's OpenGL driver: the same adapter code over a second driver.</summary>
[TestClass]
public sealed class Tier0SdlRendererOpenGL : DeviceConformance
{
    protected override IGraphicsBackendFactory Factory { get; } = new SdlRendererBackendFactory();
    protected override string? Api => "opengl";
    protected override string ExpectedApi => "opengl";
    protected override GraphicsTier ExpectedTier => GraphicsTier.Tier0;
    protected override GraphicsFeatures ExpectedFeatures => GraphicsFeatures.RenderTargets | GraphicsFeatures.AddressRepeat | GraphicsFeatures.CustomBlend;
}

/// <summary>Tier 0 on SDL_Renderer's Vulkan driver.</summary>
[TestClass]
public sealed class Tier0SdlRendererVulkan : DeviceConformance
{
    protected override IGraphicsBackendFactory Factory { get; } = new SdlRendererBackendFactory();
    protected override string? Api => "vulkan";
    protected override string ExpectedApi => "vulkan";
    protected override GraphicsTier ExpectedTier => GraphicsTier.Tier0;
    protected override GraphicsFeatures ExpectedFeatures => GraphicsFeatures.RenderTargets | GraphicsFeatures.AddressRepeat | GraphicsFeatures.CustomBlend;
}

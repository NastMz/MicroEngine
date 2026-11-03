using System.Diagnostics.CodeAnalysis;
using MicroEngine.Platform;

namespace MicroEngine.Graphics.Devices;

/// <summary>Creates devices of one backend. A backend that accepts only its own surfaces says so through <see cref="UnsupportedReason"/>.</summary>
public interface IGraphicsBackendFactory
{
    /// <summary>Backend name: "sdl-renderer", "raylib", "reference" (and, later, "sdl-gpu").</summary>
    string Name { get; }

    /// <summary>Null when <see cref="Create"/> can succeed for <paramref name="surface"/>, otherwise why not.</summary>
    string? UnsupportedReason(ISurface? surface);

    /// <summary>Creates a device for <paramref name="surface"/> (null = offscreen only, where the backend supports it).</summary>
    IGraphicsDevice Create(ISurface? surface, GraphicsDeviceOptions options);
}

/// <summary>Device creation options.</summary>
/// <param name="PresentMode">Present mode chosen explicitly (never left to the driver); must be in <see cref="GraphicsCapabilities.PresentModes"/> when there is a surface.</param>
/// <param name="Api">Preferred underlying API ("direct3d12", "vulkan", ...) or null for the backend default. Check <see cref="GraphicsCapabilities.ApiName"/> after creation.</param>
/// <param name="Debug">Enable API validation where available.</param>
/// <param name="PreferLowPower">Prefer an integrated adapter.</param>
public sealed record GraphicsDeviceOptions(PresentMode PresentMode = PresentMode.VSync, string? Api = null, bool Debug = false, bool PreferLowPower = false);

/// <summary>Why a device was lost.</summary>
public enum DeviceLostReason
{
    /// <summary>The GPU was reset or removed.</summary>
    DeviceReset,
    /// <summary>Render target contents were discarded by the driver.</summary>
    TargetsReset,
}

/// <summary>
/// The single graphics device contract for every tier. Tiers differ only in <see cref="Capabilities"/>; a request
/// outside them throws <see cref="GraphicsCapabilityException"/> at creation or recording time.
/// <para>Ordering rule: command lists execute in <see cref="Submit"/> order, and within a list in recording order
/// (updates included); <see cref="ReadPixels"/> observes every list submitted before it.</para>
/// <para>Threading: one thread (the render thread) creates, records, submits and presents.</para>
/// </summary>
public interface IGraphicsDevice : IDisposable
{
    /// <summary>Declared capabilities.</summary>
    GraphicsCapabilities Capabilities { get; }

    /// <summary>Optional: raised only by backends that can detect loss (SDL_GPU never raises it); resources are marked <see cref="GraphicsResource.IsLost"/>.</summary>
    event Action<DeviceLostReason>? DeviceLost;

    /// <summary>Creates a sampled texture, optionally with initial data (exactly <see cref="TextureFormats.ByteCount"/> bytes of level 0, or empty).</summary>
    Texture CreateTexture(in TextureDescription description, ReadOnlySpan<byte> initialData = default);

    /// <summary>Creates an offscreen target; requires <see cref="GraphicsFeatures.RenderTargets"/>. Contents are undefined until a pass clears or draws.</summary>
    RenderTarget CreateRenderTarget(in RenderTargetDescription description);

    /// <summary>Returns the cached sampler for a description.</summary>
    Sampler GetSampler(in SamplerDescription description);

    /// <summary>Creates a buffer (every tier).</summary>
    GpuBuffer CreateBuffer(in BufferDescription description);

    /// <summary>Creates a custom shader; requires <see cref="GraphicsFeatures.Shaders"/> and a declared format. Invalid bytecode fails here or at <see cref="CreatePipeline"/>, never at draw.</summary>
    Shader CreateShader(in ShaderDescription description);

    /// <summary>Creates a custom pipeline; requires <see cref="GraphicsFeatures.Shaders"/>, and <see cref="GraphicsFeatures.DepthBuffer"/> for depth.</summary>
    Pipeline CreatePipeline(PipelineDescription description);

    /// <summary>
    /// Returns the device-owned built-in pipeline for a kind, a blend state and the attachment formats it renders to
    /// (every tier; the 2D batcher needs nothing else). A blend state outside
    /// <see cref="GraphicsCapabilities.BuiltInBlendStates"/> requires <see cref="GraphicsFeatures.CustomBlend"/>.
    /// </summary>
    Pipeline GetBuiltInPipeline(BuiltInPipeline kind, BlendState blend, TextureFormat colorFormat, TextureFormat? depthFormat = null);

    /// <summary>Starts an empty command list for this device.</summary>
    CommandList BeginCommands();

    /// <summary>Executes a finished list (no open pass) after every list submitted before it. A list is submitted once.</summary>
    void Submit(CommandList commands);

    /// <summary>
    /// Starts reading <paramref name="region"/> of an offscreen target; completes through the returned fence. Surface
    /// targets cannot be read: captures render offscreen, read back, then draw the target to the surface.
    /// </summary>
    ReadbackRequest ReadPixels(RenderTarget target, RectI region);

    /// <summary>
    /// Acquires this frame's surface target. False when there is nothing to present to (no surface, minimised, zero
    /// size): the frame then skips its surface passes and presentation. The target expires at <see cref="Present"/>. Its
    /// <see cref="RenderTarget.ColorFormat"/> is the format pipelines in its passes must declare; a tier-0 backend, whose
    /// built-in pipelines do not depend on the format, reports the nominal format of its back buffer.
    /// </summary>
    bool TryAcquireSurfaceTarget([NotNullWhen(true)] out RenderTarget? target);

    /// <summary>Presents the acquired surface target (no-op when none was acquired) and expires it.</summary>
    void Present();
}

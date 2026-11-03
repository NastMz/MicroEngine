namespace MicroEngine.Graphics.Devices;

/// <summary>
/// Deterministic counts of the work a device received (<see cref="GraphicsDeviceBase.Counters"/>): the same calls give
/// the same counts on every backend and every run, so tests can assert draw calls, passes and uploads exactly as a
/// performance budget. Every count is cumulative since the device was created; subtract two readings to measure a span.
/// </summary>
/// <param name="Submissions">Command lists submitted.</param>
/// <param name="Passes">Render passes in the submitted lists.</param>
/// <param name="DrawCalls"><c>Draw</c> and <c>DrawIndexed</c> commands in the submitted lists.</param>
/// <param name="PipelineChanges"><c>SetPipeline</c> commands in the submitted lists.</param>
/// <param name="TextureBindings"><c>SetTexture</c> commands in the submitted lists.</param>
/// <param name="BufferUpdates"><c>UpdateBuffer</c> commands in the submitted lists.</param>
/// <param name="TextureUpdates"><c>UpdateTexture</c> commands in the submitted lists.</param>
/// <param name="BytesUploaded">Bytes carried by the buffer and texture updates.</param>
/// <param name="Presents">Frames presented (a <see cref="IGraphicsDevice.Present"/> with an acquired surface target).</param>
/// <param name="ResourcesCreated">Resources the device created (device-owned ones included).</param>
/// <param name="LiveResources">Resources alive now (created and not disposed or expired).</param>
public readonly record struct GraphicsCounters(
    long Submissions = 0,
    long Passes = 0,
    long DrawCalls = 0,
    long PipelineChanges = 0,
    long TextureBindings = 0,
    long BufferUpdates = 0,
    long TextureUpdates = 0,
    long BytesUploaded = 0,
    long Presents = 0,
    long ResourcesCreated = 0,
    int LiveResources = 0)
{
    /// <summary>The counts accumulated between two readings (<see cref="LiveResources"/> is the later reading's).</summary>
    public static GraphicsCounters operator -(GraphicsCounters later, GraphicsCounters earlier) => new(
        later.Submissions - earlier.Submissions,
        later.Passes - earlier.Passes,
        later.DrawCalls - earlier.DrawCalls,
        later.PipelineChanges - earlier.PipelineChanges,
        later.TextureBindings - earlier.TextureBindings,
        later.BufferUpdates - earlier.BufferUpdates,
        later.TextureUpdates - earlier.TextureUpdates,
        later.BytesUploaded - earlier.BytesUploaded,
        later.Presents - earlier.Presents,
        later.ResourcesCreated - earlier.ResourcesCreated,
        later.LiveResources);
}

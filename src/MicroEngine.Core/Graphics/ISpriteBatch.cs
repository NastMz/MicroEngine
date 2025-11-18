using MicroEngine.Core.Math;
using MicroEngine.Core.Resources;

namespace MicroEngine.Core.Graphics;

/// <summary>
/// Interface for sprite batch rendering.
/// Allows efficient rendering of multiple sprites with automatic batching.
/// </summary>
public interface ISpriteBatch
{
    /// <summary>
    /// Gets the current sort mode.
    /// </summary>
    SpriteSortMode SortMode { get; }

    /// <summary>
    /// Gets the number of sprites currently queued.
    /// </summary>
    int SpriteCount { get; }

    /// <summary>
    /// Begins a sprite batch operation.
    /// </summary>
    /// <param name="sortMode">Sort mode for this batch.</param>
    void Begin(SpriteSortMode sortMode = SpriteSortMode.Deferred);

    /// <summary>
    /// Adds a sprite to the batch.
    /// </summary>
    /// <param name="texture">Texture to draw.</param>
    /// <param name="position">Position in world/screen space.</param>
    /// <param name="sourceRect">Source rectangle in texture (null = full texture).</param>
    /// <param name="tint">Color tint.</param>
    /// <param name="rotation">Rotation in degrees.</param>
    /// <param name="origin">Origin point for rotation (in pixels).</param>
    /// <param name="scale">Scale factor.</param>
    /// <param name="layerDepth">Layer depth for sorting (0-1).</param>
    void Draw(
        ITexture texture,
        Vector2 position,
        Rectangle? sourceRect = null,
        Color? tint = null,
        float rotation = 0f,
        Vector2? origin = null,
        Vector2? scale = null,
        float layerDepth = 0f);

    /// <summary>
    /// Adds a sprite to the batch using a sprite region.
    /// </summary>
    /// <param name="texture">Texture to draw.</param>
    /// <param name="position">Position in world/screen space.</param>
    /// <param name="region">Sprite region defining source and pivot.</param>
    /// <param name="tint">Color tint.</param>
    /// <param name="rotation">Rotation in degrees.</param>
    /// <param name="scale">Scale factor.</param>
    /// <param name="layerDepth">Layer depth for sorting (0-1).</param>
    void DrawRegion(
        ITexture texture,
        Vector2 position,
        SpriteRegion region,
        Color? tint = null,
        float rotation = 0f,
        Vector2? scale = null,
        float layerDepth = 0f);

    /// <summary>
    /// Ends the batch and submits all sprites for rendering.
    /// </summary>
    void End();

    /// <summary>
    /// Immediately flushes all queued sprites to the GPU.
    /// Only needed if you want to mix batch and non-batch rendering.
    /// </summary>
    void Flush();
}

using MicroEngine.Core.Math;
using MicroEngine.Core.Resources;

namespace MicroEngine.Core.Graphics;

/// <summary>
/// Core implementation of sprite batching.
/// Queues sprites and sorts them before rendering for optimal performance.
/// </summary>
public class SpriteBatch : ISpriteBatch
{
    private readonly List<SpriteBatchItem> _items;
    private readonly IRenderBackend2D _renderBackend;
    private bool _begun;
    private SpriteSortMode _sortMode;

    /// <inheritdoc/>
    public SpriteSortMode SortMode => _sortMode;

    /// <inheritdoc/>
    public int SpriteCount => _items.Count;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpriteBatch"/> class.
    /// </summary>
    /// <param name="renderBackend">Render backend for drawing.</param>
    /// <param name="initialCapacity">Initial capacity for sprite queue.</param>
    public SpriteBatch(IRenderBackend2D renderBackend, int initialCapacity = 256)
    {
        _renderBackend = renderBackend ?? throw new ArgumentNullException(nameof(renderBackend));
        _items = new List<SpriteBatchItem>(initialCapacity);
        _begun = false;
        _sortMode = SpriteSortMode.Deferred;
    }

    /// <inheritdoc/>
    public void Begin(SpriteSortMode sortMode = SpriteSortMode.Deferred)
    {
        if (_begun)
        {
            throw new InvalidOperationException("SpriteBatch.Begin called twice without End");
        }

        _begun = true;
        _sortMode = sortMode;
        _items.Clear();
    }

    /// <inheritdoc/>
    public void Draw(
        ITexture texture,
        Vector2 position,
        Rectangle? sourceRect = null,
        Color? tint = null,
        float rotation = 0f,
        Vector2? origin = null,
        Vector2? scale = null,
        float layerDepth = 0f)
    {
        if (!_begun)
        {
            throw new InvalidOperationException("SpriteBatch.Draw called before Begin");
        }

        if (texture == null)
        {
            throw new ArgumentNullException(nameof(texture));
        }

        // Create sprite region
        var source = sourceRect ?? new Rectangle(0, 0, texture.Width, texture.Height);
        var normalizedOrigin = origin.HasValue
            ? new Vector2(origin.Value.X / source.Width, origin.Value.Y / source.Height)
            : new Vector2(0.5f, 0.5f);

        var region = new SpriteRegion(source, normalizedOrigin);

        // Create batch item
        var item = new SpriteBatchItem(
            texture,
            position,
            region,
            rotation,
            scale ?? Vector2.One,
            tint ?? Color.White,
            layerDepth);

        // Immediate mode: draw right away
        if (_sortMode == SpriteSortMode.Immediate)
        {
            DrawItem(item);
        }
        else
        {
            _items.Add(item);
        }
    }

    /// <inheritdoc/>
    public void DrawRegion(
        ITexture texture,
        Vector2 position,
        SpriteRegion region,
        Color? tint = null,
        float rotation = 0f,
        Vector2? scale = null,
        float layerDepth = 0f)
    {
        if (!_begun)
        {
            throw new InvalidOperationException("SpriteBatch.DrawRegion called before Begin");
        }

        if (texture == null)
        {
            throw new ArgumentNullException(nameof(texture));
        }

        var item = new SpriteBatchItem(
            texture,
            position,
            region,
            rotation,
            scale ?? Vector2.One,
            tint ?? Color.White,
            layerDepth);

        if (_sortMode == SpriteSortMode.Immediate)
        {
            DrawItem(item);
        }
        else
        {
            _items.Add(item);
        }
    }

    /// <inheritdoc/>
    public void End()
    {
        if (!_begun)
        {
            throw new InvalidOperationException("SpriteBatch.End called without Begin");
        }

        Flush();
        _begun = false;
    }

    /// <inheritdoc/>
    public void Flush()
    {
        if (_items.Count == 0)
        {
            return;
        }

        // Sort if needed
        SortSprites();

        // Render all items
        foreach (var item in _items)
        {
            DrawItem(item);
        }

        _items.Clear();
    }

    private void SortSprites()
    {
        switch (_sortMode)
        {
            case SpriteSortMode.Texture:
                _items.Sort((a, b) => a.Texture.Id.Value.CompareTo(b.Texture.Id.Value));
                break;

            case SpriteSortMode.BackToFront:
                _items.Sort((a, b) => b.LayerDepth.CompareTo(a.LayerDepth));
                break;

            case SpriteSortMode.FrontToBack:
                _items.Sort((a, b) => a.LayerDepth.CompareTo(b.LayerDepth));
                break;

            case SpriteSortMode.Deferred:
            case SpriteSortMode.Immediate:
            default:
                // No sorting
                break;
        }
    }

    private void DrawItem(SpriteBatchItem item)
    {
        var source = item.Region.Source;
        var pivot = item.Region.Pivot;

        // Calculate origin in pixels
        var originPixels = new Vector2(
            source.Width * pivot.X,
            source.Height * pivot.Y);

        // Calculate destination size
        var destWidth = source.Width * item.Scale.X;
        var destHeight = source.Height * item.Scale.Y;

        var destRect = new Rectangle(
            item.Position.X,
            item.Position.Y,
            destWidth,
            destHeight);

        _renderBackend.DrawTexturePro(
            item.Texture,
            source,
            destRect,
            originPixels,
            item.Rotation,
            item.Tint);
    }
}

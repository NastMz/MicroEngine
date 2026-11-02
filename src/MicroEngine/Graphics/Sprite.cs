using System.Numerics;

namespace MicroEngine.Graphics;

/// <summary>A region of a texture with a pivot (origin) in source pixels.</summary>
/// <remarks>Sprites usually come from a <see cref="SpriteAtlas"/>, which owns their texture.</remarks>
/// <param name="Texture">The texture that holds the pixels; owned by whoever created it (usually an atlas).</param>
/// <param name="Source">The sprite's rectangle in the texture, in texture pixels.</param>
/// <param name="Origin">The anchor in source pixels from the sprite's top-left: the point placed at the drawing position, and the pivot for rotation and scale.</param>
public sealed record Sprite(Texture Texture, RectF Source, Vector2 Origin)
{
    /// <summary>Width of the sprite in pixels.</summary>
    public int Width => (int)Source.Width;
    /// <summary>Height of the sprite in pixels.</summary>
    public int Height => (int)Source.Height;
}

/// <summary>
/// Packs many procedurally authored <see cref="PixelImage"/>s into one texture and
/// exposes them by name. Keeps draw calls batched and art code independent of layout.
/// </summary>
/// <remarks>
/// Add images with <see cref="Add"/>, <see cref="AddFrames"/> or <see cref="AddGrid"/>, then call <see cref="Build"/> once
/// from <c>Game.Load</c> (it needs the graphics device). Names are case-sensitive.
/// </remarks>
/// <example>
/// <code>
/// var hero = new PixelImage(16, 16);
/// hero.FillRect(4, 2, 8, 12, Color.White);
/// var atlas = new SpriteAtlasBuilder()
///     .Add("hero", hero, origin: new Vector2(8, 16))
///     .Build(Graphics);
///
/// // In Draw:
/// renderer.Draw(atlas["hero"], new Vector2(160, 120));
/// </code>
/// </example>
public sealed class SpriteAtlasBuilder
{
    private readonly List<(string Name, PixelImage Image, Vector2 Origin)> _entries = [];
    private readonly HashSet<string> _names = new(StringComparer.Ordinal);

    /// <summary>Adds an image under <paramref name="name"/>. The image is copied into the atlas when <see cref="Build"/> runs, so keep it unchanged until then.</summary>
    /// <param name="name">Unique, non-empty sprite name.</param>
    /// <param name="image">The pixels.</param>
    /// <param name="origin">The sprite's anchor in its own pixels (see <see cref="Sprite.Origin"/>); null for the top-left corner.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty or already added.</exception>
    public SpriteAtlasBuilder Add(string name, PixelImage image, Vector2? origin = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(image);
        if (!_names.Add(name)) throw new ArgumentException($"Duplicate sprite '{name}'.", nameof(name));
        _entries.Add((name, image, origin ?? Vector2.Zero));
        return this;
    }

    /// <summary>Adds each frame as "name0", "name1", ... sharing the same origin.</summary>
    public SpriteAtlasBuilder AddFrames(string name, IEnumerable<PixelImage> frames, Vector2? origin = null)
    {
        int i = 0;
        foreach (var frame in frames) Add(name + i++, frame, origin);
        return this;
    }

    /// <summary>
    /// Slices a sprite sheet laid out on a regular grid (row-major) into frames "name0", "name1", ...
    /// <paramref name="count"/> limits the number of cells (default: every cell); fully transparent
    /// cells are skipped only when they are trailing cells of the last row.
    /// </summary>
    public SpriteAtlasBuilder AddGrid(string name, PixelImage sheet, int cellWidth, int cellHeight, int? count = null, Vector2? origin = null)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        if (cellWidth <= 0 || cellHeight <= 0) throw new ArgumentOutOfRangeException(nameof(cellWidth));
        int columns = sheet.Width / cellWidth, rows = sheet.Height / cellHeight;
        int total = Math.Min(count ?? columns * rows, columns * rows);
        if (total == 0) throw new ArgumentException("The sheet is smaller than one cell.", nameof(sheet));
        var frames = Enumerable.Range(0, total).Select(i => sheet.Crop(i % columns * cellWidth, i / columns * cellHeight, cellWidth, cellHeight)).ToList();
        if (count is null)
            while (frames.Count > 1 && frames[^1].OpaqueBounds() is null) frames.RemoveAt(frames.Count - 1);
        return AddFrames(name, frames, origin);
    }

    /// <summary>Number of sprites added so far (each frame counts as one).</summary>
    public int Count => _entries.Count;

    /// <summary>Shelf-packs every image (with 1px padding) into the smallest power-of-two width that fits.</summary>
    public SpriteAtlas Build(IGraphicsDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);
        if (_entries.Count == 0) throw new InvalidOperationException("Atlas has no sprites.");
        var order = _entries.Select((e, i) => (e, i)).OrderByDescending(p => p.e.Image.Height).ThenBy(p => p.i).ToList();
        long area = _entries.Sum(e => (long)(e.Image.Width + 1) * (e.Image.Height + 1));
        int width = 64;
        while ((long)width * width < area || width < _entries.Max(e => e.Image.Width) + 1) width *= 2;
        var places = new (int X, int Y)[_entries.Count];
        int x = 0, y = 0, shelf = 0;
        foreach (var (e, i) in order)
        {
            if (x + e.Image.Width + 1 > width) { x = 0; y += shelf; shelf = 0; }
            places[i] = (x, y);
            x += e.Image.Width + 1;
            shelf = Math.Max(shelf, e.Image.Height + 1);
        }
        int height = 1;
        while (height < y + shelf) height *= 2;
        var sheet = new PixelImage(width, height);
        for (int i = 0; i < _entries.Count; i++) sheet.Blit(_entries[i].Image, places[i].X, places[i].Y);
        var texture = device.CreateTexture(sheet);
        var sprites = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        for (int i = 0; i < _entries.Count; i++)
        {
            var (name, image, origin) = _entries[i];
            sprites[name] = new Sprite(texture, new RectF(places[i].X, places[i].Y, image.Width, image.Height), origin);
        }
        return new SpriteAtlas(texture, sprites);
    }
}

/// <summary>
/// Named sprites packed into one texture, built by <see cref="SpriteAtlasBuilder.Build"/>. The atlas owns the texture:
/// dispose the atlas (usually in <c>Game.Unload</c>) when its sprites are no longer drawn.
/// </summary>
public sealed class SpriteAtlas : IDisposable
{
    private readonly Dictionary<string, Sprite> _sprites;
    private readonly Dictionary<string, Sprite[]> _frames = new(StringComparer.Ordinal);
    /// <summary>The packed texture shared by every sprite of the atlas.</summary>
    public Texture Texture { get; }
    internal SpriteAtlas(Texture texture, Dictionary<string, Sprite> sprites) => (Texture, _sprites) = (texture, sprites);

    /// <summary>The sprite named <paramref name="name"/>.</summary>
    /// <exception cref="KeyNotFoundException">No sprite has that name; check with <see cref="Contains"/>.</exception>
    public Sprite this[string name] => _sprites.TryGetValue(name, out var s) ? s : throw new KeyNotFoundException($"Sprite '{name}' is not in the atlas.");
    /// <summary>Whether a sprite named <paramref name="name"/> exists.</summary>
    public bool Contains(string name) => _sprites.ContainsKey(name);
    /// <summary>Names of every sprite in the atlas.</summary>
    public IReadOnlyCollection<string> Names => _sprites.Keys;

    /// <summary>
    /// The "name0".."nameN" frames added through <see cref="SpriteAtlasBuilder.AddFrames"/>, in order. Collected once per
    /// name and shared, read-only: it can be called every frame.
    /// </summary>
    /// <exception cref="KeyNotFoundException">No sprite is named <paramref name="name"/> followed by 0.</exception>
    public IReadOnlyList<Sprite> Frames(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (_frames.TryGetValue(name, out var cached)) return cached;
        var list = new List<Sprite>();
        for (int i = 0; _sprites.TryGetValue(name + i, out var s); i++) list.Add(s);
        if (list.Count == 0) throw new KeyNotFoundException($"No frames named '{name}N'.");
        return _frames[name] = list.ToArray();
    }

    /// <summary>Disposes the texture; sprites of this atlas must not be drawn afterwards.</summary>
    public void Dispose() => Texture.Dispose();
}

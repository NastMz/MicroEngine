using System.Numerics;

namespace MicroEngine.Graphics;

/// <summary>A region of a texture with a pivot (origin) in source pixels.</summary>
public sealed record Sprite(Texture Texture, RectF Source, Vector2 Origin)
{
    public int Width => (int)Source.Width;
    public int Height => (int)Source.Height;
}

/// <summary>
/// Packs many procedurally authored <see cref="PixelImage"/>s into one texture and
/// exposes them by name. Keeps draw calls batched and art code independent of layout.
/// </summary>
public sealed class SpriteAtlasBuilder
{
    private readonly List<(string Name, PixelImage Image, Vector2 Origin)> _entries = [];
    private readonly HashSet<string> _names = new(StringComparer.Ordinal);

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

public sealed class SpriteAtlas : IDisposable
{
    private readonly Dictionary<string, Sprite> _sprites;
    public Texture Texture { get; }
    internal SpriteAtlas(Texture texture, Dictionary<string, Sprite> sprites) => (Texture, _sprites) = (texture, sprites);

    public Sprite this[string name] => _sprites.TryGetValue(name, out var s) ? s : throw new KeyNotFoundException($"Sprite '{name}' is not in the atlas.");
    public bool Contains(string name) => _sprites.ContainsKey(name);
    public IReadOnlyCollection<string> Names => _sprites.Keys;

    /// <summary>Collects "name0".."nameN" frames added through <see cref="SpriteAtlasBuilder.AddFrames"/>.</summary>
    public Sprite[] Frames(string name)
    {
        var list = new List<Sprite>();
        for (int i = 0; _sprites.TryGetValue(name + i, out var s); i++) list.Add(s);
        if (list.Count == 0) throw new KeyNotFoundException($"No frames named '{name}N'.");
        return list.ToArray();
    }

    public void Dispose() => Texture.Dispose();
}

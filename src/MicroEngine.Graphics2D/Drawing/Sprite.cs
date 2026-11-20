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

    /// <summary>
    /// Shelf-packs every image into the smallest power-of-two width that fits, with a 1-pixel gap between images. Sampled
    /// linearly, each image also gets a 1-pixel border repeating its edge pixels, so scaled sprites and tiles never
    /// show their neighbours' colours or seams.
    /// </summary>
    /// <param name="device">Creates the texture.</param>
    /// <param name="filter">The texture's filter; null for the device's <see cref="IGraphicsDevice.DefaultFilter"/>.</param>
    public SpriteAtlas Build(IGraphicsDevice device, TextureFilter? filter = null)
    {
        ArgumentNullException.ThrowIfNull(device);
        if (_entries.Count == 0) throw new InvalidOperationException("Atlas has no sprites.");
        var sampling = filter ?? device.DefaultFilter;
        int border = sampling == TextureFilter.Linear ? 1 : 0, pad = 1 + 2 * border;
        var order = _entries.Select((e, i) => (e, i)).OrderByDescending(p => p.e.Image.Height).ThenBy(p => p.i).ToList();
        long area = _entries.Sum(e => (long)(e.Image.Width + pad) * (e.Image.Height + pad));
        int width = 64;
        while ((long)width * width < area || width < _entries.Max(e => e.Image.Width) + pad) width *= 2;
        var places = new (int X, int Y)[_entries.Count];
        int x = 0, y = 0, shelf = 0;
        foreach (var (e, i) in order)
        {
            if (x + e.Image.Width + pad > width) { x = 0; y += shelf; shelf = 0; }
            places[i] = (x + border, y + border);
            x += e.Image.Width + pad;
            shelf = Math.Max(shelf, e.Image.Height + pad);
        }
        int height = 1;
        while (height < y + shelf) height *= 2;
        var sheet = new PixelImage(width, height);
        for (int i = 0; i < _entries.Count; i++)
        {
            var image = _entries[i].Image;
            sheet.Blit(image, places[i].X, places[i].Y);
            if (border > 0) Extrude(sheet, image, places[i].X, places[i].Y);
        }
        var texture = device.CreateTexture(sheet, sampling);
        var sprites = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        for (int i = 0; i < _entries.Count; i++)
        {
            var (name, image, origin) = _entries[i];
            sprites[name] = new Sprite(texture, new RectF(places[i].X, places[i].Y, image.Width, image.Height), origin);
        }
        return new SpriteAtlas(texture, sprites);
    }

    /// <summary>Copies the outermost pixels of <paramref name="image"/>, placed at (x, y), one pixel outwards (corners included).</summary>
    private static void Extrude(PixelImage sheet, PixelImage image, int x, int y)
    {
        int w = image.Width, h = image.Height;
        for (int i = -1; i <= w; i++)
        {
            int sx = Math.Clamp(i, 0, w - 1);
            sheet[x + i, y - 1] = image[sx, 0];
            sheet[x + i, y + h] = image[sx, h - 1];
        }
        for (int j = 0; j < h; j++)
        {
            sheet[x - 1, y + j] = image[0, j];
            sheet[x + w, y + j] = image[w - 1, j];
        }
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
    private readonly Dictionary<string, AnimationClip> _clips = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (Sprite[] Frames, float[] Durations)> _tags = new(StringComparer.Ordinal);
    private readonly bool _ownsTexture = true;
    /// <summary>The packed texture shared by every sprite of the atlas.</summary>
    public Texture Texture { get; }
    internal SpriteAtlas(Texture texture, Dictionary<string, Sprite> sprites) => (Texture, _sprites) = (texture, sprites);

    private SpriteAtlas(Texture texture, AtlasData data, bool ownsTexture)
    {
        (Texture, _ownsTexture) = (texture, ownsTexture);
        _sprites = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        var sprites = new Sprite[data.Regions.Count];
        for (int i = 0; i < sprites.Length; i++)
        {
            var region = data.Regions[i];
            var r = region.Source;
            if (r.X < 0 || r.Y < 0 || r.Right > texture.Width || r.Bottom > texture.Height)
                throw new InvalidDataException($"Frame '{region.Name}' ({r.X}, {r.Y}, {r.Width}, {r.Height}) lies outside the {texture.Width}x{texture.Height} atlas image.");
            if (!_sprites.TryAdd(region.Name, sprites[i] = new Sprite(texture, r, region.Origin)))
                throw new InvalidDataException($"The atlas names two frames '{region.Name}'.");
        }
        foreach (var tag in data.Animations)
        {
            var indices = Enumerable.Range(tag.From, tag.To - tag.From + 1).ToList();
            if (tag.Direction is AnimationDirection.Reverse or AnimationDirection.PingPongReverse) indices.Reverse();
            // Ping-pong plays back to the start without repeating either end: 0 1 2 1, then 0 again.
            if (tag.Direction is AnimationDirection.PingPong or AnimationDirection.PingPongReverse && indices.Count > 2)
                indices.AddRange(indices.Skip(1).Take(indices.Count - 2).Reverse().ToList());
            _tags[tag.Name] = ([.. indices.Select(i => sprites[i])], [.. indices.Select(i => data.Regions[i].Duration ?? DefaultFrameDuration)]);
        }
    }

    /// <summary>The duration of tagged frames whose file gives none, in seconds (a tenth of a second).</summary>
    public const float DefaultFrameDuration = 0.1f;

    /// <summary>
    /// Makes an atlas from <paramref name="texture"/> and the frames a packing tool described in <paramref name="data"/>.
    /// </summary>
    /// <param name="texture">The atlas image as a texture.</param>
    /// <param name="data">The frames and tags (<see cref="AtlasData.Parse"/>).</param>
    /// <param name="ownsTexture">Whether disposing the atlas disposes the texture (true by default).</param>
    /// <exception cref="InvalidDataException">A frame lies outside the texture, or two frames share a name.</exception>
    public static SpriteAtlas Create(Texture texture, AtlasData data, bool ownsTexture = true)
    {
        ArgumentNullException.ThrowIfNull(texture);
        ArgumentNullException.ThrowIfNull(data);
        return new SpriteAtlas(texture, data, ownsTexture);
    }

    /// <summary>
    /// Loads an atlas made by a packing tool: its JSON or XML file and the image it names (next to the file), or
    /// <paramref name="imagePath"/>. The atlas owns the texture.
    /// </summary>
    /// <param name="device">Creates the texture.</param>
    /// <param name="path">The atlas file (TexturePacker or Aseprite JSON, Starling XML).</param>
    /// <param name="options">Default pivot and naming; null for <see cref="AtlasOptions.Default"/>.</param>
    /// <param name="filter">The texture's filter; null for the device's default.</param>
    /// <param name="imagePath">The image, when the file names none or names it wrongly; null to use the file's.</param>
    /// <exception cref="FileNotFoundException">The atlas file or its image does not exist.</exception>
    /// <exception cref="InvalidDataException">The file is not an atlas, or does not fit its image; the message names the file.</exception>
    public static SpriteAtlas Load(IGraphicsDevice device, string path, AtlasOptions? options = null, TextureFilter? filter = null, string? imagePath = null)
    {
        ArgumentNullException.ThrowIfNull(device);
        var data = AtlasData.Load(path, options);
        string image = imagePath ?? ImageNextTo(path, data);
        var texture = device.CreateTexture(PixelImage.Load(image), filter ?? device.DefaultFilter);
        try { return new SpriteAtlas(texture, data, ownsTexture: true); }
        catch (InvalidDataException e)
        {
            texture.Dispose();
            throw new InvalidDataException($"{path}: {e.Message}", e);
        }
    }

    /// <summary>The image an atlas file names, resolved next to the file; the file's name with ".png" when it names none.</summary>
    internal static string ImageNextTo(string path, AtlasData data) =>
        Path.Combine(Path.GetDirectoryName(path) ?? "", data.ImagePath ?? Path.GetFileNameWithoutExtension(path) + ".png");

    /// <summary>Names of the animations the atlas file tagged (Aseprite frame tags).</summary>
    public IReadOnlyCollection<string> Animations => _tags.Keys;

    /// <summary>
    /// The clip of the frame tag <paramref name="name"/>, with each frame's duration from the file and its direction
    /// (forward, reverse, ping-pong) applied. Built once per tag and loop setting, then shared.
    /// </summary>
    /// <exception cref="KeyNotFoundException">The atlas has no tag of that name.</exception>
    public AnimationClip Clip(string name, bool loop = true)
    {
        ArgumentNullException.ThrowIfNull(name);
        string key = loop ? name : name + "\0once";
        if (_clips.TryGetValue(key, out var clip)) return clip;
        if (!_tags.TryGetValue(name, out var tag))
            throw new KeyNotFoundException($"The atlas has no animation '{name}' (tags: {(_tags.Count == 0 ? "none" : string.Join(", ", _tags.Keys))}).");
        return _clips[key] = new AnimationClip(tag.Frames, tag.Durations, loop);
    }

    /// <summary>
    /// A clip of the numbered frames "name0", "name1"... (or "name1", "name2"... when the numbering starts at 1, as many
    /// packs do) at <paramref name="framesPerSecond"/>.
    /// </summary>
    /// <exception cref="KeyNotFoundException">No frames have that name.</exception>
    public AnimationClip Clip(string name, float framesPerSecond, bool loop = true) => new(Frames(name), framesPerSecond, loop);

    /// <summary>The sprite named <paramref name="name"/>.</summary>
    /// <exception cref="KeyNotFoundException">No sprite has that name; check with <see cref="Contains"/>.</exception>
    public Sprite this[string name] => _sprites.TryGetValue(name, out var s) ? s : throw new KeyNotFoundException($"Sprite '{name}' is not in the atlas.");
    /// <summary>Whether a sprite named <paramref name="name"/> exists.</summary>
    public bool Contains(string name) => _sprites.ContainsKey(name);
    /// <summary>Names of every sprite in the atlas.</summary>
    public IReadOnlyCollection<string> Names => _sprites.Keys;

    /// <summary>
    /// The "name0".."nameN" frames added through <see cref="SpriteAtlasBuilder.AddFrames"/>, in order; a numbering that
    /// starts at 1 ("walk1", "walk2"... as in many packs) works too. Collected once per name and shared, read-only: it can
    /// be called every frame.
    /// </summary>
    /// <exception cref="KeyNotFoundException">No sprite is named <paramref name="name"/> followed by 0 or 1.</exception>
    public IReadOnlyList<Sprite> Frames(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (_frames.TryGetValue(name, out var cached)) return cached;
        var list = new List<Sprite>();
        int first = _sprites.ContainsKey(name + "0") ? 0 : 1;
        for (int i = first; _sprites.TryGetValue(name + i, out var s); i++) list.Add(s);
        if (list.Count == 0) throw new KeyNotFoundException($"No frames named '{name}N'.");
        return _frames[name] = list.ToArray();
    }

    /// <summary>Disposes the texture (unless the atlas was created with <c>ownsTexture: false</c>); sprites of this atlas must not be drawn afterwards.</summary>
    public void Dispose()
    {
        if (_ownsTexture) Texture.Dispose();
    }
}

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using MicroEngine.Graphics;

namespace MicroEngine.Core;

/// <summary>A content file that could not be loaded: missing, unreadable, in an unsupported format, or named with the wrong letter case.</summary>
public sealed class AssetLoadException : Exception
{
    /// <summary>The path the game asked for, relative to the content folder.</summary>
    public string AssetPath { get; }
    /// <summary>What the game asked for it as ("texture", "atlas", "font"...).</summary>
    public string Kind { get; }

    /// <summary>Records which asset failed and why.</summary>
    public AssetLoadException(string assetPath, string kind, string message, Exception? inner = null)
        : base($"Could not load {kind} '{assetPath}': {message}", inner) => (AssetPath, Kind) = (assetPath, kind);
}

/// <summary>One loaded asset, as <see cref="AssetCache.Loaded"/> lists it.</summary>
/// <param name="Path">Its content path.</param>
/// <param name="Kind">"texture", "atlas", "font" or "folder atlas".</param>
/// <param name="Bytes">Device memory its textures take now (font pages grow as glyphs are drawn).</param>
/// <param name="LoadMilliseconds">Time spent reading, decoding and uploading it.</param>
/// <param name="DuringPlay">Whether it was loaded after the game's first update, when a slow load shows as a hitch.</param>
public readonly record struct AssetInfo(string Path, string Kind, long Bytes, double LoadMilliseconds, bool DuringPlay);

/// <summary>Totals over a scope and its children (<see cref="AssetCache.Statistics"/>).</summary>
/// <param name="Count">Assets loaded and not released.</param>
/// <param name="TextureBytes">Device memory of their textures.</param>
/// <param name="LoadMilliseconds">Time spent loading them.</param>
/// <param name="LoadsDuringPlay">How many were loaded after the first update.</param>
public readonly record struct AssetStatistics(int Count, long TextureBytes, double LoadMilliseconds, int LoadsDuringPlay);

/// <summary>
/// Loads, shares and releases a game's content files (<c>Game.Assets</c>): images as textures, packing tools' atlases,
/// folders of images packed into one atlas, and TTF/OTF fonts. The same request returns the same object, so a texture
/// used by ten scenes is decoded and uploaded once; everything a scope loaded is disposed with it.
/// </summary>
/// <remarks>
/// <para>
/// Paths are relative to the content folder, with '/' separators, and must match the file names' letter case exactly
/// on every system, because Linux, Android and browsers would not find "Hero.png" for "hero.png" even where Windows does.
/// </para>
/// <para>
/// <c>Game.Assets</c> lives as long as the game and is disposed after <c>Unload</c>. For assets of one level or scene,
/// create a child with <see cref="CreateScope"/> and dispose it when the level ends: a child finds what its parents
/// already loaded instead of loading it again, and releases only its own. Loading belongs in <c>Load</c>, a scene's
/// <c>Enter</c> or a loading screen; a load during play is reported, because decoding a large image takes longer than a
/// frame. <see cref="Preload"/> decodes on worker threads where the platform has them and uploads within a time budget
/// per frame, for loading screens.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var hero = Assets.Atlas("atlases/hero.json");            // TexturePacker or Aseprite JSON, Starling XML
/// var sky = Assets.Texture("backgrounds/sky.jpg");
/// var ui = Assets.Font("fonts/Inter-Regular.ttf");
///
/// using var level = Assets.CreateScope();                   // released when the level ends
/// var tiles = level.Atlas("levels/forest/tiles.xml");
/// </code>
/// </example>
public sealed class AssetCache : IDisposable
{
    private readonly IGraphicsDevice _device;
    private readonly string? _root;
    private ContentFolder? _content;
    private readonly AssetCache? _parent;
    private readonly List<AssetCache> _children = [];
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    private readonly Queue<Pending> _uploads = new();
    private readonly HashSet<string> _warned = new(StringComparer.Ordinal);
    private bool _disposed;

    private sealed class Entry(string path, string kind, IDisposable value, double milliseconds, bool duringPlay)
    {
        public readonly string Path = path, Kind = kind;
        public readonly IDisposable Value = value;
        public readonly double Milliseconds = milliseconds;
        public readonly bool DuringPlay = duringPlay;
        public long Bytes => Value switch
        {
            Texture t => t.IsDisposed ? 0 : t.SizeInBytes,
            SpriteAtlas a => a.Texture.IsDisposed ? 0 : a.Texture.SizeInBytes,
            ScalableFont f => f.TextureBytes,
            _ => 0,
        };
    }

    /// <summary>Creates the root scope of a game's assets.</summary>
    /// <param name="device">Creates the textures.</param>
    /// <param name="root">The content folder; null for <see cref="ContentFolder.DefaultRoot"/> or the <c>Content</c> folder next to the executable. It is opened on the first load, so games without content files never need one.</param>
    public AssetCache(IGraphicsDevice device, string? root = null)
    {
        _device = device ?? throw new ArgumentNullException(nameof(device));
        _root = root;
    }

    private AssetCache(AssetCache parent, string? name)
    {
        (_device, _root, _content, _parent, Name) = (parent._device, parent._root, parent._content, parent, name);
        Playing = parent.Playing;
        BackgroundDecoding = parent.BackgroundDecoding;
        FrameBudgetMilliseconds = parent.FrameBudgetMilliseconds;
    }

    /// <summary>The scope's name for diagnostics (null for the root and unnamed scopes).</summary>
    public string? Name { get; }

    /// <summary>The content folder files are read from (opened on first use).</summary>
    /// <exception cref="AssetLoadException">The folder does not exist.</exception>
    public ContentFolder Content
    {
        get
        {
            if (_content is not null) return _content;
            if (_parent is not null) return _content = _parent.Content;
            try { return _content = new ContentFolder(_root); }
            catch (DirectoryNotFoundException e) { throw new AssetLoadException("", "content folder", e.Message, e); }
        }
    }

    /// <summary>
    /// Whether the game is past its first update; loads from then on count as loads during play (<see cref="AssetInfo.DuringPlay"/>).
    /// The engine sets it.
    /// </summary>
    public bool Playing
    {
        get => _playing;
        set
        {
            _playing = value;
            foreach (var child in _children) child.Playing = value;
        }
    }
    private bool _playing;

    /// <summary>
    /// A load during play that takes at least this many milliseconds is reported once on the error output, naming the
    /// file (4 by default: a quarter of a 60 Hz frame). Zero or less turns the reports off.
    /// </summary>
    public double SlowLoadMilliseconds { get; set; } = 4;

    /// <summary>
    /// Whether <see cref="Preload"/> decodes on worker threads (true by default, except in browsers, where .NET runs on
    /// one thread and decoding happens within <see cref="FrameBudgetMilliseconds"/> instead).
    /// </summary>
    public bool BackgroundDecoding { get; set; } = !OperatingSystem.IsBrowser();

    /// <summary>Time per frame <see cref="Update"/> spends finishing preloaded assets (decoding where needed, uploading), in milliseconds; at least one asset finishes per frame.</summary>
    public double FrameBudgetMilliseconds { get; set; } = 4;

    // ---- Loading

    /// <summary>The image at <paramref name="path"/> as a texture, sampled with <paramref name="filter"/> (the game's default when null).</summary>
    /// <exception cref="AssetLoadException">The file is missing, wrongly cased or not an image the engine reads.</exception>
    public Texture Texture(string path, TextureFilter? filter = null)
    {
        var sampling = filter ?? _device.DefaultFilter;
        return Get(path, "texture", Variant(sampling), () => _device.CreateTexture(DecodeImage(path, "texture"), sampling));
    }

    /// <summary>
    /// The image at <paramref name="path"/> decoded into a new <see cref="PixelImage"/> each call (not cached: the game may
    /// change it, for example to recolour it before packing).
    /// </summary>
    /// <exception cref="AssetLoadException">The file is missing, wrongly cased or not an image the engine reads.</exception>
    public PixelImage Image(string path) => DecodeImage(path, "image");

    /// <summary>
    /// The atlas a packing tool described at <paramref name="path"/> (TexturePacker or Aseprite JSON, Starling XML) with the
    /// image it names next to it.
    /// </summary>
    /// <param name="path">The atlas file.</param>
    /// <param name="options">Default pivot and naming; null for <see cref="AtlasOptions.Default"/>.</param>
    /// <param name="filter">The texture's filter; null for the game's default.</param>
    /// <exception cref="AssetLoadException">The file or its image is missing or invalid.</exception>
    public SpriteAtlas Atlas(string path, AtlasOptions? options = null, TextureFilter? filter = null)
    {
        options ??= AtlasOptions.Default;
        var sampling = filter ?? _device.DefaultFilter;
        return Get(path, "atlas", Variant(sampling, options), () =>
        {
            var (data, image) = DecodeAtlas(path, options);
            return BuildAtlas(path, data, image, sampling);
        });
    }

    /// <summary>
    /// Every image directly inside <paramref name="folder"/> packed into one atlas at load, named by file name without its
    /// extension ("items/gem.png" is "gem"): a folder of loose sprites, no packing tool needed.
    /// </summary>
    /// <param name="folder">A folder of the content folder.</param>
    /// <param name="pivot">The anchor of every sprite as a fraction of its size; (0, 0), the top-left, by default.</param>
    /// <param name="filter">The texture's filter; null for the game's default.</param>
    /// <exception cref="AssetLoadException">The folder is missing or holds no image, or an image cannot be read.</exception>
    public SpriteAtlas AtlasFromFolder(string folder, Vector2 pivot = default, TextureFilter? filter = null)
    {
        var sampling = filter ?? _device.DefaultFilter;
        return Get(folder, "folder atlas", Variant(sampling, pivot), () =>
        {
            var files = Files(folder, "folder atlas").Where(f => ImageDecoder.Extensions.Contains(System.IO.Path.GetExtension(f))).ToList();
            if (files.Count == 0) throw new AssetLoadException(folder, "folder atlas", "the folder holds no image.");
            var builder = new SpriteAtlasBuilder();
            foreach (var file in files)
            {
                var image = DecodeImage(file, "folder atlas");
                builder.Add(System.IO.Path.GetFileNameWithoutExtension(file), image, pivot * new Vector2(image.Width, image.Height));
            }
            return builder.Build(_device, sampling);
        });
    }

    /// <summary>The TTF or OTF font at <paramref name="path"/>, drawn at any size (<see cref="ScalableFont"/>).</summary>
    /// <exception cref="AssetLoadException">The file is missing, wrongly cased or not a font the engine reads.</exception>
    public ScalableFont Font(string path, FontOptions? options = null)
    {
        options ??= FontOptions.Default;
        return Get(path, "font", Variant(options), () => OpenFont(path, Read(path, "font"), options));
    }

    /// <summary>The bytes of any content file (not cached).</summary>
    /// <exception cref="AssetLoadException">The file is missing or wrongly cased.</exception>
    public byte[] Bytes(string path) => Read(path, "file");

    /// <summary>The text of any content file, as UTF-8 (not cached).</summary>
    /// <exception cref="AssetLoadException">The file is missing or wrongly cased.</exception>
    public string Text(string path) => System.Text.Encoding.UTF8.GetString(Read(path, "file"));

    /// <summary>Whether this scope or one of its parents has <paramref name="path"/> loaded (in any form).</summary>
    public bool IsLoaded(string path)
    {
        string key = Normalize(path) + "|";
        for (var scope = this; scope is not null; scope = scope._parent)
            if (scope._entries.Keys.Any(k => k.StartsWith(key, StringComparison.Ordinal))) return true;
        return false;
    }

    // ---- Lifecycle

    /// <summary>
    /// A child scope: it reuses what this scope (and its parents) loaded and disposes only what it loads itself, when it is
    /// disposed. Use one per level or scene whose assets should be freed when it ends.
    /// </summary>
    public AssetCache CreateScope(string? name = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var child = new AssetCache(this, name);
        _children.Add(child);
        return child;
    }

    /// <summary>Disposes everything this scope loaded from <paramref name="path"/>; it loads again if asked for later. Parents' assets are not touched.</summary>
    /// <returns>Whether anything was released.</returns>
    public bool Unload(string path)
    {
        string key = Normalize(path) + "|";
        var keys = _entries.Keys.Where(k => k.StartsWith(key, StringComparison.Ordinal)).ToList();
        foreach (var k in keys)
        {
            _entries[k].Value.Dispose();
            _entries.Remove(k);
        }
        return keys.Count > 0;
    }

    /// <summary>Disposes every asset of this scope and of its child scopes. The root scope is disposed by the engine after <c>Game.Unload</c>.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var child in _children.ToList()) child.Dispose();
        _children.Clear();
        foreach (var entry in _entries.Values) entry.Value.Dispose();
        _entries.Clear();
        _uploads.Clear();
        _parent?._children.Remove(this);
    }

    // ---- Diagnostics

    /// <summary>The assets of this scope and its children, in load order within each scope.</summary>
    public IReadOnlyList<AssetInfo> Loaded =>
        [.. _entries.Values.Select(e => new AssetInfo(e.Path, e.Kind, e.Bytes, e.Milliseconds, e.DuringPlay)), .. _children.SelectMany(c => c.Loaded)];

    /// <summary>Counts, texture memory and load time of this scope and its children.</summary>
    public AssetStatistics Statistics
    {
        get
        {
            var all = Loaded;
            return new AssetStatistics(all.Count, all.Sum(a => a.Bytes), all.Sum(a => a.LoadMilliseconds), all.Count(a => a.DuringPlay));
        }
    }

    // ---- Preloading

    /// <summary>
    /// Starts loading <paramref name="paths"/> without blocking: images become textures, ".json" and ".xml" files atlases,
    /// ".ttf" and ".otf" files fonts. Files are read and decoded on worker threads (see <see cref="BackgroundDecoding"/>);
    /// the engine uploads finished ones each frame within <see cref="FrameBudgetMilliseconds"/>. Afterwards the usual
    /// calls (<see cref="Texture"/>, <see cref="Atlas"/>, <see cref="Font"/>) return them at once.
    /// </summary>
    /// <returns>The batch, to show progress on a loading screen or wait for.</returns>
    public AssetBatch Preload(params string[] paths)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(paths);
        var batch = new AssetBatch(this, paths.Length);
        // Opened here, on the game's thread, so a missing folder fails now and workers share one folder.
        _ = Content;
        foreach (var path in paths)
        {
            string ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
            var kind = ImageDecoder.Extensions.Contains(ext) ? "texture" : ext is ".json" or ".xml" ? "atlas" : ext is ".ttf" or ".otf" ? "font"
                : throw new ArgumentException($"'{path}' is not an image, atlas or font file.", nameof(paths));
            var pending = new Pending(batch, path, kind);
            if (FindLoaded(path, kind) is not null) { batch.Complete(null); continue; }
            if (BackgroundDecoding) pending.Decoding = Task.Run(() => pending.Decode(this));
            _uploads.Enqueue(pending);
        }
        return batch;
    }

    /// <summary>
    /// Finishes preloaded assets within <see cref="FrameBudgetMilliseconds"/> (at least one per call). The engine calls it
    /// once per frame for <c>Game.Assets</c>; it also runs child scopes.
    /// </summary>
    public void Update()
    {
        if (_disposed) return;
        long start = Stopwatch.GetTimestamp();
        bool first = true;
        while (_uploads.TryPeek(out var pending))
        {
            if (!first && Stopwatch.GetElapsedTime(start).TotalMilliseconds >= FrameBudgetMilliseconds) break;
            // A worker still decoding: try again next frame rather than wait.
            if (pending.Decoding is { IsCompleted: false }) break;
            _uploads.Dequeue();
            Finish(pending);
            first = false;
        }
        foreach (var child in _children.ToList()) child.Update();
    }

    internal void Drain(AssetBatch batch)
    {
        while (_uploads.Count > 0 && batch.Remaining > 0) Finish(_uploads.Dequeue());
        foreach (var child in _children.ToList()) child.Drain(batch);
    }

    private void Finish(Pending pending)
    {
        try
        {
            if (pending.Decoding is { } task) task.Wait();
            else pending.Decode(this);
            if (pending.Error is { } error) throw error;
            // The keys the direct calls use with default options, so Texture(path) finds a preloaded texture.
            string key = Key(pending.Path, pending.Kind, pending.Kind switch
            {
                "texture" => Variant(_device.DefaultFilter),
                "atlas" => Variant(_device.DefaultFilter, AtlasOptions.Default),
                _ => Variant(FontOptions.Default),
            });
            if (FindLoaded(pending.Path, pending.Kind) is null)
            {
                long started = Stopwatch.GetTimestamp();
                IDisposable value = pending.Kind switch
                {
                    "texture" => _device.CreateTexture(pending.Image!, _device.DefaultFilter),
                    "atlas" => BuildAtlas(pending.Path, pending.Atlas!, pending.Image!, _device.DefaultFilter),
                    _ => OpenFont(pending.Path, pending.Bytes!, FontOptions.Default),
                };
                Store(key, pending.Path, pending.Kind, value, pending.DecodeMilliseconds + Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            }
            pending.Batch.Complete(null);
        }
        catch (Exception e) when (e is AssetLoadException or AggregateException)
        {
            var error = e as AssetLoadException ?? (e.InnerException as AssetLoadException) ?? new AssetLoadException(pending.Path, pending.Kind, e.Message, e);
            pending.Batch.Complete(error);
        }
    }

    private sealed class Pending(AssetBatch batch, string path, string kind)
    {
        public readonly AssetBatch Batch = batch;
        public readonly string Path = path, Kind = kind;
        public Task? Decoding;
        public PixelImage? Image;
        public AtlasData? Atlas;
        public byte[]? Bytes;
        public double DecodeMilliseconds;
        public AssetLoadException? Error;

        /// <summary>Reads and decodes on whichever thread runs it; touches no device.</summary>
        public void Decode(AssetCache owner)
        {
            long started = Stopwatch.GetTimestamp();
            try
            {
                switch (Kind)
                {
                    case "texture": Image = owner.DecodeImage(Path, Kind); break;
                    case "atlas": (Atlas, Image) = owner.DecodeAtlas(Path, AtlasOptions.Default); break;
                    default: Bytes = owner.Read(Path, Kind); break;
                }
            }
            catch (AssetLoadException e) { Error = e; }
            DecodeMilliseconds = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        }
    }

    // ---- Internals

    private T Get<T>(string path, string kind, string variant, Func<T> load) where T : class, IDisposable
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string key = Key(path, kind, variant);
        for (var scope = this; scope is not null; scope = scope._parent)
            if (scope._entries.TryGetValue(key, out var found)) return (T)found.Value;
        long started = Stopwatch.GetTimestamp();
        T value;
        try { value = load(); }
        catch (Exception e) when (e is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException)
        {
            throw new AssetLoadException(Normalize(path), kind, e.Message, e);
        }
        Store(key, path, kind, value, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        return value;
    }

    private void Store(string key, string path, string kind, IDisposable value, double milliseconds)
    {
        _entries[key] = new Entry(Normalize(path), kind, value, milliseconds, Playing);
        if (Playing && SlowLoadMilliseconds > 0 && milliseconds >= SlowLoadMilliseconds && _warned.Add(key))
            Console.Error.WriteLine($"assets: {kind} '{Normalize(path)}' was loaded during play and took {milliseconds:0.0} ms (a visible hitch); load it in Load, or Preload it behind a loading screen.");
    }

    private IDisposable? FindLoaded(string path, string kind)
    {
        string prefix = Normalize(path) + "|" + kind + "|";
        for (var scope = this; scope is not null; scope = scope._parent)
            foreach (var (key, entry) in scope._entries)
                if (key.StartsWith(prefix, StringComparison.Ordinal)) return entry.Value;
        return null;
    }

    private static string Key(string path, string kind, string variant) => Normalize(path) + "|" + kind + "|" + variant;
    private static string Variant(params object?[] parts) => string.Join(";", parts.Select(p => p?.ToString() ?? ""));
    private static string Normalize(string path)
    {
        path = path.Replace('\\', '/').TrimStart('/');
        while (path.StartsWith("./", StringComparison.Ordinal)) path = path[2..];
        return path;
    }

    private byte[] Read(string path, string kind)
    {
        try { return Content.ReadBytes(Normalize(path)); }
        catch (Exception e) when (e is IOException or ArgumentException or UnauthorizedAccessException)
        {
            throw new AssetLoadException(Normalize(path), kind, e.Message, e);
        }
    }

    private PixelImage DecodeImage(string path, string kind)
    {
        var bytes = Read(path, kind);
        try { return PixelImage.Decode(bytes); }
        catch (InvalidDataException e) { throw new AssetLoadException(Normalize(path), kind, e.Message, e); }
    }

    private (AtlasData Data, PixelImage Image) DecodeAtlas(string path, AtlasOptions options)
    {
        AtlasData data;
        try { data = AtlasData.Parse(System.Text.Encoding.UTF8.GetString(Read(path, "atlas")), options); }
        catch (InvalidDataException e) { throw new AssetLoadException(Normalize(path), "atlas", e.Message, e); }
        string folder = System.IO.Path.GetDirectoryName(Normalize(path))?.Replace('\\', '/') ?? "";
        string image = data.ImagePath ?? System.IO.Path.GetFileNameWithoutExtension(path) + ".png";
        string imagePath = folder.Length == 0 ? image : folder + "/" + image;
        try { return (data, DecodeImage(imagePath, "atlas")); }
        catch (AssetLoadException e) { throw new AssetLoadException(Normalize(path), "atlas", $"its image: {e.Message}", e); }
    }

    private SpriteAtlas BuildAtlas(string path, AtlasData data, PixelImage image, TextureFilter filter)
    {
        var texture = _device.CreateTexture(image, filter);
        try { return SpriteAtlas.Create(texture, data); }
        catch (InvalidDataException e)
        {
            texture.Dispose();
            throw new AssetLoadException(Normalize(path), "atlas", e.Message, e);
        }
    }

    private ScalableFont OpenFont(string path, byte[] bytes, FontOptions options)
    {
        try { return ScalableFont.FromBytes(_device, bytes, options, Normalize(path)); }
        catch (InvalidDataException e) { throw new AssetLoadException(Normalize(path), "font", e.Message, e); }
    }

    private IReadOnlyList<string> Files(string folder, string kind)
    {
        try { return Content.Files(Normalize(folder)); }
        catch (Exception e) when (e is IOException or ArgumentException)
        {
            throw new AssetLoadException(Normalize(folder), kind, e.Message, e);
        }
    }
}

/// <summary>Assets started by <see cref="AssetCache.Preload"/>: progress for a loading screen, errors, and a way to wait.</summary>
public sealed class AssetBatch
{
    private readonly AssetCache _owner;
    private readonly List<AssetLoadException> _errors = [];

    internal AssetBatch(AssetCache owner, int count) => (_owner, Count) = (owner, count);

    /// <summary>How many assets the batch holds.</summary>
    public int Count { get; }
    /// <summary>How many are finished (loaded or failed).</summary>
    public int Completed { get; private set; }
    internal int Remaining => Count - Completed;
    /// <summary>Finished share, from 0 to 1 (1 for an empty batch).</summary>
    public float Progress => Count == 0 ? 1 : Completed / (float)Count;
    /// <summary>Whether every asset is finished.</summary>
    public bool IsDone => Completed >= Count;
    /// <summary>The assets that could not be loaded.</summary>
    public IReadOnlyList<AssetLoadException> Errors => _errors;

    internal void Complete(AssetLoadException? error)
    {
        Completed++;
        if (error is not null) _errors.Add(error);
    }

    /// <summary>Finishes every asset of the batch now, on this thread (decoding what workers have not finished).</summary>
    /// <exception cref="AggregateException">Some assets could not be loaded; <see cref="Errors"/> lists them.</exception>
    public void Wait()
    {
        _owner.Drain(this);
        if (_errors.Count > 0) throw new AggregateException("Some assets could not be loaded.", _errors);
    }
}

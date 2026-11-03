using System.Diagnostics;
using System.Numerics;
using MicroEngine.Content;
using MicroEngine.Graphics;
using MicroEngine.Platform;

namespace MicroEngine.Core;

/// <summary>One loaded asset, as <see cref="AssetCache.Loaded"/> lists it.</summary>
/// <param name="Path">Its content path.</param>
/// <param name="Kind">"texture", "atlas", "font", "folder atlas", or the kind of another loader (<see cref="AssetLoaderRegistry"/>).</param>
/// <param name="Bytes">Memory it takes now, as its loader reports it: device memory for textures (font pages grow as glyphs are drawn), decoded samples for sounds, the file for music.</param>
/// <param name="LoadMilliseconds">Time spent reading, decoding and uploading it.</param>
/// <param name="DuringPlay">Whether it was loaded after the game's first update, when a slow load shows as a hitch.</param>
public readonly record struct AssetInfo(string Path, string Kind, long Bytes, double LoadMilliseconds, bool DuringPlay);

/// <summary>Totals over a scope and its children (<see cref="AssetCache.Statistics"/>).</summary>
/// <param name="Count">Assets loaded and not released.</param>
/// <param name="TextureBytes">Device memory of their textures (images, atlases, font pages).</param>
/// <param name="LoadMilliseconds">Time spent loading them.</param>
/// <param name="LoadsDuringPlay">How many were loaded after the first update.</param>
public readonly record struct AssetStatistics(int Count, long TextureBytes, double LoadMilliseconds, int LoadsDuringPlay)
{
    /// <summary>Memory of the audio they hold: decoded sound samples and the bytes of music files (kinds "sound" and "music").</summary>
    public long AudioBytes { get; init; }
}

/// <summary>
/// Loads, shares and releases a game's content files (<c>Game.Assets</c>): images as textures, packing tools' atlases,
/// folders of images packed into one atlas, and TTF/OTF fonts. The same request returns the same object, so a texture
/// used by ten scenes is decoded and uploaded once, whichever scopes ask for it; an asset is disposed when the last scope
/// holding it releases it.
/// </summary>
/// <remarks>
/// <para>
/// Paths are relative to the content source, with '/' separators, and must match the file names' letter case exactly
/// on every system, because Linux, Android and browsers would not find "Hero.png" for "hero.png" even where Windows does.
/// The content is a folder by default; the composition may name another source, such as a zip archive
/// (<see cref="Hosting.HostBuilder.Content"/>, <see cref="ZipContentSource"/>).
/// </para>
/// <para>
/// <c>Game.Assets</c> lives as long as the game and is disposed after <c>Unload</c>. For assets of one level or scene,
/// create a child with <see cref="CreateScope"/> and dispose it when the level ends. Scopes share: a scope that asks for
/// an asset another scope (a parent, a sibling) already loaded gets the same object instead of loading it again, and holds
/// it too; disposing a scope or calling <see cref="Unload"/> releases its hold, and an asset is disposed when no scope
/// holds it any more. Loading belongs in <c>Load</c>, a scene's
/// <c>Enter</c> or a loading screen; a load during play is reported, because decoding a large image takes longer than a
/// frame. <see cref="Preload"/> decodes on worker threads where the platform has them and uploads within a time budget
/// per frame, for loading screens.
/// </para>
/// <para>
/// The cache is built on the engine's asset store (<see cref="Store"/>): every scope is an <see cref="AssetScope"/>
/// (<see cref="Scope"/>), so typed handles (<see cref="Asset{T}"/>) and loaders of other kinds registered in
/// <see cref="Loaders"/> share the same lifetimes.
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
    private IContentSource? _composed;
    private ContentFolder? _folder;
    private readonly AssetCache? _parent;
    private readonly AssetScope _scope;
    private readonly List<AssetCache> _children = [];
    private readonly Queue<Pending> _uploads = new();
    private bool _disposed;

    /// <summary>Creates the root scope of a game's assets.</summary>
    /// <param name="device">Creates the textures.</param>
    /// <param name="root">The content folder; null for the composition's (<see cref="Hosting.HostBuilder.Content"/>) or the <c>Content</c> folder next to the executable. It is opened on the first load, so games without content files never need one.</param>
    public AssetCache(IGraphicsDevice device, string? root = null)
    {
        _device = device ?? throw new ArgumentNullException(nameof(device));
        _root = root;
        var loaders = new AssetLoaderRegistry();
        GraphicsAssetLoaders.Register(loaders, device);
        Audio.AudioAssetLoaders.Register(loaders);
        _scope = new AssetStore(OpenSource, loaders).Root;
    }

    private AssetCache(AssetCache parent, string? name)
    {
        (_device, _root, _parent) = (parent._device, parent._root, parent);
        _scope = parent._scope.CreateScope(name);
        BackgroundDecoding = parent.BackgroundDecoding;
        FrameBudgetMilliseconds = parent.FrameBudgetMilliseconds;
    }

    /// <summary>The scope's name for diagnostics (null for the root and unnamed scopes).</summary>
    public string? Name => _scope.Name;

    /// <summary>The asset scope behind this cache scope: typed handles (<see cref="AssetScope.Load{T}"/>) with the same holds and lifetimes.</summary>
    public AssetScope Scope => _scope;

    /// <summary>The game's asset store: its content source, loaders and root scope.</summary>
    public AssetStore Store => _scope.Store;

    /// <summary>The loaders of the store, one per kind: "texture", "atlas", "folder atlas", "font", "sound", "music" and any a game registers.</summary>
    public AssetLoaderRegistry Loaders => Store.Loaders;

    /// <summary>Where content files are read from (opened on first use): a folder, a zip archive or another source of the composition.</summary>
    /// <exception cref="AssetLoadException">The content folder does not exist.</exception>
    public IContentSource Source => Store.Source;

    /// <summary>The content folder files are read from (opened on first use).</summary>
    /// <exception cref="AssetLoadException">The folder does not exist.</exception>
    /// <exception cref="InvalidOperationException">The content is not a folder (a zip archive, for example); use <see cref="Source"/>.</exception>
    public ContentFolder Content
    {
        get
        {
            if (_folder is not null) return _folder;
            return _folder = Source switch
            {
                ContentFolder folder => folder,
                FolderContentSource folder => new ContentFolder(folder.Root),
                var other => throw new InvalidOperationException($"The content comes from {other.Location} ({other.GetType().Name}), not a folder; read it through Assets.Source."),
            };
        }
    }

    /// <summary>The game's folder, the composition's source, or the <c>Content</c> folder next to the executable.</summary>
    private IContentSource OpenSource()
    {
        try { return _root is not null ? new ContentFolder(_root) : _composed ?? new ContentFolder(); }
        catch (DirectoryNotFoundException e) { throw new AssetLoadException("", "content folder", e.Message, e); }
    }

    /// <summary>The composition's content (<see cref="Hosting.HostBuilder.Content"/>), used when the game names no folder of its own: a folder, a zip archive or any other source.</summary>
    internal void UseComposedContent(IContentSource source) => _composed = source ?? throw new ArgumentNullException(nameof(source));

    /// <summary>Where warnings about loads during play go: the game's <see cref="Kernel.ILog"/>, or standard error when none was set.</summary>
    internal Kernel.ILog? Log
    {
        get => Store.Log;
        set => Store.Log = value;
    }

    /// <summary>
    /// Whether the game is past its first update; loads from then on count as loads during play (<see cref="AssetInfo.DuringPlay"/>).
    /// The engine sets it.
    /// </summary>
    public bool Playing
    {
        get => _scope.Playing;
        set => _scope.Playing = value;
    }

    /// <summary>
    /// A load during play that takes at least this many milliseconds is reported once on the error output, naming the
    /// file (4 by default: a quarter of a 60 Hz frame). Zero or less turns the reports off.
    /// </summary>
    public double SlowLoadMilliseconds
    {
        get => _scope.SlowLoadMilliseconds;
        set => _scope.SlowLoadMilliseconds = value;
    }

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
    public Texture Texture(string path, TextureFilter? filter = null) =>
        _scope.Acquire<Texture>(new AssetKey(GraphicsAssetLoaders.Texture, AssetPath.Normalize(path), null, (long)(filter ?? _device.DefaultFilter)));

    /// <summary>
    /// The image at <paramref name="path"/> decoded into a new <see cref="PixelImage"/> each call (not cached: the game may
    /// change it, for example to recolour it before packing).
    /// </summary>
    /// <exception cref="AssetLoadException">The file is missing, wrongly cased or not an image the engine reads.</exception>
    public PixelImage Image(string path) => GraphicsAssetLoaders.DecodeImage(Source, AssetPath.Normalize(path), "image");

    /// <summary>
    /// The atlas a packing tool described at <paramref name="path"/> (TexturePacker or Aseprite JSON, Starling XML) with the
    /// image it names next to it.
    /// </summary>
    /// <param name="path">The atlas file.</param>
    /// <param name="options">Default pivot and naming; null for <see cref="AtlasOptions.Default"/>.</param>
    /// <param name="filter">The texture's filter; null for the game's default.</param>
    /// <exception cref="AssetLoadException">The file or its image is missing or invalid.</exception>
    public SpriteAtlas Atlas(string path, AtlasOptions? options = null, TextureFilter? filter = null) =>
        _scope.Acquire<SpriteAtlas>(new AssetKey(GraphicsAssetLoaders.Atlas, AssetPath.Normalize(path), options ?? AtlasOptions.Default, (long)(filter ?? _device.DefaultFilter)));

    /// <summary>
    /// Every image directly inside <paramref name="folder"/> packed into one atlas at load, named by file name without its
    /// extension ("items/gem.png" is "gem"): a folder of loose sprites, no packing tool needed.
    /// </summary>
    /// <param name="folder">A folder of the content.</param>
    /// <param name="pivot">The anchor of every sprite as a fraction of its size; (0, 0), the top-left, by default.</param>
    /// <param name="filter">The texture's filter; null for the game's default.</param>
    /// <exception cref="AssetLoadException">The folder is missing or holds no image, or an image cannot be read.</exception>
    public SpriteAtlas AtlasFromFolder(string folder, Vector2 pivot = default, TextureFilter? filter = null) =>
        _scope.Acquire<SpriteAtlas>(new AssetKey(GraphicsAssetLoaders.FolderAtlas, AssetPath.Normalize(folder), pivot, (long)(filter ?? _device.DefaultFilter)));

    /// <summary>The TTF or OTF font at <paramref name="path"/>, drawn at any size (<see cref="ScalableFont"/>).</summary>
    /// <exception cref="AssetLoadException">The file is missing, wrongly cased or not a font the engine reads.</exception>
    public ScalableFont Font(string path, FontOptions? options = null) =>
        _scope.Acquire<ScalableFont>(new AssetKey(GraphicsAssetLoaders.Font, AssetPath.Normalize(path), options ?? FontOptions.Default));

    /// <summary>The bytes of any content file (not cached).</summary>
    /// <exception cref="AssetLoadException">The file is missing or wrongly cased.</exception>
    public byte[] Bytes(string path) => GraphicsAssetLoaders.Read(Source, AssetPath.Normalize(path), "file");

    /// <summary>The text of any content file, as UTF-8 (not cached).</summary>
    /// <exception cref="AssetLoadException">The file is missing or wrongly cased.</exception>
    public string Text(string path) => System.Text.Encoding.UTF8.GetString(Bytes(path));

    /// <summary>Whether this scope or one of its parents has <paramref name="path"/> loaded (in any form).</summary>
    public bool IsLoaded(string path) => _scope.IsLoaded(path);

    // ---- Lifecycle

    /// <summary>
    /// A child scope: an asset it asks for that another scope already loaded is the same object, and disposing the scope
    /// releases what it holds, disposing only what no other scope holds. Use one per level or scene whose assets should be
    /// freed when it ends.
    /// </summary>
    public AssetCache CreateScope(string? name = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var child = new AssetCache(this, name);
        _children.Add(child);
        return child;
    }

    /// <summary>
    /// Releases what this scope holds from <paramref name="path"/> (in any form): each object is disposed unless another
    /// scope still holds it, and loads again if asked for later. What only other scopes hold is not touched.
    /// </summary>
    /// <returns>Whether this scope held anything from the file.</returns>
    public bool Unload(string path) => _scope.Release(path);

    /// <summary>
    /// Releases every asset this scope and its child scopes hold, disposing those no other scope holds. The root scope is
    /// disposed by the engine after <c>Game.Unload</c>.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var child in _children.ToList()) child.Dispose();
        _children.Clear();
        _scope.Dispose();
        _uploads.Clear();
        _parent?._children.Remove(this);
    }

    // ---- Diagnostics

    /// <summary>
    /// How many assets this scope and its children hold: those they loaded and those they asked for that another scope
    /// (a parent included) had loaded, an asset several of them share counting once. On the root scope, every asset of
    /// the game. Allocates nothing, so it can be checked every frame.
    /// </summary>
    public int Count => _scope.Count;

    /// <summary>The assets of this scope and its children, each once, in the order each scope asked for them.</summary>
    public IReadOnlyList<AssetInfo> Loaded => [.. _scope.Loaded.Select(r => new AssetInfo(r.Path, r.Kind, r.Bytes, r.LoadMilliseconds, r.DuringPlay))];

    /// <summary>Counts, memory and load time of this scope and its children.</summary>
    public AssetStatistics Statistics
    {
        get
        {
            var all = _scope.Loaded;
            return new AssetStatistics(all.Count, all.Where(a => GraphicsAssetLoaders.IsGraphics(a.Kind)).Sum(a => a.Bytes),
                all.Sum(a => a.LoadMilliseconds), all.Count(a => a.DuringPlay))
            {
                AudioBytes = all.Where(a => a.Kind is Audio.AudioAssetLoaders.Sound or Audio.AudioAssetLoaders.Music).Sum(a => a.Bytes),
            };
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
        // Opened here, on the game's thread, so a missing folder fails now and workers share one source.
        var source = Source;
        foreach (var path in paths)
        {
            string ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
            var kind = ImageDecoder.Extensions.Contains(ext) ? GraphicsAssetLoaders.Texture : ext is ".json" or ".xml" ? GraphicsAssetLoaders.Atlas : ext is ".ttf" or ".otf" ? GraphicsAssetLoaders.Font
                : throw new ArgumentException($"'{path}' is not an image, atlas or font file.", nameof(paths));
            var pending = new Pending(batch, AssetPath.Normalize(path), kind);
            if (_scope.HoldLoaded(kind, pending.Path)) { batch.Complete(null); continue; }
            if (BackgroundDecoding) pending.Decoding = Task.Run(() => pending.Decode(source));
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
        long finishing = Stopwatch.GetTimestamp();
        try
        {
            if (pending.Decoding is { } task) task.Wait();
            else pending.Decode(Source);
            if (pending.Error is { } error) throw error;
            // The keys the direct calls use with default options, so Texture(path) finds a preloaded texture.
            var filter = (long)_device.DefaultFilter;
            var key = pending.Kind switch
            {
                GraphicsAssetLoaders.Texture => new AssetKey(pending.Kind, pending.Path, null, filter),
                GraphicsAssetLoaders.Atlas => new AssetKey(pending.Kind, pending.Path, AtlasOptions.Default, filter),
                _ => new AssetKey(pending.Kind, pending.Path, FontOptions.Default),
            };
            if (!_scope.HoldLoaded(pending.Kind, pending.Path))
            {
                IDisposable value = pending.Kind switch
                {
                    GraphicsAssetLoaders.Texture => _device.CreateTexture(pending.Image!, _device.DefaultFilter),
                    GraphicsAssetLoaders.Atlas => GraphicsAssetLoaders.BuildAtlas(_device, pending.Path, pending.Atlas!, pending.Image!, _device.DefaultFilter),
                    _ => GraphicsAssetLoaders.OpenFont(_device, pending.Path, pending.Bytes!, FontOptions.Default),
                };
                // The load time counts the worker's decoding; a hitch is only what this thread spent (all of it without workers).
                double blocking = Stopwatch.GetElapsedTime(finishing).TotalMilliseconds;
                _scope.Add(key, value, (pending.Decoding is null ? 0 : pending.DecodeMilliseconds) + blocking, blocking);
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
        public void Decode(IContentSource source)
        {
            long started = Stopwatch.GetTimestamp();
            try
            {
                switch (Kind)
                {
                    case GraphicsAssetLoaders.Texture: Image = GraphicsAssetLoaders.DecodeImage(source, Path, Kind); break;
                    case GraphicsAssetLoaders.Atlas: (Atlas, Image) = GraphicsAssetLoaders.DecodeAtlas(source, Path, AtlasOptions.Default); break;
                    default: Bytes = GraphicsAssetLoaders.Read(source, Path, Kind); break;
                }
            }
            catch (AssetLoadException e) { Error = e; }
            catch (Exception e) when (e is not OutOfMemoryException) { Error = new AssetLoadException(Path, Kind, e.Message, e); }
            DecodeMilliseconds = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
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

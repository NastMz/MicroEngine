using System.Diagnostics;
using System.Numerics;
using MicroEngine.Content;
using MicroEngine.Graphics;
using MicroEngine.Platform;

namespace MicroEngine.Core;

/// <summary>One loaded asset, as <see cref="AssetCache.Loaded"/> lists it.</summary>
/// <param name="Path">Its content path.</param>
/// <param name="Kind">"texture", "atlas", "font", "folder atlas", "shader", "material", "render target", or the kind of another loader (<see cref="AssetLoaderRegistry"/>).</param>
/// <param name="Bytes">Memory it takes now, as its loader reports it: device memory for textures (font pages grow as glyphs are drawn), decoded samples for sounds, the file for music.</param>
/// <param name="LoadMilliseconds">Time spent reading, decoding and uploading it.</param>
/// <param name="DuringPlay">Whether it was loaded after the game's first update, when a slow load shows as a hitch.</param>
public readonly record struct AssetInfo(string Path, string Kind, long Bytes, double LoadMilliseconds, bool DuringPlay);

/// <summary>The size of a render target of the assets (the options of a "render target" asset key).</summary>
/// <param name="Width">Width in pixels.</param>
/// <param name="Height">Height in pixels.</param>
public readonly record struct RenderTargetSize(int Width, int Height);

/// <summary>Totals over a scope and its children (<see cref="AssetCache.Statistics"/>).</summary>
/// <param name="Count">Assets loaded and not released.</param>
/// <param name="TextureBytes">Device memory of their textures (images, atlases, font pages, render targets).</param>
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
/// frame. <see cref="Preload(string[])"/> decodes on worker threads where the platform has them and uploads within a time budget
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
    // The children as they were when a frame update or a drain started (scopes may be created or disposed while it runs);
    // reused so the frame update makes no garbage. Null while a visit uses it.
    private AssetCache[]? _visit = [];
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

    /// <summary>The loaders of the store, one per kind: "texture", "atlas", "folder atlas", "font", "shader", "material", "render target", "sound", "music" and any a game registers.</summary>
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
    /// Whether <see cref="Preload(string[])"/> decodes on worker threads (true by default, except in browsers, where .NET runs on
    /// one thread and decoding happens within <see cref="FrameBudgetMilliseconds"/> instead).
    /// </summary>
    public bool BackgroundDecoding { get; set; } = !OperatingSystem.IsBrowser();

    /// <summary>Time per frame <see cref="Update"/> spends finishing preloaded assets (decoding where needed, uploading), in milliseconds; at least one asset finishes per frame.</summary>
    public double FrameBudgetMilliseconds { get; set; } = 4;

    // ---- Loading

    /// <summary>The image at <paramref name="path"/> as a texture, sampled with <paramref name="filter"/> (the game's default when null).</summary>
    /// <exception cref="AssetLoadException">The file is missing, wrongly cased or not an image the engine reads.</exception>
    public Texture Texture(string path, TextureFilter? filter = null) =>
        _scope.Acquire<Texture>(TextureKey(path, filter));

    /// <summary>
    /// The image at <paramref name="path"/> decoded into a new <see cref="PixelImage"/> each call (not cached: the game may
    /// change it, for example to recolour it before packing).
    /// </summary>
    /// <exception cref="AssetLoadException">The file is missing, wrongly cased or not an image the engine reads.</exception>
    public PixelImage Image(string path) => GraphicsAssetLoaders.DecodeImage(Source, AssetPath.Normalize(path), "image");

    /// <summary>
    /// The atlas a packing tool described at <paramref name="path"/> (TexturePacker or Aseprite JSON, Starling XML) with the
    /// image it names next to it; or the sprite sheets a <c>.sheet</c> file describes (<see cref="SpriteSheet"/>), sliced
    /// and packed into one atlas with their clips.
    /// </summary>
    /// <param name="path">The atlas or sheet file.</param>
    /// <param name="options">Default pivot and naming; null for <see cref="AtlasOptions.Default"/>.</param>
    /// <param name="filter">The texture's filter; null for the game's default.</param>
    /// <exception cref="AssetLoadException">The file or its image is missing or invalid.</exception>
    public SpriteAtlas Atlas(string path, AtlasOptions? options = null, TextureFilter? filter = null) =>
        _scope.Acquire<SpriteAtlas>(AtlasKey(path, options, filter));

    /// <summary>
    /// The frames and clips of the sheet at <paramref name="path"/> as pixels, new each call (not cached: the game may
    /// change them): a <c>.sheet</c> description, or an artist's file a registered loader reads as a sprite sheet. Pack
    /// them with others through <see cref="SpriteAtlasBuilder.AddSheet"/>; <see cref="Atlas"/> loads the same file
    /// straight into an atlas.
    /// </summary>
    /// <param name="path">The sheet file.</param>
    /// <param name="options">The pivot of frames the file gives none; null for <see cref="AtlasOptions.Default"/>.</param>
    /// <exception cref="AssetLoadException">The file or an image it names is missing or invalid, or no loader reads that kind of file as a sheet.</exception>
    public SpriteSheet Sheet(string path, AtlasOptions? options = null)
    {
        string normalized = AssetPath.Normalize(path);
        var loader = GraphicsAssetLoaders.SheetLoader(Loaders, normalized)
            ?? throw new AssetLoadException(normalized, GraphicsAssetLoaders.Sheet, $"no loader reads '{System.IO.Path.GetExtension(normalized)}' files as sprite sheets (a .sheet description does; other formats come with their package).");
        return Store.Read<SpriteSheet>(new AssetKey(loader.Kind, normalized, options ?? AtlasOptions.Default));
    }

    /// <summary>
    /// Every image directly inside <paramref name="folder"/> packed into one atlas at load, named by file name without its
    /// extension ("items/gem.png" is "gem"): a folder of loose sprites, no packing tool needed.
    /// </summary>
    /// <param name="folder">A folder of the content.</param>
    /// <param name="pivot">The anchor of every sprite as a fraction of its size; (0, 0), the top-left, by default.</param>
    /// <param name="filter">The texture's filter; null for the game's default.</param>
    /// <exception cref="AssetLoadException">The folder is missing or holds no image, or an image cannot be read.</exception>
    public SpriteAtlas AtlasFromFolder(string folder, Vector2 pivot = default, TextureFilter? filter = null) =>
        _scope.Acquire<SpriteAtlas>(FolderAtlasKey(folder, pivot, filter));

    /// <summary>The TTF or OTF font at <paramref name="path"/>, drawn at any size (<see cref="ScalableFont"/>).</summary>
    /// <exception cref="AssetLoadException">The file is missing, wrongly cased or not a font the engine reads.</exception>
    public ScalableFont Font(string path, FontOptions? options = null) =>
        _scope.Acquire<ScalableFont>(FontKey(path, options));

    /// <summary>The PCM WAV file at <paramref name="path"/> decoded into a clip, shared and released like any asset; play it with <c>Audio.AddSound(name, clip)</c>.</summary>
    /// <exception cref="AssetLoadException">The file is missing, wrongly cased or not an 8, 16 or 24-bit PCM WAV.</exception>
    public Audio.AudioClip Sound(string path) =>
        _scope.Acquire<Audio.AudioClip>(PlainKey(Audio.AudioAssetLoaders.Sound, path));

    /// <summary>
    /// The music file at <paramref name="path"/> (OGG, WAV; MP3 on raylib), read whole and decoded while it plays; play
    /// it with <c>Audio.AddMusic(name, music)</c>.
    /// </summary>
    /// <exception cref="AssetLoadException">The file is missing or wrongly cased.</exception>
    public Audio.EncodedAudio Music(string path) =>
        _scope.Acquire<Audio.EncodedAudio>(PlainKey(Audio.AudioAssetLoaders.Music, path));

    /// <summary>
    /// The compiled shader file at <paramref name="path"/> (written by <c>microengine shaders</c>), parsed once and
    /// shared. A material needs only <see cref="Material"/>; this is the file itself, for a game that creates shaders
    /// on the graphics device.
    /// </summary>
    /// <exception cref="AssetLoadException">The file is missing, wrongly cased or not a compiled shader this engine reads.</exception>
    public Graphics.Devices.CompiledShader Shader(string path) =>
        _scope.Acquire<Graphics.Devices.CompiledShader>(PlainKey(GraphicsAssetLoaders.Shader, path));

    /// <summary>
    /// A material from the compiled fragment shader at <paramref name="path"/> (<see cref="Material2D"/>): draw with it
    /// through <see cref="IRenderer.PushState"/>. It is shared like any asset (the same path and fallback give the same
    /// material, whose parameters each draw takes as they are when it is made) and released with its scope. Where the
    /// graphics device cannot run shaders it draws with <paramref name="fallback"/>.
    /// </summary>
    /// <param name="path">A <c>.frag.shader</c> file of the content.</param>
    /// <param name="fallback">What the material's draws become where shaders cannot run; plain draws by default.</param>
    /// <exception cref="AssetLoadException">The file is missing, wrongly cased, not a compiled shader, or not a fragment shader.</exception>
    public Material2D Material(string path, MaterialFallback fallback = default) =>
        _scope.Acquire<Material2D>(MaterialKey(path, fallback));

    /// <summary>
    /// A render target of <paramref name="width"/> x <paramref name="height"/> pixels that this scope owns: a picture to
    /// draw into (<see cref="IRenderer.PushTarget"/>) and then draw like a texture. No file describes it, so it is known
    /// by <paramref name="name"/>: the same name, size and filter give the same target, and it is released with the
    /// scope like any asset.
    /// </summary>
    /// <param name="name">A name of the game's choosing ("water/reflection"); it is what diagnostics list.</param>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <param name="filter">How its texture is sampled; null for the game's default.</param>
    /// <exception cref="AssetLoadException">A size that is not positive.</exception>
    public RenderTarget2D RenderTarget(string name, int width, int height, TextureFilter? filter = null) =>
        _scope.Acquire<RenderTarget2D>(RenderTargetKey(name, width, height, filter));

    // The key of each kind, for the calls above and for preloads: the same request is the same key.
    internal AssetKey TextureKey(string path, TextureFilter? filter) => new(GraphicsAssetLoaders.Texture, AssetPath.Normalize(path), null, (long)(filter ?? _device.DefaultFilter));
    internal AssetKey AtlasKey(string path, AtlasOptions? options, TextureFilter? filter) => new(GraphicsAssetLoaders.Atlas, AssetPath.Normalize(path), options ?? AtlasOptions.Default, (long)(filter ?? _device.DefaultFilter));
    internal AssetKey FolderAtlasKey(string folder, Vector2 pivot, TextureFilter? filter) => new(GraphicsAssetLoaders.FolderAtlas, AssetPath.Normalize(folder), pivot, (long)(filter ?? _device.DefaultFilter));
    internal static AssetKey FontKey(string path, FontOptions? options) => new(GraphicsAssetLoaders.Font, AssetPath.Normalize(path), options ?? FontOptions.Default);
    internal static AssetKey MaterialKey(string path, MaterialFallback fallback) => new(GraphicsAssetLoaders.Material, AssetPath.Normalize(path), fallback);
    internal AssetKey RenderTargetKey(string name, int width, int height, TextureFilter? filter) => new(GraphicsAssetLoaders.RenderTarget, AssetPath.Normalize(name), new RenderTargetSize(width, height), (long)(filter ?? _device.DefaultFilter));
    internal static AssetKey PlainKey(string kind, string path) => new(kind, AssetPath.Normalize(path));

    /// <summary>
    /// The key a file is loaded under when only its path is given: the kind of the loader its extension chooses, with
    /// that kind's default options. A file a loader reads as a sprite sheet is the atlas it makes; a compiled fragment
    /// shader is the material it makes.
    /// </summary>
    /// <exception cref="ArgumentException">No loader reads files with that extension.</exception>
    internal AssetKey KeyOf(string path)
    {
        string normalized = AssetPath.Normalize(path);
        if (normalized.EndsWith(".frag" + Graphics.Devices.CompiledShader.FileExtension, StringComparison.OrdinalIgnoreCase)) return MaterialKey(normalized, default);
        string? kind = Loaders.KindOf(normalized);
        if (kind is not null && typeof(SpriteSheet).IsAssignableFrom(Loaders.Get(kind).AssetType)) kind = GraphicsAssetLoaders.Atlas;
        return kind switch
        {
            GraphicsAssetLoaders.Texture => TextureKey(normalized, null),
            GraphicsAssetLoaders.Atlas => AtlasKey(normalized, null, null),
            GraphicsAssetLoaders.Font => FontKey(normalized, null),
            null => throw new ArgumentException($"'{path}' cannot be preloaded: no loader reads '{System.IO.Path.GetExtension(normalized)}' files (loaders read {string.Join(", ", Loaders.Kinds)}). Register a loader for it in Assets.Loaders, or name its kind with Preload(load => load.Asset(kind, path)).", nameof(path)),
            _ => PlainKey(kind, normalized),
        };
    }

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
    /// Starts loading <paramref name="paths"/> without blocking, each as the kind its extension chooses: images become
    /// textures, atlas and sheet files atlases, fonts fonts, ".wav" files sounds, ".ogg" and ".mp3" files music, compiled
    /// fragment shaders materials, and files of a loader the game registered that loader's kind, all with default options
    /// (<see cref="Preload(Action{AssetPreloadList})"/> takes options). Files are read and decoded on worker threads (see
    /// <see cref="BackgroundDecoding"/>); the engine finishes them each frame within <see cref="FrameBudgetMilliseconds"/>.
    /// Afterwards the usual calls (<see cref="Texture"/>, <see cref="Atlas"/>, <see cref="Sound"/>...) return them at once.
    /// </summary>
    /// <returns>The batch, to show progress on a loading screen or wait for.</returns>
    /// <exception cref="ArgumentException">No loader reads a file's extension; the message names the file.</exception>
    public AssetBatch Preload(params string[] paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        var keys = new AssetKey[paths.Length];
        for (int i = 0; i < paths.Length; i++) keys[i] = KeyOf(paths[i]);
        return Preload(keys, anyOptions: true);
    }

    /// <summary>
    /// Starts loading what <paramref name="list"/> names, without blocking: the same calls as on this cache
    /// (<c>load.Atlas(path, options)</c>, <c>load.Sound(path)</c>, <c>load.Asset(kind, path)</c>...) with the same
    /// options, so the later direct call with those options finds the asset loaded.
    /// </summary>
    /// <example>
    /// <code>
    /// _batch = Assets.Preload(load =>
    /// {
    ///     load.Atlas("characters/hero.sheet", new AtlasOptions(Pivot: new Vector2(0.5f, 1)));
    ///     load.Files("backgrounds/castle.png", "sfx/door.wav", "music/boss.ogg");
    ///     load.Asset("level", "levels/castle.lvl");
    /// });
    /// </code>
    /// </example>
    /// <returns>The batch, to show progress on a loading screen or wait for.</returns>
    public AssetBatch Preload(Action<AssetPreloadList> list)
    {
        ArgumentNullException.ThrowIfNull(list);
        var requests = new AssetPreloadList(this);
        list(requests);
        return Preload(requests.Keys, anyOptions: false);
    }

    /// <summary>Starts loading the assets of <paramref name="keys"/> without blocking (see <see cref="Preload(string[])"/>).</summary>
    /// <returns>The batch, to show progress on a loading screen or wait for.</returns>
    public AssetBatch Preload(IEnumerable<AssetKey> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);
        return Preload([.. keys], anyOptions: false);
    }

    private AssetBatch Preload(IReadOnlyList<AssetKey> keys, bool anyOptions)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var batch = new AssetBatch(this, keys.Count);
        // Opened here, on the game's thread, so a missing folder fails now and workers share one source. A batch of
        // assets no file describes (render targets) needs none.
        bool opened = false;
        foreach (var key in keys)
        {
            var loader = Loaders.Get(key.Kind);
            var pending = new Pending(batch, key, loader);
            batch.Track(pending.Key.Path);
            // A file given by its path alone is done if it is loaded in any form of its kind; one asked for with options, only as asked.
            if (anyOptions ? _scope.HoldLoaded(key.Kind, key.Path) : _scope.TryAcquire<object>(key, out _)) { batch.Complete(pending.Key.Path, null); continue; }
            if (BackgroundDecoding && loader is IStagedAssetLoader staged)
            {
                if (!opened) { _ = Source; opened = true; }
                var context = Store.CreateContext(key);
                pending.Preparing = Task.Run(() => pending.Prepare(staged, context));
            }
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
            if (pending.Preparing is { IsCompleted: false }) break;
            _uploads.Dequeue();
            Finish(pending);
            first = false;
        }
        VisitChildren(null);
    }

    internal void Drain(AssetBatch batch)
    {
        while (_uploads.Count > 0 && batch.Remaining > 0) Finish(_uploads.Dequeue());
        VisitChildren(batch);
    }

    /// <summary>Updates (<paramref name="drain"/> null) or drains every child that existed when the visit started, in order.</summary>
    private void VisitChildren(AssetBatch? drain)
    {
        int count = _children.Count;
        if (count == 0) return;
        // A visit that starts inside another visit of this scope (null: the outer one holds the array) uses a copy of its own.
        var reused = _visit;
        _visit = null;
        var visit = reused is not null && reused.Length >= count ? reused : new AssetCache[count];
        _children.CopyTo(visit);
        try
        {
            for (int i = 0; i < count; i++)
            {
                if (drain is null) visit[i].Update();
                else visit[i].Drain(drain);
            }
        }
        finally
        {
            Array.Clear(visit, 0, count);
            if (reused is not null) _visit = visit;
        }
    }

    private void Finish(Pending pending)
    {
        long finishing = Stopwatch.GetTimestamp();
        try
        {
            if (!_scope.TryAcquire<object>(pending.Key, out _))
            {
                var context = Store.CreateContext(pending.Key);
                object value;
                if (pending.Loader is IStagedAssetLoader staged)
                {
                    if (pending.Preparing is { } task) task.Wait();
                    else pending.Prepare(staged, context);
                    if (pending.Error is { } error) throw error;
                    value = staged.Complete(context, pending.Prepared!);
                }
                // A loader without stages may use the device anywhere: it runs whole here, on the game's thread.
                else value = ((IAssetLoader<object>)pending.Loader).Load(context);
                // The load time counts the worker's part; a hitch is only what this thread spent (all of it without workers).
                double blocking = Stopwatch.GetElapsedTime(finishing).TotalMilliseconds;
                _scope.Add(pending.Key, value, (pending.Preparing is null ? 0 : pending.PrepareMilliseconds) + blocking, blocking);
            }
            pending.Batch.Complete(pending.Key.Path, null);
        }
        catch (Exception e) when (e is AssetLoadException or AggregateException or IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            var error = e as AssetLoadException ?? (e.InnerException as AssetLoadException) ?? new AssetLoadException(pending.Key.Path, pending.Key.Kind, (e.InnerException ?? e).Message, e);
            pending.Batch.Complete(pending.Key.Path, error);
        }
    }

    private sealed class Pending(AssetBatch batch, AssetKey key, IAssetLoader loader)
    {
        public readonly AssetBatch Batch = batch;
        public readonly AssetKey Key = key with { Path = AssetPath.Normalize(key.Path) };
        public readonly IAssetLoader Loader = loader;
        public Task? Preparing;
        public object? Prepared;
        public double PrepareMilliseconds;
        public AssetLoadException? Error;

        /// <summary>The loader's first stage, on whichever thread runs it; touches no device.</summary>
        public void Prepare(IStagedAssetLoader staged, AssetLoadContext context)
        {
            long started = Stopwatch.GetTimestamp();
            try { Prepared = staged.Prepare(context); }
            catch (AssetLoadException e) { Error = e; }
            catch (Exception e) when (e is not OutOfMemoryException) { Error = new AssetLoadException(Key.Path, Key.Kind, e.Message, e); }
            PrepareMilliseconds = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        }
    }
}

/// <summary>
/// What a preload is asked to load (<see cref="AssetCache.Preload(Action{AssetPreloadList})"/>): the loading calls of
/// <see cref="AssetCache"/> with the same parameters, recorded instead of run.
/// </summary>
public sealed class AssetPreloadList
{
    private readonly AssetCache _assets;
    private readonly List<AssetKey> _keys = [];

    internal AssetPreloadList(AssetCache assets) => _assets = assets;

    internal IReadOnlyList<AssetKey> Keys => _keys;

    private AssetPreloadList Add(AssetKey key)
    {
        _keys.Add(key);
        return this;
    }

    /// <summary>Files by path alone, each as the kind its extension chooses, with default options (see <see cref="AssetCache.Preload(string[])"/>).</summary>
    /// <exception cref="ArgumentException">No loader reads a file's extension.</exception>
    public AssetPreloadList Files(params string[] paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        foreach (var path in paths) Add(_assets.KeyOf(path));
        return this;
    }

    /// <summary>An image as a texture (<see cref="AssetCache.Texture"/>).</summary>
    public AssetPreloadList Texture(string path, TextureFilter? filter = null) => Add(_assets.TextureKey(path, filter));
    /// <summary>An atlas or sheet file (<see cref="AssetCache.Atlas"/>).</summary>
    public AssetPreloadList Atlas(string path, AtlasOptions? options = null, TextureFilter? filter = null) => Add(_assets.AtlasKey(path, options, filter));
    /// <summary>A folder of images packed into one atlas (<see cref="AssetCache.AtlasFromFolder"/>).</summary>
    public AssetPreloadList AtlasFromFolder(string folder, Vector2 pivot = default, TextureFilter? filter = null) => Add(_assets.FolderAtlasKey(folder, pivot, filter));
    /// <summary>A TTF or OTF font (<see cref="AssetCache.Font"/>).</summary>
    public AssetPreloadList Font(string path, FontOptions? options = null) => Add(AssetCache.FontKey(path, options));
    /// <summary>A PCM WAV file as a sound (<see cref="AssetCache.Sound"/>).</summary>
    public AssetPreloadList Sound(string path) => Add(AssetCache.PlainKey(Audio.AudioAssetLoaders.Sound, path));
    /// <summary>A music file (<see cref="AssetCache.Music"/>).</summary>
    public AssetPreloadList Music(string path) => Add(AssetCache.PlainKey(Audio.AudioAssetLoaders.Music, path));
    /// <summary>A compiled shader file (<see cref="AssetCache.Shader"/>).</summary>
    public AssetPreloadList Shader(string path) => Add(AssetCache.PlainKey(GraphicsAssetLoaders.Shader, path));
    /// <summary>A material from a compiled fragment shader (<see cref="AssetCache.Material"/>).</summary>
    public AssetPreloadList Material(string path, MaterialFallback fallback = default) => Add(AssetCache.MaterialKey(path, fallback));
    /// <summary>A render target (<see cref="AssetCache.RenderTarget"/>).</summary>
    public AssetPreloadList RenderTarget(string name, int width, int height, TextureFilter? filter = null) => Add(_assets.RenderTargetKey(name, width, height, filter));
    /// <summary>An asset of any kind by its loader's kind, as <c>Assets.Scope.Load(kind, path, options, variant)</c> asks for it: the kinds a game or a package registers.</summary>
    public AssetPreloadList Asset(string kind, string path, object? options = null, long variant = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        return Add(new AssetKey(kind, AssetPath.Normalize(path), options, variant));
    }
}

/// <summary>Assets started by <see cref="AssetCache.Preload(string[])"/>: progress for a loading screen, errors, and a way to wait.</summary>
public sealed class AssetBatch
{
    private readonly AssetCache _owner;
    private readonly List<AssetLoadException> _errors = [];
    // The paths not finished yet, in the order they were asked for.
    private readonly List<string> _waiting = [];

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
    /// <summary>The path of the first asset still loading, for a loading screen to name; null when the batch is done.</summary>
    public string? Current => _waiting.Count > 0 ? _waiting[0] : null;

    internal void Track(string path) => _waiting.Add(path);

    internal void Complete(string path, AssetLoadException? error)
    {
        Completed++;
        _waiting.Remove(path);
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

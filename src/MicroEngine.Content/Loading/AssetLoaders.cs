using System.Diagnostics.CodeAnalysis;
using MicroEngine.Core;
using MicroEngine.Platform;

namespace MicroEngine.Content;

/// <summary>
/// Makes one kind of asset from content files: decodes an image into a texture, a WAV into an audio clip... A loader is
/// registered once per store under its <see cref="Kind"/> (<see cref="AssetLoaderRegistry"/>); the store calls it the
/// first time a key of that kind is asked for and shares the result.
/// </summary>
public interface IAssetLoader
{
    /// <summary>The name requests use for this kind of asset ("texture", "sound"...), compared ordinally.</summary>
    string Kind { get; }
    /// <summary>The type of the objects it makes.</summary>
    Type AssetType { get; }
    /// <summary>File extensions (lower case, with the dot) this loader is chosen for when only a path is given; empty when it is asked for by kind only.</summary>
    IReadOnlyList<string> Extensions { get; }
    /// <summary>The memory <paramref name="asset"/> takes now, for diagnostics (0 when unknown).</summary>
    long SizeInBytes(object asset);
}

/// <summary>A loader of assets of type <typeparamref name="T"/> (see <see cref="IAssetLoader"/>).</summary>
public interface IAssetLoader<out T> : IAssetLoader where T : class
{
    /// <summary>
    /// Reads and makes the asset of <see cref="AssetLoadContext.Key"/>. Failures are exceptions: an
    /// <see cref="AssetLoadException"/>, or an <see cref="IOException"/>, <see cref="InvalidDataException"/>,
    /// <see cref="UnauthorizedAccessException"/>, <see cref="ArgumentException"/> or <see cref="NotSupportedException"/>,
    /// which the store reports as an <see cref="AssetLoadException"/> naming the asset.
    /// </summary>
    T Load(AssetLoadContext context);
}

/// <summary>A loader written as a function (see <see cref="IAssetLoader{T}"/>).</summary>
/// <typeparam name="T">The type of the assets.</typeparam>
/// <param name="kind">The kind requests use.</param>
/// <param name="load">Makes the asset.</param>
/// <param name="extensions">Extensions it is chosen for (lower case, with the dot).</param>
/// <param name="size">The memory an asset takes; null for 0.</param>
public sealed class AssetLoader<T>(string kind, Func<AssetLoadContext, T> load, IReadOnlyList<string>? extensions = null, Func<T, long>? size = null) : IAssetLoader<T> where T : class
{
    /// <inheritdoc/>
    public string Kind { get; } = string.IsNullOrWhiteSpace(kind) ? throw new ArgumentException("A loader needs a kind.", nameof(kind)) : kind;
    /// <inheritdoc/>
    public Type AssetType => typeof(T);
    /// <inheritdoc/>
    public IReadOnlyList<string> Extensions { get; } = extensions ?? [];
    /// <inheritdoc/>
    public T Load(AssetLoadContext context) => load(context);
    /// <inheritdoc/>
    public long SizeInBytes(object asset) => size is not null && asset is T value ? size(value) : 0;
}

/// <summary>What a loader is asked to load, and the content it reads it from.</summary>
public sealed class AssetLoadContext
{
    internal AssetLoadContext(AssetKey key, IContentSource source) => (Key, Source) = (key, source);

    /// <summary>The asset asked for (kind, normalized path, options, variant).</summary>
    public AssetKey Key { get; }
    /// <summary>The asset's path in <see cref="Source"/>.</summary>
    public string Path => Key.Path;
    /// <summary>The load settings, or null for the loader's defaults.</summary>
    public object? Options => Key.Options;
    /// <summary>Where the files are.</summary>
    public IContentSource Source { get; }

    /// <summary>The whole file at <paramref name="path"/> (the asset's own path when null).</summary>
    /// <exception cref="AssetLoadException">The file is missing, wrongly cased or outside the source; the message names this asset.</exception>
    public byte[] ReadBytes(string? path = null)
    {
        var file = path is null ? Key.Path : AssetPath.Normalize(path);
        try { return Source.ReadAllBytes(file); }
        catch (Exception e) when (e is IOException or ArgumentException or UnauthorizedAccessException)
        {
            throw new AssetLoadException(Key.Path, Key.Kind, file == Key.Path ? e.Message : $"'{file}': {e.Message}", e);
        }
    }

    /// <summary>The files directly inside <paramref name="folder"/> (the asset's own path when null), in ordinal order.</summary>
    /// <exception cref="AssetLoadException">The folder is missing or outside the source.</exception>
    public IReadOnlyList<string> Files(string? folder = null)
    {
        try { return Source.Files(folder is null ? Key.Path : AssetPath.Normalize(folder)); }
        catch (Exception e) when (e is IOException or ArgumentException or UnauthorizedAccessException)
        {
            throw new AssetLoadException(Key.Path, Key.Kind, e.Message, e);
        }
    }
}

/// <summary>
/// The loaders of a store, one per kind. The engine registers its own (images, atlases, fonts, sounds, music); a game
/// or module adds kinds of its own, or replaces one by registering another loader with the same kind before the first
/// load of that kind.
/// </summary>
public sealed class AssetLoaderRegistry
{
    private readonly Dictionary<string, IAssetLoader> _loaders = new(StringComparer.Ordinal);

    /// <summary>Registers <paramref name="loader"/>, replacing any loader of the same kind.</summary>
    /// <returns>This registry, for chaining.</returns>
    public AssetLoaderRegistry Add(IAssetLoader loader)
    {
        ArgumentNullException.ThrowIfNull(loader);
        _loaders[loader.Kind] = loader;
        return this;
    }

    /// <summary>The kinds registered, in ordinal order.</summary>
    public IReadOnlyList<string> Kinds => [.. _loaders.Keys.Order(StringComparer.Ordinal)];

    /// <summary>The loader of <paramref name="kind"/>, if one is registered.</summary>
    public bool TryGet(string kind, [NotNullWhen(true)] out IAssetLoader? loader) => _loaders.TryGetValue(kind, out loader);

    /// <summary>The loader of <paramref name="kind"/>.</summary>
    /// <exception cref="InvalidOperationException">No loader of that kind is registered; the message lists those that are.</exception>
    public IAssetLoader Get(string kind) => TryGet(kind, out var loader) ? loader
        : throw new InvalidOperationException($"No asset loader is registered for '{kind}'. Registered: {string.Join(", ", Kinds)}.");

    /// <summary>The kind of the loader whose <see cref="IAssetLoader.Extensions"/> holds <paramref name="path"/>'s extension (the first registered), or null.</summary>
    public string? KindOf(string path)
    {
        var extension = System.IO.Path.GetExtension(path).ToLowerInvariant();
        if (extension.Length == 0) return null;
        foreach (var loader in _loaders.Values)
            if (loader.Extensions.Contains(extension)) return loader.Kind;
        return null;
    }
}

/// <summary>
/// Reserved: a build-time step that turns a source file into the file a loader reads (a texture compressed for the GPU,
/// a model converted from glTF, a font baked into a distance field), driven by settings kept next to the source file.
/// The engine does not call importers yet; the interface fixes where that step will plug in so loaders never need to
/// change when it arrives. Content stays usable without any importer: loaders read source files directly.
/// </summary>
public interface IAssetImporter
{
    /// <summary>A name for the import step, for diagnostics and settings files.</summary>
    string Name { get; }
    /// <summary>The source file extensions it imports (lower case, with the dot).</summary>
    IReadOnlyList<string> SourceExtensions { get; }
    /// <summary>Writes the imported form of <paramref name="source"/> to <paramref name="output"/>.</summary>
    /// <param name="sourcePath">The source file's content path.</param>
    /// <param name="source">The source file.</param>
    /// <param name="settings">The import settings kept next to the source, by name.</param>
    /// <param name="output">Where the imported file is written.</param>
    void Import(string sourcePath, Stream source, IReadOnlyDictionary<string, string> settings, Stream output);
}

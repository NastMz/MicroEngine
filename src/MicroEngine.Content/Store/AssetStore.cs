using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using MicroEngine.Core;
using MicroEngine.Kernel;
using MicroEngine.Platform;

namespace MicroEngine.Content;

/// <summary>A game's assets: where content comes from, the loaders that read it, and the scopes that hold what was loaded.</summary>
public interface IAssetStore
{
    /// <summary>Where content files are read from.</summary>
    IContentSource Source { get; }
    /// <summary>The loaders, one per kind of asset.</summary>
    AssetLoaderRegistry Loaders { get; }
    /// <summary>The scope that lives as long as the store; create child scopes from it for levels and scenes.</summary>
    AssetScope Root { get; }
    /// <summary>The asset of <paramref name="asset"/>.</summary>
    /// <exception cref="InvalidOperationException">The handle is <see cref="Asset{T}.None"/>, stale (every scope released it) or of another store.</exception>
    T Get<T>(Asset<T> asset) where T : class;
}

/// <summary>
/// One entry per asset key for the whole game, shared by the scopes that hold it: a scope that asks for an asset another
/// scope already loaded gets the same object and holds it too; the object is disposed (when it is
/// <see cref="IDisposable"/>) once the last scope holding it releases it, and its handles become stale. Games never count
/// references: lifetimes follow scopes.
/// </summary>
/// <remarks>Used from the game's thread; loaders may be given work done elsewhere through <see cref="AssetScope.Add{T}"/>.</remarks>
public sealed class AssetStore : IAssetStore, IDisposable
{
    private readonly Func<IContentSource> _open;
    private IContentSource? _source;
    internal readonly Dictionary<AssetKey, AssetEntry> Shared = [];
    internal readonly HandlePool<AssetEntry> Handles = new();
    internal static readonly ILog DefaultLog = new ConsoleLog();

    /// <summary>A store reading from <paramref name="source"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    public AssetStore(IContentSource source, AssetLoaderRegistry? loaders = null)
        : this(source is null ? throw new ArgumentNullException(nameof(source)) : () => source, loaders) { }

    /// <summary>
    /// A store whose source is opened by <paramref name="openSource"/> on the first read, so a game without content files
    /// never needs one.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="openSource"/> is null.</exception>
    public AssetStore(Func<IContentSource> openSource, AssetLoaderRegistry? loaders = null)
    {
        _open = openSource ?? throw new ArgumentNullException(nameof(openSource));
        Loaders = loaders ?? new AssetLoaderRegistry();
        Root = new AssetScope(this, null, null);
    }

    /// <summary>Where content files are read from, opened on first use.</summary>
    /// <exception cref="AssetLoadException">The source cannot be opened (a missing folder or archive); its kind is "content source".</exception>
    public IContentSource Source
    {
        get
        {
            if (_source is not null) return _source;
            try { return _source = _open(); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException)
            {
                throw new AssetLoadException("", "content source", e.Message, e);
            }
        }
    }

    /// <summary>Whether <see cref="Source"/> has been opened.</summary>
    public bool IsSourceOpen => _source is not null;

    /// <inheritdoc/>
    public AssetLoaderRegistry Loaders { get; }

    /// <inheritdoc/>
    public AssetScope Root { get; }

    /// <summary>Where reports of slow loads during play go; standard error when null.</summary>
    public ILog? Log { get; set; }

    /// <summary>How many assets are loaded (each once, however many scopes hold it). Allocates nothing.</summary>
    public int Count => Shared.Count;

    /// <inheritdoc/>
    public T Get<T>(Asset<T> asset) where T : class
    {
        if (TryGet(asset, out var value)) return value;
        if (asset.IsNone) throw new InvalidOperationException($"The handle is {asset}: it refers to nothing.");
        if (Handles.TryGet(new Handle<AssetEntry>(asset.Index, asset.Generation), out var entry))
            throw new InvalidOperationException($"{entry.Key} is a {entry.Value.GetType().Name}, not a {typeof(T).Name}.");
        throw new InvalidOperationException($"The handle {asset} is stale: its asset was released by every scope that held it, or it belongs to another store.");
    }

    /// <summary>The asset of <paramref name="asset"/>, if it is still loaded and of type <typeparamref name="T"/>.</summary>
    public bool TryGet<T>(Asset<T> asset, [NotNullWhen(true)] out T? value) where T : class
    {
        if (Handles.TryGet(new Handle<AssetEntry>(asset.Index, asset.Generation), out var entry) && entry.Value is T typed)
        {
            value = typed;
            return true;
        }
        value = null;
        return false;
    }

    /// <summary>Whether <paramref name="asset"/> still refers to a loaded asset.</summary>
    public bool IsLoaded<T>(Asset<T> asset) where T : class => TryGet(asset, out _);

    /// <summary>Releases every scope's assets, disposing them. The source is not disposed: whoever made it owns it.</summary>
    public void Dispose() => Root.Dispose();

    internal AssetEntry Create(in AssetKey key, object value, IAssetLoader? loader, double milliseconds, bool duringPlay)
    {
        var entry = new AssetEntry(key, value, loader, milliseconds, duringPlay);
        Shared.Add(key, entry);
        entry.Handle = Handles.Add(entry);
        return entry;
    }

    /// <summary>Forgets <paramref name="entry"/> (no scope holds it any more): its handles go stale and its object is disposed.</summary>
    internal void Destroy(AssetEntry entry)
    {
        Shared.Remove(entry.Key);
        Handles.Remove(entry.Handle);
        if (entry.Value is IDisposable disposable) disposable.Dispose();
    }

}

/// <summary>An asset of the store and the scopes holding it.</summary>
internal sealed class AssetEntry(AssetKey key, object value, IAssetLoader? loader, double milliseconds, bool duringPlay)
{
    public readonly AssetKey Key = key;
    public readonly object Value = value;
    public readonly IAssetLoader? Loader = loader;
    public readonly double Milliseconds = milliseconds;
    public readonly bool DuringPlay = duringPlay;
    public readonly List<AssetScope> Holders = [];
    public Handle<AssetEntry> Handle;
    public long Bytes => Loader?.SizeInBytes(Value) ?? 0;
}

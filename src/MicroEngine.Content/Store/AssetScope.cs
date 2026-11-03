using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using MicroEngine.Core;
using MicroEngine.Kernel;

namespace MicroEngine.Content;

/// <summary>
/// An owner of assets: what a game, a level or a scene holds. Every scope that asks for an asset holds it, whichever
/// scope loaded it first, so sibling scopes share one object; disposing a scope (or <see cref="Release(string)"/>)
/// releases its holds, and an asset is disposed when no scope holds it any more. Child scopes are released with their
/// parent.
/// </summary>
public sealed class AssetScope : IDisposable
{
    private readonly AssetStore _store;
    private readonly List<AssetScope> _children = [];
    // What this scope holds, in the order it first asked.
    private readonly Dictionary<AssetKey, AssetEntry> _entries = [];
    private readonly HashSet<string> _warned = new(StringComparer.Ordinal);
    private bool _playing;

    internal AssetScope(AssetStore store, AssetScope? parent, string? name)
    {
        (_store, Parent, Name) = (store, parent, name);
        _playing = parent?._playing ?? false;
    }

    /// <summary>The store the scope belongs to.</summary>
    public AssetStore Store => _store;
    /// <summary>The scope it was created from; null for the store's root.</summary>
    public AssetScope? Parent { get; }
    /// <summary>The scope's name for diagnostics (null for the root and unnamed scopes).</summary>
    public string? Name { get; }
    /// <summary>Whether the scope was disposed; it can no longer load or hold anything.</summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Whether the game is past its first update; loads from then on count as loads during play
    /// (<see cref="AssetRecord.DuringPlay"/>). Setting it sets it on child scopes too; new children inherit it.
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

    /// <summary>
    /// A load during play that blocks the game's thread at least this many milliseconds is reported once (to
    /// <see cref="AssetStore.Log"/>), naming the file (4 by default: a quarter of a 60 Hz frame). Zero or less turns the
    /// reports off.
    /// </summary>
    public double SlowLoadMilliseconds { get; set; } = 4;

    // ---- Loading

    /// <summary>
    /// The asset of kind <paramref name="kind"/> at <paramref name="path"/>, loaded by the kind's loader the first time
    /// any scope asks for it and held by this scope from now on.
    /// </summary>
    /// <exception cref="AssetLoadException">The file is missing, wrongly cased or cannot be read as that kind.</exception>
    /// <exception cref="InvalidOperationException">No loader of that kind is registered, or it makes something other than a <typeparamref name="T"/>.</exception>
    public Asset<T> Load<T>(string kind, string path, object? options = null, long variant = 0) where T : class
    {
        var entry = Entry<T>(new AssetKey(kind, path, options, variant), null);
        return new Asset<T>(entry.Handle.Index, entry.Handle.Generation);
    }

    /// <summary>
    /// The asset of <paramref name="key"/>, loaded by its kind's loader when no scope has it, and held by this scope from
    /// now on. A hit this scope already holds allocates nothing.
    /// </summary>
    /// <exception cref="AssetLoadException">The file is missing, wrongly cased or cannot be read as that kind.</exception>
    /// <exception cref="InvalidOperationException">No loader of that kind is registered, or it makes something other than a <typeparamref name="T"/>.</exception>
    public T Acquire<T>(in AssetKey key) where T : class => Cast<T>(Entry<T>(key, null));

    /// <summary>The asset of <paramref name="key"/> made by <paramref name="load"/> when no scope has it, held by this scope from now on.</summary>
    /// <exception cref="AssetLoadException"><paramref name="load"/> failed; the message names the asset.</exception>
    public T Acquire<T>(in AssetKey key, Func<AssetLoadContext, T> load) where T : class
    {
        ArgumentNullException.ThrowIfNull(load);
        return Cast<T>(Entry<T>(key, load));
    }

    /// <summary>The asset of <paramref name="key"/> if any scope has it (this scope then holds it); never loads.</summary>
    public bool TryAcquire<T>(in AssetKey key, [NotNullWhen(true)] out T? value) where T : class
    {
        ThrowIfDisposed();
        var normalized = Normalized(key);
        if (Find(normalized) is { Value: T typed })
        {
            value = typed;
            return true;
        }
        value = null;
        return false;
    }

    /// <summary>The handle of an asset this scope holds under <paramref name="key"/>, or <see cref="Asset{T}.None"/>.</summary>
    public Asset<T> HandleOf<T>(in AssetKey key) where T : class =>
        _entries.TryGetValue(Normalized(key), out var entry) && entry.Value is T ? new Asset<T>(entry.Handle.Index, entry.Handle.Generation) : default;

    /// <summary>
    /// Stores <paramref name="value"/>, made elsewhere (decoded on a worker, uploaded later), under <paramref name="key"/>,
    /// held by this scope. The store disposes it when the last scope releases it.
    /// </summary>
    /// <param name="key">Its key.</param>
    /// <param name="value">The asset.</param>
    /// <param name="loadMilliseconds">The whole load, work on other threads included.</param>
    /// <param name="blockingMilliseconds">The part spent on the game's thread, which is what a player sees as a hitch; the whole load when null.</param>
    /// <exception cref="InvalidOperationException">An asset is already stored under <paramref name="key"/>.</exception>
    public Asset<T> Add<T>(in AssetKey key, T value, double loadMilliseconds, double? blockingMilliseconds = null) where T : class
    {
        ArgumentNullException.ThrowIfNull(value);
        ThrowIfDisposed();
        var normalized = Normalized(key);
        if (_store.Shared.ContainsKey(normalized)) throw new InvalidOperationException($"{normalized} is already loaded.");
        _store.Loaders.TryGet(normalized.Kind, out var loader);
        var entry = Keep(normalized, value, loader, loadMilliseconds, blockingMilliseconds);
        return new Asset<T>(entry.Handle.Index, entry.Handle.Generation);
    }

    /// <summary>Whether any scope has <paramref name="path"/> loaded as <paramref name="kind"/> (with any options); this scope then holds it.</summary>
    public bool HoldLoaded(string kind, string path)
    {
        ThrowIfDisposed();
        string normalized = AssetPath.Normalize(path);
        foreach (var key in _entries.Keys)
            if (key.Path == normalized && key.Kind == kind) return true;
        foreach (var (key, entry) in _store.Shared)
            if (key.Path == normalized && key.Kind == kind)
            {
                Hold(entry);
                return true;
            }
        return false;
    }

    /// <summary>The asset of <paramref name="asset"/> (see <see cref="AssetStore.Get{T}"/>).</summary>
    public T Get<T>(Asset<T> asset) where T : class => _store.Get(asset);

    /// <summary>Whether this scope or one of its parents holds anything from <paramref name="path"/> (in any form).</summary>
    public bool IsLoaded(string path)
    {
        string normalized = AssetPath.Normalize(path);
        for (var scope = this; scope is not null; scope = scope.Parent)
            foreach (var key in scope._entries.Keys)
                if (key.Path == normalized) return true;
        return false;
    }

    // ---- Lifecycle

    /// <summary>A child scope; it is released with this one. Use one per level or scene whose assets should be freed when it ends.</summary>
    /// <exception cref="ObjectDisposedException">The scope was disposed.</exception>
    public AssetScope CreateScope(string? name = null)
    {
        ThrowIfDisposed();
        var child = new AssetScope(_store, this, name);
        _children.Add(child);
        return child;
    }

    /// <summary>
    /// Releases what this scope holds from <paramref name="path"/> (in any form): each asset is disposed unless another
    /// scope still holds it, and loads again if asked for later. What only other scopes hold is not touched.
    /// </summary>
    /// <returns>Whether this scope held anything from the file.</returns>
    public bool Release(string path)
    {
        string normalized = AssetPath.Normalize(path);
        List<AssetEntry>? released = null;
        foreach (var (key, entry) in _entries)
            if (key.Path == normalized) (released ??= []).Add(entry);
        if (released is null) return false;
        foreach (var entry in released) Let(entry);
        return true;
    }

    /// <summary>Releases this scope's hold on <paramref name="asset"/>; it is disposed when no scope holds it any more.</summary>
    /// <returns>Whether this scope held it.</returns>
    public bool Release<T>(Asset<T> asset) where T : class
    {
        if (!_store.Handles.TryGet(new Handle<AssetEntry>(asset.Index, asset.Generation), out var entry) || !_entries.ContainsKey(entry.Key)) return false;
        Let(entry);
        return true;
    }

    /// <summary>Releases every asset this scope and its children hold, disposing those no other scope holds.</summary>
    public void Dispose()
    {
        if (IsDisposed) return;
        IsDisposed = true;
        foreach (var child in _children.ToList()) child.Dispose();
        _children.Clear();
        foreach (var entry in _entries.Values.ToList()) Unhold(entry);
        _entries.Clear();
        Parent?._children.Remove(this);
    }

    // ---- Diagnostics

    /// <summary>
    /// How many assets this scope and its children hold, an asset several of them share counting once; on the root
    /// scope, every asset of the store. Allocates nothing, so it can be checked every frame.
    /// </summary>
    public int Count
    {
        get
        {
            if (Parent is null) return _store.Shared.Count;
            int count = 0;
            foreach (var entry in _store.Shared.Values)
                foreach (var holder in entry.Holders)
                    if (holder.IsWithin(this)) { count++; break; }
            return count;
        }
    }

    /// <summary>The assets this scope and its children hold, each once, in the order each scope asked for them.</summary>
    public IReadOnlyList<AssetRecord> Loaded
    {
        get
        {
            var seen = new HashSet<AssetEntry>();
            var loaded = new List<AssetRecord>();
            Collect(seen, loaded);
            return loaded;
        }
    }

    /// <summary>The objects this scope and its children hold, each once, in the order they were asked for.</summary>
    public IReadOnlyList<object> Objects
    {
        get
        {
            var seen = new HashSet<AssetEntry>();
            var objects = new List<object>();
            CollectObjects(seen, objects);
            return objects;
        }
    }

    // ---- Internals

    private void Collect(HashSet<AssetEntry> seen, List<AssetRecord> into)
    {
        foreach (var e in _entries.Values)
            if (seen.Add(e)) into.Add(new AssetRecord(e.Key.Path, e.Key.Kind, e.Bytes, e.Milliseconds, e.DuringPlay));
        foreach (var child in _children) child.Collect(seen, into);
    }

    private void CollectObjects(HashSet<AssetEntry> seen, List<object> into)
    {
        foreach (var e in _entries.Values)
            if (seen.Add(e)) into.Add(e.Value);
        foreach (var child in _children) child.CollectObjects(seen, into);
    }

    private bool IsWithin(AssetScope scope)
    {
        for (var s = this; s is not null; s = s.Parent)
            if (s == scope) return true;
        return false;
    }

    private static AssetKey Normalized(in AssetKey key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key.Kind);
        if (key.Path is null) throw new ArgumentException("An asset path is needed.", nameof(key));
        var path = key.Path.Length == 0 ? key.Path : AssetPath.Normalize(key.Path);
        return ReferenceEquals(path, key.Path) ? key : key with { Path = path };
    }

    /// <summary>The entry of <paramref name="key"/> when any scope has it, held by this scope from now on; null otherwise.</summary>
    private AssetEntry? Find(in AssetKey key)
    {
        if (_entries.TryGetValue(key, out var held)) return held;
        if (!_store.Shared.TryGetValue(key, out var shared)) return null;
        Hold(shared);
        return shared;
    }

    private AssetEntry Entry<T>(in AssetKey requested, Func<AssetLoadContext, T>? load) where T : class
    {
        ThrowIfDisposed();
        var key = Normalized(requested);
        if (Find(key) is { } found) return found;
        if (key.Path.Length == 0) throw new ArgumentException("An asset path is needed.", nameof(requested));
        IAssetLoader? loader = null;
        if (load is null)
        {
            loader = _store.Loaders.Get(key.Kind);
            if (!typeof(T).IsAssignableFrom(loader.AssetType))
                throw new InvalidOperationException($"The '{key.Kind}' loader makes {loader.AssetType.Name} assets, not {typeof(T).Name}.");
        }
        else _store.Loaders.TryGet(key.Kind, out loader);
        long started = Stopwatch.GetTimestamp();
        object value;
        try
        {
            var context = new AssetLoadContext(key, _store.Source);
            value = load is not null ? load(context) : ((IAssetLoader<object>)loader!).Load(context);
        }
        catch (Exception e) when (e is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            throw new AssetLoadException(key.Path, key.Kind, e.Message, e);
        }
        return Keep(key, value, loader, Stopwatch.GetElapsedTime(started).TotalMilliseconds, null);
    }

    private AssetEntry Keep(in AssetKey key, object value, IAssetLoader? loader, double milliseconds, double? blocking)
    {
        var entry = _store.Create(key, value, loader, milliseconds, Playing);
        Hold(entry);
        double hitch = blocking ?? milliseconds;
        if (Playing && SlowLoadMilliseconds > 0 && hitch >= SlowLoadMilliseconds && _warned.Add(key.Kind + "|" + key.Path))
            (_store.Log ?? AssetStore.DefaultLog).Write(LogLevel.Warning, "assets", $"{key.Kind} '{key.Path}' was loaded during play and took {hitch:0.0} ms (a visible hitch); load it in Load, or Preload it behind a loading screen.");
        return entry;
    }

    private void Hold(AssetEntry entry)
    {
        _entries.Add(entry.Key, entry);
        entry.Holders.Add(this);
    }

    /// <summary>Lets go of <paramref name="entry"/> and forgets it.</summary>
    private void Let(AssetEntry entry)
    {
        _entries.Remove(entry.Key);
        Unhold(entry);
    }

    /// <summary>Removes this scope from the entry's holders; destroys the entry when none is left.</summary>
    private void Unhold(AssetEntry entry)
    {
        entry.Holders.Remove(this);
        if (entry.Holders.Count == 0) _store.Destroy(entry);
    }

    private static T Cast<T>(AssetEntry entry) where T : class => entry.Value as T
        ?? throw new InvalidOperationException($"{entry.Key} is a {entry.Value.GetType().Name}, not a {typeof(T).Name}.");

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(IsDisposed, this);
}

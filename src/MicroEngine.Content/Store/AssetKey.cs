namespace MicroEngine.Content;

/// <summary>
/// What identifies a stored asset: the loader's kind, the content path and the settings it was loaded with. Two
/// requests with equal keys share one object. A struct of references and values, so a lookup allocates nothing.
/// </summary>
/// <param name="Kind">The kind of the loader that makes it ("texture", "sound"...; see <see cref="AssetLoaderRegistry"/>).</param>
/// <param name="Path">The path in the content source, '/'-separated (normalized by the store, see <see cref="AssetPath.Normalize"/>).</param>
/// <param name="Options">Load settings compared by value (a record), or null for the loader's defaults.</param>
/// <param name="Variant">A small value that also tells loads apart (a sampling filter, for example) without boxing it.</param>
public readonly record struct AssetKey(string Kind, string Path, object? Options = null, long Variant = 0)
{
    /// <summary>"kind 'path'" for messages.</summary>
    public override string ToString() => $"{Kind} '{Path}'";
}

/// <summary>
/// A typed reference to an asset in an <see cref="AssetStore"/>: a slot and the generation of the asset that held it.
/// Once the asset is released by every scope that held it (and disposed), the handle is stale: reading it fails instead
/// of reaching whatever reuses the slot. <c>default</c> is <see cref="None"/>.
/// </summary>
/// <typeparam name="T">The asset's type (a texture, an audio clip...).</typeparam>
/// <param name="Index">The store's slot.</param>
/// <param name="Generation">The generation of the slot's asset; 0 only for <see cref="None"/>.</param>
public readonly record struct Asset<T>(int Index, uint Generation) where T : class
{
    /// <summary>The handle of nothing.</summary>
    public static Asset<T> None => default;
    /// <summary>Whether this is <see cref="None"/>.</summary>
    public bool IsNone => Generation == 0;
    /// <inheritdoc/>
    public override string ToString() => IsNone ? $"Asset<{typeof(T).Name}>:none" : $"Asset<{typeof(T).Name}>:{Index}#{Generation}";
}

/// <summary>One asset a scope holds, as <see cref="AssetScope.Loaded"/> lists it.</summary>
/// <param name="Path">Its content path.</param>
/// <param name="Kind">Its loader's kind.</param>
/// <param name="Bytes">Memory its loader reports for it now (device memory for textures, samples for sounds).</param>
/// <param name="LoadMilliseconds">Time spent loading it (reading, decoding and creating it).</param>
/// <param name="DuringPlay">Whether it was loaded after the game's first update, when a slow load shows as a hitch.</param>
public readonly record struct AssetRecord(string Path, string Kind, long Bytes, double LoadMilliseconds, bool DuringPlay);

/// <summary>Content paths: relative to the source, '/'-separated.</summary>
public static class AssetPath
{
    /// <summary>
    /// '/' separators, no leading '/' or "./", "a/../b" as "b"; the same string when nothing changes, so lookups allocate
    /// nothing. A ".." that leaves the source stays, so the source refuses it.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="path"/> is null, empty or white space.</exception>
    public static string Normalize(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (path.Contains('\\')) path = path.Replace('\\', '/');
        if (path.StartsWith('/')) path = path.TrimStart('/');
        if (!path.StartsWith("./", StringComparison.Ordinal) && !path.Contains("/.", StringComparison.Ordinal) && !path.StartsWith("../", StringComparison.Ordinal))
            return path;
        var parts = new List<string>();
        foreach (var part in path.Split('/'))
        {
            if (part is "." or "") continue;
            if (part == ".." && parts.Count > 0 && parts[^1] != "..") parts.RemoveAt(parts.Count - 1);
            else parts.Add(part);
        }
        return string.Join('/', parts);
    }
}

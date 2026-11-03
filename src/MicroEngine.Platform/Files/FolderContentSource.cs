using System.Collections.Concurrent;

namespace MicroEngine.Platform;

/// <summary>
/// Content files in a folder on disk. Names must match the files' letter case exactly on every system: "Hero.png" for
/// "hero.png" works on Windows and macOS but not on Linux, Android or in browsers, so it fails everywhere, naming the
/// real spelling. Paths that leave the folder (<c>..</c>, another drive) are refused.
/// </summary>
/// <remarks>Safe to read from several threads (asset workers check names concurrently).</remarks>
public sealed class FolderContentSource : IContentSource
{
    // Directory listings, kept so every check after the first in a folder costs a lookup.
    private readonly ConcurrentDictionary<string, HashSet<string>> _listings = new(StringComparer.Ordinal);

    /// <summary>Opens a folder.</summary>
    /// <param name="root">The folder, absolute or relative to the working directory.</param>
    /// <exception cref="ArgumentException"><paramref name="root"/> is null, empty or white space.</exception>
    /// <exception cref="DirectoryNotFoundException">The folder does not exist.</exception>
    public FolderContentSource(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        Root = Path.GetFullPath(root);
        if (!Directory.Exists(Root)) throw new DirectoryNotFoundException($"Content folder not found: {Root}");
    }

    /// <summary>The absolute path of the folder.</summary>
    public string Root { get; }

    /// <summary>The same as <see cref="Root"/>.</summary>
    public string Location => Root;

    /// <summary>The full path of the file at <paramref name="path"/>, after the checks of <see cref="Open"/>.</summary>
    /// <exception cref="ArgumentException">The path leaves the folder.</exception>
    /// <exception cref="FileNotFoundException">No such file, or its name is spelled with another letter case.</exception>
    public string PathOf(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var full = Resolve(path) ?? throw new ArgumentException($"'{path}' is outside the content folder.", nameof(path));
        // On a case-sensitive system a wrong spelling is simply missing; CheckCase then names the real one.
        CheckCase(full);
        if (!File.Exists(full)) throw new FileNotFoundException($"Content file not found: {full}", full);
        return full;
    }

    /// <inheritdoc/>
    public bool Exists(string path) => !string.IsNullOrWhiteSpace(path) && Resolve(path) is { } full && File.Exists(full);

    /// <inheritdoc/>
    public Stream Open(string path) => File.OpenRead(PathOf(path));

    /// <inheritdoc/>
    public IReadOnlyList<string> Files(string folder)
    {
        ArgumentNullException.ThrowIfNull(folder);
        var full = (folder.Trim('/').Length == 0 ? Root : Resolve(folder)) ?? throw new ArgumentException($"'{folder}' is outside the content folder.", nameof(folder));
        if (!Directory.Exists(full)) throw new DirectoryNotFoundException($"Content folder not found: {full}");
        if (full != Root) CheckCase(full);
        return [.. Directory.GetFiles(full).Select(f => Path.GetRelativePath(Root, f).Replace(Path.DirectorySeparatorChar, '/')).Order(StringComparer.Ordinal)];
    }

    /// <summary>Fails when a part of <paramref name="path"/> (below the root) is spelled with another letter case than on disk.</summary>
    private void CheckCase(string path)
    {
        var parts = Path.GetRelativePath(Root, path).Split(Path.DirectorySeparatorChar);
        string current = Root;
        foreach (var part in parts)
        {
            if (!Directory.Exists(current)) return;
            var names = _listings.GetOrAdd(current, dir => new HashSet<string>(Directory.EnumerateFileSystemEntries(dir).Select(Path.GetFileName)!, StringComparer.Ordinal));
            if (!names.Contains(part))
            {
                string? actual = names.FirstOrDefault(n => string.Equals(n, part, StringComparison.OrdinalIgnoreCase));
                if (actual is not null)
                    throw new FileNotFoundException($"Content file not found: '{part}' is spelled '{actual}' on disk ({path}). Names must match exactly: Linux, Android and browsers tell letter case apart.", path);
            }
            current = Path.Combine(current, part);
        }
    }

    /// <summary>The full path of <paramref name="relative"/>, or null when it leaves the root (<c>..</c>, another drive).</summary>
    private string? Resolve(string relative)
    {
        var path = Path.GetFullPath(Path.Combine(Root, relative.Replace('/', Path.DirectorySeparatorChar)));
        var inside = Path.GetRelativePath(Root, path);
        return inside == ".." || inside.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) || Path.IsPathRooted(inside) ? null : path;
    }
}

namespace MicroEngine.Core;

/// <summary>
/// Locates a game's asset files. By default the root is the <c>Content</c> folder next to the
/// executable (games copy it there at build time). Missing files fail with the full path so a
/// broken install or a typo is obvious.
/// </summary>
public sealed class ContentFolder
{
    /// <summary>The absolute path of the content folder.</summary>
    public string Root { get; }

    /// <summary>
    /// Root used when a game passes none. Null means the <c>Content</c> folder next to the executable; a host whose
    /// files live elsewhere (a phone app unpacking its package) sets it before the game is built.
    /// </summary>
    public static string? DefaultRoot { get; set; }

    /// <summary>Opens a content folder.</summary>
    /// <param name="root">The folder, absolute or relative to the working directory; null uses <see cref="DefaultRoot"/> or the <c>Content</c> folder next to the executable.</param>
    /// <exception cref="DirectoryNotFoundException">The folder does not exist.</exception>
    public ContentFolder(string? root = null)
    {
        Root = Path.GetFullPath(root ?? DefaultRoot ?? Path.Combine(AppContext.BaseDirectory, "Content"));
        if (!Directory.Exists(Root)) throw new DirectoryNotFoundException($"Content folder not found: {Root}");
    }

    /// <summary>
    /// Full path of a file given relative to the root with '/' separators. The name must match the file's letter case
    /// exactly, on every system: "Hero.png" for "hero.png" works on Windows and macOS but not on Linux, Android or in
    /// browsers, so it fails everywhere, naming the file's real spelling.
    /// </summary>
    /// <exception cref="ArgumentException">The path leaves the content folder.</exception>
    /// <exception cref="FileNotFoundException">No such file, or its name is spelled with another letter case.</exception>
    public string PathOf(string relative)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relative);
        var path = Resolve(relative) ?? throw new ArgumentException($"'{relative}' is outside the content folder.", nameof(relative));
        if (!File.Exists(path)) throw new FileNotFoundException($"Content file not found: {path}", path);
        CheckCase(path);
        return path;
    }

    /// <summary>
    /// The files directly inside <paramref name="folder"/> (relative to the root, '/' separators; "" for the root), as
    /// '/'-separated paths relative to the root, in ordinal order.
    /// </summary>
    /// <exception cref="DirectoryNotFoundException">The folder does not exist.</exception>
    public IReadOnlyList<string> Files(string folder)
    {
        ArgumentNullException.ThrowIfNull(folder);
        var path = (folder.Trim('/').Length == 0 ? Root : Resolve(folder)) ?? throw new ArgumentException($"'{folder}' is outside the content folder.", nameof(folder));
        if (!Directory.Exists(path)) throw new DirectoryNotFoundException($"Content folder not found: {path}");
        if (path != Root) CheckCase(path);
        return [.. Directory.GetFiles(path).Select(f => Path.GetRelativePath(Root, f).Replace(Path.DirectorySeparatorChar, '/')).Order(StringComparer.Ordinal)];
    }

    // Directory listings, kept so every check after the first in a folder costs a lookup. Workers preloading assets
    // check names concurrently.
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, HashSet<string>> _listings = new(StringComparer.Ordinal);

    /// <summary>Fails when a part of <paramref name="path"/> (below the root) is spelled with another letter case than on disk.</summary>
    private void CheckCase(string path)
    {
        var parts = Path.GetRelativePath(Root, path).Split(Path.DirectorySeparatorChar);
        string current = Root;
        foreach (var part in parts)
        {
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

    /// <summary>Whether a file exists at <paramref name="relative"/> (relative to <see cref="Root"/>, '/' separators). False for a missing file and for a path outside the folder.</summary>
    public bool Exists(string relative) => !string.IsNullOrWhiteSpace(relative) && Resolve(relative) is { } path && File.Exists(path);

    /// <summary>The full path of <paramref name="relative"/>, or null when it leaves the root (<c>..</c>, another drive).</summary>
    private string? Resolve(string relative)
    {
        var path = Path.GetFullPath(Path.Combine(Root, relative.Replace('/', Path.DirectorySeparatorChar)));
        var inside = Path.GetRelativePath(Root, path);
        return inside == ".." || inside.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) || Path.IsPathRooted(inside) ? null : path;
    }
    /// <summary>Reads a whole file.</summary>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    public byte[] ReadBytes(string relative) => File.ReadAllBytes(PathOf(relative));
    /// <summary>Decodes a PNG file into an image (see <see cref="Graphics.PixelImage.Load"/>).</summary>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    public Graphics.PixelImage Image(string relative) => Graphics.PixelImage.Load(PathOf(relative));
}

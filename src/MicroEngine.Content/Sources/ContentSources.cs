using System.IO.Compression;
using MicroEngine.Platform;

namespace MicroEngine.Content;

/// <summary>Reading from any content source, and opening one from a path.</summary>
public static class ContentSources
{
    /// <summary>Reads the whole file at <paramref name="path"/>.</summary>
    /// <exception cref="ArgumentException">The path leaves the source.</exception>
    /// <exception cref="FileNotFoundException">No such file, or its name is spelled with another letter case.</exception>
    public static byte[] ReadAllBytes(this IContentSource source, string path)
    {
        ArgumentNullException.ThrowIfNull(source);
        using var stream = source.Open(path);
        if (stream is MemoryStream memory && memory.TryGetBuffer(out var buffer) && buffer.Offset == 0 && buffer.Count == buffer.Array!.Length && memory.Position == 0)
            return buffer.Array;
        using var copy = stream.CanSeek ? new MemoryStream((int)Math.Max(0, stream.Length - stream.Position)) : new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }

    /// <summary>
    /// The content at <paramref name="path"/>: a folder (<see cref="FolderContentSource"/>) or a zip archive
    /// (<see cref="ZipContentSource"/>, a file ending in ".zip").
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="path"/> is null, empty or white space.</exception>
    /// <exception cref="FileNotFoundException">Nothing exists at <paramref name="path"/>, or it is a file other than a zip archive.</exception>
    /// <exception cref="InvalidDataException">The file is not a valid zip archive.</exception>
    public static IContentSource Open(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (Directory.Exists(path)) return new FolderContentSource(path);
        if (File.Exists(path) && path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) return new ZipContentSource(path);
        throw new FileNotFoundException(File.Exists(path)
            ? $"Content must be a folder or a .zip archive: {Path.GetFullPath(path)}"
            : $"Content not found: {Path.GetFullPath(path)}", path);
    }
}

/// <summary>
/// The names of a source's files, checked as a folder checks them: exact letter case on every system (naming the real
/// spelling when only the case differs), '/' separators, no way out of the source.
/// </summary>
internal sealed class ContentListing
{
    private readonly HashSet<string> _files = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<string>> _children = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _spellings = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _location;

    public ContentListing(string location, IEnumerable<string> files)
    {
        _location = location;
        _children[""] = [];
        foreach (var raw in files)
        {
            var file = raw.Replace('\\', '/').Trim('/');
            if (file.Length == 0 || !_files.Add(file)) continue;
            Remember(file);
            int slash = file.LastIndexOf('/');
            string folder = slash < 0 ? "" : file[..slash];
            Child(folder).Add(file);
            // Every folder above it exists too (a folder holding only folders lists no file), with its spelling.
            while (folder.Length > 0)
            {
                Remember(folder);
                Child(folder);
                int up = folder.LastIndexOf('/');
                folder = up < 0 ? "" : folder[..up];
            }
        }
        foreach (var list in _children.Values) list.Sort(StringComparer.Ordinal);
    }

    private List<string> Child(string folder) => _children.TryGetValue(folder, out var list) ? list : _children[folder] = [];

    private void Remember(string path) => _spellings.TryAdd(path, path);

    /// <summary>The source-relative path of <paramref name="path"/>, or an exception saying why it is not a file of the source.</summary>
    public string Resolve(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var normalized = AssetPath.Normalize(path);
        if (normalized == ".." || normalized.StartsWith("../", StringComparison.Ordinal))
            throw new ArgumentException($"'{path}' is outside the content source.", nameof(path));
        if (_files.Contains(normalized)) return normalized;
        throw new FileNotFoundException(Misspelled(normalized) is { } real
            ? $"Content file not found: '{normalized}' is spelled '{real}' in {_location}. Names must match exactly: Linux, Android and browsers tell letter case apart."
            : $"Content file not found: {normalized} in {_location}", normalized);
    }

    public bool Exists(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        var normalized = AssetPath.Normalize(path);
        return _files.Contains(normalized);
    }

    public IReadOnlyList<string> Files(string folder)
    {
        ArgumentNullException.ThrowIfNull(folder);
        var normalized = folder.Trim('/').Length == 0 ? "" : AssetPath.Normalize(folder).TrimEnd('/');
        if (normalized == ".." || normalized.StartsWith("../", StringComparison.Ordinal))
            throw new ArgumentException($"'{folder}' is outside the content source.", nameof(folder));
        if (_children.TryGetValue(normalized, out var files)) return [.. files];
        throw new DirectoryNotFoundException(Misspelled(normalized) is { } real
            ? $"Content folder not found: '{normalized}' is spelled '{real}' in {_location}. Names must match exactly: Linux, Android and browsers tell letter case apart."
            : $"Content folder not found: {normalized} in {_location}");
    }

    /// <summary>The stored spelling of <paramref name="path"/> when it differs from it only in letter case.</summary>
    private string? Misspelled(string path) => _spellings.TryGetValue(path, out var real) && real != path ? real : null;
}

/// <summary>
/// Content files inside a zip archive: a game's content shipped as one file. Names follow the folder rules (exact
/// letter case everywhere, '/' separators, no way out of the archive). Each file is decompressed into memory when it is
/// opened, so streams are independent and the source can be read from several threads.
/// </summary>
public sealed class ZipContentSource : IContentSource, IDisposable
{
    private readonly ZipArchive _archive;
    private readonly Dictionary<string, ZipArchiveEntry> _entries = new(StringComparer.Ordinal);
    private readonly ContentListing _listing;
    private readonly Lock _gate = new();

    /// <summary>Opens the zip archive at <paramref name="path"/>; the source keeps the file open until disposed.</summary>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="InvalidDataException">The file is not a valid zip archive.</exception>
    public ZipContentSource(string path) : this(File.OpenRead(path), Path.GetFullPath(path), leaveOpen: false) { }

    /// <summary>Reads the zip archive in <paramref name="archive"/> (a readable, seekable stream).</summary>
    /// <param name="archive">The archive's bytes.</param>
    /// <param name="location">A description of where it came from, for messages.</param>
    /// <param name="leaveOpen">Whether disposing the source leaves <paramref name="archive"/> open.</param>
    /// <exception cref="InvalidDataException">The stream is not a valid zip archive.</exception>
    public ZipContentSource(Stream archive, string location, bool leaveOpen = false)
    {
        ArgumentNullException.ThrowIfNull(archive);
        ArgumentException.ThrowIfNullOrWhiteSpace(location);
        Location = location;
        try { _archive = new ZipArchive(archive, ZipArchiveMode.Read, leaveOpen); }
        catch
        {
            if (!leaveOpen) archive.Dispose();
            throw;
        }
        foreach (var entry in _archive.Entries)
        {
            var name = entry.FullName.Replace('\\', '/');
            if (name.EndsWith('/')) continue;
            _entries.TryAdd(name.TrimStart('/'), entry);
        }
        _listing = new ContentListing(location, _entries.Keys);
    }

    /// <summary>The archive's path or the description it was opened with.</summary>
    public string Location { get; }

    /// <inheritdoc/>
    public bool Exists(string path) => _listing.Exists(path);

    /// <inheritdoc/>
    public Stream Open(string path)
    {
        var entry = _entries[_listing.Resolve(path)];
        lock (_gate)
        {
            var memory = new MemoryStream(checked((int)entry.Length));
            using (var stream = entry.Open()) stream.CopyTo(memory);
            memory.Position = 0;
            return memory;
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> Files(string folder) => _listing.Files(folder);

    /// <summary>Closes the archive.</summary>
    public void Dispose() => _archive.Dispose();
}

/// <summary>
/// Content files a host lists and opens itself, where the files are not a folder .NET can read: an Android app's
/// packaged assets (opened through the platform's asset manager), files a browser page fetched. The listing is fixed
/// when the source is made; names follow the folder rules (exact letter case everywhere, '/' separators).
/// </summary>
public sealed class ListedContentSource : IContentSource
{
    private readonly ContentListing _listing;
    private readonly Func<string, Stream> _open;

    /// <summary>A source over <paramref name="files"/>, read with <paramref name="open"/>.</summary>
    /// <param name="location">A description of where the files are, for messages.</param>
    /// <param name="files">Every file, relative to the source with '/' separators.</param>
    /// <param name="open">Opens one of <paramref name="files"/> (given exactly as listed); called from any thread.</param>
    /// <exception cref="ArgumentException"><paramref name="location"/> is null, empty or white space.</exception>
    public ListedContentSource(string location, IEnumerable<string> files, Func<string, Stream> open)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(location);
        ArgumentNullException.ThrowIfNull(files);
        Location = location;
        _open = open ?? throw new ArgumentNullException(nameof(open));
        _listing = new ContentListing(location, files);
    }

    /// <inheritdoc/>
    public string Location { get; }

    /// <inheritdoc/>
    public bool Exists(string path) => _listing.Exists(path);

    /// <inheritdoc/>
    public Stream Open(string path) => _open(_listing.Resolve(path));

    /// <inheritdoc/>
    public IReadOnlyList<string> Files(string folder) => _listing.Files(folder);
}

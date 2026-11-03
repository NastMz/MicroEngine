using MicroEngine.Platform;

namespace MicroEngine.Core;

/// <summary>
/// Locates a game's asset files. By default the root is the <c>Content</c> folder next to the
/// executable (games copy it there at build time). Missing files fail with the full path so a
/// broken install or a typo is obvious. A content source (<see cref="IContentSource"/>) over a
/// <see cref="FolderContentSource"/>.
/// </summary>
public sealed class ContentFolder : IContentSource
{
    private readonly FolderContentSource _source;

    /// <summary>The absolute path of the content folder.</summary>
    public string Root => _source.Root;

    /// <summary>The same as <see cref="Root"/>.</summary>
    public string Location => _source.Location;

    /// <summary>
    /// The folder used when a game and its composition name none: <c>Content</c> next to the executable. A rule, not a
    /// setting: a host whose files live elsewhere (a phone app, a macOS bundle) composes its folder
    /// (<see cref="Hosting.HostBuilder.Content"/>).
    /// </summary>
    public static string DefaultRoot => Path.Combine(AppContext.BaseDirectory, "Content");

    /// <summary>Opens a content folder.</summary>
    /// <param name="root">The folder, absolute or relative to the working directory; null uses the <c>Content</c> folder next to the executable. A host whose files live elsewhere (a phone app, a macOS bundle) composes its folder instead (<see cref="Hosting.HostBuilder.Content"/>).</param>
    /// <exception cref="DirectoryNotFoundException">The folder does not exist.</exception>
    public ContentFolder(string? root = null) =>
        _source = new FolderContentSource(root ?? DefaultRoot);

    /// <summary>
    /// Full path of a file given relative to the root with '/' separators. The name must match the file's letter case
    /// exactly, on every system: "Hero.png" for "hero.png" works on Windows and macOS but not on Linux, Android or in
    /// browsers, so it fails everywhere, naming the file's real spelling.
    /// </summary>
    /// <exception cref="ArgumentException">The path leaves the content folder.</exception>
    /// <exception cref="FileNotFoundException">No such file, or its name is spelled with another letter case.</exception>
    public string PathOf(string relative) => _source.PathOf(relative);

    /// <summary>
    /// The files directly inside <paramref name="folder"/> (relative to the root, '/' separators; "" for the root), as
    /// '/'-separated paths relative to the root, in ordinal order.
    /// </summary>
    /// <exception cref="DirectoryNotFoundException">The folder does not exist.</exception>
    public IReadOnlyList<string> Files(string folder) => _source.Files(folder);

    /// <summary>Whether a file exists at <paramref name="relative"/> (relative to <see cref="Root"/>, '/' separators). False for a missing file and for a path outside the folder.</summary>
    public bool Exists(string relative) => _source.Exists(relative);

    /// <summary>Opens a file for reading, after the checks of <see cref="PathOf"/>.</summary>
    public Stream Open(string relative) => _source.Open(relative);

    /// <summary>Reads a whole file.</summary>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    public byte[] ReadBytes(string relative) => File.ReadAllBytes(PathOf(relative));
    /// <summary>Decodes a PNG file into an image (see <see cref="Graphics.PixelImage.Load"/>).</summary>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    public Graphics.PixelImage Image(string relative) => Graphics.PixelImage.Load(PathOf(relative));
}

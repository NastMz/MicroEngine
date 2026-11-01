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

    /// <summary>Full path of a file given relative to the root with '/' separators.</summary>
    public string PathOf(string relative)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relative);
        var path = Path.GetFullPath(Path.Combine(Root, relative.Replace('/', Path.DirectorySeparatorChar)));
        if (!path.StartsWith(Root, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException($"'{relative}' is outside the content folder.", nameof(relative));
        if (!File.Exists(path)) throw new FileNotFoundException($"Content file not found: {path}", path);
        return path;
    }

    /// <summary>Whether a file exists at <paramref name="relative"/> (relative to <see cref="Root"/>, '/' separators). Never throws for a missing file.</summary>
    public bool Exists(string relative) => File.Exists(Path.Combine(Root, relative.Replace('/', Path.DirectorySeparatorChar)));
    /// <summary>Reads a whole file.</summary>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    public byte[] ReadBytes(string relative) => File.ReadAllBytes(PathOf(relative));
    /// <summary>Decodes a PNG file into an image (see <see cref="Graphics.PixelImage.Load"/>).</summary>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    public Graphics.PixelImage Image(string relative) => Graphics.PixelImage.Load(PathOf(relative));
}

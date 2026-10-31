namespace MicroEngine.Core;

/// <summary>
/// Locates a game's asset files. By default the root is the <c>Content</c> folder next to the
/// executable (games copy it there at build time). Missing files fail with the full path so a
/// broken install or a typo is obvious.
/// </summary>
public sealed class ContentFolder
{
    public string Root { get; }

    /// <summary>
    /// Root used when a game passes none. Null means the <c>Content</c> folder next to the executable; a host whose
    /// files live elsewhere (a phone app unpacking its package) sets it before the game is built.
    /// </summary>
    public static string? DefaultRoot { get; set; }

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

    public bool Exists(string relative) => File.Exists(Path.Combine(Root, relative.Replace('/', Path.DirectorySeparatorChar)));
    public byte[] ReadBytes(string relative) => File.ReadAllBytes(PathOf(relative));
    public Graphics.PixelImage Image(string relative) => Graphics.PixelImage.Load(PathOf(relative));
}

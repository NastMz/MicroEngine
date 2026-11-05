namespace MicroEngine.Cli;

/// <summary>
/// The licence files of the native engine libraries a package carries, shipped with the tool in <c>Notices/</c> and
/// copied into the package's <c>licenses/</c> folder: SDL 3 and raylib (both zlib).
/// </summary>
public static class EngineNotices
{
    public const string Sdl = "SDL-LICENSE.txt";
    public const string Raylib = "raylib-LICENSE.txt";

    private static readonly (string Licence, string[] Libraries)[] Natives =
    [
        (Sdl, ["SDL3.dll", "libSDL3.so", "libSDL3.dylib"]),
        (Raylib, ["raylib.dll", "libraylib.so", "libraylib.dylib"]),
    ];

    /// <summary>The licence file of each native library among <paramref name="files"/> (paths or names), in a fixed order.</summary>
    public static List<string> ForFiles(IEnumerable<string> files)
    {
        var names = files.Select(f => Path.GetFileName(f.Replace('\\', '/'))).ToHashSet(StringComparer.Ordinal);
        return Natives.Where(n => n.Libraries.Any(names.Contains)).Select(n => n.Licence).ToList();
    }

    /// <summary>Where the tool keeps the licence file named <paramref name="name"/>.</summary>
    public static string PathOf(string name) => Path.Combine(Tool.Home, "Notices", name);
}

using MicroEngine.Core;
using MicroEngine.Raylib;
using MicroEngine.Sdl;

namespace MicroEngine.Desktop;

/// <summary>
/// Standard entry point for desktop games: <see cref="GameLauncher"/> with the official desktop platforms, in order of
/// preference: <c>sdl</c> (the default on Windows, Linux and macOS), <c>raylib</c>, <c>sdl-software</c> (SDL's CPU
/// renderer, for machines without a usable GPU such as virtual machines and CI runners) and <c>headless</c>. A game's
/// <c>Program.cs</c> is one line; choosing a platform is the player's or the test's business (<c>--platform</c>,
/// <c>MICROENGINE_PLATFORM</c>), never the game's.
/// </summary>
public static class DesktopLauncher
{
    /// <summary>A new catalog of the official desktop platforms; add a profile of your own to offer it with <c>--platform</c>.</summary>
    public static PlatformCatalog CreateCatalog() => new PlatformCatalog()
        .Add(SdlPlatform.Profile)
        .Add(RaylibPlatform.Profile)
        .Add(SdlPlatform.CreateProfile("software"))
        .Add(HeadlessPlatform.Profile);

    public static int Run(Game game, string[] args) => Run(game, args, CreateCatalog());

    /// <summary>Runs with a catalog and request changes of the host's own (a custom platform, another audio device).</summary>
    public static int Run(Game game, string[] args, PlatformCatalog catalog, Func<PlatformRequest, PlatformRequest>? configure = null)
    {
        ContentFolder.DefaultRoot ??= BundleContentRoot(AppContext.BaseDirectory);
        return GameLauncher.Run(game, args, catalog, configure);
    }

    /// <summary>
    /// The content folder of a macOS application bundle (<c>Name.app/Contents/Resources/Content</c>) when the executable
    /// runs from <c>Contents/MacOS</c>, where a signed bundle keeps only code; null anywhere else, so the
    /// <c>Content</c> folder next to the executable stays the default.
    /// </summary>
    public static string? BundleContentRoot(string executableDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executableDirectory);
        var dir = new DirectoryInfo(Path.TrimEndingDirectorySeparator(executableDirectory));
        if (dir.Name != "MacOS" || dir.Parent is not { Name: "Contents" } contents) return null;
        if (Directory.Exists(Path.Combine(dir.FullName, "Content"))) return null;
        var resources = Path.Combine(contents.FullName, "Resources", "Content");
        return Directory.Exists(resources) ? resources : null;
    }
}

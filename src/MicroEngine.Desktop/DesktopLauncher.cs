using MicroEngine.Core;
using MicroEngine.Hosting;
using MicroEngine.Platform;
using MicroEngine.Raylib;
using MicroEngine.Sdl;

namespace MicroEngine.Desktop;

/// <summary>
/// Standard entry point for desktop games: the shared launcher model (<see cref="GameHost"/>, the command line of
/// <see cref="GameLauncher"/>) with the official desktop platforms, in order of preference: <c>sdl</c> (the default on
/// Windows, Linux and macOS), <c>raylib</c>, <c>sdl-software</c> (SDL's CPU renderer, for machines without a usable GPU
/// such as virtual machines and CI runners) and <c>headless</c>. A game's <c>Program.cs</c> is one line that hands
/// over its <c>Configure</c> entry point, the same one the browser and phone builds call; choosing a platform is the
/// player's or the test's business (<c>--platform</c>, <c>MICROENGINE_PLATFORM</c>), never the game's.
/// </summary>
/// <example>
/// <code>
/// return DesktopLauncher.Run(MainGame.Configure, args);
/// </code>
/// </example>
public static class DesktopLauncher
{
    /// <summary>A new catalog of the official desktop platforms; add a profile of your own to offer it with <c>--platform</c>.</summary>
    public static PlatformCatalog CreateCatalog() => new PlatformCatalog()
        .Add(SdlPlatform.Profile)
        .Add(RaylibPlatform.Profile)
        .Add(SdlPlatform.CreateProfile("software"))
        .Add(HeadlessPlatform.Profile);

    /// <summary>
    /// Runs the game a composition chooses (the game's <c>Configure</c> entry point, which calls
    /// <see cref="HostBuilderExtensions.UseGame"/>) with the command line <paramref name="args"/>. The desktop host's
    /// defaults come first (<see cref="Defaults"/>); <paramref name="configure"/> may replace any of them.
    /// </summary>
    /// <returns>The process exit code: 0 on success, 1 on a game or platform error, 2 on bad arguments.</returns>
    public static int Run(Action<HostBuilder> configure, string[] args) => GameLauncher.Run(args, configure, Defaults);

    /// <summary>Runs <paramref name="game"/> with the arguments of <see cref="GameLauncher"/> on the official desktop platforms; call it from <c>Program.cs</c>.</summary>
    /// <param name="game">The game to run.</param>
    /// <param name="args">The process arguments.</param>
    /// <returns>The process exit code: 0 on success, 1 on a game or platform error, 2 on bad arguments.</returns>
    public static int Run(Game game, string[] args) => Run(game, args, CreateCatalog());

    /// <summary>Runs with a catalog and request changes of the host's own (a custom platform, another audio device).</summary>
    public static int Run(Game game, string[] args, PlatformCatalog catalog, Func<PlatformRequest, PlatformRequest>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(catalog);
        return GameLauncher.Run(args, host =>
        {
            host.UsePlatforms(catalog).UseGame(() => game);
            if (configure is not null) host.ConfigureRequest(configure);
        }, Defaults);
    }

    /// <summary>
    /// The desktop host's defaults: the official platforms (<see cref="CreateCatalog"/>) and, inside a macOS application
    /// bundle, its <c>Contents/Resources/Content</c> folder as the content (<see cref="BundleContentRoot"/>).
    /// </summary>
    public static void Defaults(HostBuilder host)
    {
        ArgumentNullException.ThrowIfNull(host);
        host.UsePlatforms(CreateCatalog());
        if (BundleContentRoot(host.Environment.BaseDirectory) is { } bundle) host.Content = new FolderContentSource(bundle);
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

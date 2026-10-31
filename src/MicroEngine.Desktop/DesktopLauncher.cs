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

    public static int Run(Game game, string[] args) => GameLauncher.Run(game, args, CreateCatalog());

    /// <summary>Runs with a catalog and request changes of the host's own (a custom platform, another audio device).</summary>
    public static int Run(Game game, string[] args, PlatformCatalog catalog, Func<PlatformRequest, PlatformRequest>? configure = null) =>
        GameLauncher.Run(game, args, catalog, configure);
}

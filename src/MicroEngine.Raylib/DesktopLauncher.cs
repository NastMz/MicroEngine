using MicroEngine.Core;
using MicroEngine.Hosting;

namespace MicroEngine.Raylib;

/// <summary>
/// Entry point for desktop games on raylib only: the shared launcher model (<see cref="GameHost"/>, the arguments of
/// <see cref="GameLauncher"/>) with the <c>raylib</c> and <c>headless</c> platforms. Games that should run on the
/// recommended platform of each system use <c>MicroEngine.Desktop.DesktopLauncher</c> instead.
/// </summary>
public static class DesktopLauncher
{
    /// <summary>A new catalog with the <c>raylib</c> and <c>headless</c> platforms.</summary>
    public static PlatformCatalog CreateCatalog() => new PlatformCatalog().Add(RaylibPlatform.Profile).Add(HeadlessPlatform.Profile);

    /// <summary>Runs the game a composition chooses (the game's <c>Configure</c> entry point); returns the process exit code.</summary>
    public static int Run(Action<HostBuilder> configure, string[] args) => GameLauncher.Run(args, configure, host => host.UsePlatforms(CreateCatalog()));

    /// <summary>Runs <paramref name="game"/> with the arguments of <see cref="GameLauncher"/>; returns the process exit code.</summary>
    public static int Run(Game game, string[] args) => GameLauncher.Run(game, args, CreateCatalog());
}

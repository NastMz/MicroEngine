using MicroEngine.Core;

namespace MicroEngine.Raylib;

/// <summary>
/// Entry point for desktop games on raylib only: <see cref="GameLauncher"/> with the <c>raylib</c> and <c>headless</c>
/// platforms (see <see cref="GameLauncher"/> for the arguments). Games that should run on the recommended platform of
/// each system use <c>MicroEngine.Desktop.DesktopLauncher</c> instead.
/// </summary>
public static class DesktopLauncher
{
    public static PlatformCatalog CreateCatalog() => new PlatformCatalog().Add(RaylibPlatform.Profile).Add(HeadlessPlatform.Profile);

    public static int Run(Game game, string[] args) => GameLauncher.Run(game, args, CreateCatalog());
}

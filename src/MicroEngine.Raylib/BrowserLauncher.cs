using System.Runtime.Versioning;
using MicroEngine.Core;

namespace MicroEngine.Raylib;

/// <summary>
/// Entry point for browser builds (raylib compiled to WebAssembly). A page cannot block its only thread in
/// a game loop, so this returns a <see cref="GameSession"/>: the page starts it, ticks it once per
/// <c>requestAnimationFrame</c> and may start it again after the game ends. Saves go to localStorage
/// (<see cref="BrowserSaveStorage"/>); that choice is made here, before any game is built, so games
/// construct their <see cref="SaveStore"/> exactly as on desktop.
/// </summary>
[SupportedOSPlatform("browser")]
public static class BrowserLauncher
{
    public static GameSession CreateSession(Func<Game> createGame)
    {
        ArgumentNullException.ThrowIfNull(createGame);
        SaveStore.DefaultStorage = gameId => new BrowserSaveStorage(gameId);
        return new GameSession(createGame, game => new RaylibPlatform(game.Settings, new DesktopOptions()));
    }
}

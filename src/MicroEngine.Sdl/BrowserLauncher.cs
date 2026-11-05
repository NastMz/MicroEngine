using System.Runtime.Versioning;
using MicroEngine.Core;
using MicroEngine.Hosting;

namespace MicroEngine.Sdl;

/// <summary>
/// Entry point for browser builds (SDL 3 compiled to WebAssembly): the shared launcher model (<see cref="GameHost"/>)
/// for a page. A page cannot block its only thread in a game loop, so this returns a <see cref="GameSession"/>: the page
/// starts it with a <see cref="LaunchRequest"/> (a script in development packages), ticks it once per
/// <c>requestAnimationFrame</c> and may start it again after the game ends. Every run composes the game with the same
/// <c>Configure</c> entry point its desktop <c>Program.cs</c> uses, so what the game configures there (platforms, request
/// changes, engine services) reaches the browser too; a scripted run is in deterministic time with seed 1, exactly as on
/// desktop and phones. Saves go to localStorage by default (<see cref="SaveStore.DefaultStorage(string)"/>).
/// </summary>
[SupportedOSPlatform("browser")]
public static class BrowserLauncher
{
    /// <summary>
    /// The session a browser page drives. The host's defaults (<see cref="Defaults"/>) come first, then
    /// <paramref name="configure"/> (the game's <c>Configure</c> entry point), for every run.
    /// </summary>
    /// <param name="configure">The game's composition.</param>
    /// <param name="page">The page whose touches, screen, visibility and vibration the platforms read.</param>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    public static GameSession CreateSession(Action<HostBuilder> configure, BrowserPage page)
    {
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentNullException.ThrowIfNull(page);
        page.Vibrator ??= ms =>
        {
            try { return BrowserHaptics.Vibrate(ms); }
            catch (System.Runtime.InteropServices.JavaScript.JSException) { return false; }
        };
        return GameHost.CreateSession(request => GameHost.Create(new HostEnvironment(HostKind.Browser), request, configure, host => Defaults(host, page)));
    }

    /// <summary>The browser host's defaults: the SDL platform of <paramref name="page"/> as its only platform.</summary>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    public static void Defaults(HostBuilder host, BrowserPage page)
    {
        ArgumentNullException.ThrowIfNull(host);
        host.UsePlatforms(new PlatformCatalog().Add(SdlPlatform.BrowserProfile(page)));
    }
}

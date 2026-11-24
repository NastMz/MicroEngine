using CourtyardKey;
using MicroEngine.Core;
using MicroEngine.Desktop;
using MicroEngine.Hosting;

return DesktopLauncher.Run(Startup.Configure, args);

namespace CourtyardKey
{
    /// <summary>The game's composition, the one every build calls: desktop (this Program.cs), browser, Android and iOS.</summary>
    public static class Startup
    {
        /// <summary>
        /// COURTYARD_START (optional) jumps to a later checkpoint for captures and debugging: village (default), road,
        /// ruins, dungeon, trial, channel, boss, ending. It is a developer variable read from the host
        /// (<see cref="HostBuilder.Environment"/>: the shell's on desktop, the page's in browsers, a phone test's
        /// <c>--env</c> on phones), never from the process directly; a blank one counts as unset. Saves need no variable:
        /// a scripted run keeps its own, and <c>--saves DIR</c> names a folder for any run.
        /// </summary>
        public static void Configure(HostBuilder host)
        {
            var start = host.Environment.Variable("COURTYARD_START");
            host.UseGame(() => new CourtyardKeyGame(start));
        }
    }
}

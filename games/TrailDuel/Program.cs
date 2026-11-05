using MicroEngine.Core;
using MicroEngine.Desktop;
using MicroEngine.Hosting;
using TrailDuel;

return DesktopLauncher.Run(Startup.Configure, args);

namespace TrailDuel
{
    /// <summary>The game's composition, the one every build calls: desktop (this Program.cs), browser, Android and iOS.</summary>
    public static class Startup
    {
        /// <summary>
        /// The developer options, read from the host's variables (environment variables on desktop, the page's query in
        /// browsers, the launch's <c>env</c> option on phones): TRAIL_SAVE_DIR keeps saves and achievements in another
        /// folder (capture runs), TRAIL_DEBUG jumps to a screen or situation and TRAIL_EVOLVE picks who evolves under
        /// TRAIL_DEBUG=evolve.
        /// </summary>
        public static void Configure(HostBuilder host)
        {
            var saveDir = host.Environment.Variable("TRAIL_SAVE_DIR");
            var debug = host.Environment.Variable("TRAIL_DEBUG");
            var evolve = host.Environment.Variable("TRAIL_EVOLVE");
            host.UseGame(() => new TrailDuelGame(saveDir, debug, evolve));
        }
    }
}

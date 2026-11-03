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
        /// ruins, dungeon, trial, channel, boss. COURTYARD_SAVE_DIR (optional) keeps saves in another folder.
        /// </summary>
        public static void Configure(HostBuilder host)
        {
            var start = host.Environment.Variables("COURTYARD_START");
            var saveDir = host.Environment.Variable("COURTYARD_SAVE_DIR");
            host.UseGame(() => new CourtyardKeyGame(saveDir, start));
        }
    }
}

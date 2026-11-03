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
        /// <summary>TRAIL_SAVE_DIR (optional) keeps saves and achievements in another folder, e.g. for capture runs.</summary>
        public static void Configure(HostBuilder host)
        {
            var saveDir = host.Environment.Variable("TRAIL_SAVE_DIR");
            host.UseGame(() => new TrailDuelGame(saveDir));
        }
    }
}

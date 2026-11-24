using MeadowRun;
using MicroEngine.Core;
using MicroEngine.Desktop;
using MicroEngine.Hosting;

return DesktopLauncher.Run(Startup.Configure, args);

namespace MeadowRun
{
    /// <summary>The game's composition, the one every build calls: desktop (this Program.cs), browser, Android and iOS.</summary>
    public static class Startup
    {
        /// <summary>
        /// Developer shortcuts come from the host's variables (MEADOW_START, MEADOW_LEVEL, MEADOW_CHECKPOINT, MEADOW_FORM):
        /// environment variables on desktop, a phone test's --env, a page's query. The desktop launcher rejects unknown
        /// arguments. Where the saves go is the run's choice (--saves, or a store of its own for a scripted run).
        /// </summary>
        public static void Configure(HostBuilder host)
        {
            var launch = LaunchOptions.From(host.Environment);
            host.UseGame(() => new MeadowGame(launch));
        }
    }
}

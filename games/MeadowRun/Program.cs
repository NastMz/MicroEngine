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
        /// Developer shortcuts come from the host's variables (MEADOW_START, MEADOW_LEVEL, MEADOW_CHECKPOINT, MEADOW_FORM,
        /// MEADOW_SAVE_DIR): environment variables on desktop, a phone test's --env, a page's query. The desktop launcher
        /// rejects unknown arguments.
        /// </summary>
        public static void Configure(HostBuilder host)
        {
            var saveDir = host.Environment.Variable("MEADOW_SAVE_DIR");
            var launch = LaunchOptions.From(host.Environment);
            host.UseGame(() => new MeadowGame(saveDir, launch));
        }
    }
}

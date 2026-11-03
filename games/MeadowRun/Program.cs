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
        /// Developer shortcuts come from environment variables (MEADOW_START, MEADOW_LEVEL, MEADOW_CHECKPOINT,
        /// MEADOW_FORM, MEADOW_SAVE_DIR) because the desktop launcher rejects unknown arguments.
        /// </summary>
        public static void Configure(HostBuilder host)
        {
            var saveDir = host.Environment.Variable("MEADOW_SAVE_DIR");
            host.UseGame(() => new MeadowGame(saveDir, LaunchOptions.FromEnvironment()));
        }
    }
}

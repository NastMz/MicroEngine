using MicroEngine.Core;
using MicroEngine.Desktop;
using MicroEngine.Hosting;
using NeonRift;

return DesktopLauncher.Run(Startup.Configure, args);

namespace NeonRift
{
    /// <summary>The game's composition, the one every build calls: desktop (this Program.cs), browser, Android and iOS.</summary>
    public static class Startup
    {
        /// <summary>
        /// Developer shortcuts come from environment variables (RIFT_START, RIFT_STAGE, RIFT_CHECKPOINT, RIFT_WEAPON,
        /// RIFT_GOD=1 for invulnerable capture runs, RIFT_AUTO=1 to let the autopilot play, RIFT_SAVE_DIR for a separate
        /// achievements folder) because the desktop launcher rejects unknown arguments.
        /// </summary>
        public static void Configure(HostBuilder host)
        {
            var saveDir = host.Environment.Variable("RIFT_SAVE_DIR");
            host.UseGame(() => new RiftGame(LaunchOptions.FromEnvironment(), saveDirectory: saveDir));
        }
    }
}

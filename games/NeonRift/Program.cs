using NeonRift;
using MicroEngine.Raylib;

// Developer shortcuts come from environment variables (RIFT_START, RIFT_STAGE, RIFT_CHECKPOINT,
// RIFT_WEAPON, RIFT_GOD=1 for invulnerable capture runs,
// RIFT_AUTO=1 to let the autopilot play, RIFT_SAVE_DIR for a separate achievements folder)
// because the desktop launcher rejects unknown arguments.
var saveDir = Environment.GetEnvironmentVariable("RIFT_SAVE_DIR");
return DesktopLauncher.Run(new RiftGame(LaunchOptions.FromEnvironment(), saveDirectory: string.IsNullOrWhiteSpace(saveDir) ? null : saveDir), args);

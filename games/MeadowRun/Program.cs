using MeadowRun;
using MicroEngine.Desktop;

// Developer shortcuts come from environment variables (MEADOW_START, MEADOW_LEVEL, MEADOW_CHECKPOINT,
// MEADOW_FORM, MEADOW_SAVE_DIR) because the desktop launcher rejects unknown arguments.
var saveDir = Environment.GetEnvironmentVariable("MEADOW_SAVE_DIR");
return DesktopLauncher.Run(new MeadowGame(string.IsNullOrWhiteSpace(saveDir) ? null : saveDir, LaunchOptions.FromEnvironment()), args);

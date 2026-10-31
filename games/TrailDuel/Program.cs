using MicroEngine.Desktop;
using TrailDuel;

// TRAIL_SAVE_DIR (optional) keeps saves and achievements in another folder, e.g. for capture runs.
var saveDir = Environment.GetEnvironmentVariable("TRAIL_SAVE_DIR");
return DesktopLauncher.Run(new TrailDuelGame(string.IsNullOrWhiteSpace(saveDir) ? null : saveDir), args);

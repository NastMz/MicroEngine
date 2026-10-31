using CourtyardKey;
using MicroEngine.Desktop;

// COURTYARD_START (optional) jumps to a later checkpoint for captures and debugging:
// village (default), road, ruins, dungeon, trial, channel, boss.
var start = Environment.GetEnvironmentVariable("COURTYARD_START");
var saveDir = Environment.GetEnvironmentVariable("COURTYARD_SAVE_DIR");
return DesktopLauncher.Run(new CourtyardKeyGame(saveDir, start), args);

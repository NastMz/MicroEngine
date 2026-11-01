using MicroEngine.Desktop;
using GemGarden;

// Runs the game on the desktop platforms: SDL 3 by default; --platform raylib, --list-platforms and the other launcher
// options work from the command line (microengine run -- --list-platforms).
return DesktopLauncher.Run(new MainGame(), args);

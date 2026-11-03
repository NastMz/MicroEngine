using MicroEngine.Desktop;
using MicroEngine.Sdl;
using Examples;

#region launch
// The official desktop catalog (sdl, raylib, sdl-software, headless) plus one platform of our own:
// "--platform sdl-reported" runs SDL and reports it at start (ReportingPlatform, Extending.cs).
var catalog = DesktopLauncher.CreateCatalog().Add(ReportingPlatform.Wrap(SdlPlatform.Profile));
return DesktopLauncher.Run(new ExamplesGame(), args, catalog);
#endregion

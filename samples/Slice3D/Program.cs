// The 3D vertical slice: an ordinary MicroEngine game on the tier-1 graphics device (docs/manual/rendering.md, "The 3D
// path"). Its composition asks for SDL's sdl-gpu device; the engine's loop steps the scene's world at 30 Hz and draws it
// at the frame's blend; the scene draws the world in 3D, a 2D HUD over it on the canvas, and a label in the UI view.
//
//   dotnet run --project samples/Slice3D                                   a window; Esc quits
//   dotnet run --project samples/Slice3D -- --script slice.script --hidden  a deterministic run with a capture
//   dotnet run --project samples/Slice3D -- --measure-e12                    cost of the tier-1 surface copy at Present
//   dotnet run --project samples/Slice3D -- --write-crate                    rewrites Content/crate.glb
using MicroEngine.Desktop;
using Slice3D;

System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
if (args is ["--write-crate"])
{
    var file = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Content", "crate.glb"));
    File.WriteAllBytes(file, CrateModel.Glb());
    Console.WriteLine($"wrote {file}");
    return 0;
}
if (args is ["--measure-e12", ..]) return MeasureE12.Run(args.Contains("--api") ? args[Array.IndexOf(args, "--api") + 1] : null);
return DesktopLauncher.Run(Slice3DGame.Configure, args);

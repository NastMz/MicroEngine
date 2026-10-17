using MapExplorer;
using MapExplorer.Model;
using MicroEngine.Desktop;
using MicroEngine.Loop;
using Raylib_cs;

try
{
    using (var window = new DesktopPlatform(new(640, 360, "MicroEngine map viewport verification")))
    {
        void Draw(MapWalker game, params (int X, int Y, Rgba Expected)[] samples)
        {
            for (int frame = 0; frame < 2; frame++) { window.Begin(); MapRenderer.Draw(window.Canvas, game); window.End(); }
            var image = Raylib.LoadImageFromScreen();
            try
            {
                foreach (var (x, y, expected) in samples)
                {
                    var actual = Raylib.GetImageColor(image, x, y);
                    if (actual.R != expected.R || actual.G != expected.G || actual.B != expected.B)
                        throw new InvalidOperationException($"Map pixel {x},{y}: {actual.R},{actual.G},{actual.B}.");
                }
            }
            finally { Raylib.UnloadImage(image); }
        }
        var initial = new MapWalker(); var scrolled = MapWalker.Create(Zone.Meadow, 508, 304);
        if (initial.CameraX != 0 || scrolled.CameraX != 200 || initial.CameraY != 140 || scrolled.CameraY != 140)
            throw new InvalidOperationException("Probe camera fixture mismatch.");
        Draw(initial, (410, 110, new(180, 100, 70)), (210, 110, new(24, 40, 32)), (45, 170, new(240, 180, 65)));
        Draw(scrolled, (210, 110, new(180, 100, 70)), (410, 110, new(24, 40, 32)), (310, 170, new(240, 180, 65)));
        var clipped = MapWalker.Create(Zone.Meadow, 558, 40);
        if (clipped.CameraX != 250 || clipped.CameraY != 0) throw new InvalidOperationException("Clipping fixture mismatch.");
        Draw(clipped, (0, 90, new(95, 130, 165)), (49, 90, new(95, 130, 165)), (50, 90, new(24, 40, 32)));
        Draw(MapWalker.Create(Zone.Ruins, 8, 304), (2, 110, new(70, 200, 210)), (170, 10, new(95, 130, 165)), (500, 300, new(32, 24, 48)));
        Console.WriteLine("map:viewport:verified");
    }
    Console.WriteLine("map:closed"); return 0;
}
catch (Exception error) { Console.Error.WriteLine($"map:failed:{error.GetType().Name}:{error.Message}"); return 1; }

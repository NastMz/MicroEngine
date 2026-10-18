using MicroEngine.Desktop;
using MicroEngine.Loop;
using Raylib_cs;

try
{
    using (var window = new DesktopPlatform(new(100, 100, "MicroEngine region verification")))
    {
        // Magenta border columns must never bleed into the selected middle 2x2 region.
        var pixels = new PixelImage(4, 2, new Rgba[]
        {
            new(255, 0, 255), new(255, 0, 0), new(0, 255, 0), new(255, 0, 255),
            new(255, 0, 255), new(0, 0, 255), new(255, 0, 0, 128), new(255, 0, 255)
        });
        IImageResource? image = null;
        for (int frame = 0; frame < 2; frame++)
        {
            window.Begin(); var canvas = (IImageRegionCanvas)window.Canvas; image ??= canvas.CreateImage(pixels);
            canvas.Clear(new(0, 0, 0)); canvas.DrawImageRegion(image, new(1, 0, 2, 2), new(20, 20, 40, 40));
            // A transparent subregion must preserve existing destination color.
            var transparent = canvas.CreateImage(new(1, 1, new Rgba[] { new(0, 0, 0, 0) }));
            canvas.DrawImageRegion(transparent, new(0, 0, 1, 1), new(20, 20, 20, 20));
            window.End(); transparent.Dispose();
        }
        var readback = Raylib.LoadImageFromScreen();
        try
        {
            foreach (var (x, y, r, g, b) in new[] { (20, 20, 255, 0, 0), (39, 25, 255, 0, 0), (40, 25, 0, 255, 0),
                (59, 20, 0, 255, 0), (25, 40, 0, 0, 255), (25, 59, 0, 0, 255), (19, 20, 0, 0, 0), (60, 20, 0, 0, 0) })
            {
                var color = Raylib.GetImageColor(readback, x, y);
                if (color.R != r || color.G != g || color.B != b)
                    throw new InvalidOperationException($"Pixel {x},{y}: {color.R},{color.G},{color.B}, expected {r},{g},{b}.");
            }
            var alpha = Raylib.GetImageColor(readback, 55, 55);
            if (alpha.R is < 127 or > 129 || alpha.G != 0 || alpha.B != 0) throw new InvalidOperationException("Region alpha mismatch.");
        }
        finally { Raylib.UnloadImage(readback); }
        image!.Dispose(); image.Dispose();
        window.Begin(); ((IImageRegionCanvas)window.Canvas).CreateImage(pixels); window.End();
        Console.WriteLine("regions:pixels:verified");
    }
    Console.WriteLine("regions:closed"); return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine($"regions:failed:{error.GetType().Name}:{error.Message}"); return 1;
}

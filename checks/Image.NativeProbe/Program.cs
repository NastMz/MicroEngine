using MicroEngine.Desktop;
using MicroEngine.Loop;
using Raylib_cs;

try
{
    using (var window = new DesktopPlatform(new(100, 100, "MicroEngine image verification")))
    {
        var pixels = new PixelImage(2, 2, new Rgba[] { new(255, 0, 0), new(0, 255, 0), new(0, 0, 255), new(0, 0, 0, 0) });
        IImageResource? resource = null;
        IImageResource? translucent = null;
        for (int frame = 0; frame < 2; frame++)
        {
            window.Begin(); var canvas = (IImageCanvas)window.Canvas;
            resource ??= canvas.CreateImage(pixels);
            translucent ??= canvas.CreateImage(new(1, 1, new Rgba[] { new(255, 0, 0, 128) }));
            canvas.Clear(new(0, 0, 0)); canvas.DrawImage(resource, new(20, 20, 40, 40));
            canvas.DrawImage(translucent, new(70, 20, 10, 10)); window.End();
        }
        var readback = Raylib.LoadImageFromScreen();
        try
        {
            foreach (var (x, y, r, g, b) in new[] { (25, 25, 255, 0, 0), (55, 25, 0, 255, 0), (25, 55, 0, 0, 255), (55, 55, 0, 0, 0), (39, 25, 255, 0, 0), (40, 25, 0, 255, 0) })
            {
                var color = Raylib.GetImageColor(readback, x, y);
                if (color.R != r || color.G != g || color.B != b)
                    throw new InvalidOperationException($"Pixel {x},{y}: {color.R},{color.G},{color.B} expected {r},{g},{b}.");
            }
            var alpha = Raylib.GetImageColor(readback, 75, 25);
            if (alpha.R is < 127 or > 129 || alpha.G != 0 || alpha.B != 0)
                throw new InvalidOperationException($"Source-over alpha mismatch: {alpha.R},{alpha.G},{alpha.B}.");
        }
        finally { Raylib.UnloadImage(readback); }
        resource!.Dispose(); resource.Dispose();
        window.Begin(); var fallback = ((IImageCanvas)window.Canvas).CreateImage(pixels); window.End();
        window.Dispose(); fallback.Dispose();
        Console.WriteLine("images:pixels:verified");
    }
    Console.WriteLine("images:closed"); return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine($"images:failed:{error.GetType().Name}:{error.Message}"); return 1;
}

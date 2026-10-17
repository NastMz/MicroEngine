using MicroEngine.Desktop;
using MicroEngine.Loop;
using Raylib_cs;

try
{
    using (var window = new DesktopPlatform(new(200, 80, "MicroEngine label verification")))
    {
        void Draw(bool label)
        {
            // Two identical frames avoid relying on which swapped buffer is readable.
            for (var frame = 0; frame < 2; frame++)
            {
                window.Begin();
                var canvas = window.Canvas;
                canvas.Clear(new Rgba(0, 0, 0));
                if (canvas is not ILabelCanvas labels) throw new InvalidOperationException("Desktop label capability missing.");
                if (label) labels.Label("Advance", 20, 20, 20, new Rgba(255, 255, 255));
                window.End();
            }
        }
        int CountRgb()
        {
            var image = Raylib.LoadImageFromScreen();
            try
            {
                var count = 0;
                for (var y = 20; y < 50; y++)
                for (var x = 20; x < 160; x++)
                {
                    var color = Raylib.GetImageColor(image, x, y);
                    if (color.R != 0 || color.G != 0 || color.B != 0) count++;
                }
                return count;
            }
            finally { Raylib.UnloadImage(image); }
        }
        Draw(false); var baseline = CountRgb();
        Draw(true); var glyph = CountRgb();
        if (baseline != 0 || glyph <= 10) throw new InvalidOperationException($"Unexpected label pixels: baseline={baseline}, glyph={glyph}.");
        Console.WriteLine($"labels:baseline:{baseline};pixels:{glyph}");
    }
    Console.WriteLine("labels:closed");
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine($"labels:failed:{error.GetType().Name}:{error.Message}");
    return 1;
}

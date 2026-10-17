using System.Diagnostics;
using MicroEngine.Loop;
using Raylib_cs;

namespace MicroEngine.Desktop;

internal sealed class RaylibApi : IDesktopApi
{
    private long _last;
    public bool Ready => Raylib.IsWindowReady();
    public void Init(DesktopWindowOptions options)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Desktop validation currently supports Windows only.");
        Raylib.InitWindow(options.Width, options.Height, options.Title);
        Raylib.SetTargetFPS(60);
        _last = Stopwatch.GetTimestamp();
    }
    public NativeFrame Read()
    {
        var now = Stopwatch.GetTimestamp();
        var elapsed = Stopwatch.GetElapsedTime(_last, now);
        _last = now;
        var actions = DesktopActions.Read(key => Raylib.IsKeyDown(key), key => Raylib.IsKeyPressed(key));
        return new(elapsed, Raylib.IsWindowFocused() && !Raylib.IsWindowMinimized(), Raylib.WindowShouldClose(), actions.Held, actions.Pressed);
    }
    public void Begin() => Raylib.BeginDrawing();
    public void End() => Raylib.EndDrawing();
    public void Clear(Rgba color) => Raylib.ClearBackground(new Color(color.R, color.G, color.B, color.A));
    public void Fill(Rect rectangle, Rgba color) => Raylib.DrawRectangleRec(new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height), new Color(color.R, color.G, color.B, color.A));
    public void Close() => Raylib.CloseWindow();
    public object CreateImage(PixelImage image)
    {
        Image cpu = default;
        Texture2D texture = default;
        Exception? failure = null;
        try
        {
            cpu = Raylib.GenImageColor(image.Width, image.Height, new Color(0, 0, 0, 0));
            if (!Raylib.IsImageValid(cpu)) throw new InvalidOperationException("Native CPU image creation failed.");
            for (int y = 0; y < image.Height; y++)
            for (int x = 0; x < image.Width; x++)
            {
                var pixel = image.Pixels[y * image.Width + x];
                Raylib.ImageDrawPixel(ref cpu, x, y, new Color(pixel.R, pixel.G, pixel.B, pixel.A));
            }
            texture = Raylib.LoadTextureFromImage(cpu);
            if (!Raylib.IsTextureValid(texture)) throw new InvalidOperationException("Native texture upload failed.");
            Raylib.SetTextureFilter(texture, TextureFilter.Point);
        }
        catch (Exception error) { failure = error; }
        // CPU storage never escapes this factory, even when upload or filter setup fails.
        if (cpu.Width != 0)
        {
            try { Raylib.UnloadImage(cpu); }
            catch (Exception error) { failure = failure is null ? error : new AggregateException(failure, error); }
        }
        if (failure is null)
        {
            try { return texture; }
            catch (Exception error) { failure = error; } // Include boxing/allocation failure in cleanup.
        }
        if (texture.Id != 0)
        {
            try { Raylib.UnloadTexture(texture); }
            catch (Exception error) { failure = new AggregateException(failure, error); }
        }
        System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        throw new InvalidOperationException("Unreachable image factory state.");
    }
    public void DrawImage(object image, Rect destination)
    {
        var texture = (Texture2D)image;
        Raylib.DrawTexturePro(texture, new Rectangle(0, 0, texture.Width, texture.Height),
            new Rectangle(destination.X, destination.Y, destination.Width, destination.Height),
            System.Numerics.Vector2.Zero, 0, new Color(255, 255, 255, 255));
    }
    public void ReleaseImage(object image) => Raylib.UnloadTexture((Texture2D)image);
    public void DrawImageRegion(object image, Rect source, Rect destination)
        => Raylib.DrawTexturePro((Texture2D)image,
            new Rectangle(source.X, source.Y, source.Width, source.Height),
            new Rectangle(destination.X, destination.Y, destination.Width, destination.Height),
            System.Numerics.Vector2.Zero, 0, new Color(255, 255, 255, 255));
    public void Label(string text, int x, int y, int pixelSize, Rgba color)
        => Raylib.DrawText(text, x, y, pixelSize, new Color(color.R, color.G, color.B, color.A));
}

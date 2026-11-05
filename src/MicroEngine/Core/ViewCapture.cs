using MicroEngine.Graphics;
using Dev = MicroEngine.Graphics.Devices;

namespace MicroEngine.Core;

/// <summary>
/// The captures of a platform that has a UI view (<see cref="IScreenView"/>), a capability of the SDL, raylib and headless
/// platforms (<see cref="Platform.IPlatformHost.TryGetCapability{T}"/>). A run's captures (<see cref="IPlatform.SaveScreenshot"/>)
/// show what the player sees: a frame that drew a UI is captured as the window, the world scaled into its frame with the
/// UI view composited over it (<see cref="CaptureScreen"/>); a frame that drew none is captured as the canvas, as before
/// the UI view existed. Tests that need the world alone read <see cref="CaptureWorld"/> or write
/// <see cref="SaveWorldScreenshot"/>.
/// </summary>
public interface IViewCapture
{
    /// <summary>Whether the last frame drew a UI view.</summary>
    bool FrameHasUi { get; }
    /// <summary>The last frame's world view: the canvas, opaque, at the canvas's size.</summary>
    PixelImage CaptureWorld();
    /// <summary>The last frame's UI view at the window's size, premultiplied and transparent where nothing was drawn; null when the frame drew none.</summary>
    PixelImage? CaptureUi();
    /// <summary>What the player saw: with a UI, the window (letterbox, world, UI over it) at the window's size; without one, <see cref="CaptureWorld"/>.</summary>
    PixelImage CaptureScreen();
    /// <summary>Writes the world view as a PNG, at the size the platform's own captures of the canvas have.</summary>
    void SaveWorldScreenshot(string path);
}

/// <summary>Builds the image the player sees from the two views, as the platforms compose a frame.</summary>
public static class ScreenCapture
{
    /// <summary>
    /// The window of <paramref name="screen"/> pixels: <paramref name="letterbox"/> (opaque), the opaque
    /// <paramref name="world"/> scaled into <paramref name="frame"/> with <paramref name="upscale"/>'s magnification filter,
    /// then the premultiplied <paramref name="ui"/> (window-sized, or null) over everything.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The screen size is not positive.</exception>
    public static PixelImage Compose(PixelImage world, PixelImage? ui, (int Width, int Height) screen, RectF frame, Color letterbox, Dev.SamplerDescription upscale)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (screen.Width <= 0 || screen.Height <= 0) throw new ArgumentOutOfRangeException(nameof(screen), "The screen size must be positive.");
        var image = new PixelImage(screen.Width, screen.Height);
        var border = letterbox with { A = 255 };
        bool linear = upscale.Mag == Dev.Filter.Linear;
        for (int y = 0; y < screen.Height; y++)
            for (int x = 0; x < screen.Width; x++)
            {
                float cx = x + 0.5f, cy = y + 0.5f;
                bool inside = frame.Width > 0 && frame.Height > 0 && cx >= frame.X && cx < frame.Right && cy >= frame.Y && cy < frame.Bottom;
                if (!inside) { image[x, y] = border; continue; }
                float u = (cx - frame.X) / frame.Width * world.Width, v = (cy - frame.Y) / frame.Height * world.Height;
                image[x, y] = (linear ? Bilinear(world, u, v) : world[Math.Min(world.Width - 1, (int)u), Math.Min(world.Height - 1, (int)v)]) with { A = 255 };
            }
        if (ui is null) return image;
        int w = Math.Min(ui.Width, screen.Width), h = Math.Min(ui.Height, screen.Height);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var s = ui[x, y];
                if (s.A == 0 && s.R == 0 && s.G == 0 && s.B == 0) continue;
                var d = image[x, y];
                int keep = 255 - s.A;
                image[x, y] = new Color(Over(s.R, d.R, keep), Over(s.G, d.G, keep), Over(s.B, d.B, keep), 255);
            }
        return image;
    }

    private static byte Over(byte source, byte destination, int keep) => (byte)Math.Min(255, source + (destination * keep + 127) / 255);

    private static Color Bilinear(PixelImage image, float u, float v)
    {
        float fx = u - 0.5f, fy = v - 0.5f;
        int x0 = (int)MathF.Floor(fx), y0 = (int)MathF.Floor(fy);
        float tx = fx - x0, ty = fy - y0;
        Color At(int x, int y) => image[Math.Clamp(x, 0, image.Width - 1), Math.Clamp(y, 0, image.Height - 1)];
        return Color.Lerp(Color.Lerp(At(x0, y0), At(x0 + 1, y0), tx), Color.Lerp(At(x0, y0 + 1), At(x0 + 1, y0 + 1), tx), ty);
    }
}

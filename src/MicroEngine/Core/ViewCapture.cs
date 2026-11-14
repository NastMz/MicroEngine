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

/// <summary>Builds the image the player sees from the two views, as the platforms compose a frame, prescale included.</summary>
public static class ScreenCapture
{
    /// <summary>
    /// The window of <paramref name="screen"/> pixels: <paramref name="letterbox"/> (opaque), the opaque
    /// <paramref name="world"/> scaled into <paramref name="frame"/> with <paramref name="upscale"/>'s magnification filter,
    /// then the premultiplied <paramref name="ui"/> (window-sized, or null) over everything.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The screen size is not positive.</exception>
    public static PixelImage Compose(PixelImage world, PixelImage? ui, (int Width, int Height) screen, RectF frame, Color letterbox, Dev.SamplerDescription upscale) =>
        Compose(world, ui, screen, frame, letterbox, upscale, 1, 1);

    /// <summary>
    /// The window as <paramref name="presentation"/> draws it: <see cref="Compose(PixelImage, PixelImage?, ValueTuple{int, int}, RectF, Color, Dev.SamplerDescription)"/>
    /// with its upscale sampler, the world enlarged first by its prescale (<see cref="Presentation.PrescaleFor"/>) with
    /// nearest sampling, as a platform's window samples it. Without a prescale it is that composition exactly.
    /// </summary>
    /// <param name="world">The world view: the canvas, opaque.</param>
    /// <param name="ui">The UI view at the window's size, premultiplied, or null.</param>
    /// <param name="screen">The window's size in pixels.</param>
    /// <param name="frame">Where the canvas lands in the window, in window pixels.</param>
    /// <param name="letterbox">The colour of the window around the frame.</param>
    /// <param name="presentation">The game's presentation: its upscale sampler and prescale.</param>
    /// <param name="maxTargetSize">The largest side the enlarged canvas may have: a device's largest render target.</param>
    /// <exception cref="ArgumentOutOfRangeException">The screen size is not positive.</exception>
    public static PixelImage Compose(PixelImage world, PixelImage? ui, (int Width, int Height) screen, RectF frame, Color letterbox, Presentation presentation, int maxTargetSize = int.MaxValue)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(presentation);
        var (factorX, factorY) = frame.Width > 0 && frame.Height > 0 ? presentation.PrescaleFor(frame, world.Width, world.Height, maxTargetSize) : (1, 1);
        return Compose(world, ui, screen, frame, letterbox, presentation.Upscale, factorX, factorY);
    }

    /// <summary>The composition, sampling the world as if each of its pixels were repeated by the two factors (never built as an image).</summary>
    private static PixelImage Compose(PixelImage world, PixelImage? ui, (int Width, int Height) screen, RectF frame, Color letterbox, Dev.SamplerDescription upscale, int factorX, int factorY)
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
                float u = (cx - frame.X) / frame.Width * (world.Width * factorX), v = (cy - frame.Y) / frame.Height * (world.Height * factorY);
                image[x, y] = (linear ? Bilinear(world, u, v, factorX, factorY) : world[Math.Min(world.Width - 1, (int)u / factorX), Math.Min(world.Height - 1, (int)v / factorY)]) with { A = 255 };
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

    private static Color Bilinear(PixelImage image, float u, float v, int factorX, int factorY)
    {
        float fx = u - 0.5f, fy = v - 0.5f;
        int x0 = (int)MathF.Floor(fx), y0 = (int)MathF.Floor(fy);
        float tx = fx - x0, ty = fy - y0;
        Color At(int x, int y) => image[Math.Clamp(x, 0, image.Width * factorX - 1) / factorX, Math.Clamp(y, 0, image.Height * factorY - 1) / factorY];
        return Color.Lerp(Color.Lerp(At(x0, y0), At(x0 + 1, y0), tx), Color.Lerp(At(x0, y0 + 1), At(x0 + 1, y0 + 1), tx), ty);
    }
}

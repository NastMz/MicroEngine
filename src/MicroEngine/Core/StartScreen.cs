using System.Numerics;
using MicroEngine.Graphics;

namespace MicroEngine.Core;

/// <summary>
/// The screen a game opens on while it loads: its start-screen image (<c>MicroEngineSplashImage</c>, by default the
/// icon) centred on its colour (<c>MicroEngineSplashColor</c>), and "Made with MicroEngine" below when the game asks
/// for it (<c>MicroEngineSplashBranding</c>). Phones and browsers show the same image and colour before the engine
/// starts (launch screens, the loading page), so the player sees one continuous screen from the tap to the first frame.
/// <see cref="GameLoop"/> draws it once, before <see cref="Game.Load"/>, on platforms that run in real time; it adds no
/// delay, and an error while loading is reported as before.
/// </summary>
public static class StartScreen
{
    /// <summary>The text shown when the game turns branding on.</summary>
    public const string Branding = "Made with MicroEngine";

    /// <summary>
    /// The image size on a <paramref name="canvasWidth"/> by <paramref name="canvasHeight"/> canvas: half the shorter
    /// side, in whole pixels, keeping the image's shape.
    /// </summary>
    public static (int Width, int Height) ImageSize(PixelImage image, int canvasWidth, int canvasHeight)
    {
        ArgumentNullException.ThrowIfNull(image);
        int box = Math.Max(8, Math.Min(canvasWidth, canvasHeight) / 2);
        return image.FitInside(box, box);
    }

    /// <summary>Draws the start screen of <paramref name="identity"/> into <paramref name="r"/>; textures made from <paramref name="graphics"/> are released before it returns.</summary>
    public static void Draw(IRenderer r, IGraphicsDevice graphics, AppIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(r);
        ArgumentNullException.ThrowIfNull(graphics);
        ArgumentNullException.ThrowIfNull(identity);
        r.Clear(identity.SplashColor);
        if (identity.SplashImage is { } image)
        {
            var (w, h) = ImageSize(image, r.Width, r.Height);
            using var texture = graphics.CreateTexture(image.Resized(w, h));
            float top = MathF.Floor((r.Height - h) / 2f) - (identity.SplashBranding ? 6 : 0);
            r.Draw(texture, new RectF(0, 0, w, h), new Vector2(MathF.Floor((r.Width - w) / 2f), top), Color.White);
        }
        if (identity.SplashBranding)
        {
            using var font = BitmapFont.CreateDefault(graphics);
            var c = identity.SplashColor;
            // Light text on dark colours, dark text on light ones.
            bool light = c.R * 0.299f + c.G * 0.587f + c.B * 0.114f > 150;
            font.DrawCentered(r, Branding, new Vector2(r.Width / 2f, r.Height - 12), light ? Color.Hex("1c1b2e") : Color.Hex("f4f1de"));
        }
    }
}

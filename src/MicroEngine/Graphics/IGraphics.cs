using System.Numerics;

namespace MicroEngine.Graphics;

/// <summary>A GPU (or headless) texture owned by the active graphics device.</summary>
public abstract class Texture : IDisposable
{
    /// <summary>Width in pixels.</summary>
    public int Width { get; }
    /// <summary>Height in pixels.</summary>
    public int Height { get; }
    /// <summary>Whether <see cref="Dispose"/> was called. Drawing a disposed texture fails.</summary>
    public bool IsDisposed { get; private set; }
    /// <summary>Records the size of a texture created by a graphics device.</summary>
    protected Texture(int width, int height) => (Width, Height) = (width, height);
    /// <summary>The whole texture as a source rectangle: (0, 0, <see cref="Width"/>, <see cref="Height"/>).</summary>
    public RectF Bounds => new(0, 0, Width, Height);
    /// <summary>Releases the device texture. Safe to call more than once. Whoever created the texture (or the atlas or font that owns it) disposes it.</summary>
    public void Dispose()
    {
        if (IsDisposed) return;
        IsDisposed = true;
        Release();
        GC.SuppressFinalize(this);
    }
    /// <summary>Frees the device resource; called once by <see cref="Dispose"/>.</summary>
    protected abstract void Release();
}

/// <summary>
/// How drawn pixels combine with the canvas (see <see cref="IRenderer.SetBlendMode"/>): <c>Alpha</c> (normal
/// transparency, the default), <c>Additive</c> (adds light: glows, sparks, fire) and <c>Multiply</c> (darkens: shadows, tints).
/// </summary>
public enum BlendMode { Alpha, Additive, Multiply }

/// <summary>Per-draw sprite options. Origin is in source pixels and is also the rotation pivot.</summary>
/// <param name="FlipX">Mirrors the sprite horizontally.</param>
/// <param name="FlipY">Mirrors the sprite vertically.</param>
/// <param name="Rotation">Clockwise rotation in radians around <paramref name="Origin"/>.</param>
/// <param name="Origin">Rotation and scale pivot, in source pixels from the top-left of the source rectangle.</param>
/// <param name="Scale">Scale factors per axis; null draws at 1:1.</param>
public readonly record struct DrawOptions(
    bool FlipX = false,
    bool FlipY = false,
    float Rotation = 0f,
    Vector2 Origin = default,
    Vector2? Scale = null)
{
    /// <summary>No flip, rotation or scale: the same as <c>default</c>.</summary>
    public static readonly DrawOptions None = new();
}

/// <summary>
/// Immediate-mode 2D drawing in virtual pixels. Coordinates go through the camera
/// pushed with <see cref="PushCamera"/>; with no camera they are screen space.
/// </summary>
public interface IRenderer
{
    /// <summary>Width of the virtual canvas in pixels.</summary>
    int Width { get; }
    /// <summary>Height of the virtual canvas in pixels.</summary>
    int Height { get; }
    /// <summary>Fills the whole canvas with <paramref name="color"/>. The game clears to <see cref="Core.GameSettings.Background"/> before the scenes draw.</summary>
    void Clear(Color color);
    /// <summary>Draws the <paramref name="source"/> part of <paramref name="texture"/> with its top-left at <paramref name="position"/>, multiplied by <paramref name="tint"/>.</summary>
    /// <param name="texture">The texture; must not be disposed.</param>
    /// <param name="source">The rectangle of the texture to draw, in texture pixels.</param>
    /// <param name="position">Where the top-left of the unrotated, scaled source lands, in virtual pixels (world pixels under a camera).</param>
    /// <param name="tint">Colour multiplier; <see cref="Color.White"/> draws the texture unchanged.</param>
    /// <param name="options">Flip, rotation, pivot and scale.</param>
    void Draw(Texture texture, RectF source, Vector2 position, Color tint, in DrawOptions options = default);
    /// <summary>Draws the <paramref name="source"/> part of <paramref name="texture"/> scaled to fill <paramref name="destination"/>.</summary>
    void DrawStretched(Texture texture, RectF source, RectF destination, Color tint);
    /// <summary>Fills a rectangle.</summary>
    void FillRect(RectF rect, Color color);
    /// <summary>Fills a circle; <paramref name="radius"/> is in pixels.</summary>
    void FillCircle(Vector2 center, float radius, Color color);
    /// <summary>Fills a triangle.</summary>
    void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color);
    /// <summary>Draws a straight line <paramref name="thickness"/> pixels wide. A zero-length line draws nothing.</summary>
    void DrawLine(Vector2 from, Vector2 to, float thickness, Color color);
    /// <summary>
    /// Draws in the world coordinates of <paramref name="camera"/> until <see cref="PopCamera"/>: its <see cref="Camera2D.RenderOffset"/>
    /// replaces the current offset (cameras do not combine). Every push needs a pop in the same frame.
    /// </summary>
    void PushCamera(Camera2D camera);
    /// <summary>Returns to the coordinates in use before the last <see cref="PushCamera"/>.</summary>
    /// <exception cref="InvalidOperationException">No camera was pushed.</exception>
    void PopCamera();
    /// <summary>Sets how later draws combine with the canvas. Every frame starts with <see cref="BlendMode.Alpha"/>.</summary>
    void SetBlendMode(BlendMode mode);
    /// <summary>Restricts drawing to a screen-space rectangle; pass null to remove.</summary>
    void SetClip(RectF? screenRect);
}

/// <summary>Creates device resources. Separated so headless tests can provide fakes.</summary>
public interface IGraphicsDevice
{
    /// <summary>Uploads <paramref name="image"/> as a texture. The caller owns the texture and disposes it.</summary>
    Texture CreateTexture(PixelImage image);
}

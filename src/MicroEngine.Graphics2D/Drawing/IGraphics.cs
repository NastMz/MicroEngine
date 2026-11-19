using System.Numerics;

namespace MicroEngine.Graphics;

/// <summary>
/// How a texture is sampled when it is drawn bigger or smaller than its pixels, or between them: <c>Nearest</c> keeps
/// hard pixel edges (pixel art), <c>Linear</c> blends neighbouring pixels (smooth, high-resolution art and text).
/// </summary>
public enum TextureFilter { Nearest, Linear }

/// <summary>A GPU (or headless) texture owned by the active graphics device.</summary>
public abstract class Texture : IDisposable
{
    private TextureFilter _filter;

    /// <summary>Width in pixels.</summary>
    public int Width { get; }
    /// <summary>Height in pixels.</summary>
    public int Height { get; }
    /// <summary>
    /// Whether <see cref="Dispose"/> was called. Drawing a disposed texture throws <see cref="ObjectDisposedException"/>
    /// on every renderer, and nothing is drawn.
    /// </summary>
    public bool IsDisposed { get; private set; }
    /// <summary>
    /// The graphics device that created the texture, or null for a texture whose device did not say (one made with the
    /// constructor that takes no device). Renderers draw only the textures of their own device.
    /// </summary>
    public IGraphicsDevice? Device { get; }
    /// <summary>Records the size and filter of a texture created by a graphics device.</summary>
    protected Texture(int width, int height, TextureFilter filter = TextureFilter.Nearest) => (Width, Height, _filter) = (width, height, filter);
    /// <summary>Records the size, filter and creating <paramref name="device"/> of a texture.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="device"/> is null.</exception>
    protected Texture(IGraphicsDevice device, int width, int height, TextureFilter filter = TextureFilter.Nearest) : this(width, height, filter) =>
        Device = device ?? throw new ArgumentNullException(nameof(device));

    /// <summary>
    /// The checks every renderer makes before it draws <paramref name="texture"/>, so that all backends fail alike: a
    /// disposed texture throws <see cref="ObjectDisposedException"/>, and a texture another graphics device created (one
    /// that is not a <typeparamref name="TTexture"/>, or whose <see cref="Device"/> is not <paramref name="device"/>)
    /// throws <see cref="ArgumentException"/>. Backend authors call it first in every drawing method that takes a texture.
    /// </summary>
    /// <typeparam name="TTexture">The texture type of the renderer's device.</typeparam>
    /// <param name="texture">The texture a game asked to draw.</param>
    /// <param name="device">The renderer's graphics device.</param>
    /// <returns><paramref name="texture"/> as the device's own type.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="texture"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">The texture was disposed.</exception>
    /// <exception cref="ArgumentException">Another graphics device created the texture.</exception>
    public static TTexture CheckDrawable<TTexture>(Texture texture, IGraphicsDevice device) where TTexture : Texture
    {
        ArgumentNullException.ThrowIfNull(texture);
        ArgumentNullException.ThrowIfNull(device);
        ObjectDisposedException.ThrowIf(texture.IsDisposed, texture);
        if (texture is TTexture own && (texture.Device is null || ReferenceEquals(texture.Device, device))) return own;
        string owner = texture.Device?.GetType().Name ?? texture.GetType().Name;
        throw new ArgumentException($"The {texture.Width}x{texture.Height} texture was created by another graphics device ({owner}); " +
            $"a renderer draws only the textures of its own device ({device.GetType().Name}). Create it with the game's Graphics.", nameof(texture));
    }
    /// <summary>The whole texture as a source rectangle: (0, 0, <see cref="Width"/>, <see cref="Height"/>).</summary>
    public RectF Bounds => new(0, 0, Width, Height);
    /// <summary>Memory the texture's pixels take on the device, in bytes (4 per pixel).</summary>
    public long SizeInBytes => (long)Width * Height * 4;

    /// <summary>How the texture is sampled; it can change at any time and applies to the next draws.</summary>
    public TextureFilter Filter
    {
        get => _filter;
        set
        {
            if (_filter == value) return;
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            _filter = value;
            ApplyFilter(value);
        }
    }

    /// <summary>
    /// Replaces the pixels of a rectangle of the texture (glyphs added to a font page, a painted canvas). The pixels are
    /// row-major, <paramref name="width"/> per row.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The rectangle leaves the texture.</exception>
    /// <exception cref="ArgumentException"><paramref name="pixels"/> holds fewer than <paramref name="width"/> × <paramref name="height"/> colours.</exception>
    public void Update(int x, int y, int width, int height, ReadOnlySpan<Color> pixels)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (x < 0 || y < 0 || width < 0 || height < 0 || x + width > Width || y + height > Height)
            throw new ArgumentOutOfRangeException(nameof(x), $"The rectangle ({x}, {y}, {width}, {height}) leaves the {Width}x{Height} texture.");
        if (pixels.Length < width * height) throw new ArgumentException($"{width}x{height} pixels need {width * height} colours, got {pixels.Length}.", nameof(pixels));
        if (width == 0 || height == 0) return;
        Upload(x, y, width, height, pixels[..(width * height)]);
    }

    /// <summary>Copies a whole image into the texture at (<paramref name="x"/>, <paramref name="y"/>).</summary>
    public void Update(PixelImage image, int x = 0, int y = 0)
    {
        ArgumentNullException.ThrowIfNull(image);
        Update(x, y, image.Width, image.Height, image.Pixels);
    }

    /// <summary>Sends <paramref name="pixels"/> (exactly width × height, already checked) to the device.</summary>
    protected abstract void Upload(int x, int y, int width, int height, ReadOnlySpan<Color> pixels);
    /// <summary>Applies a new filter on the device; called by <see cref="Filter"/> when it changes.</summary>
    protected abstract void ApplyFilter(TextureFilter filter);
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
/// <remarks>
/// Every renderer follows the same rules. Pixel (x, y) is the square from (x, y) to (x + 1, y + 1) and its centre is
/// (x + 0.5, y + 0.5). A rectangle or an unrotated sprite covers exactly the pixels whose centre lies inside it, a
/// centre on its left or top edge counting as inside and one on its right or bottom edge as outside (the top-left rule
/// of Direct3D and Vulkan): a 1 × 1 rectangle at (2.5, 2.5) covers pixel (2, 2) only. A sprite pixel shows the texel
/// under its centre (nearest) or the bilinear mix of the four texels around it (linear, clamped at the texture's edge).
/// Colours are given straight (not premultiplied); the canvas stores them premultiplied (colour times alpha), so linear
/// sampling never darkens transparent edges, and 8-bit results are rounded to nearest. Circles, lines and triangles
/// cover their shape's area but their edge pixels may differ between renderers.
/// </remarks>
public interface IRenderer
{
    /// <summary>Width of the virtual canvas in pixels.</summary>
    int Width { get; }
    /// <summary>Height of the virtual canvas in pixels.</summary>
    int Height { get; }
    /// <summary>
    /// Replaces every pixel of the canvas with <paramref name="color"/>, alpha included, whatever the clip
    /// (<see cref="SetClip"/>) and blend mode, and replaces what was drawn before it in the frame. The clip still applies
    /// to the draws after it. The game clears to its settings' <c>Background</c> colour before the scenes draw.
    /// </summary>
    void Clear(Color color);
    /// <summary>Draws the <paramref name="source"/> part of <paramref name="texture"/> with its top-left at <paramref name="position"/>, multiplied by <paramref name="tint"/>.</summary>
    /// <param name="texture">A texture of this renderer's graphics device, not disposed.</param>
    /// <param name="source">The rectangle of the texture to draw, in texture pixels.</param>
    /// <param name="position">Where the top-left of the unrotated, scaled source lands, in virtual pixels (world pixels under a camera).</param>
    /// <param name="tint">Colour multiplier; <see cref="Color.White"/> draws the texture unchanged.</param>
    /// <param name="options">Flip, rotation, pivot and scale.</param>
    /// <exception cref="ObjectDisposedException"><paramref name="texture"/> was disposed; nothing is drawn.</exception>
    /// <exception cref="ArgumentException">Another graphics device created <paramref name="texture"/>; nothing is drawn.</exception>
    void Draw(Texture texture, RectF source, Vector2 position, Color tint, in DrawOptions options = default);
    /// <summary>Draws the <paramref name="source"/> part of <paramref name="texture"/> scaled to fill <paramref name="destination"/>.</summary>
    /// <exception cref="ObjectDisposedException"><paramref name="texture"/> was disposed; nothing is drawn.</exception>
    /// <exception cref="ArgumentException">Another graphics device created <paramref name="texture"/>; nothing is drawn.</exception>
    void DrawStretched(Texture texture, RectF source, RectF destination, Color tint);
    /// <summary>Fills a rectangle: the pixels whose centre lies inside it (see the rules of <see cref="IRenderer"/>). A fully transparent colour draws nothing.</summary>
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
    /// <summary>
    /// Sets how later draws combine with the canvas. Every frame starts with <see cref="BlendMode.Alpha"/>. With source
    /// colour s (after the tint), its alpha a and canvas colour d: <c>Alpha</c> writes s·a + d·(1 − a),
    /// <c>Additive</c> s·a + d, <c>Multiply</c> s·a·d + d·(1 − a) (the colour multiplies the canvas in proportion to its
    /// opacity), each channel clamped to 0..1.
    /// </summary>
    void SetBlendMode(BlendMode mode);
    /// <summary>
    /// Restricts drawing to a screen-space rectangle (the camera does not move it); pass null to remove. X, Y, Width and
    /// Height are truncated to whole pixels. <see cref="Clear"/> ignores the clip.
    /// </summary>
    void SetClip(RectF? screenRect);
}

/// <summary>Creates device resources. Separated so headless tests can provide fakes.</summary>
public interface IGraphicsDevice
{
    /// <summary>
    /// The filter of textures created without one: <see cref="CreateTexture(PixelImage)"/>, atlases. The engine sets it
    /// from the game settings (<c>TextureFilter</c>, or its presentation's default sampler) before <c>Game.Load</c>.
    /// Glyph pages of a scalable font follow it too unless the font's options choose a filter.
    /// </summary>
    TextureFilter DefaultFilter { get; set; }
    /// <summary>Uploads <paramref name="image"/> as a texture sampled with <paramref name="filter"/>. The caller owns the texture and disposes it.</summary>
    Texture CreateTexture(PixelImage image, TextureFilter filter);
    /// <summary>Uploads <paramref name="image"/> as a texture sampled with <see cref="DefaultFilter"/>. The caller owns the texture and disposes it.</summary>
    Texture CreateTexture(PixelImage image) => CreateTexture(image, DefaultFilter);
}

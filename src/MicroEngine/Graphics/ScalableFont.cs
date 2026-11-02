using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using FontStashSharp;
using FontStashSharp.Interfaces;

namespace MicroEngine.Graphics;

/// <summary>How a <see cref="ScalableFont"/> turns outlines into pixels.</summary>
/// <param name="Antialiasing">
/// Smooth glyph edges (true, the default: HD text). False gives hard edges, for pixel fonts distributed as TTF and drawn at
/// their design size or whole multiples of it.
/// </param>
/// <param name="Filter">
/// How glyph pages are sampled when text is scaled or rotated; null picks linear for antialiased text and nearest
/// otherwise.
/// </param>
/// <param name="PageSize">
/// Width and height of each glyph page texture: 512 by default (1 MB), enough for the printable ASCII of a few sizes;
/// another page is added when one fills up.
/// </param>
/// <param name="Kerning">Applies the font's kerning pairs (true by default).</param>
/// <param name="SnapToPixels">
/// Places each glyph on whole canvas pixels (true by default) so unscaled text stays sharp; false keeps fractional
/// positions, smoother for slowly moving text.
/// </param>
public sealed record FontOptions(bool Antialiasing = true, TextureFilter? Filter = null, int PageSize = 512, bool Kerning = true, bool SnapToPixels = true)
{
    /// <summary>The default: antialiased, linear, kerned, snapped to pixels.</summary>
    public static FontOptions Default { get; } = new();
    /// <summary>For pixel fonts in TTF/OTF files: no antialiasing, nearest sampling.</summary>
    public static FontOptions Pixel { get; } = new(Antialiasing: false);
}

/// <summary>
/// A TrueType or OpenType font (TTF, OTF with TrueType or CFF outlines) that draws text at any size. Glyphs are
/// rasterized the first time they are drawn at a size and kept in shared glyph pages (textures), so later frames only
/// draw quads; <see cref="Prepare"/> rasterizes ahead, so the first frames of play do no font work.
/// </summary>
/// <remarks>
/// Built on FontStashSharp (StbTrueTypeSharp rasterizer). One <see cref="ScalableFont"/> serves every size; extra faces
/// added with <see cref="AddFallback"/> supply characters the main face lacks (another script, symbols). Missing
/// characters draw as '?'. Usually loaded and owned by <c>Game.Assets</c>; a font made here is disposed by its creator.
/// </remarks>
/// <example>
/// <code>
/// var font = ScalableFont.Load(Graphics, "Content/fonts/Inter-Regular.ttf");
/// font.Draw(renderer, "Score 120", new Vector2(24, 24), 32, Color.White);
/// var title = font.At(64);              // an IFont for widgets and layout
/// title.DrawCentered(renderer, "Paused", new Vector2(640, 360), Color.White);
/// </code>
/// </example>
public sealed class ScalableFont : IDisposable
{
    private readonly FontSystem _system;
    private readonly PageManager _pages;
    private readonly Dictionary<float, SizedFont> _sizes = [];
    private readonly FontOptions _options;
    private TextureFilter _filter;
    private bool _disposed;

    private ScalableFont(IGraphicsDevice device, byte[] data, FontOptions? options, string? name)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(data);
        _options = options ?? FontOptions.Default;
        if (_options.PageSize is < 64 or > 8192) throw new ArgumentOutOfRangeException(nameof(options), "PageSize must be between 64 and 8192.");
        _filter = _options.Filter ?? (_options.Antialiasing ? TextureFilter.Linear : TextureFilter.Nearest);
        Name = name ?? "font";
        _pages = new PageManager(device, this);
        var settings = new FontSystemSettings
        {
            TextureWidth = _options.PageSize,
            TextureHeight = _options.PageSize,
            // The engine's blend modes take straight alpha: white glyphs whose alpha is the coverage.
            PremultiplyAlpha = false,
        };
        settings.GlyphRenderer = RenderGlyph;
        _system = new FontSystem(settings) { UseKernings = _options.Kerning, DefaultCharacter = '?' };
        Add(data, Name);
    }

    /// <summary>Opens a font from the bytes of a TTF or OTF file.</summary>
    /// <param name="device">Creates the glyph pages.</param>
    /// <param name="data">The whole font file.</param>
    /// <param name="options">Rasterization and sampling; null for <see cref="FontOptions.Default"/>.</param>
    /// <param name="name">A name for messages and diagnostics.</param>
    /// <exception cref="InvalidDataException">The data is not a font the rasterizer reads.</exception>
    public static ScalableFont FromBytes(IGraphicsDevice device, byte[] data, FontOptions? options = null, string? name = null) => new(device, data, options, name);

    /// <summary>Opens a TTF or OTF file.</summary>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="InvalidDataException">The file is not a font the rasterizer reads; the message names it.</exception>
    public static ScalableFont Load(IGraphicsDevice device, string path, FontOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return new ScalableFont(device, File.ReadAllBytes(path), options, Path.GetFileName(path));
    }

    /// <summary>The file name (or the name given), for messages.</summary>
    public string Name { get; }
    /// <summary>The options the font was opened with.</summary>
    public FontOptions Options => _options;
    /// <summary>Glyph pages created so far (each <see cref="FontOptions.PageSize"/> squared, 4 bytes per pixel).</summary>
    public int Pages => _pages.Count;
    /// <summary>Device memory of the glyph pages, in bytes.</summary>
    public long TextureBytes => _pages.Bytes;
    /// <summary>Glyphs rasterized so far, across every size (a measure of font work done).</summary>
    public int GlyphsRasterized { get; private set; }

    /// <summary>How the glyph pages are sampled; applies to existing and future pages.</summary>
    public TextureFilter Filter
    {
        get => _filter;
        set
        {
            _filter = value;
            _pages.SetFilter(value);
        }
    }

    /// <summary>
    /// Adds another TTF/OTF face whose glyphs are used for characters this font lacks (accents, other scripts, icons).
    /// Faces are searched in the order they were added. Glyphs already rasterized are dropped (as by <see cref="Reset"/>),
    /// so add fallbacks while loading.
    /// </summary>
    /// <exception cref="InvalidDataException">The data is not a font the rasterizer reads.</exception>
    public ScalableFont AddFallback(byte[] data, string? name = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(data);
        Add(data, name ?? "fallback");
        // Characters already looked up keep the glyph they found ('?' for a missing one) until the cache is dropped.
        if (_sizes.Count > 0) Reset();
        return this;
    }

    private void Add(byte[] data, string name)
    {
        // The rasterizer reads a font through raw pointers without bounds checks: a truncated or foreign file makes it
        // read past the array (hanging or crashing the game). Its table directory is checked here first.
        if (CheckStructure(data) is { } problem) throw new InvalidDataException($"{name} is not a TrueType or OpenType font the engine can read ({problem}).");
        try { _system.AddFont(data); }
        catch (Exception e) when (e is not OutOfMemoryException and not ObjectDisposedException)
        {
            throw new InvalidDataException($"{name} is not a TrueType or OpenType font the engine can read ({e.Message}).", e);
        }
    }

    /// <summary>
    /// What is wrong with the file's structure, or null when every table of its (first) font lies inside the data: a
    /// TrueType ("\0\x01\0\0", "true") or OpenType ("OTTO") header, or a collection ("ttcf") whose first font is one.
    /// </summary>
    internal static string? CheckStructure(ReadOnlySpan<byte> data)
    {
        static uint U32(ReadOnlySpan<byte> d, int at) => System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(d[at..]);
        if (data.Length < 12) return "the file is too short";
        int start = 0;
        if (U32(data, 0) == 0x74746366) // "ttcf": a collection; the rasterizer opens its first font.
        {
            if (data.Length < 16) return "the collection header is cut short";
            uint first = U32(data, 12);
            if (first > (uint)data.Length - 12) return "the collection points outside the file";
            start = (int)first;
        }
        uint version = U32(data, start);
        if (version is not (0x00010000 or 0x4F54544F or 0x74727565)) return "no TrueType or OpenType header";
        int tables = System.Buffers.Binary.BinaryPrimitives.ReadUInt16BigEndian(data[(start + 4)..]);
        if (tables == 0 || start + 12 + (long)tables * 16 > data.Length) return "the table directory is cut short";
        for (int i = 0; i < tables; i++)
        {
            int entry = start + 12 + i * 16;
            ulong offset = U32(data, entry + 8), length = U32(data, entry + 12);
            if (offset + length > (ulong)data.Length) return "a table lies outside the file";
        }
        return null;
    }

    /// <summary>The font at <paramref name="size"/> (pixels per em), as an <see cref="IFont"/>; the same object for the same size.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> is not positive.</exception>
    public SizedFont At(float size)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!(size > 0) || float.IsInfinity(size)) throw new ArgumentOutOfRangeException(nameof(size), size, "Font sizes are positive.");
        if (_sizes.TryGetValue(size, out var sized)) return sized;
        return _sizes[size] = new SizedFont(this, size);
    }

    /// <summary>Increases when <see cref="Reset"/> drops the rasterizer's fonts, so sizes fetch theirs again.</summary>
    internal int Generation { get; private set; }

    internal SpriteFontBase Rasterizer(float size)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _system.GetFont(size);
    }

    /// <summary>Draws <paramref name="text"/> at <paramref name="size"/> with its top-left at <paramref name="position"/>.</summary>
    public void Draw(IRenderer renderer, string text, Vector2 position, float size, Color color) => At(size).Draw(renderer, text, position, color);

    /// <summary>Size of <paramref name="text"/> at <paramref name="size"/>, in canvas pixels.</summary>
    public Vector2 Measure(string text, float size) => At(size).Measure(text);

    /// <summary>Distance between the tops of two lines at <paramref name="size"/>.</summary>
    public float LineHeight(float size) => At(size).LineHeight;

    /// <summary>
    /// Rasterizes <paramref name="characters"/> at each of <paramref name="sizes"/> now, so drawing them later costs no
    /// font work. Call it while loading for the text a scene will show (digits for a score, the letters of a menu).
    /// </summary>
    public void Prepare(string characters, params float[] sizes)
    {
        ArgumentNullException.ThrowIfNull(characters);
        foreach (float size in sizes) At(size).Prepare(characters);
    }

    /// <summary>Printable ASCII: letters, digits and punctuation, a good default for <see cref="Prepare"/>.</summary>
    public const string Ascii = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";

    /// <summary>Drops every rasterized glyph and its pages (freeing their memory); glyphs drawn later are rasterized again.</summary>
    public void Reset()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _system.Reset();
        _pages.Clear();
        Generation++;
    }

    /// <summary>Releases the glyph pages. Text cannot be drawn afterwards.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _system.Dispose();
        _pages.Clear();
    }

    internal void Render(IRenderer renderer, SpriteFontBase font, string text, Vector2 position, Color color)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _pages.Renderer = renderer;
        try { font.DrawText(_pages, text, position, new FSColor(color.R, color.G, color.B, color.A)); }
        finally { _pages.Renderer = null; }
    }

    // Turns the rasterizer's coverage into white pixels whose alpha is the coverage (straight alpha), or, without
    // antialiasing, fully opaque where at least half the pixel is covered. Called once per glyph rasterized.
    private void RenderGlyph(byte[] input, byte[] output, GlyphRenderOptions options)
    {
        GlyphsRasterized++;
        int size = options.Size.X * options.Size.Y;
        bool hard = !_options.Antialiasing;
        for (int i = 0; i < size; i++)
        {
            byte coverage = input[i];
            int at = i * 4;
            output[at] = output[at + 1] = output[at + 2] = 255;
            output[at + 3] = hard ? (coverage >= 128 ? (byte)255 : (byte)0) : coverage;
        }
    }

    /// <summary>
    /// FontStashSharp's two ports to an engine: glyph pages as engine textures (created and updated through the graphics
    /// device), and glyph quads drawn with the renderer of the current call.
    /// </summary>
    private sealed class PageManager(IGraphicsDevice device, ScalableFont owner) : ITexture2DManager, IFontStashRenderer
    {
        private readonly List<Texture> _pages = [];
        public IRenderer? Renderer;
        public int Uploads;
        public int Count => _pages.Count;
        public long Bytes => _pages.Sum(p => p.SizeInBytes);
        public ITexture2DManager TextureManager => this;

        public object CreateTexture(int width, int height)
        {
            var page = device.CreateTexture(new PixelImage(width, height), owner._filter);
            _pages.Add(page);
            return page;
        }

        public Point GetTextureSize(object texture) => new(((Texture)texture).Width, ((Texture)texture).Height);

        public void SetTextureData(object texture, Rectangle bounds, byte[] data)
        {
            Uploads++;
            // The rasterizer writes R, G, B, A bytes: the layout of Color.
            ((Texture)texture).Update(bounds.X, bounds.Y, bounds.Width, bounds.Height, MemoryMarshal.Cast<byte, Color>(data.AsSpan(0, bounds.Width * bounds.Height * 4)));
        }

        public void Draw(object texture, Vector2 pos, Rectangle? src, FSColor color, float rotation, Vector2 scale, float depth)
        {
            // Without a renderer the call only rasterizes (Prepare).
            if (Renderer is not { } renderer) return;
            var page = (Texture)texture;
            var source = src is { } s ? new RectF(s.X, s.Y, s.Width, s.Height) : page.Bounds;
            if (owner._options.SnapToPixels && rotation == 0) pos = new Vector2(MathF.Round(pos.X), MathF.Round(pos.Y));
            var options = rotation == 0 && scale == Vector2.One ? default : new DrawOptions(Rotation: rotation, Scale: scale);
            renderer.Draw(page, source, pos, new Color(color.R, color.G, color.B, color.A), options);
        }

        public void SetFilter(TextureFilter filter)
        {
            foreach (var page in _pages) page.Filter = filter;
        }

        public void Clear()
        {
            foreach (var page in _pages) page.Dispose();
            _pages.Clear();
        }
    }
}

/// <summary>A <see cref="ScalableFont"/> at one size: an <see cref="IFont"/> for widgets, layout and drawing.</summary>
public sealed class SizedFont : IFont
{
    private SpriteFontBase _font;
    private int _generation;

    internal SizedFont(ScalableFont family, float size)
    {
        (Family, Size) = (family, size);
        _font = family.Rasterizer(size);
        _generation = family.Generation;
    }

    /// <summary>The rasterizer's font at this size, fetched again after the family was reset.</summary>
    private SpriteFontBase Font
    {
        get
        {
            if (_generation != Family.Generation)
            {
                _font = Family.Rasterizer(Size);
                _generation = Family.Generation;
            }
            return _font;
        }
    }

    /// <summary>The font this size belongs to.</summary>
    public ScalableFont Family { get; }
    /// <summary>The size in pixels per em.</summary>
    public float Size { get; }
    /// <inheritdoc/>
    public float LineHeight => Font.LineHeight;

    /// <inheritdoc/>
    public Vector2 Measure(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length == 0) return Vector2.Zero;
        var size = Font.MeasureString(text);
        // FontStashSharp measures from the first line's top to the last glyph's bottom; layout wants whole lines.
        int lines = 1 + text.AsSpan().Count('\n');
        return new Vector2(size.X, MathF.Max(size.Y, lines * LineHeight));
    }

    /// <inheritdoc/>
    public void Draw(IRenderer renderer, string text, Vector2 position, Color color)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length == 0) return;
        Family.Render(renderer, Font, text, position, color);
    }

    /// <summary>Rasterizes <paramref name="characters"/> at this size now (see <see cref="ScalableFont.Prepare"/>).</summary>
    public void Prepare(string characters)
    {
        ArgumentNullException.ThrowIfNull(characters);
        if (characters.Length > 0) Family.Render(NoRenderer.Instance, Font, characters, Vector2.Zero, Color.White);
    }

    /// <summary>A renderer that draws nothing: rasterizing through it fills the glyph pages without drawing.</summary>
    private sealed class NoRenderer : IRenderer
    {
        public static readonly NoRenderer Instance = new();
        public int Width => 0;
        public int Height => 0;
        public void Clear(Color color) { }
        public void Draw(Texture texture, RectF source, Vector2 position, Color tint, in DrawOptions options = default) { }
        public void DrawStretched(Texture texture, RectF source, RectF destination, Color tint) { }
        public void FillRect(RectF rect, Color color) { }
        public void FillCircle(Vector2 center, float radius, Color color) { }
        public void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color) { }
        public void DrawLine(Vector2 from, Vector2 to, float thickness, Color color) { }
        public void PushCamera(Camera2D camera) { }
        public void PopCamera() { }
        public void SetBlendMode(BlendMode mode) { }
        public void SetClip(RectF? screenRect) { }
    }
}

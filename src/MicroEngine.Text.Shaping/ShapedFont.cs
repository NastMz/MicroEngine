using System.Numerics;
using System.Runtime.InteropServices;
using HarfBuzzSharp;
using StbTrueTypeSharp;
using HbBuffer = HarfBuzzSharp.Buffer;
using HbFace = HarfBuzzSharp.Face;
using HbFont = HarfBuzzSharp.Font;

namespace MicroEngine.Graphics;

/// <summary>
/// A TrueType or OpenType font that shapes its text: letters take the forms their neighbours call for (Arabic's joined
/// forms, Indic conjuncts, ligatures), marks sit where the font places them, and right-to-left scripts (Arabic, Hebrew)
/// run right to left. HarfBuzz chooses the glyphs and their positions; each glyph is rasterized the first time it is
/// drawn at a size and kept in glyph pages (textures), so later frames only draw quads.
/// </summary>
/// <remarks>
/// <para>
/// Use it in place of a <see cref="ScalableFont"/> for text in a script that needs shaping; <see cref="At"/> gives an
/// <see cref="IFont"/>, so widgets, layout and wrapping work with it unchanged. Faces added with
/// <see cref="AddFallback"/> supply the letters the main face lacks, a run of text at a time, so a Latin face with an
/// Arabic and a Hebrew one after it draws all three. Letters no face has draw as the last face's missing-glyph box.
/// </para>
/// <para>
/// Lines that mix directions follow Unicode's bidirectional algorithm (UAX #9): a line reads in the direction of its
/// first letter that has one (<see cref="ShapedFontSize.IsRightToLeft"/>), and runs of the other direction inside it,
/// a name or a number in an Arabic sentence, read in theirs. Explicit direction marks (embeddings, overrides, isolates)
/// are ignored and brackets are not paired across directions.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var font = ShapedFont.FromBytes(Graphics, Assets.Bytes("fonts/Inter-Regular.ttf"))
///     .AddFallback(Assets.Bytes("fonts/NotoSansArabic-Regular.ttf"));
/// font.At(32).Draw(renderer, "مرحبا", new Vector2(24, 24), Color.White);
/// </code>
/// </example>
public sealed class ShapedFont : IDisposable
{
    private readonly List<FontFace> _faces = [];
    private readonly Dictionary<float, ShapedFontSize> _sizes = [];
    private readonly GlyphPages _pages;
    private bool _disposed;

    private ShapedFont(IGraphicsDevice device, byte[] data, FontOptions? options, string? name)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(data);
        Options = options ?? FontOptions.Default;
        if (Options.PageSize is < 64 or > 8192) throw new ArgumentOutOfRangeException(nameof(options), "PageSize must be between 64 and 8192.");
        Name = name ?? "font";
        _pages = new GlyphPages(device, Options.PageSize, Options.Filter ?? device.DefaultFilter);
        _faces.Add(FontFace.Open(data, Name));
    }

    /// <summary>Opens a font from the bytes of a TTF or OTF file.</summary>
    /// <param name="device">Creates the glyph pages.</param>
    /// <param name="data">The whole font file.</param>
    /// <param name="options">Rasterization and sampling; null for <see cref="FontOptions.Default"/>.</param>
    /// <param name="name">A name for messages and diagnostics.</param>
    /// <exception cref="InvalidDataException">The data is not a font the engine reads.</exception>
    public static ShapedFont FromBytes(IGraphicsDevice device, byte[] data, FontOptions? options = null, string? name = null) => new(device, data, options, name);

    /// <summary>Opens a TTF or OTF file.</summary>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="InvalidDataException">The file is not a font the engine reads; the message names it.</exception>
    public static ShapedFont Load(IGraphicsDevice device, string path, FontOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return new ShapedFont(device, File.ReadAllBytes(path), options, Path.GetFileName(path));
    }

    /// <summary>The file name (or the name given), for messages.</summary>
    public string Name { get; }
    /// <summary>The options the font was opened with (<see cref="FontOptions.Kerning"/> does not apply: the font's own positioning always does).</summary>
    public FontOptions Options { get; }
    /// <summary>How many faces the font has: the main one, then its fallbacks in the order they were added.</summary>
    public int Faces => _faces.Count;
    /// <summary>Glyph pages created so far (each <see cref="FontOptions.PageSize"/> squared, 4 bytes per pixel).</summary>
    public int Pages => _pages.Count;
    /// <summary>Device memory of the glyph pages, in bytes.</summary>
    public long TextureBytes => _pages.Bytes;
    /// <summary>Glyphs rasterized so far, across every size (a measure of font work done).</summary>
    public int GlyphsRasterized => _pages.Rasterized;

    /// <summary>How the glyph pages are sampled; applies to existing and future pages.</summary>
    public TextureFilter Filter
    {
        get => _pages.Filter;
        set => _pages.Filter = value;
    }

    /// <summary>
    /// Adds another TTF/OTF face for the letters the faces before it lack (another script, symbols). Text already
    /// shaped is shaped again, so add fallbacks while loading.
    /// </summary>
    /// <exception cref="InvalidDataException">The data is not a font the engine reads.</exception>
    public ShapedFont AddFallback(byte[] data, string? name = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(data);
        _faces.Add(FontFace.Open(data, name ?? "fallback"));
        foreach (var size in _sizes.Values) size.Forget();
        return this;
    }

    /// <summary>The font at <paramref name="size"/> (the same size as a <see cref="ScalableFont"/>'s), as an <see cref="IFont"/>; the same object for the same size.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> is not positive.</exception>
    public ShapedFontSize At(float size)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!(size > 0) || float.IsInfinity(size)) throw new ArgumentOutOfRangeException(nameof(size), size, "Font sizes are positive.");
        if (_sizes.TryGetValue(size, out var sized)) return sized;
        return _sizes[size] = new ShapedFontSize(this, size);
    }

    /// <summary>Drops every rasterized glyph and its pages (freeing their memory); glyphs drawn later are rasterized again.</summary>
    public void Reset()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _pages.Clear();
    }

    /// <summary>Releases the glyph pages and the faces. Text cannot be drawn afterwards.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _pages.Clear();
        foreach (var face in _faces) face.Dispose();
        _faces.Clear();
    }

    internal void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
    internal IReadOnlyList<FontFace> FaceList => _faces;
    internal GlyphPages GlyphPages => _pages;
}

/// <summary>A <see cref="ShapedFont"/> at one size: an <see cref="IFont"/> for widgets, layout and drawing.</summary>
public sealed class ShapedFontSize : IFont
{
    private const int CacheLimit = 512;
    private readonly Dictionary<string, ShapedGlyph[]> _lines = new(StringComparer.Ordinal);
    private readonly float _scale, _em, _ascent;

    internal ShapedFontSize(ShapedFont family, float size)
    {
        (Family, Size) = (family, size);
        var main = family.FaceList[0];
        _scale = main.ScaleFor(size);
        _em = _scale * main.UnitsPerEm;
        _ascent = main.Ascent * _scale;
        LineHeight = MathF.Floor((main.Ascent - main.Descent + main.LineGap) * _scale);
    }

    /// <summary>The font this size belongs to.</summary>
    public ShapedFont Family { get; }
    /// <summary>The size, as for <see cref="ShapedFont.At"/>.</summary>
    public float Size { get; }
    /// <inheritdoc/>
    public float LineHeight { get; }
    /// <summary>Distance from the top of a line to its baseline, in canvas pixels.</summary>
    public float Baseline => _ascent;

    /// <summary>
    /// The glyphs of one line of <paramref name="line"/> (no '\n') in the order they are drawn, left to right, with
    /// where each sits: for effects of a game's own (a wave, a reveal that follows the reading order through
    /// <see cref="ShapedGlyph.Cluster"/>).
    /// </summary>
    public IReadOnlyList<ShapedGlyph> Shape(string line)
    {
        ArgumentNullException.ThrowIfNull(line);
        Family.ThrowIfDisposed();
        if (_lines.TryGetValue(line, out var glyphs)) return glyphs;
        if (_lines.Count >= CacheLimit) _lines.Clear();
        return _lines[line] = Shaper.Shape(Family.FaceList, line, ScaleOf, _ascent);
    }

    /// <summary>
    /// Whether <paramref name="line"/> reads right to left: its first letter with a direction is Hebrew, Arabic or
    /// another right-to-left script. Align such lines to the right.
    /// </summary>
    public bool IsRightToLeft(string line)
    {
        ArgumentNullException.ThrowIfNull(line);
        return Bidi.IsRightToLeft(line);
    }

    /// <inheritdoc/>
    public Vector2 Measure(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length == 0) return Vector2.Zero;
        float width = 0;
        int lines = 0;
        foreach (var line in text.Split('\n'))
        {
            lines++;
            width = MathF.Max(width, Width(Shape(line)));
        }
        return new Vector2(width, lines * LineHeight);
    }

    private static float Width(IReadOnlyList<ShapedGlyph> glyphs) =>
        glyphs.Count == 0 ? 0 : glyphs[^1].Pen + glyphs[^1].Advance;

    /// <inheritdoc/>
    public void Draw(IRenderer renderer, string text, Vector2 position, Color color)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length == 0) return;
        var pages = Family.GlyphPages;
        bool snap = Family.Options.SnapToPixels;
        float y = position.Y;
        foreach (var line in text.Split('\n'))
        {
            foreach (var g in Shape(line))
            {
                var face = Family.FaceList[g.Face];
                if (pages.Get(face, g.Face, g.Glyph, Size, ScaleOf(g.Face), !Family.Options.Antialiasing) is not { } slot) continue;
                var at = new Vector2(position.X, y) + g.Position + slot.Offset;
                if (snap) at = new Vector2(MathF.Round(at.X), MathF.Round(at.Y));
                renderer.Draw(slot.Page, slot.Source, at, color);
            }
            y += LineHeight;
        }
    }

    /// <summary>Shapes and rasterizes <paramref name="text"/> now, so drawing it later costs no font work.</summary>
    public void Prepare(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var pages = Family.GlyphPages;
        foreach (var line in text.Split('\n'))
            foreach (var g in Shape(line))
            {
                var face = Family.FaceList[g.Face];
                pages.Get(face, g.Face, g.Glyph, Size, ScaleOf(g.Face), !Family.Options.Antialiasing);
            }
    }

    internal void Forget() => _lines.Clear();

    /// <summary>
    /// Pixels per font unit of face <paramref name="face"/>: the main face's from the size, a fallback's so that its em
    /// is as large as the main face's (fallback letters match the main face's size, however tall the fallback's marks).
    /// </summary>
    internal float ScaleOf(int face) => face == 0 ? _scale : _em / Family.FaceList[face].UnitsPerEm;
}

/// <summary>One glyph of a shaped line.</summary>
/// <param name="Face">Which face of the font it comes from: 0 the main face, then the fallbacks in order.</param>
/// <param name="Glyph">The glyph's index in that face (0 is the face's missing-glyph box).</param>
/// <param name="Cluster">The index in the line's text of the first character the glyph shows; glyphs of one letter, or letters merged into one glyph, share it.</param>
/// <param name="Position">The glyph's origin, relative to the line's top-left corner: its y is the baseline, moved by any offset the font gives (marks).</param>
/// <param name="Pen">How far along the line, from its left edge, this glyph's advance starts.</param>
/// <param name="Advance">How far the glyph moves the pen.</param>
public readonly record struct ShapedGlyph(int Face, int Glyph, int Cluster, Vector2 Position, float Pen, float Advance);

/// <summary>One face of a shaped font: HarfBuzz's view of it for shaping, StbTrueType's for rasterizing.</summary>
internal sealed class FontFace : IDisposable
{
    private GCHandle _pin;
    private readonly Blob _blob;
    private readonly HbFace _face;

    private FontFace(byte[] data, string name)
    {
        Name = name;
        _pin = GCHandle.Alloc(data, GCHandleType.Pinned);
        _blob = new Blob(_pin.AddrOfPinnedObject(), data.Length, MemoryMode.ReadOnly);
        _face = new HbFace(_blob, 0);
        Shaper = new HbFont(_face);
        UnitsPerEm = Math.Max(1, _face.UnitsPerEm);
        Rasterizer = StbTrueType.CreateFont(data, 0) ?? throw new InvalidDataException($"{name} is not a TrueType or OpenType font the engine can read.");
        unsafe
        {
            int ascent, descent, gap;
            StbTrueType.stbtt_GetFontVMetrics(Rasterizer, &ascent, &descent, &gap);
            (Ascent, Descent, LineGap) = (ascent, descent, gap);
        }
    }

    /// <summary>Opens a face; a file whose tables lie outside its data is refused before the rasterizer reads it.</summary>
    /// <exception cref="InvalidDataException">The data is not a TrueType or OpenType font.</exception>
    public static FontFace Open(byte[] data, string name)
    {
        if (ScalableFont.CheckStructure(data) is { } problem)
            throw new InvalidDataException($"{name} is not a TrueType or OpenType font the engine can read ({problem}).");
        return new FontFace(data, name);
    }

    public string Name { get; }
    public HbFont Shaper { get; }
    public StbTrueType.stbtt_fontinfo Rasterizer { get; }
    public int Ascent { get; }
    public int Descent { get; }
    public int LineGap { get; }
    public int UnitsPerEm { get; }

    /// <summary>Pixels per font unit at <paramref name="size"/>: the size is the height from the highest ascender to the lowest descender, as for scalable fonts.</summary>
    public float ScaleFor(float size) => StbTrueType.stbtt_ScaleForPixelHeight(Rasterizer, size);

    public void Dispose()
    {
        Shaper.Dispose();
        _face.Dispose();
        _blob.Dispose();
        Rasterizer.Dispose();
        if (_pin.IsAllocated) _pin.Free();
    }
}

/// <summary>Shapes a line with the first face, then the runs it has no glyphs for with the next face, and so on.</summary>
internal static class Shaper
{
    private record struct Piece(int Face, int Glyph, int Cluster, float Advance, Vector2 Offset);

    public static ShapedGlyph[] Shape(IReadOnlyList<FontFace> faces, string line, Func<int, float> scaleOf, float baseline)
    {
        // Each run of one direction (Unicode's bidirectional algorithm), shaped in that direction, in drawing order.
        var pieces = new List<Piece>();
        foreach (var run in Bidi.VisualRuns(line))
            pieces.AddRange(ShapeRange(faces, 0, line, run.Start, run.Length, scaleOf, run.RightToLeft ? Direction.RightToLeft : Direction.LeftToRight));
        var result = new ShapedGlyph[pieces.Count];
        float pen = 0;
        for (int i = 0; i < pieces.Count; i++)
        {
            var p = pieces[i];
            result[i] = new ShapedGlyph(p.Face, p.Glyph, p.Cluster, new Vector2(pen + p.Offset.X, baseline - p.Offset.Y), pen, p.Advance);
            pen += p.Advance;
        }
        return result;
    }

    private static List<Piece> ShapeRange(IReadOnlyList<FontFace> faces, int faceIndex, string line, int start, int length, Func<int, float> scaleOf, Direction direction)
    {
        var face = faces[faceIndex];
        float scale = scaleOf(faceIndex);
        using var buffer = new HbBuffer();
        buffer.AddUtf16(line, start, length);
        buffer.Direction = direction;
        buffer.GuessSegmentProperties(); // the script and language; the direction stays the run's
        face.Shaper.Shape(buffer);
        var infos = buffer.GlyphInfos;
        var positions = buffer.GlyphPositions;
        var pieces = new List<Piece>(infos.Length);
        for (int i = 0; i < infos.Length; i++)
            pieces.Add(new Piece(faceIndex, (int)infos[i].Codepoint, (int)infos[i].Cluster, positions[i].XAdvance * scale,
                new Vector2(positions[i].XOffset * scale, positions[i].YOffset * scale)));
        if (faceIndex + 1 >= faces.Count || !pieces.Exists(p => p.Glyph == 0)) return pieces;

        // Where each cluster ends: the next cluster that starts after it, or the end of the range.
        var starts = pieces.Select(p => p.Cluster).Distinct().Order().ToArray();
        int End(int cluster)
        {
            int at = Array.BinarySearch(starts, cluster);
            return at + 1 < starts.Length ? starts[at + 1] : start + length;
        }

        // Each run of missing glyphs (in drawing order) is shaped again with the next face, in its place.
        var result = new List<Piece>(pieces.Count);
        for (int i = 0; i < pieces.Count;)
        {
            if (pieces[i].Glyph != 0) { result.Add(pieces[i++]); continue; }
            int first = i, from = int.MaxValue, to = 0;
            for (; i < pieces.Count && pieces[i].Glyph == 0; i++)
            {
                from = Math.Min(from, pieces[i].Cluster);
                to = Math.Max(to, End(pieces[i].Cluster));
            }
            var again = ShapeRange(faces, faceIndex + 1, line, from, to - from, scaleOf, direction);
            result.AddRange(again.Exists(p => p.Glyph != 0) ? again : pieces.GetRange(first, i - first));
        }
        return result;
    }
}

/// <summary>The glyph pages of a shaped font: each glyph rasterized once per size and face, packed in shelves.</summary>
internal sealed class GlyphPages(IGraphicsDevice device, int pageSize, TextureFilter filter)
{
    internal sealed record Slot(Texture Page, RectF Source, Vector2 Offset);

    private readonly Dictionary<(int Face, int Glyph, float Size), Slot?> _slots = [];
    private readonly List<Texture> _pages = [];
    private int _x, _y, _shelf;
    private TextureFilter _filter = filter;

    public int Count => _pages.Count;
    public long Bytes => _pages.Sum(p => p.SizeInBytes);
    public int Rasterized { get; private set; }

    public TextureFilter Filter
    {
        get => _filter;
        set
        {
            _filter = value;
            foreach (var page in _pages) page.Filter = value;
        }
    }

    /// <summary>The glyph's place in a page, rasterizing it the first time; null for a glyph with no ink (a space).</summary>
    public unsafe Slot? Get(FontFace face, int faceIndex, int glyph, float size, float scale, bool hard)
    {
        var key = (faceIndex, glyph, size);
        if (_slots.TryGetValue(key, out var slot)) return slot;
        int x0, y0, x1, y1;
        StbTrueType.stbtt_GetGlyphBitmapBox(face.Rasterizer, glyph, scale, scale, &x0, &y0, &x1, &y1);
        int w = x1 - x0, h = y1 - y0;
        if (w <= 0 || h <= 0) return _slots[key] = null;
        if (w + 2 > pageSize || h + 2 > pageSize) throw new InvalidOperationException($"A glyph of {face.Name} at size {size} ({w}x{h} pixels) does not fit a glyph page of {pageSize}; use a larger FontOptions.PageSize.");

        var coverage = new byte[w * h];
        fixed (byte* pixels = coverage) StbTrueType.stbtt_MakeGlyphBitmap(face.Rasterizer, pixels, w, h, w, scale, scale, glyph);
        // White pixels whose alpha is the coverage (straight alpha), or fully opaque where at least half is covered.
        var colors = new Color[w * h];
        for (int i = 0; i < colors.Length; i++)
            colors[i] = new Color((byte)255, (byte)255, (byte)255, hard ? (coverage[i] >= 128 ? (byte)255 : (byte)0) : coverage[i]);

        var (page, x, y) = Place(w, h);
        page.Update(x, y, w, h, colors);
        Rasterized++;
        return _slots[key] = new Slot(page, new RectF(x, y, w, h), new Vector2(x0, y0));
    }

    /// <summary>Room for a <paramref name="w"/>×<paramref name="h"/> glyph with a pixel of padding: the current shelf, a new shelf, or a new page.</summary>
    private (Texture Page, int X, int Y) Place(int w, int h)
    {
        if (_pages.Count == 0 || _x + w + 1 > pageSize) { _y += _shelf; _x = 1; _shelf = 0; }
        if (_pages.Count == 0 || _y + h + 1 > pageSize)
        {
            _pages.Add(device.CreateTexture(new PixelImage(pageSize, pageSize), _filter));
            (_x, _y, _shelf) = (1, 1, 0);
        }
        var at = (_pages[^1], _x, _y);
        _x += w + 1;
        _shelf = Math.Max(_shelf, h + 1);
        return at;
    }

    public void Clear()
    {
        foreach (var page in _pages) page.Dispose();
        _pages.Clear();
        _slots.Clear();
        (_x, _y, _shelf) = (0, 0, 0);
    }
}

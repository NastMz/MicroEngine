using System.Buffers.Binary;
using System.Text;

namespace MicroEngine.Localization;

/// <summary>
/// The characters a TrueType or OpenType font file has glyphs for, read from its character map without rasterizing
/// anything: which characters of a language's texts a font would draw as <c>?</c>. The content check uses it for the
/// fonts a string table names; a game can ask the same of the fonts it loads
/// (<c>FontCoverage.Read(Assets.Bytes("fonts/ui.ttf")).Missing(Localization.Characters())</c>).
/// </summary>
/// <remarks>
/// Having a glyph for each character is necessary and, for scripts that join or reorder letters (Arabic, Indic
/// scripts), not sufficient: those also need the shaping package to draw correctly.
/// </remarks>
public sealed class FontCoverage
{
    private readonly (int Start, int End)[] _ranges;

    private FontCoverage((int, int)[] ranges) => _ranges = ranges;

    /// <summary>How many characters the font has.</summary>
    public int Count => _ranges.Sum(r => r.End - r.Start + 1);

    /// <summary>Reads the character map of a font file (<c>.ttf</c>, <c>.otf</c>, or the first face of a <c>.ttc</c>).</summary>
    /// <exception cref="InvalidDataException">The bytes are not a font, or the font has no Unicode character map.</exception>
    public static FontCoverage Read(byte[] font)
    {
        ArgumentNullException.ThrowIfNull(font);
        try
        {
            var data = font.AsSpan();
            int start = 0;
            if (data.Length >= 16 && data[..4].SequenceEqual("ttcf"u8)) start = (int)U32(data, 12);
            uint version = U32(data, start);
            if (version is not (0x00010000 or 0x4F54544F or 0x74727565)) throw new InvalidDataException("Not a TrueType or OpenType font.");
            int tables = U16(data, start + 4), cmap = -1;
            for (int i = 0; i < tables; i++)
            {
                int record = start + 12 + i * 16;
                if (data.Slice(record, 4).SequenceEqual("cmap"u8)) cmap = (int)U32(data, record + 8);
            }
            if (cmap < 0) throw new InvalidDataException("The font has no character map (cmap table).");

            // The widest Unicode map the font has: full range (format 12) before the basic plane (format 4).
            int count = U16(data, cmap + 2), best = -1, bestRank = 0;
            for (int i = 0; i < count; i++)
            {
                int record = cmap + 4 + i * 8;
                int platform = U16(data, record), encoding = U16(data, record + 2), offset = cmap + (int)U32(data, record + 4);
                int format = U16(data, offset);
                bool unicode = platform == 0 || (platform == 3 && encoding is 1 or 10);
                int rank = !unicode ? 0 : format == 12 ? 2 : format == 4 ? 1 : 0;
                if (rank > bestRank) (best, bestRank) = (offset, rank);
            }
            if (best < 0) throw new InvalidDataException("The font has no Unicode character map the engine reads (cmap format 4 or 12).");
            var ranges = bestRank == 2 ? Format12(data, best) : Format4(data, best);
            return new FontCoverage(Merge(ranges));
        }
        catch (Exception e) when (e is ArgumentOutOfRangeException or IndexOutOfRangeException or OverflowException)
        {
            throw new InvalidDataException("Not a font the engine reads: the file is cut short or damaged.", e);
        }
    }

    private static ushort U16(ReadOnlySpan<byte> data, int at) => BinaryPrimitives.ReadUInt16BigEndian(data.Slice(at, 2));
    private static uint U32(ReadOnlySpan<byte> data, int at) => BinaryPrimitives.ReadUInt32BigEndian(data.Slice(at, 4));

    private static List<(int, int)> Format12(ReadOnlySpan<byte> data, int table)
    {
        var ranges = new List<(int, int)>();
        uint groups = U32(data, table + 12);
        for (int i = 0; i < groups; i++)
        {
            int group = table + 16 + i * 12;
            uint first = U32(data, group), last = U32(data, group + 4), glyph = U32(data, group + 8);
            // A group that starts at glyph 0 maps its first character to "no glyph".
            if (glyph == 0) first++;
            if (first <= last && last <= 0x10FFFF) ranges.Add(((int)first, (int)last));
        }
        return ranges;
    }

    private static List<(int, int)> Format4(ReadOnlySpan<byte> data, int table)
    {
        var ranges = new List<(int, int)>();
        int segments = U16(data, table + 6) / 2;
        int ends = table + 14, starts = ends + segments * 2 + 2, deltas = starts + segments * 2, offsets = deltas + segments * 2;
        for (int s = 0; s < segments; s++)
        {
            int end = U16(data, ends + s * 2), first = U16(data, starts + s * 2);
            int delta = U16(data, deltas + s * 2), offset = U16(data, offsets + s * 2);
            if (first == 0xFFFF) continue;
            int run = -1;
            for (int c = first; c <= end && c < 0xFFFF; c++)
            {
                int glyph = offset == 0 ? (c + delta) & 0xFFFF : U16(data, offsets + s * 2 + offset + (c - first) * 2);
                if (glyph != 0 && offset != 0) glyph = (glyph + delta) & 0xFFFF;
                if (glyph != 0) { if (run < 0) run = c; }
                else if (run >= 0) { ranges.Add((run, c - 1)); run = -1; }
            }
            if (run >= 0) ranges.Add((run, Math.Min(end, 0xFFFE)));
        }
        return ranges;
    }

    private static (int, int)[] Merge(List<(int Start, int End)> ranges)
    {
        ranges.Sort((a, b) => a.Start.CompareTo(b.Start));
        var merged = new List<(int Start, int End)>();
        foreach (var range in ranges)
        {
            if (merged.Count > 0 && range.Start <= merged[^1].End + 1) merged[^1] = (merged[^1].Start, Math.Max(merged[^1].End, range.End));
            else merged.Add(range);
        }
        return [.. merged];
    }

    /// <summary>Whether the font has a glyph for a character (a Unicode code point).</summary>
    public bool Covers(int codePoint)
    {
        int low = 0, high = _ranges.Length - 1;
        while (low <= high)
        {
            int middle = (low + high) / 2;
            if (codePoint < _ranges[middle].Start) high = middle - 1;
            else if (codePoint > _ranges[middle].End) low = middle + 1;
            else return true;
        }
        return false;
    }

    /// <summary>The characters of <paramref name="text"/> the font lacks, each once, in the order met. Spaces, line breaks and other characters that draw nothing never count.</summary>
    public string Missing(string text) => Missing(text, this);

    /// <summary>The characters of <paramref name="text"/> that no font of <paramref name="chain"/> has (a face and those that fill in what it lacks), each once, in the order met.</summary>
    public static string Missing(string text, params FontCoverage[] chain)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(chain);
        StringBuilder? missing = null;
        HashSet<int>? seen = null;
        foreach (var rune in text.EnumerateRunes())
        {
            if (Rune.IsWhiteSpace(rune) || Rune.IsControl(rune) || IsInvisible(rune.Value)) continue;
            bool covered = false;
            foreach (var font in chain)
                if (font.Covers(rune.Value)) { covered = true; break; }
            if (covered || !(seen ??= []).Add(rune.Value)) continue;
            (missing ??= new StringBuilder()).Append(rune.ToString());
        }
        return missing?.ToString() ?? "";
    }

    /// <summary>Marks that direct layout and draw nothing themselves: direction marks, joiners, variation selectors, the byte-order mark.</summary>
    private static bool IsInvisible(int c) => c is (>= 0x200B and <= 0x200F) or (>= 0x202A and <= 0x202E) or (>= 0x2060 and <= 0x2069) or (>= 0xFE00 and <= 0xFE0F) or 0xFEFF;
}

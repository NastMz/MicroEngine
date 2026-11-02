using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace MicroEngine.Graphics;

/// <summary>One named rectangle of an atlas image, as a packing tool described it.</summary>
/// <param name="Name">The frame's name, without a file extension ("hero_run_0" for "hero_run_0.png", "slime 0" for Aseprite's "slime 0.aseprite").</param>
/// <param name="Source">The rectangle in the atlas image, in pixels.</param>
/// <param name="Origin">
/// The anchor in the rectangle's pixels (see <see cref="Sprite.Origin"/>): the tool's pivot when it wrote one, otherwise the
/// <see cref="AtlasOptions.Pivot"/> of the untrimmed frame, so trimmed frames line up as their untrimmed originals.
/// </param>
/// <param name="Duration">How long the frame shows in an animation, in seconds, when the tool wrote it (Aseprite).</param>
public readonly record struct AtlasRegion(string Name, RectF Source, Vector2 Origin, float? Duration);

/// <summary>The order an animation plays its frames in (Aseprite's tag directions).</summary>
public enum AnimationDirection { Forward, Reverse, PingPong, PingPongReverse }

/// <summary>A named run of frames (an Aseprite frame tag).</summary>
/// <param name="Name">The tag's name.</param>
/// <param name="From">Index of the first frame in <see cref="AtlasData.Regions"/>.</param>
/// <param name="To">Index of the last frame, inclusive.</param>
/// <param name="Direction">How the frames play.</param>
public sealed record AtlasAnimation(string Name, int From, int To, AnimationDirection Direction);

/// <summary>How atlas files are read.</summary>
/// <param name="Pivot">
/// The anchor of frames whose file gives none, as a fraction of the untrimmed frame: (0, 0) is its top-left (the default),
/// (0.5, 1) the middle of its bottom edge (feet).
/// </param>
/// <param name="KeepExtensions">Keeps ".png", ".aseprite" and similar endings in frame names (false by default: "hero.png" is "hero").</param>
public sealed record AtlasOptions(Vector2 Pivot = default, bool KeepExtensions = false)
{
    /// <summary>Top-left anchors, names without image extensions.</summary>
    public static AtlasOptions Default { get; } = new();
}

/// <summary>
/// The description of an atlas image written by a packing tool, read without any graphics device: the JSON of
/// TexturePacker, Free Texture Packer and Aseprite (hash or array of frames; Aseprite's frame durations and tags), and the
/// Starling/Sparrow XML of TexturePacker, ShoeBox and Kenney's packs. Trimmed frames keep their place in the untrimmed
/// frame; rotated frames are refused (turn rotation off in the tool).
/// </summary>
public sealed class AtlasData
{
    // Endings packing tools leave on frame names: image files, and Aseprite's default "{title} {frame}.aseprite".
    private static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg", ".bmp", ".tga", ".gif", ".psd", ".aseprite", ".ase"];

    private AtlasData(string? image, List<AtlasRegion> regions, List<AtlasAnimation> animations)
        => (ImagePath, Regions, Animations) = (image, regions, animations);

    /// <summary>The atlas image as the file names it (relative to the file), or null when it names none.</summary>
    public string? ImagePath { get; }
    /// <summary>The frames, in the file's order.</summary>
    public IReadOnlyList<AtlasRegion> Regions { get; }
    /// <summary>The frame tags (Aseprite), in the file's order; empty for formats without tags.</summary>
    public IReadOnlyList<AtlasAnimation> Animations { get; }

    /// <summary>Reads atlas JSON or XML, recognised by its first character.</summary>
    /// <param name="text">The file's text.</param>
    /// <param name="options">Pivot and naming; null for <see cref="AtlasOptions.Default"/>.</param>
    /// <exception cref="InvalidDataException">The text is not an atlas in a known format; the message says what is wrong.</exception>
    public static AtlasData Parse(string text, AtlasOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        options ??= AtlasOptions.Default;
        var start = text.AsSpan().TrimStart();
        if (start.Length > 0 && start[0] == '﻿') start = start[1..].TrimStart();
        if (start.StartsWith("{")) return ParseJson(text, options);
        if (start.StartsWith("<")) return ParseXml(text, options);
        throw new InvalidDataException("Not an atlas: expected JSON (TexturePacker, Aseprite) or XML (Starling).");
    }

    /// <summary>Reads an atlas file (JSON or XML).</summary>
    /// <exception cref="InvalidDataException">The file is not an atlas in a known format; the message names it.</exception>
    public static AtlasData Load(string path, AtlasOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        try { return Parse(File.ReadAllText(path), options); }
        catch (InvalidDataException e) { throw new InvalidDataException($"{path}: {e.Message}", e); }
    }

    private static string CleanName(string raw, AtlasOptions options)
    {
        if (options.KeepExtensions) return raw;
        foreach (var ext in ImageExtensions)
            if (raw.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) return raw[..^ext.Length];
        return raw;
    }

    private static Vector2 Anchor(AtlasOptions options, Vector2? pivot, float sourceW, float sourceH, float trimX, float trimY) =>
        (pivot ?? options.Pivot) * new Vector2(sourceW, sourceH) - new Vector2(trimX, trimY);

    private static AtlasData ParseJson(string text, AtlasOptions options)
    {
        JsonDocument document;
        try { document = JsonDocument.Parse(text, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip }); }
        catch (JsonException e) { throw new InvalidDataException($"Invalid JSON: {e.Message}", e); }
        using (document)
        {
            var root = document.RootElement;
            if (!root.TryGetProperty("frames", out var frames)) throw new InvalidDataException("The JSON has no \"frames\".");
            var regions = new List<AtlasRegion>();
            if (frames.ValueKind == JsonValueKind.Array)
                foreach (var frame in frames.EnumerateArray())
                    regions.Add(JsonFrame(Str(frame, "filename") ?? throw new InvalidDataException("A frame of the \"frames\" array has no \"filename\"."), frame, options));
            else if (frames.ValueKind == JsonValueKind.Object)
                foreach (var frame in frames.EnumerateObject()) regions.Add(JsonFrame(frame.Name, frame.Value, options));
            else throw new InvalidDataException("\"frames\" is neither an array nor an object.");
            string? image = null;
            var animations = new List<AtlasAnimation>();
            if (root.TryGetProperty("meta", out var meta) && meta.ValueKind == JsonValueKind.Object)
            {
                image = Str(meta, "image");
                if (meta.TryGetProperty("frameTags", out var tags) && tags.ValueKind == JsonValueKind.Array)
                    foreach (var tag in tags.EnumerateArray())
                    {
                        string name = Str(tag, "name") ?? throw new InvalidDataException("A frame tag has no name.");
                        int from = Int(tag, "from"), to = Int(tag, "to");
                        if (from < 0 || to < from || to >= regions.Count)
                            throw new InvalidDataException($"Frame tag '{name}' covers frames {from}-{to}, but the atlas has {regions.Count}.");
                        var direction = (Str(tag, "direction") ?? "forward").ToLowerInvariant() switch
                        {
                            "forward" => AnimationDirection.Forward,
                            "reverse" => AnimationDirection.Reverse,
                            "pingpong" => AnimationDirection.PingPong,
                            "pingpong_reverse" => AnimationDirection.PingPongReverse,
                            var other => throw new InvalidDataException($"Frame tag '{name}' has an unknown direction '{other}'."),
                        };
                        animations.Add(new AtlasAnimation(name, from, to, direction));
                    }
            }
            return new AtlasData(image, regions, animations);
        }
    }

    private static AtlasRegion JsonFrame(string rawName, JsonElement frame, AtlasOptions options)
    {
        string name = CleanName(rawName, options);
        if (frame.ValueKind != JsonValueKind.Object || !frame.TryGetProperty("frame", out var rect))
            throw new InvalidDataException($"Frame '{name}' has no \"frame\" rectangle.");
        if (frame.TryGetProperty("rotated", out var rotated) && rotated.ValueKind == JsonValueKind.True)
            throw new InvalidDataException($"Frame '{name}' is rotated in the atlas; export without rotation (TexturePacker: \"Allow rotation\" off).");
        float x = Int(rect, "x"), y = Int(rect, "y"), w = Int(rect, "w"), h = Int(rect, "h");
        if (w <= 0 || h <= 0) throw new InvalidDataException($"Frame '{name}' has an empty rectangle.");
        float trimX = 0, trimY = 0, sourceW = w, sourceH = h;
        if (frame.TryGetProperty("spriteSourceSize", out var trim)) (trimX, trimY) = (Int(trim, "x"), Int(trim, "y"));
        if (frame.TryGetProperty("sourceSize", out var size)) (sourceW, sourceH) = (Int(size, "w"), Int(size, "h"));
        Vector2? pivot = frame.TryGetProperty("pivot", out var p) && p.ValueKind == JsonValueKind.Object ? new Vector2(Float(p, "x"), Float(p, "y")) : null;
        float? duration = frame.TryGetProperty("duration", out var d) && d.ValueKind == JsonValueKind.Number ? d.GetSingle() / 1000f : null;
        if (duration is <= 0) throw new InvalidDataException($"Frame '{name}' has a duration of {duration * 1000} ms; durations are positive.");
        return new AtlasRegion(name, new RectF(x, y, w, h), Anchor(options, pivot, sourceW, sourceH, trimX, trimY), duration);
    }

    private static string? Str(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static int Int(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? (int)MathF.Round(v.GetSingle())
            : throw new InvalidDataException($"Missing number \"{name}\".");

    private static float Float(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetSingle() : throw new InvalidDataException($"Missing number \"{name}\".");

    private static AtlasData ParseXml(string text, AtlasOptions options)
    {
        XDocument document;
        try { document = XDocument.Parse(text); }
        catch (XmlException e) { throw new InvalidDataException($"Invalid XML: {e.Message}", e); }
        var root = document.Root;
        if (root is null || root.Name.LocalName != "TextureAtlas")
            throw new InvalidDataException($"The XML's root is <{root?.Name.LocalName}>, not a Starling <TextureAtlas>.");
        var regions = new List<AtlasRegion>();
        foreach (var sub in root.Elements().Where(e => e.Name.LocalName == "SubTexture"))
        {
            string name = CleanName((string?)sub.Attribute("name") ?? throw new InvalidDataException("A <SubTexture> has no name."), options);
            if (string.Equals((string?)sub.Attribute("rotated"), "true", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"SubTexture '{name}' is rotated in the atlas; export without rotation.");
            float Attr(string attribute, float? fallback = null) =>
                sub.Attribute(attribute) is { } a && float.TryParse(a.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v
                : fallback ?? throw new InvalidDataException($"SubTexture '{name}' has no valid {attribute}.");
            float x = Attr("x"), y = Attr("y"), w = Attr("width"), h = Attr("height");
            if (w <= 0 || h <= 0) throw new InvalidDataException($"SubTexture '{name}' has an empty rectangle.");
            // Starling writes the trimmed rectangle's offset inside the original frame as negative frameX/frameY.
            float trimX = -Attr("frameX", 0), trimY = -Attr("frameY", 0);
            float sourceW = Attr("frameWidth", w), sourceH = Attr("frameHeight", h);
            Vector2? pivot = sub.Attribute("pivotX") is not null && sub.Attribute("pivotY") is not null
                ? new Vector2(Attr("pivotX") / sourceW, Attr("pivotY") / sourceH) : null;
            regions.Add(new AtlasRegion(name, new RectF(x, y, w, h), Anchor(options, pivot, sourceW, sourceH, trimX, trimY), null));
        }
        if (regions.Count == 0) throw new InvalidDataException("The atlas has no <SubTexture>.");
        return new AtlasData((string?)root.Attribute("imagePath"), regions, []);
    }
}

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
/// <param name="Rotated">
/// Whether the tool stored the frame turned 90 degrees clockwise to pack it tighter: <paramref name="Source"/> is then the
/// turned rectangle as it lies in the image, and <paramref name="Origin"/> the anchor of the upright frame.
/// <see cref="AtlasData.ToSheet"/> turns such frames upright.
/// </param>
public readonly record struct AtlasRegion(string Name, RectF Source, Vector2 Origin, float? Duration, bool Rotated = false)
{
    /// <summary>The frame's named points, in the rectangle's pixels (of the upright frame); null when the file gives none.</summary>
    public IReadOnlyList<SpritePoint>? Points { get; init; }
    /// <summary>The frame's named boxes, in the rectangle's pixels (of the upright frame); null when the file gives none.</summary>
    public IReadOnlyList<SpriteBox>? Boxes { get; init; }
}

/// <summary>The order an animation plays its frames in (Aseprite's tag directions).</summary>
public enum AnimationDirection { Forward, Reverse, PingPong, PingPongReverse }

/// <summary>A named run of frames (an Aseprite frame tag).</summary>
/// <param name="Name">The tag's name.</param>
/// <param name="From">Index of the first frame in <see cref="AtlasData.Regions"/>.</param>
/// <param name="To">Index of the last frame, inclusive.</param>
/// <param name="Direction">How the frames play.</param>
/// <param name="Loop">Whether it repeats (true unless the tag says it plays a number of times: Aseprite's "repeat").</param>
public sealed record AtlasAnimation(string Name, int From, int To, AnimationDirection Direction, bool Loop = true);

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
/// frame; frames the JSON says are rotated are marked (<see cref="AtlasRegion.Rotated"/>) and turned upright when the
/// atlas is loaded with its image; rotated frames of the XML format are refused (turn rotation off in the tool).
/// Aseprite's slices (<c>meta.slices</c>) become data of the frames: each slice a box of its name on the frames it
/// covers, and its pivot a point of the same name.
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

    /// <summary>
    /// The atlas as loose frames and clips, cut out of <paramref name="image"/>: each frame its own picture (one stored
    /// rotated is turned upright), tags as clips. For packing a tool's atlas again, with others or after changing its
    /// frames (<see cref="SpriteAtlasBuilder.AddSheet"/>).
    /// </summary>
    /// <param name="image">The atlas image.</param>
    /// <exception cref="InvalidDataException">A frame lies outside the image, or two frames share a name.</exception>
    public SpriteSheet ToSheet(PixelImage image)
    {
        ArgumentNullException.ThrowIfNull(image);
        var frames = new List<SheetFrame>(Regions.Count);
        foreach (var region in Regions)
        {
            var r = region.Source;
            if (r.X < 0 || r.Y < 0 || r.Right > image.Width || r.Bottom > image.Height)
                throw new InvalidDataException($"Frame '{region.Name}' ({r.X}, {r.Y}, {r.Width}, {r.Height}) lies outside the {image.Width}x{image.Height} atlas image.");
            var cut = image.Crop((int)r.X, (int)r.Y, (int)r.Width, (int)r.Height);
            // Stored a quarter turn clockwise: three more quarter turns bring it back.
            if (region.Rotated) cut = cut.Rotate90().Rotate90().Rotate90();
            frames.Add(new SheetFrame(region.Name, cut, region.Origin) { Points = region.Points ?? [], Boxes = region.Boxes ?? [] });
        }
        var clips = new List<SheetClip>(Animations.Count);
        foreach (var tag in Animations)
        {
            int count = tag.To - tag.From + 1;
            var names = new string[count];
            var durations = new float[count];
            for (int i = 0; i < count; i++)
            {
                names[i] = Regions[tag.From + i].Name;
                durations[i] = Regions[tag.From + i].Duration ?? SpriteAtlas.DefaultFrameDuration;
            }
            // A later tag of the same name replaces the earlier one, as in SpriteAtlas.Create.
            clips.RemoveAll(c => c.Name == tag.Name);
            clips.Add(new SheetClip(tag.Name, names, durations, 0, tag.Direction, tag.Loop));
        }
        return new SpriteSheet(frames, clips);
    }

    /// <summary>Whether a frame is stored rotated, so the atlas needs its pixels turned (<see cref="ToSheet"/>) before it is drawn.</summary>
    public bool HasRotatedFrames
    {
        get
        {
            foreach (var region in Regions)
                if (region.Rotated) return true;
            return false;
        }
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
        try
        {
            var root = document.RootElement;
            if (!root.TryGetProperty("frames", out var frames)) throw new InvalidDataException("The JSON has no \"frames\".");
            var regions = new List<AtlasRegion>();
            // Where each frame's kept pixels lie in its whole picture: slices are drawn on the whole picture.
            var trims = new List<Vector2>();
            if (frames.ValueKind == JsonValueKind.Array)
                foreach (var frame in frames.EnumerateArray())
                {
                    regions.Add(JsonFrame(Str(frame, "filename") ?? throw new InvalidDataException("A frame of the \"frames\" array has no \"filename\"."), frame, options));
                    trims.Add(TrimOf(frame));
                }
            else if (frames.ValueKind == JsonValueKind.Object)
                foreach (var frame in frames.EnumerateObject())
                {
                    regions.Add(JsonFrame(frame.Name, frame.Value, options));
                    trims.Add(TrimOf(frame.Value));
                }
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
                        // Aseprite writes "repeat" only for a tag that plays a number of times ("0" or nothing: for ever).
                        bool loop = !tag.TryGetProperty("repeat", out var repeat) || (repeat.ValueKind == JsonValueKind.String ? repeat.GetString() : repeat.GetRawText()) is "0" or "" or null;
                        animations.Add(new AtlasAnimation(name, from, to, direction, loop));
                    }
                if (meta.TryGetProperty("slices", out var slices) && slices.ValueKind == JsonValueKind.Array) AddSlices(slices, regions, trims);
            }
            if (regions.Count == 0) throw new InvalidDataException("The atlas has no frames.");
            return new AtlasData(image, regions, animations);
        }
        // A value of the wrong kind where an object or a number is expected ("spriteSourceSize": null).
        catch (InvalidOperationException e) { throw new InvalidDataException($"Unexpected JSON structure: {e.Message}", e); }
    }

    private static Vector2 TrimOf(JsonElement frame) =>
        frame.ValueKind == JsonValueKind.Object && frame.TryGetProperty("spriteSourceSize", out var trim) ? new Vector2(Int(trim, "x"), Int(trim, "y")) : Vector2.Zero;

    /// <summary>
    /// Aseprite's slices as data of the frames: from the frame of each key on, a box named after the slice, and a point
    /// of the same name where its pivot is. Slices are drawn on the whole picture, so a trimmed frame counts from its
    /// own corner.
    /// </summary>
    private static void AddSlices(JsonElement slices, List<AtlasRegion> regions, List<Vector2> trims)
    {
        var points = new List<SpritePoint>?[regions.Count];
        var boxes = new List<SpriteBox>?[regions.Count];
        foreach (var slice in slices.EnumerateArray())
        {
            if (slice.ValueKind != JsonValueKind.Object || Str(slice, "name") is not { Length: > 0 } name) continue;
            if (!slice.TryGetProperty("keys", out var keys) || keys.ValueKind != JsonValueKind.Array) continue;
            var ordered = keys.EnumerateArray().Where(k => k.ValueKind == JsonValueKind.Object && k.TryGetProperty("bounds", out _)).OrderBy(k => Int(k, "frame")).ToList();
            for (int k = 0; k < ordered.Count; k++)
            {
                var key = ordered[k];
                var bounds = key.GetProperty("bounds");
                float x = Int(bounds, "x"), y = Int(bounds, "y"), w = Int(bounds, "w"), h = Int(bounds, "h");
                Vector2? pivot = key.TryGetProperty("pivot", out var p) && p.ValueKind == JsonValueKind.Object ? new Vector2(Int(p, "x"), Int(p, "y")) : null;
                int from = Math.Max(0, Int(key, "frame")), to = k + 1 < ordered.Count ? Int(ordered[k + 1], "frame") : regions.Count;
                for (int i = from; i < Math.Min(to, regions.Count); i++)
                {
                    if (w > 0 && h > 0) (boxes[i] ??= []).Add(new SpriteBox(name, new RectF(x - trims[i].X, y - trims[i].Y, w, h)));
                    if (pivot is { } at) (points[i] ??= []).Add(new SpritePoint(name, new Vector2(x, y) + at - trims[i]));
                }
            }
        }
        for (int i = 0; i < regions.Count; i++)
        {
            if (points[i] is null && boxes[i] is null) continue;
            var region = regions[i];
            regions[i] = region with
            {
                Points = points[i] is { } more ? [.. region.Points ?? [], .. more] : region.Points,
                Boxes = boxes[i] is { } moreBoxes ? [.. region.Boxes ?? [], .. moreBoxes] : region.Boxes,
            };
        }
    }

    /// <summary>The points and boxes a frame carries in its own entry (as <see cref="PackedAtlas.ToJson"/> writes them), in the whole picture's pixels.</summary>
    private static (IReadOnlyList<SpritePoint>? Points, IReadOnlyList<SpriteBox>? Boxes) FrameMarks(JsonElement frame, float trimX, float trimY)
    {
        List<SpritePoint>? points = null;
        List<SpriteBox>? boxes = null;
        if (frame.TryGetProperty("points", out var ps) && ps.ValueKind == JsonValueKind.Object)
            foreach (var p in ps.EnumerateObject())
                (points ??= []).Add(new SpritePoint(p.Name, new Vector2((float)Double(p.Value, "x") - trimX, (float)Double(p.Value, "y") - trimY)));
        if (frame.TryGetProperty("boxes", out var bs) && bs.ValueKind == JsonValueKind.Object)
            foreach (var b in bs.EnumerateObject())
                (boxes ??= []).Add(new SpriteBox(b.Name, new RectF((float)Double(b.Value, "x") - trimX, (float)Double(b.Value, "y") - trimY, (float)Double(b.Value, "w"), (float)Double(b.Value, "h"))));
        return (points, boxes);
    }

    private static AtlasRegion JsonFrame(string rawName, JsonElement frame, AtlasOptions options)
    {
        string name = CleanName(rawName, options);
        if (frame.ValueKind != JsonValueKind.Object || !frame.TryGetProperty("frame", out var rect))
            throw new InvalidDataException($"Frame '{name}' has no \"frame\" rectangle.");
        // TexturePacker and Free Texture Packer keep the upright size in "frame" and store the pixels turned 90 degrees clockwise.
        bool turned = frame.TryGetProperty("rotated", out var rotated) && rotated.ValueKind == JsonValueKind.True;
        float x = Int(rect, "x"), y = Int(rect, "y"), w = Int(rect, "w"), h = Int(rect, "h");
        if (w <= 0 || h <= 0) throw new InvalidDataException($"Frame '{name}' has an empty rectangle.");
        float trimX = 0, trimY = 0, sourceW = w, sourceH = h;
        if (frame.TryGetProperty("spriteSourceSize", out var trim)) (trimX, trimY) = (Int(trim, "x"), Int(trim, "y"));
        if (frame.TryGetProperty("sourceSize", out var size)) (sourceW, sourceH) = (Int(size, "w"), Int(size, "h"));
        bool hasPivot = frame.TryGetProperty("pivot", out var p) && p.ValueKind == JsonValueKind.Object;
        float? duration = frame.TryGetProperty("duration", out var d) && d.ValueKind == JsonValueKind.Number ? d.GetSingle() / 1000f : null;
        if (duration is <= 0) throw new InvalidDataException($"Frame '{name}' has a duration of {duration * 1000} ms; durations are positive.");
        // A file's pivot is a fraction of the frame: multiplied at double precision, a whole-pixel anchor written as a
        // fraction (27 of 55) comes back as that pixel.
        var origin = hasPivot ? new Vector2((float)(Double(p, "x") * sourceW) - trimX, (float)(Double(p, "y") * sourceH) - trimY)
            : Anchor(options, null, sourceW, sourceH, trimX, trimY);
        var (points, boxes) = FrameMarks(frame, trimX, trimY);
        return new AtlasRegion(name, turned ? new RectF(x, y, h, w) : new RectF(x, y, w, h), origin, duration, turned) { Points = points, Boxes = boxes };
    }

    private static string? Str(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static int Int(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? (int)MathF.Round(v.GetSingle())
            : throw new InvalidDataException($"Missing number \"{name}\".");

    private static double Double(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : throw new InvalidDataException($"Missing number \"{name}\".");

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

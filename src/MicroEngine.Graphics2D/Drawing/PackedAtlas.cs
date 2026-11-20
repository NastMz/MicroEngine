using System.Globalization;
using System.Text;
using System.Text.Json;

namespace MicroEngine.Graphics;

/// <summary>
/// Sprites packed into one picture by <see cref="SpriteAtlasBuilder.Pack"/>, without a graphics device: the picture,
/// where each sprite lies in it, and the clips. <see cref="ToJson"/> writes the description packing tools write, so the
/// pair (a PNG and a JSON file) is an ordinary atlas for <c>Assets.Atlas</c>.
/// </summary>
/// <param name="Image">The packed picture.</param>
/// <param name="Regions">Each sprite's name, rectangle and anchor, in the order they were added.</param>
/// <param name="Clips">The animations.</param>
public sealed record PackedAtlas(PixelImage Image, IReadOnlyList<AtlasRegion> Regions, IReadOnlyList<SheetClip> Clips)
{
    /// <summary>
    /// The atlas as the JSON of packing tools (an array of frames with Aseprite's frame durations and tags), which
    /// <see cref="AtlasData.Parse"/> and <c>Assets.Atlas</c> read. The same atlas always gives the same text.
    /// </summary>
    /// <param name="imageName">The picture's file name, next to the JSON file ("items.png").</param>
    /// <remarks>
    /// A clip is written as a frame tag over consecutive frames. One whose frames are not consecutive sprites, or that
    /// times a frame differently from another clip, gets entries of its own named "clip#0", "clip#1"..., on the same
    /// pixels. A fixed rate is written as each frame's duration in milliseconds.
    /// </remarks>
    /// <exception cref="InvalidOperationException">A clip names a sprite the atlas does not have.</exception>
    public string ToJson(string imageName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imageName);
        // What the file lists: every sprite, then one more entry per frame of a clip that a frame tag cannot describe
        // as it stands (frames that are not consecutive, or a frame two clips time differently). Such entries are
        // named "clip#0", "clip#1"... and lie on the same pixels.
        var entries = new List<(string Name, AtlasRegion Region)>(Regions.Count);
        var index = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var region in Regions)
        {
            index[region.Name] = entries.Count;
            entries.Add((region.Name, region));
        }
        var durations = new List<double?>(new double?[entries.Count]);
        var tags = new List<(SheetClip Clip, int From, int To)>();
        foreach (var clip in Clips)
        {
            var at = new int[clip.Frames.Count];
            var ms = new double[clip.Frames.Count];
            bool asIs = true;
            for (int i = 0; i < at.Length; i++)
            {
                at[i] = index.TryGetValue(clip.Frames[i], out int found) ? found : throw new InvalidOperationException($"Clip '{clip.Name}' names the sprite '{clip.Frames[i]}', which is not in the atlas.");
                ms[i] = clip.Durations is { } d ? Math.Round(d[i] * 1000.0, 3) : Math.Round(1000.0 / clip.FramesPerSecond, 3);
                asIs &= at[i] == at[0] + i && (durations[at[i]] is not { } other || other == ms[i]);
            }
            int from = at[0];
            if (asIs)
                for (int i = 0; i < at.Length; i++) durations[at[i]] = ms[i];
            else
            {
                from = entries.Count;
                for (int i = 0; i < at.Length; i++)
                {
                    string alias = $"{clip.Name}#{i}";
                    if (index.ContainsKey(alias)) throw new InvalidOperationException($"Clip '{clip.Name}' cannot be written: the atlas already has a sprite named '{alias}'.");
                    index[alias] = entries.Count;
                    entries.Add((alias, entries[at[i]].Region));
                    durations.Add(ms[i]);
                }
            }
            tags.Add((clip, from, from + at.Length - 1));
        }
        using var stream = new MemoryStream();
        using (var json = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            json.WriteStartObject();
            json.WriteStartArray("frames");
            for (int i = 0; i < entries.Count; i++)
            {
                var (name, region) = entries[i];
                int w = (int)region.Source.Width, h = (int)region.Source.Height;
                json.WriteStartObject();
                json.WriteString("filename", name);
                Rect(json, "frame", (int)region.Source.X, (int)region.Source.Y, w, h);
                json.WriteBoolean("rotated", false);
                json.WriteBoolean("trimmed", false);
                Rect(json, "spriteSourceSize", 0, 0, w, h);
                json.WriteStartObject("sourceSize");
                json.WriteNumber("w", w);
                json.WriteNumber("h", h);
                json.WriteEndObject();
                json.WriteStartObject("pivot");
                json.WriteNumber("x", (double)region.Origin.X / w);
                json.WriteNumber("y", (double)region.Origin.Y / h);
                json.WriteEndObject();
                if (durations[i] is { } ms) json.WriteNumber("duration", ms);
                json.WriteEndObject();
            }
            json.WriteEndArray();
            json.WriteStartObject("meta");
            json.WriteString("app", "MicroEngine");
            json.WriteString("image", imageName);
            json.WriteStartObject("size");
            json.WriteNumber("w", Image.Width);
            json.WriteNumber("h", Image.Height);
            json.WriteEndObject();
            json.WriteStartArray("frameTags");
            foreach (var (clip, from, to) in tags)
            {
                json.WriteStartObject();
                json.WriteString("name", clip.Name);
                json.WriteNumber("from", from);
                json.WriteNumber("to", to);
                json.WriteString("direction", clip.Direction switch
                {
                    AnimationDirection.Reverse => "reverse",
                    AnimationDirection.PingPong => "pingpong",
                    AnimationDirection.PingPongReverse => "pingpong_reverse",
                    _ => "forward",
                });
                if (!clip.Loop) json.WriteString("repeat", 1.ToString(CultureInfo.InvariantCulture));
                json.WriteEndObject();
            }
            json.WriteEndArray();
            json.WriteEndObject();
            json.WriteEndObject();
        }
        // One line ending on every system, so the same atlas is the same file wherever it is packed.
        return Encoding.UTF8.GetString(stream.ToArray()).ReplaceLineEndings("\n") + "\n";
    }

    private static void Rect(Utf8JsonWriter json, string name, int x, int y, int w, int h)
    {
        json.WriteStartObject(name);
        json.WriteNumber("x", x);
        json.WriteNumber("y", y);
        json.WriteNumber("w", w);
        json.WriteNumber("h", h);
        json.WriteEndObject();
    }
}

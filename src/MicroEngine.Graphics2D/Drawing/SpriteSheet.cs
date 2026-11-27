using System.Numerics;
using System.Text.Json;

namespace MicroEngine.Graphics;

/// <summary>One frame of a sprite sheet as pixels, before it is packed into an atlas.</summary>
/// <param name="Name">The sprite's name in the atlas ("drone0", "prop_palm").</param>
/// <param name="Image">Its pixels.</param>
/// <param name="Origin">Its anchor in its own pixels (see <see cref="Sprite.Origin"/>).</param>
public sealed record SheetFrame(string Name, PixelImage Image, Vector2 Origin);

/// <summary>A named animation of a sprite sheet: frames by name, how long each shows, and how they play.</summary>
/// <param name="Name">The clip's name (<see cref="SpriteAtlas.Clip(string, bool?)"/>).</param>
/// <param name="Frames">The frames' names, in forward order.</param>
/// <param name="Durations">Seconds each frame shows, one per frame; null for a fixed rate (<paramref name="FramesPerSecond"/>).</param>
/// <param name="FramesPerSecond">The rate of a clip without <paramref name="Durations"/>.</param>
/// <param name="Direction">The order the frames play in.</param>
/// <param name="Loop">Whether it starts over after its last frame.</param>
public sealed record SheetClip(string Name, IReadOnlyList<string> Frames, IReadOnlyList<float>? Durations, float FramesPerSecond,
    AnimationDirection Direction = AnimationDirection.Forward, bool Loop = true);

/// <summary>
/// Frames and clips read from a description of sprite sheets, as pixels: what an artist's tool or a <c>.sheet</c> file
/// says about one or more images (cell size, named strips, pivots, animation timing), ready to be packed into a
/// <see cref="SpriteAtlas"/> with <see cref="SpriteAtlasBuilder.AddSheet"/>. <c>Assets.Atlas("art/enemies.sheet")</c>
/// does both steps; <c>Assets.Sheet(path)</c> gives the frames to change or to pack with others.
/// </summary>
/// <remarks>
/// <para>A <c>.sheet</c> file is JSON (comments and trailing commas allowed):</para>
/// <code>
/// {
///   "pivot": [0.5, 1],                                  // anchor of every frame, as a fraction of it (or "origin": [x, y] in pixels)
///   "sheets": [
///     { "image": "drone.png", "name": "drone", "cell": [55, 52], "origin": [27, 26], "fps": 12 },
///     { "image": "palm.png", "name": "prop_palm" },     // no cell: the whole image is one sprite
///     { "image": "tiles.png", "cell": [16, 16], "margin": 0, "spacing": 0,
///       "strips": { "walk": { "from": 8, "count": 6, "durations": [100, 100, 200, 100, 100, 200], "direction": "pingpong", "loop": false },
///                   "ledge": { "at": [6, 11], "count": 4 },
///                   "fill": { "cells": [[4, 1], [1, 1]] },
///                   "odd": { "rects": [[504, 80, 16, 16]] } },
///       "sprites": { "gem": [2, 3], "logo": [0, 64, 48, 16], "key": { "cell": [3, 1], "origin": [8, 15] } } }
///   ],
///   "clips": { "hop": { "frames": ["drone1", "drone3", "drone1"], "fps": 6, "loop": false },
///              "patrol": { "strip": "drone", "fps": 8 } }
/// }
/// </code>
/// <para>
/// An image with a <c>name</c> and a <c>cell</c> is one strip: frames "name0", "name1"... row by row, without the fully
/// transparent cells at its end unless <c>count</c> says how many. <c>strips</c> name runs of cells of one image
/// (<c>from</c> a cell index or <c>at</c> a column and row, <c>cells</c>, or <c>rects</c> in pixels); <c>sprites</c> name
/// single cells (two numbers) or rectangles (four). A strip with <c>fps</c> or <c>durations</c> (milliseconds per frame)
/// is also a clip of that name; <c>clips</c> name more of them, from frames listed by name or a whole strip.
/// <c>direction</c> is forward, reverse, pingpong or pingpong_reverse. Image paths are relative to the file.
/// </para>
/// </remarks>
public sealed class SpriteSheet
{
    /// <summary>The frames, in the description's order.</summary>
    public IReadOnlyList<SheetFrame> Frames { get; }
    /// <summary>The clips, in the description's order.</summary>
    public IReadOnlyList<SheetClip> Clips { get; }
    /// <summary>
    /// The names of the strips the description declares, in its order: runs of frames "name0", "name1"... that
    /// <see cref="SpriteAtlas.Frames"/> takes. Empty for a sheet whose source declares none (an Aseprite file, a
    /// packing tool's atlas, a sheet made in code).
    /// </summary>
    public IReadOnlyList<string> Strips { get; init; } = [];

    /// <summary>A sheet of <paramref name="frames"/> and <paramref name="clips"/> (a reader of another format makes one).</summary>
    /// <exception cref="InvalidDataException">Two frames or two clips share a name, a clip names a frame that is missing, or its timing does not fit its frames.</exception>
    public SpriteSheet(IEnumerable<SheetFrame> frames, IEnumerable<SheetClip>? clips = null)
    {
        ArgumentNullException.ThrowIfNull(frames);
        Frames = [.. frames];
        Clips = clips is null ? [] : [.. clips];
        if (Frames.Count == 0) throw new InvalidDataException("The sheet has no frames.");
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var frame in Frames)
        {
            if (string.IsNullOrWhiteSpace(frame.Name)) throw new InvalidDataException("A frame has no name.");
            if (!names.Add(frame.Name)) throw new InvalidDataException($"The sheet has two frames named '{frame.Name}'.");
        }
        var clipNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var clip in Clips)
        {
            if (!clipNames.Add(clip.Name)) throw new InvalidDataException($"The sheet has two clips named '{clip.Name}'.");
            if (clip.Frames.Count == 0) throw new InvalidDataException($"Clip '{clip.Name}' has no frames.");
            foreach (var frame in clip.Frames)
                if (!names.Contains(frame)) throw new InvalidDataException($"Clip '{clip.Name}' names the frame '{frame}', which the sheet does not have.");
            if (clip.Durations is { } durations)
            {
                if (durations.Count != clip.Frames.Count)
                    throw new InvalidDataException($"Clip '{clip.Name}' has {clip.Frames.Count} frames and {durations.Count} durations; give one per frame.");
                foreach (float d in durations)
                    if (!(d > 0) || float.IsInfinity(d)) throw new InvalidDataException($"Clip '{clip.Name}' has a duration of {d * 1000} ms; durations are positive.");
            }
            else if (!(clip.FramesPerSecond > 0) || float.IsInfinity(clip.FramesPerSecond))
                throw new InvalidDataException($"Clip '{clip.Name}' plays at {clip.FramesPerSecond} frames per second; the rate is positive.");
        }
    }

    /// <summary>Reads a <c>.sheet</c> file and the images it names, next to it.</summary>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="InvalidDataException">The description is wrong or an image is missing; the message names the file.</exception>
    public static SpriteSheet Load(string path, AtlasOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string folder = Path.GetDirectoryName(path) ?? "";
        try { return Parse(File.ReadAllText(path), image => PixelImage.Load(Path.Combine(folder, image)), options); }
        catch (InvalidDataException e) { throw new InvalidDataException($"{path}: {e.Message}", e); }
    }

    /// <summary>Reads the text of a <c>.sheet</c> file.</summary>
    /// <param name="text">The file's text.</param>
    /// <param name="image">Gives the image the description names (a path relative to the file), each asked for once.</param>
    /// <param name="options">The pivot of frames the description gives none; null for <see cref="AtlasOptions.Default"/>.</param>
    /// <exception cref="InvalidDataException">The text is not a sheet description, names an image <paramref name="image"/> cannot give, or places a frame outside its image; the message says which.</exception>
    public static SpriteSheet Parse(string text, Func<string, PixelImage> image, AtlasOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(image);
        JsonDocument document;
        try { document = JsonDocument.Parse(text, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip }); }
        catch (JsonException e) { throw new InvalidDataException($"Invalid JSON: {e.Message}", e); }
        using (document)
            return new Reader(image, options ?? AtlasOptions.Default).Read(document.RootElement);
    }

    /// <summary>The anchor of a frame: pixels (<c>origin</c>) or a fraction of it (<c>pivot</c>).</summary>
    private readonly record struct Anchor(Vector2 Value, bool Fraction)
    {
        public Vector2 Of(int width, int height) => Fraction ? Value * new Vector2(width, height) : Value;
    }

    private sealed class Reader(Func<string, PixelImage> open, AtlasOptions options)
    {
        private readonly Dictionary<string, PixelImage> _images = new(StringComparer.Ordinal);
        private readonly List<SheetFrame> _frames = [];
        private readonly List<SheetClip> _clips = [];
        private readonly HashSet<string> _names = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string[]> _strips = new(StringComparer.Ordinal);

        public SpriteSheet Read(JsonElement root)
        {
            if (root.ValueKind != JsonValueKind.Object) throw new InvalidDataException("A sheet description is a JSON object with \"sheets\".");
            Known(root, "the file", "pivot", "origin", "sheets", "clips");
            var anchor = AnchorOf(root, "the file") ?? new Anchor(options.Pivot, true);
            if (!root.TryGetProperty("sheets", out var sheets) || sheets.ValueKind != JsonValueKind.Array || sheets.GetArrayLength() == 0)
                throw new InvalidDataException("The description has no \"sheets\": [ { \"image\": ... } ].");
            foreach (var sheet in sheets.EnumerateArray()) ReadSheet(sheet, anchor);
            if (root.TryGetProperty("clips", out var clips))
            {
                if (clips.ValueKind != JsonValueKind.Object) throw new InvalidDataException("\"clips\" is an object of named clips.");
                foreach (var clip in clips.EnumerateObject()) ReadClip(clip.Name, clip.Value);
            }
            return new SpriteSheet(_frames, _clips) { Strips = [.. _strips.Keys] };
        }

        private void ReadSheet(JsonElement sheet, Anchor inherited)
        {
            if (sheet.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Each entry of \"sheets\" is an object with an \"image\".");
            string file = Text(sheet, "image") ?? throw new InvalidDataException("An entry of \"sheets\" has no \"image\".");
            string where = $"sheet '{file}'";
            Known(sheet, where, "image", "name", "cell", "margin", "spacing", "count", "pivot", "origin", "fps", "durations", "direction", "loop", "strips", "sprites");
            var image = Image(file);
            var anchor = AnchorOf(sheet, where) ?? inherited;
            var grid = sheet.TryGetProperty("cell", out var cell) ? new Grid(image, file, Pair(cell, where, "cell"), PairOrOne(sheet, "margin", where), PairOrOne(sheet, "spacing", where)) : null;
            bool any = false;
            if (Text(sheet, "name") is { } name)
            {
                any = true;
                if (grid is null)
                {
                    foreach (var key in new[] { "count", "fps", "durations", "direction", "loop" })
                        if (sheet.TryGetProperty(key, out _)) throw new InvalidDataException($"{where} has \"{key}\" but no \"cell\": without a cell size the whole image is the one sprite '{name}'.");
                    Add(name, image.Clone(), anchor);
                }
                else
                {
                    int? count = sheet.TryGetProperty("count", out var c) ? Whole(c, where, "count") : null;
                    var cells = grid.Cells(0, Math.Min(count ?? grid.Total, grid.Total), where);
                    // The same rule as SpriteAtlasBuilder.AddGrid: without a count, fully transparent cells at the end are not frames.
                    if (count is null)
                        while (cells.Count > 1 && cells[^1].OpaqueBounds() is null) cells.RemoveAt(cells.Count - 1);
                    AddStrip(name, cells, anchor, sheet, where);
                }
            }
            if (sheet.TryGetProperty("strips", out var strips))
            {
                any = true;
                if (strips.ValueKind != JsonValueKind.Object) throw new InvalidDataException($"\"strips\" of {where} is an object of named strips.");
                foreach (var strip in strips.EnumerateObject()) ReadStrip(strip.Name, strip.Value, image, file, grid, anchor);
            }
            if (sheet.TryGetProperty("sprites", out var sprites))
            {
                any = true;
                if (sprites.ValueKind != JsonValueKind.Object) throw new InvalidDataException($"\"sprites\" of {where} is an object of named sprites.");
                foreach (var sprite in sprites.EnumerateObject()) ReadSprite(sprite.Name, sprite.Value, image, file, grid, anchor);
            }
            if (!any) throw new InvalidDataException($"{where} names nothing: give it a \"name\", \"strips\" or \"sprites\".");
        }

        private void ReadStrip(string name, JsonElement strip, PixelImage image, string file, Grid? grid, Anchor inherited)
        {
            string where = $"strip '{name}' of '{file}'";
            if (strip.ValueKind != JsonValueKind.Object) throw new InvalidDataException($"{where} is an object: {{ \"from\": 0, \"count\": 4 }}.");
            Known(strip, where, "from", "at", "count", "cells", "rects", "pivot", "origin", "fps", "durations", "direction", "loop");
            var anchor = AnchorOf(strip, where) ?? inherited;
            List<PixelImage> frames = [];
            if (strip.TryGetProperty("rects", out var rects))
            {
                if (rects.ValueKind != JsonValueKind.Array) throw new InvalidDataException($"\"rects\" of {where} is an array of [x, y, width, height].");
                foreach (var rect in rects.EnumerateArray()) frames.Add(Cut(image, file, Numbers(rect, 4, where, "rects"), where));
            }
            else
            {
                var cells = grid ?? throw new InvalidDataException($"{where} counts cells, so its sheet needs a \"cell\": [width, height] (or give the strip \"rects\" in pixels).");
                if (strip.TryGetProperty("cells", out var list))
                {
                    if (list.ValueKind != JsonValueKind.Array) throw new InvalidDataException($"\"cells\" of {where} is an array of [column, row].");
                    foreach (var at in list.EnumerateArray())
                    {
                        var (column, row) = Pair(at, where, "cells");
                        frames.Add(cells.Cell(column, row, where));
                    }
                }
                else
                {
                    int from = 0;
                    if (strip.TryGetProperty("at", out var at))
                    {
                        var (column, row) = Pair(at, where, "at");
                        if (column < 0 || column >= cells.Columns) throw new InvalidDataException($"{where} starts at column {column}, but '{file}' has {cells.Columns}.");
                        from = row * cells.Columns + column;
                    }
                    else if (strip.TryGetProperty("from", out var f)) from = Whole(f, where, "from");
                    // Without a count, a strip runs to the end of its row.
                    int count = strip.TryGetProperty("count", out var c) ? Whole(c, where, "count") : cells.Columns - from % cells.Columns;
                    if (count <= 0) throw new InvalidDataException($"{where} has a count of {count}; a strip has at least one frame.");
                    frames = cells.Cells(from, count, where);
                }
            }
            if (frames.Count == 0) throw new InvalidDataException($"{where} has no frames.");
            AddStrip(name, frames, anchor, strip, where);
        }

        private void ReadSprite(string name, JsonElement sprite, PixelImage image, string file, Grid? grid, Anchor inherited)
        {
            string where = $"sprite '{name}' of '{file}'";
            var anchor = inherited;
            var place = sprite;
            if (sprite.ValueKind == JsonValueKind.Object)
            {
                Known(sprite, where, "cell", "rect", "pivot", "origin");
                anchor = AnchorOf(sprite, where) ?? inherited;
                if (!sprite.TryGetProperty("rect", out place) && !sprite.TryGetProperty("cell", out place))
                    throw new InvalidDataException($"{where} needs a \"cell\": [column, row] or a \"rect\": [x, y, width, height].");
            }
            if (place.ValueKind != JsonValueKind.Array || place.GetArrayLength() is not (2 or 4))
                throw new InvalidDataException($"{where} is [column, row], [x, y, width, height] or an object with \"cell\" or \"rect\".");
            if (place.GetArrayLength() == 4) Add(name, Cut(image, file, Numbers(place, 4, where, "rect"), where), anchor);
            else
            {
                var cells = grid ?? throw new InvalidDataException($"{where} names a cell, so its sheet needs a \"cell\": [width, height] (or give four numbers: x, y, width, height).");
                var (column, row) = Pair(place, where, "cell");
                Add(name, cells.Cell(column, row, where), anchor);
            }
        }

        /// <summary>A clip of frames named elsewhere in the file: a list of names (a frame may repeat) or a whole strip.</summary>
        private void ReadClip(string name, JsonElement clip)
        {
            string where = $"clip '{name}'";
            if (clip.ValueKind != JsonValueKind.Object) throw new InvalidDataException($"{where} is an object: {{ \"frames\": [\"hero0\", \"hero1\"], \"fps\": 8 }}.");
            Known(clip, where, "frames", "strip", "fps", "durations", "direction", "loop");
            bool hasFrames = clip.TryGetProperty("frames", out var frames);
            string? strip = Text(clip, "strip");
            if (hasFrames == (strip is not null)) throw new InvalidDataException($"{where} needs either \"frames\": [names] or \"strip\": the name of a strip.");
            string[] names;
            if (strip is not null)
                names = _strips.TryGetValue(strip, out var of) ? of : throw new InvalidDataException($"{where} plays the strip '{strip}', which the file does not have (strips: {(_strips.Count == 0 ? "none" : string.Join(", ", _strips.Keys))}).");
            else
            {
                if (frames.ValueKind != JsonValueKind.Array) throw new InvalidDataException($"\"frames\" of {where} is an array of frame names.");
                names = [.. frames.EnumerateArray().Select(f => f.ValueKind == JsonValueKind.String ? f.GetString()! : throw new InvalidDataException($"\"frames\" of {where} holds something that is not a name."))];
                if (names.Length == 0) throw new InvalidDataException($"{where} has no frames.");
                foreach (var frame in names)
                    if (!_names.Contains(frame)) throw new InvalidDataException($"{where} names the frame '{frame}', which the file does not have.");
            }
            if (!AddClip(name, names, clip, where)) throw new InvalidDataException($"{where} needs \"fps\" or \"durations\".");
        }

        /// <summary>Adds "name0", "name1"... and, when <paramref name="timing"/> has a rate or durations, the clip "name".</summary>
        private void AddStrip(string name, List<PixelImage> frames, Anchor anchor, JsonElement timing, string where)
        {
            var names = new string[frames.Count];
            for (int i = 0; i < frames.Count; i++) Add(names[i] = name + i, frames[i], anchor);
            if (!_strips.TryAdd(name, names)) throw new InvalidDataException($"The sheet has two strips named '{name}'.");
            if (AddClip(name, names, timing, where)) return;
            foreach (var key in new[] { "direction", "loop" })
                if (timing.TryGetProperty(key, out _)) throw new InvalidDataException($"{where} has \"{key}\" but no \"fps\" or \"durations\": only a strip with timing is a clip.");
        }

        /// <summary>Adds the clip <paramref name="name"/> of <paramref name="names"/> when <paramref name="timing"/> has a rate or durations; false when it has neither.</summary>
        private bool AddClip(string name, string[] names, JsonElement timing, string where)
        {
            bool hasFps = timing.TryGetProperty("fps", out var fps), hasDurations = timing.TryGetProperty("durations", out var durations);
            if (!hasFps && !hasDurations) return false;
            if (hasFps && hasDurations) throw new InvalidDataException($"{where} has both \"fps\" and \"durations\"; give one.");
            float[]? seconds = null;
            float rate = 0;
            if (hasDurations)
            {
                if (durations.ValueKind != JsonValueKind.Array) throw new InvalidDataException($"\"durations\" of {where} is an array of milliseconds, one per frame.");
                seconds = [.. durations.EnumerateArray().Select(d => Number(d, where, "durations") / 1000f)];
                if (seconds.Length != names.Length)
                    throw new InvalidDataException($"{where} has {names.Length} frames and {seconds.Length} durations; give one per frame.");
            }
            else rate = Number(fps, where, "fps");
            var direction = (Text(timing, "direction") ?? "forward").ToLowerInvariant() switch
            {
                "forward" => AnimationDirection.Forward,
                "reverse" => AnimationDirection.Reverse,
                "pingpong" => AnimationDirection.PingPong,
                "pingpong_reverse" => AnimationDirection.PingPongReverse,
                var other => throw new InvalidDataException($"{where} has an unknown direction '{other}' (forward, reverse, pingpong, pingpong_reverse)."),
            };
            bool loop = !timing.TryGetProperty("loop", out var l) || l.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => throw new InvalidDataException($"\"loop\" of {where} is true or false."),
            };
            if (_clips.Any(c => c.Name == name)) throw new InvalidDataException($"The sheet has two clips named '{name}'.");
            _clips.Add(new SheetClip(name, names, seconds, rate, direction, loop));
            return true;
        }

        private void Add(string name, PixelImage frame, Anchor anchor)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new InvalidDataException("A frame has no name.");
            if (!_names.Add(name)) throw new InvalidDataException($"The sheet has two frames named '{name}'.");
            _frames.Add(new SheetFrame(name, frame, anchor.Of(frame.Width, frame.Height)));
        }

        private PixelImage Image(string file)
        {
            if (_images.TryGetValue(file, out var image)) return image;
            try { return _images[file] = open(file) ?? throw new InvalidDataException("no image was given."); }
            catch (Exception e) when (e is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                throw new InvalidDataException($"The image '{file}' cannot be read: {e.Message}", e);
            }
        }

        private static PixelImage Cut(PixelImage image, string file, int[] r, string where)
        {
            if (r[2] <= 0 || r[3] <= 0) throw new InvalidDataException($"{where} has an empty rectangle ({r[0]}, {r[1]}, {r[2]}, {r[3]}).");
            if (r[0] < 0 || r[1] < 0 || r[0] + r[2] > image.Width || r[1] + r[3] > image.Height)
                throw new InvalidDataException($"{where}: the rectangle ({r[0]}, {r[1]}, {r[2]}, {r[3]}) lies outside the {image.Width}x{image.Height} image '{file}'.");
            return image.Crop(r[0], r[1], r[2], r[3]);
        }

        private static Anchor? AnchorOf(JsonElement element, string where)
        {
            bool pivot = element.TryGetProperty("pivot", out var p), origin = element.TryGetProperty("origin", out var o);
            if (pivot && origin) throw new InvalidDataException($"{where} has both \"pivot\" (a fraction of the frame) and \"origin\" (pixels); give one.");
            if (!pivot && !origin) return null;
            var value = pivot ? p : o;
            string key = pivot ? "pivot" : "origin";
            if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() != 2) throw new InvalidDataException($"\"{key}\" of {where} is [x, y].");
            return new Anchor(new Vector2(Number(value[0], where, key), Number(value[1], where, key)), pivot);
        }

        private static void Known(JsonElement element, string where, params string[] known)
        {
            foreach (var property in element.EnumerateObject())
                if (Array.IndexOf(known, property.Name) < 0)
                    throw new InvalidDataException($"{where} has an unknown property \"{property.Name}\" (known: {string.Join(", ", known)}).");
        }

        private static string? Text(JsonElement element, string name) =>
            element.TryGetProperty(name, out var v) ? v.ValueKind == JsonValueKind.String ? v.GetString() : throw new InvalidDataException($"\"{name}\" is text.") : null;

        private static float Number(JsonElement value, string where, string key) =>
            value.ValueKind == JsonValueKind.Number ? value.GetSingle() : throw new InvalidDataException($"\"{key}\" of {where} holds something that is not a number.");

        private static int Whole(JsonElement value, string where, string key) =>
            value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int n) ? n : throw new InvalidDataException($"\"{key}\" of {where} is a whole number.");

        private static int[] Numbers(JsonElement value, int count, string where, string key)
        {
            if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() != count)
                throw new InvalidDataException($"\"{key}\" of {where} takes {count} whole numbers.");
            var numbers = new int[count];
            for (int i = 0; i < count; i++) numbers[i] = Whole(value[i], where, key);
            return numbers;
        }

        private static (int X, int Y) Pair(JsonElement value, string where, string key)
        {
            var n = Numbers(value, 2, where, key);
            return (n[0], n[1]);
        }

        /// <summary>A margin or spacing: one number for both axes, or [x, y]; 0 when absent.</summary>
        private static (int X, int Y) PairOrOne(JsonElement element, string key, string where)
        {
            if (!element.TryGetProperty(key, out var value)) return (0, 0);
            if (value.ValueKind == JsonValueKind.Number) { int n = Whole(value, where, key); return (n, n); }
            return Pair(value, where, key);
        }
    }

    /// <summary>The cells of an image: a size, a margin around them all and a gap between them.</summary>
    private sealed class Grid
    {
        private readonly PixelImage _image;
        private readonly string _file;
        private readonly (int X, int Y) _cell, _margin, _spacing;

        public Grid(PixelImage image, string file, (int X, int Y) cell, (int X, int Y) margin, (int X, int Y) spacing)
        {
            (_image, _file, _cell, _margin, _spacing) = (image, file, cell, margin, spacing);
            if (cell.X <= 0 || cell.Y <= 0) throw new InvalidDataException($"\"cell\" of sheet '{file}' is [{cell.X}, {cell.Y}]; a cell has a positive size.");
            if (margin.X < 0 || margin.Y < 0 || spacing.X < 0 || spacing.Y < 0) throw new InvalidDataException($"\"margin\" and \"spacing\" of sheet '{file}' are 0 or more.");
            Columns = (image.Width - 2 * margin.X + spacing.X) / (cell.X + spacing.X);
            Rows = (image.Height - 2 * margin.Y + spacing.Y) / (cell.Y + spacing.Y);
            if (Columns <= 0 || Rows <= 0)
                throw new InvalidDataException($"The {image.Width}x{image.Height} image '{file}' is smaller than one {cell.X}x{cell.Y} cell.");
        }

        public int Columns { get; }
        public int Rows { get; }
        public int Total => Columns * Rows;

        public PixelImage Cell(int column, int row, string where)
        {
            if (column < 0 || row < 0 || column >= Columns || row >= Rows)
                throw new InvalidDataException($"{where}: the cell at column {column}, row {row} lies outside the {_image.Width}x{_image.Height} image '{_file}' ({Columns} columns, {Rows} rows of {_cell.X}x{_cell.Y}).");
            return _image.Crop(_margin.X + column * (_cell.X + _spacing.X), _margin.Y + row * (_cell.Y + _spacing.Y), _cell.X, _cell.Y);
        }

        public List<PixelImage> Cells(int from, int count, string where)
        {
            if (from < 0) throw new InvalidDataException($"{where} starts at cell {from}; cells count from 0.");
            var cells = new List<PixelImage>(count);
            for (int i = from; i < from + count; i++) cells.Add(Cell(i % Columns, i / Columns, where));
            return cells;
        }
    }
}

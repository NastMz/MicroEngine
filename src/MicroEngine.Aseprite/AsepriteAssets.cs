using System.Numerics;
using AsepriteDotNet.Aseprite;
using AsepriteDotNet.Aseprite.Types;
using AsepriteDotNet.IO;
using MicroEngine.Content;
using MicroEngine.Core;
using MicroEngine.Graphics;

namespace MicroEngine.Aseprite;

/// <summary>
/// Aseprite files (<c>.aseprite</c>, <c>.ase</c>) as sprite sheets, read as the artist saved them: no export step. Each
/// frame is the picture Aseprite shows (visible layers flattened with their blend modes and opacity), named
/// "<c>file</c> 0", "<c>file</c> 1"... like Aseprite's own sheet export; each frame tag is a clip with every frame's
/// duration, its direction and whether it repeats; each slice is a sprite of its own, named after it.
/// </summary>
/// <remarks>
/// <para>
/// Frames are anchored at the pivot of the file's first slice that has one (the slice's corner plus its pivot, at
/// each frame's key), so the artist sets a character's feet once in Aseprite; a file without such a slice uses the
/// caller's <see cref="AtlasOptions.Pivot"/>. A slice's own sprite is anchored at its pivot the same way.
/// </para>
/// <para>
/// Register the format once (<see cref="AddAseprite"/>, or just call <see cref="Aseprite"/>) and the file is an atlas
/// like any other: <c>Assets.Atlas("hero.aseprite")</c>, <c>Assets.Sheet</c>, <c>Assets.Preload</c> and the scopes work
/// on it, from folders, archives and browser packages alike.
/// </para>
/// </remarks>
public static class AsepriteAssets
{
    /// <summary>The asset kind of the frames and clips of an Aseprite file (a <see cref="SpriteSheet"/>).</summary>
    public const string Kind = "aseprite";

    /// <summary>The loader of the <c>aseprite</c> kind (extensions <c>.aseprite</c> and <c>.ase</c>).</summary>
    public static IAssetLoader<SpriteSheet> Loader { get; } = new AssetLoader<SpriteSheet>(Kind,
        c => Read(Path.GetFileNameWithoutExtension(c.Path), c.ReadBytes(), c.Options as AtlasOptions), [".aseprite", ".ase"]);

    /// <summary>Registers the <c>aseprite</c> kind on <paramref name="loaders"/>, so <c>Assets.Atlas</c> and <c>Assets.Sheet</c> read Aseprite files.</summary>
    /// <returns><paramref name="loaders"/>, for chaining.</returns>
    public static AssetLoaderRegistry AddAseprite(this AssetLoaderRegistry loaders)
    {
        ArgumentNullException.ThrowIfNull(loaders);
        return loaders.Add(Loader);
    }

    /// <summary>
    /// The Aseprite file at <paramref name="path"/> as an atlas with its clips, loaded once and shared like any asset
    /// (the format is registered on first use; afterwards <c>Assets.Atlas(path)</c> gives the same object).
    /// </summary>
    /// <param name="assets">The game's assets (<c>Game.Assets</c>) or one of its scopes.</param>
    /// <param name="path">The file's content path.</param>
    /// <param name="options">The pivot of frames when the file has no slice with one; null for <see cref="AtlasOptions.Default"/>.</param>
    /// <param name="filter">The texture's filter; null for the game's default.</param>
    /// <exception cref="AssetLoadException">The file is missing, wrongly cased or not a file Aseprite saves.</exception>
    public static SpriteAtlas Aseprite(this AssetCache assets, string path, AtlasOptions? options = null, TextureFilter? filter = null)
    {
        ArgumentNullException.ThrowIfNull(assets);
        if (!assets.Loaders.TryGet(Kind, out _)) assets.Loaders.AddAseprite();
        return assets.Atlas(path, options, filter);
    }

    /// <summary>Reads the bytes of an Aseprite file, without a graphics device or a content source: for tools and tests.</summary>
    /// <param name="name">The name frames take ("hero" gives "hero 0", "hero 1"...): the file's name without its extension.</param>
    /// <param name="bytes">The file.</param>
    /// <param name="options">The pivot of frames when the file has no slice with one; null for <see cref="AtlasOptions.Default"/>.</param>
    /// <exception cref="InvalidDataException">The bytes are not a file Aseprite saves; the message says what is wrong.</exception>
    public static SpriteSheet Read(string name, byte[] bytes, AtlasOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(bytes);
        options ??= AtlasOptions.Default;
        AsepriteFile file;
        try
        {
            using var stream = new MemoryStream(bytes, writable: false);
            file = AsepriteFileLoader.FromStream(name, stream, leaveOpen: true, preMultiplyAlpha: false);
        }
        catch (Exception e) when (e is InvalidOperationException or EndOfStreamException or IOException or ArgumentException or NotSupportedException or IndexOutOfRangeException or InvalidDataException)
        {
            throw new InvalidDataException($"Not a file Aseprite saves: {e.Message}", e);
        }
        int width = file.CanvasWidth, height = file.CanvasHeight;
        if (file.FrameCount == 0 || width <= 0 || height <= 0) throw new InvalidDataException("The Aseprite file has no frames.");

        // The first slice with a pivot anchors every frame: the artist marks the feet once.
        AsepriteSlice? anchor = null;
        foreach (var slice in file.Slices)
            if (slice.HasPivot && slice.Keys.Length > 0) { anchor = slice; break; }

        var images = new PixelImage[file.FrameCount];
        var names = new string[file.FrameCount];
        var frames = new List<SheetFrame>(file.FrameCount + file.Slices.Length);
        for (int i = 0; i < file.FrameCount; i++)
        {
            var pixels = file.Frames[i].FlattenFrame(onlyVisibleLayers: true, includeBackgroundLayer: true, includeTilemapCels: true);
            var image = images[i] = new PixelImage(width, height);
            for (int p = 0; p < pixels.Length; p++)
                image[p % width, p / width] = new Color(pixels[p].R, pixels[p].G, pixels[p].B, pixels[p].A);
            var origin = options.Pivot * new Vector2(width, height);
            if (anchor is not null)
            {
                var key = KeyAt(anchor, i);
                origin = new Vector2(key.Bounds.X + key.Pivot.X, key.Bounds.Y + key.Pivot.Y);
            }
            frames.Add(new SheetFrame(names[i] = $"{name} {i}", image, origin));
        }

        var taken = new HashSet<string>(names, StringComparer.Ordinal);
        foreach (var slice in file.Slices)
        {
            if (slice.Keys.Length == 0) continue;
            var key = slice.Keys[0];
            var b = key.Bounds;
            if (b.Width <= 0 || b.Height <= 0 || b.X < 0 || b.Y < 0 || b.X + b.Width > width || b.Y + b.Height > height || key.FrameIndex < 0 || key.FrameIndex >= images.Length)
                throw new InvalidDataException($"The slice '{slice.Name}' ({b.X}, {b.Y}, {b.Width}, {b.Height}) lies outside the {width}x{height} canvas.");
            if (!taken.Add(slice.Name)) throw new InvalidDataException($"The file has two slices named '{slice.Name}' (or a slice named like a frame); a slice is a sprite of that name.");
            var origin = slice.HasPivot ? new Vector2(key.Pivot.X, key.Pivot.Y) : options.Pivot * new Vector2(b.Width, b.Height);
            frames.Add(new SheetFrame(slice.Name, images[key.FrameIndex].Crop(b.X, b.Y, b.Width, b.Height), origin));
        }

        var clips = new List<SheetClip>(file.Tags.Length);
        var clipNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var tag in file.Tags)
        {
            if (tag.From < 0 || tag.To < tag.From || tag.To >= file.FrameCount)
                throw new InvalidDataException($"The tag '{tag.Name}' covers frames {tag.From}-{tag.To}, but the file has {file.FrameCount}.");
            if (!clipNames.Add(tag.Name)) throw new InvalidDataException($"The file has two tags named '{tag.Name}'; a tag is a clip of that name.");
            int count = tag.To - tag.From + 1;
            var durations = new float[count];
            for (int i = 0; i < count; i++)
            {
                float seconds = (float)file.Frames[tag.From + i].Duration.TotalSeconds;
                durations[i] = seconds > 0 ? seconds : SpriteAtlas.DefaultFrameDuration;
            }
            var direction = (int)tag.LoopDirection switch
            {
                1 => AnimationDirection.Reverse,
                2 => AnimationDirection.PingPong,
                3 => AnimationDirection.PingPongReverse,
                _ => AnimationDirection.Forward,
            };
            // Aseprite's "repeat": 0 plays for ever, a number plays that many times and stops.
            clips.Add(new SheetClip(tag.Name, new ArraySegment<string>(names, tag.From, count), durations, 0, direction, Loop: tag.Repeat == 0));
        }
        return new SpriteSheet(frames, clips);
    }

    /// <summary>The key of <paramref name="slice"/> in force at <paramref name="frame"/>: the last one at or before it, or its first.</summary>
    private static AsepriteSliceKey KeyAt(AsepriteSlice slice, int frame)
    {
        var found = slice.Keys[0];
        foreach (var key in slice.Keys)
            if (key.FrameIndex <= frame && key.FrameIndex >= found.FrameIndex) found = key;
        return found;
    }
}

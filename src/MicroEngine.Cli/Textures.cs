using BCnEncoder.Encoder;
using BCnEncoder.Shared;
using MicroEngine.Graphics;
using MicroEngine.Graphics.Devices;

namespace MicroEngine.Cli;

/// <summary>
/// `microengine textures`: converts image files ahead of time into the KTX 2.0 files (<see cref="TextureFile"/>) a
/// game may ship beside or instead of them. Every image under the source folder becomes a <c>.ktx2</c> file at the same
/// place under the output folder, its colour multiplied by its alpha as the device draws it, either as BC7 blocks
/// (a quarter of the memory on a device that samples them; close to the image, not equal) or as the image's own
/// pixels (exactly the image, on every device), each deflated. The files load without decoding, on every platform.
/// </summary>
public static class Textures
{
    /// <summary>What converting one folder did.</summary>
    /// <param name="Written">Files written (or, with check, files that are missing or out of date).</param>
    /// <param name="Unchanged">Files already up to date.</param>
    public readonly record struct BuildResult(IReadOnlyList<string> Written, IReadOnlyList<string> Unchanged);

    private static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg", ".bmp", ".tga", ".gif", ".psd"];

    /// <summary>
    /// Converts every image under <paramref name="source"/> (its subfolders included) into <paramref name="output"/>.
    /// A file is up to date when it exists, is not older than its image and already has the format asked for. With
    /// <paramref name="check"/> nothing is written and <see cref="BuildResult.Written"/> lists the files that are not.
    /// </summary>
    /// <param name="source">The folder of images.</param>
    /// <param name="output">The folder the <c>.ktx2</c> files go to.</param>
    /// <param name="format">"bc7" or "rgba".</param>
    /// <param name="quality">"fast", "balanced" or "best": how long the block encoder searches (bc7 only).</param>
    /// <param name="check">Write nothing; list what is missing or out of date.</param>
    /// <exception cref="CliException">No folder, no image, an unknown format or quality, an image that cannot be read, or one whose size BC7 cannot hold.</exception>
    public static BuildResult Convert(string source, string output, string format, string quality, bool check)
    {
        var target = format switch
        {
            "bc7" => TextureFormat.Bc7,
            "rgba" => TextureFormat.Rgba8Unorm,
            _ => throw new CliException($"--format is bc7 or rgba, not '{format}'.", 2),
        };
        var effort = quality switch
        {
            "fast" => CompressionQuality.Fast,
            "balanced" => CompressionQuality.Balanced,
            "best" => CompressionQuality.BestQuality,
            _ => throw new CliException($"--quality is fast, balanced or best, not '{quality}'.", 2),
        };
        if (!Directory.Exists(source)) throw new CliException($"no texture folder {source}.", 2);
        var files = Directory.GetFiles(source, "*", SearchOption.AllDirectories)
            .Where(f => ImageExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase)).Order(StringComparer.Ordinal).ToList();
        if (files.Count == 0) throw new CliException($"no image ({string.Join(", ", ImageExtensions)}) in {source}.", 2);
        string ResultOf(string file) => Path.Combine(output, Path.ChangeExtension(Path.GetRelativePath(source, file), TextureFile.FileExtension));
        var shared = files.GroupBy(ResultOf, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
        if (shared is not null)
            throw new CliException($"{string.Join(" and ", shared.Select(f => Path.GetRelativePath(source, f)))} would both become {Path.GetRelativePath(output, shared.Key)}. Rename one.", 2);
        var written = new List<string>();
        var unchanged = new List<string>();
        foreach (var file in files)
        {
            string result = ResultOf(file);
            if (UpToDate(file, result, target)) { unchanged.Add(result); continue; }
            written.Add(result);
            if (check) continue;
            var texture = ConvertFile(file, target, effort);
            Directory.CreateDirectory(Path.GetDirectoryName(result)!);
            File.WriteAllBytes(result, texture.ToBytes());
        }
        return new BuildResult(written, unchanged);
    }

    /// <summary>Converts one image file; the colour of the result is multiplied by its alpha.</summary>
    /// <exception cref="CliException">The file is not an image the engine reads, or its size is not a multiple of four in both directions and <paramref name="format"/> is BC7.</exception>
    public static TextureFile ConvertFile(string file, TextureFormat format, CompressionQuality quality = CompressionQuality.Balanced)
    {
        PixelImage image;
        try { image = PixelImage.Decode(File.ReadAllBytes(file)); }
        catch (InvalidDataException e) { throw new CliException($"{Path.GetFileName(file)}: {e.Message}", 2); }
        var rgba = image.ToRgba();
        PremultipliedColor.PremultiplyInPlace(rgba);
        if (format == TextureFormat.Rgba8Unorm) return new TextureFile(image.Width, image.Height, format, [rgba]);
        if (format != TextureFormat.Bc7) throw new CliException($"{format} is not a format this command writes.", 2);
        if (image.Width % 4 != 0 || image.Height % 4 != 0)
            throw new CliException($"{Path.GetFileName(file)} is {image.Width}x{image.Height}: BC7 stores blocks of 4x4 pixels, and Direct3D 12 takes only textures whose width and height are multiples of 4. Pad the image, or store it with --format rgba.", 2);
        var encoder = new BcEncoder();
        encoder.OutputOptions.GenerateMipMaps = false;
        encoder.OutputOptions.Quality = quality;
        encoder.OutputOptions.Format = CompressionFormat.Bc7;
        var blocks = encoder.EncodeToRawBytes(rgba, image.Width, image.Height, PixelFormat.Rgba32)[0];
        return new TextureFile(image.Width, image.Height, format, [blocks]);
    }

    private static bool UpToDate(string source, string result, TextureFormat format)
    {
        if (!File.Exists(result) || File.GetLastWriteTimeUtc(result) < File.GetLastWriteTimeUtc(source)) return false;
        try { return TextureFile.Parse(File.ReadAllBytes(result)).Format == format; }
        catch (InvalidDataException) { return false; }
    }
}

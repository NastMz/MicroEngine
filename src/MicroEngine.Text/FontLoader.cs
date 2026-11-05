using MicroEngine.Content;
using MicroEngine.Core;
using MicroEngine.Platform;

namespace MicroEngine.Graphics;

/// <summary>
/// The scalable-font loader of the asset store, registered under the kind "font": it reads a TTF or OTF file through
/// the store's content source and opens it as a <see cref="ScalableFont"/> on a graphics device. A key's options are
/// its <see cref="FontOptions"/> (<see cref="FontOptions.Default"/> when none); the size it reports is the texture memory
/// of its glyph pages.
/// </summary>
internal static class FontLoader
{
    /// <summary>The asset kind of scalable fonts.</summary>
    public const string Kind = "font";

    /// <summary>The file extensions the loader opens.</summary>
    public static readonly string[] Extensions = [".ttf", ".otf"];

    /// <summary>Registers the loader in <paramref name="loaders"/>; its fonts create their pages on <paramref name="device"/>.</summary>
    public static void Register(AssetLoaderRegistry loaders, IGraphicsDevice device)
    {
        ArgumentNullException.ThrowIfNull(loaders);
        ArgumentNullException.ThrowIfNull(device);
        loaders.Add(new AssetLoader<ScalableFont>(Kind,
            c => Open(device, c.Path, Read(c.Source, c.Path), (FontOptions?)c.Options ?? FontOptions.Default),
            Extensions, f => f.TextureBytes));
    }

    /// <summary>Opens <paramref name="bytes"/> as the font at <paramref name="path"/>; a damaged file names the asset.</summary>
    /// <exception cref="AssetLoadException">The bytes are not a usable TTF or OTF font.</exception>
    public static ScalableFont Open(IGraphicsDevice device, string path, byte[] bytes, FontOptions options)
    {
        try { return ScalableFont.FromBytes(device, bytes, options, path); }
        catch (InvalidDataException e) { throw new AssetLoadException(path, Kind, e.Message, e); }
    }

    private static byte[] Read(IContentSource source, string path)
    {
        try { return source.ReadAllBytes(path); }
        catch (Exception e) when (e is IOException or ArgumentException or UnauthorizedAccessException)
        {
            throw new AssetLoadException(path, Kind, e.Message, e);
        }
    }
}

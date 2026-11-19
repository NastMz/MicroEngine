using System.Numerics;
using MicroEngine.Content;
using MicroEngine.Graphics;
using MicroEngine.Platform;

namespace MicroEngine.Core;

/// <summary>
/// The image and atlas loaders of <see cref="AssetCache"/>, and the font loader of text (<see cref="FontLoader"/>), registered in its store under the kinds "texture",
/// "atlas", "folder atlas" and "font", and the loaders of what a game draws with: "shader" (a compiled shader file),
/// "material" (a material from a compiled fragment shader) and "render target" (a picture to draw into, which no file
/// describes: its path is a name the game gives it). They read through the store's content source and create their
/// resources on the game's graphics device. Their keys: the sampling filter is the key's variant; an atlas's
/// <see cref="AtlasOptions"/>, a folder atlas's pivot (a <see cref="Vector2"/>), a font's <see cref="FontOptions"/>, a
/// material's <see cref="MaterialFallback"/> and a render target's <see cref="RenderTargetSize"/> are its options.
/// </summary>
/// <remarks>These loaders move to the 2D graphics module when it exists; the kinds and keys stay.</remarks>
internal static class GraphicsAssetLoaders
{
    public const string Texture = "texture", Atlas = "atlas", FolderAtlas = "folder atlas", Font = FontLoader.Kind;
    public const string Shader = "shader", Material = "material", RenderTarget = "render target";

    /// <summary>Whether <paramref name="kind"/> is one of these loaders' kinds, whose sizes are texture memory.</summary>
    public static bool IsGraphics(string kind) => kind is Texture or Atlas or FolderAtlas or Font or RenderTarget;

    public static void Register(AssetLoaderRegistry loaders, IGraphicsDevice device)
    {
        loaders.Add(new AssetLoader<Texture>(Texture,
            c => device.CreateTexture(DecodeImage(c.Source, c.Path, c.Key.Kind), (TextureFilter)c.Key.Variant),
            [.. ImageDecoder.Extensions.Order(StringComparer.Ordinal)], t => t.IsDisposed ? 0 : t.SizeInBytes));
        loaders.Add(new AssetLoader<SpriteAtlas>(Atlas, c =>
        {
            var (data, image) = DecodeAtlas(c.Source, c.Path, (AtlasOptions?)c.Options ?? AtlasOptions.Default);
            return BuildAtlas(device, c.Path, data, image, (TextureFilter)c.Key.Variant);
        }, [".json", ".xml"], AtlasBytes));
        loaders.Add(new AssetLoader<SpriteAtlas>(FolderAtlas, c =>
        {
            var pivot = c.Options is Vector2 p ? p : default;
            var files = c.Files().Where(f => ImageDecoder.Extensions.Contains(Path.GetExtension(f))).ToList();
            if (files.Count == 0) throw new AssetLoadException(c.Path, c.Key.Kind, "the folder holds no image.");
            var builder = new SpriteAtlasBuilder();
            foreach (var file in files)
            {
                var image = DecodeImage(c.Source, file, c.Key.Kind);
                builder.Add(Path.GetFileNameWithoutExtension(file), image, pivot * new Vector2(image.Width, image.Height));
            }
            return builder.Build(device, (TextureFilter)c.Key.Variant);
        }, size: AtlasBytes));
        // Scalable fonts are text's: its loader registers itself under the same kind.
        FontLoader.Register(loaders, device);
        loaders.Add(new AssetLoader<Graphics.Devices.CompiledShader>(Shader, c => ParseShader(c.Source, c.Path, c.Key.Kind), [Graphics.Devices.CompiledShader.FileExtension]));
        loaders.Add(new AssetLoader<Material2D>(Material, c =>
        {
            var shader = ParseShader(c.Source, c.Path, c.Key.Kind);
            if (shader.Stage != Graphics.Devices.ShaderStage.Fragment)
                throw new AssetLoadException(c.Path, c.Key.Kind, "a material is made from a fragment shader (a .frag.shader file); the engine supplies the vertex stage.");
            return device.CreateMaterial(shader, c.Options is MaterialFallback fallback ? fallback : default);
        }));
        loaders.Add(new AssetLoader<RenderTarget2D>(RenderTarget, c =>
        {
            if (c.Options is not RenderTargetSize size)
                throw new AssetLoadException(c.Path, c.Key.Kind, "a render target is not a file: ask for it with Assets.RenderTarget(name, width, height).");
            if (size.Width <= 0 || size.Height <= 0) throw new AssetLoadException(c.Path, c.Key.Kind, $"its size must be positive, not {size.Width} x {size.Height}.");
            return device.CreateRenderTarget(size.Width, size.Height, (TextureFilter)c.Key.Variant);
        }, size: t => t.IsDisposed ? 0 : t.SizeInBytes));
    }

    private static Graphics.Devices.CompiledShader ParseShader(IContentSource source, string path, string kind)
    {
        var bytes = Read(source, path, kind);
        try { return Graphics.Devices.CompiledShader.Parse(bytes); }
        catch (InvalidDataException e) { throw new AssetLoadException(path, kind, e.Message, e); }
    }

    private static long AtlasBytes(SpriteAtlas a) => a.Texture.IsDisposed ? 0 : a.Texture.SizeInBytes;

    /// <summary>The bytes of <paramref name="path"/>; a failure names the asset (any thread).</summary>
    public static byte[] Read(IContentSource source, string path, string kind)
    {
        try { return source.ReadAllBytes(path); }
        catch (Exception e) when (e is IOException or ArgumentException or UnauthorizedAccessException)
        {
            throw new AssetLoadException(path, kind, e.Message, e);
        }
    }

    public static PixelImage DecodeImage(IContentSource source, string path, string kind)
    {
        var bytes = Read(source, path, kind);
        try { return PixelImage.Decode(bytes); }
        catch (InvalidDataException e) { throw new AssetLoadException(path, kind, e.Message, e); }
    }

    public static (AtlasData Data, PixelImage Image) DecodeAtlas(IContentSource source, string path, AtlasOptions options)
    {
        AtlasData data;
        try { data = AtlasData.Parse(System.Text.Encoding.UTF8.GetString(Read(source, path, Atlas)), options); }
        catch (InvalidDataException e) { throw new AssetLoadException(path, Atlas, e.Message, e); }
        string folder = Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "";
        string image = data.ImagePath ?? Path.GetFileNameWithoutExtension(path) + ".png";
        string imagePath = AssetPath.Normalize(folder.Length == 0 ? image : folder + "/" + image);
        try { return (data, DecodeImage(source, imagePath, Atlas)); }
        catch (AssetLoadException e) { throw new AssetLoadException(path, Atlas, $"its image: {e.Message}", e); }
    }

    public static SpriteAtlas BuildAtlas(IGraphicsDevice device, string path, AtlasData data, PixelImage image, TextureFilter filter)
    {
        var texture = device.CreateTexture(image, filter);
        try { return SpriteAtlas.Create(texture, data); }
        catch (InvalidDataException e)
        {
            texture.Dispose();
            throw new AssetLoadException(path, Atlas, e.Message, e);
        }
    }

    public static ScalableFont OpenFont(IGraphicsDevice device, string path, byte[] bytes, FontOptions options) =>
        FontLoader.Open(device, path, bytes, options);
}

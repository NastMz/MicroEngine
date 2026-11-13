using System.Text;
using MicroEngine.Content;
using MicroEngine.Core;

namespace MicroEngine.Tiled;

/// <summary>
/// The <c>tiled map</c> asset kind: a Tiled map (<c>.tmx</c> or <c>.tmj</c>) with the tilesets (<c>.tsx</c>,
/// <c>.tsj</c>) and templates (<c>.tx</c>, <c>.tj</c>) it references, read through the store's content source so it
/// loads from folders, archives and browser packages alike. The asset is data (<see cref="Tiled.TiledMap"/>); its
/// tile images become textures with <see cref="TiledSprites"/>.
/// </summary>
public static class TiledAssets
{
    /// <summary>The asset kind.</summary>
    public const string Kind = "tiled map";

    /// <summary>The loader of the <c>tiled map</c> kind (extensions <c>.tmx</c> and <c>.tmj</c>).</summary>
    public static IAssetLoader<TiledMap> Loader { get; } = new AssetLoader<TiledMap>(Kind, Load, [".tmx", ".tmj"]);

    /// <summary>Registers the <c>tiled map</c> kind on <paramref name="loaders"/>.</summary>
    /// <returns><paramref name="loaders"/>, for chaining.</returns>
    public static AssetLoaderRegistry AddTiledMaps(this AssetLoaderRegistry loaders)
    {
        ArgumentNullException.ThrowIfNull(loaders);
        return loaders.Add(Loader);
    }

    /// <summary>
    /// The map at <paramref name="path"/>, loaded once per scope like any asset (the kind is registered on first use).
    /// </summary>
    /// <param name="assets">The game's assets (<c>Game.Assets</c>) or one of its scopes.</param>
    /// <param name="path">The map's content path.</param>
    /// <exception cref="AssetLoadException">The map, or a file it references, is missing or not a map Tiled saves; or the map is not orthogonal.</exception>
    public static TiledMap TiledMap(this AssetCache assets, string path)
    {
        ArgumentNullException.ThrowIfNull(assets);
        if (!assets.Loaders.TryGet(Kind, out _)) assets.Loaders.AddTiledMaps();
        return assets.Scope.Get(assets.Scope.Load<TiledMap>(Kind, path));
    }

    private static TiledMap Load(AssetLoadContext context) =>
        TiledReader.Read(context.Path, file => Encoding.UTF8.GetString(context.ReadBytes(file)));
}

using MicroEngine.Content;

namespace MicroEngine.Graphics3D;

/// <summary>
/// The <c>mesh</c> asset kind: a glTF 2.0 file (<c>.gltf</c> with its buffers and images beside it, or <c>.glb</c>) read
/// into <see cref="MeshData"/> by <see cref="GltfReader"/>. Register it on a store's loaders with <see cref="AddMeshes"/>;
/// the asset is CPU data, uploaded to a device with <see cref="Renderer3D.Upload"/>.
/// </summary>
public static class MeshAssets
{
    /// <summary>The asset kind.</summary>
    public const string Kind = "mesh";

    /// <summary>The loader of the <c>mesh</c> kind (extensions <c>.gltf</c> and <c>.glb</c>).</summary>
    public static IAssetLoader<MeshData> Loader { get; } = new AssetLoader<MeshData>(Kind, Load, [".gltf", ".glb"], mesh => mesh.SizeInBytes);

    /// <summary>Registers the <c>mesh</c> kind on <paramref name="loaders"/>.</summary>
    /// <returns><paramref name="loaders"/>, for chaining.</returns>
    public static AssetLoaderRegistry AddMeshes(this AssetLoaderRegistry loaders)
    {
        ArgumentNullException.ThrowIfNull(loaders);
        return loaders.Add(Loader);
    }

    private static MeshData Load(AssetLoadContext context)
    {
        string folder = Path.GetDirectoryName(context.Path)?.Replace('\\', '/') ?? "";
        return GltfReader.Read(context.ReadBytes(), context.Path,
            uri => context.ReadBytes(folder.Length == 0 ? uri : $"{folder}/{uri}"));
    }
}

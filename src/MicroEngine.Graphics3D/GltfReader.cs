using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using System.Text.Json;
using StbImageSharp;

namespace MicroEngine.Graphics3D;

/// <summary>
/// A minimal glTF 2.0 reader: the meshes of the default scene with their node transforms applied, as triangle lists of
/// positions, normals and the first texture coordinate, and each primitive's base colour (factor and texture, PNG or
/// JPEG). Reads <c>.gltf</c> (JSON with external or <c>data:</c> buffers and images) and <c>.glb</c>. It does not read
/// skins, animations, morph targets, cameras, lights, other material properties, sparse accessors or required
/// extensions: a file that needs one of them is refused with an <see cref="InvalidDataException"/> naming it, never
/// drawn incompletely. glTF's conventions are the engine's: right-handed, +Y up, counter-clockwise front faces, texture
/// row 0 at the top.
/// </summary>
public static class GltfReader
{
    private const uint GlbMagic = 0x46546C67, JsonChunk = 0x4E4F534A, BinChunk = 0x004E4942;

    /// <summary>Reads a glTF file.</summary>
    /// <param name="file">The bytes of the .gltf or .glb file.</param>
    /// <param name="name">The file name (diagnostics and the mesh name when the file names none).</param>
    /// <param name="resolve">Reads a file the glTF refers to by relative URI (external buffers and images); null refuses them.</param>
    /// <exception cref="InvalidDataException">The file is malformed or needs a feature this reader does not read; the message says which.</exception>
    public static MeshData Read(ReadOnlySpan<byte> file, string name, Func<string, byte[]>? resolve = null)
    {
        byte[]? glbBinary = null;
        byte[] json;
        if (file.Length >= 12 && BinaryPrimitives.ReadUInt32LittleEndian(file) == GlbMagic)
            (json, glbBinary) = SplitGlb(file, name);
        else json = file.ToArray();
        JsonDocument document;
        try { document = JsonDocument.Parse(json); }
        catch (JsonException e) { throw new InvalidDataException($"{name}: not glTF JSON ({e.Message})", e); }
        using (document)
        {
            try { return new Reader(document.RootElement, name, glbBinary, resolve).Read(); }
            catch (Exception e) when (e is KeyNotFoundException or InvalidOperationException or FormatException or IndexOutOfRangeException or ArgumentException)
            {
                throw new InvalidDataException($"{name}: malformed glTF ({e.Message})", e);
            }
        }
    }

    private static (byte[] Json, byte[]? Binary) SplitGlb(ReadOnlySpan<byte> file, string name)
    {
        uint version = BinaryPrimitives.ReadUInt32LittleEndian(file[4..]);
        if (version != 2) throw new InvalidDataException($"{name}: GLB version {version}; only glTF 2.0 is read");
        int length = (int)Math.Min(BinaryPrimitives.ReadUInt32LittleEndian(file[8..]), (uint)file.Length);
        byte[]? json = null;
        byte[]? binary = null;
        for (int offset = 12; offset + 8 <= length;)
        {
            int chunkLength = (int)BinaryPrimitives.ReadUInt32LittleEndian(file[offset..]);
            uint type = BinaryPrimitives.ReadUInt32LittleEndian(file[(offset + 4)..]);
            if (chunkLength < 0 || offset + 8 + chunkLength > length) throw new InvalidDataException($"{name}: GLB chunk runs past the end of the file");
            var data = file.Slice(offset + 8, chunkLength);
            if (type == JsonChunk && json is null) json = data.ToArray();
            else if (type == BinChunk && binary is null) binary = data.ToArray();
            offset += 8 + ((chunkLength + 3) & ~3);
        }
        if (json is null) throw new InvalidDataException($"{name}: GLB without a JSON chunk");
        return (json, binary);
    }

    private sealed class Reader(JsonElement root, string name, byte[]? glbBinary, Func<string, byte[]>? resolve)
    {
        private readonly Dictionary<int, byte[]> _buffers = [];
        private readonly Dictionary<int, ImageData> _images = [];

        public MeshData Read()
        {
            if (root.TryGetProperty("asset", out var asset) && asset.TryGetProperty("version", out var v) && v.GetString() is { } version && !version.StartsWith("2.", StringComparison.Ordinal))
                throw new InvalidDataException($"{name}: glTF {version}; only glTF 2.0 is read");
            if (root.TryGetProperty("extensionsRequired", out var required) && required.GetArrayLength() > 0)
                throw new InvalidDataException($"{name}: needs the extension(s) {string.Join(", ", required.EnumerateArray().Select(e => e.GetString()))}, which this reader does not read");
            if (!root.TryGetProperty("meshes", out var meshes) || meshes.GetArrayLength() == 0)
                throw new InvalidDataException($"{name}: the file has no mesh");

            var primitives = new List<MeshPrimitive>();
            string meshName = Path.GetFileNameWithoutExtension(name);
            var roots = SceneRoots();
            if (roots is null)
            {
                AddMesh(meshes[0], Matrix4x4.Identity, primitives);
                if (meshes[0].TryGetProperty("name", out var n) && n.GetString() is { Length: > 0 } s) meshName = s;
            }
            else
            {
                var nodes = root.GetProperty("nodes");
                var stack = new Stack<(int Node, Matrix4x4 Parent, int Depth)>(roots.Select(r => (r, Matrix4x4.Identity, 0)).Reverse());
                while (stack.TryPop(out var item))
                {
                    if (item.Depth > 64) throw new InvalidDataException($"{name}: node hierarchy deeper than 64 levels (a cycle?)");
                    var node = nodes[item.Node];
                    var world = LocalMatrix(node) * item.Parent;
                    if (node.TryGetProperty("mesh", out var meshIndex)) AddMesh(meshes[meshIndex.GetInt32()], world, primitives);
                    if (node.TryGetProperty("children", out var children))
                        foreach (var child in children.EnumerateArray().Reverse()) stack.Push((child.GetInt32(), world, item.Depth + 1));
                }
                if (primitives.Count == 0) throw new InvalidDataException($"{name}: the default scene draws no mesh");
            }
            return new MeshData(meshName, primitives);
        }

        private List<int>? SceneRoots()
        {
            if (!root.TryGetProperty("scenes", out var scenes) || scenes.GetArrayLength() == 0 || !root.TryGetProperty("nodes", out _)) return null;
            int scene = root.TryGetProperty("scene", out var s) ? s.GetInt32() : 0;
            return scenes[scene].TryGetProperty("nodes", out var nodes) ? [.. nodes.EnumerateArray().Select(n => n.GetInt32())] : [];
        }

        private static Matrix4x4 LocalMatrix(JsonElement node)
        {
            if (node.TryGetProperty("matrix", out var m))
            {
                // glTF matrices are column-major: column j holds row-vector basis j in System.Numerics' row-vector form.
                var f = m.EnumerateArray().Select(x => x.GetSingle()).ToArray();
                return new Matrix4x4(f[0], f[1], f[2], f[3], f[4], f[5], f[6], f[7], f[8], f[9], f[10], f[11], f[12], f[13], f[14], f[15]);
            }
            var scale = node.TryGetProperty("scale", out var sc) ? Vec3(sc) : Vector3.One;
            var rotation = node.TryGetProperty("rotation", out var r) ? new Quaternion(r[0].GetSingle(), r[1].GetSingle(), r[2].GetSingle(), r[3].GetSingle()) : Quaternion.Identity;
            var translation = node.TryGetProperty("translation", out var t) ? Vec3(t) : Vector3.Zero;
            return Matrix4x4.CreateScale(scale) * Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(translation);
        }

        private void AddMesh(JsonElement mesh, Matrix4x4 world, List<MeshPrimitive> into)
        {
            Matrix4x4.Invert(world, out var inverse);
            var normalMatrix = Matrix4x4.Transpose(inverse);
            bool mirrored = world.GetDeterminant() < 0;
            foreach (var primitive in mesh.GetProperty("primitives").EnumerateArray())
            {
                int mode = primitive.TryGetProperty("mode", out var md) ? md.GetInt32() : 4;
                if (mode != 4) throw new InvalidDataException($"{name}: primitive mode {mode}; only triangle lists (4) are read");
                if (primitive.TryGetProperty("targets", out _)) throw new InvalidDataException($"{name}: morph targets are not read");
                var attributes = primitive.GetProperty("attributes");
                var positions = ReadVec3(attributes.GetProperty("POSITION").GetInt32());
                var normals = attributes.TryGetProperty("NORMAL", out var na) ? ReadVec3(na.GetInt32()) : null;
                var uvs = attributes.TryGetProperty("TEXCOORD_0", out var ta) ? ReadVec2(ta.GetInt32()) : null;
                uint[] indices = primitive.TryGetProperty("indices", out var ia) ? ReadIndices(ia.GetInt32()) : [.. Enumerable.Range(0, positions.Length).Select(i => (uint)i)];
                if (indices.Length % 3 != 0) throw new InvalidDataException($"{name}: {indices.Length} indices is not a whole number of triangles");
                foreach (var index in indices)
                    if (index >= positions.Length) throw new InvalidDataException($"{name}: index {index} outside {positions.Length} vertices");
                if (mirrored)
                    for (int i = 0; i < indices.Length; i += 3) (indices[i + 1], indices[i + 2]) = (indices[i + 2], indices[i + 1]);
                normals ??= SmoothNormals(positions, indices, mirrored);
                var vertices = new MeshVertex[positions.Length];
                for (int i = 0; i < vertices.Length; i++)
                {
                    var n = Vector3.TransformNormal(normals[i], normalMatrix);
                    vertices[i] = new MeshVertex(Vector3.Transform(positions[i], world), n == Vector3.Zero ? n : Vector3.Normalize(n), uvs?[i] ?? Vector2.Zero);
                }
                var (color, texture) = Material(primitive);
                into.Add(new MeshPrimitive(vertices, indices, color, texture));
            }
        }

        private static Vector3[] SmoothNormals(Vector3[] positions, uint[] indices, bool flipped)
        {
            var normals = new Vector3[positions.Length];
            for (int i = 0; i < indices.Length; i += 3)
            {
                uint a = indices[i], b = indices[i + 1], c = indices[i + 2];
                var face = Vector3.Cross(positions[b] - positions[a], positions[c] - positions[a]); // area-weighted
                if (flipped) face = -face; // the indices were swapped for the mirrored world; normals are transformed after
                normals[a] += face; normals[b] += face; normals[c] += face;
            }
            for (int i = 0; i < normals.Length; i++) if (normals[i] != Vector3.Zero) normals[i] = Vector3.Normalize(normals[i]);
            return normals;
        }

        private (Vector4 Color, ImageData? Texture) Material(JsonElement primitive)
        {
            if (!primitive.TryGetProperty("material", out var mi)) return (Vector4.One, null);
            var material = root.GetProperty("materials")[mi.GetInt32()];
            if (!material.TryGetProperty("pbrMetallicRoughness", out var pbr)) return (Vector4.One, null);
            var color = pbr.TryGetProperty("baseColorFactor", out var f) ? new Vector4(f[0].GetSingle(), f[1].GetSingle(), f[2].GetSingle(), f[3].GetSingle()) : Vector4.One;
            if (!pbr.TryGetProperty("baseColorTexture", out var bt)) return (color, null);
            if (bt.TryGetProperty("texCoord", out var tc) && tc.GetInt32() != 0) throw new InvalidDataException($"{name}: the base colour texture uses TEXCOORD_{tc.GetInt32()}; only TEXCOORD_0 is read");
            var texture = root.GetProperty("textures")[bt.GetProperty("index").GetInt32()];
            if (!texture.TryGetProperty("source", out var source)) throw new InvalidDataException($"{name}: a texture has no image source (an extension's image format?)");
            return (color, Image(source.GetInt32()));
        }

        private ImageData Image(int index)
        {
            if (_images.TryGetValue(index, out var cached)) return cached;
            var image = root.GetProperty("images")[index];
            byte[] encoded = image.TryGetProperty("bufferView", out var bv) ? [.. BufferView(bv.GetInt32())] : Uri(image.GetProperty("uri").GetString()!);
            ImageResult decoded;
            try { decoded = ImageResult.FromMemory(encoded, ColorComponents.RedGreenBlueAlpha); }
            catch (Exception e) when (e is not OutOfMemoryException) { throw new InvalidDataException($"{name}: image {index} is not a PNG or JPEG this reader decodes ({e.Message})", e); }
            return _images[index] = new ImageData(decoded.Width, decoded.Height, decoded.Data);
        }

        private Vector3[] ReadVec3(int accessor)
        {
            var (data, count, stride, component, type) = Accessor(accessor);
            if (type != "VEC3" || component != 5126) throw new InvalidDataException($"{name}: accessor {accessor} is {type}/{component}; positions and normals must be float VEC3");
            var result = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                var s = data.AsSpan(i * stride, 12);
                result[i] = new Vector3(BinaryPrimitives.ReadSingleLittleEndian(s), BinaryPrimitives.ReadSingleLittleEndian(s[4..]), BinaryPrimitives.ReadSingleLittleEndian(s[8..]));
            }
            return result;
        }

        private Vector2[] ReadVec2(int accessor)
        {
            var (data, count, stride, component, type) = Accessor(accessor);
            if (type != "VEC2") throw new InvalidDataException($"{name}: accessor {accessor} is {type}; texture coordinates must be VEC2");
            var result = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                var s = data.AsSpan(i * stride);
                result[i] = component switch
                {
                    5126 => new Vector2(BinaryPrimitives.ReadSingleLittleEndian(s), BinaryPrimitives.ReadSingleLittleEndian(s[4..])),
                    5121 => new Vector2(s[0] / 255f, s[1] / 255f),
                    5123 => new Vector2(BinaryPrimitives.ReadUInt16LittleEndian(s) / 65535f, BinaryPrimitives.ReadUInt16LittleEndian(s[2..]) / 65535f),
                    _ => throw new InvalidDataException($"{name}: accessor {accessor}: texture coordinates of component type {component}"),
                };
            }
            return result;
        }

        private uint[] ReadIndices(int accessor)
        {
            var (data, count, stride, component, type) = Accessor(accessor);
            if (type != "SCALAR") throw new InvalidDataException($"{name}: index accessor {accessor} is {type}");
            var result = new uint[count];
            for (int i = 0; i < count; i++)
            {
                var s = data.AsSpan(i * stride);
                result[i] = component switch
                {
                    5121 => s[0],
                    5123 => BinaryPrimitives.ReadUInt16LittleEndian(s),
                    5125 => BinaryPrimitives.ReadUInt32LittleEndian(s),
                    _ => throw new InvalidDataException($"{name}: index accessor {accessor} of component type {component}"),
                };
            }
            return result;
        }

        private (ArraySegment<byte> Data, int Count, int Stride, int Component, string Type) Accessor(int index)
        {
            var accessor = root.GetProperty("accessors")[index];
            if (accessor.TryGetProperty("sparse", out _)) throw new InvalidDataException($"{name}: accessor {index} is sparse; sparse accessors are not read");
            int count = accessor.GetProperty("count").GetInt32();
            int component = accessor.GetProperty("componentType").GetInt32();
            string type = accessor.GetProperty("type").GetString()!;
            int size = ComponentSize(component) * (type switch { "SCALAR" => 1, "VEC2" => 2, "VEC3" => 3, "VEC4" => 4, _ => throw new InvalidDataException($"{name}: accessor type {type}") });
            if (!accessor.TryGetProperty("bufferView", out var bv)) throw new InvalidDataException($"{name}: accessor {index} has no buffer view");
            var view = root.GetProperty("bufferViews")[bv.GetInt32()];
            int stride = view.TryGetProperty("byteStride", out var st) ? st.GetInt32() : size;
            int offset = accessor.TryGetProperty("byteOffset", out var o) ? o.GetInt32() : 0;
            var data = BufferView(bv.GetInt32())[offset..];
            if (count > 0 && (long)(count - 1) * stride + size > data.Count) throw new InvalidDataException($"{name}: accessor {index} runs past its buffer view");
            return (data, count, stride, component, type);
        }

        private static int ComponentSize(int component) => component switch
        {
            5120 or 5121 => 1,
            5122 or 5123 => 2,
            5125 or 5126 => 4,
            _ => throw new InvalidDataException($"component type {component}"),
        };

        private ArraySegment<byte> BufferView(int index)
        {
            var view = root.GetProperty("bufferViews")[index];
            var buffer = Buffer(view.GetProperty("buffer").GetInt32());
            int offset = view.TryGetProperty("byteOffset", out var o) ? o.GetInt32() : 0;
            int length = view.GetProperty("byteLength").GetInt32();
            if (offset < 0 || length < 0 || offset + length > buffer.Length) throw new InvalidDataException($"{name}: buffer view {index} runs past its buffer");
            return new ArraySegment<byte>(buffer, offset, length);
        }

        private byte[] Buffer(int index)
        {
            if (_buffers.TryGetValue(index, out var cached)) return cached;
            var buffer = root.GetProperty("buffers")[index];
            byte[] data = buffer.TryGetProperty("uri", out var uri) ? Uri(uri.GetString()!)
                : glbBinary ?? throw new InvalidDataException($"{name}: buffer {index} has no URI and the file has no binary chunk");
            return _buffers[index] = data;
        }

        private byte[] Uri(string uri)
        {
            if (uri.StartsWith("data:", StringComparison.Ordinal))
            {
                int comma = uri.IndexOf(',');
                if (comma < 0 || !uri.AsSpan(0, comma).EndsWith(";base64", StringComparison.Ordinal)) throw new InvalidDataException($"{name}: only base64 data URIs are read");
                return Convert.FromBase64String(uri[(comma + 1)..]);
            }
            if (resolve is null) throw new InvalidDataException($"{name}: refers to '{uri}', and no file resolver was given");
            return resolve(System.Uri.UnescapeDataString(uri));
        }

        private static Vector3 Vec3(JsonElement a) => new(a[0].GetSingle(), a[1].GetSingle(), a[2].GetSingle());
    }
}

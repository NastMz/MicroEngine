using System.Buffers.Binary;
using System.IO.Compression;
using System.Numerics;
using System.Text;
using MicroEngine.Graphics3D;

namespace MicroEngine.Conformance.Graphics3D;

/// <summary>Meshes and glTF files built in code for the 3D tests: exact geometry, no external asset.</summary>
public static class TestMeshes
{
    /// <summary>An axis-aligned cube of side <paramref name="size"/> centred on the origin, outward counter-clockwise faces, flat normals, UVs 0..1 per face.</summary>
    public static MeshData Cube(float size, Vector4 color, ImageData? texture = null)
    {
        var (vertices, indices) = CubeGeometry(size);
        return new MeshData("cube", [new MeshPrimitive(vertices, indices, color, texture)]);
    }

    /// <summary>The cube's vertices and indices (24 vertices, 36 indices).</summary>
    public static (MeshVertex[] Vertices, uint[] Indices) CubeGeometry(float size)
    {
        float h = size / 2;
        // Each face: normal, and the two in-plane axes (u to the right, v up as seen from outside).
        (Vector3 N, Vector3 U, Vector3 V)[] faces =
        [
            (Vector3.UnitZ, Vector3.UnitX, Vector3.UnitY), (-Vector3.UnitZ, -Vector3.UnitX, Vector3.UnitY),
            (Vector3.UnitX, -Vector3.UnitZ, Vector3.UnitY), (-Vector3.UnitX, Vector3.UnitZ, Vector3.UnitY),
            (Vector3.UnitY, Vector3.UnitX, -Vector3.UnitZ), (-Vector3.UnitY, Vector3.UnitX, Vector3.UnitZ),
        ];
        var vertices = new List<MeshVertex>();
        var indices = new List<uint>();
        foreach (var (n, u, v) in faces)
        {
            uint b = (uint)vertices.Count;
            var c = n * h;
            vertices.Add(new(c - u * h - v * h, n, new(0, 1)));
            vertices.Add(new(c + u * h - v * h, n, new(1, 1)));
            vertices.Add(new(c + u * h + v * h, n, new(1, 0)));
            vertices.Add(new(c - u * h + v * h, n, new(0, 0)));
            indices.AddRange([b, b + 1, b + 2, b, b + 2, b + 3]);
        }
        return ([.. vertices], [.. indices]);
    }

    /// <summary>A .gltf (JSON with a base64 buffer) of one cube, optionally with a base colour texture (a PNG data URI) and a node transform.</summary>
    public static byte[] CubeGltf(float size, Vector4 color, ImageData? texture = null, string? nodeExtras = null, bool withNormals = true)
    {
        var (vertices, indices) = CubeGeometry(size);
        var bin = new MemoryStream();
        void F(float f) { Span<byte> b = stackalloc byte[4]; BinaryPrimitives.WriteSingleLittleEndian(b, f); bin.Write(b); }
        foreach (var v in vertices) { F(v.Position.X); F(v.Position.Y); F(v.Position.Z); }
        int normalsOffset = (int)bin.Length;
        foreach (var v in vertices) { F(v.Normal.X); F(v.Normal.Y); F(v.Normal.Z); }
        int uvOffset = (int)bin.Length;
        foreach (var v in vertices) { F(v.TexCoord.X); F(v.TexCoord.Y); }
        int indexOffset = (int)bin.Length;
        var b2 = new byte[2];
        foreach (var i in indices) { BinaryPrimitives.WriteUInt16LittleEndian(b2, (ushort)i); bin.Write(b2); }
        int total = (int)bin.Length;
        var min = new Vector3(-size / 2);
        var max = new Vector3(size / 2);
        string I(float f) => f.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        string attributes = withNormals ? "\"POSITION\": 0, \"NORMAL\": 1, \"TEXCOORD_0\": 2" : "\"POSITION\": 0, \"TEXCOORD_0\": 2";
        string material = texture is null
            ? $"{{ \"pbrMetallicRoughness\": {{ \"baseColorFactor\": [{I(color.X)}, {I(color.Y)}, {I(color.Z)}, {I(color.W)}] }} }}"
            : $"{{ \"pbrMetallicRoughness\": {{ \"baseColorFactor\": [{I(color.X)}, {I(color.Y)}, {I(color.Z)}, {I(color.W)}], \"baseColorTexture\": {{ \"index\": 0 }} }} }}";
        string images = texture is null ? "" : $", \"textures\": [{{ \"source\": 0 }}], \"images\": [{{ \"uri\": \"data:image/png;base64,{Convert.ToBase64String(Png(texture))}\" }}]";
        string json = $$"""
            {
              "asset": { "version": "2.0" },
              "scene": 0,
              "scenes": [{ "nodes": [0] }],
              "nodes": [{ "mesh": 0{{(nodeExtras is null ? "" : ", " + nodeExtras)}} }],
              "meshes": [{ "name": "cube", "primitives": [{ "attributes": { {{attributes}} }, "indices": 3, "material": 0 }] }],
              "materials": [{{material}}],
              "buffers": [{ "byteLength": {{total}}, "uri": "data:application/octet-stream;base64,{{Convert.ToBase64String(bin.ToArray())}}" }],
              "bufferViews": [
                { "buffer": 0, "byteOffset": 0, "byteLength": {{normalsOffset}} },
                { "buffer": 0, "byteOffset": {{normalsOffset}}, "byteLength": {{uvOffset - normalsOffset}} },
                { "buffer": 0, "byteOffset": {{uvOffset}}, "byteLength": {{indexOffset - uvOffset}} },
                { "buffer": 0, "byteOffset": {{indexOffset}}, "byteLength": {{total - indexOffset}} }
              ],
              "accessors": [
                { "bufferView": 0, "componentType": 5126, "count": {{vertices.Length}}, "type": "VEC3", "min": [{{I(min.X)}}, {{I(min.Y)}}, {{I(min.Z)}}], "max": [{{I(max.X)}}, {{I(max.Y)}}, {{I(max.Z)}}] },
                { "bufferView": 1, "componentType": 5126, "count": {{vertices.Length}}, "type": "VEC3" },
                { "bufferView": 2, "componentType": 5126, "count": {{vertices.Length}}, "type": "VEC2" },
                { "bufferView": 3, "componentType": 5123, "count": {{indices.Length}}, "type": "SCALAR" }
              ]{{images}}
            }
            """;
        return Encoding.UTF8.GetBytes(json);
    }

    /// <summary>The same file as a .glb: the JSON chunk with the buffer URI removed and the bytes in the binary chunk.</summary>
    public static byte[] ToGlb(byte[] gltf)
    {
        var json = Encoding.UTF8.GetString(gltf);
        const string marker = "\"uri\": \"data:application/octet-stream;base64,";
        int start = json.IndexOf(marker, StringComparison.Ordinal);
        int end = json.IndexOf('"', start + marker.Length);
        var bin = Convert.FromBase64String(json[(start + marker.Length)..end]);
        json = json[..start].TrimEnd().TrimEnd(',') + json[(end + 1)..];
        var jsonBytes = Encoding.UTF8.GetBytes(json);
        int jsonPadded = (jsonBytes.Length + 3) & ~3, binPadded = (bin.Length + 3) & ~3;
        var glb = new byte[12 + 8 + jsonPadded + 8 + binPadded];
        var s = glb.AsSpan();
        BinaryPrimitives.WriteUInt32LittleEndian(s, 0x46546C67);
        BinaryPrimitives.WriteUInt32LittleEndian(s[4..], 2);
        BinaryPrimitives.WriteUInt32LittleEndian(s[8..], (uint)glb.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(s[12..], (uint)jsonPadded);
        BinaryPrimitives.WriteUInt32LittleEndian(s[16..], 0x4E4F534A);
        s.Slice(20, jsonPadded).Fill((byte)' ');
        jsonBytes.CopyTo(s[20..]);
        int b = 20 + jsonPadded;
        BinaryPrimitives.WriteUInt32LittleEndian(s[b..], (uint)binPadded);
        BinaryPrimitives.WriteUInt32LittleEndian(s[(b + 4)..], 0x004E4942);
        bin.CopyTo(s[(b + 8)..]);
        return glb;
    }

    /// <summary>An uncompressed-filter RGBA8 PNG of <paramref name="image"/>.</summary>
    public static byte[] Png(ImageData image)
    {
        var png = new MemoryStream();
        png.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, image.Width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), image.Height);
        header[8] = 8; header[9] = 6;
        Chunk(png, "IHDR", header);
        var raw = new MemoryStream();
        for (int y = 0; y < image.Height; y++)
        {
            raw.WriteByte(0);
            raw.Write(image.Rgba, y * image.Width * 4, image.Width * 4);
        }
        var compressed = new MemoryStream();
        using (var z = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true)) z.Write(raw.ToArray());
        Chunk(png, "IDAT", compressed.ToArray());
        Chunk(png, "IEND", []);
        return png.ToArray();

        static void Chunk(Stream s, string type, byte[] data)
        {
            Span<byte> b = stackalloc byte[4];
            BinaryPrimitives.WriteInt32BigEndian(b, data.Length);
            s.Write(b);
            var typed = Encoding.ASCII.GetBytes(type).Concat(data).ToArray();
            s.Write(typed);
            BinaryPrimitives.WriteUInt32BigEndian(b, Crc32(typed));
            s.Write(b);
        }

        static uint Crc32(byte[] data)
        {
            uint crc = 0xFFFFFFFF;
            foreach (var d in data)
            {
                crc ^= d;
                for (int k = 0; k < 8; k++) crc = (crc & 1) != 0 ? 0xEDB88320 ^ (crc >> 1) : crc >> 1;
            }
            return ~crc;
        }
    }
}

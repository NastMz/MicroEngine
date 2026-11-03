using System.Buffers.Binary;
using System.Numerics;
using System.Text;

namespace Slice3D;

/// <summary>
/// The sample's model, made here so its source is in the repository (MIT, like the sample): a unit cube with outward
/// counter-clockwise faces, flat normals and a 64 × 64 crate texture (planks with a frame and a diagonal brace), written as
/// a binary glTF 2.0 file (<c>--write-crate</c> regenerates Content/crate.glb).
/// </summary>
internal static class CrateModel
{
    public static byte[] Glb()
    {
        var positions = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var indices = new List<ushort>();
        (Vector3 N, Vector3 U, Vector3 V)[] faces =
        [
            (Vector3.UnitZ, Vector3.UnitX, Vector3.UnitY), (-Vector3.UnitZ, -Vector3.UnitX, Vector3.UnitY),
            (Vector3.UnitX, -Vector3.UnitZ, Vector3.UnitY), (-Vector3.UnitX, Vector3.UnitZ, Vector3.UnitY),
            (Vector3.UnitY, Vector3.UnitX, -Vector3.UnitZ), (-Vector3.UnitY, Vector3.UnitX, Vector3.UnitZ),
        ];
        foreach (var (n, u, v) in faces)
        {
            var b = (ushort)positions.Count;
            var c = n * 0.5f;
            foreach (var (du, dv, uv) in new[] { (-1f, -1f, new Vector2(0, 1)), (1f, -1f, new Vector2(1, 1)), (1f, 1f, new Vector2(1, 0)), (-1f, 1f, new Vector2(0, 0)) })
            {
                positions.Add(c + u * (du * 0.5f) + v * (dv * 0.5f));
                normals.Add(n);
                uvs.Add(uv);
            }
            indices.AddRange([b, (ushort)(b + 1), (ushort)(b + 2), b, (ushort)(b + 2), (ushort)(b + 3)]);
        }

        var bin = new MemoryStream();
        var f = new byte[4];
        void Float(float x) { BinaryPrimitives.WriteSingleLittleEndian(f, x); bin.Write(f); }
        foreach (var p in positions) { Float(p.X); Float(p.Y); Float(p.Z); }
        int normalOffset = (int)bin.Length;
        foreach (var n in normals) { Float(n.X); Float(n.Y); Float(n.Z); }
        int uvOffset = (int)bin.Length;
        foreach (var t in uvs) { Float(t.X); Float(t.Y); }
        int indexOffset = (int)bin.Length;
        var s = new byte[2];
        foreach (var i in indices) { BinaryPrimitives.WriteUInt16LittleEndian(s, i); bin.Write(s); }
        while (bin.Length % 4 != 0) bin.WriteByte(0);
        int imageOffset = (int)bin.Length;
        var png = Png.Encode(64, 64, Texture());
        bin.Write(png);
        while (bin.Length % 4 != 0) bin.WriteByte(0);
        int total = (int)bin.Length;
        int count = positions.Count;

        string json = $$$$"""
            {"asset":{"version":"2.0","generator":"MicroEngine Slice3D CrateModel"},"scene":0,"scenes":[{"nodes":[0]}],
            "nodes":[{"name":"crate","mesh":0}],
            "meshes":[{"name":"crate","primitives":[{"attributes":{"POSITION":0,"NORMAL":1,"TEXCOORD_0":2},"indices":3,"material":0}]}],
            "materials":[{"name":"wood","pbrMetallicRoughness":{"baseColorFactor":[1,1,1,1],"baseColorTexture":{"index":0}}}],
            "textures":[{"source":0}],"images":[{"bufferView":4,"mimeType":"image/png"}],
            "buffers":[{"byteLength":{{{{total}}}}}],
            "bufferViews":[{"buffer":0,"byteOffset":0,"byteLength":{{{{normalOffset}}}}},{"buffer":0,"byteOffset":{{{{normalOffset}}}},"byteLength":{{{{uvOffset - normalOffset}}}}},
            {"buffer":0,"byteOffset":{{{{uvOffset}}}},"byteLength":{{{{indexOffset - uvOffset}}}}},{"buffer":0,"byteOffset":{{{{indexOffset}}}},"byteLength":{{{{indices.Count * 2}}}}},
            {"buffer":0,"byteOffset":{{{{imageOffset}}}},"byteLength":{{{{png.Length}}}}}],
            "accessors":[{"bufferView":0,"componentType":5126,"count":{{{{count}}}},"type":"VEC3","min":[-0.5,-0.5,-0.5],"max":[0.5,0.5,0.5]},
            {"bufferView":1,"componentType":5126,"count":{{{{count}}}},"type":"VEC3"},{"bufferView":2,"componentType":5126,"count":{{{{count}}}},"type":"VEC2"},
            {"bufferView":3,"componentType":5123,"count":{{{{indices.Count}}}},"type":"SCALAR"}]}
            """;
        var jsonBytes = Encoding.UTF8.GetBytes(json.ReplaceLineEndings(""));
        int jsonLength = (jsonBytes.Length + 3) & ~3;
        var glb = new byte[12 + 8 + jsonLength + 8 + total];
        var g = glb.AsSpan();
        BinaryPrimitives.WriteUInt32LittleEndian(g, 0x46546C67);
        BinaryPrimitives.WriteUInt32LittleEndian(g[4..], 2);
        BinaryPrimitives.WriteUInt32LittleEndian(g[8..], (uint)glb.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(g[12..], (uint)jsonLength);
        BinaryPrimitives.WriteUInt32LittleEndian(g[16..], 0x4E4F534A);
        g.Slice(20, jsonLength).Fill((byte)' ');
        jsonBytes.CopyTo(g[20..]);
        int b2 = 20 + jsonLength;
        BinaryPrimitives.WriteUInt32LittleEndian(g[b2..], (uint)total);
        BinaryPrimitives.WriteUInt32LittleEndian(g[(b2 + 4)..], 0x004E4942);
        bin.ToArray().CopyTo(g[(b2 + 8)..]);
        return glb;
    }

    /// <summary>64 × 64 RGBA: horizontal planks with grain, a dark frame and a diagonal brace.</summary>
    private static byte[] Texture()
    {
        var rgba = new byte[64 * 64 * 4];
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                int plank = y / 13;
                float grain = 0.88f + 0.12f * MathF.Sin((x + plank * 17) * 0.45f + MathF.Sin(y * 0.9f) * 2);
                (float r, float g, float b) c = (0.78f * grain, 0.55f * grain, 0.3f * grain);
                if (y % 13 == 0) c = (0.35f, 0.22f, 0.12f);                                   // gaps between planks
                bool frame = x < 6 || x >= 58 || y < 6 || y >= 58;
                bool brace = Math.Abs(x - y) < 4 && !frame;
                if (frame || brace) c = (0.55f * grain, 0.36f * grain, 0.18f * grain);
                if (frame && (x is 6 or 57 || y is 6 or 57)) c = (0.25f, 0.15f, 0.08f);
                int i = (y * 64 + x) * 4;
                rgba[i] = (byte)(c.r * 255); rgba[i + 1] = (byte)(c.g * 255); rgba[i + 2] = (byte)(c.b * 255); rgba[i + 3] = 255;
            }
        return rgba;
    }
}

/// <summary>PNG writing (8-bit RGBA, zlib) for the model's texture and the captures.</summary>
internal static class Png
{
    public static byte[] Encode(int width, int height, byte[] rgba)
    {
        var png = new MemoryStream();
        png.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
        header[8] = 8; header[9] = 6;
        Chunk(png, "IHDR", header);
        var raw = new MemoryStream();
        for (int y = 0; y < height; y++)
        {
            raw.WriteByte(0);
            raw.Write(rgba, y * width * 4, width * 4);
        }
        var compressed = new MemoryStream();
        using (var z = new System.IO.Compression.ZLibStream(compressed, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: true)) z.Write(raw.ToArray());
        Chunk(png, "IDAT", compressed.ToArray());
        Chunk(png, "IEND", []);
        return png.ToArray();
    }

    private static void Chunk(Stream s, string type, byte[] data)
    {
        var b = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(b, data.Length);
        s.Write(b);
        var typed = Encoding.ASCII.GetBytes(type).Concat(data).ToArray();
        s.Write(typed);
        uint crc = 0xFFFFFFFF;
        foreach (var d in typed)
        {
            crc ^= d;
            for (int k = 0; k < 8; k++) crc = (crc & 1) != 0 ? 0xEDB88320 ^ (crc >> 1) : crc >> 1;
        }
        BinaryPrimitives.WriteUInt32BigEndian(b, ~crc);
        s.Write(b);
    }
}

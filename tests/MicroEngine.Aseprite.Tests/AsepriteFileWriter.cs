using System.Text;
using MicroEngine.Graphics;

namespace MicroEngine.Aseprite.Tests;

/// <summary>
/// Writes small Aseprite files from the documented format (aseprite/docs/ase-file-specs.md), so the tests read real
/// <c>.aseprite</c> bytes without the editor: RGBA colour, one cel per layer and frame stored raw, frame tags and
/// slices. Not a general writer: only what the tests need.
/// </summary>
internal sealed class AsepriteFileWriter(int width, int height)
{
    private readonly List<(string Name, bool Visible, byte Opacity)> _layers = [];
    private readonly List<(int Milliseconds, List<(int Layer, int X, int Y, PixelImage Image)> Cels)> _frames = [];
    private readonly List<(string Name, int From, int To, byte Direction, int Repeat)> _tags = [];
    private readonly List<(string Name, int Frame, int X, int Y, int W, int H, (int X, int Y)? Pivot)> _slices = [];

    public AsepriteFileWriter Layer(string name, bool visible = true, byte opacity = 255)
    {
        _layers.Add((name, visible, opacity));
        return this;
    }

    /// <summary>Adds a frame shown for <paramref name="milliseconds"/>, with one cel per (layer, x, y, image).</summary>
    public AsepriteFileWriter Frame(int milliseconds, params (int Layer, int X, int Y, PixelImage Image)[] cels)
    {
        _frames.Add((milliseconds, [.. cels]));
        return this;
    }

    /// <summary>Adds a tag; <paramref name="direction"/> is 0 forward, 1 reverse, 2 ping-pong, 3 ping-pong reverse; <paramref name="repeat"/> 0 is for ever.</summary>
    public AsepriteFileWriter Tag(string name, int from, int to, byte direction = 0, int repeat = 0)
    {
        _tags.Add((name, from, to, direction, repeat));
        return this;
    }

    public AsepriteFileWriter Slice(string name, int frame, int x, int y, int w, int h, (int X, int Y)? pivot = null)
    {
        _slices.Add((name, frame, x, y, w, h, pivot));
        return this;
    }

    public byte[] ToBytes()
    {
        var frames = new List<byte[]>();
        for (int f = 0; f < _frames.Count; f++)
        {
            var chunks = new List<byte[]>();
            if (f == 0)
            {
                foreach (var (name, visible, opacity) in _layers)
                    chunks.Add(Chunk(0x2004, w =>
                    {
                        w.Write((ushort)(visible ? 3 : 2));   // visible | editable
                        w.Write((ushort)0);                   // a normal (image) layer
                        w.Write((ushort)0);                   // child level
                        w.Write((ushort)0);
                        w.Write((ushort)0);
                        w.Write((ushort)0);                   // blend mode: normal
                        w.Write(opacity);
                        w.Write(new byte[3]);
                        Text(w, name);
                    }));
                if (_tags.Count > 0)
                    chunks.Add(Chunk(0x2018, w =>
                    {
                        w.Write((ushort)_tags.Count);
                        w.Write(new byte[8]);
                        foreach (var (name, from, to, direction, repeat) in _tags)
                        {
                            w.Write((ushort)from);
                            w.Write((ushort)to);
                            w.Write(direction);
                            w.Write((ushort)repeat);
                            w.Write(new byte[6]);
                            w.Write(new byte[3]);
                            w.Write((byte)0);
                            Text(w, name);
                        }
                    }));
                foreach (var (name, frame, x, y, sw, sh, pivot) in _slices)
                    chunks.Add(Chunk(0x2022, w =>
                    {
                        w.Write(1u);                          // one key
                        w.Write(pivot is null ? 0u : 2u);     // flags: has a pivot
                        w.Write(0u);
                        Text(w, name);
                        w.Write((uint)frame);
                        w.Write(x);
                        w.Write(y);
                        w.Write((uint)sw);
                        w.Write((uint)sh);
                        if (pivot is { } p)
                        {
                            w.Write(p.X);
                            w.Write(p.Y);
                        }
                    }));
            }
            foreach (var (layer, x, y, image) in _frames[f].Cels)
                chunks.Add(Chunk(0x2005, w =>
                {
                    w.Write((ushort)layer);
                    w.Write((short)x);
                    w.Write((short)y);
                    w.Write((byte)255);                       // cel opacity
                    w.Write((ushort)0);                       // raw image data
                    w.Write((short)0);                        // z-index
                    w.Write(new byte[5]);
                    w.Write((ushort)image.Width);
                    w.Write((ushort)image.Height);
                    w.Write(image.ToRgba());
                }));
            using var body = new MemoryStream();
            using var writer = new BinaryWriter(body);
            int size = 16 + chunks.Sum(c => c.Length);
            writer.Write((uint)size);
            writer.Write((ushort)0xF1FA);
            writer.Write((ushort)Math.Min(chunks.Count, 0xFFFF));
            writer.Write((ushort)_frames[f].Milliseconds);
            writer.Write(new byte[2]);
            writer.Write((uint)chunks.Count);
            foreach (var chunk in chunks) writer.Write(chunk);
            writer.Flush();
            frames.Add(body.ToArray());
        }
        using var file = new MemoryStream();
        using var header = new BinaryWriter(file);
        header.Write((uint)(128 + frames.Sum(f => f.Length)));
        header.Write((ushort)0xA5E0);
        header.Write((ushort)_frames.Count);
        header.Write((ushort)width);
        header.Write((ushort)height);
        header.Write((ushort)32);                             // RGBA
        header.Write(1u);                                     // layer opacity is valid
        header.Write((ushort)100);                            // deprecated speed
        header.Write(0u);
        header.Write(0u);
        header.Write((byte)0);                                // transparent palette entry
        header.Write(new byte[3]);
        header.Write((ushort)0);                              // colours
        header.Write((byte)1);
        header.Write((byte)1);                                // square pixels
        header.Write((short)0);
        header.Write((short)0);
        header.Write((ushort)16);
        header.Write((ushort)16);                             // grid
        header.Write(new byte[84]);
        foreach (var frame in frames) header.Write(frame);
        header.Flush();
        return file.ToArray();
    }

    private static byte[] Chunk(ushort type, Action<BinaryWriter> write)
    {
        using var data = new MemoryStream();
        using (var w = new BinaryWriter(data, Encoding.UTF8, leaveOpen: true)) write(w);
        using var chunk = new MemoryStream();
        using var writer = new BinaryWriter(chunk);
        writer.Write((uint)(6 + data.Length));
        writer.Write(type);
        writer.Write(data.ToArray());
        writer.Flush();
        return chunk.ToArray();
    }

    private static void Text(BinaryWriter w, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        w.Write((ushort)bytes.Length);
        w.Write(bytes);
    }
}

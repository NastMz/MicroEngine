using System.Buffers.Binary;
using System.IO.Compression;

namespace MicroEngine.Graphics.Devices;

/// <summary>
/// A texture stored as the device takes it, in a KTX 2.0 file (the Khronos texture container, <c>.ktx2</c>): its size,
/// its format and the bytes of each mip level, ready to upload, with nothing to decode. It is what
/// <c>microengine textures</c> writes from image files, and what other tools that write KTX 2.0 produce; a game may
/// ship it beside or instead of an image, and nothing requires it. <see cref="Create"/> makes the texture on a
/// device; a device that does not sample the file's format (block-compressed formats are a capability, see
/// <see cref="GraphicsCapabilities.Supports"/>) gets the typed <see cref="GraphicsCapabilityException"/>, as for any
/// undeclared capability, so a game can keep its image file for those devices.
/// </summary>
/// <remarks>
/// <para>Read: one 2D texture (no array, cube or 3D texture) in one of <see cref="TextureFormat.Rgba8Unorm"/>,
/// <see cref="TextureFormat.Bc7"/>, <see cref="TextureFormat.Etc2Rgba8"/> or <see cref="TextureFormat.Astc4x4"/>, its
/// levels stored plainly or deflated (KTX's ZLIB supercompression). A format tagged sRGB is read as its plain format:
/// the engine uses a texture's bytes as it uses an image file's, without a transfer function. Zstandard and Basis
/// Universal (BasisLZ, UASTC) files are refused with a message: they need a decompressor or a transcoder the engine
/// does not carry.</para>
/// <para>Written: <see cref="TextureFormat.Rgba8Unorm"/> and <see cref="TextureFormat.Bc7"/>, with the data format
/// descriptor of each, the premultiplied-alpha flag, and a <c>KTXwriter</c> entry.</para>
/// </remarks>
public sealed class TextureFile
{
    /// <summary>The file extension of a KTX 2.0 texture.</summary>
    public const string FileExtension = ".ktx2";

    private const uint SchemeNone = 0, SchemeBasisLz = 1, SchemeZstandard = 2, SchemeZlib = 3;
    private const int HeaderLength = 80, LevelEntryLength = 24;
    private static ReadOnlySpan<byte> Identifier => [0xAB, 0x4B, 0x54, 0x58, 0x20, 0x32, 0x30, 0xBB, 0x0D, 0x0A, 0x1A, 0x0A];
    private static ReadOnlySpan<byte> Writer => "KTXwriter\0MicroEngine TextureFile\0"u8;
    private readonly ReadOnlyMemory<byte>[] _levels;

    /// <summary>Creates a texture file from the bytes of its levels.</summary>
    /// <param name="width">Width of level 0 in texels.</param>
    /// <param name="height">Height of level 0 in texels.</param>
    /// <param name="format">Texel format; not a depth format.</param>
    /// <param name="levels">Level 0 first, each half the size of the one before (at least 1), tightly packed: <see cref="TextureFormats.ByteCount"/> bytes each.</param>
    /// <param name="alpha">Whether colour is already multiplied by alpha (what the device draws) or straight.</param>
    /// <exception cref="ArgumentOutOfRangeException">A size that is not positive.</exception>
    /// <exception cref="ArgumentException">A depth format, no level, more levels than the size has, or a level whose length does not match its size.</exception>
    public TextureFile(int width, int height, TextureFormat format, IReadOnlyList<ReadOnlyMemory<byte>> levels, AlphaMode alpha = AlphaMode.Premultiplied)
    {
        ArgumentNullException.ThrowIfNull(levels);
        if (width < 1 || height < 1) throw new ArgumentOutOfRangeException(nameof(width), $"a texture's size must be positive, not {width}x{height}");
        if (TextureFormats.IsDepth(format)) throw new ArgumentException($"{format} has no data to store", nameof(format));
        if (levels.Count < 1 || levels.Count > MaxLevels(width, height))
            throw new ArgumentException($"a {width}x{height} texture has 1 to {MaxLevels(width, height)} levels, not {levels.Count}", nameof(levels));
        (Width, Height, Format, Alpha) = (width, height, format, alpha);
        _levels = [.. levels];
        for (int i = 0; i < _levels.Length; i++)
        {
            var (w, h) = SizeOf(i);
            if (_levels[i].Length != ByteCount(format, w, h))
                throw new ArgumentException($"level {i} ({w}x{h}, {format}) must be {ByteCount(format, w, h)} bytes, not {_levels[i].Length}", nameof(levels));
        }
    }

    /// <summary>Width of level 0 in texels.</summary>
    public int Width { get; }

    /// <summary>Height of level 0 in texels.</summary>
    public int Height { get; }

    /// <summary>Texel format.</summary>
    public TextureFormat Format { get; }

    /// <summary>Whether the colour channels are already multiplied by alpha.</summary>
    public AlphaMode Alpha { get; }

    /// <summary>Number of mip levels stored.</summary>
    public int Levels => _levels.Length;

    /// <summary>The bytes of a level as the device takes them (level 0 is the whole texture).</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="level"/> is not a stored level.</exception>
    public ReadOnlyMemory<byte> Level(int level)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(level);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(level, _levels.Length);
        return _levels[level];
    }

    /// <summary>The size in texels of a level: each is half the one before, never under 1.</summary>
    public (int Width, int Height) SizeOf(int level)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(level);
        return (Math.Max(1, Width >> level), Math.Max(1, Height >> level));
    }

    /// <summary>Whether a device with <paramref name="capabilities"/> samples this file's format, so <see cref="Create"/> succeeds on it.</summary>
    public bool CanCreate(GraphicsCapabilities capabilities)
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        return capabilities.Supports(Format, TextureUsage.Sampled) && Width <= capabilities.Limits.MaxTextureSize && Height <= capabilities.Limits.MaxTextureSize;
    }

    /// <summary>
    /// Creates the texture on <paramref name="device"/> from level 0, which the caller owns and disposes. Further
    /// levels of the file are not uploaded (no device declares <see cref="GraphicsFeatures.Mipmaps"/> yet). Straight
    /// <see cref="TextureFormat.Rgba8Unorm"/> data is premultiplied on the way, as an image upload is; a
    /// block-compressed file with straight alpha is created as it is, with <see cref="AlphaMode.Straight"/> in its
    /// description, because blocks cannot be converted without decoding them.
    /// </summary>
    /// <param name="device">The device the texture lives on.</param>
    /// <param name="name">Debug name.</param>
    /// <exception cref="GraphicsCapabilityException">The device does not sample <see cref="Format"/>.</exception>
    public Texture Create(IGraphicsDevice device, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(device);
        if (Alpha == AlphaMode.Straight && Format == TextureFormat.Rgba8Unorm && device.Capabilities.Supports(Format, TextureUsage.Sampled))
        {
            var texels = _levels[0].ToArray();
            PremultipliedColor.PremultiplyInPlace(texels);
            return device.CreateTexture(new TextureDescription(Width, Height, Format, Name: name), texels);
        }
        return device.CreateTexture(new TextureDescription(Width, Height, Format, Alpha: Alpha, Name: name), _levels[0].Span);
    }

    /// <summary>Reads a KTX 2.0 file.</summary>
    /// <exception cref="InvalidDataException">The data is not a KTX 2.0 file, is truncated or inconsistent, or holds what this type does not read (see the remarks of <see cref="TextureFile"/>); the message says which.</exception>
    public static TextureFile Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < Identifier.Length || !data[..Identifier.Length].SequenceEqual(Identifier))
            throw new InvalidDataException("not a KTX 2.0 texture file (its first twelve bytes are not the KTX 2.0 identifier)");
        if (data.Length < HeaderLength) throw new InvalidDataException("truncated KTX 2.0 file: the header is 80 bytes");
        uint Word(ReadOnlySpan<byte> bytes, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(bytes[offset..]);
        uint vkFormat = Word(data, 12), width = Word(data, 20), height = Word(data, 24), depth = Word(data, 28);
        uint layers = Word(data, 32), faces = Word(data, 36), levelCount = Math.Max(1, Word(data, 40)), scheme = Word(data, 44);
        if (depth > 1) throw new InvalidDataException("a 3D texture; one 2D texture is read");
        if (layers > 1) throw new InvalidDataException("a texture array; one 2D texture is read");
        if (faces != 1) throw new InvalidDataException("a cube map; one 2D texture is read");
        if (scheme == SchemeBasisLz) throw new InvalidDataException("a Basis Universal (BasisLZ) file, which needs a transcoder; store the texture in a GPU format (microengine textures)");
        if (scheme == SchemeZstandard) throw new InvalidDataException("levels compressed with Zstandard, which is not read; store them plainly or deflated (ZLIB)");
        if (scheme is not (SchemeNone or SchemeZlib)) throw new InvalidDataException($"unknown supercompression scheme {scheme}");
        var format = vkFormat switch
        {
            37 or 43 => TextureFormat.Rgba8Unorm,      // VK_FORMAT_R8G8B8A8_UNORM, _SRGB
            145 or 146 => TextureFormat.Bc7,           // VK_FORMAT_BC7_UNORM_BLOCK, _SRGB_BLOCK
            151 or 152 => TextureFormat.Etc2Rgba8,     // VK_FORMAT_ETC2_R8G8B8A8_UNORM_BLOCK, _SRGB_BLOCK
            157 or 158 => TextureFormat.Astc4x4,       // VK_FORMAT_ASTC_4x4_UNORM_BLOCK, _SRGB_BLOCK
            0 => throw new InvalidDataException("vkFormat 0 (a Basis Universal or otherwise undefined format) is not read; store the texture in a GPU format (microengine textures)"),
            _ => throw new InvalidDataException($"vkFormat {vkFormat} is not read (RGBA8, BC7, ETC2 RGBA8 and ASTC 4x4 are)"),
        };
        if (width < 1 || height < 1 || width > 65536 || height > 65536) throw new InvalidDataException($"the texture's size is {width}x{height}");
        if (levelCount > MaxLevels((int)width, (int)height)) throw new InvalidDataException($"{levelCount} levels; a {width}x{height} texture has at most {MaxLevels((int)width, (int)height)}");
        if (data.Length < HeaderLength + (long)levelCount * LevelEntryLength) throw new InvalidDataException("truncated KTX 2.0 file: the level index is cut short");

        // The data format descriptor says whether alpha is premultiplied (a flag of its basic block); without one, straight.
        var alpha = AlphaMode.Straight;
        uint dfdOffset = Word(data, 48), dfdLength = Word(data, 52);
        if (dfdLength >= 16)
        {
            if (dfdOffset + (long)dfdLength > data.Length) throw new InvalidDataException("truncated KTX 2.0 file: the data format descriptor is outside the file");
            if ((data[(int)dfdOffset + 15] & 1) != 0) alpha = AlphaMode.Premultiplied;
        }

        var levels = new ReadOnlyMemory<byte>[levelCount];
        for (int i = 0; i < levels.Length; i++)
        {
            var entry = data.Slice(HeaderLength + i * LevelEntryLength, LevelEntryLength);
            ulong offset = BinaryPrimitives.ReadUInt64LittleEndian(entry), length = BinaryPrimitives.ReadUInt64LittleEndian(entry[8..]);
            int levelWidth = Math.Max(1, (int)width >> i), levelHeight = Math.Max(1, (int)height >> i);
            long size = ByteCount(format, levelWidth, levelHeight);
            if (size > Array.MaxLength) throw new InvalidDataException($"level {i} ({levelWidth}x{levelHeight}, {format}) is {size} bytes, more than can be read");
            int expected = (int)size;
            if (offset > (ulong)data.Length || length > (ulong)data.Length - offset) throw new InvalidDataException($"level {i} is outside the file");
            var stored = data.Slice((int)offset, (int)length);
            // Deflate shrinks data at most about 1032 times: a level that announces more is refused before its memory is reserved.
            if (scheme == SchemeZlib && size > (stored.Length + 1L) * MaxDeflateRatio) throw new InvalidDataException($"level {i} cannot inflate to its {expected} bytes from {stored.Length}");
            if (scheme == SchemeZlib) levels[i] = Inflate(stored, expected, i);
            else if (stored.Length != expected) throw new InvalidDataException($"level {i} is {stored.Length} bytes; a {levelWidth}x{levelHeight} {format} level is {expected} bytes");
            else levels[i] = stored.ToArray();
        }
        return new TextureFile((int)width, (int)height, format, levels, alpha);
    }

    private static byte[] Inflate(ReadOnlySpan<byte> stored, int expected, int level)
    {
        var output = new byte[expected];
        bool more;
        try
        {
            using var inflate = new ZLibStream(new MemoryStream(stored.ToArray()), CompressionMode.Decompress);
            inflate.ReadExactly(output);
            more = inflate.ReadByte() != -1;
        }
        catch (Exception e) when (e is EndOfStreamException or InvalidDataException)
        {
            throw new InvalidDataException($"level {level} does not inflate to its {expected} bytes ({e.Message})", e);
        }
        if (more) throw new InvalidDataException($"level {level} inflates to more than its {expected} bytes");
        return output;
    }

    /// <summary>The file form of this texture: a KTX 2.0 file.</summary>
    /// <param name="deflate">True (the default) stores each level deflated (KTX's ZLIB supercompression), which most textures shrink well under; false stores them plainly, ready to be read without inflating.</param>
    /// <exception cref="NotSupportedException"><see cref="Format"/> is not one this type writes (<see cref="TextureFormat.Rgba8Unorm"/>, <see cref="TextureFormat.Bc7"/>).</exception>
    public byte[] ToBytes(bool deflate = true)
    {
        var (vkFormat, model, blockSize, samples) = Format switch
        {
            // (channel, bit offset, bit length - 1, upper): red, green, blue, alpha.
            TextureFormat.Rgba8Unorm => (37u, (byte)1, 4, new (byte Channel, ushort Offset, byte Length, uint Upper)[] { (0, 0, 7, 255), (1, 8, 7, 255), (2, 16, 7, 255), (15, 24, 7, 255) }),
            TextureFormat.Bc7 => (145u, (byte)134, 16, new (byte Channel, ushort Offset, byte Length, uint Upper)[] { (0, 0, 127, uint.MaxValue) }),
            _ => throw new NotSupportedException($"{Format} is not written; Rgba8Unorm and Bc7 are"),
        };
        bool blocks = TextureFormats.IsCompressed(Format);

        // The data format descriptor: its total size, then one basic block (24 bytes and 16 per sample).
        var dfd = new byte[4 + 24 + 16 * samples.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(dfd, (uint)dfd.Length);
        BinaryPrimitives.WriteUInt16LittleEndian(dfd.AsSpan(8), 2);                                  // versionNumber: KDF 1.3
        BinaryPrimitives.WriteUInt16LittleEndian(dfd.AsSpan(10), (ushort)(dfd.Length - 4));          // descriptorBlockSize
        dfd[12] = model;
        dfd[13] = 1;                                                                                  // primaries: BT.709
        dfd[14] = 1;                                                                                  // transfer: linear (the bytes are used as they are)
        dfd[15] = (byte)(Alpha == AlphaMode.Premultiplied ? 1 : 0);
        if (blocks) (dfd[16], dfd[17]) = (3, 3);                                                      // texel block: 4×4 (stored minus one)
        dfd[20] = (byte)blockSize;                                                                    // bytesPlane0: the size before deflating, deflated or not
        for (int i = 0; i < samples.Length; i++)
        {
            var sample = dfd.AsSpan(28 + i * 16);
            BinaryPrimitives.WriteUInt16LittleEndian(sample, samples[i].Offset);
            sample[2] = samples[i].Length;
            sample[3] = samples[i].Channel;
            BinaryPrimitives.WriteUInt32LittleEndian(sample[12..], samples[i].Upper);
        }

        // One key/value entry: its length, then "KTXwriter", a NUL, the value and a NUL, padded to four bytes.
        var kvd = new byte[(4 + Writer.Length + 3) / 4 * 4];
        BinaryPrimitives.WriteUInt32LittleEndian(kvd, (uint)Writer.Length);
        Writer.CopyTo(kvd.AsSpan(4));

        var stored = new byte[_levels.Length][];
        for (int i = 0; i < stored.Length; i++) stored[i] = deflate ? Deflate(_levels[i].Span) : _levels[i].ToArray();

        // Levels are stored from the smallest to the largest; without supercompression each starts at a multiple of the
        // block size (and of four).
        int dfdOffset = HeaderLength + _levels.Length * LevelEntryLength, kvdOffset = dfdOffset + dfd.Length;
        int alignment = deflate ? 1 : Math.Max(4, blockSize);
        var offsets = new long[_levels.Length];
        long position = kvdOffset + kvd.Length;
        for (int i = _levels.Length - 1; i >= 0; i--)
        {
            position = (position + alignment - 1) / alignment * alignment;
            offsets[i] = position;
            position += stored[i].Length;
        }

        var file = new byte[position];
        Identifier.CopyTo(file);
        uint[] header = [vkFormat, 1, (uint)Width, (uint)Height, 0, 0, 1, (uint)_levels.Length, deflate ? SchemeZlib : SchemeNone, (uint)dfdOffset, (uint)dfd.Length, (uint)kvdOffset, (uint)kvd.Length];
        for (int i = 0; i < header.Length; i++) BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(12 + i * 4), header[i]);
        // The supercompression global data offset and length (bytes 64 to 80) stay zero: there is none.
        for (int i = 0; i < _levels.Length; i++)
        {
            var entry = file.AsSpan(HeaderLength + i * LevelEntryLength);
            BinaryPrimitives.WriteUInt64LittleEndian(entry, (ulong)offsets[i]);
            BinaryPrimitives.WriteUInt64LittleEndian(entry[8..], (ulong)stored[i].Length);
            BinaryPrimitives.WriteUInt64LittleEndian(entry[16..], (ulong)_levels[i].Length);
            stored[i].CopyTo(file.AsSpan((int)offsets[i]));
        }
        dfd.CopyTo(file.AsSpan(dfdOffset));
        kvd.CopyTo(file.AsSpan(kvdOffset));
        return file;
    }

    private static byte[] Deflate(ReadOnlySpan<byte> data)
    {
        using var output = new MemoryStream();
        using (var deflate = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true)) deflate.Write(data);
        return output.ToArray();
    }

    private static int MaxLevels(int width, int height) => 32 - int.LeadingZeroCount(Math.Max(width, height));

    private const int MaxDeflateRatio = 1032;

    // TextureFormats.ByteCount in 64 bits: a size read from a file may announce more bytes than an int counts.
    private static long ByteCount(TextureFormat format, int width, int height) => TextureFormats.IsCompressed(format)
        ? ((width + 3L) / 4) * ((height + 3L) / 4) * 16
        : (long)width * height * TextureFormats.ByteCount(format, 1, 1);

    /// <inheritdoc/>
    public override string ToString() => $"{Width}x{Height} {Format}, {Levels} level{(Levels == 1 ? "" : "s")}, {(Alpha == AlphaMode.Premultiplied ? "premultiplied" : "straight")} alpha";
}

using System.Buffers.Binary;
using System.Numerics;
using MicroEngine.Graphics.Devices;

namespace MicroEngine.Graphics.Tests;

/// <summary>
/// The texture file (KTX 2.0): what it writes, what it reads back, what it refuses with a reason, and what a device is
/// given: the pixels as they are, premultiplied when the file says they are not, or the typed error where the device
/// does not sample the format.
/// </summary>
[TestClass]
public sealed class TextureFileTests
{
    private static byte[] Pixels(int width, int height, int seed = 1)
    {
        var rgba = new byte[width * height * 4];
        for (int i = 0; i < rgba.Length; i += 4)
        {
            byte a = (byte)((i * 37 + seed * 11) % 256);
            (rgba[i], rgba[i + 1], rgba[i + 2], rgba[i + 3]) = ((byte)(a * 3 / 4), (byte)(a / 2), (byte)(a / 5), a); // premultiplied: no channel above alpha
        }
        return rgba;
    }

    private static TextureFile ThreeLevels() => new(4, 4, TextureFormat.Rgba8Unorm, [Pixels(4, 4), Pixels(2, 2, 2), Pixels(1, 1, 3)]);

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void File_RoundTripsSizeFormatAlphaAndEveryLevel_AndIsByteStable(bool deflate)
    {
        var file = ThreeLevels();
        var bytes = file.ToBytes(deflate);
        var read = TextureFile.Parse(bytes);
        Assert.AreEqual((4, 4, TextureFormat.Rgba8Unorm, AlphaMode.Premultiplied, 3), (read.Width, read.Height, read.Format, read.Alpha, read.Levels));
        for (int level = 0; level < 3; level++)
            CollectionAssert.AreEqual(file.Level(level).ToArray(), read.Level(level).ToArray(), $"level {level}");
        CollectionAssert.AreEqual(bytes, read.ToBytes(deflate), "the same texture always gives the same file");
        Assert.AreEqual((2, 2), read.SizeOf(1));
        Assert.AreEqual((1, 1), read.SizeOf(2));
    }

    [TestMethod]
    public void File_KeepsStraightAlphaAsStraight()
    {
        var read = TextureFile.Parse(new TextureFile(2, 2, TextureFormat.Rgba8Unorm, [Pixels(2, 2)], AlphaMode.Straight).ToBytes());
        Assert.AreEqual(AlphaMode.Straight, read.Alpha);
    }

    [TestMethod]
    public void File_IsKtx2_AHeaderALevelIndexADescriptorAndLevelsFromTheSmallest()
    {
        var bytes = ThreeLevels().ToBytes(deflate: false);
        CollectionAssert.AreEqual(new byte[] { 0xAB, 0x4B, 0x54, 0x58, 0x20, 0x32, 0x30, 0xBB, 0x0D, 0x0A, 0x1A, 0x0A }, bytes[..12], "the KTX 2.0 identifier");
        uint Word(int offset) => BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset));
        ulong Long(int offset) => BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(offset));
        Assert.AreEqual(37u, Word(12), "vkFormat: VK_FORMAT_R8G8B8A8_UNORM");
        Assert.AreEqual(1u, Word(16), "typeSize");
        Assert.AreEqual((4u, 4u, 0u), (Word(20), Word(24), Word(28)), "width, height, depth (0: a 2D texture)");
        Assert.AreEqual((0u, 1u, 3u, 0u), (Word(32), Word(36), Word(40), Word(44)), "layers, faces, levels, supercompression");
        int dfd = (int)Word(48), dfdLength = (int)Word(52);
        Assert.AreEqual(80 + 3 * 24, dfd, "the descriptor follows the level index");
        Assert.AreEqual((uint)dfdLength, Word(dfd), "dfdTotalSize");
        Assert.AreEqual((byte)1, bytes[dfd + 12], "colour model: RGBSDA");
        Assert.AreEqual((byte)1, bytes[dfd + 15], "flags: alpha premultiplied");
        Assert.AreEqual((byte)4, bytes[dfd + 20], "bytesPlane0");
        // Level 2 (1×1) is stored first and level 0 (4×4) last, each at a multiple of four bytes.
        (ulong offset, ulong length)[] levels = [.. Enumerable.Range(0, 3).Select(i => (Long(80 + i * 24), Long(88 + i * 24)))];
        Assert.IsTrue(levels[2].offset < levels[1].offset && levels[1].offset < levels[0].offset);
        Assert.AreEqual((64ul, 16ul, 4ul), (levels[0].length, levels[1].length, levels[2].length));
        Assert.IsTrue(levels.All(l => l.offset % 4 == 0));
        Assert.AreEqual((ulong)bytes.Length, levels[0].offset + levels[0].length);
        CollectionAssert.AreEqual(Pixels(4, 4), bytes[(int)levels[0].offset..]);

        var deflated = ThreeLevels().ToBytes(deflate: true);
        Assert.AreEqual(3u, BinaryPrimitives.ReadUInt32LittleEndian(deflated.AsSpan(44)), "supercompression: ZLIB");
        Assert.AreEqual(64ul, BinaryPrimitives.ReadUInt64LittleEndian(deflated.AsSpan(96)), "the uncompressed length of level 0");
        Assert.AreEqual((byte)4, deflated[(int)BinaryPrimitives.ReadUInt32LittleEndian(deflated.AsSpan(48)) + 20], "bytesPlane0 keeps the size before deflating");
    }

    [TestMethod]
    public void File_OfBlocks_NamesTheBlockFormat()
    {
        var blocks = new byte[2 * 2 * 16]; // 8×8 pixels: four 4×4 blocks
        var bytes = new TextureFile(8, 8, TextureFormat.Bc7, [blocks]).ToBytes(deflate: false);
        Assert.AreEqual(145u, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(12)), "vkFormat: VK_FORMAT_BC7_UNORM_BLOCK");
        int dfd = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(48));
        Assert.AreEqual((byte)134, bytes[dfd + 12], "colour model: BC7");
        CollectionAssert.AreEqual(new byte[] { 3, 3, 0, 0 }, bytes[(dfd + 16)..(dfd + 20)], "texel block: 4×4");
        Assert.AreEqual((byte)16, bytes[dfd + 20], "bytesPlane0");
        Assert.AreEqual(0ul, BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(80)) % 16, "blocks start at a multiple of their size");
        var read = TextureFile.Parse(bytes);
        Assert.AreEqual((8, 8, TextureFormat.Bc7, 1), (read.Width, read.Height, read.Format, read.Levels));
    }

    // A file as another tool may write it: no descriptor to speak of, no key/value data, an sRGB format, one level.
    private static byte[] ByHand(uint vkFormat = 43, uint width = 2, uint height = 2, uint depth = 0, uint layers = 0, uint faces = 1, uint levels = 1, uint scheme = 0,
        ulong? levelOffset = null, ulong? levelLength = null, int payload = 16)
    {
        var bytes = new byte[104 + payload];
        new byte[] { 0xAB, 0x4B, 0x54, 0x58, 0x20, 0x32, 0x30, 0xBB, 0x0D, 0x0A, 0x1A, 0x0A }.CopyTo(bytes, 0);
        uint[] header = [vkFormat, 1, width, height, depth, layers, faces, levels, scheme, 0, 0, 0, 0];
        for (int i = 0; i < header.Length; i++) BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12 + i * 4), header[i]);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(80), levelOffset ?? 104);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(88), levelLength ?? (ulong)payload);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(96), levelLength ?? (ulong)payload);
        for (int i = 0; i < payload; i++) bytes[104 + i] = (byte)(i + 1);
        return bytes;
    }

    [TestMethod]
    public void Parse_ReadsAFileWithoutADescriptor_AsStraightAlpha()
    {
        var read = TextureFile.Parse(ByHand());
        Assert.AreEqual((2, 2, TextureFormat.Rgba8Unorm, AlphaMode.Straight, 1), (read.Width, read.Height, read.Format, read.Alpha, read.Levels), "an sRGB format is read as its plain format: the bytes are used as an image file's are");
        CollectionAssert.AreEqual(Enumerable.Range(1, 16).Select(i => (byte)i).ToArray(), read.Level(0).ToArray());
        Assert.AreEqual(TextureFormat.Bc7, TextureFile.Parse(ByHand(vkFormat: 146, width: 4, height: 4)).Format, "the sRGB block format is read as the block format");
        Assert.AreEqual(TextureFormat.Etc2Rgba8, TextureFile.Parse(ByHand(vkFormat: 151, width: 4, height: 4)).Format);
        Assert.AreEqual(TextureFormat.Astc4x4, TextureFile.Parse(ByHand(vkFormat: 157, width: 4, height: 4)).Format);
        Assert.AreEqual(TextureFormat.Rgba8Unorm, TextureFile.Parse(ByHand(vkFormat: 37)).Format);
        Assert.AreEqual(1, TextureFile.Parse(ByHand(levels: 0)).Levels, "levelCount 0 (make the mipmaps at load) holds one level");
    }

    [TestMethod]
    public void Parse_RefusesWhatItDoesNotRead_AndSaysWhat()
    {
        static void Refused(byte[] data, string expected)
        {
            var error = Assert.ThrowsExactly<InvalidDataException>(() => TextureFile.Parse(data));
            StringAssert.Contains(error.Message, expected);
        }
        Refused([], "KTX 2.0");
        Refused(new byte[200], "KTX 2.0");
        Refused(ByHand()[..90], "truncated");
        Refused(ByHand(scheme: 2), "Zstandard");
        Refused(ByHand(scheme: 1), "Basis");
        Refused(ByHand(faces: 6), "cube");
        Refused(ByHand(layers: 3), "array");
        Refused(ByHand(depth: 2), "3D");
        Refused(ByHand(vkFormat: 131), "vkFormat 131");
        Refused(ByHand(vkFormat: 0), "vkFormat 0");
        Refused(ByHand(width: 0), "size");
        Refused(ByHand(levelOffset: 4000), "outside the file");
        Refused(ByHand(levelLength: 12), "16 bytes");
        Refused(ByHand(levels: 4), "levels");
    }

    [TestMethod]
    public void Constructor_RefusesLevelsThatDoNotMatchTheirSize()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new TextureFile(4, 4, TextureFormat.Rgba8Unorm, [new byte[60]]));
        Assert.ThrowsExactly<ArgumentException>(() => new TextureFile(4, 4, TextureFormat.Rgba8Unorm, [new byte[64], new byte[64]]));
        Assert.ThrowsExactly<ArgumentException>(() => new TextureFile(4, 4, TextureFormat.Rgba8Unorm, []));
        Assert.ThrowsExactly<ArgumentException>(() => new TextureFile(4, 4, TextureFormat.Rgba8Unorm, [new byte[64], new byte[16], new byte[4], new byte[4]]), "a 4×4 texture has three levels");
        Assert.ThrowsExactly<ArgumentException>(() => new TextureFile(4, 4, TextureFormat.D16Unorm, [new byte[32]]));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TextureFile(0, 4, TextureFormat.Rgba8Unorm, [new byte[64]]));
        Assert.ThrowsExactly<NotSupportedException>(() => new TextureFile(4, 4, TextureFormat.Rgba16Float, [new byte[128]]).ToBytes(), "only formats the reader reads are written");
    }

    private static byte[] DrawAndReadBack(ReferenceGraphicsDevice device, Devices.Texture texture)
    {
        using var batcher = new Renderer2D(device);
        using var target = device.CreateRenderTarget(new RenderTargetDescription(texture.Width, texture.Height));
        batcher.Begin(new BatchSettings(target, Blend: BlendState.Opaque, Clear: new ColorF(0, 0, 0, 0)));
        batcher.Draw(texture, new RectF(0, 0, texture.Width, texture.Height), Matrix3x2.Identity, ColorF.White);
        batcher.End();
        var request = device.ReadPixels(target, target.Bounds);
        request.Wait();
        return request.Pixels.ToArray();
    }

    [TestMethod]
    public void Create_GivesTheDeviceThePixelsOfTheLargestLevel()
    {
        using var device = new ReferenceGraphicsDevice();
        var file = TextureFile.Parse(ThreeLevels().ToBytes());
        Assert.IsTrue(file.CanCreate(device.Capabilities));
        using var texture = file.Create(device);
        Assert.AreEqual((4, 4, TextureFormat.Rgba8Unorm, 1, AlphaMode.Premultiplied), (texture.Description.Width, texture.Description.Height, texture.Description.Format, texture.Description.MipLevels, texture.Description.Alpha));
        CollectionAssert.AreEqual(Pixels(4, 4), DrawAndReadBack(device, texture), "premultiplied pixels reach the device unchanged");
    }

    [TestMethod]
    public void Create_PremultipliesAStraightRgbaFile()
    {
        using var device = new ReferenceGraphicsDevice();
        byte[] straight = [200, 100, 50, 128, 255, 255, 255, 0, 10, 20, 30, 255, 255, 0, 0, 1];
        using var texture = new TextureFile(2, 2, TextureFormat.Rgba8Unorm, [straight], AlphaMode.Straight).Create(device);
        Assert.AreEqual(AlphaMode.Premultiplied, texture.Description.Alpha);
        CollectionAssert.AreEqual(new byte[] { 100, 50, 25, 128, 0, 0, 0, 0, 10, 20, 30, 255, 1, 0, 0, 1 }, DrawAndReadBack(device, texture));
        CollectionAssert.AreEqual(new byte[] { 200, 100, 50, 128 }, straight[..4], "the file's own bytes are not changed");
    }

    [TestMethod]
    public void Create_OnADeviceThatDoesNotSampleTheFormat_ThrowsTheCapabilityError()
    {
        using var device = new ReferenceGraphicsDevice();
        var file = new TextureFile(8, 8, TextureFormat.Bc7, [new byte[64]]);
        Assert.IsFalse(file.CanCreate(device.Capabilities));
        var error = Assert.ThrowsExactly<GraphicsCapabilityException>(() => file.Create(device));
        Assert.AreEqual("TextureFormat.Bc7 as Sampled", error.Requirement);
        Assert.AreEqual(ReferenceGraphicsDevice.BackendName, error.Device);
    }
}

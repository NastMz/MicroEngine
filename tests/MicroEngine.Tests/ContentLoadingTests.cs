using System.Buffers.Binary;
using System.IO.Compression;
using System.Numerics;
using MicroEngine.Audio;
using MicroEngine.Core;
using MicroEngine.Graphics;

namespace MicroEngine.Tests;

[TestClass]
public sealed class ContentLoadingTests
{
    // Builds a minimal PNG so the decoder is exercised on every colour type and filter without fixtures.
    private static byte[] Png(int width, int height, int colorType, int bitDepth, Func<int, byte[]> row, byte filter = 0, byte[]? plte = null, byte[]? trns = null)
    {
        using var raw = new MemoryStream();
        for (int y = 0; y < height; y++)
        {
            raw.WriteByte(filter);
            var bytes = row(y);
            if (filter == 1)
            {
                // Sub filter with one byte per pixel step for 8-bit data: store differences.
                int bpp = colorType switch { 2 => 3, 4 => 2, 6 => 4, _ => 1 };
                var encoded = (byte[])bytes.Clone();
                for (int i = encoded.Length - 1; i >= bpp; i--) encoded[i] = (byte)(bytes[i] - bytes[i - bpp]);
                bytes = encoded;
            }
            raw.Write(bytes);
        }
        using var compressed = new MemoryStream();
        using (var z = new ZLibStream(compressed, CompressionLevel.Fastest, leaveOpen: true)) z.Write(raw.ToArray());
        using var png = new MemoryStream();
        png.Write([0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A]);
        void Chunk(string type, byte[] data)
        {
            Span<byte> len = stackalloc byte[4];
            BinaryPrimitives.WriteInt32BigEndian(len, data.Length);
            png.Write(len);
            png.Write(System.Text.Encoding.ASCII.GetBytes(type));
            png.Write(data);
            png.Write(new byte[4]); // CRC is not verified by the decoder.
        }
        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
        header[8] = (byte)bitDepth;
        header[9] = (byte)colorType;
        Chunk("IHDR", header);
        if (plte is not null) Chunk("PLTE", plte);
        if (trns is not null) Chunk("tRNS", trns);
        Chunk("IDAT", compressed.ToArray());
        Chunk("IEND", []);
        return png.ToArray();
    }

    [TestMethod]
    public void Png_decodes_rgba_with_filters()
    {
        var data = Png(2, 2, 6, 8, y => y == 0 ? [255, 0, 0, 255, 0, 255, 0, 128] : [0, 0, 255, 255, 10, 20, 30, 0], filter: 1);
        var image = PixelImage.FromPng(data);
        Assert.AreEqual((2, 2), (image.Width, image.Height));
        Assert.AreEqual(new Color(255, 0, 0), image[0, 0]);
        Assert.AreEqual(new Color(0, 255, 0, 128), image[1, 0]);
        Assert.AreEqual(new Color(0, 0, 255), image[0, 1]);
        Assert.AreEqual(0, image[1, 1].A);
    }

    [TestMethod]
    public void Png_decodes_rgb_and_packed_palette_with_transparency()
    {
        var rgb = PixelImage.FromPng(Png(1, 1, 2, 8, _ => [1, 2, 3]));
        Assert.AreEqual(new Color(1, 2, 3), rgb[0, 0]);

        // Four 2-bit indices in one byte: 0,1,2,3. Index 0 is transparent through tRNS.
        var paletted = PixelImage.FromPng(Png(4, 1, 3, 2, _ => [0b00_01_10_11],
            plte: [0, 0, 0, 255, 0, 0, 0, 255, 0, 0, 0, 255], trns: [0]));
        Assert.AreEqual(0, paletted[0, 0].A);
        Assert.AreEqual(new Color(255, 0, 0), paletted[1, 0]);
        Assert.AreEqual(new Color(0, 255, 0), paletted[2, 0]);
        Assert.AreEqual(new Color(0, 0, 255), paletted[3, 0]);
    }

    [TestMethod]
    public void Png_rejects_corrupt_files_and_reads_16_bit_channels()
    {
        Assert.ThrowsExactly<InvalidDataException>(() => PixelImage.FromPng([1, 2, 3]));
        // 16-bit channels go through the fallback decoder: the high byte of each channel is the 8-bit value.
        var deep = PixelImage.FromPng(Png(1, 1, 2, 16, _ => [0xFF, 0x10, 0x80, 0x20, 0x00, 0x30]));
        Assert.AreEqual(new Color(255, 128, 0), deep[0, 0]);
    }

    [TestMethod]
    public void Load_reports_the_path_of_a_broken_file()
    {
        var path = Path.Combine(Path.GetTempPath(), $"me-broken-{Guid.NewGuid():N}.png");
        File.WriteAllBytes(path, [1, 2, 3]);
        try
        {
            var e = Assert.ThrowsExactly<InvalidDataException>(() => PixelImage.Load(path));
            StringAssert.Contains(e.Message, path);
        }
        finally { File.Delete(path); }
    }

    [TestMethod]
    public void Image_crop_clear_bounds_and_rotation()
    {
        var image = new PixelImage(5, 5);
        image[3, 2] = Color.White;
        Assert.AreEqual((3, 2, 1, 1), image.OpaqueBounds());
        var crop = image.Crop(2, 1, 3, 3);
        Assert.AreEqual(Color.White, crop[1, 1]);
        Assert.AreEqual(Color.Transparent, image.Crop(4, 4, 3, 3)[2, 2], "Outside the source is transparent.");

        // A quarter turn clockwise around the centre moves (3,2) (right of centre) to (2,3) (below).
        var turned = image.Rotated(MathF.PI / 2, 2.5f, 2.5f);
        Assert.AreEqual(Color.White, turned[2, 3]);
        Assert.AreEqual(Color.Transparent, turned[3, 2]);

        image.ClearRect(0, 0, 5, 5);
        Assert.IsNull(image.OpaqueBounds());
    }

    [TestMethod]
    public void Scale2x_doubles_size_and_rounds_diagonal_steps()
    {
        // A 2x2 diagonal: white at top-left and bottom-right, transparent elsewhere.
        var image = new PixelImage(2, 2);
        image[0, 0] = Color.White;
        image[1, 1] = Color.White;
        var big = image.Scale2x();
        Assert.AreEqual((4, 4), (big.Width, big.Height));
        // Each source pixel keeps its outer corners...
        Assert.AreEqual(Color.White, big[0, 0]);
        Assert.AreEqual(Color.White, big[3, 3]);
        Assert.AreEqual(Color.Transparent, big[3, 0]);
        // ...the gap pixels take the corners that touch both diagonal neighbours, and the lit pixels
        // give up the corner facing the gap, so the 2x step becomes a one-pixel slope.
        Assert.AreEqual(Color.White, big[2, 1]);
        Assert.AreEqual(Color.White, big[1, 2]);
        Assert.AreEqual(Color.Transparent, big[1, 1]);
        Assert.AreEqual(Color.Transparent, big[2, 2]);

        var flat = new PixelImage(3, 1).Clear(Color.White).Scale2x();
        for (int x = 0; x < 6; x++) Assert.AreEqual(Color.White, flat[x, 1], "A flat area stays flat.");
    }

    [TestMethod]
    public void Atlas_slices_grid_sheets_and_drops_trailing_empty_cells()
    {
        var sheet = new PixelImage(6, 4);
        sheet[0, 0] = Color.White;
        sheet[2, 0] = Color.White;
        sheet[4, 0] = Color.White;
        sheet[0, 2] = Color.White; // fourth cell; cells five and six stay empty
        var atlas = new SpriteAtlasBuilder().AddGrid("f", sheet, 2, 2, origin: new Vector2(1, 2)).Build(new HeadlessGraphicsDevice());
        var frames = atlas.Frames("f");
        Assert.HasCount(4, frames);
        Assert.AreEqual(new Vector2(1, 2), frames[0].Origin);
        var limited = new SpriteAtlasBuilder().AddGrid("g", sheet, 2, 2, count: 6).Build(new HeadlessGraphicsDevice());
        Assert.HasCount(6, limited.Frames("g"), "An explicit count keeps empty cells.");
    }

    [TestMethod]
    public void Wav_round_trips_through_the_decoder()
    {
        var clip = new AudioClip([0f, 0.5f, -0.5f, 1f], 22050, 2);
        var decoded = AudioClip.FromWav(clip.ToWav());
        Assert.AreEqual(22050, decoded.SampleRate);
        Assert.AreEqual(2, decoded.Channels);
        Assert.HasCount(4, decoded.Samples);
        Assert.AreEqual(0.5f, decoded.Samples[1], 0.001f);
        Assert.AreEqual(-0.5f, decoded.Samples[2], 0.001f);
        Assert.ThrowsExactly<InvalidDataException>(() => AudioClip.FromWav([0, 1, 2]));
    }

    [TestMethod]
    public void Mixer_registers_sound_and_music_files()
    {
        var dir = Directory.CreateTempSubdirectory("me-audio-");
        try
        {
            var wav = Path.Combine(dir.FullName, "beep.wav");
            File.WriteAllBytes(wav, new AudioClip([0.1f, 0.2f]).ToWav());
            var ogg = Path.Combine(dir.FullName, "song.ogg");
            File.WriteAllBytes(ogg, [1, 2, 3]);
            var device = new SilentAudioDevice();
            var mixer = new AudioMixer(device);
            mixer.AddSoundFile("beep", wav);
            mixer.AddMusicFile("song", ogg);
            Assert.IsTrue(mixer.HasSound("beep"));
            Assert.IsTrue(mixer.HasMusic("song"));
            mixer.PlayMusic("song", 0);
            Assert.AreEqual("song", mixer.CurrentMusic);
        }
        finally { dir.Delete(true); }
    }

    [TestMethod]
    public void Content_folder_resolves_files_and_reports_missing_ones()
    {
        var dir = Directory.CreateTempSubdirectory("me-content-");
        try
        {
            Directory.CreateDirectory(Path.Combine(dir.FullName, "img"));
            File.WriteAllBytes(Path.Combine(dir.FullName, "img", "a.png"), Png(1, 1, 2, 8, _ => [9, 9, 9]));
            var content = new ContentFolder(dir.FullName);
            Assert.AreEqual(new Color(9, 9, 9), content.Image("img/a.png")[0, 0]);
            Assert.IsTrue(content.Exists("img/a.png"));
            var e = Assert.ThrowsExactly<FileNotFoundException>(() => content.PathOf("img/missing.png"));
            StringAssert.Contains(e.Message, "missing.png");
            Assert.ThrowsExactly<ArgumentException>(() => content.PathOf("../escape.png"));
            Assert.ThrowsExactly<DirectoryNotFoundException>(() => new ContentFolder(Path.Combine(dir.FullName, "nope")));
        }
        finally { dir.Delete(true); }
    }

    [TestMethod]
    public void Parallax_layer_tiles_across_the_screen()
    {
        var device = new HeadlessGraphicsDevice();
        var texture = device.CreateTexture(new PixelImage(100, 10));
        var renderer = new HeadlessRenderer(250, 100);
        new ParallaxLayer(texture) { Factor = 0.5f }.Draw(renderer, new Vector2(30, 0));
        Assert.AreEqual(3, renderer.DrawCalls, "Offset -15 needs copies at -15, 85 and 185 to cover 250 px.");
        renderer.Reset();
        new ParallaxLayer(texture) { RepeatX = false }.Draw(renderer, Vector2.Zero);
        Assert.AreEqual(1, renderer.DrawCalls);
    }

    [TestMethod]
    public void A_content_folder_without_a_root_is_the_Content_folder_next_to_the_program()
    {
        Assert.AreEqual(Path.Combine(AppContext.BaseDirectory, "Content"), ContentFolder.DefaultRoot);
        Assert.AreEqual(Path.GetFullPath(AppContext.BaseDirectory), new ContentFolder(AppContext.BaseDirectory).Root, "an explicit root wins");
        if (Directory.Exists(ContentFolder.DefaultRoot)) return;
        var e = Assert.ThrowsExactly<DirectoryNotFoundException>(() => new ContentFolder());
        StringAssert.Contains(e.Message, Path.Combine(AppContext.BaseDirectory, "Content"));
    }

    [TestMethod]
    public void EncodedPngsDecodeToTheSamePixelsAtEveryScale()
    {
        var rgba = new byte[3 * 2 * 4];
        for (int i = 0; i < rgba.Length; i++) rgba[i] = (byte)(i * 11);
        var image = PixelImage.FromPng(PngEncoder.Encode(rgba, 3, 2));
        Assert.AreEqual(3, image.Width);
        CollectionAssert.AreEqual(rgba, image.ToRgba());
        var big = PixelImage.FromPng(PngEncoder.Encode(rgba, 3, 2, scale: 3));
        Assert.AreEqual(9, big.Width);
        Assert.AreEqual(6, big.Height);
        Assert.AreEqual(image[2, 1], big[8, 5]);
        Assert.AreEqual(image[1, 0], big[3, 2]);
    }
}

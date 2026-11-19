using MicroEngine.Graphics;
using MicroEngine.Graphics.Devices;

namespace MicroEngine.Cli.Tests;

/// <summary>
/// `microengine textures`: every image of a folder becomes a KTX 2.0 file the engine reads, premultiplied, either
/// block-compressed (BC7, close to the image) or as the image's own pixels (exactly the image); files already up to
/// date are left alone; what cannot be converted is refused with the file's name.
/// </summary>
[TestClass]
public sealed class TexturesTests
{
    private string _dir = "";

    [TestInitialize]
    public void CreateFolders() => _dir = Directory.CreateTempSubdirectory("microengine-textures-").FullName;

    [TestCleanup]
    public void DeleteFolders() => Directory.Delete(_dir, recursive: true);

    private string Source => Path.Combine(_dir, "Textures");
    private string Output => Path.Combine(_dir, "Content", "textures");

    /// <summary>A 16×12 image with opaque, translucent and transparent areas and colour that changes smoothly.</summary>
    private static PixelImage Picture(int width = 16, int height = 12)
    {
        var image = new PixelImage(width, height);
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
            image[x, y] = new Color((byte)(40 + x * 12), (byte)(200 - y * 10), (byte)(90 + x * 3 + y * 2), (byte)(x < 4 ? 0 : x < 8 ? 128 : 255));
        return image;
    }

    private string Write(string name, PixelImage image)
    {
        string path = Path.Combine(Source, name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, PngEncoder.Encode(image.ToRgba(), image.Width, image.Height));
        return path;
    }

    private static byte[] Premultiplied(PixelImage image)
    {
        var rgba = image.ToRgba();
        PremultipliedColor.PremultiplyInPlace(rgba);
        return rgba;
    }

    [TestMethod]
    public void Rgba_StoresTheImagesPremultipliedPixelsExactly_KeepingTheFolderLayout()
    {
        Write("hero.png", Picture());
        Write(Path.Combine("level1", "sky.png"), Picture(8, 8));
        var result = Textures.Convert(Source, Output, "rgba", "balanced", check: false);
        CollectionAssert.AreEquivalent(new[] { Path.Combine(Output, "hero.ktx2"), Path.Combine(Output, "level1", "sky.ktx2") }, result.Written.ToArray());
        Assert.IsEmpty(result.Unchanged);
        var file = TextureFile.Parse(File.ReadAllBytes(Path.Combine(Output, "hero.ktx2")));
        Assert.AreEqual((16, 12, TextureFormat.Rgba8Unorm, AlphaMode.Premultiplied, 1), (file.Width, file.Height, file.Format, file.Alpha, file.Levels));
        CollectionAssert.AreEqual(Premultiplied(Picture()), file.Level(0).ToArray());
    }

    [TestMethod]
    public void Bc7_StoresBlocksThatDecodeCloseToTheImage()
    {
        Write("hero.png", Picture());
        var result = Textures.Convert(Source, Output, "bc7", "balanced", check: false);
        Assert.HasCount(1, result.Written);
        var file = TextureFile.Parse(File.ReadAllBytes(result.Written[0]));
        Assert.AreEqual((16, 12, TextureFormat.Bc7, AlphaMode.Premultiplied, 1), (file.Width, file.Height, file.Format, file.Alpha, file.Levels));
        Assert.AreEqual(4 * 3 * 16, file.Level(0).Length, "four by three blocks of sixteen bytes");
        var decoded = new BCnEncoder.Decoder.BcDecoder().DecodeRaw(file.Level(0).ToArray(), 16, 12, BCnEncoder.Shared.CompressionFormat.Bc7);
        var expected = Premultiplied(Picture());
        // Close, not equal: the picture's colour changes in three directions inside each block, which BC7 approximates.
        double squared = 0;
        for (int i = 0; i < decoded.Length; i++)
        {
            int r = decoded[i].r - expected[i * 4], g = decoded[i].g - expected[i * 4 + 1], b = decoded[i].b - expected[i * 4 + 2], a = decoded[i].a - expected[i * 4 + 3];
            Assert.IsLessThanOrEqualTo(1, Math.Abs(a), $"alpha of pixel {i % 16},{i / 16}: it is constant in every block");
            Assert.IsLessThanOrEqualTo(16, Math.Max(Math.Abs(r), Math.Max(Math.Abs(g), Math.Abs(b))), $"pixel {i % 16},{i / 16}");
            squared += r * r + g * g + b * b + a * a;
        }
        double psnr = 10 * Math.Log10(255 * 255 / (squared / (decoded.Length * 4)));
        Assert.IsGreaterThanOrEqualTo(38, psnr, "peak signal-to-noise ratio in dB");
    }

    [TestMethod]
    public void AFileThatIsUpToDate_IsLeftAlone_AndCheckWritesNothing()
    {
        string source = Write("hero.png", Picture());
        var check = Textures.Convert(Source, Output, "rgba", "balanced", check: true);
        Assert.HasCount(1, check.Written, "check lists what is missing");
        Assert.IsFalse(Directory.Exists(Output), "and writes nothing");

        Textures.Convert(Source, Output, "rgba", "balanced", check: false);
        string target = Path.Combine(Output, "hero.ktx2");
        var written = File.GetLastWriteTimeUtc(target);
        var again = Textures.Convert(Source, Output, "rgba", "balanced", check: false);
        Assert.IsEmpty(again.Written);
        CollectionAssert.AreEqual(new[] { target }, again.Unchanged.ToArray());
        Assert.AreEqual(written, File.GetLastWriteTimeUtc(target));

        // A newer source, or another format asked for, makes it out of date.
        File.SetLastWriteTimeUtc(source, written.AddMinutes(1));
        Assert.HasCount(1, Textures.Convert(Source, Output, "rgba", "balanced", check: true).Written);
        File.SetLastWriteTimeUtc(source, written.AddMinutes(-1));
        Assert.IsEmpty(Textures.Convert(Source, Output, "rgba", "balanced", check: true).Written);
        Assert.HasCount(1, Textures.Convert(Source, Output, "bc7", "balanced", check: true).Written);
    }

    [TestMethod]
    public void WhatCannotBeConverted_IsRefusedWithTheReason()
    {
        static void Refused(Action action, string expected)
        {
            var error = Assert.ThrowsExactly<CliException>(action);
            StringAssert.Contains(error.Message, expected);
        }
        Refused(() => Textures.Convert(Source, Output, "rgba", "balanced", check: false), "no texture folder");
        Directory.CreateDirectory(Source);
        Refused(() => Textures.Convert(Source, Output, "rgba", "balanced", check: false), "no image");
        Write("odd.png", Picture(10, 7));
        Refused(() => Textures.Convert(Source, Output, "bc7", "balanced", check: false), "odd.png is 10x7");
        Assert.HasCount(1, Textures.Convert(Source, Output, "rgba", "balanced", check: false).Written, "any size can be stored as pixels");
        Refused(() => Textures.Convert(Source, Output, "dxt1", "balanced", check: false), "--format");
        Refused(() => Textures.Convert(Source, Output, "bc7", "ultra", check: false), "--quality");
        File.WriteAllBytes(Path.Combine(Source, "broken.png"), [1, 2, 3]);
        Refused(() => Textures.Convert(Source, Output, "rgba", "balanced", check: false), "broken.png");
    }

    [TestMethod]
    public async Task TheCommand_ConvertsTheGamesTexturesFolder()
    {
        Write("hero.png", Picture());
        var output = new StringWriter();
        var console = Console.Out;
        Console.SetOut(output);
        int exit;
        try { exit = await Cli.RunAsync(["textures", "--source", Source, "--out", Output, "--format", "rgba"]); }
        finally { Console.SetOut(console); }
        Assert.AreEqual(0, exit, output.ToString());
        Assert.IsTrue(File.Exists(Path.Combine(Output, "hero.ktx2")));
        StringAssert.Contains(output.ToString(), "textures: 1 written, 0 unchanged");

        Console.SetOut(output);
        try { exit = await Cli.RunAsync(["textures", "--source", Source, "--out", Output, "--format", "bc7", "--check"]); }
        finally { Console.SetOut(console); }
        Assert.AreEqual(1, exit, "check fails when a file is out of date");
        StringAssert.Contains(output.ToString(), "out of date");
    }
}

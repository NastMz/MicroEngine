using ContentBench;
using MicroEngine.Graphics;

namespace Benchmarks.Tests;

/// <summary>
/// The content benchmark's own logic (benchmarks/ContentBench): the content it draws is the same on every run and is
/// what its description says, the PNG files it writes are the pixels it drew, the prototype decoder reads them to the
/// same pixels as the engine's, and a mipmap chain halves and averages. The measurements are not tests.
/// </summary>
[TestClass]
public sealed class ContentBenchTests
{
    [TestMethod]
    public void TheContent_IsTheSameOnEveryRun_AndOfTheKindItsNameSays()
    {
        foreach (string kind in Pictures.Kinds)
            CollectionAssert.AreEqual(Pictures.Draw(kind, 256), Pictures.Draw(kind, 256), kind);
        static (int Clear, int Partial, int Opaque) Alpha(byte[] rgba)
        {
            int clear = 0, partial = 0, opaque = 0;
            for (int i = 3; i < rgba.Length; i += 4) { if (rgba[i] == 0) clear++; else if (rgba[i] == 255) opaque++; else partial++; }
            return (clear, partial, opaque);
        }
        var sprites = Alpha(Pictures.Draw("sprites", 512));
        Assert.IsTrue(sprites.Clear > 512 * 512 / 3 && sprites.Opaque > 512 * 512 / 3 && sprites.Partial > 0, $"a page of sprites over transparency, with antialiased edges: {sprites}");
        Assert.AreEqual((0, 0, 256 * 256), Alpha(Pictures.Draw("painted", 256)), "a painted backdrop is opaque");
        var tiles = Alpha(Pictures.Draw("tiles", 512));
        Assert.IsTrue(tiles.Opaque > 512 * 512 / 2 && tiles.Partial > 0, $"a tileset, mostly opaque, with soft-edged foliage: {tiles}");
        Assert.ThrowsExactly<AggregateException>(() => Pictures.Draw("photo", 16));
    }

    [TestMethod]
    public void ThePngFiles_AreThePixelsDrawn_AndThePrototypeDecoderReadsThemLikeTheEngines()
    {
        foreach (string kind in Pictures.Kinds.Append("emblem"))
        {
            var rgba = Pictures.Draw(kind, 128);
            var png = Pictures.EncodePng(rgba, 128, 128);
            var image = PixelImage.Decode(png);
            Assert.AreEqual((128, 128), (image.Width, image.Height));
            CollectionAssert.AreEqual(rgba, image.ToRgba(), $"{kind}: the engine's decoder");
            var fast = FastPng.Decode(png, out int width, out int height);
            Assert.IsNotNull(fast, kind);
            Assert.AreEqual((128, 128), (width, height));
            CollectionAssert.AreEqual(rgba, fast, $"{kind}: the prototype decoder");
        }
    }

    [TestMethod]
    public void ThePrototypeDecoder_ReadsTheEnginesOwnPngFilesToo_AndAnswersNullForWhatItDoesNotRead()
    {
        // The engine's encoder writes unfiltered rows; scaled by 2 the picture is 8×6.
        byte[] rgba = [.. Enumerable.Range(0, 4 * 3 * 4).Select(i => (byte)(i * 5))];
        var png = PngEncoder.Encode(rgba, 4, 3, scale: 2);
        var fast = FastPng.Decode(png, out int width, out int height);
        Assert.AreEqual((8, 6), (width, height));
        CollectionAssert.AreEqual(PixelImage.Decode(png).ToRgba(), fast);

        Assert.IsNull(FastPng.Decode(new byte[100], out _, out _), "not a PNG");
        var sixteen = (byte[])png.Clone();
        sixteen[24] = 16;
        Assert.IsNull(FastPng.Decode(sixteen, out _, out _), "16 bits per channel");
        var interlaced = (byte[])png.Clone();
        interlaced[28] = 1;
        Assert.IsNull(FastPng.Decode(interlaced, out _, out _), "interlaced");
        var palette = (byte[])png.Clone();
        palette[25] = 3;
        Assert.IsNull(FastPng.Decode(palette, out _, out _), "a palette");
    }

    [TestMethod]
    public void AMipmapChain_HalvesDownToOnePixel_AveragingFourTexels()
    {
        byte[] rgba = new byte[4 * 2 * 4];
        for (int i = 0; i < 8; i++) (rgba[i * 4], rgba[i * 4 + 1], rgba[i * 4 + 2], rgba[i * 4 + 3]) = ((byte)(i * 10), (byte)(i % 2 * 100), 7, 255);
        var chain = Mips.Chain(rgba, 4, 2);
        CollectionAssert.AreEqual(new[] { 32, 8, 4 }, chain.Select(level => level.Length).ToArray(), "4×2, 2×1, 1×1");
        // Level 1, first texel: texels 0, 1, 4, 5 of level 0 → red (0 + 10 + 40 + 50) / 4 = 25, green (0 + 100 + 0 + 100) / 4 = 50.
        CollectionAssert.AreEqual(new byte[] { 25, 50, 7, 255 }, chain[1][..4]);
        CollectionAssert.AreEqual(new byte[] { 45, 50, 7, 255 }, chain[1][4..8]);
        CollectionAssert.AreEqual(new byte[] { 35, 50, 7, 255 }, chain[2]);
    }
}

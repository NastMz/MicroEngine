using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Graphics.Devices;

namespace ContentBench;

/// <summary>Mipmap chains made on the CPU, as an offline step or a loader would make them.</summary>
internal static class Mips
{
    /// <summary>
    /// The whole chain of a premultiplied RGBA image, level 0 first, down to 1×1: each level averages the 2×2 texels
    /// of the one above (a box filter, which is exact on premultiplied colour).
    /// </summary>
    public static List<byte[]> Chain(byte[] rgba, int width, int height)
    {
        var levels = new List<byte[]> { rgba };
        while (width > 1 || height > 1)
        {
            int w = Math.Max(1, width / 2), h = Math.Max(1, height / 2);
            var above = levels[^1];
            var level = new byte[w * h * 4];
            int aboveWidth = width, aboveHeight = height;
            Parallel.For(0, h, y =>
            {
                int y0 = Math.Min(y * 2, aboveHeight - 1), y1 = Math.Min(y * 2 + 1, aboveHeight - 1);
                for (int x = 0; x < w; x++)
                {
                    int x0 = Math.Min(x * 2, aboveWidth - 1), x1 = Math.Min(x * 2 + 1, aboveWidth - 1);
                    int a = (y0 * aboveWidth + x0) * 4, b = (y0 * aboveWidth + x1) * 4, c = (y1 * aboveWidth + x0) * 4, d = (y1 * aboveWidth + x1) * 4, o = (y * w + x) * 4;
                    for (int channel = 0; channel < 4; channel++)
                        level[o + channel] = (byte)((above[a + channel] + above[b + channel] + above[c + channel] + above[d + channel] + 2) / 4);
                }
            });
            levels.Add(level);
            (width, height) = (w, h);
        }
        return levels;
    }
}

/// <summary>
/// What drawing art smaller than its pixels loses when the texture has one level. The engine's reference device (the
/// written sampling rule, on the CPU) draws a page at a half, a quarter and an eighth of its size with linear
/// filtering while it scrolls one texel at a time, and each frame is compared with the exact answer: the average of
/// the texels under each pixel. The same frames are drawn from the level of a mipmap chain that matches the scale,
/// which is what a sampler with mipmaps reads at these scales.
/// </summary>
internal static class Minify
{
    public static void Run(string dir, int size)
    {
        Console.WriteLine($"# Minification on the reference device, {size}² pages, linear filtering, error against the exact area average (8-bit levels, premultiplied)");
        Console.WriteLine();
        Console.WriteLine("| Page | Drawn at | One level: RMS error | worst pixel | flicker while scrolling | Matching mip level: RMS error | worst pixel | flicker while scrolling |");
        Console.WriteLine("| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |");
        using var device = new ReferenceGraphicsDevice();
        using var batcher = new Renderer2D(device);
        foreach (string kind in Pictures.Kinds)
        {
            var rgba = PixelImage.Decode(File.ReadAllBytes(Path.Combine(dir, Program.Name(kind, size) + ".png"))).ToRgba();
            PremultipliedColor.PremultiplyInPlace(rgba);
            var chain = Mips.Chain(rgba, size, size);
            using var full = device.CreateTexture(new TextureDescription(size, size), rgba);
            for (int level = 1; level <= 3; level++)
            {
                int factor = 1 << level, drawn = size / factor;
                // A window of the page, so the CPU rasterizer stays quick: at most 256 target pixels square, and short of
                // the page's right edge, which scrolls towards it.
                int window = Math.Min(256, drawn - 8);
                using var target = device.CreateRenderTarget(new RenderTargetDescription(window, window));
                using var mip = device.CreateTexture(new TextureDescription(drawn, drawn), chain[level]);
                var one = new Stats();
                var matched = new Stats();
                byte[]? previousOne = null, previousMatched = null, previousExact = null;
                for (int shift = 0; shift < factor * 2; shift++)
                {
                    // The page scrolls left by `shift` texels: shift / factor target pixels.
                    var exact = Exact(rgba, size, factor, shift, window);
                    var drawnOne = Draw(device, batcher, target, full, size, 1f / factor, -shift / (float)factor);
                    var drawnMatched = Draw(device, batcher, target, mip, drawn, 1f, -shift / (float)factor);
                    one.Add(drawnOne, exact, previousOne, previousExact);
                    matched.Add(drawnMatched, exact, previousMatched, previousExact);
                    (previousOne, previousMatched, previousExact) = (drawnOne, drawnMatched, exact);
                }
                Console.WriteLine($"| {kind} | 1/{factor} | {one.Rms:0.00} | {one.Worst} | {one.Flicker:0.00} | {matched.Rms:0.00} | {matched.Worst} | {matched.Flicker:0.00} |");
            }
        }
    }

    private static byte[] Draw(ReferenceGraphicsDevice device, Renderer2D batcher, RenderTarget target, MicroEngine.Graphics.Devices.Texture texture, int textureSize, float scale, float offset)
    {
        batcher.Begin(new BatchSettings(target, Sampler: SamplerDescription.LinearClamp, Clear: new ColorF(0, 0, 0, 0)));
        batcher.Draw(texture, new RectF(0, 0, textureSize, textureSize), Matrix3x2.CreateScale(scale) * Matrix3x2.CreateTranslation(offset, 0), ColorF.White);
        batcher.End();
        var request = device.ReadPixels(target, target.Bounds);
        request.Wait();
        return request.Pixels.ToArray();
    }

    // Target pixel (x, y) covers texels [x·factor + shift, (x + 1)·factor + shift) × [y·factor, (y + 1)·factor).
    private static byte[] Exact(byte[] rgba, int size, int factor, int shift, int window)
    {
        var exact = new byte[window * window * 4];
        for (int y = 0; y < window; y++)
        for (int x = 0; x < window; x++)
        for (int channel = 0; channel < 4; channel++)
        {
            int sum = 0;
            for (int j = 0; j < factor; j++)
            for (int i = 0; i < factor; i++)
            {
                int sx = x * factor + shift + i, sy = y * factor + j;
                if (sx < size) sum += rgba[(sy * size + sx) * 4 + channel];
            }
            exact[(y * window + x) * 4 + channel] = (byte)((sum + factor * factor / 2) / (factor * factor));
        }
        return exact;
    }

    private sealed class Stats
    {
        private double _squared, _flicker;
        private long _count, _flickerCount;
        public int Worst { get; private set; }
        public double Rms => Math.Sqrt(_squared / _count);
        /// <summary>Mean change of a pixel between two frames beyond the change of the exact picture.</summary>
        public double Flicker => _flickerCount == 0 ? 0 : _flicker / _flickerCount;

        public void Add(byte[] drawn, byte[] exact, byte[]? previousDrawn, byte[]? previousExact)
        {
            for (int i = 0; i < drawn.Length; i++)
            {
                int error = Math.Abs(drawn[i] - exact[i]);
                _squared += error * error;
                Worst = Math.Max(Worst, error);
            }
            _count += drawn.Length;
            if (previousDrawn is null || previousExact is null) return;
            for (int i = 0; i < drawn.Length; i++)
                _flicker += Math.Max(0, Math.Abs(drawn[i] - previousDrawn[i]) - Math.Abs(exact[i] - previousExact[i]));
            _flickerCount += drawn.Length;
        }
    }
}

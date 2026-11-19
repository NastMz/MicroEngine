using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using BCnEncoder.Encoder;
using BCnEncoder.Shared;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Sdl;
using Dev = MicroEngine.Graphics.Devices;

namespace ContentBench;

// CONTENT-1 (docs/evaluations/content-1.md). What loading image files directly costs on high-resolution content, and
// what the candidates of an offline step would change. Run in Release, on an idle machine:
//
//   ContentBench generate DIR                      draws the content: {sprites,painted,tiles}-{1024,2048,4096}.png
//   ContentBench direct DIR [--device D] [--runs N] [--audio DIR]
//                                                  each stage of a direct load, per file
//   ContentBench candidates DIR [--runs N]         offline candidates: block compression (BC7) and the image's own
//                                                  pixels as KTX 2.0 files (TextureFile, what `microengine textures`
//                                                  writes), their plain payloads, and mipmap chains on the CPU
//   ContentBench level DIR --path P [--size S] [--pages N] [--device D]
//                                                  one loading screen in a fresh process: N pages of each kind at
//                                                  size S through path P: direct (Assets.Texture), preload
//                                                  (Assets.Preload), lean and fast (the PNG decoded straight to device
//                                                  bytes, by the engine's decoder or the prototype), ktx-rgba and
//                                                  ktx-bc7 (KTX 2.0 files through TextureFile), rgba and bc7 (the
//                                                  same payloads stored plainly, with nothing to inflate)
//   ContentBench minify DIR [--size S]             what drawing art smaller than its pixels loses without mipmaps
//   ContentBench emblem FILE [--size S]            writes the picture the Examples sample ships (256 pixels square)
//
// D is sdl-renderer (the default device), sdl-gpu or headless. Timings are medians of N runs (5) after a warm-up.
internal static class Program
{
    internal static readonly int[] Sizes = [1024, 2048, 4096];

    public static int Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        if (args.Length < 2) return Usage();
        string dir = Path.GetFullPath(args[1]);
        var options = new Dictionary<string, string>();
        for (int i = 2; i + 1 < args.Length; i += 2) options[args[i].TrimStart('-')] = args[i + 1];
        int runs = int.Parse(options.GetValueOrDefault("runs", "5"));
        string device = options.GetValueOrDefault("device", "sdl-renderer");
        switch (args[0])
        {
            case "generate": Generate(dir); return 0;
            case "emblem": File.WriteAllBytes(dir, Pictures.EncodePng(Pictures.Draw("emblem", int.Parse(options.GetValueOrDefault("size", "256"))), int.Parse(options.GetValueOrDefault("size", "256")), int.Parse(options.GetValueOrDefault("size", "256")))); return 0;
            case "direct": Direct(dir, device, runs, options.GetValueOrDefault("audio")); return 0;
            case "candidates": Candidates(dir, runs); return 0;
            case "level": Level(dir, options.GetValueOrDefault("path", "direct"), int.Parse(options.GetValueOrDefault("size", "2048")), int.Parse(options.GetValueOrDefault("pages", "4")), device); return 0;
            case "minify": Minify.Run(dir, int.Parse(options.GetValueOrDefault("size", "2048"))); return 0;
            default: return Usage();
        }
    }

    private static int Usage()
    {
        Console.Error.WriteLine("ContentBench generate|direct|candidates|level|minify DIR [options]; see benchmarks/ContentBench/Program.cs");
        return 2;
    }

    internal static string Name(string kind, int size) => $"{kind}-{size}";

    // ---- generate

    private static void Generate(string dir)
    {
        Directory.CreateDirectory(dir);
        Console.WriteLine("| File | Pixels | PNG | Decoded (RGBA) | PNG / decoded | Transparent | Partly transparent |");
        Console.WriteLine("| --- | ---: | ---: | ---: | ---: | ---: | ---: |");
        foreach (int size in Sizes)
        foreach (string kind in Pictures.Kinds)
        {
            var rgba = Pictures.Draw(kind, size);
            var png = Pictures.EncodePng(rgba, size, size);
            File.WriteAllBytes(Path.Combine(dir, Name(kind, size) + ".png"), png);
            int clear = 0, partial = 0;
            for (int i = 3; i < rgba.Length; i += 4) { if (rgba[i] == 0) clear++; else if (rgba[i] < 255) partial++; }
            double pixels = size * (double)size;
            Console.WriteLine($"| {Name(kind, size)}.png | {size}×{size} | {Bytes(png.Length)} | {Bytes(rgba.Length)} | {png.Length * 100.0 / rgba.Length:0.0}% | {clear * 100 / pixels:0}% | {partial * 100 / pixels:0.0}% |");
        }
    }

    // ---- direct

    private static void Direct(string dir, string deviceName, int runs, string? audio)
    {
        using var platform = Open(deviceName, out string described);
        var graphics = platform.Graphics;
        graphics.DefaultFilter = TextureFilter.Linear;
        Console.WriteLine($"# Direct loading: {described}, {Machine()}, {runs} runs (median)");
        Console.WriteLine();
        Console.WriteLine("| File | Stage | Median | Allocated | GC 0/1/2 per load | GC pause per load |");
        Console.WriteLine("| --- | --- | ---: | ---: | --- | ---: |");
        foreach (int size in Sizes)
        foreach (string kind in Pictures.Kinds)
        {
            string file = Name(kind, size) + ".png", path = Path.Combine(dir, file);
            if (!File.Exists(path)) continue;
            var bytes = File.ReadAllBytes(path);
            var image = PixelImage.Decode(bytes);
            Row(file, "read the file", runs, () => File.ReadAllBytes(path));
            Row(file, "decode (PixelImage.Decode)", runs, () => PixelImage.Decode(bytes));
            Row(file, "decode, the decoder alone (StbImageSharp)", runs, () => StbImageSharp.ImageResult.FromMemory(bytes, StbImageSharp.ColorComponents.RedGreenBlueAlpha));
            var fast = FastPng.Decode(bytes, out _, out _) ?? throw new InvalidOperationException($"{file}: the prototype decoder does not read it");
            if (!fast.AsSpan().SequenceEqual(image.ToRgba())) throw new InvalidOperationException($"{file}: the prototype decoder gave other pixels");
            Row(file, "decode, prototype (inflated by the runtime)", runs, () => FastPng.Decode(bytes, out _, out _));
            Row(file, "upload (CreateTexture)", runs, () => graphics.CreateTexture(image).Dispose());
            if (graphics is DeviceGraphics { Device: var raw })
            {
                var texels = image.ToRgba();
                Row(file, "upload, the device alone (premultiplied bytes)", runs, () => raw.CreateTexture(new Dev.TextureDescription(size, size), texels).Dispose());
            }
            Row(file, "all: Assets.Texture(path)", runs, () => { using var cache = new AssetCache(graphics, dir); cache.Texture(file); });
        }
        if (audio is null) return;
        foreach (string path in Directory.GetFiles(audio).Where(f => f.EndsWith(".wav", StringComparison.Ordinal) || f.EndsWith(".ogg", StringComparison.Ordinal)).OrderBy(f => f, StringComparer.Ordinal))
        {
            string file = Path.GetFileName(path);
            string label = $"{file} ({Bytes(new FileInfo(path).Length)})";
            if (file.EndsWith(".wav", StringComparison.Ordinal))
                Row(label, "all: Assets.Sound(path)", runs, () => { using var cache = new AssetCache(graphics, audio); cache.Sound(file); });
            Row(label, "all: Assets.Music(path)", runs, () => { using var cache = new AssetCache(graphics, audio); cache.Music(file); });
        }
    }

    // ---- candidates

    private static void Candidates(string dir, int runs)
    {
        Console.WriteLine($"# Offline candidates: {Machine()}, {runs} runs (median)");
        Console.WriteLine();
        Console.WriteLine("| File | PNG | KTX2, pixels | KTX2, BC7 | BC7 blocks | BC7 encode, balanced | error (PSNR) | BC7 encode, fast | error (PSNR) | Mipmap chain on the CPU | Chain, extra bytes |");
        Console.WriteLine("| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |");
        foreach (int size in Sizes)
        foreach (string kind in Pictures.Kinds)
        {
            string name = Name(kind, size), path = Path.Combine(dir, name + ".png");
            if (!File.Exists(path)) continue;
            var rgba = PixelImage.Decode(File.ReadAllBytes(path)).ToRgba();
            Dev.PremultipliedColor.PremultiplyInPlace(rgba);
            File.WriteAllBytes(Path.Combine(dir, name + ".rgba"), rgba);
            var pixelsFile = new Dev.TextureFile(size, size, Dev.TextureFormat.Rgba8Unorm, [rgba]).ToBytes();
            File.WriteAllBytes(Path.Combine(dir, name + ".ktx-rgba"), pixelsFile);

            var (blocks, encodeSeconds, psnr) = Encode(rgba, size, CompressionQuality.Balanced);
            var (_, fastSeconds, fastPsnr) = Encode(rgba, size, CompressionQuality.Fast);
            File.WriteAllBytes(Path.Combine(dir, name + ".bc7"), blocks);
            var blocksFile = new Dev.TextureFile(size, size, Dev.TextureFormat.Bc7, [blocks]).ToBytes();
            File.WriteAllBytes(Path.Combine(dir, name + ".ktx-bc7"), blocksFile);

            var times = new List<double>();
            long chainBytes = 0;
            for (int i = 0; i <= runs; i++)
            {
                long start = Stopwatch.GetTimestamp();
                chainBytes = Mips.Chain(rgba, size, size).Skip(1).Sum(level => (long)level.Length);
                if (i > 0) times.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            }
            times.Sort();
            Console.WriteLine($"| {name} | {Bytes(new FileInfo(path).Length)} | {Bytes(pixelsFile.Length)} | {Bytes(blocksFile.Length)} | {Bytes(blocks.Length)} | {encodeSeconds:0.0} s | {psnr:0.0} dB | {fastSeconds:0.0} s | {fastPsnr:0.0} dB | {times[times.Count / 2]:0.0} ms | {Bytes(chainBytes)} |");
        }
    }

    // BC7 blocks of premultiplied pixels with the encoder `microengine textures` uses, the time it took and how far
    // the decoded blocks are from the pixels (peak signal-to-noise ratio over the four channels).
    private static (byte[] Blocks, double Seconds, double Psnr) Encode(byte[] rgba, int size, CompressionQuality quality)
    {
        var encoder = new BcEncoder();
        encoder.OutputOptions.GenerateMipMaps = false;
        encoder.OutputOptions.Quality = quality;
        encoder.OutputOptions.Format = CompressionFormat.Bc7;
        var clock = Stopwatch.StartNew();
        var blocks = encoder.EncodeToRawBytes(rgba, size, size, PixelFormat.Rgba32)[0];
        double seconds = clock.Elapsed.TotalSeconds;
        var decoded = new BCnEncoder.Decoder.BcDecoder().DecodeRaw(blocks, size, size, CompressionFormat.Bc7);
        double squared = 0;
        for (int i = 0; i < decoded.Length; i++)
        {
            int r = decoded[i].r - rgba[i * 4], g = decoded[i].g - rgba[i * 4 + 1], b = decoded[i].b - rgba[i * 4 + 2], a = decoded[i].a - rgba[i * 4 + 3];
            squared += r * r + g * g + b * b + a * a;
        }
        double mse = squared / (decoded.Length * 4.0);
        return (blocks, seconds, mse == 0 ? double.PositiveInfinity : 10 * Math.Log10(255 * 255 / mse));
    }

    // ---- level

    // One loading screen, in a process of its own so that garbage, pauses and peak memory are those of this path alone:
    // `pages` pages of each kind at one size, loaded at a 60 Hz frame cadence as a loading scene would.
    private static void Level(string dir, string path, int size, int pages, string deviceName)
    {
        var files = new List<string>();
        string extension = path is "direct" or "preload" or "lean" or "fast" ? ".png" : "." + path;
        string scratch = Path.Combine(dir, $"level-{size}");
        Directory.CreateDirectory(scratch);
        foreach (string kind in Pictures.Kinds)
            for (int i = 0; i < pages; i++)
            {
                // The same pixels under different names: the cache holds one object per path, so each is a load of its own.
                string copy = $"{Name(kind, size)}-{i}{extension}";
                if (!File.Exists(Path.Combine(scratch, copy))) File.Copy(Path.Combine(dir, Name(kind, size) + extension), Path.Combine(scratch, copy));
                files.Add(copy);
            }
        foreach (string file in files) File.ReadAllBytes(Path.Combine(scratch, file)); // the system's file cache, for every path alike

        using var platform = Open(deviceName, out string described);
        var graphics = platform.Graphics;
        graphics.DefaultFilter = TextureFilter.Linear;
        var device = (graphics as DeviceGraphics)?.Device;
        if (path is not ("direct" or "preload") && device is null) throw new InvalidOperationException($"path '{path}' creates device textures; '{deviceName}' has no graphics device");
        var format = path.EndsWith("bc7", StringComparison.Ordinal) ? Dev.TextureFormat.Bc7 : Dev.TextureFormat.Rgba8Unorm;
        if (device is not null && !device.Capabilities.Supports(format, Dev.TextureUsage.Sampled)) throw new InvalidOperationException($"{described} does not sample {format}");

        var keep = new List<IDisposable>();
        using var cache = new AssetCache(graphics, scratch);
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        long gpuBefore = GpuMemory.CurrentUsage();
        long allocatedBefore = GC.GetTotalAllocatedBytes(true);
        int gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2);
        var pauseBefore = GC.GetTotalPauseDuration();
        double longestFrame = 0;
        int frames = 0;
        var clock = Stopwatch.StartNew();

        void Frame(Action work)
        {
            long start = Stopwatch.GetTimestamp();
            work();
            double ms = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            longestFrame = Math.Max(longestFrame, ms);
            frames++;
        }

        switch (path)
        {
            case "direct":
                // Every file in one call, as a game does in Load: one long frame.
                Frame(() => { foreach (string file in files) cache.Texture(file); });
                break;
            case "preload":
            {
                AssetBatch? batch = null;
                Frame(() => batch = cache.Preload([.. files]));
                while (!batch!.IsDone)
                {
                    Frame(cache.Update);
                    if (!batch.IsDone) Thread.Sleep(Math.Max(0, 16 - (int)longestFrame % 16));
                }
                if (batch.Errors.Count > 0) throw batch.Errors[0];
                break;
            }
            case "lean":
            case "fast":
                // The same PNG files, decoded straight to bytes, premultiplied in place and handed to the device: what a
                // texture loader that makes no PixelImage would do. "fast" decodes with the prototype decoder.
                Frame(() =>
                {
                    foreach (string file in files)
                    {
                        var png = File.ReadAllBytes(Path.Combine(scratch, file));
                        var texels = path == "fast" ? FastPng.Decode(png, out _, out _)! : StbImageSharp.ImageResult.FromMemory(png, StbImageSharp.ColorComponents.RedGreenBlueAlpha).Data;
                        Dev.PremultipliedColor.PremultiplyInPlace(texels);
                        keep.Add(device!.CreateTexture(new Dev.TextureDescription(size, size), texels));
                    }
                });
                break;
            default:
                Frame(() =>
                {
                    foreach (string file in files)
                    {
                        var payload = File.ReadAllBytes(Path.Combine(scratch, file));
                        keep.Add(path.StartsWith("ktx", StringComparison.Ordinal) ? Dev.TextureFile.Parse(payload).Create(device!) : device!.CreateTexture(new Dev.TextureDescription(size, size, format), payload));
                    }
                });
                break;
        }
        double elapsed = clock.Elapsed.TotalMilliseconds;
        long allocated = GC.GetTotalAllocatedBytes(true) - allocatedBefore;
        double pause = (GC.GetTotalPauseDuration() - pauseBefore).TotalMilliseconds;
        (gen0, gen1, gen2) = (GC.CollectionCount(0) - gen0, GC.CollectionCount(1) - gen1, GC.CollectionCount(2) - gen2);

        // Draw every texture of the drawing API once and present, so a device that defers allocation has made it.
        if (path is "direct" or "preload")
            for (int i = 0; i < 3; i++)
            {
                var renderer = platform.BeginFrame();
                foreach (string file in files) renderer.DrawStretched(cache.Texture(file), new RectF(0, 0, size, size), new RectF(0, 0, 64, 64), Color.White);
                platform.EndFrame();
            }
        else if (device is not null)
        {
            // Device textures, through a batch of the benchmark's own into a small target.
            using var batcher = new Renderer2D(device);
            using var target = device.CreateRenderTarget(new Dev.RenderTargetDescription(64, 64));
            for (int i = 0; i < 3; i++)
            {
                batcher.Begin(new BatchSettings(target, Sampler: Dev.SamplerDescription.LinearClamp));
                foreach (var texture in keep) batcher.Draw((Dev.Texture)texture, new RectF(0, 0, size, size), System.Numerics.Matrix3x2.CreateScale(64f / size), ColorF.White);
                batcher.End();
                platform.BeginFrame();
                platform.EndFrame();
            }
        }
        Thread.Sleep(500);
        long gpu = GpuMemory.CurrentUsage() - gpuBefore;
        using var process = Process.GetCurrentProcess();
        long texels = (long)files.Count * Dev.TextureFormats.ByteCount(format, size, size);
        long disk = files.Sum(f => new FileInfo(Path.Combine(scratch, f)).Length);
        Console.WriteLine($"| {path} | {described} | {files.Count} × {size}² | {Bytes(disk)} | {elapsed:0} ms | {frames} | {longestFrame:0} ms | {Bytes(allocated)} | {gen0}/{gen1}/{gen2} | {pause:0.0} ms | {Bytes(process.PeakWorkingSet64)} | {Bytes(texels)} | {(gpuBefore < 0 ? "n/a" : Bytes(gpu))} |");
        foreach (var texture in keep) texture.Dispose();
    }

    internal const string LevelHeader =
        "| Path | Device | Pages | On disk | Loading screen | Frames | Longest frame | Allocated | GC 0/1/2 | GC pauses | Peak working set | Texture bytes (computed) | GPU memory (measured) |\n" +
        "| --- | --- | --- | ---: | ---: | ---: | ---: | ---: | --- | ---: | ---: | ---: | ---: |";

    // ---- shared

    internal static IPlatform Open(string deviceName, out string described)
    {
        if (deviceName == "headless")
        {
            described = "headless";
            return HeadlessPlatform.For(new GameSettings("ContentBench", 1280, 720, 1));
        }
        var settings = new GameSettings("ContentBench", 1280, 720, 1) { Presentation = Presentation.HighResolution2D, Graphics = new GraphicsSettings { Device = deviceName } };
        var platform = SdlPlatform.Profile.Create(new PlatformRequest(settings) { Hidden = true, WaitForDisplay = false, Mute = true });
        string api = platform.Graphics is DeviceGraphics g ? $"{g.Device.Capabilities.ApiName}, {g.Device.Capabilities.Adapter}" : "?";
        described = $"{deviceName} ({api})";
        return platform;
    }

    internal static string Machine() => $".NET {Environment.Version}, {Environment.ProcessorCount} logical cores, {(System.Runtime.GCSettings.IsServerGC ? "server" : "workstation")} GC";

    private static void Row(string file, string stage, int runs, Action action)
    {
        action();
        var times = new List<double>();
        var allocations = new List<long>();
        int gen0 = 0, gen1 = 0, gen2 = 0;
        double pause = 0;
        for (int i = 0; i < runs; i++)
        {
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            int before0 = GC.CollectionCount(0), before1 = GC.CollectionCount(1), before2 = GC.CollectionCount(2);
            var pauseBefore = GC.GetTotalPauseDuration();
            long allocatedBefore = GC.GetTotalAllocatedBytes(true);
            long start = Stopwatch.GetTimestamp();
            action();
            times.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            allocations.Add(GC.GetTotalAllocatedBytes(true) - allocatedBefore);
            pause += (GC.GetTotalPauseDuration() - pauseBefore).TotalMilliseconds;
            gen0 += GC.CollectionCount(0) - before0; gen1 += GC.CollectionCount(1) - before1; gen2 += GC.CollectionCount(2) - before2;
        }
        times.Sort();
        allocations.Sort();
        Console.WriteLine($"| {file} | {stage} | {times[times.Count / 2]:0.00} ms | {Bytes(allocations[allocations.Count / 2])} | {gen0 / (double)runs:0.#}/{gen1 / (double)runs:0.#}/{gen2 / (double)runs:0.#} | {pause / runs:0.00} ms |");
    }

    internal static string Bytes(long b) => b >= 1 << 20 ? $"{b / 1048576.0:0.0} MB" : b >= 1024 ? $"{b / 1024.0:0.0} KB" : $"{b} B";
}

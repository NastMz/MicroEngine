using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Sdl;

// Times the asset pipeline with the AssetGallery sample's files (ASSET-1's measurements):
//
//   AssetBench [--content DIR] [--headless] [--runs N]
//
// Uploads go to a hidden SDL window (real textures) unless --headless or no display is available. Each timing is the
// median of N runs (5 by default) after one warm-up run; allocations are measured on this thread.
string? content = null;
bool headless = false;
int runs = 5;
for (int i = 0; i < args.Length; i++)
    switch (args[i])
    {
        case "--content": content = args[++i]; break;
        case "--headless": headless = true; break;
        case "--runs": runs = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
    }
content ??= FindGallery();
var settings = new GameSettings("AssetBench", 1280, 720, 1) { Presentation = Presentation.HighResolution2D };
IPlatform platform;
string device;
try
{
    if (headless) throw new InvalidOperationException("--headless");
    platform = SdlPlatform.Profile.Create(new PlatformRequest(settings) { Hidden = true, WaitForDisplay = false, Mute = true });
    device = "SDL " + (platform.TryGetCapability<IPlatformDiagnostics>(out var facts) ? facts.Diagnostics.GetValueOrDefault("renderer", "?") : "?");
}
catch (InvalidOperationException e)
{
    platform = HeadlessPlatform.For(settings);
    device = $"headless ({e.Message})";
}
using var _ = platform;
var graphics = platform.Graphics;
graphics.DefaultFilter = TextureFilter.Linear;
var folder = new ContentFolder(content);
Console.WriteLine($"# AssetBench: {device}, .NET {Environment.Version}, {Environment.ProcessorCount} cores, {runs} runs (median)");
Console.WriteLine();
Console.WriteLine("| Measurement | Median | Allocated |");
Console.WriteLine("| --- | ---: | ---: |");

void Row(string name, Action action, string unit = "ms", double divide = 1)
{
    action();
    var times = new List<double>();
    long allocated = 0;
    for (int i = 0; i < runs; i++)
    {
        GC.Collect();
        long before = GC.GetAllocatedBytesForCurrentThread();
        long start = Stopwatch.GetTimestamp();
        action();
        times.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds / divide);
        allocated = (GC.GetAllocatedBytesForCurrentThread() - before) / (long)divide;
    }
    times.Sort();
    string value = unit == "µs" ? string.Create(CultureInfo.InvariantCulture, $"{times[times.Count / 2] * 1000:0.0} µs") : string.Create(CultureInfo.InvariantCulture, $"{times[times.Count / 2]:0.00} ms");
    Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"| {name} | {value} | {Bytes(allocated)} |"));
}

static string Bytes(long b) => b >= 1 << 20 ? string.Create(CultureInfo.InvariantCulture, $"{b / 1048576.0:0.0} MB") : b >= 1024 ? string.Create(CultureInfo.InvariantCulture, $"{b / 1024.0:0.0} KB") : $"{b} B";

// Decoding.
var players = File.ReadAllBytes(folder.PathOf("atlases/players.png"));
var tiles = File.ReadAllBytes(folder.PathOf("atlases/tiles.png"));
var jpeg = File.ReadAllBytes(folder.PathOf("backgrounds/set2_background.jpg"));
Row("PNG decode, 512×512 RGBA (PixelImage.Decode)", () => PixelImage.Decode(players));
Row("PNG decode, 512×1024 RGBA (PixelImage.Decode)", () => PixelImage.Decode(tiles));
Row("PNG decode, 512×1024 RGBA (StbImageSharp alone)", () => StbImageSharp.ImageResult.FromMemory(tiles, StbImageSharp.ColorComponents.RedGreenBlueAlpha));
Row("JPEG decode, 640×480 (PixelImage.Decode)", () => PixelImage.Decode(jpeg));

// Uploads.
var image1024 = new PixelImage(1024, 1024).Clear(new Color(200, 100, 50));
var image2048 = new PixelImage(2048, 2048).Clear(new Color(200, 100, 50));
Row("Texture upload, 1024×1024 (4 MB)", () => graphics.CreateTexture(image1024).Dispose());
Row("Texture upload, 2048×2048 (16 MB)", () => graphics.CreateTexture(image2048).Dispose());
var page = graphics.CreateTexture(new PixelImage(512, 512));
var glyph = new Color[32 * 40];
Row("Texture region update, 32×40 (one glyph)", () => page.Update(10, 10, 32, 40, glyph), "µs");

// Atlases and the cache.
Row("Atlas load, Starling XML + 512×512 PNG", () => { using var cache = new AssetCache(graphics, content); cache.Atlas("atlases/players.xml"); });
Row("Atlas load, Aseprite JSON + 256×256 PNG", () => { using var cache = new AssetCache(graphics, content); cache.Atlas("atlases/enemies.json"); });
Row("Folder atlas, 10 PNGs packed at load", () => { using var cache = new AssetCache(graphics, content); cache.AtlasFromFolder("items"); });
using (var cache = new AssetCache(graphics, content))
{
    cache.Texture("items/blueGem.png");
    Row("Cache hit, Texture(path), per lookup", () => { for (int i = 0; i < 10_000; i++) cache.Texture("items/blueGem.png"); }, "µs", 10_000);
}

// Fonts.
var inter = File.ReadAllBytes(folder.PathOf("fonts/Inter-Regular.ttf"));
var serif = File.ReadAllBytes(folder.PathOf("fonts/SourceSerif4-Regular.otf"));
Row("Font open, Inter TTF (412 KB)", () => ScalableFont.FromBytes(graphics, inter).Dispose());
Row("Font open, Source Serif OTF/CFF (241 KB)", () => ScalableFont.FromBytes(graphics, serif).Dispose());
Row("Prepare ASCII at 24 px, TTF (95 glyphs)", () => { using var f = ScalableFont.FromBytes(graphics, inter); f.Prepare(ScalableFont.Ascii, 24); });
Row("Prepare ASCII at 64 px, TTF (95 glyphs)", () => { using var f = ScalableFont.FromBytes(graphics, inter); f.Prepare(ScalableFont.Ascii, 64); });
Row("Prepare ASCII at 24 px, OTF/CFF (95 glyphs)", () => { using var f = ScalableFont.FromBytes(graphics, serif); f.Prepare(ScalableFont.Ascii, 24); });
using (var font = ScalableFont.FromBytes(graphics, inter))
{
    font.Prepare(ScalableFont.Ascii, 24);
    var renderer = platform.BeginFrame();
    const string line = "Score 0123456789 - The quick brown fox";
    Row("Draw a prepared 38-character line, per call", () => { for (int i = 0; i < 1000; i++) font.Draw(renderer, line, new Vector2(10, 10), 24, Color.White); }, "µs", 1000);
    Row("Measure a 38-character line, per call", () => { for (int i = 0; i < 1000; i++) font.Measure(line, 24); }, "µs", 1000);
    platform.EndFrame();
    Console.WriteLine();
    Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Glyph pages after ASCII at 24 px: {font.Pages} ({Bytes(font.TextureBytes)}), glyphs rasterized: {font.GlyphsRasterized}"));
}

static string FindGallery()
{
    for (var dir = AppContext.BaseDirectory; dir is not null; dir = Path.GetDirectoryName(dir))
        if (Directory.Exists(Path.Combine(dir, "samples", "AssetGallery", "Content"))) return Path.Combine(dir, "samples", "AssetGallery", "Content");
    throw new DirectoryNotFoundException("samples/AssetGallery/Content not found; pass --content DIR.");
}

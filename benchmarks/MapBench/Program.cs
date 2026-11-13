using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Numerics;
using System.Text.Json;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Tiled;

// Times large Tiled maps (TILE-1's measurements):
//
//   MapBench [--size N] [--layers L] [--runs R]
//
// Writes an N × N map of 16-pixel tiles (1000 × 1000 by default) with L tile layers (3), each base64 and zlib
// compressed as Tiled saves it, then times loading it through the asset cache (parsing, decompressing, building the
// layers) and one frame of drawing every layer with a 320 × 180 camera at zoom 1 and zoom 0.5, on the headless
// renderer: the CPU cost of culling and submitting, not the GPU's. Each timing is the median of R runs (5) after one
// warm-up run; allocations are measured on this thread.
int size = 1000, layers = 3, runs = 5;
for (int i = 0; i < args.Length; i++)
    switch (args[i])
    {
        case "--size": size = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
        case "--layers": layers = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
        case "--runs": runs = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
    }

var folder = Directory.CreateTempSubdirectory("microengine-mapbench");
try
{
    long fileBytes = WriteMap(folder.FullName, size, layers);
    Console.WriteLine($"# MapBench: {size} × {size} tiles, {layers} layers, file {fileBytes / 1024.0:0} KB, .NET {Environment.Version}, {runs} runs (median)");
    Console.WriteLine();
    Console.WriteLine("| Measurement | Median | Allocated |");
    Console.WriteLine("| --- | ---: | ---: |");

    var device = new HeadlessGraphicsDevice();
    TiledMap map = null!;
    Row("Load the map (parse, decompress, build layers)", () =>
    {
        using var assets = new AssetCache(device, folder.FullName);
        map = assets.TiledMap("map.tmj");
    });
    var keep = new AssetCache(device, folder.FullName);
    map = keep.TiledMap("map.tmj");
    var sprites = new TiledSprites(map, keep);
    var renderer = new HeadlessRenderer(320, 180);
    foreach (float zoom in new[] { 1f, 0.5f })
    {
        var camera = new Camera2D(320, 180) { Zoom = zoom };
        camera.CenterOn(new Vector2(map.PixelWidth / 2f, map.PixelHeight / 2f));
        renderer.Reset();
        sprites.DrawAll(renderer, camera);
        int draws = renderer.DrawCalls;
        Row(string.Create(CultureInfo.InvariantCulture, $"Draw one frame at zoom {zoom} ({draws} tiles)"), () =>
        {
            renderer.Reset();
            sprites.DrawAll(renderer, camera);
        }, "µs", 1000);
    }
    long cells = (long)size * size * layers;
    Console.WriteLine();
    Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Layer memory: {cells * (sizeof(int) + 1) / (1024.0 * 1024):0.0} MB ({cells:N0} cells: an id and an orientation each)."));
}
finally { folder.Delete(recursive: true); }

void Row(string name, Action action, string unit = "ms", double scale = 1)
{
    action();
    var times = new List<double>();
    long allocated = 0;
    for (int i = 0; i < runs; i++)
    {
        long before = GC.GetAllocatedBytesForCurrentThread();
        var watch = Stopwatch.StartNew();
        action();
        times.Add(watch.Elapsed.TotalMilliseconds * scale);
        allocated = GC.GetAllocatedBytesForCurrentThread() - before;
    }
    times.Sort();
    Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"| {name} | {times[times.Count / 2]:0.##} {unit} | {allocated / 1024.0:0} KB |"));
}

// A map as Tiled saves it: one embedded tileset over a 64 × 64 image of four tiles, and every layer filled with a
// pattern of all four (empty cells included), base64 over zlib.
static long WriteMap(string folder, int size, int layers)
{
    var image = new PixelImage(64, 64);
    for (int y = 0; y < 64; y++)
        for (int x = 0; x < 64; x++) image[x, y] = new Color((byte)(x * 4), (byte)(y * 4), 128);
    File.WriteAllBytes(Path.Combine(folder, "tiles.png"), PngEncoder.Encode(image.ToRgba(), image.Width, image.Height));
    var layerList = new List<object>();
    for (int l = 0; l < layers; l++)
    {
        var raw = new byte[size * size * 4];
        for (int i = 0; i < size * size; i++) BitConverter.TryWriteBytes(raw.AsSpan(i * 4), (uint)((i * 7 + l * 3) % 17 % 5));
        using var packed = new MemoryStream();
        using (var z = new ZLibStream(packed, CompressionLevel.Fastest)) z.Write(raw);
        layerList.Add(new Dictionary<string, object>
        {
            ["type"] = "tilelayer", ["id"] = l + 1, ["name"] = $"layer{l}", ["x"] = 0, ["y"] = 0, ["width"] = size, ["height"] = size,
            ["opacity"] = 1, ["visible"] = true, ["encoding"] = "base64", ["compression"] = "zlib", ["data"] = Convert.ToBase64String(packed.ToArray()),
        });
    }
    var map = new Dictionary<string, object>
    {
        ["type"] = "map", ["version"] = "1.10", ["tiledversion"] = "1.11.0", ["orientation"] = "orthogonal", ["renderorder"] = "right-down",
        ["width"] = size, ["height"] = size, ["tilewidth"] = 16, ["tileheight"] = 16, ["infinite"] = false, ["compressionlevel"] = -1,
        ["nextlayerid"] = layers + 1, ["nextobjectid"] = 1,
        ["tilesets"] = new object[] { new Dictionary<string, object>
        {
            ["firstgid"] = 1, ["name"] = "tiles", ["tilewidth"] = 16, ["tileheight"] = 16, ["tilecount"] = 16, ["columns"] = 4,
            ["image"] = "tiles.png", ["imagewidth"] = 64, ["imageheight"] = 64, ["margin"] = 0, ["spacing"] = 0,
        } },
        ["layers"] = layerList,
    };
    string path = Path.Combine(folder, "map.tmj");
    File.WriteAllText(path, JsonSerializer.Serialize(map));
    return new FileInfo(path).Length;
}

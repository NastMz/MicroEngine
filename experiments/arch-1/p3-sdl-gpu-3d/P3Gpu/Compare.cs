using SDL;
using static SDL.SDL3;

namespace P3Gpu;

/// <summary>
/// <c>P3Gpu compare a.png b.png</c>: counts differing pixels and classifies them as edge pixels (a pixel whose
/// 3x3 neighbourhood in <c>a</c> is not uniform) or interior pixels. Used to compare adapters and backends.
/// </summary>
public static unsafe class Compare
{
    public static int Run(string[] files)
    {
        if (files.Length == 0 || files.Length % 2 != 0) { Console.Error.WriteLine("usage: P3Gpu compare a.png b.png [c.png d.png ...]"); return 100; }
        var failures = 0;
        for (var i = 0; i < files.Length; i += 2) failures += Pair(files[i], files[i + 1]);
        return failures;
    }

    private static int Pair(string first, string second)
    {
        string[] files = [first, second];
        var a = Load(files[0]);
        var b = Load(files[1]);
        if (a.Width != b.Width || a.Height != b.Height) { Console.WriteLine("size differs"); return 1; }
        int differing = 0, edge = 0, maxDelta = 0;
        for (var y = 0; y < a.Height; y++)
        for (var x = 0; x < a.Width; x++)
        {
            Rgba p = a[x, y], q = b[x, y];
            if (p == q) continue;
            differing++;
            maxDelta = Math.Max(maxDelta, new[] { Math.Abs(p.R - q.R), Math.Abs(p.G - q.G), Math.Abs(p.B - q.B), Math.Abs(p.A - q.A) }.Max());
            if (IsEdge(a, x, y)) edge++;
        }
        Console.WriteLine($"{Path.GetFileName(files[0])} vs {Path.GetFileName(files[1])}: {differing} differing pixels " +
                          $"({edge} on edges, {differing - edge} interior), max channel delta {maxDelta}");
        return differing - edge == 0 ? 0 : 1;
    }

    private static bool IsEdge(Image image, int x, int y)
    {
        var centre = image[x, y];
        for (var dy = -1; dy <= 1; dy++)
        for (var dx = -1; dx <= 1; dx++)
        {
            int nx = x + dx, ny = y + dy;
            if (nx < 0 || ny < 0 || nx >= image.Width || ny >= image.Height) continue;
            if (image[nx, ny] != centre) return true;
        }
        return false;
    }

    private static Image Load(string path)
    {
        var surface = SDL_LoadPNG(path);
        if (surface == null) throw new InvalidOperationException($"SDL_LoadPNG {path}: {SDL_GetError()}");
        var rgba = SDL_ConvertSurface(surface, SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888);
        SDL_DestroySurface(surface);
        var bytes = new byte[rgba->w * rgba->h * 4];
        for (var y = 0; y < rgba->h; y++)
            new ReadOnlySpan<byte>((byte*)rgba->pixels + y * rgba->pitch, rgba->w * 4).CopyTo(bytes.AsSpan(y * rgba->w * 4));
        var image = new Image(rgba->w, rgba->h, bytes);
        SDL_DestroySurface(rgba);
        return image;
    }
}

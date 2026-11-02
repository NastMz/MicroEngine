using System.Numerics;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Raylib;
using MicroEngine.Sdl;

// Rendering probes for the ARCH-1 audit (A2). Each probe runs on every backend profile it is given, in a hidden window.
string outDir = args.Length > 0 ? args[0] : "out";
string which = args.Length > 1 ? args[1] : "sdl";
Directory.CreateDirectory(outDir);

PlatformProfile profile = which switch
{
    "raylib" => RaylibPlatform.Profile,
    "sdl" => SdlPlatform.Profile,
    _ => SdlPlatform.CreateProfile(which),
};

var settings = new GameSettings("probe", 64, 16) { Scaling = CanvasScaling.Pixel };
var request = new PlatformRequest(settings) { Hidden = true, FixedStep = true, Mute = true, CaptureScale = 1 };
using var platform = profile.Create(request);
Console.WriteLine($"== {profile.Name} renderer={(platform as IPlatformDiagnostics)?.Diagnostics.GetValueOrDefault("renderer")}");

string Shot(string name)
{
    platform.PollEvents();
    var p = Path.Combine(outDir, $"{which}-{name}.png");
    platform.SaveScreenshot(p);
    return p;
}

// --- Probe 1: straight-alpha bilinear fringe. A 2x1 texture: opaque red | transparent black, linear, stretched 32x.
{
    var img = new PixelImage(2, 1);
    img[0, 0] = new Color(255, 0, 0, 255);
    img[1, 0] = new Color(0, 0, 0, 0);
    using var tex = platform.Graphics.CreateTexture(img, TextureFilter.Linear);
    var r = platform.BeginFrame();
    r.Clear(Color.White);
    r.DrawStretched(tex, new RectF(0, 0, 2, 1), new RectF(0, 0, 64, 16), Color.White);
    platform.EndFrame();
    var shot = PixelImage.Load(Shot("fringe"));
    // Ideal (premultiplied) result at the midpoint (alpha 0.5 over white): (255,128,128). Straight alpha: ~(191,128,128).
    var row = Enumerable.Range(0, 64).Select(x => shot[x, 8]).ToArray();
    var mid = row[32];
    int minR = row.Min(c => c.R);
    Console.WriteLine($"fringe: mid={mid} minR={minR} (premultiplied ideal keeps R=255 everywhere; R<255 means dark fringe)");
}

// --- Probe 2: SetClip then Clear. Does Clear respect the clip rectangle?
{
    var r = platform.BeginFrame();
    r.Clear(Color.White);
    r.SetClip(new RectF(0, 0, 8, 8));
    r.Clear(new Color(255, 0, 0));
    r.SetClip(null);
    platform.EndFrame();
    var shot = PixelImage.Load(Shot("clipclear"));
    int red = 0;
    for (int y = 0; y < shot.Height; y++) for (int x = 0; x < shot.Width; x++) if (shot[x, y].G < 128) red++;
    Console.WriteLine($"clip+clear: red pixels={red} of {shot.Width * shot.Height} (64 if Clear respects the clip)");
}

// --- Probe 3: drawing a disposed texture. Headless throws ObjectDisposedException; what do real backends do?
{
    var img = new PixelImage(4, 4).Clear(new Color(0, 0, 255));
    var tex = platform.Graphics.CreateTexture(img);
    tex.Dispose();
    try
    {
        var r = platform.BeginFrame();
        r.Clear(Color.White);
        r.Draw(tex, tex.Bounds, Vector2.Zero, Color.White);
        platform.EndFrame();
        var shot = PixelImage.Load(Shot("disposed"));
        Console.WriteLine($"disposed draw: no exception; pixel(1,1)={shot[1, 1]}");
    }
    catch (Exception e) { Console.WriteLine($"disposed draw: {e.GetType().Name}: {e.Message}"); try { platform.EndFrame(); } catch { } }
}

// --- Probe 4: a texture from another device (headless) drawn by this backend's renderer.
{
    var foreign = new HeadlessGraphicsDevice().CreateTexture(new PixelImage(4, 4));
    try
    {
        var r = platform.BeginFrame();
        r.Draw(foreign, foreign.Bounds, Vector2.Zero, Color.White);
        platform.EndFrame();
        Console.WriteLine("foreign texture: no exception");
    }
    catch (Exception e) { Console.WriteLine($"foreign texture: {e.GetType().Name}: {e.Message.Split('\n')[0]}"); try { platform.EndFrame(); } catch { } }
}

// --- Probe 5: sub-pixel placement. A 1x1 white texture (nearest) and a FillRect at x=10.5 on black; which pixels light?
{
    using var tex = platform.Graphics.CreateTexture(new PixelImage(1, 1).Clear(Color.White), TextureFilter.Nearest);
    var r = platform.BeginFrame();
    r.Clear(Color.Black);
    r.Draw(tex, tex.Bounds, new Vector2(10.5f, 2.5f), Color.White);
    r.FillRect(new RectF(20.5f, 2.5f, 1, 1), Color.White);
    r.FillCircle(new Vector2(40, 8), 3.5f, Color.White);
    r.DrawLine(new Vector2(50, 2), new Vector2(60, 12), 1, Color.White);
    platform.EndFrame();
    var shot = PixelImage.Load(Shot("subpixel"));
    string Lit(int x0, int x1, int y0, int y1)
    {
        var lit = new List<string>();
        for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++) if (shot[x, y].R > 0) lit.Add($"{x},{y}:{shot[x, y].R}");
        return string.Join(" ", lit);
    }
    Console.WriteLine($"subpixel sprite@10.5,2.5: {Lit(8, 13, 0, 5)}");
    Console.WriteLine($"subpixel rect@20.5,2.5: {Lit(18, 23, 0, 5)}");
    int circle = 0, line = 0;
    for (int y = 0; y < 16; y++) { for (int x = 34; x < 47; x++) if (shot[x, y].R > 127) circle++; for (int x = 48; x < 63; x++) if (shot[x, y].R > 127) line++; }
    Console.WriteLine($"circle r=3.5 lit={circle}; line lit={line}");
}

// --- Probe 6: blend mode Multiply with alpha and Additive over a mid-grey.
{
    var r = platform.BeginFrame();
    r.Clear(new Color(128, 128, 128));
    r.SetBlendMode(BlendMode.Multiply);
    r.FillRect(new RectF(0, 0, 16, 16), new Color(255, 0, 0, 128));
    r.SetBlendMode(BlendMode.Additive);
    r.FillRect(new RectF(16, 0, 16, 16), new Color(100, 0, 0, 128));
    r.SetBlendMode(BlendMode.Alpha);
    platform.EndFrame();
    var shot = PixelImage.Load(Shot("blend"));
    Console.WriteLine($"multiply(255,0,0,128) over 128: {shot[8, 8]}; additive(100,0,0,128) over 128: {shot[24, 8]}");
}

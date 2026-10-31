using System.Runtime.InteropServices.JavaScript;
using Raylib_cs;

// Probe: init a raylib window on the page canvas, draw primitives and a texture, read keys,
// and drive frames from the browser's requestAnimationFrame instead of a blocking loop.
Console.WriteLine("probe: main");
Probe.Init();

public static partial class Probe
{
    private static Texture2D _tex;
    private static float _x = 40;
    private static int _frames;

    public static void Init()
    {
        Raylib.InitWindow(640, 360, "probe");
        var img = Raylib.GenImageChecked(32, 32, 8, 8, Color.Red, Color.Yellow);
        _tex = Raylib.LoadTextureFromImage(img);
        Raylib.UnloadImage(img);
        Console.WriteLine($"probe: window ready {Raylib.GetScreenWidth()}x{Raylib.GetScreenHeight()} tex={_tex.Width}");
    }

    [JSExport]
    public static void Frame(double dt)
    {
        if (Raylib.IsKeyDown(KeyboardKey.Right)) _x += 120 * (float)dt;
        if (Raylib.IsKeyDown(KeyboardKey.Left)) _x -= 120 * (float)dt;
        Raylib.BeginDrawing();
        Raylib.ClearBackground(new Color(20, 20, 40, 255));
        Raylib.DrawRectangle(10, 10, 100, 50, Color.SkyBlue);
        Raylib.DrawCircle(320, 180, 30, Color.Green);
        Raylib.DrawTexture(_tex, (int)_x, 200, Color.White);
        Raylib.EndDrawing();
        if (++_frames % 120 == 0) Console.WriteLine($"probe: frame {_frames} x={_x:0}");
    }
}

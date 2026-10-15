using System.Diagnostics;
using MicroEngine.Loop;
using Raylib_cs;

namespace MicroEngine.Desktop;

internal sealed class RaylibApi : IDesktopApi
{
    private long _last;
    public bool Ready => Raylib.IsWindowReady();
    public void Init(DesktopWindowOptions options)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Desktop validation currently supports Windows only.");
        Raylib.InitWindow(options.Width, options.Height, options.Title);
        Raylib.SetTargetFPS(60);
        _last = Stopwatch.GetTimestamp();
    }
    public NativeFrame Read()
    {
        var now = Stopwatch.GetTimestamp();
        var elapsed = Stopwatch.GetElapsedTime(_last, now);
        _last = now;
        var held = new List<string>(); var pressed = new List<string>();
        Add("left", KeyboardKey.Left, KeyboardKey.A);
        Add("right", KeyboardKey.Right, KeyboardKey.D);
        Add("jump", KeyboardKey.Space, KeyboardKey.Space);
        return new(elapsed, Raylib.IsWindowFocused() && !Raylib.IsWindowMinimized(), Raylib.WindowShouldClose(), held.ToArray(), pressed.ToArray());

        void Add(string action, KeyboardKey first, KeyboardKey second)
        {
            if (Raylib.IsKeyDown(first) || Raylib.IsKeyDown(second)) held.Add(action);
            if (Raylib.IsKeyPressed(first) || Raylib.IsKeyPressed(second)) pressed.Add(action);
        }
    }
    public void Begin() => Raylib.BeginDrawing();
    public void End() => Raylib.EndDrawing();
    public void Clear(Rgba color) => Raylib.ClearBackground(new Color(color.R, color.G, color.B, color.A));
    public void Fill(Rect rectangle, Rgba color) => Raylib.DrawRectangleRec(new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height), new Color(color.R, color.G, color.B, color.A));
    public void Close() => Raylib.CloseWindow();
}

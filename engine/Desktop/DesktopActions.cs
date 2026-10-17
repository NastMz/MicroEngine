using Raylib_cs;

namespace MicroEngine.Desktop;

internal static class DesktopActions
{
    internal static (string[] Held, string[] Pressed) Read(Func<KeyboardKey, bool> held, Func<KeyboardKey, bool> pressed)
    {
        var down = new List<string>(); var edges = new List<string>();
        Add("left", KeyboardKey.Left, KeyboardKey.A);
        Add("right", KeyboardKey.Right, KeyboardKey.D);
        Add("jump", KeyboardKey.Space, KeyboardKey.Space);
        Add("up", KeyboardKey.Up, KeyboardKey.W);
        Add("down", KeyboardKey.Down, KeyboardKey.S);
        return (down.ToArray(), edges.ToArray());

        void Add(string action, KeyboardKey first, KeyboardKey second)
        {
            if (held(first) || (second != first && held(second))) down.Add(action);
            if (pressed(first) || (second != first && pressed(second))) edges.Add(action);
        }
    }
}

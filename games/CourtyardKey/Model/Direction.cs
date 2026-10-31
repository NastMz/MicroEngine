using System.Numerics;

namespace CourtyardKey.Model;

/// <summary>The four cardinal facings used by every actor, door and room edge.</summary>
public enum Dir { Down, Up, Left, Right }

public static class DirExtensions
{
    public static Vector2 ToVector(this Dir d) => d switch
    {
        Dir.Down => new Vector2(0, 1),
        Dir.Up => new Vector2(0, -1),
        Dir.Left => new Vector2(-1, 0),
        _ => new Vector2(1, 0),
    };

    public static (int X, int Y) ToStep(this Dir d) => d switch
    {
        Dir.Down => (0, 1),
        Dir.Up => (0, -1),
        Dir.Left => (-1, 0),
        _ => (1, 0),
    };

    public static Dir Opposite(this Dir d) => d switch
    {
        Dir.Down => Dir.Up,
        Dir.Up => Dir.Down,
        Dir.Left => Dir.Right,
        _ => Dir.Left,
    };

    public static bool IsHorizontal(this Dir d) => d is Dir.Left or Dir.Right;

    /// <summary>The dominant cardinal direction of a vector (ties favour the vertical axis).</summary>
    public static Dir FromVector(Vector2 v)
        => MathF.Abs(v.X) > MathF.Abs(v.Y) ? (v.X < 0 ? Dir.Left : Dir.Right) : (v.Y < 0 ? Dir.Up : Dir.Down);

    /// <summary>Rotation in radians that turns a "pointing up" sprite to face this direction.</summary>
    public static float Angle(this Dir d) => d switch
    {
        Dir.Up => 0,
        Dir.Right => MathF.PI / 2,
        Dir.Down => MathF.PI,
        _ => -MathF.PI / 2,
    };
}

using System.Numerics;

namespace MicroEngine.Scenes;

/// <summary>
/// Position, rotation and scale relative to the parent (or to the world for a root entity). Stored in 3D; 2D games use
/// the <c>2D</c> helpers, which read and write X/Y and the rotation about Z. <c>default</c> is degenerate (zero scale and
/// a zero quaternion): entities start at <see cref="Identity"/>.
/// </summary>
public struct Transform
{
    /// <summary>Translation.</summary>
    public Vector3 Position;
    /// <summary>Rotation (unit quaternion).</summary>
    public Quaternion Rotation;
    /// <summary>Scale per axis.</summary>
    public Vector3 Scale;

    /// <summary>No translation, no rotation, unit scale.</summary>
    public static Transform Identity => new() { Rotation = Quaternion.Identity, Scale = Vector3.One };

    /// <summary>X and Y of <see cref="Position"/>; writing keeps Z. Exact: a written value reads back bit for bit.</summary>
    public Vector2 Position2D
    {
        readonly get => new(Position.X, Position.Y);
        set { Position.X = value.X; Position.Y = value.Y; }
    }

    /// <summary>
    /// The rotation about Z in radians. Writing replaces <see cref="Rotation"/>; reading converts back from the
    /// quaternion, so a written angle reads back within a few ulps, not bit for bit (keep the angle in game state when
    /// the exact value matters).
    /// </summary>
    public float Rotation2D
    {
        readonly get => 2 * MathF.Atan2(Rotation.Z, Rotation.W);
        set => Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, value);
    }

    /// <summary>X and Y of <see cref="Scale"/>; writing keeps Z.</summary>
    public Vector2 Scale2D
    {
        readonly get => new(Scale.X, Scale.Y);
        set { Scale.X = value.X; Scale.Y = value.Y; }
    }

    /// <summary>The matrix that maps this transform's space to its parent's (scale, then rotate, then translate).</summary>
    public readonly Matrix4x4 ToMatrix()
    {
        var m = Matrix4x4.CreateFromQuaternion(Rotation);
        if (Scale != Vector3.One)
        {
            m.M11 *= Scale.X; m.M12 *= Scale.X; m.M13 *= Scale.X;
            m.M21 *= Scale.Y; m.M22 *= Scale.Y; m.M23 *= Scale.Y;
            m.M31 *= Scale.Z; m.M32 *= Scale.Z; m.M33 *= Scale.Z;
        }
        m.Translation = Position;
        return m;
    }

    /// <summary>
    /// The transform <paramref name="t"/> of the way from <paramref name="from"/> to <paramref name="to"/>: positions and
    /// scales lerp, rotations slerp. Returns <paramref name="to"/> exactly when <paramref name="t"/> is 1 or more or the
    /// two are equal, so a fixed-step run draws exactly the simulated state.
    /// </summary>
    public static Transform Interpolate(in Transform from, in Transform to, float t)
    {
        if (t >= 1 || Equal(from, to)) return to;
        return new Transform
        {
            Position = from.Position == to.Position ? to.Position : Vector3.Lerp(from.Position, to.Position, t),
            Rotation = from.Rotation == to.Rotation ? to.Rotation : Quaternion.Slerp(from.Rotation, to.Rotation, t),
            Scale = from.Scale == to.Scale ? to.Scale : Vector3.Lerp(from.Scale, to.Scale, t),
        };
    }

    internal static bool Equal(in Transform a, in Transform b) =>
        a.Position == b.Position && a.Rotation == b.Rotation && a.Scale == b.Scale;

    internal readonly bool TranslationOnly => Rotation == Quaternion.Identity && Scale == Vector3.One;
}

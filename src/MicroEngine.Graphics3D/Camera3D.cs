using System.Numerics;

namespace MicroEngine.Graphics3D;

/// <summary>
/// A perspective camera with the engine's conventions (System.Numerics, right-handed): it looks from
/// <see cref="Position"/> towards <see cref="Target"/> with <see cref="Up"/> up, and projects with a vertical field of
/// view onto depth 0 (<see cref="Near"/>) .. 1 (<see cref="Far"/>). <see cref="View"/> and <see cref="Projection"/> are
/// <c>Matrix4x4.CreateLookAt</c> and <c>Matrix4x4.CreatePerspectiveFieldOfView</c>; a point maps to clip space as
/// <c>Vector4.Transform(p, View * Projection)</c>.
/// </summary>
public sealed class Camera3D
{
    private float _fieldOfView = MathF.PI / 3, _near = 0.1f, _far = 100;

    /// <summary>Where the camera is (world space).</summary>
    public Vector3 Position { get; set; } = new(0, 0, 5);

    /// <summary>The point it looks at (world space); must differ from <see cref="Position"/>.</summary>
    public Vector3 Target { get; set; } = Vector3.Zero;

    /// <summary>The world direction that is up on screen; not parallel to the view direction.</summary>
    public Vector3 Up { get; set; } = Vector3.UnitY;

    /// <summary>The vertical field of view in radians, in (0, π).</summary>
    /// <exception cref="ArgumentOutOfRangeException">Outside (0, π).</exception>
    public float FieldOfView
    {
        get => _fieldOfView;
        set => _fieldOfView = value is > 0 and < MathF.PI ? value : throw new ArgumentOutOfRangeException(nameof(value), value, "the field of view is an angle in (0, π) radians");
    }

    /// <summary>The near plane distance, maps to depth 0; positive and less than <see cref="Far"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Not positive.</exception>
    public float Near
    {
        get => _near;
        set => _near = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), value, "the near plane is a positive distance");
    }

    /// <summary>The far plane distance, maps to depth 1; greater than <see cref="Near"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Not positive.</exception>
    public float Far
    {
        get => _far;
        set => _far = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), value, "the far plane is a positive distance");
    }

    /// <summary>World to view space.</summary>
    public Matrix4x4 View => Matrix4x4.CreateLookAt(Position, Target, Up);

    /// <summary>View to clip space for a viewport of <paramref name="aspect"/> = width / height.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="aspect"/> is not positive, or <see cref="Near"/> is not less than <see cref="Far"/>.</exception>
    public Matrix4x4 Projection(float aspect)
    {
        if (!(aspect > 0)) throw new ArgumentOutOfRangeException(nameof(aspect), aspect, "the aspect ratio is width / height, positive");
        if (_near >= _far) throw new ArgumentOutOfRangeException(nameof(Near), $"the near plane ({_near}) must be closer than the far plane ({_far})");
        return Matrix4x4.CreatePerspectiveFieldOfView(_fieldOfView, aspect, _near, _far);
    }

    /// <summary>World to clip space: <c>View * Projection(aspect)</c>.</summary>
    public Matrix4x4 ViewProjection(float aspect) => View * Projection(aspect);

    /// <summary>
    /// Where a world point lands in a viewport of <paramref name="width"/> × <paramref name="height"/> pixels (origin
    /// top-left, y down) and its depth (0..1), or null when it is behind the camera.
    /// </summary>
    public (Vector2 Pixel, float Depth)? Project(Vector3 world, int width, int height)
    {
        var clip = Vector4.Transform(new Vector4(world, 1), ViewProjection((float)width / height));
        if (clip.W <= 0) return null;
        var ndc = new Vector3(clip.X, clip.Y, clip.Z) / clip.W;
        return (new Vector2((ndc.X + 1) / 2 * width, (1 - ndc.Y) / 2 * height), ndc.Z);
    }
}

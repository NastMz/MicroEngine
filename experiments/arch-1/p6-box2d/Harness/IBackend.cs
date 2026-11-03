// ARCH-1 P6: the operations the prototype needs from a Box2D v3 backend, in the shape of native/me_box2d.h.
// Two implementations: NativeBackend (P/Invoke into me_box2d, Box2D v3 C17) and ManagedBackend (Box2D.NET, pure C#).
// Scenarios.cs runs the same scenes and contract checks on both.
namespace P6;

public enum BodyKind { Static = 0, Kinematic = 1, Dynamic = 2 }

[Flags]
public enum ShapeFlags { None = 0, Sensor = 1, SensorEvents = 2, ContactEvents = 4, OneWay = 8 }

public interface IBackend : IDisposable
{
    string Name { get; }

    /// <summary>The Box2D version the backend reports (b2GetVersion), as "major.minor.revision".</summary>
    string Version { get; }

    void CreateWorld(float gravityX, float gravityY, bool enableSleep);
    void Step(float timeStep, int subSteps);

    int CreateBody(BodyKind kind, float x, float y, float angle = 0);
    int AddBox(int body, float halfWidth, float halfHeight, float centerX, float centerY, ulong category, ulong mask,
        ShapeFlags flags, float density = 1, float friction = 0.6f);
    int AddCapsule(int body, float x1, float y1, float x2, float y2, float radius, ulong category, ulong mask,
        ShapeFlags flags, float density = 1, float friction = 0.6f);

    /// <summary>x, y, cos, sin.</summary>
    (float X, float Y, float C, float S) GetTransform(int body);
    void SetPosition(int body, float x, float y);
    void SetVelocity(int body, float vx, float vy);

    /// <summary>Writes x, y, cos, sin of every body (creation order); returns the body count.</summary>
    int ReadBodies(Span<float> output);

    /// <summary>Closest hit: shape handle or -1.</summary>
    int RayCast(float x, float y, float dx, float dy, ulong category, ulong mask, out RayHit hit);

    /// <summary>Exact overlap with an axis-aligned box.</summary>
    int OverlapBox(float minX, float minY, float maxX, float maxY, ulong category, ulong mask, Span<int> shapes);

    /// <summary>Broad-phase only: every shape whose fat AABB overlaps the box.</summary>
    int OverlapAabbBroad(float minX, float minY, float maxX, float maxY, ulong category, ulong mask, Span<int> shapes);

    /// <summary>Closest box sweep: shape handle or -1; fraction 1 when nothing is hit.</summary>
    int CastBox(float centerX, float centerY, float halfWidth, float halfHeight, float dx, float dy, ulong category,
        ulong mask, out float fraction, out float normalX, out float normalY);

    /// <summary>Box2D mover (CollideMover + SolvePlanes + CastMover) for a kinematic capsule body; teleports the body.
    /// Shapes in <paramref name="oneWayCategory"/> are one-way: ignored while moving up, kept as floors when their
    /// plane faces up. Returns the number of distinct shapes touched (planes: nx, ny, offset).</summary>
    int MoveCapsule(int body, float x1, float y1, float x2, float y2, float radius, float dx, float dy, ulong category,
        ulong mask, ulong oneWayCategory, out float x, out float y, Span<float> planes, Span<int> shapes);

    /// <summary>Sensor events of the last step: (sensor, visitor) pairs.</summary>
    int SensorEvents(Span<int> begin, Span<int> end, out int endCount);

    /// <summary>Contact begin events of the last step: (a, b) pairs and the manifold normal (a to b).</summary>
    int ContactBeginEvents(Span<int> pairs, Span<float> normals);

    (int Calls, int Disabled) OneWayStats();

    (int Bodies, int Awake, int Contacts) Counters();
}

public readonly record struct RayHit(float X, float Y, float NormalX, float NormalY, float Fraction);

public static class Layers
{
    public const ulong Static = 1, Player = 2, Sensor = 4, Dynamic = 8, Enemy = 16, OneWay = 32, All = ulong.MaxValue;
}

using System.Runtime.InteropServices;

namespace P6;

/// <summary>Box2D v3 (C17) through the flat me_box2d layer: only int, float, ulong and pointer parameters.</summary>
public sealed unsafe class NativeBackend : IBackend
{
    private const string Lib = "me_box2d";
    private IntPtr _world;

    public string Name => "native";

    public string Version
    {
        get
        {
            int v = me_version();
            return $"{v / 10000}.{v / 100 % 100}.{v % 100}";
        }
    }

    public void CreateWorld(float gravityX, float gravityY, bool enableSleep) =>
        _world = me_world_create(gravityX, gravityY, enableSleep ? 1 : 0);

    public void Dispose()
    {
        if (_world != IntPtr.Zero) me_world_destroy(_world);
        _world = IntPtr.Zero;
    }

    public void Step(float timeStep, int subSteps) => me_step(_world, timeStep, subSteps);

    public int CreateBody(BodyKind kind, float x, float y, float angle = 0) => me_body_create(_world, (int)kind, x, y, angle);

    public int AddBox(int body, float halfWidth, float halfHeight, float centerX, float centerY, ulong category, ulong mask,
        ShapeFlags flags, float density = 1, float friction = 0.6f) =>
        me_shape_box(_world, body, halfWidth, halfHeight, centerX, centerY, category, mask, (int)flags, density, friction);

    public int AddCapsule(int body, float x1, float y1, float x2, float y2, float radius, ulong category, ulong mask,
        ShapeFlags flags, float density = 1, float friction = 0.6f) =>
        me_shape_capsule(_world, body, x1, y1, x2, y2, radius, category, mask, (int)flags, density, friction);

    public (float X, float Y, float C, float S) GetTransform(int body)
    {
        float* t = stackalloc float[4];
        me_body_transform(_world, body, t);
        return (t[0], t[1], t[2], t[3]);
    }

    public void SetPosition(int body, float x, float y) => me_body_set_position(_world, body, x, y);
    public void SetVelocity(int body, float vx, float vy) => me_body_set_velocity(_world, body, vx, vy);

    public int ReadBodies(Span<float> output)
    {
        fixed (float* p = output) return me_bodies_read(_world, p, output.Length / 4);
    }

    public int RayCast(float x, float y, float dx, float dy, ulong category, ulong mask, out RayHit hit)
    {
        float* o = stackalloc float[5];
        int shape = me_ray_cast(_world, x, y, dx, dy, category, mask, o);
        hit = shape < 0 ? default : new RayHit(o[0], o[1], o[2], o[3], o[4]);
        return shape;
    }

    public int OverlapBox(float minX, float minY, float maxX, float maxY, ulong category, ulong mask, Span<int> shapes)
    {
        fixed (int* p = shapes) return me_overlap_box(_world, minX, minY, maxX, maxY, category, mask, p, shapes.Length);
    }

    public int OverlapAabbBroad(float minX, float minY, float maxX, float maxY, ulong category, ulong mask, Span<int> shapes)
    {
        fixed (int* p = shapes) return me_overlap_aabb_broad(_world, minX, minY, maxX, maxY, category, mask, p, shapes.Length);
    }

    public int CastBox(float centerX, float centerY, float halfWidth, float halfHeight, float dx, float dy, ulong category,
        ulong mask, out float fraction, out float normalX, out float normalY)
    {
        float* o = stackalloc float[3];
        int shape = me_cast_box(_world, centerX, centerY, halfWidth, halfHeight, dx, dy, category, mask, o);
        fraction = o[0]; normalX = o[1]; normalY = o[2];
        return shape;
    }

    public int MoveCapsule(int body, float x1, float y1, float x2, float y2, float radius, float dx, float dy, ulong category,
        ulong mask, ulong oneWayCategory, out float x, out float y, Span<float> planes, Span<int> shapes)
    {
        float* xy = stackalloc float[2];
        int n;
        fixed (float* pp = planes)
        fixed (int* ps = shapes)
            n = me_move_capsule(_world, body, x1, y1, x2, y2, radius, dx, dy, category, mask, oneWayCategory, xy, pp, ps,
                Math.Min(shapes.Length, planes.Length / 3));
        x = xy[0]; y = xy[1];
        return n;
    }

    public int SensorEvents(Span<int> begin, Span<int> end, out int endCount)
    {
        int ec;
        int n;
        fixed (int* pb = begin)
        fixed (int* pe = end)
            n = me_sensor_events(_world, pb, begin.Length / 2, pe, end.Length / 2, &ec);
        endCount = ec;
        return n;
    }

    public int ContactBeginEvents(Span<int> pairs, Span<float> normals)
    {
        fixed (int* pp = pairs)
        fixed (float* pn = normals)
            return me_contact_begin_events(_world, pp, pn, Math.Min(pairs.Length, normals.Length) / 2);
    }

    public (int Calls, int Disabled) OneWayStats()
    {
        int c, d;
        me_one_way_stats(_world, &c, &d);
        return (c, d);
    }

    public (int Bodies, int Awake, int Contacts) Counters()
    {
        int* c = stackalloc int[3];
        me_counters(_world, c);
        return (c[0], c[1], c[2]);
    }

    [DllImport(Lib)] private static extern void me_counters(IntPtr w, int* out3);
    [DllImport(Lib)] private static extern int me_version();
    [DllImport(Lib)] private static extern IntPtr me_world_create(float gx, float gy, int sleep);
    [DllImport(Lib)] private static extern void me_world_destroy(IntPtr w);
    [DllImport(Lib)] private static extern void me_step(IntPtr w, float dt, int subSteps);
    [DllImport(Lib)] private static extern int me_body_create(IntPtr w, int type, float x, float y, float angle);
    [DllImport(Lib)] private static extern void me_body_transform(IntPtr w, int body, float* out4);
    [DllImport(Lib)] private static extern void me_body_set_position(IntPtr w, int body, float x, float y);
    [DllImport(Lib)] private static extern void me_body_set_velocity(IntPtr w, int body, float vx, float vy);
    [DllImport(Lib)] private static extern int me_bodies_read(IntPtr w, float* output, int max);
    [DllImport(Lib)] private static extern int me_shape_box(IntPtr w, int body, float hw, float hh, float cx, float cy, ulong cat, ulong mask, int flags, float density, float friction);
    [DllImport(Lib)] private static extern int me_shape_capsule(IntPtr w, int body, float x1, float y1, float x2, float y2, float r, ulong cat, ulong mask, int flags, float density, float friction);
    [DllImport(Lib)] private static extern int me_ray_cast(IntPtr w, float x, float y, float dx, float dy, ulong cat, ulong mask, float* out5);
    [DllImport(Lib)] private static extern int me_overlap_box(IntPtr w, float minX, float minY, float maxX, float maxY, ulong cat, ulong mask, int* shapes, int max);
    [DllImport(Lib)] private static extern int me_overlap_aabb_broad(IntPtr w, float minX, float minY, float maxX, float maxY, ulong cat, ulong mask, int* shapes, int max);
    [DllImport(Lib)] private static extern int me_cast_box(IntPtr w, float cx, float cy, float hw, float hh, float dx, float dy, ulong cat, ulong mask, float* out3);
    [DllImport(Lib)] private static extern int me_move_capsule(IntPtr w, int body, float x1, float y1, float x2, float y2, float r, float dx, float dy, ulong cat, ulong mask, ulong oneWay, float* xy, float* planes, int* shapes, int max);
    [DllImport(Lib)] private static extern int me_sensor_events(IntPtr w, int* begin, int maxBegin, int* end, int maxEnd, int* endCount);
    [DllImport(Lib)] private static extern int me_contact_begin_events(IntPtr w, int* pairs, float* normals, int max);
    [DllImport(Lib)] private static extern void me_one_way_stats(IntPtr w, int* calls, int* disabled);
}

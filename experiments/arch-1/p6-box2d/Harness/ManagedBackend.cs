using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Contacts;
using static Box2D.NET.B2Cores;
using static Box2D.NET.B2Distances;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Movers;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;

namespace P6;

/// <summary>Box2D.NET (ikpil), the pure C# port; the same algorithms and defaults as native/me_box2d.c.</summary>
public sealed class ManagedBackend : IBackend
{
    private B2WorldId _world;
    private readonly List<B2BodyId> _bodies = new();
    private readonly List<B2ShapeId> _shapes = new();
    private readonly List<ShapeFlags> _flags = new();
    private int _preSolveCalls, _preSolveDisabled;
    private readonly B2CollisionPlane[] _planes = new B2CollisionPlane[16];
    private readonly int[] _planeShapes = new int[16];
    private int _planeCount;
    private ulong _oneWayCategory;
    private readonly List<int> _collected = new();
    private readonly CastHit _hit = new();

    private sealed class CastHit
    {
        public B2ShapeId Shape;
        public float Fraction;
        public B2Vec2 Normal;
    }

    public string Name => "managed";

    public string Version
    {
        get
        {
            B2Version v = b2GetVersion();
            return $"{v.major}.{v.minor}.{v.revision}";
        }
    }

    public void CreateWorld(float gravityX, float gravityY, bool enableSleep)
    {
        B2WorldDef def = b2DefaultWorldDef();
        def.gravity = new B2Vec2(gravityX, gravityY);
        def.enableSleep = enableSleep;
        _world = b2CreateWorld(in def);
        b2World_SetPreSolveCallback(_world, PreSolve, this);
    }

    public void Dispose()
    {
        if (!B2_IS_NULL(_world)) b2DestroyWorld(_world);
        _world = default;
    }

    private int Handle(B2ShapeId id) => b2Shape_IsValid(id) ? (int)b2Shape_GetUserData(id).GetSigned(0) - 1 : -1;

    // One-way platforms: the upstream "Platformer" sample's rule (normal from the platform to the other shape points up).
    private static bool PreSolve(B2ShapeId a, B2ShapeId b, B2Vec2 point, B2Vec2 normal, object context)
    {
        var self = (ManagedBackend)context;
        int ha = self.Handle(a), hb = self.Handle(b);
        float sign;
        if (ha >= 0 && self._flags[ha].HasFlag(ShapeFlags.OneWay)) sign = 1;
        else if (hb >= 0 && self._flags[hb].HasFlag(ShapeFlags.OneWay)) sign = -1;
        else return true;
        self._preSolveCalls++;
        if (sign * normal.Y > 0.7f) return true;
        self._preSolveDisabled++;
        return false;
    }

    public void Step(float timeStep, int subSteps) => b2World_Step(_world, timeStep, subSteps);

    public int CreateBody(BodyKind kind, float x, float y, float angle = 0)
    {
        B2BodyDef def = b2DefaultBodyDef();
        def.type = kind == BodyKind.Static ? B2BodyType.b2_staticBody
            : kind == BodyKind.Kinematic ? B2BodyType.b2_kinematicBody : B2BodyType.b2_dynamicBody;
        def.position = new B2Vec2(x, y);
        def.rotation = b2MakeRot(angle);
        _bodies.Add(b2CreateBody(_world, in def));
        return _bodies.Count - 1;
    }

    private static B2ShapeDef ShapeDef(ulong category, ulong mask, ShapeFlags flags, float density, float friction)
    {
        B2ShapeDef def = b2DefaultShapeDef();
        def.filter = b2DefaultFilter();
        def.filter.categoryBits = category;
        def.filter.maskBits = mask;
        def.isSensor = flags.HasFlag(ShapeFlags.Sensor);
        def.enableSensorEvents = flags.HasFlag(ShapeFlags.SensorEvents);
        def.enableContactEvents = flags.HasFlag(ShapeFlags.ContactEvents);
        def.enablePreSolveEvents = flags.HasFlag(ShapeFlags.OneWay);
        def.density = density;
        def.material.friction = friction;
        return def;
    }

    private int AddShape(B2ShapeId id, ShapeFlags flags)
    {
        _shapes.Add(id);
        _flags.Add(flags);
        b2Shape_SetUserData(id, new B2UserData((long)_shapes.Count));
        return _shapes.Count - 1;
    }

    public int AddBox(int body, float halfWidth, float halfHeight, float centerX, float centerY, ulong category, ulong mask,
        ShapeFlags flags, float density = 1, float friction = 0.6f)
    {
        B2ShapeDef def = ShapeDef(category, mask, flags, density, friction);
        B2Polygon box = b2MakeOffsetBox(halfWidth, halfHeight, new B2Vec2(centerX, centerY), b2Rot_identity);
        return AddShape(b2CreatePolygonShape(_bodies[body], in def, in box), flags);
    }

    public int AddCapsule(int body, float x1, float y1, float x2, float y2, float radius, ulong category, ulong mask,
        ShapeFlags flags, float density = 1, float friction = 0.6f)
    {
        B2ShapeDef def = ShapeDef(category, mask, flags, density, friction);
        var capsule = new B2Capsule(new B2Vec2(x1, y1), new B2Vec2(x2, y2), radius);
        return AddShape(b2CreateCapsuleShape(_bodies[body], in def, in capsule), flags);
    }

    public (float X, float Y, float C, float S) GetTransform(int body)
    {
        B2Vec2 p = b2Body_GetPosition(_bodies[body]);
        B2Rot q = b2Body_GetRotation(_bodies[body]);
        return (p.X, p.Y, q.c, q.s);
    }

    public void SetPosition(int body, float x, float y) =>
        b2Body_SetTransform(_bodies[body], new B2Vec2(x, y), b2Body_GetRotation(_bodies[body]));

    public void SetVelocity(int body, float vx, float vy) => b2Body_SetLinearVelocity(_bodies[body], new B2Vec2(vx, vy));

    public int ReadBodies(Span<float> output)
    {
        int n = Math.Min(_bodies.Count, output.Length / 4);
        for (int i = 0; i < n; i++)
        {
            var t = GetTransform(i);
            output[4 * i] = t.X; output[4 * i + 1] = t.Y; output[4 * i + 2] = t.C; output[4 * i + 3] = t.S;
        }
        return n;
    }

    private static B2QueryFilter Query(ulong category, ulong mask) => new B2QueryFilter(category, mask);

    public int RayCast(float x, float y, float dx, float dy, ulong category, ulong mask, out RayHit hit)
    {
        B2RayResult r = b2World_CastRayClosest(_world, new B2Vec2(x, y), new B2Vec2(dx, dy), Query(category, mask));
        if (!r.hit)
        {
            hit = default;
            return -1;
        }
        hit = new RayHit(r.point.X, r.point.Y, r.normal.X, r.normal.Y, r.fraction);
        return Handle(r.shapeId);
    }

    private static bool Collect(B2ShapeId id, object context)
    {
        var self = (ManagedBackend)context;
        self._collected.Add(self.Handle(id));
        return true;
    }

    private int Copy(Span<int> shapes)
    {
        int n = Math.Min(_collected.Count, shapes.Length);
        for (int i = 0; i < n; i++) shapes[i] = _collected[i];
        return _collected.Count;
    }

    public int OverlapBox(float minX, float minY, float maxX, float maxY, ulong category, ulong mask, Span<int> shapes)
    {
        Span<B2Vec2> pts = stackalloc B2Vec2[4]
        {
            new B2Vec2(minX, minY), new B2Vec2(maxX, minY), new B2Vec2(maxX, maxY), new B2Vec2(minX, maxY),
        };
        B2ShapeProxy proxy = b2MakeProxy(pts, 4, 0);
        _collected.Clear();
        b2World_OverlapShape(_world, ref proxy, Query(category, mask), Collect, this);
        return Copy(shapes);
    }

    public int OverlapAabbBroad(float minX, float minY, float maxX, float maxY, ulong category, ulong mask, Span<int> shapes)
    {
        var box = new B2AABB(new B2Vec2(minX, minY), new B2Vec2(maxX, maxY));
        _collected.Clear();
        b2World_OverlapAABB(_world, in box, Query(category, mask), Collect, this);
        return Copy(shapes);
    }

#if B2NET_PKG
    // Box2D.NET 3.1.654 (the published package): the callback context is an object.
    private static float Closest(B2ShapeId id, B2Vec2 point, B2Vec2 normal, float fraction, object state)
    {
        var context = (CastHit)state;
#else
    private static float Closest(B2ShapeId id, B2Vec2 point, B2Vec2 normal, float fraction, ref CastHit context)
    {
#endif
        context.Shape = id;
        context.Fraction = fraction;
        context.Normal = normal;
        return fraction;
    }

    public int CastBox(float centerX, float centerY, float halfWidth, float halfHeight, float dx, float dy, ulong category,
        ulong mask, out float fraction, out float normalX, out float normalY)
    {
        Span<B2Vec2> pts = stackalloc B2Vec2[4]
        {
            new B2Vec2(centerX - halfWidth, centerY - halfHeight), new B2Vec2(centerX + halfWidth, centerY - halfHeight),
            new B2Vec2(centerX + halfWidth, centerY + halfHeight), new B2Vec2(centerX - halfWidth, centerY + halfHeight),
        };
        B2ShapeProxy proxy = b2MakeProxy(pts, 4, 0);
        _hit.Shape = b2_nullShapeId;
        _hit.Fraction = 1;
        _hit.Normal = default;
        b2World_CastShape(_world, ref proxy, new B2Vec2(dx, dy), Query(category, mask), Closest, _hit);
        fraction = _hit.Fraction; normalX = _hit.Normal.X; normalY = _hit.Normal.Y;
        return B2_IS_NULL(_hit.Shape) ? -1 : Handle(_hit.Shape);
    }

    private static bool CollectPlane(B2ShapeId id, ref B2PlaneResult result, object context)
    {
        var self = (ManagedBackend)context;
        if ((b2Shape_GetFilter(id).categoryBits & self._oneWayCategory) != 0 && result.plane.normal.Y < 0.7f) return true;
        if (self._planeCount < self._planes.Length)
        {
            self._planes[self._planeCount] = new B2CollisionPlane(result.plane, float.MaxValue, 0, true);
            self._planeShapes[self._planeCount] = self.Handle(id);
            self._planeCount++;
        }
        return true;
    }

    public int MoveCapsule(int body, float x1, float y1, float x2, float y2, float radius, float dx, float dy, ulong category,
        ulong mask, ulong oneWayCategory, out float x, out float y, Span<float> planes, Span<int> shapes)
    {
        var t = GetTransform(body);
        var p = new B2Vec2(t.X, t.Y);
        var target = new B2Vec2(p.X + dx, p.Y + dy);
        B2QueryFilter filter = Query(category, dy > 0 ? mask & ~oneWayCategory : mask);
        _oneWayCategory = oneWayCategory;
        int reported = 0, max = Math.Min(shapes.Length, planes.Length / 3);
        const float tolerance = 0.01f;
        for (int iteration = 0; iteration < 5; iteration++)
        {
            _planeCount = 0;
            var mover = new B2Capsule(new B2Vec2(p.X + x1, p.Y + y1), new B2Vec2(p.X + x2, p.Y + y2), radius);
            b2World_CollideMover(_world, in mover, in filter, CollectPlane, this);
            B2PlaneSolverResult result = b2SolvePlanes(b2Sub(target, p), _planes, _planeCount);
            float fraction = b2World_CastMover(_world, in mover, result.translation, in filter);
            B2Vec2 delta = b2MulSV(fraction, result.translation);
            p = b2Add(p, delta);
            for (int i = 0; i < _planeCount; i++)
            {
                bool seen = false;
                for (int j = 0; j < reported; j++) seen |= shapes[j] == _planeShapes[i];
                if (!seen && reported < max)
                {
                    planes[3 * reported] = _planes[i].plane.normal.X;
                    planes[3 * reported + 1] = _planes[i].plane.normal.Y;
                    planes[3 * reported + 2] = _planes[i].plane.offset;
                    shapes[reported] = _planeShapes[i];
                    reported++;
                }
            }
            if (b2LengthSquared(delta) < tolerance * tolerance) break;
        }
        SetPosition(body, p.X, p.Y);
        x = p.X; y = p.Y;
        return reported;
    }

    public int SensorEvents(Span<int> begin, Span<int> end, out int endCount)
    {
        B2SensorEvents e = b2World_GetSensorEvents(_world);
        for (int i = 0; i < Math.Min(e.beginCount, begin.Length / 2); i++)
        {
            begin[2 * i] = Handle(e.beginEvents[i].sensorShapeId);
            begin[2 * i + 1] = Handle(e.beginEvents[i].visitorShapeId);
        }
        for (int i = 0; i < Math.Min(e.endCount, end.Length / 2); i++)
        {
            end[2 * i] = Handle(e.endEvents[i].sensorShapeId);
            end[2 * i + 1] = Handle(e.endEvents[i].visitorShapeId);
        }
        endCount = e.endCount;
        return e.beginCount;
    }

    public int ContactBeginEvents(Span<int> pairs, Span<float> normals)
    {
        B2ContactEvents e = b2World_GetContactEvents(_world);
        int n = Math.Min(e.beginCount, Math.Min(pairs.Length, normals.Length) / 2);
        for (int i = 0; i < n; i++)
        {
            pairs[2 * i] = Handle(e.beginEvents[i].shapeIdA);
            pairs[2 * i + 1] = Handle(e.beginEvents[i].shapeIdB);
            B2ContactData d = b2Contact_GetData(e.beginEvents[i].contactId);
            normals[2 * i] = d.manifold.normal.X;
            normals[2 * i + 1] = d.manifold.normal.Y;
        }
        return e.beginCount;
    }

    public (int Calls, int Disabled) OneWayStats() => (_preSolveCalls, _preSolveDisabled);

    public (int Bodies, int Awake, int Contacts) Counters()
    {
        B2Counters c = b2World_GetCounters(_world);
        return (c.bodyCount, b2World_GetAwakeBodyCount(_world), c.contactCount);
    }
}

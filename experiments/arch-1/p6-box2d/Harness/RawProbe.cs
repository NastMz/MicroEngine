using System.Runtime.InteropServices;

namespace P6;

/// <summary>
/// Calls Box2D's own C API directly, the way an existing binding (or a generated one) would: structs passed and
/// returned by value (b2Version, b2WorldId, b2BodyId, b2Vec2, the large b2WorldDef/b2BodyDef/b2ShapeDef/b2Polygon).
/// It answers whether the P/Invoke signature shapes of a "raw" binding survive the .NET wasm interpreter, which the
/// flat me_box2d layer avoids. Box2D v3.2.0 layout (single precision): b2BodyDef.type at 0, position at 4.
/// </summary>
public static unsafe class RawProbe
{
    private const string Lib = "me_box2d";

    [StructLayout(LayoutKind.Sequential)] private struct Version3 { public int Major, Minor, Revision; }
    [StructLayout(LayoutKind.Sequential)] private struct WorldId { public ushort Index1, Generation; }
    [StructLayout(LayoutKind.Sequential)] private struct BodyId { public int Index1; public ushort World0, Generation; }
    [StructLayout(LayoutKind.Sequential)] private struct ShapeId { public int Index1; public ushort World0, Generation; }
    [StructLayout(LayoutKind.Sequential)] private struct Vec2 { public float X, Y; }
    [StructLayout(LayoutKind.Sequential, Size = 512)] private struct Blob { public long First; }

    [DllImport(Lib)] private static extern Version3 b2GetVersion();
    [DllImport(Lib)] private static extern Blob b2DefaultWorldDef();
    [DllImport(Lib)] private static extern WorldId b2CreateWorld(Blob* def);
    [DllImport(Lib)] private static extern Blob b2DefaultBodyDef();
    [DllImport(Lib)] private static extern BodyId b2CreateBody(WorldId world, Blob* def);
    [DllImport(Lib)] private static extern Blob b2DefaultShapeDef();
    [DllImport(Lib)] private static extern Blob b2MakeBox(float halfWidth, float halfHeight);
    [DllImport(Lib)] private static extern ShapeId b2CreatePolygonShape(BodyId body, Blob* def, Blob* polygon);
    [DllImport(Lib)] private static extern void b2World_Step(WorldId world, float timeStep, int subSteps);
    [DllImport(Lib)] private static extern Vec2 b2Body_GetPosition(BodyId body);
    [DllImport(Lib)] private static extern void b2DestroyWorld(WorldId world);

    /// <summary>Runs the calls one by one; returns a report naming the last call reached.</summary>
    public static string Run(Action<string>? progress = null)
    {
        string step = "b2GetVersion";
        try
        {
            progress?.Invoke(step);
            Version3 v = b2GetVersion();
            step = "b2DefaultWorldDef"; progress?.Invoke(step);
            Blob wd = b2DefaultWorldDef();
            Vec2* gravity = (Vec2*)&wd;
            float gy = gravity->Y;
            step = "b2CreateWorld"; progress?.Invoke(step);
            WorldId world = b2CreateWorld(&wd);
            step = "b2DefaultBodyDef"; progress?.Invoke(step);
            Blob bd = b2DefaultBodyDef();
            byte* b = (byte*)&bd;
            *(int*)b = 2; // b2_dynamicBody
            *(Vec2*)(b + 4) = new Vec2 { X = 0, Y = 10 };
            step = "b2CreateBody"; progress?.Invoke(step);
            BodyId body = b2CreateBody(world, &bd);
            step = "b2DefaultShapeDef"; progress?.Invoke(step);
            Blob sd = b2DefaultShapeDef();
            step = "b2MakeBox"; progress?.Invoke(step);
            Blob box = b2MakeBox(0.5f, 0.5f);
            step = "b2CreatePolygonShape"; progress?.Invoke(step);
            ShapeId shape = b2CreatePolygonShape(body, &sd, &box);
            step = "b2World_Step"; progress?.Invoke(step);
            for (int i = 0; i < 60; i++) b2World_Step(world, 1 / 60f, 4);
            step = "b2Body_GetPosition"; progress?.Invoke(step);
            Vec2 p = b2Body_GetPosition(body);
            step = "b2DestroyWorld"; progress?.Invoke(step);
            b2DestroyWorld(world);
            // Free fall for 1 s from y = 10 with g = -10: y ≈ 5 (semi-implicit Euler with substeps: slightly lower).
            bool ok = v.Major == 3 && gy == -10f && world.Index1 > 0 && body.Index1 > 0 && shape.Index1 > 0 && p.Y < 5.2f && p.Y > 4.7f;
            return $"raw {(ok ? "PASS" : "FAIL")}: version={v.Major}.{v.Minor}.{v.Revision} defaultGravityY={gy} " +
                   $"world={world.Index1} body={body.Index1} shape={shape.Index1} y(1s)={p.Y:R}";
        }
        catch (Exception e)
        {
            return $"raw FAIL at {step}: {e.GetType().Name}: {e.Message}";
        }
    }
}

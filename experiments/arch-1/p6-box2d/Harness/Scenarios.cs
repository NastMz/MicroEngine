using System.Diagnostics;

namespace P6;

/// <summary>The measured scene and the contract checks, written once against IBackend.</summary>
public static class Scenarios
{
    public const float Dt = 1 / 60f;
    public const int SubSteps = 4;

    // ---------------------------------------------------------------------------------------------------------------
    // Benchmark: N dynamic 0.5 m boxes, each slightly rotated, rained in 20 columns into a 12 m wide static container;
    // they tumble, pile up and keep touching (about 2.5 contacts per box at the end). 60 Hz, 4 substeps, sleep off
    // (worst case: nothing ever sleeps), single-threaded (no task system on either backend).

    public sealed record BenchResult(string Backend, string Version, int Boxes, int Steps, double MeanMs, double MedianMs,
        double P95Ms, double MaxMs, double TotalMs, string FinalHash, string Checkpoints, string Counters);

    public static void BuildPile(IBackend b, int boxes, bool sleep)
    {
        b.CreateWorld(0, -10, sleep);
        int ground = b.CreateBody(BodyKind.Static, 0, 0);
        b.AddBox(ground, 7, 0.5f, 0, -0.5f, Layers.Static, Layers.All, ShapeFlags.None);      // floor, top y = 0
        b.AddBox(ground, 0.5f, 20, -6.5f, 20, Layers.Static, Layers.All, ShapeFlags.None);    // walls, inner x = -6..6
        b.AddBox(ground, 0.5f, 20, 6.5f, 20, Layers.Static, Layers.All, ShapeFlags.None);
        const int columns = 20;
        for (int i = 0; i < boxes; i++)
        {
            int c = i % columns, r = i / columns;
            float x = -5.225f + c * 0.55f;
            float y = 1.0f + r * 0.75f;
            float angle = 0.1f * ((i % 7) - 3);
            int body = b.CreateBody(BodyKind.Dynamic, x, y, angle);
            b.AddBox(body, 0.25f, 0.25f, 0, 0, Layers.Dynamic, Layers.All, ShapeFlags.None);
        }
    }

    public static BenchResult Bench(Func<IBackend> factory, int boxes = 500, int steps = 600, bool sleep = false)
    {
        using IBackend b = factory();
        BuildPile(b, boxes, sleep);
        var times = new double[steps];
        var buffer = new float[(boxes + 1) * 4];
        var checkpoints = new List<string>();
        double tick = 1000.0 / Stopwatch.Frequency;
        for (int s = 0; s < steps; s++)
        {
            long t0 = Stopwatch.GetTimestamp();
            b.Step(Dt, SubSteps);
            times[s] = (Stopwatch.GetTimestamp() - t0) * tick;
            int n = s + 1;
            if (n is 1 or 10 or 60 or 120 or 300 or 600 or 1200)
                checkpoints.Add($"{n}:{Hash(b, buffer)}");
        }
        string final = Hash(b, buffer);
        var counters = b.Counters();
        var sorted = (double[])times.Clone();
        Array.Sort(sorted);
        return new BenchResult(b.Name, b.Version, boxes, steps, times.Average(), sorted[steps / 2],
            sorted[(int)(steps * 0.95)], sorted[^1], times.Sum(), final, string.Join(" ", checkpoints),
            $"bodies={counters.Bodies} awake={counters.Awake} contacts={counters.Contacts}");
    }

    /// <summary>FNV-1a 64 over the bit patterns of every body's x, y, cos, sin.</summary>
    public static string Hash(IBackend b, float[] buffer)
    {
        int n = b.ReadBodies(buffer);
        ulong h = 14695981039346656037UL;
        for (int i = 0; i < n * 4; i++)
        {
            uint bits = (uint)BitConverter.SingleToInt32Bits(buffer[i]);
            for (int k = 0; k < 4; k++)
            {
                h ^= (bits >> (8 * k)) & 0xFF;
                h *= 1099511628211UL;
            }
        }
        return h.ToString("x16");
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Contract checks: the operations of the dossier's IPhysicsWorld2D common contract, expressed on Box2D v3.

    public sealed class Report
    {
        public readonly List<string> Lines = new();
        public int Passed, Failed;

        public void Check(string name, bool ok, string detail)
        {
            if (ok) Passed++; else Failed++;
            Lines.Add($"{(ok ? "PASS" : "FAIL")} {name}: {detail}");
        }

        public void Note(string text) => Lines.Add("NOTE " + text);
    }

    private sealed class Level
    {
        public required IBackend B;
        public int Ground, Wall, Thin, Sensor, Enemy, OneWay, Player, PlayerShape;
        public int SensorBegins, SensorEnds, StepIndex, FirstBeginStep = -1;
        public readonly int[] Begin = new int[64], End = new int[64], Pairs = new int[128];
        public readonly float[] Normals = new float[128], Planes = new float[48];
        public readonly int[] PlaneShapes = new int[16], Shapes = new int[64];
        public readonly List<(int A, int B, float Nx, float Ny)> Contacts = new();

        public void Step()
        {
            B.Step(Dt, SubSteps);
            StepIndex++;
            int nb = B.SensorEvents(Begin, End, out int ne);
            for (int i = 0; i < nb; i++)
                if (Begin[2 * i] == Sensor && Begin[2 * i + 1] == PlayerShape)
                {
                    SensorBegins++;
                    if (FirstBeginStep < 0) FirstBeginStep = StepIndex;
                }
            for (int i = 0; i < ne; i++)
                if (End[2 * i] == Sensor && End[2 * i + 1] == PlayerShape) SensorEnds++;
            int nc = B.ContactBeginEvents(Pairs, Normals);
            for (int i = 0; i < Math.Min(nc, 64); i++) Contacts.Add((Pairs[2 * i], Pairs[2 * i + 1], Normals[2 * i], Normals[2 * i + 1]));
        }

        /// <summary>Player move (Box2D mover) followed by a world step; returns the shapes touched.</summary>
        public int Move(float dx, float dy, out float x, out float y)
        {
            int n = B.MoveCapsule(Player, 0, -0.5f, 0, 0.5f, 0.3f, dx, dy, Layers.Player,
                Layers.Static | Layers.Dynamic | Layers.OneWay, Layers.OneWay, out x, out y, Planes, PlaneShapes);
            Step();
            return n;
        }

        public bool Grounded(int n)
        {
            for (int i = 0; i < n; i++) if (Planes[3 * i + 1] > 0.7f) return true;
            return false;
        }

        public bool Touched(int n, int shape)
        {
            for (int i = 0; i < n; i++) if (PlaneShapes[i] == shape) return true;
            return false;
        }
    }

    private static bool Near(float a, float b, float tol) => MathF.Abs(a - b) <= tol;

    public static Report Contract(Func<IBackend> factory)
    {
        var r = new Report();
        using IBackend b = factory();
        b.CreateWorld(0, -10, true);
        var L = new Level { B = b };
        int level = b.CreateBody(BodyKind.Static, 0, 0);
        L.Ground = b.AddBox(level, 20, 0.5f, 0, -0.5f, Layers.Static, Layers.All, ShapeFlags.None);          // top y = 0
        L.Wall = b.AddBox(level, 0.5f, 2, 5.5f, 2, Layers.Static, Layers.All, ShapeFlags.None);              // x 5..6
        L.Thin = b.AddBox(level, 2, 0.05f, -4, 3.05f, Layers.Static, Layers.All, ShapeFlags.None);           // y 3..3.1
        L.Sensor = b.AddBox(level, 0.5f, 1, 2.5f, 1, Layers.Sensor, Layers.All,
            ShapeFlags.Sensor | ShapeFlags.SensorEvents);                                                   // x 2..3
        L.Enemy = b.AddBox(level, 0.25f, 1, -1.25f, 1, Layers.Enemy, Layers.All, ShapeFlags.None);           // x -1.5..-1
        L.OneWay = b.AddBox(level, 2, 0.1f, 10, 2.1f, Layers.OneWay, Layers.All, ShapeFlags.OneWay);         // y 2..2.2, x 8..12
        L.Player = b.CreateBody(BodyKind.Kinematic, 0, 1.5f);
        L.PlayerShape = b.AddCapsule(L.Player, 0, -0.5f, 0, 0.5f, 0.3f, Layers.Player,
            Layers.Static | Layers.Sensor | Layers.Dynamic | Layers.OneWay, ShapeFlags.SensorEvents | ShapeFlags.ContactEvents);
        // Tolerances: the mover stops within its 0.01 m iteration tolerance plus Box2D's linear slop (0.005 m).
        const float tol = 0.02f;
        r.Note($"backend {b.Name}, Box2D {b.Version}; capsule half-height 0.8 m (segment ±0.5, radius 0.3)");

        // 1. Kinematic move lands on the ground and reports a ground plane.
        int n = 0;
        float x = 0, y = 0;
        for (int i = 0; i < 30; i++) n = L.Move(0, -0.2f, out x, out y);
        r.Check("move.lands", Near(y, 0.8f, tol) && L.Grounded(n) && L.Touched(n, L.Ground),
            $"y={y:F4} (expected 0.8), grounded={L.Grounded(n)}, planes={n}");

        // 2. Blocked by a wall while walking right; the sensor in the way reports enter and exit.
        for (int i = 0; i < 40; i++) n = L.Move(0.2f, -0.05f, out x, out y);
        bool wallPlane = false;
        for (int i = 0; i < n; i++) wallPlane |= L.PlaneShapes[i] == L.Wall && L.Planes[3 * i] < -0.7f;
        r.Check("move.blocked-by-wall", Near(x, 4.7f, tol) && wallPlane && Near(y, 0.8f, tol),
            $"x={x:F4} (expected 4.7), y={y:F4}, wall plane={wallPlane}");
        r.Check("sensor.enter-exit", L.SensorBegins == 1 && L.SensorEnds == 1,
            $"begin={L.SensorBegins} end={L.SensorEnds} (expected 1/1), first begin at step {L.FirstBeginStep}");

        // 3. Layers and masks: the player's mask has no Enemy bit, so it walks through the enemy box.
        bool touchedEnemy = false;
        for (int i = 0; i < 40; i++)
        {
            n = L.Move(-0.2f, -0.05f, out x, out y);
            touchedEnemy |= L.Touched(n, L.Enemy);
        }
        r.Check("layers.mask-skips-enemy", Near(x, -3.3f, tol) && !touchedEnemy, $"x={x:F4} (expected -3.3), touched enemy={touchedEnemy}");
        r.Check("sensor.second-pass", L.SensorBegins == 2 && L.SensorEnds == 2, $"begin={L.SensorBegins} end={L.SensorEnds}");
        int hitEnemy = b.RayCast(-3, 1, 10, 0, Layers.All, Layers.Enemy, out RayHit he);
        int hitStatic = b.RayCast(-3, 1, 10, 0, Layers.All, Layers.Static, out RayHit hs);
        r.Check("layers.ray-mask", hitEnemy == L.Enemy && Near(he.X, -1.5f, 0.001f) && hitStatic == L.Wall && Near(hs.X, 5, 0.001f),
            $"mask Enemy -> shape {hitEnemy} at x={he.X:F4}; mask Static -> shape {hitStatic} at x={hs.X:F4} fraction={hs.Fraction:F4}");

        // 4. Ray cast with point, normal and fraction.
        int hit = b.RayCast(0, 5, 0, -10, Layers.All, Layers.Static, out RayHit h);
        r.Check("ray.closest", hit == L.Ground && Near(h.Y, 0, 0.001f) && Near(h.NormalY, 1, 0.001f) && Near(h.Fraction, 0.5f, 0.001f),
            $"shape={hit} point=({h.X:F4},{h.Y:F4}) normal=({h.NormalX:F3},{h.NormalY:F3}) fraction={h.Fraction:F4}");

        int throughSensor = b.RayCast(2.5f, 5, 0, -10, Layers.All, Layers.All, out RayHit hsen);
        int overSensor = b.OverlapBox(2.4f, 0.5f, 2.6f, 1.5f, Layers.All, Layers.All, L.Shapes);
        r.Note($"queries and sensors: a ray down through the sensor (mask All) hits shape {throughSensor} at y={hsen.Y:F3} " +
               $"(sensor={L.Sensor}, ground={L.Ground}); an overlap inside the sensor returns {overSensor} shape(s)");

        // 5. AABB overlap: exact (shape overlap with a box proxy) vs broad phase (fat AABBs).
        int exactGap = b.OverlapBox(6.01f, 1, 6.2f, 2, Layers.All, Layers.Static, L.Shapes);
        int broadGap = b.OverlapAabbBroad(6.01f, 1, 6.2f, 2, Layers.All, Layers.Static, L.Shapes);
        int exactHit = b.OverlapBox(5.9f, 1, 6.2f, 2, Layers.All, Layers.Static, L.Shapes);
        bool wallFound = exactHit >= 1 && L.Shapes[0] == L.Wall;
        r.Check("overlap.exact", exactGap == 0 && wallFound, $"box 1 cm right of the wall -> {exactGap} shapes; box over the wall -> {exactHit} (wall={wallFound})");
        r.Note($"overlap.broad: b2World_OverlapAABB with the box 1 cm right of the wall returns {broadGap} shape(s) (fat AABB)");

        // 6. No tunnelling: one 5 m move down onto a 0.1 m solid (WO-2's E1 shape), capsule mover and box sweep.
        b.SetPosition(L.Player, -4, 5);
        n = L.Move(0, -5, out x, out y);
        r.Check("move.no-tunnel-capsule", Near(y, 3.9f, tol) && L.Touched(n, L.Thin), $"y={y:F4} (expected 3.9 on the 0.1 m solid)");
        int cast = b.CastBox(-4, 5, 0.3f, 0.8f, 0, -5, Layers.All, Layers.Static, out float f, out float cnx, out float cny);
        r.Check("cast.no-tunnel-box", cast == L.Thin && Near(5 - 5 * f, 3.9f, tol) && Near(cny, 1, 0.001f),
            $"shape={cast} fraction={f:F4} -> y={5 - 5 * f:F4} normal=({cnx:F3},{cny:F3})");

        // 7. Box collide-and-slide built on the box sweep (no box mover in Box2D): emulation.
        // Box2D's shape cast reports a shape already within its tolerance (touching) as an initial overlap: fraction 0
        // and a zero normal. A slide loop that rests the box on the ground therefore needs a skin: after each hit the box
        // backs off the surface along the normal.
        (float X, float Y, HashSet<int> Contacts, int Stuck) BoxSlide(float skin)
        {
            float bx = 0, by = 0.81f;
            var contacts = new HashSet<int>();
            int stuck = 0;
            for (int i = 0; i < 20; i++)
            {
                float dx = 0.5f, dy = -0.3f;
                for (int it = 0; it < 4 && (dx != 0 || dy != 0); it++)
                {
                    int s = b.CastBox(bx, by, 0.3f, 0.8f, dx, dy, Layers.All, Layers.Static, out float fr, out float nx, out float ny);
                    if (s < 0) { bx += dx; by += dy; break; }
                    if (fr == 0 && nx == 0 && ny == 0) { stuck++; break; }
                    contacts.Add(s);
                    bx += dx * fr + nx * skin; by += dy * fr + ny * skin;
                    float rx = dx * (1 - fr), ry = dy * (1 - fr), d = rx * nx + ry * ny;
                    if (d < 0) { rx -= d * nx; ry -= d * ny; }
                    dx = rx; dy = ry;
                }
            }
            return (bx, by, contacts, stuck);
        }
        var naive = BoxSlide(0);
        r.Note($"emulated.box-slide without skin: box at ({naive.X:F4},{naive.Y:F4}), stuck iterations={naive.Stuck} of 20 " +
               "(touching counts as initial overlap: fraction 0, zero normal)");
        const float skin = 0.01f;
        var slide = BoxSlide(skin);
        r.Check("emulated.box-slide", Near(slide.X, 4.7f - skin, tol) && Near(slide.Y, 0.8f + skin, tol) &&
                                      slide.Contacts.Contains(L.Ground) && slide.Contacts.Contains(L.Wall),
            $"skin {skin} m: box at ({slide.X:F4},{slide.Y:F4}) (expected {4.7f - skin}, {0.8f + skin}), " +
            $"contacts={string.Join(",", slide.Contacts)}, stuck iterations={slide.Stuck}");

        // 8. Contact begin events with normals for a dynamic body; none between the kinematic player and statics.
        int crate = b.CreateBody(BodyKind.Dynamic, -8, 2);
        int crateShape = b.AddBox(crate, 0.25f, 0.25f, 0, 0, Layers.Dynamic, Layers.All, ShapeFlags.ContactEvents);
        for (int i = 0; i < 90; i++) L.Step();
        var ct = b.GetTransform(crate);
        var crateContact = L.Contacts.FirstOrDefault(c => (c.A == crateShape && c.B == L.Ground) || (c.A == L.Ground && c.B == crateShape));
        bool hasCrate = crateContact != default;
        r.Check("events.contact-normal", hasCrate && Near(MathF.Abs(crateContact.Ny), 1, 0.001f) && Near(ct.Y, 0.25f, tol),
            $"crate rests at y={ct.Y:F4}; begin event ({crateContact.A},{crateContact.B}) normal=({crateContact.Nx:F3},{crateContact.Ny:F3})");
        int playerContacts = L.Contacts.Count(c => c.A == L.PlayerShape || c.B == L.PlayerShape);
        r.Note($"events: contact begin events involving the kinematic player (contact events enabled) = {playerContacts}; " +
               "Box2D creates no contacts between kinematic and static bodies, so the mover's planes are the player's contacts");

        // 9. One-way platform for dynamic bodies through the pre-solve hook.
        int jumper = b.CreateBody(BodyKind.Dynamic, 9, 1.0f);
        b.AddBox(jumper, 0.25f, 0.25f, 0, 0, Layers.Dynamic, Layers.All, ShapeFlags.None);
        b.SetVelocity(jumper, 0, 9);
        int dropper = b.CreateBody(BodyKind.Dynamic, 11, 4);
        b.AddBox(dropper, 0.25f, 0.25f, 0, 0, Layers.Dynamic, Layers.All, ShapeFlags.None);
        float peak = 0;
        for (int i = 0; i < 180; i++)
        {
            L.Step();
            peak = MathF.Max(peak, b.GetTransform(jumper).Y);
        }
        var (calls, disabled) = b.OneWayStats();
        float jy = b.GetTransform(jumper).Y, dy2 = b.GetTransform(dropper).Y;
        r.Check("oneway.dynamic-presolve", peak > 3 && Near(jy, 2.45f, tol) && Near(dy2, 2.45f, tol) && disabled > 0,
            $"jumper peak={peak:F3} rests at {jy:F4}; dropper rests at {dy2:F4} (expected 2.45); pre-solve calls={calls} disabled={disabled}");

        // 10. One-way platform for the kinematic mover: filtered in the mover (query mask while rising, plane filter).
        b.SetPosition(L.Player, 10.2f, 0.8f);
        L.Step();
        for (int i = 0; i < 10; i++) L.Move(0, 0.3f, out x, out y);
        float risen = y;
        for (int i = 0; i < 15; i++) n = L.Move(0, -0.2f, out x, out y);
        r.Check("oneway.mover", Near(risen, 3.8f, tol) && Near(y, 3.0f, tol) && L.Touched(n, L.OneWay) && L.Grounded(n),
            $"rose through to y={risen:F4} (expected 3.8), landed at y={y:F4} (expected 3.0), on platform={L.Touched(n, L.OneWay)}");

        return r;
    }
}

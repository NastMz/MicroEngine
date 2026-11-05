using System.Numerics;
using System.Text;
using MicroEngine.Kernel;
using MicroEngine.Physics;
using MicroEngine.Physics.Tiles;
using MicroEngine.Scenes;
using MicroEngine.World;

namespace MicroEngine.Tests.Physics;

/// <summary>
/// The tile backend's broad phase (a grid of cells over its box shapes) changes how much it looks at, never what it
/// finds: every result is the one a scan of every shape gives, in the same order, so state traces stay identical.
/// </summary>
[TestClass]
public sealed class TileBroadPhaseTests
{
    private const int Tile = 16, Width = 48, Height = 30;

    private static TileGrid Level()
    {
        var layer = new TileLayer(Width, Height);
        for (int x = 0; x < Width; x++) { layer[x, Height - 1] = 1; layer[x, 0] = 1; }
        for (int y = 0; y < Height; y++) { layer[0, y] = 1; layer[Width - 1, y] = 1; }
        for (int p = 0; p < 10; p++)
        for (int x = 0; x < 5; x++) layer[3 + p * 5 % 40 + x, 5 + p * 7 % 20] = 2;
        return new TileGrid(layer, Tile, new Dictionary<int, TileFlags> { [1] = TileFlags.Solid, [2] = TileFlags.OneWay });
    }

    /// <summary>Runs one scripted history and writes every result, event and the state hash of each step.</summary>
    private static string Run(TilePhysicsWorld2D world, out int contacts, out int sensorEvents)
    {
        var log = new StringBuilder();
        contacts = sensorEvents = 0;
        var level = world.CreateBody(new EntityId(1, 1), BodyDescription.Static());
        world.AddTiles(level, Level());
        world.AddShape(world.CreateBody(new EntityId(2, 1), BodyDescription.Static(new Vector2(380, 470))), ShapeDescription.Box(new Vector2(700, 6)));
        for (int i = 0; i < 24; i++)
        {
            var b = world.CreateBody(new EntityId(3, 1), BodyDescription.Static(new Vector2(40 + i * 29 % 680, 90 + i * 53 % 330)));
            world.AddShape(b, ShapeDescription.Box(new Vector2(24, 8)) with { OneWay = i % 5 == 0 });
        }
        for (int i = 0; i < 12; i++)
        {
            var s = world.CreateBody(new EntityId(4, 1), BodyDescription.Static(new Vector2(60 + i * 61 % 640, 80 + i * 37 % 360)));
            world.AddShape(s, ShapeDescription.Sensor(new Vector2(32, 32)));
        }
        var lift = world.CreateBody(new EntityId(5, 1), BodyDescription.Kinematic(new Vector2(300, 400)) with { PushesBodies = true });
        world.AddShape(lift, ShapeDescription.Box(new Vector2(60, 8)));
        var bodies = new List<BodyId>();
        var velocity = new List<Vector2>();
        void Spawn(int i)
        {
            var id = world.CreateBody(new EntityId((uint)(100 + i), 1), BodyDescription.Kinematic(new Vector2(24 + i * 37 % 700, 24 + i * 13 % 400)));
            world.AddShape(id, ShapeDescription.Box(new Vector2(12, 14)));
            bodies.Add(id);
            velocity.Add(new Vector2(i % 2 == 0 ? 70 : -70, 0));
        }
        for (int i = 0; i < 120; i++) Spawn(i);
        Span<ShapeId> found = stackalloc ShapeId[16];
        const float dt = 1 / 60f;
        for (int step = 0; step < 300; step++)
        {
            world.Move(lift, new Vector2(MathF.Sin(step * 0.05f) * 2, MathF.Cos(step * 0.05f) * 1.5f));
            for (int i = 0; i < bodies.Count; i++)
            {
                if (!world.IsAlive(bodies[i])) continue;
                var v = velocity[i];
                v.Y = MathF.Min(v.Y + 900 * dt, 600);
                var r = world.Move(bodies[i], v * dt, new QueryFilter { PassOneWay = (step + i) % 97 == 0 });
                if (r.OnGround || r.HitCeiling) v.Y = r.OnGround && step % 60 == i % 60 ? -330 : 0;
                if (r.HitWall) v.X = -v.X;
                velocity[i] = v;
                contacts += r.TotalContacts;
                log.Append(BitConverter.SingleToInt32Bits(r.Position.X)).Append(',').Append(BitConverter.SingleToInt32Bits(r.Position.Y))
                    .Append(r.OnGround ? 'g' : '-').Append(r.HitWall ? 'w' : '-').Append(r.HitCeiling ? 'c' : '-');
                foreach (var c in r.Contacts) log.Append(c.Shape).Append(c.Normal);
                log.Append('|');
            }
            if (step % 37 == 5) world.SetPosition(bodies[step % bodies.Count], new Vector2(100 + step % 500, 60));
            if (step % 50 == 25) world.DestroyBody(bodies[step % bodies.Count]);
            if (step % 50 == 30) Spawn(200 + step);
            world.Step(dt);
            sensorEvents += world.SensorEvents.Length;
            foreach (var e in world.SensorEvents) log.Append(e.Kind).Append(e.Sensor).Append(e.Visitor);
            for (int k = 0; k < 4; k++)
            {
                var from = new Vector2(30 + (step * 41 + k * 173) % 700, 20 + (step * 17 + k * 59) % 200);
                bool hit = world.RayCast(from, from + new Vector2(MathF.Sin(step + k) * 300, 250), new QueryFilter { IncludeSensors = k == 2 }, out var h);
                log.Append(hit ? $"{h.Shape}{h.Point}{h.Normal}{h.Fraction}" : "miss");
                int n = world.Overlap(new RectF(from.X - 40, from.Y, 80 + k * 40, 60), new QueryFilter { IncludeSensors = k == 1 }, found);
                log.Append(n);
                foreach (var s in found[..Math.Min(n, found.Length)]) log.Append(s);
            }
            var hash = new StateHasher();
            world.HashState(hash);
            log.Append('#').Append(hash.Value).Append('\n');
        }
        return log.ToString();
    }

    [TestMethod]
    public void Every_move_query_and_event_is_the_one_a_scan_of_every_shape_gives()
    {
        string full = Run(new TilePhysicsWorld2D(TilePhysicsWorld2D.DefaultCellSize, fullScan: true), out int contacts, out int sensors);
        Assert.IsTrue(contacts > 1000 && sensors > 20, $"the history exercises the world: {contacts} contacts, {sensors} sensor events");
        foreach (float cell in new[] { 16f, TilePhysicsWorld2D.DefaultCellSize, 300f })
            Assert.AreEqual(full, Run(new TilePhysicsWorld2D(cell, fullScan: false), out _, out _), $"cells of {cell}");
        Assert.AreEqual(full, Run(new TilePhysicsWorld2D(), out _, out _), "the public world uses the broad phase with the default cell");
    }

    [TestMethod]
    public void The_grid_files_a_shape_in_the_cells_it_covers_and_large_shapes_where_every_query_sees_them()
    {
        var grid = new ShapeGrid(10);
        grid.Update(3, new RectF(0, 0, 5, 5));
        grid.Update(1, new RectF(25, 25, 10, 10));
        grid.Update(7, new RectF(-500, 0, 1000, 4)); // 100 cells wide: a large shape
        CollectionAssert.AreEqual(new[] { 3, 7 }, grid.Query(new RectF(1, 1, 2, 2)).ToArray());
        CollectionAssert.AreEqual(new[] { 1, 3, 7 }, grid.Query(new RectF(0, 0, 30, 30)).ToArray(), "ascending slot order");
        grid.Update(3, new RectF(26, 26, 2, 2));
        CollectionAssert.AreEqual(new[] { 7 }, grid.Query(new RectF(1, 1, 2, 2)).ToArray(), "refiled where it moved");
        CollectionAssert.AreEqual(new[] { 1, 3, 7 }, grid.Query(new RectF(26, 26, 1, 1)).ToArray());
        grid.Remove(1);
        grid.Remove(1);
        CollectionAssert.AreEqual(new[] { 3, 7 }, grid.Query(new RectF(20, 20, 20, 20)).ToArray());
        CollectionAssert.AreEqual(new[] { 3, 7 }, grid.Query(new RectF(-1e6f, -1e6f, 2e6f, 2e6f)).ToArray(), "a query larger than the grid walks the filed cells");
    }
}

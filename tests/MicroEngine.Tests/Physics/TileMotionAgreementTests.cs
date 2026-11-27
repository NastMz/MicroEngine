using System.Numerics;
using MicroEngine.Physics;
using MicroEngine.Physics.Tiles;
using MicroEngine.World;

namespace MicroEngine.Tests.Physics;

/// <summary>
/// A top-down character on the tile backend against <see cref="TileMotion.Move"/> over the same grid with the same
/// walk: where the two agree bit for bit, and where and why they do not.
/// </summary>
[TestClass]
public sealed class TileMotionAgreementTests
{
    private sealed class Room : ICollisionGrid
    {
        private readonly bool[,] _solid = new bool[20, 12];

        public Room()
        {
            var random = new Random(7);
            for (int y = 0; y < 12; y++)
            for (int x = 0; x < 20; x++)
                _solid[x, y] = x == 0 || y == 0 || x == 19 || y == 11 || (random.Next(7) == 0 && !(x is > 8 and < 12 && y is > 4 and < 8));
        }

        public int TileSize => 16;
        public TileFlags FlagsAt(int x, int y) => x < 0 || y < 0 || x >= 20 || y >= 12 || _solid[x, y] ? TileFlags.Solid : TileFlags.None;
    }

    private readonly record struct Outcome(int Apart, float Worst, int FlagsApart, int Grazes);

    /// <summary>6000 steps of seeded directions; <paramref name="offset"/> is the box's centre relative to the body's origin.</summary>
    private static Outcome Walk(Vector2 offset)
    {
        var grid = new Room();
        var world = new TilePhysicsWorld2D();
        world.AddTiles(world.CreateBody(default, BodyDescription.Static()), grid);
        var size = new Vector2(10, 10);
        var start = new Vector2(155.3f, 91.7f);
        var cornerToOrigin = size / 2 - offset;
        var hero = TopDownController2D.Create(world, default, start + cornerToOrigin, size, new TopDownMotion { MoveSpeed = 70 }, offset: offset);
        var box = new RectF(start.X, start.Y, size.X, size.Y);
        var random = new Random(3);
        var move = Vector2.Zero;
        int apart = 0, flagsApart = 0, grazes = 0;
        float worst = 0;
        for (int i = 0; i < 6000; i++)
        {
            if (i % 23 == 0) move = new Vector2(random.Next(-1, 2), random.Next(-1, 2));
            var direction = move.Length() > 1 ? move / move.Length() : move;
            var delta = direction * 70 * (1 / 60f);
            var result = hero.Step(1 / 60f, move);
            var tile = TileMotion.Move(box, delta, grid);
            box = tile.Bounds;
            var corner = hero.Position - cornerToOrigin;
            bool same = corner == box.Position;
            bool sameFlags = result.StoppedGoing(new Vector2(-1, 0)) == tile.HitLeft && result.StoppedGoing(new Vector2(1, 0)) == tile.HitRight
                && result.StoppedGoing(new Vector2(0, -1)) == tile.HitCeiling && result.StoppedGoing(new Vector2(0, 1)) == tile.HitFloor;
            if (!same)
            {
                apart++;
                worst = MathF.Max(worst, (corner - box.Position).Length());
            }
            if (!sameFlags) flagsApart++;
            if (!same || !sameFlags)
            {
                // The one rule that differs: a move toward smaller coordinates that ends less than the world's
                // tolerance inside a tile. TileMotion pushes the box out and reports the hit; the world lets it be.
                var inside = box.Position - corner;
                if ((tile.HitLeft || tile.HitCeiling) && inside.X >= 0 && inside.Y >= 0 && inside.X < 1e-3f && inside.Y < 1e-3f) grazes++;
                // The world goes on from where it is; the comparison goes on from the helper's box.
                hero.Position = box.Position + cornerToOrigin;
            }
        }
        return new Outcome(apart, worst, flagsApart, grazes);
    }

    [TestMethod]
    public void With_the_origin_at_the_box_corner_the_controller_moves_as_TileMotion_does()
    {
        var outcome = Walk(offset: new Vector2(5, 5));
        // Every step is identical, position and stopped sides, except a graze: one step of this walk.
        Assert.IsTrue(outcome.Apart <= 2, $"positions apart in {outcome.Apart} steps");
        Assert.AreEqual(outcome.Apart, outcome.Grazes, "every difference is a move that ended less than 0.001 inside a tile");
        Assert.AreEqual(outcome.Apart, outcome.FlagsApart, "and the stopped sides differ only there");
        Assert.IsTrue(outcome.Worst < 1e-3f, $"worst {outcome.Worst}");
    }

    [TestMethod]
    public void With_the_origin_at_the_centre_the_last_bits_differ_and_nothing_else()
    {
        // The body's position is its origin: a centre adds and removes half the size around every move, which
        // rounds differently from moving the corner. Hundreds of steps differ, none by a thousandth of a unit.
        var outcome = Walk(offset: Vector2.Zero);
        Assert.IsTrue(outcome.Apart > 100, $"positions apart in {outcome.Apart} steps");
        Assert.IsTrue(outcome.Worst < 1e-3f, $"worst {outcome.Worst}");
        Assert.IsTrue(outcome.FlagsApart <= 2, $"stopped sides apart in {outcome.FlagsApart} steps");
    }
}

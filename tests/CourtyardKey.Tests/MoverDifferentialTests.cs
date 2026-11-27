using System.Numerics;
using CourtyardKey.Entities;
using CourtyardKey.Scenes;
using MicroEngine;
using MicroEngine.Physics;
using MicroEngine.Physics.Tiles;
using MicroEngine.World;

namespace CourtyardKey.Tests;

/// <summary>
/// Every move the game makes with <see cref="TileMotion"/> during two whole playthroughs, repeated on the tile physics
/// world from the same start: the same tiles, the same solid actors, the same box (the body's origin at its top-left
/// corner) and the same delta. The end position must be equal bit for bit, and the sides that stopped the move too.
/// </summary>
[TestClass]
public sealed class MoverDifferentialTests
{
    private const ulong Tiles = 1, Solids = 2, Mover = 4;

    [TestMethod]
    public void TheTileWorldMovesEveryActorAsTileMotionDoes()
    {
        var worlds = new Dictionary<ICollisionGrid, TilePhysicsWorld2D>(ReferenceEqualityComparer.Instance);
        var calls = new Dictionary<string, int>();
        var apart = new Dictionary<string, int>();
        var flags = new Dictionary<string, int>();
        string? first = null;
        float worst = 0;
        int total = 0;
        PlayScene.MoveObserved = (actor, delta, grid, solids, expected) =>
        {
            if (!worlds.TryGetValue(grid, out var world))
            {
                worlds[grid] = world = new TilePhysicsWorld2D();
                world.AddTiles(world.CreateBody(default, BodyDescription.Static()), grid, 4096, 4096, Tiles);
            }
            var box = actor.Bounds;
            var obstacles = world.CreateBody(default, BodyDescription.Static());
            foreach (var s in solids)
                world.AddShape(obstacles, ShapeDescription.Box(s.Size, s.Center) with { Category = Solids });
            var body = world.CreateBody(default, BodyDescription.Kinematic(box.Position));
            world.AddShape(body, ShapeDescription.Box(box.Size, box.Size / 2) with { Category = Mover, Mask = Tiles | Solids });
            var move = world.Move(body, delta, new QueryFilter(Mover, Tiles | Solids));
            var got = world.PositionOf(body);
            world.DestroyBody(body);
            world.DestroyBody(obstacles);

            string kind = actor.GetType().Name;
            total++;
            calls[kind] = calls.GetValueOrDefault(kind) + 1;
            bool samePlace = got == expected.Position;
            bool sameSides = move.StoppedGoing(-Vector2.UnitX) == expected.HitLeft && move.StoppedGoing(Vector2.UnitX) == expected.HitRight
                && move.StoppedGoing(-Vector2.UnitY) == expected.HitCeiling && move.StoppedGoing(Vector2.UnitY) == expected.HitFloor;
            if (!samePlace) { apart[kind] = apart.GetValueOrDefault(kind) + 1; worst = MathF.Max(worst, (got - expected.Position).Length()); }
            if (!sameSides) flags[kind] = flags.GetValueOrDefault(kind) + 1;
            if (!samePlace || !sameSides)
                first ??= FormattableString.Invariant($"move {total}, {kind} in {actor.World.Room.Key}: box {box.X:R},{box.Y:R} {box.Width}x{box.Height}, delta {delta.X:R},{delta.Y:R}, {solids.Count} solids; TileMotion {expected.Position.X:R},{expected.Position.Y:R} L{expected.HitLeft} R{expected.HitRight} U{expected.HitCeiling} D{expected.HitFloor}; world {got.X:R},{got.Y:R} L{move.StoppedGoing(-Vector2.UnitX)} R{move.StoppedGoing(Vector2.UnitX)} U{move.StoppedGoing(-Vector2.UnitY)} D{move.StoppedGoing(Vector2.UnitY)}");
        };
        try
        {
            new PlaythroughTests().TheOpeningCanBeCompletedFromTheTitleToTheDungeon();
            new DungeonRunTests().TheWholeDungeonAndTheEndingCanBeCompleted();
        }
        finally { PlayScene.MoveObserved = null; }

        string Table(Dictionary<string, int> d) => string.Join(", ", d.OrderBy(p => p.Key).Select(p => $"{p.Key} {p.Value}"));
        string report = $"{total} moves ({Table(calls)}); position apart in {apart.Values.Sum()} ({Table(apart)}), worst {worst:R} px; sides apart in {flags.Values.Sum()} ({Table(flags)}); first: {first}";
        Console.WriteLine(report);
        Assert.IsNull(first, report);
    }
}

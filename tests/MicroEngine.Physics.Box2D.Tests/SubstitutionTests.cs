using System.Numerics;
using MicroEngine.Physics;
using MicroEngine.Physics.Box2D;
using MicroEngine.Physics.Tiles;
using PhysicsSubstitution;

namespace MicroEngine.Physics.Box2D.Tests;

/// <summary>
/// Physics substitution, scenario 1: the sample's platformer gameplay code (samples/PhysicsSubstitution) runs unchanged
/// on each backend and has the properties the common contract fixes. Each backend runs the same tests; only the world
/// they are given differs.
/// </summary>
public abstract class SubstitutionScenario
{
    protected abstract IPhysicsWorld2D CreateWorld();

    private IPhysicsWorld2D? _world;

    [TestCleanup]
    public void Free() => (_world as IDisposable)?.Dispose();

    private Platformer Play(out List<Vector2> trace)
    {
        _world = CreateWorld();
        var game = new Platformer(_world);
        trace = game.Play();
        return game;
    }

    private float Tolerance => _world!.Skin + 1e-3f;
    private (Vector2 Position, MoveResult Move) Part(Platformer game, string name) { var p = game.Parts.Single(p => p.Part == name); return (p.Position, p.Move); }
    private static float Bottom(Vector2 hero) => hero.Y + Platformer.HeroSize.Y / 2;

    [TestMethod]
    public void The_hero_lands_on_the_ground()
    {
        var game = Play(out _);
        var (at, move) = Part(game, "land");
        float gap = 128 - Bottom(at); // the floor's top is row 8, y 128
        Assert.IsTrue(gap >= -1e-3f && gap <= Tolerance, $"resting gap {gap}");
        Assert.IsTrue(move.OnGround && game.Level.Contains(move.Ground), move.ToString());
    }

    [TestMethod]
    public void The_pillar_blocks_the_hero()
    {
        var game = Play(out _);
        var (at, move) = Part(game, "walk into the pillar");
        float gap = 288 - (at.X + Platformer.HeroSize.X / 2); // the pillar's left side is column 18, x 288
        Assert.IsTrue(gap >= -1e-3f && gap <= Tolerance, $"gap to the pillar {gap}");
        Assert.IsTrue(move.HitWall && move.OnGround, move.ToString());
    }

    [TestMethod]
    public void A_one_way_platform_lets_the_hero_jump_up_through_it_land_on_it_and_drop_through_it()
    {
        var game = Play(out _);
        var (on, move) = Part(game, "jump onto the platform");
        float gap = 64 - Bottom(on); // its top is row 4, y 64
        Assert.IsTrue(gap >= -1e-3f && gap <= Tolerance, $"on the platform: gap {gap}");
        Assert.IsTrue(move.OnGround, move.ToString());
        var (below, _) = Part(game, "drop through it");
        Assert.AreEqual(128, Bottom(below), Tolerance, "dropped back to the floor");
    }

    [TestMethod]
    public void Sensor_events_fire_for_the_coins_and_the_goal()
    {
        var game = Play(out _);
        Assert.AreEqual(2, game.Collected.Count, string.Join(", ", game.Collected));
        Assert.IsTrue(game.GoalStep > 0, "the goal reported the hero entering it");
        Assert.IsTrue(game.Collected.All(c => game.Coins.Contains(c.Coin)));
    }

    [TestMethod]
    public void A_fast_hero_does_not_tunnel_through_the_floor_or_the_pillar()
    {
        var game = Play(out _);
        var filter = new QueryFilter(Platformer.HeroLayer, Platformer.LevelLayer);
        _world!.SetPosition(game.Hero, new Vector2(100, 30));
        var fall = _world.Move(game.Hero, new Vector2(0, 400), filter); // 25 tiles in one move onto a 1-tile floor
        Assert.IsTrue(fall.OnGround, fall.ToString());
        Assert.AreEqual(128, Bottom(_world.PositionOf(game.Hero)), Tolerance);
        var dash = _world.Move(game.Hero, new Vector2(500, 0), filter); // through 11 tiles at once into the 4-tile pillar
        Assert.IsTrue(dash.HitWall, dash.ToString());
        Assert.AreEqual(288 - Platformer.HeroSize.X / 2, _world.PositionOf(game.Hero).X, Tolerance);
    }
}

[TestClass]
public sealed class TileSubstitutionScenario : SubstitutionScenario
{
    protected override IPhysicsWorld2D CreateWorld() => new TilePhysicsWorld2D();
}

[TestClass]
public sealed class Box2DSubstitutionScenario : SubstitutionScenario
{
    protected override IPhysicsWorld2D CreateWorld() => Box2DTesting.Create(new Box2DWorldOptions { UnitsPerMeter = 16, YDown = true });
    [TestCleanup] public void FreeBox2D() => Box2DTesting.Free();
}

/// <summary>
/// What differs between the backends in scenario 1, within what the contract allows, measured on the same run; and
/// scenario 2, the capability scenario.
/// </summary>
[TestClass]
public sealed class SubstitutionDifferences
{
    [TestInitialize]
    public void Natives() => Box2DTesting.RequireNatives();

    [TestCleanup]
    public void Free() => Box2DTesting.Free();

    [TestMethod]
    public void The_same_run_differs_only_by_the_resting_gap_and_by_at_most_one_step_of_event_timing()
    {
        var tiles = new Platformer(new TilePhysicsWorld2D());
        var box2dWorld = Box2DTesting.Create(new Box2DWorldOptions { UnitsPerMeter = 16, YDown = true });
        var box2d = new Platformer(box2dWorld);
        var a = tiles.Play();
        var b = box2d.Play();
        Assert.AreEqual(a.Count, b.Count);
        float worst = a.Zip(b).Max(p => Vector2.Distance(p.First, p.Second));
        Assert.IsTrue(worst <= box2dWorld.Skin + 1e-3f, $"the trajectories stay within Box2D's skin ({box2dWorld.Skin} px): worst {worst}");
        Assert.AreEqual(tiles.Collected.Count, box2d.Collected.Count);
        foreach (var (t, x) in tiles.Collected.Zip(box2d.Collected))
            Assert.IsTrue(Math.Abs(t.Step - x.Step) <= 1, $"a coin's entry: step {t.Step} on tiles, {x.Step} on Box2D");
        Assert.IsTrue(Math.Abs(tiles.GoalStep - box2d.GoalStep) <= 1, $"the goal: step {tiles.GoalStep} on tiles, {box2d.GoalStep} on Box2D");
    }

    /// <summary>Scenario 2: dynamic crates run on Box2D and are refused with the typed error on the tile backend.</summary>
    [TestMethod]
    public void Dynamic_crates_settle_on_Box2D_and_are_refused_cleanly_by_the_tile_backend()
    {
        var box2d = Box2DTesting.Create(new Box2DWorldOptions { UnitsPerMeter = 16, YDown = true });
        var crates = Crates.Run(box2d);
        Assert.AreEqual(5, crates.Count);
        Assert.IsTrue(crates.All(c => c.Position.Y < 128 + box2d.Skin), "every crate is above the floor's top (y 128)");
        Assert.IsTrue(crates.Any(c => MathF.Abs(c.Rotation) > 0.05f), "the falling crate tipped something over");

        var tiles = new TilePhysicsWorld2D();
        var e = Assert.ThrowsExactly<PhysicsCapabilityException>(() => Crates.Run(tiles));
        Assert.AreEqual(PhysicsCapabilities.DynamicBodies, e.Missing);
        Assert.AreEqual("tiles", e.Backend);
        Assert.AreEqual(1, tiles.Bodies.Length, "only the floor exists: the refused crate was never created");
    }
}

using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Physics;
using MicroEngine.Physics.Box2D;
using PhysicsPlayground;

namespace Samples.Tests;

/// <summary>The physics playground: joints and dynamics on a scene's Box2D world. Skipped without the native library of this machine.</summary>
[TestClass]
public sealed class PhysicsPlaygroundTests
{
    private static (PlaygroundGame Game, PlaygroundScene Scene) Start()
    {
        if (!Box2DPhysicsWorld2D.IsSupported) Assert.Inconclusive("The me_box2d native library of this runtime is missing: run src/MicroEngine.Physics.Box2D/native/build.sh <rid>.");
        var game = new PlaygroundGame();
        game.InitializeForTest(HeadlessPlatform.For(game.Settings));
        return (game, (PlaygroundScene)game.Scenes.Current!);
    }

    private static void Step(Game game, int steps)
    {
        for (int i = 0; i < steps; i++) game.StepForTest();
    }

    [TestMethod]
    public void The_chain_hangs_the_rope_holds_the_lantern_and_the_motors_drive_the_cart()
    {
        var (game, scene) = Start();
        var world = scene.Physics.World;
        Assert.IsInstanceOfType<Box2DPhysicsWorld2D>(world, "the game named the backend once");
        Step(game, 120);
        var end = world.PositionOf(scene.ChainEnd);
        Assert.AreEqual(60, end.X, 2, "the chain hangs straight down from its pin");
        Assert.AreEqual(83, end.Y, 2);
        float rope = Vector2.Distance(world.AnchorAOf(scene.Rope), world.AnchorBOf(scene.Rope));
        Assert.IsTrue(rope is > 39 and < 41, $"the lantern hangs at the rope's length: {rope}");
        float x0 = world.PositionOf(scene.Chassis).X;
        game.Input.SetForced("right", true);
        Step(game, 90);
        game.Input.SetForced("right", false);
        Assert.IsTrue(world.PositionOf(scene.Chassis).X > x0 + 40, $"the cart drove right: {x0} -> {world.PositionOf(scene.Chassis).X}");
        float up = Vector2.Dot(world.VelocityOf(scene.Chassis), world.Up);
        game.Input.SetForced("confirm", true);
        Step(game, 1);
        game.Input.SetForced("confirm", false);
        Assert.IsTrue(Vector2.Dot(world.VelocityOf(scene.Chassis), world.Up) > up + 60, "the hop is an impulse");
    }

    [TestMethod]
    public void Impacts_are_counted_the_beam_lists_what_is_ahead_and_a_blast_throws_the_crates()
    {
        var (game, scene) = Start();
        var world = scene.Physics.World;
        Step(game, 120);
        Assert.IsTrue(scene.Impacts >= 1, $"the cart and the crates landed: {scene.Impacts} impacts");
        Assert.IsTrue(scene.HardestImpact > 0);
        Assert.IsTrue(scene.Beam.Length >= 2, $"the beam goes through the crates to the wall: {scene.Beam.Length}");
        for (int i = 1; i < scene.Beam.Length; i++)
            Assert.IsTrue(scene.Beam[i].Fraction >= scene.Beam[i - 1].Fraction, "nearest first");
        Assert.AreEqual(BodyKind.Static, world.KindOf(world.BodyOf(scene.Beam[^1].Shape)), "the last thing it reaches is the room");

        // Drive up to the crates, then blast: they fly away from the cart.
        game.Input.SetForced("right", true);
        Step(game, 60);
        game.Input.SetForced("right", false);
        var crates = new List<BodyId>();
        foreach (var body in world.Bodies)
            if (world.KindOf(body) == BodyKind.Dynamic && Math.Abs(world.MassOf(body) - 0.5f * 144 / 256) < 0.01f) crates.Add(body);
        Assert.AreEqual(3, crates.Count);
        float before = crates.Max(c => world.VelocityOf(c).Length());
        game.Input.SetForced("cancel", true);
        Step(game, 1);
        game.Input.SetForced("cancel", false);
        Assert.AreEqual(1, scene.Blasts);
        float after = crates.Max(c => world.VelocityOf(c).Length());
        Assert.IsTrue(after > before + 40, $"the blast threw a crate: {before} -> {after}");
    }

    [TestMethod]
    public void The_same_run_gives_the_same_state_hash_and_the_world_ends_with_the_scene()
    {
        ulong Run(out PlaygroundGame game, out IPhysicsWorld2D world)
        {
            var (g, scene) = Start();
            g.Input.SetForced("right", true);
            Step(g, 150);
            game = g;
            world = scene.Physics.World;
            return g.ComputeStateHash().Hash;
        }
        ulong first = Run(out var a, out var worldA);
        ulong second = Run(out var b, out var worldB);
        Assert.AreEqual(first, second);
        a.Scenes.Pop();
        b.Scenes.Pop();
        Step(a, 1);
        Step(b, 1);
        Assert.ThrowsExactly<ObjectDisposedException>(() => worldA.Step(1 / 60f), "the scene disposed its world");
        Assert.ThrowsExactly<ObjectDisposedException>(() => worldB.Step(1 / 60f));
    }
}

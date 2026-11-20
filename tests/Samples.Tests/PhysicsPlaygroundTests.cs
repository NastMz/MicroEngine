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

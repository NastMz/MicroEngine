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
    public void The_classic_character_skids_clears_a_corner_and_finds_the_hidden_block()
    {
        var (game, _) = Start();
        var scene = new ClassicScene();
        game.Scenes.Push(scene);
        Step(game, 30);
        var hero = scene.Hero;
        Assert.IsTrue(hero.IsGrounded);
        float floor = hero.Position.Y;

        // Run right, then reverse: a skid.
        game.Input.SetForced("right", true);
        Step(game, 40);
        game.Input.SetForced("right", false);
        game.Input.SetForced("left", true);
        Step(game, 6);
        game.Input.SetForced("left", false);
        Assert.IsTrue(scene.SkidSteps >= 2, $"reversing at speed skids: {scene.SkidSteps} steps");
        Step(game, 40);

        // Under the edge of the ledge (x 90..150): the head would clip 2 pixels of it and is nudged past.
        hero.Teleport(new Vector2(86, floor));
        hero.Velocity = Vector2.Zero;
        Step(game, 5);
        game.Input.SetForced("confirm", true);
        float highest = floor;
        for (int i = 0; i < 40; i++)
        {
            Step(game, 1);
            highest = MathF.Min(highest, hero.Position.Y);
        }
        game.Input.SetForced("confirm", false);
        Assert.AreEqual(1, scene.Corrections, "nudged past the corner once");
        Assert.IsTrue(highest < 110, $"the jump went on past the ledge: {highest}");
        Step(game, 90);

        // The hidden block (x 202..218, y 110..126): falling onto it goes through; a head from below finds it.
        hero.Teleport(new Vector2(210, 90));
        hero.Velocity = Vector2.Zero;
        Step(game, 90);
        Assert.AreEqual(floor, hero.Position.Y, 0.5f, "it fell through the hidden block to the floor");
        Assert.IsFalse(scene.Revealed);
        game.Input.SetForced("confirm", true);
        Step(game, 30);
        game.Input.SetForced("confirm", false);
        Assert.IsTrue(scene.Revealed, "the head found it");
        Step(game, 60);
        hero.Teleport(new Vector2(210, 90));
        hero.Velocity = Vector2.Zero;
        Step(game, 90);
        Assert.AreEqual(110, hero.Position.Y, 0.5f, "found, it is a block to stand on: its feet on the block's top");
        Assert.IsTrue(scene.HardestLanding > 100, $"landings report their speed: {scene.HardestLanding}");

        // Run is held: it goes at the run's speed, and a running jump keeps that speed in the air with run released.
        hero.Teleport(new Vector2(30, floor));
        hero.Velocity = Vector2.Zero;
        Step(game, 10);
        game.Input.SetForced("right", true);
        game.Input.SetForced("cancel", true);
        Step(game, 40);
        Assert.AreEqual(152, hero.Velocity.X, 0.5f, "the run's speed");
        game.Input.SetForced("confirm", true);
        Step(game, 1);
        game.Input.SetForced("cancel", false);
        Step(game, 12);
        Assert.IsFalse(hero.IsGrounded);
        Assert.AreEqual(152, hero.Velocity.X, 0.5f, "a running jump keeps its speed in the air");
        game.Input.SetForced("confirm", false);
        game.Input.SetForced("right", false);
        Step(game, 90);

        // The plank (x 16..64, top at y 136): down alone stands, down and jump drops through to the floor.
        hero.Teleport(new Vector2(40, 130));
        hero.Velocity = Vector2.Zero;
        Step(game, 30);
        Assert.AreEqual(136, hero.Position.Y, 0.5f, "standing on the plank");
        game.Input.SetForced("down", true);
        Step(game, 10);
        Assert.AreEqual(136, hero.Position.Y, 0.5f, "down alone does not drop");
        game.Input.SetForced("confirm", true);
        Step(game, 1);
        Assert.IsTrue((hero.Events & CharacterEvents.DroppedThrough) != 0, "down and jump drops");
        game.Input.SetForced("confirm", false);
        game.Input.SetForced("down", false);
        Step(game, 60);
        Assert.AreEqual(floor, hero.Position.Y, 0.5f, "through the plank to the floor");
    }

    [TestMethod]
    public void The_tile_level_holds_what_rolls_over_it_and_an_explosion_of_Box2D_throws_it()
    {
        var (game, _) = Start();
        var scene = new TileScene();
        game.Scenes.Push(scene);
        Step(game, 1);
        var world = scene.Physics.World;
        Assert.AreEqual(PhysicsCapabilities.TileSlopes, world.Capabilities & PhysicsCapabilities.TileSlopes);
        Step(game, 240);
        foreach (var body in scene.Balls)
        {
            var at = world.PositionOf(body);
            Assert.IsTrue(at.X > 16 && at.X < 304 && at.Y > 16 && at.Y < 160, $"inside the level, on its tiles: {at}");
            Assert.IsTrue(world.VelocityOf(body).Length() < 30, $"come to rest or nearly: {world.VelocityOf(body)}");
        }
        Span<ShapeId> shapes = stackalloc ShapeId[4];
        Assert.AreEqual(1, world.ShapesOf(world.BodyOf(scene.Level), shapes), "the whole level is one shape");
        float before = scene.Balls.Max(b => world.VelocityOf(b).Length());
        game.Input.SetForced("confirm", true);
        Step(game, 2);
        game.Input.SetForced("confirm", false);
        Assert.AreEqual(1, scene.Explosions);
        Assert.IsTrue(scene.Balls.Max(b => world.VelocityOf(b).Length()) > before + 20, "the explosion threw something");
        Step(game, 120);
        foreach (var body in scene.Balls)
        {
            var at = world.PositionOf(body);
            Assert.IsTrue(at.X > 16 && at.X < 304 && at.Y > 16 && at.Y < 160, $"still inside the level: {at}");
        }
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

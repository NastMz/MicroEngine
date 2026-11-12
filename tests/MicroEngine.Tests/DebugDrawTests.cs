using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Physics;
using MicroEngine.Physics.Tiles;
using MicroEngine.Scenes;
using MicroEngine.World;

namespace MicroEngine.Tests;

/// <summary>DEBUG-1: the debug drawing queue and the physics debug view.</summary>
[TestClass]
public sealed class DebugDrawTests
{
    private static readonly Color Red = new(255, 0, 0);

    private sealed class BlankGame : Game
    {
        public override GameSettings Settings { get; } = new("debug draw", 32, 16) { Background = Color.Black };
        protected override void Load() => Scenes.Push(new Blank());

        private sealed class Blank : Scene
        {
            public override void Update(float dt) { }
            public override void Draw(IRenderer r) { }
        }
    }

    private static (BlankGame Game, HeadlessPlatform Platform) Start()
    {
        var game = new BlankGame();
        var platform = new HeadlessPlatform(32, 16) { Rasterize = true };
        game.InitializeForTest(platform);
        game.StepForTest();
        return (game, platform);
    }

    private static PixelImage Frame(BlankGame game, HeadlessPlatform platform)
    {
        game.Render(platform.BeginFrame());
        platform.EndFrame();
        return ((IViewCapture)platform).CaptureWorld();
    }

    [TestMethod]
    public void Calls_do_nothing_while_disabled()
    {
        var (game, platform) = Start();
        Assert.IsFalse(game.DebugDraw.Enabled);
        game.DebugDraw.Rect(new RectF(2.5f, 2.5f, 8, 8), Red, space: DebugSpace.Screen);
        game.DebugDraw.Text(Vector2.Zero, "x", Red);
        Assert.AreEqual(0, game.DebugDraw.Count);
        Assert.AreEqual(Color.Black, Frame(game, platform)[2, 6]);
    }

    [TestMethod]
    public void Items_are_drawn_above_the_scenes_for_one_frame_unless_they_last_longer()
    {
        var (game, platform) = Start();
        game.DebugDraw.Enabled = true;
        game.DebugDraw.Rect(new RectF(2.5f, 2.5f, 8, 8), Red, space: DebugSpace.Screen);
        game.DebugDraw.Line(new Vector2(20, 12.5f), new Vector2(30, 12.5f), Color.White, seconds: 5, space: DebugSpace.Screen);
        Assert.AreEqual(2, game.DebugDraw.Count);
        var first = Frame(game, platform);
        Assert.AreEqual(Red, first[2, 6], "the rectangle's left edge");
        Assert.AreEqual(Color.Black, first[6, 6], "an outline, not a fill");
        Assert.AreEqual(Color.White, first[25, 12]);
        Assert.AreEqual(1, game.DebugDraw.Count, "the one-frame item is gone, the five-second one stays");

        var second = Frame(game, platform);
        Assert.AreEqual(Color.Black, second[2, 6]);
        Assert.AreEqual(Color.White, second[25, 12]);
        game.DebugDraw.Enabled = false;
        Assert.AreEqual(0, game.DebugDraw.Count, "turning it off drops what is pending");
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => { game.DebugDraw.Enabled = true; game.DebugDraw.Line(Vector2.Zero, Vector2.One, Red, seconds: -1); });
    }

    [TestMethod]
    public void World_items_follow_the_scene_camera_and_screen_items_do_not()
    {
        var (game, platform) = Start();
        game.Scenes.Current!.WorldCamera = new Camera2D(32, 16) { Position = new Vector2(10, 0) };
        game.DebugDraw.Enabled = true;
        game.DebugDraw.Line(new Vector2(12.5f, 0), new Vector2(12.5f, 16), Red);
        game.DebugDraw.Line(new Vector2(12.5f, 0), new Vector2(12.5f, 16), Color.White, space: DebugSpace.Screen);
        var frame = Frame(game, platform);
        Assert.AreEqual(Red, frame[2, 8], "world x 12.5 is 2.5 on screen");
        Assert.AreEqual(Color.White, frame[12, 8]);
    }

    private sealed class Cells : ICollisionGrid
    {
        public int TileSize => 4;
        public TileFlags FlagsAt(int x, int y) => (x, y) switch { (0, 3) or (1, 3) => TileFlags.Solid, (2, 3) => TileFlags.OneWay, _ => TileFlags.None };
    }

    [TestMethod]
    public void The_physics_view_draws_every_shape_the_tiles_in_view_and_nothing_while_disabled()
    {
        var (game, platform) = Start();
        var world = new TilePhysicsWorld2D();
        var level = world.CreateBody(default, BodyDescription.Static());
        world.AddTiles(level, new Cells());
        var player = world.CreateBody(new EntityId(1, 1), BodyDescription.Kinematic(new Vector2(20.5f, 6.5f)));
        world.AddShape(player, ShapeDescription.Box(new Vector2(4, 4)));
        world.AddShape(player, ShapeDescription.Sensor(new Vector2(8, 8)));

        game.DebugDraw.Physics(world);
        Assert.AreEqual(0, game.DebugDraw.Count, "disabled: nothing is read or queued");

        game.DebugDraw.Enabled = true;
        game.DebugDraw.Physics(world);
        Assert.AreEqual(4 + 4 + 2 + 1, game.DebugDraw.Count, "box and sensor outlines, two solid tiles, one one-way top");
        var frame = Frame(game, platform);
        Assert.AreEqual(DebugDraw.PhysicsColors.Kinematic, frame[18, 6], "the player box's left edge at x 18.5");
        Assert.IsTrue(Enumerable.Range(0, 8).Any(x => Enumerable.Range(12, 4).Any(y => frame[x, y] == DebugDraw.PhysicsColors.Static)), "the solid tiles, rows 12 to 15");
    }
}

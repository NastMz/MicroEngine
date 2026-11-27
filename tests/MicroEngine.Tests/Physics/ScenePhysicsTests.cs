using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Physics;
using MicroEngine.Physics.Tiles;
using MicroEngine.Scenes;
using MicroEngine.World;

namespace MicroEngine.Tests.Physics;

/// <summary>A physics world that lives with its scene (<see cref="ScenePhysics"/>): made in one call, stepped, hashed and disposed by the scene.</summary>
[TestClass]
public sealed class ScenePhysicsTests
{
    /// <summary>A backend that is not the tile backend's type: it forwards to one and records what the scene does to it.</summary>
    private sealed class Wrapped(TilePhysicsWorld2D inner) : IPhysicsWorld2D, IStateHashed, IDisposable
    {
        public List<float> Steps { get; } = [];
        public bool Disposed { get; private set; }
        public string Backend => "wrapped";
        public PhysicsCapabilities Capabilities => inner.Capabilities;
        public float Skin => inner.Skin;
        public Vector2 Up => inner.Up;
        public long StepCount => inner.StepCount;
        public BodyId CreateBody(EntityId owner, in BodyDescription body) => inner.CreateBody(owner, body);
        public void DestroyBody(BodyId body) => inner.DestroyBody(body);
        public bool IsAlive(BodyId body) => inner.IsAlive(body);
        public ReadOnlySpan<BodyId> Bodies => inner.Bodies;
        public EntityId OwnerOf(BodyId body) => inner.OwnerOf(body);
        public BodyKind KindOf(BodyId body) => inner.KindOf(body);
        public Vector2 PositionOf(BodyId body) => inner.PositionOf(body);
        public float RotationOf(BodyId body) => inner.RotationOf(body);
        public void SetPosition(BodyId body, Vector2 position) => inner.SetPosition(body, position);
        public Vector2 VelocityOf(BodyId body) => inner.VelocityOf(body);
        public void SetVelocity(BodyId body, Vector2 velocity) => inner.SetVelocity(body, velocity);
        public ShapeId AddShape(BodyId body, in ShapeDescription shape) => inner.AddShape(body, shape);
        public void RemoveShape(ShapeId shape) => inner.RemoveShape(shape);
        public bool IsAlive(ShapeId shape) => inner.IsAlive(shape);
        public BodyId BodyOf(ShapeId shape) => inner.BodyOf(shape);
        public int ShapesOf(BodyId body, Span<ShapeId> results) => inner.ShapesOf(body, results);
        public ShapeDescription DescriptionOf(ShapeId shape) => inner.DescriptionOf(shape);
        public ShapeId AddTiles(BodyId body, ICollisionGrid grid, int columns, int rows, ulong category, ulong mask) => inner.AddTiles(body, grid, columns, rows, category, mask);
        public ICollisionGrid? TilesOf(ShapeId shape) => inner.TilesOf(shape);
        public MoveResult Move(BodyId body, Vector2 delta, in QueryFilter filter = default) => inner.Move(body, delta, filter);
        public bool RayCast(Vector2 from, Vector2 to, in QueryFilter filter, out RayHit hit) => inner.RayCast(from, to, filter, out hit);
        public int Overlap(in RectF area, in QueryFilter filter, Span<ShapeId> results) => inner.Overlap(area, filter, results);
        public void Step(float dt)
        {
            Steps.Add(dt);
            inner.Step(dt);
        }
        public ReadOnlySpan<ContactEvent> ContactEvents => inner.ContactEvents;
        public ReadOnlySpan<SensorEvent> SensorEvents => inner.SensorEvents;
        public void HashState(StateHasher hash) => inner.HashState(hash);
        public void Dispose() => Disposed = true;
    }

    private sealed class TestGame(Scene first, Func<IPhysicsWorld2D>? backend = null) : Game
    {
        public override GameSettings Settings { get; } = new("scene-physics");
        protected override void Load()
        {
            if (backend is not null) PhysicsBackend = backend;
            Scenes.Push(first);
        }
    }

    private sealed class Level(bool overlay = false) : Scene
    {
        public Action<Level>? OnEnter, OnUpdate;
        public long StepsSeenInUpdate = -1;
        public override bool IsOverlay => overlay;
        public override void Enter() => OnEnter?.Invoke(this);
        public override void Update(float dt)
        {
            if (Systems.Contains<ScenePhysics>()) StepsSeenInUpdate = Physics.World.StepCount;
            OnUpdate?.Invoke(this);
        }
        public override void Draw(IRenderer renderer) { }
    }

    private static TestGame Start(Scene first, Func<IPhysicsWorld2D>? backend = null)
    {
        var game = new TestGame(first, backend);
        game.InitializeForTest(new HeadlessPlatform());
        return game;
    }

    [TestMethod]
    public void A_scene_gets_a_tile_world_in_one_call_and_it_steps_after_the_scenes_update()
    {
        var scene = new Level { OnEnter = s => Assert.IsInstanceOfType<TilePhysicsWorld2D>(s.Physics.World) };
        var game = Start(scene);
        Assert.AreSame(scene.Physics, scene.Systems.Get<ScenePhysics>(), "one physics system per scene");
        game.StepForTest();
        Assert.AreEqual(0, scene.StepsSeenInUpdate, "the scene's update runs before the world steps");
        Assert.AreEqual(1, scene.Physics.World.StepCount);
        game.StepForTest();
        Assert.AreEqual(2, scene.Physics.World.StepCount, "once per fixed step");
    }

    [TestMethod]
    public void The_world_follows_the_scene_worlds_clock_and_stands_still_under_a_blocking_overlay()
    {
        var wrapped = new Wrapped(new TilePhysicsWorld2D());
        var scene = new Level { OnEnter = s => { _ = s.Objects; s.Systems.Add(new ScenePhysics(wrapped)); } };
        var game = Start(scene);
        game.StepForTest();
        game.StepForTest();
        Assert.AreEqual(2, wrapped.Steps.Count);
        Assert.AreEqual(1 / 60f, wrapped.Steps[1], 1e-6f);
        scene.Objects.Clock.Scale = 0.5f;
        game.StepForTest();
        Assert.AreEqual(0.5f / 60, wrapped.Steps[^1], 1e-6f, "slow motion slows physics");
        scene.Objects.Clock.Scale = 1;
        scene.Objects.Clock.Freeze(1);
        game.StepForTest();
        Assert.AreEqual(3, wrapped.Steps.Count, "a frozen world's physics stands still");
        scene.Objects.Clock.Unfreeze();
        game.Scenes.Push(new Level(overlay: true));
        game.StepForTest();
        game.StepForTest();
        Assert.AreEqual(3, wrapped.Steps.Count, "a covered scene's physics stands still");
        game.Scenes.Pop();
        game.StepForTest();
        game.StepForTest();
        Assert.AreEqual(5, wrapped.Steps.Count, "and goes on when the overlay leaves");
    }

    [TestMethod]
    public void A_character_placed_by_its_feet_gets_its_feet_in_its_transform()
    {
        TopDownController2D? actor = null;
        EntityId owner = default;
        var scene = new Level
        {
            OnEnter = s =>
            {
                var floor = s.Physics.World.CreateBody(default, BodyDescription.Static(new Vector2(50, 108)));
                s.Physics.World.AddShape(floor, ShapeDescription.Box(new Vector2(200, 16)));
                owner = s.Objects.Create();
                // A 12 x 14 box whose centre is 7 above the body's origin: the origin is the feet.
                actor = TopDownController2D.Create(s.Physics.World, owner, new Vector2(50, 60), new Vector2(12, 14), offset: new Vector2(0, -7));
            },
            OnUpdate = _ => actor!.Step(1 / 60f, new Vector2(0, 1)),
        };
        var game = Start(scene);
        for (int i = 0; i < 60; i++) game.StepForTest();
        Assert.AreEqual(new Vector2(50, 100), actor!.Position, "the feet stand on the floor's top");
        Assert.AreEqual(new Vector2(50, 100), scene.Objects.Local(owner).Position2D, "and the transform is where the feet are");
    }

    [TestMethod]
    public void Body_poses_are_written_to_their_owners_transforms()
    {
        BodyId body = default;
        EntityId owner = default;
        var scene = new Level
        {
            OnEnter = s =>
            {
                owner = s.Objects.Create();
                body = s.Physics.World.CreateBody(owner, BodyDescription.Kinematic(new Vector2(5, 6)));
                s.Physics.World.AddShape(body, ShapeDescription.Box(new Vector2(4, 4)));
            },
            OnUpdate = s => s.Physics.World.Move(body, new Vector2(2, 0)),
        };
        var game = Start(scene);
        game.StepForTest();
        Assert.AreEqual(new Vector2(7, 6), scene.Objects.Local(owner).Position2D);
        scene.Physics.SyncTransforms = false;
        game.StepForTest();
        Assert.AreEqual(new Vector2(7, 6), scene.Objects.Local(owner).Position2D, "a scene that places its own objects turns the sync off");
    }

    [TestMethod]
    public void The_world_and_tracked_controllers_are_part_of_the_scenes_state_hash()
    {
        CharacterController2D? hero = null;
        var scene = new Level
        {
            OnEnter = s =>
            {
                var floor = s.Physics.World.CreateBody(default, BodyDescription.Static(new Vector2(0, 108)));
                s.Physics.World.AddShape(floor, ShapeDescription.Box(new Vector2(600, 16)));
                hero = s.Physics.Track(CharacterController2D.Create(s.Physics.World, default, new Vector2(0, 50), new Vector2(12, 14)));
            },
        };
        var game = Start(scene);
        game.StepForTest();
        ulong before = game.ComputeStateHash().Hash;
        scene.Physics.World.SetPosition(hero!.Body, new Vector2(3, 50));
        ulong moved = game.ComputeStateHash().Hash;
        Assert.AreNotEqual(before, moved, "a body's position is in the hash");
        hero.Velocity = new Vector2(10, 0);
        Assert.AreNotEqual(moved, game.ComputeStateHash().Hash, "a tracked controller's state is in the hash");
        ulong tracked = game.ComputeStateHash().Hash;
        Assert.IsTrue(scene.Physics.Untrack(hero));
        Assert.AreNotEqual(tracked, game.ComputeStateHash().Hash);
    }

    [TestMethod]
    public void The_backend_is_chosen_in_one_place_and_the_world_is_disposed_with_its_scene()
    {
        var made = new List<Wrapped>();
        var first = new Level { OnEnter = s => _ = s.Physics };
        var game = Start(first, () =>
        {
            made.Add(new Wrapped(new TilePhysicsWorld2D()));
            return made[^1];
        });
        Assert.AreEqual(1, made.Count);
        Assert.AreSame(made[0], first.Physics.World, "every scene's physics comes from Game.PhysicsBackend");
        game.StepForTest();
        Assert.IsFalse(made[0].Disposed);
        game.Scenes.Pop();
        game.StepForTest();
        Assert.IsTrue(made[0].Disposed, "the scene's world ends with the scene");
    }

    [TestMethod]
    public void Debug_draw_reads_tile_grids_through_the_contract()
    {
        var wrapped = new Wrapped(new TilePhysicsWorld2D());
        var grid = new TileGrid(TileLayer.Parse(["#.", "##"], new Dictionary<char, int> { ['#'] = 1 }), 16, new Dictionary<int, TileFlags> { [1] = TileFlags.Solid })
        {
            OutsideFlags = TileFlags.None,
        };
        var level = wrapped.CreateBody(default, BodyDescription.Static());
        var tiles = wrapped.AddTiles(level, grid, 2, 2, CollisionLayers.Default, CollisionLayers.All);
        Assert.AreSame(grid, wrapped.TilesOf(tiles));
        var game = Start(new Level());
        game.DebugDraw.Enabled = true;
        game.DebugDraw.Physics(wrapped, new RectF(0, 0, 32, 32));
        Assert.AreEqual(3, game.DebugDraw.Count, "three solid tiles drawn for a world that is not the tile backend's class");
    }

    [TestMethod]
    public void Tile_geometry_is_a_contract_member_refused_by_backends_that_do_not_declare_it()
    {
        IPhysicsWorld2D tiles = new TilePhysicsWorld2D();
        var grid = new TileGrid(new TileLayer(2, 2), 16, new Dictionary<int, TileFlags>());
        var body = tiles.CreateBody(default, BodyDescription.Static());
        var shape = tiles.AddTiles(body, grid, 2, 2);
        Assert.AreSame(grid, tiles.TilesOf(shape));
        Assert.IsNull(tiles.TilesOf(tiles.AddShape(body, ShapeDescription.Box(new Vector2(4, 4)))), "a box is not a tile grid");
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => tiles.AddTiles(body, grid, 0, 2));
    }
}

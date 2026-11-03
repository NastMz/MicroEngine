using System.Numerics;
using Arch1.Scene;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace Arch1.Scene.Tests;

/// <summary>The rules the P7 world proposes for the stage 3 IWorld, checked on every attachment storage design.</summary>
[TestClass]
public sealed class WorldRulesTests
{
    private sealed class Tag(string name) { public string Name { get; } = name; public override string ToString() => Name; }
    private class Base { }
    private sealed class Derived : Base { }

    private sealed class Notified(List<string> log, string name) : IDestroyedCallback
    {
        public World? World { get; set; }
        public void OnDestroyed(EntityId entity) =>
            log.Add($"{name}:{World!.Local(entity).Position2D.X}");
    }

    private struct Velocity { public Vector2 Value; }

    public static IEnumerable<object[]> Storages =>
        [[AttachmentStorage.DenseStable], [AttachmentStorage.DenseSwapRemove], [AttachmentStorage.PerEntityDictionary]];

    public static IEnumerable<object[]> OrderKeepingStorages =>
        [[AttachmentStorage.DenseStable], [AttachmentStorage.PerEntityDictionary]];

    private static List<string> Names(World world)
    {
        var names = new List<string>();
        foreach (var tag in world.All<Tag>()) names.Add(tag.Name);
        return names;
    }

    private static EntityId Tagged(World world, string name)
    {
        var id = world.Create();
        world.Attach(id, new Tag(name));
        return id;
    }

    [TestMethod]
    [DynamicData(nameof(Storages))]
    public void ANewEntityIsLiveAtIdentity(AttachmentStorage storage)
    {
        var world = new World(storage);
        var a = world.Create();
        var b = world.Create();

        Assert.IsFalse(a.IsNone);
        Assert.AreNotEqual(a, b);
        Assert.IsTrue(world.IsAlive(a));
        Assert.AreEqual(2, world.Count);
        Assert.AreEqual(Vector3.Zero, world.Local(a).Position);
        Assert.AreEqual(Quaternion.Identity, world.Local(a).Rotation);
        Assert.AreEqual(Vector3.One, world.Local(a).Scale);
        Assert.AreEqual(EntityId.None, world.ParentOf(a));
        Assert.IsFalse(world.IsAlive(EntityId.None));
    }

    [TestMethod]
    [DynamicData(nameof(Storages))]
    public void DestroyIsImmediateForQueriesAndDataStaysReadableUntilTheStepEnds(AttachmentStorage storage)
    {
        var world = new World(storage);
        world.BeginStep(1);
        var a = Tagged(world, "a");
        world.Local(a).Position2D = new Vector2(7, 3);

        world.Destroy(a);

        Assert.IsFalse(world.IsAlive(a), "Dead at once.");
        Assert.AreEqual(0, world.Count);
        CollectionAssert.AreEqual(Array.Empty<string>(), Names(world), "Iterations skip it at once.");
        Assert.AreEqual(new Vector2(7, 3), world.Local(a).Position2D, "Its transform is readable until the step ends.");
        Assert.IsTrue(world.TryGet<Tag>(a, out var tag) && tag.Name == "a", "Its attachments are readable until the step ends.");

        world.EndStep();

        Assert.IsFalse(world.TryGet<Tag>(a, out _), "Released: the id is stale.");
        Assert.ThrowsExactly<InvalidOperationException>(() => world.Local(a));
        Assert.ThrowsExactly<InvalidOperationException>(() => world.Get<Tag>(a));
    }

    [TestMethod]
    [DynamicData(nameof(Storages))]
    public void AReusedSlotGetsANewGenerationAndTheOldIdReachesNothing(AttachmentStorage storage)
    {
        var world = new World(storage);
        var a = Tagged(world, "a");
        world.Destroy(a);
        world.EndStep();

        var b = Tagged(world, "b");

        Assert.AreEqual(a.Index, b.Index, "The slot is reused.");
        Assert.AreNotEqual(a.Generation, b.Generation);
        Assert.IsFalse(world.IsAlive(a));
        Assert.IsFalse(world.TryGet<Tag>(a, out _), "The old id does not reach the new entity's attachment.");
        Assert.AreEqual("b", world.Get<Tag>(b).Name);
    }

    [TestMethod]
    [DynamicData(nameof(Storages))]
    public void DestroyingADestroyedOrStaleIdDoesNothing(AttachmentStorage storage)
    {
        var world = new World(storage);
        var a = world.Create();
        var b = world.Create();
        world.Destroy(a);
        world.Destroy(a);
        world.EndStep();
        world.Destroy(a);
        world.Destroy(EntityId.None);

        Assert.IsTrue(world.IsAlive(b));
        Assert.AreEqual(1, world.Count);
    }

    [TestMethod]
    [DynamicData(nameof(OrderKeepingStorages))]
    public void IterationFollowsAttachOrderAcrossDestructionAndSlotReuse(AttachmentStorage storage)
    {
        var world = new World(storage);
        var ids = new[] { "a", "b", "c", "d", "e" }.Select(n => Tagged(world, n)).ToArray();
        world.Destroy(ids[1]);
        world.Destroy(ids[3]);
        world.EndStep();
        Tagged(world, "f");
        Tagged(world, "g");

        CollectionAssert.AreEqual(new[] { "a", "c", "e", "f", "g" }, Names(world));
    }

    [TestMethod]
    public void SwapRemoveStorageLosesAttachOrder()
    {
        var world = new World(AttachmentStorage.DenseSwapRemove);
        var ids = new[] { "a", "b", "c", "d", "e" }.Select(n => Tagged(world, n)).ToArray();
        world.Destroy(ids[1]);
        world.EndStep();

        CollectionAssert.AreEqual(new[] { "a", "e", "c", "d" }, Names(world), "The last element fills the hole.");
    }

    [TestMethod]
    [DynamicData(nameof(Storages))]
    public void AnIterationSkipsWhatIsDestroyedDuringItAndVisitsWhatIsCreatedOnlyNextTime(AttachmentStorage storage)
    {
        var world = new World(storage);
        var a = Tagged(world, "a");
        var b = Tagged(world, "b");
        var c = Tagged(world, "c");
        var seen = new List<string>();

        foreach (var tag in world.All<Tag>())
        {
            seen.Add(tag.Name);
            if (tag.Name == "a")
            {
                world.Destroy(c);
                Tagged(world, "new");
            }
        }

        CollectionAssert.AreEqual(new[] { "a", "b" }, seen);
        CollectionAssert.AreEqual(new[] { "a", "b", "new" }, Names(world));
        _ = a; _ = b;
    }

    [TestMethod]
    [DynamicData(nameof(Storages))]
    public void AttachmentsAreKeyedByTheTypeArgumentNotTheRuntimeType(AttachmentStorage storage)
    {
        var world = new World(storage);
        var e = world.Create();
        var attached = world.Attach<Base>(e, new Derived());

        Assert.IsTrue(world.TryGet<Base>(e, out var asBase));
        Assert.AreSame(attached, asBase);
        Assert.IsFalse(world.TryGet<Derived>(e, out _), "No polymorphic lookup.");
        Assert.AreEqual(1, world.All<Base>().Count());
    }

    [TestMethod]
    [DynamicData(nameof(Storages))]
    public void AttachReplacesAndDetachRemoves(AttachmentStorage storage)
    {
        var world = new World(storage);
        var e = world.Create();
        world.Attach(e, new Tag("first"));
        world.Attach(e, new Tag("second"));

        Assert.AreEqual("second", world.Get<Tag>(e).Name);
        CollectionAssert.AreEqual(new[] { "second" }, Names(world));

        Assert.IsTrue(world.Detach<Tag>(e));
        Assert.IsFalse(world.Detach<Tag>(e));
        Assert.IsFalse(world.TryGet<Tag>(e, out _));
        CollectionAssert.AreEqual(Array.Empty<string>(), Names(world));
        Assert.IsTrue(world.IsAlive(e), "Detaching leaves the entity.");
    }

    [TestMethod]
    [DynamicData(nameof(Storages))]
    public void DestroyedCallbacksRunWhenTheStepEndsInDestroyOrderWithDataReadable(AttachmentStorage storage)
    {
        var world = new World(storage);
        var log = new List<string>();
        var ids = new EntityId[3];
        for (int i = 0; i < 3; i++)
        {
            ids[i] = world.Create();
            world.Local(ids[i]).Position2D = new Vector2(i, 0);
            world.Attach(ids[i], new Notified(log, $"n{i}") { World = world });
        }

        world.Destroy(ids[2]);
        world.Destroy(ids[0]);
        Assert.AreEqual(0, log.Count, "Nothing is told before the step ends.");
        world.EndStep();

        CollectionAssert.AreEqual(new[] { "n2:2", "n0:0" }, log);
    }

    [TestMethod]
    [DynamicData(nameof(Storages))]
    public void DestroyingAParentDestroysItsSubtree(AttachmentStorage storage)
    {
        var world = new World(storage);
        var root = world.Create();
        var child = world.Create(root);
        var grandchild = world.Create(child);
        var other = world.Create();

        world.Destroy(root);

        Assert.IsFalse(world.IsAlive(child));
        Assert.IsFalse(world.IsAlive(grandchild));
        Assert.IsTrue(world.IsAlive(other));
        Assert.AreEqual(1, world.Count);
    }

    [TestMethod]
    public void AChildIsPlacedByItsParent()
    {
        var world = new World();
        var parent = world.Create();
        var child = world.Create(parent);
        world.Local(parent).Position2D = new Vector2(100, 50);
        world.Local(parent).Rotation2D = MathF.PI / 2;
        world.Local(child).Position2D = new Vector2(10, 0);

        var p = world.DrawPosition2D(child, 1);

        Assert.AreEqual(100, p.X, 1e-4);
        Assert.AreEqual(60, p.Y, 1e-4, "Rotated a quarter turn by the parent.");
        Assert.AreEqual(parent, world.ParentOf(child));

        world.SetParent(child, EntityId.None);
        Assert.AreEqual(new Vector2(10, 0), world.DrawPosition2D(child, 1), "A root keeps its local transform.");
    }

    [TestMethod]
    public void SetParentRefusesACycle()
    {
        var world = new World();
        var a = world.Create();
        var b = world.Create(a);

        Assert.ThrowsExactly<InvalidOperationException>(() => world.SetParent(a, b));
        Assert.ThrowsExactly<InvalidOperationException>(() => world.SetParent(a, a));
    }

    [TestMethod]
    [DynamicData(nameof(Storages))]
    public void DrawingBlendsFromTheStepStartAndIsExactAtOne(AttachmentStorage storage)
    {
        var world = new World(storage);
        var e = world.Create();
        world.Local(e).Position2D = new Vector2(10, 10);
        world.BeginStep(1);
        world.Local(e).Position2D = new Vector2(10.3f, 20);

        Assert.AreEqual(Vector2.Lerp(new(10, 10), new(10.3f, 20), 0.5f), world.DrawPosition2D(e, 0.5f));
        Assert.AreEqual(new Vector2(10, 10), world.DrawPosition2D(e, 0));
        Assert.AreEqual(new Vector2(10.3f, 20), world.DrawPosition2D(e, 1), "Exactly the simulated position at 1.");
        Assert.AreEqual(world.Local(e).ToMatrix(), world.WorldMatrix(e, 1));
    }

    [TestMethod]
    public void ANewOrTeleportedEntityIsDrawnWhereItIsUntilTheNextStep()
    {
        var world = new World();
        var walker = world.Create();
        world.BeginStep(1);
        var spawned = world.Create();
        world.Local(spawned).Position2D = new Vector2(50, 0);
        world.Local(walker).Position2D = new Vector2(500, 0);
        world.Teleport(walker);
        world.Local(walker).Position2D = new Vector2(501, 0);

        Assert.AreEqual(new Vector2(50, 0), world.DrawPosition2D(spawned, 0), "New: no previous position.");
        Assert.AreEqual(new Vector2(501, 0), world.DrawPosition2D(walker, 0), "Teleported: drawn where it is, even after later moves this step.");

        world.BeginStep(2);
        world.Local(walker).Position2D = new Vector2(503, 0);
        Assert.AreEqual(new Vector2(502, 0), world.DrawPosition2D(walker, 0.5f), "Blends again from the next step.");
    }

    [TestMethod]
    public void TeleportingAParentSnapsItsChildren()
    {
        var world = new World();
        var parent = world.Create();
        var child = world.Create(parent);
        world.Local(child).Position2D = new Vector2(1, 0);
        world.BeginStep(1);
        world.Local(parent).Position2D = new Vector2(1000, 0);
        world.Teleport(parent);

        Assert.AreEqual(new Vector2(1001, 0), world.DrawPosition2D(child, 0.25f));
    }

    [TestMethod]
    public void AWorldThatDidNotStepInTheLatestEngineStepIsDrawnAtBlendOne()
    {
        var world = new World();
        Assert.AreEqual(1f, world.Blend, "Before any frame.");
        world.BeginStep(5);
        world.BeginDraw(latestEngineStep: 5, engineBlend: 0.3f);
        Assert.AreEqual(0.3f, world.Blend, "It stepped: the engine's blend.");

        // An overlay froze the scene: the engine stepped (6) but this world did not.
        world.BeginDraw(latestEngineStep: 6, engineBlend: 0.3f);
        Assert.AreEqual(1f, world.Blend, "Frozen: drawn as its last step, not rocking between its last two.");
        Assert.AreEqual(5, world.SteppedAt);
    }

    [TestMethod]
    public void DrawingWithoutABlendArgumentUsesTheWorldsBlend()
    {
        var world = new World();
        var e = world.Create();
        world.BeginStep(1);
        world.Local(e).Position2D = new Vector2(10, 0);
        world.BeginDraw(1, 0.5f);
        Assert.AreEqual(new Vector2(5, 0), world.DrawPosition2D(e));
        world.BeginDraw(2, 0.5f);
        Assert.AreEqual(new Vector2(10, 0), world.DrawPosition2D(e));
    }

    [TestMethod]
    public void TheClockScalesFreezesAndIsExactAtScaleOne()
    {
        var clock = new WorldClock();
        float dt = 1 / 60f;
        Assert.AreEqual(dt, clock.Advance(dt), "Bit for bit at scale 1.");
        clock.Scale = 0.5f;
        Assert.AreEqual(dt * 0.5f, clock.Advance(dt));
        clock.Scale = 1;

        clock.Freeze(0.05f);
        clock.Freeze(0.02f);
        Assert.AreEqual(0.05f, clock.FreezeRemaining, "The longer freeze wins.");
        int frozen = 0;
        while (clock.Advance(dt) == 0) frozen++;
        Assert.AreEqual(3, frozen, "0.05 s at 60 Hz freezes three whole steps.");
        Assert.IsFalse(clock.Frozen);
        Assert.AreEqual(dt + dt * 0.5f + dt, clock.Time, 1e-6);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => clock.Scale = -1);
    }

    [TestMethod]
    [DynamicData(nameof(Storages))]
    public void StructAttachmentsAreWrittenThroughTheirReference(AttachmentStorage storage)
    {
        var world = new World(storage);
        var e = world.Create();
        world.AttachValue(e, new Velocity { Value = new Vector2(1, 2) });

        world.Value<Velocity>(e).Value.X = 9;

        Assert.AreEqual(new Vector2(9, 2), world.Value<Velocity>(e).Value);
    }

    [TestMethod]
    public void TwoDHelpersArePreciseWherePromised()
    {
        var t = Transform.Identity;
        var p = new Vector2(123.456f, -0.1f);
        t.Position2D = p;
        Assert.AreEqual(p, t.Position2D, "Positions read back bit for bit.");
        t.Rotation2D = 0.3f;
        Assert.AreEqual(0.3f, t.Rotation2D, 1e-6, "Angles read back within a few ulps.");
        var to = t;
        to.Position2D = new Vector2(1, 1);
        Assert.AreEqual(to.Position, Transform.Interpolate(t, to, 1).Position);
    }
}

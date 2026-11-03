using MicroEngine.Kernel;
using MicroEngine.Scenes;

namespace MicroEngine.Physics;

/// <summary>
/// The physics module: registers a <see cref="PhysicsSystem"/> service and steps it in <see cref="FramePhase.Physics"/>
/// (system <see cref="SystemName"/>), once per fixed step, after the simulation's <see cref="FramePhase.FixedUpdate"/>.
/// </summary>
/// <example>
/// <code>
/// public override void Configure(EngineBuilder builder) => builder.Add(new PhysicsModule());
/// // later, in a scene: Game.Engine.Services.Get&lt;PhysicsSystem&gt;().Add(physics, world);
/// </code>
/// </example>
public sealed class PhysicsModule : IEngineModule
{
    /// <summary>The name of the frame system that steps physics.</summary>
    public const string SystemName = "engine.physics";

    /// <inheritdoc/>
    public void Configure(EngineBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var system = new PhysicsSystem();
        builder.Services.Add(system);
        builder.Schedule.Add(FramePhase.Physics, SystemName, system.Run);
    }
}

/// <summary>
/// Steps physics worlds, each optionally bound to the scene world whose entities own its bodies. A bound physics world
/// steps only in the engine steps its scene world stepped in, by that world's scaled step (<see cref="WorldClock.Delta"/>),
/// so a paused, frozen or covered scene's physics stands still too; after the step, the pose of every kinematic and
/// dynamic body is written to its owner's transform (<see cref="PhysicsSync.WriteTransforms"/>), which the scene world
/// interpolates for drawing. An unbound world steps by the engine's fixed step.
/// </summary>
public sealed class PhysicsSystem
{
    private sealed class Entry(IPhysicsWorld2D physics, SceneWorld? world)
    {
        public IPhysicsWorld2D Physics { get; } = physics;
        public SceneWorld? World { get; } = world;
        public long LastWorldStep { get; set; } = world?.SteppedAt ?? -1;
    }

    private readonly List<Entry> _entries = [];

    /// <summary>How many physics worlds are registered.</summary>
    public int Count => _entries.Count;

    /// <summary>Registers <paramref name="physics"/>, bound to <paramref name="world"/> when given.</summary>
    /// <exception cref="InvalidOperationException">It is already registered.</exception>
    public void Add(IPhysicsWorld2D physics, SceneWorld? world = null)
    {
        ArgumentNullException.ThrowIfNull(physics);
        if (_entries.Exists(e => ReferenceEquals(e.Physics, physics)))
            throw new InvalidOperationException("This physics world is already registered.");
        _entries.Add(new Entry(physics, world));
    }

    /// <summary>Stops stepping <paramref name="physics"/>; false when it was not registered.</summary>
    public bool Remove(IPhysicsWorld2D physics) => _entries.RemoveAll(e => ReferenceEquals(e.Physics, physics)) > 0;

    /// <summary>Steps every registered world for one fixed step of <paramref name="frame"/> (what <see cref="PhysicsModule"/> schedules).</summary>
    public void Run(FrameContext frame)
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            var e = _entries[i];
            if (e.World is null)
            {
                e.Physics.Step((float)frame.FixedDelta);
                continue;
            }
            if (e.World.SteppedAt == e.LastWorldStep) continue;
            e.LastWorldStep = e.World.SteppedAt;
            float dt = e.World.Clock.Delta;
            if (dt > 0) e.Physics.Step(dt);
            PhysicsSync.WriteTransforms(e.Physics, e.World);
        }
    }
}

/// <summary>Copies body poses to the transforms of the entities that own them.</summary>
public static class PhysicsSync
{
    /// <summary>
    /// Writes the origin (and, on backends with <see cref="PhysicsCapabilities.Rotation"/>, the angle) of every
    /// kinematic and dynamic body of <paramref name="physics"/> to its owner's transform in <paramref name="world"/>
    /// (<see cref="Transform.Position2D"/>, <see cref="Transform.Rotation2D"/>). Bodies whose owner is not alive are
    /// skipped; static bodies are never written.
    /// </summary>
    /// <exception cref="InvalidOperationException">An owner has a parent: body poses are world positions, so owners must be root entities.</exception>
    public static void WriteTransforms(IPhysicsWorld2D physics, IWorld world)
    {
        ArgumentNullException.ThrowIfNull(physics);
        ArgumentNullException.ThrowIfNull(world);
        bool rotation = (physics.Capabilities & PhysicsCapabilities.Rotation) != 0;
        foreach (var body in physics.Bodies)
        {
            if (physics.KindOf(body) == BodyKind.Static) continue;
            var owner = physics.OwnerOf(body);
            if (!world.IsAlive(owner)) continue;
            if (!world.ParentOf(owner).IsNone)
                throw new InvalidOperationException($"{body} is owned by {owner}, which has a parent; physics writes world positions, so the owners of moving bodies must be root entities.");
            ref var t = ref world.Local(owner);
            t.Position2D = physics.PositionOf(body);
            if (rotation) t.Rotation2D = physics.RotationOf(body);
        }
    }

    /// <summary>
    /// Teleports <paramref name="body"/> to <paramref name="position"/> and its owner with it: the body is placed without
    /// collision, the owner's transform is written and it is drawn there at once (<see cref="IWorld.Teleport"/>).
    /// </summary>
    public static void Teleport(IPhysicsWorld2D physics, IWorld world, BodyId body, System.Numerics.Vector2 position)
    {
        ArgumentNullException.ThrowIfNull(physics);
        ArgumentNullException.ThrowIfNull(world);
        physics.SetPosition(body, position);
        var owner = physics.OwnerOf(body);
        if (!world.IsAlive(owner)) return;
        world.Local(owner).Position2D = position;
        world.Teleport(owner);
    }
}

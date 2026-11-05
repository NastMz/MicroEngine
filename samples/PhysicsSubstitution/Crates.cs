using System.Numerics;
using MicroEngine.Physics;
using MicroEngine.Scenes;

namespace PhysicsSubstitution;

/// <summary>
/// The second scenario: a stack of crates knocked over by a falling one, which needs dynamic bodies
/// (<see cref="PhysicsCapabilities.DynamicBodies"/>) and rotation. A backend that does not declare them refuses the first
/// crate with <see cref="PhysicsCapabilityException"/> before anything changes.
/// </summary>
public static class Crates
{
    public static readonly EntityId Owner = new(10, 1);

    /// <summary>The crates' bodies after <paramref name="seconds"/> of simulation, in pixels and radians.</summary>
    public static List<(Vector2 Position, float Rotation)> Run(IPhysicsWorld2D world, float seconds = 4)
    {
        var floor = world.CreateBody(Platformer.LevelOwner, BodyDescription.Static(new Vector2(320, 136)));
        world.AddShape(floor, ShapeDescription.Box(new Vector2(640, 16)));
        var crates = new List<BodyId>();
        for (int i = 0; i < 4; i++) crates.Add(Crate(world, new Vector2(320, 120 - i * 16), 0));
        crates.Add(Crate(world, new Vector2(326, -60), 0.4f)); // dropped, turned, onto the stack's corner
        for (int i = 0, n = (int)(seconds * 60); i < n; i++) world.Step(1 / 60f);
        return crates.Select(c => (world.PositionOf(c), world.RotationOf(c))).ToList();
    }

    private static BodyId Crate(IPhysicsWorld2D world, Vector2 position, float rotation)
    {
        var body = world.CreateBody(Owner, BodyDescription.Dynamic(position) with { Rotation = rotation });
        world.AddShape(body, ShapeDescription.Box(new Vector2(16, 16)));
        return body;
    }
}

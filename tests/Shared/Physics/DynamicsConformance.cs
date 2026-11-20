using System.Numerics;
using MicroEngine.Kernel;
using MicroEngine.Physics;

namespace MicroEngine.Testing.Physics;

/// <summary>
/// Controlling dynamic bodies: forces, impulses and torque (at the centre and at a point), angular velocity, damping,
/// gravity scale, fixed rotation, setting the rotation, sleep, and reading mass data. For backends that declare
/// <see cref="PhysicsCapabilities.DynamicBodies"/> and <see cref="PhysicsCapabilities.Rotation"/>.
/// </summary>
public abstract class DynamicsConformance : SolverConformance
{
    protected override PhysicsCapabilities Capability => PhysicsCapabilities.DynamicBodies | PhysicsCapabilities.Rotation;

    /// <summary>A 16 × 16 dynamic box in the air that gravity does not pull, unless <paramref name="edit"/> says otherwise.</summary>
    private (IPhysicsWorld2D World, BodyId Body) Floating(Func<BodyDynamics, BodyDynamics>? edit = null)
    {
        var world = CreateWorld();
        var dynamics = new BodyDynamics { GravityScale = 0 };
        var (body, _) = Box(world, BodyDescription.Dynamic(default) with { Dynamics = edit is null ? dynamics : edit(dynamics) }, -8, 8, 100, 116, owner: Owner.A);
        return (world, body);
    }

    private static void Step(IPhysicsWorld2D world, float seconds, Action? each = null)
    {
        for (int i = 0, n = (int)MathF.Round(seconds * 60); i < n; i++)
        {
            each?.Invoke();
            world.Step(1 / 60f);
        }
    }

    [TestMethod]
    public void A_body_starts_with_default_dynamics_and_takes_the_ones_it_is_described_with()
    {
        var world = CreateWorld();
        var (plain, _) = Box(world, BodyDescription.Dynamic(default), -8, 8, 0, 16);
        var d = world.DynamicsOf(plain);
        Assert.AreEqual(new BodyDynamics(), d);
        Assert.AreEqual(default, d);
        Assert.IsTrue(d is { LinearDamping: 0, AngularDamping: 0, GravityScale: 1, FixedRotation: false, AllowSleep: true });
        var tuned = new BodyDynamics { LinearDamping = 0.5f, AngularDamping = 0.25f, GravityScale = 2, FixedRotation = true, AllowSleep = false };
        var (body, _) = Box(world, BodyDescription.Dynamic(default) with { Dynamics = tuned }, 40, 56, 0, 16);
        Assert.AreEqual(tuned, world.DynamicsOf(body));
        world.SetDynamics(body, tuned with { GravityScale = 0.5f });
        Assert.AreEqual(0.5f, world.DynamicsOf(body).GravityScale);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => world.SetDynamics(body, new BodyDynamics { LinearDamping = -1 }));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => world.SetDynamics(body, new BodyDynamics { GravityScale = float.NaN }));
        Assert.ThrowsExactly<InvalidOperationException>(() => world.DynamicsOf(BodyId.None));
    }

    [TestMethod]
    public void An_impulse_at_the_centre_changes_the_velocity_by_the_impulse_over_the_mass()
    {
        var (world, body) = Floating();
        float mass = world.MassOf(body);
        Assert.IsTrue(mass > 0, $"mass {mass}");
        world.ApplyImpulse(body, Speed(world, 30, 60) * mass);
        Assert.AreEqual(30, SpeedX(world, body), 0.01f);
        Assert.AreEqual(60, SpeedUp(world, body), 0.01f);
        Assert.AreEqual(0, world.AngularVelocityOf(body), 1e-4f, "through the centre: no spin");
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => world.ApplyImpulse(body, new Vector2(float.NaN, 0)));
    }

    [TestMethod]
    public void A_force_accelerates_a_body_by_the_force_over_the_mass()
    {
        var (world, body) = Floating();
        float mass = world.MassOf(body);
        Step(world, 1, () => world.ApplyForce(body, Speed(world, 50, 0) * mass));
        Assert.AreEqual(50, SpeedX(world, body), 1, "one second of 50 units per second squared");
        Step(world, 0.5f);
        Assert.AreEqual(50, SpeedX(world, body), 1, "a force lasts one step: it is applied again each step");
    }

    [TestMethod]
    public void An_impulse_or_a_force_off_centre_also_spins_the_body()
    {
        var (world, body) = Floating();
        float mass = world.MassOf(body), inertia = world.InertiaOf(body);
        var centre = world.CenterOfMassOf(body);
        Assert.IsTrue(Vector2.Distance(centre, world.PositionOf(body)) < 1e-3f * Scale, "the box is centred on its body");
        var impulse = Speed(world, 0, 10) * mass;
        world.ApplyImpulse(body, impulse, centre + By(world, 8, 0));
        Assert.AreEqual(10, SpeedUp(world, body), 0.01f, "the linear part is the same wherever it is applied");
        float expected = 8 * Scale * impulse.Length() / inertia;
        Assert.AreEqual(expected, MathF.Abs(world.AngularVelocityOf(body)), expected * 0.02f, "angular velocity = arm × impulse / inertia");

        var (world2, body2) = Floating();
        Step(world2, 0.5f, () => world2.ApplyForce(body2, Speed(world2, 0, 20) * world2.MassOf(body2), world2.CenterOfMassOf(body2) + By(world2, 8, 0)));
        Assert.IsTrue(MathF.Abs(world2.AngularVelocityOf(body2)) > 0.5f, $"a force off centre spins: {world2.AngularVelocityOf(body2)}");
        Assert.IsTrue(SpeedUp(world2, body2) > 5);
    }

    [TestMethod]
    public void Torque_and_angular_impulse_spin_a_body_in_the_direction_its_rotation_grows()
    {
        var (world, body) = Floating();
        float inertia = world.InertiaOf(body);
        Assert.IsTrue(inertia > 0);
        world.ApplyAngularImpulse(body, 2 * inertia);
        Assert.AreEqual(2, world.AngularVelocityOf(body), 0.01f, "angular impulse over inertia");
        world.SetAngularVelocity(body, 0);
        Step(world, 1, () => world.ApplyTorque(body, 3 * inertia));
        Assert.AreEqual(3, world.AngularVelocityOf(body), 0.1f, "one second of 3 radians per second squared");
        Assert.IsTrue(world.RotationOf(body) > 1, $"the angle grew with the torque: {world.RotationOf(body)}");
    }

    [TestMethod]
    public void Angular_velocity_is_read_and_set_and_turns_the_body()
    {
        var (world, body) = Floating();
        world.SetAngularVelocity(body, 1.5f);
        Assert.AreEqual(1.5f, world.AngularVelocityOf(body), 1e-5f);
        Step(world, 1);
        Assert.AreEqual(1.5f, world.RotationOf(body), 0.02f);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => world.SetAngularVelocity(body, float.PositiveInfinity));
    }

    [TestMethod]
    public void Damping_slows_a_body_down()
    {
        var (world, body) = Floating(d => d with { LinearDamping = 2, AngularDamping = 2 });
        world.SetVelocity(body, Speed(world, 100, 0));
        world.SetAngularVelocity(body, 4);
        Step(world, 1);
        Assert.IsTrue(SpeedX(world, body) is > 5 and < 40, $"about e⁻² of 100: {SpeedX(world, body)}");
        Assert.IsTrue(world.AngularVelocityOf(body) is > 0.2f and < 1.6f, $"about e⁻² of 4: {world.AngularVelocityOf(body)}");
        var (free, drifting) = Floating();
        free.SetVelocity(drifting, Speed(free, 100, 0));
        Step(free, 1);
        Assert.AreEqual(100, SpeedX(free, drifting), 0.01f, "no damping: it keeps its speed");
    }

    [TestMethod]
    public void Gravity_scale_multiplies_gravity_for_one_body()
    {
        var (world, floating) = Floating();
        var (normal, _) = Box(world, BodyDescription.Dynamic(default), 40, 56, 100, 116);
        var (heavy, _) = Box(world, BodyDescription.Dynamic(default) with { Dynamics = new BodyDynamics { GravityScale = 2 } }, 80, 96, 100, 116);
        Step(world, 0.5f);
        Assert.AreEqual(0, SpeedUp(world, floating), 1e-4f, "scale 0 floats");
        Assert.IsTrue(SpeedUp(world, normal) < 0, "gravity pulls");
        Assert.AreEqual(2 * SpeedUp(world, normal), SpeedUp(world, heavy), MathF.Abs(SpeedUp(world, normal)) * 0.01f, "scale 2 falls twice as fast");
    }

    [TestMethod]
    public void A_body_with_fixed_rotation_never_turns()
    {
        var (world, body) = Floating(d => d with { FixedRotation = true });
        world.ApplyImpulse(body, Speed(world, 0, 10) * world.MassOf(body), world.CenterOfMassOf(body) + By(world, 8, 0));
        Step(world, 0.5f);
        Assert.AreEqual(0, world.AngularVelocityOf(body), 1e-5f);
        Assert.AreEqual(0, world.RotationOf(body), 1e-4f);
        Assert.IsTrue(SpeedUp(world, body) > 9, "it still moves");
    }

    [TestMethod]
    public void Setting_the_rotation_turns_a_body_where_it_is()
    {
        var (world, body) = Floating();
        var at = world.PositionOf(body);
        world.SetRotation(body, 0.75f);
        Assert.AreEqual(0.75f, world.RotationOf(body), 0.005f);
        Assert.AreEqual(at, world.PositionOf(body));
        var (still, _) = Box(world, BodyDescription.Static(), 200, 216, 0, 16);
        world.SetRotation(still, -0.5f);
        Assert.AreEqual(-0.5f, world.RotationOf(still), 0.005f, "static and kinematic bodies turn too");
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => world.SetRotation(body, float.NaN));
    }

    [TestMethod]
    public void A_resting_body_falls_asleep_unless_told_not_to_and_wakes_when_pushed()
    {
        var world = CreateWorld();
        Floor(world);
        var (sleeper, _) = Box(world, BodyDescription.Dynamic(default), -8, 8, 0.5f, 16.5f);
        var (awake, _) = Box(world, BodyDescription.Dynamic(default) with { Dynamics = new BodyDynamics { AllowSleep = false } }, 40, 56, 0.5f, 16.5f);
        Assert.IsTrue(world.IsAwake(sleeper));
        Step(world, 3);
        Assert.IsFalse(world.IsAwake(sleeper), "at rest for three seconds: asleep");
        Assert.IsTrue(world.IsAwake(awake), "a body that may not sleep stays awake");
        world.ApplyImpulse(sleeper, Speed(world, 0, 20) * world.MassOf(sleeper), wake: false);
        Assert.IsFalse(world.IsAwake(sleeper), "an impulse that does not wake is ignored by a sleeping body");
        world.ApplyImpulse(sleeper, Speed(world, 0, 20) * world.MassOf(sleeper));
        Assert.IsTrue(world.IsAwake(sleeper), "an impulse wakes by default");
        Assert.AreEqual(20, SpeedUp(world, sleeper), 0.01f);
        world.SetAwake(sleeper, false);
        Assert.IsFalse(world.IsAwake(sleeper));
        world.SetAwake(sleeper, true);
        Assert.IsTrue(world.IsAwake(sleeper));
    }

    [TestMethod]
    public void Mass_data_follow_the_shapes()
    {
        var world = CreateWorld();
        var (centred, _) = Box(world, BodyDescription.Dynamic(default), -8, 8, 0, 16);
        float mass = world.MassOf(centred);
        float side = 16 * Scale;
        Assert.AreEqual(mass * (side * side + side * side) / 12, world.InertiaOf(centred), world.InertiaOf(centred) * 0.01f, "a box's inertia about its centre: m (w² + h²) / 12");
        var body = world.CreateBody(default, BodyDescription.Dynamic(At(world, 100, 50)));
        world.AddShape(body, ShapeDescription.Box(new Vector2(side, side), By(world, 10, 4)));
        Assert.IsTrue(Vector2.Distance(At(world, 110, 54), world.CenterOfMassOf(body)) < 1e-3f * Scale + 1e-5f, $"the centre of mass follows the shape: {world.CenterOfMassOf(body)}");
        Assert.AreEqual(mass, world.MassOf(body), mass * 1e-4f, "the same box has the same mass wherever it is");
        var (still, _) = Box(world, BodyDescription.Static(), 200, 216, 0, 16);
        Assert.AreEqual(0, world.MassOf(still), "static bodies have no mass");
    }

    [TestMethod]
    public void Only_dynamic_bodies_take_forces()
    {
        var world = CreateWorld();
        var (kinematic, _) = Box(world, BodyDescription.Kinematic(default), -8, 8, 0, 16);
        Assert.ThrowsExactly<InvalidOperationException>(() => world.ApplyForce(kinematic, Vector2.One));
        Assert.ThrowsExactly<InvalidOperationException>(() => world.ApplyImpulse(kinematic, Vector2.One));
        Assert.ThrowsExactly<InvalidOperationException>(() => world.ApplyTorque(kinematic, 1));
        Assert.ThrowsExactly<InvalidOperationException>(() => world.ApplyForce(BodyId.None, Vector2.One));
    }

    [TestMethod]
    public void Dynamics_are_part_of_the_state_hash_and_applying_forces_allocates_nothing()
    {
        static ulong Hash(IPhysicsWorld2D world)
        {
            var hash = new StateHasher();
            ((IStateHashed)world).HashState(hash);
            return hash.Value;
        }
        var (world, body) = Floating();
        ulong before = Hash(world);
        world.SetDynamics(body, world.DynamicsOf(body) with { LinearDamping = 1 });
        Assert.AreNotEqual(before, Hash(world), "damping changes what happens next: it is state");
        var centre = world.CenterOfMassOf(body);
        AllocationAssert.None(() =>
        {
            world.ApplyForce(body, new Vector2(0.01f, 0));
            world.ApplyImpulse(body, new Vector2(0, 0.001f), centre);
            world.ApplyTorque(body, 0.001f);
            _ = world.DynamicsOf(body);
            _ = world.MassOf(body) + world.InertiaOf(body) + world.AngularVelocityOf(body);
            world.Step(1 / 240f);
        }, because: "forces are applied every step");
    }
}

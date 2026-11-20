using System.Numerics;
using MicroEngine.Kernel;
using MicroEngine.Physics;

namespace MicroEngine.Testing.Physics;

/// <summary>
/// Joints (<see cref="PhysicsCapabilities.Joints"/>): revolute, distance and rope, prismatic, weld, wheel and target
/// joints, their limits, motors and springs, anchors and reaction forces, breaking, and their place in the state hash.
/// </summary>
public abstract class JointConformance : SolverConformance
{
    protected override PhysicsCapabilities Capability => PhysicsCapabilities.Joints | PhysicsCapabilities.DynamicBodies;

    private static void Step(IPhysicsWorld2D world, float seconds, Action? each = null)
    {
        for (int i = 0, n = (int)MathF.Round(seconds * 60); i < n; i++)
        {
            world.Step(1 / 60f);
            each?.Invoke();
        }
    }

    private float Distance(Vector2 a, Vector2 b) => Vector2.Distance(a, b) / Scale;

    /// <summary>A static anchor at (0, 100) and a 16 × 16 dynamic box centred at (<paramref name="x"/>, <paramref name="height"/>).</summary>
    private (IPhysicsWorld2D World, BodyId Anchor, BodyId Body) Hanging(float x = 40, float height = 100, BodyDynamics dynamics = default)
    {
        var world = CreateWorld();
        var (anchor, _) = Box(world, BodyDescription.Static(), -2, 2, 98, 102, owner: Owner.Level);
        var (body, _) = Box(world, BodyDescription.Dynamic(default) with { Dynamics = dynamics }, x - 8, x + 8, height - 8, height + 8, owner: Owner.A);
        return (world, anchor, body);
    }

    [TestMethod]
    public void A_revolute_joint_makes_a_pendulum()
    {
        var (world, anchor, body) = Hanging();
        var pivot = At(world, 0, 100);
        var joint = world.CreateJoint(JointDescription.Revolute(anchor, body, pivot));
        Assert.IsTrue(world.IsAlive(joint));
        Assert.AreEqual(1, world.Joints.Length);
        Assert.AreEqual(JointKind.Revolute, world.DescriptionOf(joint).Kind);
        float lowest = 100;
        Step(world, 2, () =>
        {
            Assert.AreEqual(40, Distance(pivot, world.PositionOf(body)), 0.5f, "the arm keeps its length");
            lowest = MathF.Min(lowest, HeightOf(world, world.PositionOf(body)));
        });
        Assert.IsTrue(lowest < 65, $"it swung down: {lowest}");
        Assert.IsTrue(Distance(world.AnchorAOf(joint), pivot) < 0.01f, "the anchor on the static body stays where it was");
        Assert.IsTrue(Distance(world.AnchorBOf(joint), pivot) < 0.5f, "and the anchor on the pendulum stays with it");
        Assert.IsTrue(world.ReactionForceOf(joint).Length() > 0, "the joint holds the pendulum's weight");
    }

    [TestMethod]
    public void A_revolute_limit_bounds_the_angle()
    {
        var (world, anchor, body) = Hanging();
        var joint = world.CreateJoint(JointDescription.Revolute(anchor, body, At(world, 0, 100)) with { EnableLimit = true, Lower = -0.5f, Upper = 0.5f });
        Assert.AreEqual(0, world.JointCoordinateOf(joint), 0.01f, "the angle is measured from where the joint was made");
        float widest = 0;
        Step(world, 2, () => widest = MathF.Max(widest, MathF.Abs(world.JointCoordinateOf(joint))));
        Assert.IsTrue(widest is > 0.4f and < 0.56f, $"it swung to the limit and no further: {widest}");
        world.SetJointLimit(joint, false, 0, 0);
        Step(world, 1, () => widest = MathF.Max(widest, MathF.Abs(world.JointCoordinateOf(joint))));
        Assert.IsTrue(widest > 1, $"without the limit it swings on: {widest}");
        Assert.ThrowsExactly<ArgumentException>(() => world.SetJointLimit(joint, true, 1, -1));
    }

    [TestMethod]
    public void A_revolute_motor_turns_a_body_at_its_speed()
    {
        var (world, anchor, body) = Hanging(0, 100, new BodyDynamics { GravityScale = 0 });
        float inertia = world.InertiaOf(body);
        var joint = world.CreateJoint(JointDescription.Revolute(anchor, body, At(world, 0, 100)) with { EnableMotor = true, MotorSpeed = 2, MaxMotorForce = inertia * 100 });
        Step(world, 1);
        Assert.AreEqual(2, world.AngularVelocityOf(body), 0.05f, "radians per second, the way rotation grows");
        Assert.IsTrue(world.RotationOf(body) > 1.5f);
        world.SetJointMotor(joint, true, -1, inertia * 100);
        Step(world, 1);
        Assert.AreEqual(-1, world.AngularVelocityOf(body), 0.05f);
        Assert.AreEqual(-1, world.DescriptionOf(joint).MotorSpeed, "the description follows the joint");
    }

    [TestMethod]
    public void A_distance_joint_keeps_two_points_apart_and_a_rope_only_stops_them_from_parting()
    {
        var (world, anchor, body) = Hanging(0, 60);
        var top = At(world, 0, 100);
        var rod = world.CreateJoint(JointDescription.Distance(anchor, body, top, world.PositionOf(body)));
        Assert.AreEqual(40, world.DescriptionOf(rod).Length / Scale, 0.01f, "the rest length is the distance when it was made");
        world.ApplyImpulse(body, Speed(world, 80, 0) * world.MassOf(body));
        Step(world, 2, () => Assert.AreEqual(40, Distance(top, world.AnchorBOf(rod)), 0.6f, "a rod"));
        Assert.AreEqual(40, world.JointCoordinateOf(rod) / Scale, 0.6f, "its coordinate is its length");
        world.SetJointLength(rod, 25 * Scale);
        Step(world, 2);
        Assert.AreEqual(25, Distance(top, world.AnchorBOf(rod)), 0.6f, "a shorter rod pulls the body in");

        var (world2, anchor2, body2) = Hanging(0, 90);
        var rope = world2.CreateJoint(JointDescription.Rope(anchor2, body2, At(world2, 0, 100), world2.PositionOf(body2), 50 * Scale));
        float h0 = HeightOf(world2, world2.PositionOf(body2));
        Step(world2, 0.1f);
        float fell = h0 - HeightOf(world2, world2.PositionOf(body2));
        Assert.IsTrue(fell > 0.4f * 0.5f * 9.81f * 32 * 0.01f, $"a slack rope lets the body fall freely: {fell}");
        float longest = 0;
        Step(world2, 3, () => longest = MathF.Max(longest, Distance(At(world2, 0, 100), world2.AnchorBOf(rope))));
        Assert.IsTrue(longest is > 49 and < 51.5f, $"the rope stops it at its length: {longest}");
    }

    [TestMethod]
    public void A_prismatic_joint_slides_along_its_axis_with_a_motor_and_stops_at_its_limits()
    {
        var (world, anchor, body) = Hanging(0, 100);
        float mass = world.MassOf(body);
        var joint = world.CreateJoint(JointDescription.Prismatic(anchor, body, world.PositionOf(body), new Vector2(1, 0)) with
        {
            EnableMotor = true, MotorSpeed = 30 * Scale, MaxMotorForce = mass * 3000 * Scale,
            EnableLimit = true, Lower = 0, Upper = 50 * Scale,
        });
        Step(world, 1);
        Assert.AreEqual(30, XOf(world.PositionOf(body)), 1.5f, "one second at 30 units a second");
        Assert.AreEqual(100, HeightOf(world, world.PositionOf(body)), 0.5f, "the rail holds it against gravity");
        Assert.AreEqual(30, world.JointCoordinateOf(joint) / Scale, 1.5f, "the coordinate is the translation");
        Step(world, 2);
        Assert.AreEqual(50, XOf(world.PositionOf(body)), 0.6f, "the upper limit");
        Assert.AreEqual(0, world.RotationOf(body), 0.02f, "a prismatic joint does not turn");
    }

    [TestMethod]
    public void A_weld_joint_makes_two_bodies_move_as_one()
    {
        var world = CreateWorld();
        var dynamics = new BodyDynamics { GravityScale = 0 };
        var (a, _) = Box(world, BodyDescription.Dynamic(default) with { Dynamics = dynamics }, -8, 8, 92, 108, owner: Owner.A);
        var (b, _) = Box(world, BodyDescription.Dynamic(default) with { Dynamics = dynamics }, 12, 28, 92, 108, owner: Owner.B);
        world.CreateJoint(JointDescription.Weld(a, b, At(world, 10, 100)));
        world.ApplyImpulse(b, Speed(world, 0, 40) * world.MassOf(b));
        Step(world, 1, () =>
        {
            Assert.AreEqual(20, Distance(world.PositionOf(a), world.PositionOf(b)), 0.5f, "they stay 20 units apart");
            Assert.AreEqual(world.RotationOf(a), world.RotationOf(b), 0.03f, "and turn together");
        });
        Assert.IsTrue(MathF.Abs(world.RotationOf(a)) > 0.3f, "the off-centre push turned the pair");
    }

    [TestMethod]
    public void A_wheel_joint_with_a_motor_drives_a_cart()
    {
        var world = CreateWorld();
        if ((world.Capabilities & PhysicsCapabilities.Circles) == 0) Assert.Inconclusive($"{world.Backend} has no circles");
        Floor(world);
        var (chassis, _) = Box(world, BodyDescription.Dynamic(default), -20, 20, 12, 22, owner: Owner.A);
        var wheels = new List<BodyId>();
        var joints = new List<JointId>();
        foreach (float x in new[] { -14f, 14f })
        {
            var wheel = world.CreateBody(Owner.B, BodyDescription.Dynamic(At(world, x, 6.2f)));
            world.AddShape(wheel, ShapeDescription.Circle(6 * Scale) with { Friction = 0.9f });
            wheels.Add(wheel);
            joints.Add(world.CreateJoint(JointDescription.Wheel(chassis, wheel, world.PositionOf(wheel), world.Up) with
            {
                EnableSpring = true, Hertz = 5, DampingRatio = 0.7f,
                EnableMotor = true, MotorSpeed = 6, MaxMotorForce = world.InertiaOf(wheel) * 2000,
            }));
        }
        Step(world, 0.5f);
        float x0 = XOf(world.PositionOf(chassis));
        Step(world, 2);
        float travelled = MathF.Abs(XOf(world.PositionOf(chassis)) - x0);
        Assert.IsTrue(travelled > 30, $"the motor drives the cart: {travelled} units in two seconds");
        Assert.AreEqual(6, MathF.Abs(world.AngularVelocityOf(wheels[0])), 1.5f, "the wheels turn at the motor's speed");
        for (int i = 0; i < 2; i++)
            Assert.AreEqual(i == 0 ? -14 : 14, XOf(world.PositionOf(wheels[i])) - XOf(world.PositionOf(chassis)), 1.5f, "the suspension keeps each wheel under the chassis");
        Assert.IsTrue(HeightOf(world, world.PositionOf(chassis)) is > 8 and < 26, "the chassis rides on its springs");
        world.SetJointSpring(joints[0], true, 8, 1);
        Assert.AreEqual(8, world.DescriptionOf(joints[0]).Hertz);
    }

    [TestMethod]
    public void A_target_joint_drags_a_body_to_a_point()
    {
        var (world, _, body) = Hanging(0, 60);
        var grab = world.PositionOf(body);
        var joint = world.CreateJoint(JointDescription.Target(body, grab) with { MaxForce = world.MassOf(body) * 100000 * Scale });
        Step(world, 0.5f);
        Assert.IsTrue(Distance(world.PositionOf(body), grab) < 3, "held where it was grabbed, against gravity");
        var target = At(world, 60, 90);
        world.SetJointTarget(joint, target);
        Step(world, 2);
        Assert.IsTrue(Distance(world.PositionOf(body), target) < 3, $"dragged to the target: {world.PositionOf(body)} / {target}");
        Assert.AreEqual(target, world.DescriptionOf(joint).TargetPoint);
        world.DestroyJoint(joint);
        Step(world, 0.5f);
        Assert.IsTrue(HeightOf(world, world.PositionOf(body)) < 85, "released: it falls");
    }

    [TestMethod]
    public void Joints_end_with_their_bodies_and_stale_ids_are_refused()
    {
        var (world, anchor, body) = Hanging();
        var joint = world.CreateJoint(JointDescription.Revolute(anchor, body, At(world, 0, 100)));
        var second = world.CreateJoint(JointDescription.Distance(anchor, body, At(world, 0, 100), world.PositionOf(body)));
        Assert.AreEqual(2, world.Joints.Length);
        world.DestroyJoint(second);
        Assert.IsFalse(world.IsAlive(second));
        world.DestroyJoint(second);
        Assert.AreEqual(1, world.Joints.Length);
        Assert.AreEqual(joint, world.Joints[0]);
        world.DestroyBody(body);
        Assert.IsFalse(world.IsAlive(joint), "destroying a body destroys its joints");
        Assert.AreEqual(0, world.Joints.Length);
        Assert.ThrowsExactly<InvalidOperationException>(() => world.AnchorAOf(joint));
        Assert.ThrowsExactly<InvalidOperationException>(() => world.SetJointMotor(joint, true, 1, 1));
        Assert.ThrowsExactly<InvalidOperationException>(() => world.DescriptionOf(joint));
        Assert.IsFalse(world.IsAlive(JointId.None));
        world.Step(1 / 60f);

        var (other, _) = Box(world, BodyDescription.Dynamic(default), 60, 76, 100, 116);
        Assert.ThrowsExactly<InvalidOperationException>(() => world.CreateJoint(JointDescription.Revolute(anchor, body, default)), "a destroyed body");
        Assert.ThrowsExactly<ArgumentException>(() => world.CreateJoint(JointDescription.Revolute(other, other, default)), "a body cannot be joined to itself");
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => world.CreateJoint(JointDescription.Revolute(anchor, other, default) with { Hertz = -1 }));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => world.CreateJoint(JointDescription.Prismatic(anchor, other, default, Vector2.Zero)), "an axis needs a direction");
        Assert.AreEqual(0, world.Joints.Length, "refused joints were never created");
    }

    [TestMethod]
    public void A_joint_breaks_when_its_reaction_force_passes_its_break_force()
    {
        var (world, anchor, body) = Hanging(0, 60);
        float weight = world.MassOf(body) * 9.81f * (world.Skin > 0.1f ? 32 : 1); // newtons in world units
        var strong = world.CreateJoint(JointDescription.Distance(anchor, body, At(world, 0, 100), world.PositionOf(body)) with { BreakForce = weight * 5 });
        Step(world, 1);
        Assert.IsTrue(world.IsAlive(strong), "it holds the weight");
        Assert.AreEqual(0, world.BrokenJoints.Length);
        Assert.AreEqual(weight, world.ReactionForceOf(strong).Length(), weight * 0.1f, "the reaction force is the weight it carries");
        world.DestroyJoint(strong);
        var weak = world.CreateJoint(JointDescription.Distance(anchor, body, At(world, 0, 100), world.PositionOf(body)) with { BreakForce = weight * 0.5f });
        int broken = 0;
        Step(world, 1, () =>
        {
            foreach (var j in world.BrokenJoints)
            {
                Assert.AreEqual(weak, j);
                broken++;
            }
        });
        Assert.AreEqual(1, broken, "reported once, in the step it broke");
        Assert.IsFalse(world.IsAlive(weak));
        Assert.IsTrue(HeightOf(world, world.PositionOf(body)) < 40, "and the body fell");
    }

    [TestMethod]
    public void Joints_are_part_of_the_state_hash_and_stepping_them_allocates_nothing()
    {
        static ulong Hash(IPhysicsWorld2D world)
        {
            var hash = new StateHasher();
            ((IStateHashed)world).HashState(hash);
            return hash.Value;
        }
        (IPhysicsWorld2D, JointId) Scene()
        {
            var (world, anchor, body) = Hanging();
            return (world, world.CreateJoint(JointDescription.Revolute(anchor, body, At(world, 0, 100))));
        }
        var (a, ja) = Scene();
        var (b, jb) = Scene();
        var (bare, _, _) = Hanging();
        Assert.AreNotEqual(Hash(bare), Hash(a), "a joint is state");
        for (int i = 0; i < 120; i++)
        {
            a.Step(1 / 60f);
            b.Step(1 / 60f);
            Assert.AreEqual(Hash(a), Hash(b), $"the same calls give the same state, step {i}");
        }
        a.SetJointMotor(ja, true, 1, 1);
        Assert.AreNotEqual(Hash(a), Hash(b), "a joint's motor is state");
        b.SetJointMotor(jb, true, 1, 1);
        Assert.AreEqual(Hash(a), Hash(b));
        AllocationAssert.None(() =>
        {
            a.Step(1 / 240f);
            _ = a.AnchorAOf(ja) + a.AnchorBOf(ja) + a.ReactionForceOf(ja);
            _ = a.JointCoordinateOf(ja) + a.ReactionTorqueOf(ja);
            foreach (var j in a.Joints) _ = a.IsAlive(j);
        }, because: "joints are read every step");
    }
}

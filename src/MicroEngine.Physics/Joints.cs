using System.Numerics;

namespace MicroEngine.Physics;

/// <summary>
/// A joint of an <see cref="IPhysicsWorld2D"/> (<see cref="PhysicsCapabilities.Joints"/>). Generation-checked like
/// <see cref="BodyId"/>; <c>default</c> is <see cref="None"/>.
/// </summary>
/// <param name="Index">The slot.</param>
/// <param name="Generation">The generation of the slot's joint; 0 only for <see cref="None"/>.</param>
public readonly record struct JointId(uint Index, uint Generation)
{
    /// <summary>The id of no joint.</summary>
    public static JointId None => default;
    /// <summary>Whether this is <see cref="None"/>.</summary>
    public bool IsNone => Generation == 0;
    /// <inheritdoc/>
    public override string ToString() => IsNone ? "joint:none" : $"joint:{Index}#{Generation}";
}

/// <summary>What a joint constrains.</summary>
public enum JointKind
{
    /// <summary>A pin: the two bodies turn about a shared point (hinges, pendulums, chains, ragdoll limbs). Limit and coordinate are angles in radians; the motor is a torque.</summary>
    Revolute,
    /// <summary>Keeps two points at a distance: a rod, a spring, or a rope with a limit and no spring force. Limit and coordinate are lengths.</summary>
    Distance,
    /// <summary>A slider: body B moves along an axis fixed in body A and does not turn (pistons, lifts, doors). Limit and coordinate are translations; the motor is a force.</summary>
    Prismatic,
    /// <summary>Glues two bodies together; with a spring, a soft glue that bends.</summary>
    Weld,
    /// <summary>A wheel: body B slides along an axis fixed in body A (the suspension, with a spring) and spins freely or by a motor torque.</summary>
    Wheel,
    /// <summary>Pulls a point of one body toward a world point with a spring of bounded force (dragging a body with the pointer).</summary>
    Target,
}

/// <summary>
/// What <see cref="IPhysicsWorld2D.CreateJoint"/> creates. Build one with a factory (<see cref="Revolute"/>,
/// <see cref="Distance"/>, <see cref="Rope"/>, <see cref="Prismatic"/>, <see cref="Weld"/>, <see cref="Wheel"/>,
/// <see cref="Target"/>) and set limits, motors and springs with <c>with</c>. Anchors and axes are given in world
/// space as the bodies are when the joint is created; the joint then keeps them fixed in each body.
/// </summary>
public readonly record struct JointDescription
{
    /// <summary>What the joint constrains.</summary>
    public JointKind Kind { get; init; }
    /// <summary>The first body (for a wheel, the chassis; unused by a target joint).</summary>
    public BodyId BodyA { get; init; }
    /// <summary>The second body (for a wheel, the wheel; for a target joint, the dragged body).</summary>
    public BodyId BodyB { get; init; }
    /// <summary>The world point of the joint on body A when it is created.</summary>
    public Vector2 AnchorA { get; init; }
    /// <summary>The world point of the joint on body B when it is created (the same as <see cref="AnchorA"/> except for distance joints).</summary>
    public Vector2 AnchorB { get; init; }
    /// <summary>The world direction body B slides along (prismatic, wheel); any length but zero.</summary>
    public Vector2 Axis { get; init; }
    /// <summary>Whether the two bodies still collide with each other (false by default).</summary>
    public bool CollideConnected { get; init; }
    /// <summary>Whether <see cref="Lower"/> and <see cref="Upper"/> bound the joint's coordinate.</summary>
    public bool EnableLimit { get; init; }
    /// <summary>The lower limit: radians (revolute), translation (prismatic, wheel) or the shortest length (distance).</summary>
    public float Lower { get; init; }
    /// <summary>The upper limit: radians (revolute), translation (prismatic, wheel) or the longest length (distance).</summary>
    public float Upper { get; init; }
    /// <summary>Whether the motor drives the joint.</summary>
    public bool EnableMotor { get; init; }
    /// <summary>The speed the motor aims for: radians per second (revolute, wheel) or world units per second (prismatic, distance).</summary>
    public float MotorSpeed { get; init; }
    /// <summary>The most the motor applies to reach its speed: a torque (revolute, wheel) or a force (prismatic, distance).</summary>
    public float MaxMotorForce { get; init; }
    /// <summary>Whether the joint is springy: a distance spring, a wheel's suspension, a soft weld, a revolute or prismatic joint pulled back to where it was made.</summary>
    public bool EnableSpring { get; init; }
    /// <summary>The spring's stiffness as a frequency in cycles per second (0 with <see cref="EnableSpring"/>: no spring force).</summary>
    public float Hertz { get; init; }
    /// <summary>The spring's damping ratio: 0 oscillates forever, 1 settles without overshoot.</summary>
    public float DampingRatio { get; init; }
    /// <summary>A distance joint's rest length; negative (the factories' default) for the distance between the anchors when it is created.</summary>
    public float Length { get; init; }
    /// <summary>A target joint's world point (<see cref="IPhysicsWorld2D.SetJointTarget"/> moves it).</summary>
    public Vector2 TargetPoint { get; init; }
    /// <summary>The most force a target joint's spring applies.</summary>
    public float MaxForce { get; init; }
    /// <summary>
    /// The joint is destroyed at the end of a step in which the force it applies to hold the bodies together exceeds
    /// this (it is then in <see cref="IPhysicsWorld2D.BrokenJoints"/>); 0 (the default) never breaks.
    /// </summary>
    public float BreakForce { get; init; }

    /// <summary>A pin at the world point <paramref name="anchor"/>.</summary>
    public static JointDescription Revolute(BodyId a, BodyId b, Vector2 anchor) =>
        new() { Kind = JointKind.Revolute, BodyA = a, BodyB = b, AnchorA = anchor, AnchorB = anchor };

    /// <summary>A rod between a point of each body, as long as they are apart now. Add <see cref="EnableSpring"/> for a spring.</summary>
    public static JointDescription Distance(BodyId a, BodyId b, Vector2 anchorA, Vector2 anchorB) =>
        new() { Kind = JointKind.Distance, BodyA = a, BodyB = b, AnchorA = anchorA, AnchorB = anchorB, Length = -1 };

    /// <summary>A rope between a point of each body: slack up to <paramref name="maxLength"/>, taut beyond.</summary>
    public static JointDescription Rope(BodyId a, BodyId b, Vector2 anchorA, Vector2 anchorB, float maxLength) =>
        Distance(a, b, anchorA, anchorB) with { EnableSpring = true, Hertz = 0, EnableLimit = true, Lower = 0, Upper = maxLength };

    /// <summary>A slider through the world point <paramref name="anchor"/> along the world direction <paramref name="axis"/>.</summary>
    public static JointDescription Prismatic(BodyId a, BodyId b, Vector2 anchor, Vector2 axis) =>
        new() { Kind = JointKind.Prismatic, BodyA = a, BodyB = b, AnchorA = anchor, AnchorB = anchor, Axis = axis };

    /// <summary>Glue at the world point <paramref name="anchor"/>.</summary>
    public static JointDescription Weld(BodyId a, BodyId b, Vector2 anchor) =>
        new() { Kind = JointKind.Weld, BodyA = a, BodyB = b, AnchorA = anchor, AnchorB = anchor };

    /// <summary>A wheel centred at the world point <paramref name="anchor"/> whose suspension travels along <paramref name="axis"/> (usually the world's up).</summary>
    public static JointDescription Wheel(BodyId chassis, BodyId wheel, Vector2 anchor, Vector2 axis) =>
        new() { Kind = JointKind.Wheel, BodyA = chassis, BodyB = wheel, AnchorA = anchor, AnchorB = anchor, Axis = axis };

    /// <summary>A spring that pulls the world point <paramref name="anchor"/> of <paramref name="body"/> toward a target, which starts there.</summary>
    public static JointDescription Target(BodyId body, Vector2 anchor) =>
        new() { Kind = JointKind.Target, BodyB = body, AnchorA = anchor, AnchorB = anchor, TargetPoint = anchor, EnableSpring = true, Hertz = 5, DampingRatio = 1, MaxForce = float.MaxValue };

    /// <summary>Throws when the description is invalid whatever the backend declares.</summary>
    /// <exception cref="ArgumentOutOfRangeException">An undefined kind, a value that is not finite, a negative frequency, damping, force or length limit, or a prismatic or wheel joint without an axis.</exception>
    /// <exception cref="ArgumentException">Both bodies are the same, or the lower limit is above the upper one.</exception>
    public void Validate()
    {
        if (!Enum.IsDefined(Kind)) throw new ArgumentOutOfRangeException(nameof(Kind), Kind, "Not a joint kind.");
        if (Kind != JointKind.Target && BodyA == BodyB) throw new ArgumentException("A joint connects two different bodies.", nameof(BodyB));
        Finite(AnchorA.X, nameof(AnchorA)); Finite(AnchorA.Y, nameof(AnchorA));
        Finite(AnchorB.X, nameof(AnchorB)); Finite(AnchorB.Y, nameof(AnchorB));
        Finite(TargetPoint.X, nameof(TargetPoint)); Finite(TargetPoint.Y, nameof(TargetPoint));
        Finite(Axis.X, nameof(Axis)); Finite(Axis.Y, nameof(Axis));
        if (Kind is JointKind.Prismatic or JointKind.Wheel && Axis == Vector2.Zero) throw new ArgumentOutOfRangeException(nameof(Axis), Axis, "A prismatic or wheel joint needs an axis.");
        ValidateLimit(Lower, Upper);
        Finite(MotorSpeed, nameof(MotorSpeed));
        NotNegative(MaxMotorForce, nameof(MaxMotorForce));
        NotNegative(Hertz, nameof(Hertz));
        NotNegative(DampingRatio, nameof(DampingRatio));
        if (float.IsNaN(Length) || float.IsInfinity(Length)) throw new ArgumentOutOfRangeException(nameof(Length), Length, "The length must be finite.");
        NotNegative(MaxForce, nameof(MaxForce));
        NotNegative(BreakForce, nameof(BreakForce));
    }

    /// <summary>Throws when a limit is not finite or its lower bound is above its upper one.</summary>
    public static void ValidateLimit(float lower, float upper)
    {
        Finite(lower, nameof(Lower));
        Finite(upper, nameof(Upper));
        if (lower > upper) throw new ArgumentException($"The lower limit ({lower}) is above the upper one ({upper}).", nameof(lower));
    }

    private static void Finite(float value, string name)
    {
        if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(name, value, "The value must be finite.");
    }

    private static void NotNegative(float value, string name)
    {
        if (!(value >= 0)) throw new ArgumentOutOfRangeException(name, value, "The value must be zero or more.");
    }
}

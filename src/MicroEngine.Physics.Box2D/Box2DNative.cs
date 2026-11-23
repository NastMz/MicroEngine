using System.Runtime.InteropServices;

namespace MicroEngine.Physics.Box2D;

/// <summary>Box2D's <c>b2WorldId</c>, laid out as Box2D declares it, to pass to Box2D's own functions.</summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct B2WorldId
{
    /// <summary>Box2D's <c>index1</c>.</summary>
    public readonly ushort Index1;
    /// <summary>Box2D's <c>generation</c>.</summary>
    public readonly ushort Generation;
}

/// <summary>Box2D's <c>b2BodyId</c>, laid out as Box2D declares it.</summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct B2BodyId
{
    /// <summary>Box2D's <c>index1</c>.</summary>
    public readonly int Index1;
    /// <summary>Box2D's <c>world0</c>.</summary>
    public readonly ushort World0;
    /// <summary>Box2D's <c>generation</c>.</summary>
    public readonly ushort Generation;
}

/// <summary>Box2D's <c>b2ShapeId</c>, laid out as Box2D declares it.</summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct B2ShapeId
{
    /// <summary>Box2D's <c>index1</c>.</summary>
    public readonly int Index1;
    /// <summary>Box2D's <c>world0</c>.</summary>
    public readonly ushort World0;
    /// <summary>Box2D's <c>generation</c>.</summary>
    public readonly ushort Generation;
}

/// <summary>Box2D's <c>b2JointId</c>, laid out as Box2D declares it.</summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct B2JointId
{
    /// <summary>Box2D's <c>index1</c>.</summary>
    public readonly int Index1;
    /// <summary>Box2D's <c>world0</c>.</summary>
    public readonly ushort World0;
    /// <summary>Box2D's <c>generation</c>.</summary>
    public readonly ushort Generation;
}

/// <summary>
/// The way out of the physics contract: the Box2D ids behind a <see cref="Box2DPhysicsWorld2D"/>, for a game that
/// calls Box2D's own C API for something the contract does not cover (an explosion, a conveyor's surface speed, a
/// motor joint that drives a velocity, wind). Asked for with <see cref="Box2DWorldOptions.NativeAccess"/> and read
/// from <see cref="Box2DPhysicsWorld2D.NativeIds"/>.
/// </summary>
/// <remarks>
/// <para>The game declares the functions it calls, against <see cref="Library"/>, with these id types:</para>
/// <code>
/// [DllImport(Box2DNative.Library)] static extern void b2Body_SetBullet(B2BodyId body, [MarshalAs(UnmanagedType.U1)] bool flag);
/// b2Body_SetBullet(world.NativeIds.Body(crate), true);
/// </code>
/// <para>What that costs:</para>
/// <list type="bullet">
/// <item>Units are Box2D's: metres, radians, y up, whatever <see cref="Box2DWorldOptions"/> maps the game's units to.</item>
/// <item>The engine does not know what was changed: it is not in the state hash, not in
/// <see cref="IPhysicsWorld2D.DescriptionOf(ShapeId)"/>, and bodies, shapes and joints created through Box2D have no
/// engine id (queries and events skip them). Never destroy through Box2D what the engine created.</item>
/// <item>Box2D's API changes between its releases; <see cref="Box2DPhysicsWorld2D.Box2DVersion"/> says which one the
/// package carries.</item>
/// <item>It works where the package ships a shared library that exports Box2D: Windows, Linux, macOS and Android. In
/// the browser the runtime does not pass these ids by value, and on iOS it is not tested: not supported there.</item>
/// <item>No Box2D function may be called while a step runs, and worlds are not thread-safe.</item>
/// </list>
/// </remarks>
public sealed unsafe class Box2DNative
{
    /// <summary>The native library that holds Box2D's functions: the name for a game's own <c>DllImport</c> declarations.</summary>
    public const string Library = Native.Library;

    private readonly Box2DPhysicsWorld2D _world;

    internal Box2DNative(Box2DPhysicsWorld2D world) => _world = world;

    /// <summary>The Box2D world.</summary>
    /// <exception cref="ObjectDisposedException">The world was disposed.</exception>
    public B2WorldId World
    {
        get
        {
            long raw = 0;
            Native.me_native_id(_world.NativeWorld, 0, 0, &raw);
            return *(B2WorldId*)&raw;
        }
    }

    /// <summary>The Box2D body of <paramref name="body"/>.</summary>
    /// <exception cref="InvalidOperationException">The id is stale.</exception>
    public B2BodyId Body(BodyId body)
    {
        long raw = 0;
        Native.me_native_id(_world.NativeWorld, 1, _world.NativeBody(body), &raw);
        return *(B2BodyId*)&raw;
    }

    /// <summary>The Box2D shape of <paramref name="shape"/>.</summary>
    /// <exception cref="InvalidOperationException">The id is stale, or the shape is tile geometry, which is many Box2D shapes.</exception>
    public B2ShapeId Shape(ShapeId shape)
    {
        long raw = 0;
        if (Native.me_native_id(_world.NativeWorld, 2, _world.NativeShape(shape), &raw) == 0)
            throw new InvalidOperationException($"{shape} is tile geometry: it stands for many Box2D shapes and has no id of its own.");
        return *(B2ShapeId*)&raw;
    }

    /// <summary>The Box2D joint of <paramref name="joint"/>.</summary>
    /// <exception cref="InvalidOperationException">The id is stale.</exception>
    public B2JointId Joint(JointId joint)
    {
        long raw = 0;
        Native.me_native_id(_world.NativeWorld, 3, _world.NativeJoint(joint), &raw);
        return *(B2JointId*)&raw;
    }
}

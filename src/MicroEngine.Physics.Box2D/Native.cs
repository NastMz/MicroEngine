using System.Reflection;
using System.Runtime.InteropServices;

namespace MicroEngine.Physics.Box2D;

/// <summary>
/// The P/Invoke surface of the me_box2d layer (native/me_box2d.h). Every parameter is an <see cref="int"/>,
/// <see cref="float"/>, <see cref="ulong"/> or a pointer: the .NET WebAssembly interpreter aborts on structs passed by
/// value and mishandles 64-bit enums and function pointers, so nothing else crosses.
/// </summary>
internal static unsafe partial class Native
{
    public const string Library = "me_box2d";
    /// <summary>The <c>ME_API_VERSION</c> this assembly is written against.</summary>
    public const int ApiVersion = 3;

    public const int Static = 0, Kinematic = 1, Dynamic = 2;
    public const int Box = 0, Circle = 1, Capsule = 2, Polygon = 3;
    public const int Sensor = 1, SensorEvents = 2, OneWay = 4;
    public const int QuerySensors = 1, QueryPassOneWay = 2;

    static Native()
    {
        // iOS links the layer statically into the app: the symbols are in the main program.
        if (OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() || OperatingSystem.IsMacCatalyst())
            NativeLibrary.SetDllImportResolver(typeof(Native).Assembly, Resolve);
    }

    private static nint Resolve(string name, Assembly assembly, DllImportSearchPath? path) =>
        name == Library ? NativeLibrary.GetMainProgramHandle() : 0;

    [LibraryImport(Library)] public static partial int me_api_version();
    [LibraryImport(Library)] public static partial int me_box2d_version();
    [LibraryImport(Library)] public static partial float me_move_skin();

    [LibraryImport(Library)] public static partial nint me_world_create(float gravityX, float gravityY, int subSteps, int enableSleep);
    [LibraryImport(Library)] public static partial void me_world_destroy(nint world);
    [LibraryImport(Library)] public static partial void me_world_step(nint world, float timeStep);
    [LibraryImport(Library)] public static partial void me_world_counters(nint world, int* out4);

    [LibraryImport(Library)] public static partial int me_body_create(nint world, int type, float x, float y, float angle, int bullet);
    [LibraryImport(Library)] public static partial void me_body_destroy(nint world, int body);
    [LibraryImport(Library)] public static partial void me_body_state(nint world, int body, float* out6);
    [LibraryImport(Library)] public static partial void me_body_set_position(nint world, int body, float x, float y);
    [LibraryImport(Library)] public static partial void me_body_set_velocity(nint world, int body, float vx, float vy);
    [LibraryImport(Library)] public static partial float me_body_mass(nint world, int body);
    [LibraryImport(Library)] public static partial void me_body_set_rotation(nint world, int body, float angle);
    [LibraryImport(Library)] public static partial void me_body_set_angular_velocity(nint world, int body, float angularVelocity);
    [LibraryImport(Library)] public static partial void me_body_apply(nint world, int body, int kind, float x, float y, float px, float py, int atPoint, int wake);
    [LibraryImport(Library)] public static partial void me_body_apply_angular(nint world, int body, int kind, float value, int wake);
    [LibraryImport(Library)] public static partial void me_body_get_dynamics(nint world, int body, float* out5);
    [LibraryImport(Library)] public static partial void me_body_set_dynamics(nint world, int body, float* in5);
    [LibraryImport(Library)] public static partial int me_body_is_awake(nint world, int body);
    [LibraryImport(Library)] public static partial void me_body_set_awake(nint world, int body, int awake);
    [LibraryImport(Library)] public static partial void me_body_mass_data(nint world, int body, float* out4);

    [LibraryImport(Library)]
    public static partial int me_shape_create(nint world, int body, int kind, float* geometry, int pointCount, float radius,
        ulong category, ulong mask, int flags, float density, float friction, float restitution);
    [LibraryImport(Library)] public static partial void me_shape_destroy(nint world, int shape);
    [LibraryImport(Library)] public static partial void me_shape_bounds(nint world, int shape, float* out4);

    [LibraryImport(Library)] public static partial int me_joint_create(nint world, int kind, int bodyA, int bodyB, float* p17, int flags);
    [LibraryImport(Library)] public static partial void me_joint_destroy(nint world, int joint);
    [LibraryImport(Library)] public static partial void me_joint_state(nint world, int joint, float* out8);
    [LibraryImport(Library)] public static partial void me_joint_set_limit(nint world, int joint, int enable, float lower, float upper);
    [LibraryImport(Library)] public static partial void me_joint_set_motor(nint world, int joint, int enable, float speed, float maxForce);
    [LibraryImport(Library)] public static partial void me_joint_set_spring(nint world, int joint, int enable, float hertz, float dampingRatio);
    [LibraryImport(Library)] public static partial void me_joint_set_target(nint world, int joint, float x, float y);
    [LibraryImport(Library)] public static partial void me_joint_set_length(nint world, int joint, float length);

    [LibraryImport(Library)]
    public static partial int me_ray_cast(nint world, float x, float y, float dx, float dy, ulong category, ulong mask, int queryFlags, float* out5);
    [LibraryImport(Library)]
    public static partial int me_overlap_box(nint world, float minX, float minY, float maxX, float maxY, ulong category, ulong mask,
        int queryFlags, int* outShapes, int maxShapes);
    [LibraryImport(Library)]
    public static partial int me_move(nint world, int body, int shape, float dx, float dy, ulong category, ulong mask, int queryFlags,
        float* outXY2, int* outShapes, float* outNormals, int maxContacts);

    [LibraryImport(Library)]
    public static partial int me_sensor_events(nint world, int* outBegin, int maxBegin, int* outEnd, int maxEnd, int* outEndCount);
    [LibraryImport(Library)]
    public static partial int me_contact_events(nint world, int* outBegin, float* outNormals, int maxBegin, int* outEnd, int maxEnd, int* outEndCount);
}

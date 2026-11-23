// me_box2d: the flat C layer between MicroEngine.Physics.Box2D and Box2D v3 (box2d.env pins the release).
//
// Every function takes and returns only int, float, uint64_t and pointers to caller-owned buffers: no struct is passed
// or returned by value, and no function pointer crosses the boundary. The .NET WebAssembly interpreter aborts on
// 8-byte structs passed by value (Box2D's own b2BodyId and b2ShapeId) and mishandles 64-bit enum and function-pointer
// parameters, so the managed side never sees Box2D's types, which also change between Box2D versions. Every callback
// Box2D needs (queries, the one-way pre-solve rule) is implemented here: no managed code runs inside Box2D.
//
// Units are Box2D's: metres, radians, y up. Bodies and shapes are addressed by handles, small integers this layer
// issues in creation order and never reuses within a world; a destroyed handle stays invalid. Results that name shapes
// give handles, or -1 for a shape that no longer exists. A world is single-threaded (no Box2D task system), so the
// same calls give the same bits on every target built with floating-point contraction off.
#ifndef ME_BOX2D_H
#define ME_BOX2D_H

#include <stdint.h>

#if defined( ME_BOX2D_STATIC )
#define ME_API
#elif defined( _WIN32 )
#define ME_API __declspec( dllexport )
#else
#define ME_API __attribute__( ( visibility( "default" ) ) )
#endif

#ifdef __cplusplus
extern "C" {
#endif

// Raised whenever a signature or a rule of this layer changes; the managed side refuses another value.
#define ME_API_VERSION 4

typedef struct MeWorld MeWorld;

enum MeBodyType
{
	ME_STATIC = 0,
	ME_KINEMATIC = 1,
	ME_DYNAMIC = 2,
};

enum MeShapeKind
{
	ME_BOX = 0,		// geometry: half width, half height, centre x, centre y (body-local, axis-aligned)
	ME_CIRCLE = 1,	// geometry: centre x, centre y; radius
	ME_CAPSULE = 2, // geometry: x1, y1, x2, y2 (the two centres, body-local); radius
	ME_POLYGON = 3, // geometry: x, y of each of pointCount (3..8) body-local points; their convex hull
};

enum MeShapeFlags
{
	ME_SENSOR = 1,		  // a sensor: never collides, reports overlaps
	ME_SENSOR_EVENTS = 2, // sensors see this shape (needed on the visitor too)
	ME_ONE_WAY = 4,		  // a one-way platform: stops only what comes down onto its top (+y)
};

enum MeJointKind
{
	ME_REVOLUTE = 0,  // a pin; limit and coordinate in radians, motor torque
	ME_DISTANCE = 1,  // two points at a distance; limit and coordinate are lengths, motor force
	ME_PRISMATIC = 2, // a slider along the axis; limit and coordinate are translations, motor force
	ME_WELD = 3,	  // glue; the spring makes it soft
	ME_WHEEL = 4,	  // slides along the axis (suspension spring) and spins (motor torque)
	ME_TARGET = 5,	  // a spring of bounded force that pulls a point of body B to a world point (Box2D's motor joint)
};

enum MeQueryFlags
{
	ME_QUERY_SENSORS = 1,		// rays and overlaps return sensors
	ME_QUERY_PASS_ONE_WAY = 2,	// one-way platforms are passed through
};

// The layer's ME_API_VERSION and Box2D's version (major * 10000 + minor * 100 + revision).
ME_API int me_api_version( void );
ME_API int me_box2d_version( void );
// The largest gap (metres) me_move leaves between the moved shape and the surface that stopped it or that it rests on;
// moves never end inside a surface they did not start in.
ME_API float me_move_skin( void );

// A world with gravity (m/s²), stepped with subSteps sub-steps per me_world_step; enableSleep lets resting dynamic
// bodies sleep (it changes no result, only the work done). NULL when Box2D already holds its maximum of 128 worlds.
// Box2D keeps its worlds in one process-wide table: create and destroy worlds from one thread at a time.
ME_API MeWorld* me_world_create( float gravityX, float gravityY, int subSteps, int enableSleep );
ME_API void me_world_destroy( MeWorld* world );
// Advances the world by timeStep seconds (0 is allowed: events are recomputed, nothing moves). The events of the
// step replace the previous ones.
ME_API void me_world_step( MeWorld* world, float timeStep );
// bodies, shapes, contacts, awake bodies.
ME_API void me_world_counters( MeWorld* world, int* out4 );

// Creates a body (MeBodyType) at (x, y) turned by angle radians; bullet enables continuous collision for a dynamic body.
ME_API int me_body_create( MeWorld* world, int type, float x, float y, float angle, int bullet );
// Destroys the body and its shapes.
ME_API void me_body_destroy( MeWorld* world, int body );
// x, y, angle, linear velocity x, y, angular velocity.
ME_API void me_body_state( MeWorld* world, int body, float* out6 );
// Teleports the body (no collision; dynamic bodies it overlaps are pushed out by the next step).
ME_API void me_body_set_position( MeWorld* world, int body, float x, float y );
ME_API void me_body_set_velocity( MeWorld* world, int body, float vx, float vy );
ME_API float me_body_mass( MeWorld* world, int body );
// Turns the body to angle radians where it is (no collision), like me_body_set_position.
ME_API void me_body_set_rotation( MeWorld* world, int body, float angle );
ME_API void me_body_set_angular_velocity( MeWorld* world, int body, float angularVelocity );
// Applies (x, y) to a dynamic body: a force (kind 0, newtons, for the next step only) or a linear impulse (kind 1,
// newton seconds, at once); through the centre of mass, or at the world point (px, py) when atPoint is not 0. wake
// wakes a sleeping body; without it a sleeping body ignores the call.
ME_API void me_body_apply( MeWorld* world, int body, int kind, float x, float y, float px, float py, int atPoint, int wake );
// A torque (kind 0, newton metres, for the next step only) or an angular impulse (kind 1, at once).
ME_API void me_body_apply_angular( MeWorld* world, int body, int kind, float value, int wake );
// linear damping, angular damping, gravity scale, fixed rotation (0 or 1), sleep allowed (0 or 1).
ME_API void me_body_get_dynamics( MeWorld* world, int body, float* out5 );
ME_API void me_body_set_dynamics( MeWorld* world, int body, const float* in5 );
ME_API int me_body_is_awake( MeWorld* world, int body );
ME_API void me_body_set_awake( MeWorld* world, int body, int awake );
// mass (kg), world centre of mass x, y, rotational inertia about it (kg m²).
ME_API void me_body_mass_data( MeWorld* world, int body, float* out4 );

// Attaches a shape (MeShapeKind, geometry as described there) with collision layers, MeShapeFlags, density (kg/m²),
// friction and restitution; returns its handle, or -1 when the geometry is not a valid shape (a degenerate polygon).
ME_API int me_shape_create( MeWorld* world, int body, int kind, const float* geometry, int pointCount, float radius,
	uint64_t category, uint64_t mask, int flags, float density, float friction, float restitution );
ME_API void me_shape_destroy( MeWorld* world, int shape );
// The shape's world bounds: min x, min y, max x, max y.
ME_API void me_shape_bounds( MeWorld* world, int shape, float* out4 );

// Creates a joint (MeJointKind) between bodies a and b (a is ignored by ME_TARGET) and returns its handle, or -1 when a
// body is not alive. Joint handles are issued like body handles. p17, in world space as the bodies are now:
//   0, 1   anchor on body A (ME_TARGET: the target point)        2, 3   anchor on body B
//   4, 5   axis direction (ME_PRISMATIC, ME_WHEEL), any length
//   6      limit enabled      7, 8   lower, upper (ME_DISTANCE: shortest and longest length)
//   9      motor enabled     10      motor speed       11   the most motor force or torque
//  12      spring enabled    13      spring hertz      14   spring damping ratio
//  15      rest length (ME_DISTANCE; negative: the distance between the anchors)
//  16      the most spring force (ME_TARGET)
// A revolute joint's angle and a prismatic joint's translation are 0 where the joint is created. flags: 1 lets the two
// bodies collide. Destroying a body destroys its joints.
ME_API int me_joint_create( MeWorld* world, int kind, int bodyA, int bodyB, const float* p17, int flags );
ME_API void me_joint_destroy( MeWorld* world, int joint );
// World anchor on A x, y, on B x, y, constraint force x, y, constraint torque, coordinate (angle, translation or
// length; 0 for the other kinds).
ME_API void me_joint_state( MeWorld* world, int joint, float* out8 );
// These wake the joint's bodies; a kind without the feature ignores the call.
ME_API void me_joint_set_limit( MeWorld* world, int joint, int enable, float lower, float upper );
ME_API void me_joint_set_motor( MeWorld* world, int joint, int enable, float speed, float maxForce );
ME_API void me_joint_set_spring( MeWorld* world, int joint, int enable, float hertz, float dampingRatio );
ME_API void me_joint_set_target( MeWorld* world, int joint, float x, float y );
ME_API void me_joint_set_length( MeWorld* world, int joint, float length );

// The closest shape the segment from (x, y) to (x + dx, y + dy) enters, among the shapes whose layers accept the
// query's (category, mask) and MeQueryFlags; returns its handle or -1. out5: point x, y, normal x, y, fraction. A shape
// holding the start point is not hit; a one-way platform is hit only on its top.
ME_API int me_ray_cast( MeWorld* world, float x, float y, float dx, float dy, uint64_t category, uint64_t mask,
	int queryFlags, float* out5 );
// The shapes that truly overlap the axis-aligned box (b2World_OverlapShape, not the broad phase's fat bounds), in
// handle order; touching is not overlapping. Writes at most maxShapes handles and returns how many overlap.
ME_API int me_overlap_box( MeWorld* world, float minX, float minY, float maxX, float maxY, uint64_t category,
	uint64_t mask, int queryFlags, int* outShapes, int maxShapes );

/// Every shape the segment enters, nearest first (handle order at the same fraction), one hit per shape, by the rules
// of me_ray_cast. Writes at most maxHits handles and five floats each (point x, y, normal x, y, fraction) and returns
// how many shapes the segment enters.
ME_API int me_ray_cast_all( MeWorld* world, float x, float y, float dx, float dy, uint64_t category, uint64_t mask,
	int queryFlags, int* outShapes, float* outHits5, int maxHits );
// Query shapes are rounded boxes centred at a point: a core of half extents (hx, hy) with a radius around it. A box
// has no radius, a circle no core, a standing capsule a core with hx 0.
// The first shape the query shape would touch moving its centre from (x, y) by (dx, dy); returns its handle or -1.
// out5: the point where they touch x, y, the surface normal x, y, fraction. A shape it overlaps at the start is not
// hit; one within the slop of it but apart is hit at fraction 0 when the cast moves into it. A one-way platform is hit
// only on its top by a shape that started above it going down. The shape stops B2_LINEAR_SLOP short of the surface.
ME_API int me_shape_cast( MeWorld* world, float hx, float hy, float radius, float x, float y, float dx, float dy,
	uint64_t category, uint64_t mask, int queryFlags, float* out5 );
// The shapes that truly overlap the query shape centred at (x, y), in handle order, like me_overlap_box.
ME_API int me_overlap_shape( MeWorld* world, float hx, float hy, float radius, float x, float y, uint64_t category,
	uint64_t mask, int queryFlags, int* outShapes, int maxShapes );
// The shapes that hold the point (b2Shape_TestPoint), in handle order.
ME_API int me_overlap_point( MeWorld* world, float x, float y, uint64_t category, uint64_t mask, int queryFlags,
	int* outShapes, int maxShapes );
// The shape whose surface is nearest (x, y) within maxDistance (the lowest handle at the same distance); returns its
// handle or -1. out3: the nearest point x, y and its distance; a shape holding the point is at distance 0.
ME_API int me_closest_point( MeWorld* world, float x, float y, float maxDistance, uint64_t category, uint64_t mask,
	int queryFlags, float* out3 );

// Moves a kinematic body by (dx, dy) with collide-and-slide of its shape `shape` against the shapes the query accepts
// (sensors never block; MeQueryFlags as for queries; shapes of the body itself and shapes it already overlaps at the
// start never block). A box (or polygon) moves along x, then y, each as a shape cast that stops within me_move_skin of
// the first surface; a capsule uses Box2D's mover (b2World_CollideMover, b2SolvePlanes, then a cast). The body is
// teleported to the result, written to outXY2. Contacts are the surfaces that stopped the move or touch the shape at
// the end within me_move_skin: shape handles and unit normals pointing from the surface to the moved shape. Writes at
// most maxContacts and returns how many there are.
ME_API int me_move( MeWorld* world, int body, int shape, float dx, float dy, uint64_t category, uint64_t mask,
	int queryFlags, float* outXY2, int* outShapes, float* outNormals, int maxContacts );

// The sensor events of the last step as (sensor, visitor) handle pairs: begins, then ends through outEnd and
// *outEndCount. Visitors are non-sensor shapes of kinematic and dynamic bodies; a destroyed shape is -1. Returns the
// begin count (more than maxBegin when the buffer was short; likewise *outEndCount).
ME_API int me_sensor_events( MeWorld* world, int* outBegin, int maxBegin, int* outEnd, int maxEnd, int* outEndCount );
// The contact events of the last step (pairs with a dynamic body): begins as (a, b) handle pairs with four floats each
// in outBegin4 (the contact normal from a to b x, y, the contact point x, y: the middle of the manifold's points), ends
// as pairs through outEnd and *outEndCount; a destroyed shape is -1.
ME_API int me_contact_events( MeWorld* world, int* outBegin, float* outBegin4, int maxBegin, int* outEnd, int maxEnd,
	int* outEndCount );
// The speed (m/s) two shapes must approach each other at for their contact to be an impact (Box2D's hit event
// threshold, 1 m/s by default).
ME_API void me_world_set_impact_speed( MeWorld* world, float speed );
// The impacts of the last step (Box2D's hit events; pairs with a dynamic body): (a, b) handle pairs and six floats
// each (point x, y, normal from a to b x, y, approach speed, the normal impulse the contact applied in the step, 0
// when it no longer exists). Writes at most maxImpacts and returns how many there are.
ME_API int me_impact_events( MeWorld* world, int* outShapes, float* out6, int maxImpacts );
// The manifold points of the body's touching contacts: (own shape, other shape) handle pairs and six floats each
// (point x, y, normal from the other shape to the body x, y, separation, total normal impulse of the last step).
// Writes at most maxPoints and returns how many there are.
ME_API int me_body_contacts( MeWorld* world, int body, int* outShapes, float* out6, int maxPoints );

#ifdef __cplusplus
}
#endif
#endif

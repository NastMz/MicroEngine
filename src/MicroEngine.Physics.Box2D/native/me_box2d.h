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
#define ME_API_VERSION 1

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
// bodies sleep (it changes no result, only the work done).
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

// Attaches a shape (MeShapeKind, geometry as described there) with collision layers, MeShapeFlags, density (kg/m²),
// friction and restitution; returns its handle, or -1 when the geometry is not a valid shape (a degenerate polygon).
ME_API int me_shape_create( MeWorld* world, int body, int kind, const float* geometry, int pointCount, float radius,
	uint64_t category, uint64_t mask, int flags, float density, float friction, float restitution );
ME_API void me_shape_destroy( MeWorld* world, int shape );
// The shape's world bounds: min x, min y, max x, max y.
ME_API void me_shape_bounds( MeWorld* world, int shape, float* out4 );

// The closest shape the segment from (x, y) to (x + dx, y + dy) enters, among the shapes whose layers accept the
// query's (category, mask) and MeQueryFlags; returns its handle or -1. out5: point x, y, normal x, y, fraction. A shape
// holding the start point is not hit; a one-way platform is hit only on its top.
ME_API int me_ray_cast( MeWorld* world, float x, float y, float dx, float dy, uint64_t category, uint64_t mask,
	int queryFlags, float* out5 );
// The shapes that truly overlap the axis-aligned box (b2World_OverlapShape, not the broad phase's fat bounds), in
// handle order; touching is not overlapping. Writes at most maxShapes handles and returns how many overlap.
ME_API int me_overlap_box( MeWorld* world, float minX, float minY, float maxX, float maxY, uint64_t category,
	uint64_t mask, int queryFlags, int* outShapes, int maxShapes );

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
// The contact events of the last step (pairs with a dynamic body): begins as (a, b) handle pairs with the contact
// normal from a to b in outNormals, ends as pairs through outEnd and *outEndCount; a destroyed shape is -1.
ME_API int me_contact_events( MeWorld* world, int* outBegin, float* outNormals, int maxBegin, int* outEnd, int maxEnd,
	int* outEndCount );

#ifdef __cplusplus
}
#endif
#endif

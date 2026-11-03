// ARCH-1 P6: a flat C layer over Box2D v3, shaped after the dossier's IPhysicsWorld2D common contract.
//
// Every function takes and returns only int, float, uint64 and pointers to caller-owned buffers: no struct passed or
// returned by value, no 64-bit enum, no function pointer crosses the boundary. That keeps the P/Invoke signatures the
// .NET WebAssembly interpreter handles (ARCH-1 P4 found it mishandles 64-bit enum parameters and function pointers)
// and keeps the C# side free of Box2D's struct layouts, which change between Box2D versions.
//
// Bodies and shapes are addressed by small integer handles (indices into tables the world owns); a shape's user data
// is its handle, so query and event results come back as handles.
#ifndef ME_BOX2D_H
#define ME_BOX2D_H

#include <stdint.h>

#if defined(_WIN32)
#define ME_API __declspec(dllexport)
#else
#define ME_API __attribute__((visibility("default")))
#endif

#ifdef __cplusplus
extern "C" {
#endif

typedef struct MeWorld MeWorld;

enum MeBodyType { ME_STATIC = 0, ME_KINEMATIC = 1, ME_DYNAMIC = 2 };

enum MeShapeFlags
{
	ME_SENSOR = 1,          // isSensor
	ME_SENSOR_EVENTS = 2,   // enableSensorEvents (both shapes need it)
	ME_CONTACT_EVENTS = 4,  // enableContactEvents
	ME_ONE_WAY = 8,         // one-way platform through the pre-solve hook (dynamic bodies only)
};

ME_API int me_version(void);  // major*10000 + minor*100 + revision, from b2GetVersion
ME_API MeWorld* me_world_create(float gravityX, float gravityY, int enableSleep);
ME_API void me_world_destroy(MeWorld* world);
ME_API void me_step(MeWorld* world, float timeStep, int subSteps);

ME_API int me_body_create(MeWorld* world, int type, float x, float y, float angle);
ME_API void me_body_transform(MeWorld* world, int body, float* xyCosSin4);
ME_API void me_body_set_position(MeWorld* world, int body, float x, float y);  // teleport (b2Body_SetTransform)
// Kinematic: velocity that reaches the target within timeStep (b2Body_SetTargetTransform), so it pushes dynamics.
ME_API void me_body_set_target(MeWorld* world, int body, float x, float y, float timeStep);
ME_API void me_body_set_velocity(MeWorld* world, int body, float vx, float vy);
// x, y, cos, sin of every body in creation order; returns the body count.
ME_API int me_bodies_read(MeWorld* world, float* out, int maxBodies);

ME_API int me_shape_box(MeWorld* world, int body, float halfWidth, float halfHeight, float centerX, float centerY,
	uint64_t category, uint64_t mask, int flags, float density, float friction);
ME_API int me_shape_capsule(MeWorld* world, int body, float x1, float y1, float x2, float y2, float radius,
	uint64_t category, uint64_t mask, int flags, float density, float friction);
ME_API int me_shape_body(MeWorld* world, int shape);

// Closest ray hit: returns the shape handle or -1; out5 = point x, y, normal x, y, fraction.
ME_API int me_ray_cast(MeWorld* world, float x, float y, float dx, float dy, uint64_t category, uint64_t mask,
	float* out5);
// Exact overlap of an axis-aligned box (b2World_OverlapShape with a box proxy): shape handles, returns the count.
ME_API int me_overlap_box(MeWorld* world, float minX, float minY, float maxX, float maxY, uint64_t category,
	uint64_t mask, int* outShapes, int maxShapes);
// Broad-phase overlap (b2World_OverlapAABB): shapes whose *fat* AABB overlaps; may include non-touching shapes.
ME_API int me_overlap_aabb_broad(MeWorld* world, float minX, float minY, float maxX, float maxY, uint64_t category,
	uint64_t mask, int* outShapes, int maxShapes);
// Closest box cast: returns the shape handle or -1; out3 = fraction, normal x, y.
ME_API int me_cast_box(MeWorld* world, float centerX, float centerY, float halfWidth, float halfHeight, float dx,
	float dy, uint64_t category, uint64_t mask, float* out3);
// Capsule character move with Box2D's mover API (b2World_CollideMover + b2SolvePlanes + b2World_CastMover), the
// algorithm of Box2D's "Mover" sample. The capsule is (x1,y1)-(x2,y2) relative to the body origin. Moves the kinematic
// body (teleport), writes the new origin to outXY2 and the collision planes (normal x, y, offset) to outPlanes; returns
// the plane count; plane shapes go to outShapes. Shapes whose category is in oneWayCategory are one-way platforms for
// the mover: left out of the queries while moving up, and kept only as floors (plane normal pointing up) otherwise.
ME_API int me_move_capsule(MeWorld* world, int body, float x1, float y1, float x2, float y2, float radius, float dx,
	float dy, uint64_t category, uint64_t mask, uint64_t oneWayCategory, float* outXY2, float* outPlanes,
	int* outShapes, int maxPlanes);

// Buffered events of the last step. Pairs of shape handles (sensor, visitor) / (a, b); contact normals point from a
// to b. Each returns its count; the second count goes through *outCount2.
ME_API int me_sensor_events(MeWorld* world, int* outBegin, int maxBegin, int* outEnd, int maxEnd, int* outEndCount);
ME_API int me_contact_begin_events(MeWorld* world, int* outPairs, float* outNormals, int maxEvents);

// bodies, awake bodies, contacts (b2World_GetCounters, b2World_GetAwakeBodyCount).
ME_API void me_counters(MeWorld* world, int* out3);

// Number of pre-solve callbacks run and contacts disabled by the one-way rule since the world was created.
ME_API void me_one_way_stats(MeWorld* world, int* calls, int* disabled);

#ifdef __cplusplus
}
#endif
#endif

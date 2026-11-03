// ARCH-1 P6: implementation of me_box2d.h over Box2D v3 (see that header for the shape of the API).
//
// Builds against two Box2D API generations:
//   - ME_ORIGIN_API defined: Box2D v3.2.0 (large-world API: queries take a b2Pos origin, b2Pos positions,
//     pre-solve receives the manifold).
//   - undefined: Box2D before upstream PR #1066 (for example f2086ed6, the commit Box2D.NET's main branch ports).
#include "me_box2d.h"

#include "box2d/box2d.h"

#include <float.h>
#include <stdlib.h>
#include <string.h>

#ifdef ME_ORIGIN_API
#define ME_ORIGIN ( b2Pos ){ 0.0f, 0.0f },
#define ME_PLANE_DELTA delta
#else
#define ME_ORIGIN
#define ME_PLANE_DELTA translation
#endif

struct MeWorld
{
	b2WorldId id;
	b2BodyId* bodies;
	int bodyCount, bodyCapacity;
	b2ShapeId* shapes;
	int* shapeFlags;
	int shapeCount, shapeCapacity;
	int preSolveCalls, preSolveDisabled;
};

static void* grow( void* p, int* capacity, int needed, size_t size )
{
	if ( needed <= *capacity )
		return p;
	int c = *capacity < 16 ? 16 : *capacity;
	while ( c < needed )
		c *= 2;
	*capacity = c;
	return realloc( p, (size_t)c * size );
}

static int shape_handle( b2ShapeId id )
{
	if ( b2Shape_IsValid( id ) == false )
		return -1;
	return (int)(intptr_t)b2Shape_GetUserData( id ) - 1;
}

static b2Filter make_filter( uint64_t category, uint64_t mask )
{
	b2Filter f = b2DefaultFilter();
	f.categoryBits = category;
	f.maskBits = mask;
	return f;
}

static b2QueryFilter make_query( uint64_t category, uint64_t mask )
{
	b2QueryFilter f = { category, mask };
	return f;
}

// One-way platforms: the contact stays enabled only when the other shape is above the platform (the normal from
// the platform to the other shape points up). The upstream "Platformer" sample's rule.
static int one_way_enable( MeWorld* w, b2ShapeId a, b2ShapeId b, b2Vec2 normalAtoB )
{
	int ha = shape_handle( a ), hb = shape_handle( b );
	float sign;
	if ( ha >= 0 && ( w->shapeFlags[ha] & ME_ONE_WAY ) )
		sign = 1.0f;
	else if ( hb >= 0 && ( w->shapeFlags[hb] & ME_ONE_WAY ) )
		sign = -1.0f;
	else
		return 1;
	w->preSolveCalls += 1;
	if ( sign * normalAtoB.y > 0.7f )
		return 1;
	w->preSolveDisabled += 1;
	return 0;
}

#ifdef ME_ORIGIN_API
static void pre_solve( b2ShapeId a, b2ShapeId b, b2Manifold* manifold, void* context )
{
	if ( one_way_enable( (MeWorld*)context, a, b, manifold->normal ) == 0 )
		manifold->pointCount = 0;
}

// v3.2.0 also asks before a continuous (time of impact) event; the same rule applies.
static bool pre_continuous( b2ShapeId a, b2ShapeId b, b2Pos point, b2Vec2 normal, void* context )
{
	(void)point;
	return one_way_enable( (MeWorld*)context, a, b, normal ) != 0;
}
#else
static bool pre_solve( b2ShapeId a, b2ShapeId b, b2Vec2 point, b2Vec2 normal, void* context )
{
	(void)point;
	return one_way_enable( (MeWorld*)context, a, b, normal ) != 0;
}
#endif

ME_API int me_version( void )
{
	b2Version v = b2GetVersion();
	return v.major * 10000 + v.minor * 100 + v.revision;
}

ME_API MeWorld* me_world_create( float gravityX, float gravityY, int enableSleep )
{
	MeWorld* w = (MeWorld*)calloc( 1, sizeof( MeWorld ) );
	b2WorldDef def = b2DefaultWorldDef();
	def.gravity = ( b2Vec2 ){ gravityX, gravityY };
	def.enableSleep = enableSleep != 0;
	w->id = b2CreateWorld( &def );
#ifdef ME_ORIGIN_API
	b2World_SetPreSolveCallback( w->id, pre_solve, pre_continuous, w );
#else
	b2World_SetPreSolveCallback( w->id, pre_solve, w );
#endif
	return w;
}

ME_API void me_world_destroy( MeWorld* w )
{
	b2DestroyWorld( w->id );
	free( w->bodies );
	free( w->shapes );
	free( w->shapeFlags );
	free( w );
}

ME_API void me_step( MeWorld* w, float timeStep, int subSteps )
{
	b2World_Step( w->id, timeStep, subSteps );
}

ME_API int me_body_create( MeWorld* w, int type, float x, float y, float angle )
{
	b2BodyDef def = b2DefaultBodyDef();
	def.type = type == ME_STATIC ? b2_staticBody : type == ME_KINEMATIC ? b2_kinematicBody : b2_dynamicBody;
	def.position.x = x;
	def.position.y = y;
	def.rotation = b2MakeRot( angle );
	w->bodies = (b2BodyId*)grow( w->bodies, &w->bodyCapacity, w->bodyCount + 1, sizeof( b2BodyId ) );
	w->bodies[w->bodyCount] = b2CreateBody( w->id, &def );
	return w->bodyCount++;
}

ME_API void me_body_transform( MeWorld* w, int body, float* out )
{
	b2BodyId id = w->bodies[body];
	b2Rot q = b2Body_GetRotation( id );
#ifdef ME_ORIGIN_API
	b2Pos p = b2Body_GetPosition( id );
#else
	b2Vec2 p = b2Body_GetPosition( id );
#endif
	out[0] = (float)p.x;
	out[1] = (float)p.y;
	out[2] = q.c;
	out[3] = q.s;
}

ME_API void me_body_set_position( MeWorld* w, int body, float x, float y )
{
	b2BodyId id = w->bodies[body];
#ifdef ME_ORIGIN_API
	b2Body_SetTransform( id, ( b2Pos ){ x, y }, b2Body_GetRotation( id ) );
#else
	b2Body_SetTransform( id, ( b2Vec2 ){ x, y }, b2Body_GetRotation( id ) );
#endif
}

ME_API void me_body_set_target( MeWorld* w, int body, float x, float y, float timeStep )
{
	b2BodyId id = w->bodies[body];
#ifdef ME_ORIGIN_API
	b2WorldTransform t = { { x, y }, b2Body_GetRotation( id ) };
#else
	b2Transform t = { { x, y }, b2Body_GetRotation( id ) };
#endif
	b2Body_SetTargetTransform( id, t, timeStep, true );
}

ME_API void me_body_set_velocity( MeWorld* w, int body, float vx, float vy )
{
	b2Body_SetLinearVelocity( w->bodies[body], ( b2Vec2 ){ vx, vy } );
}

ME_API int me_bodies_read( MeWorld* w, float* out, int maxBodies )
{
	int n = w->bodyCount < maxBodies ? w->bodyCount : maxBodies;
	for ( int i = 0; i < n; ++i )
		me_body_transform( w, i, out + 4 * i );
	return n;
}

static int add_shape( MeWorld* w, b2ShapeId id, int flags )
{
	int need = w->shapeCount + 1;
	int cap = w->shapeCapacity;
	w->shapes = (b2ShapeId*)grow( w->shapes, &cap, need, sizeof( b2ShapeId ) );
	cap = w->shapeCapacity;
	w->shapeFlags = (int*)grow( w->shapeFlags, &cap, need, sizeof( int ) );
	w->shapeCapacity = cap;
	w->shapes[w->shapeCount] = id;
	w->shapeFlags[w->shapeCount] = flags;
	b2Shape_SetUserData( id, (void*)(intptr_t)( w->shapeCount + 1 ) );
	return w->shapeCount++;
}

static b2ShapeDef shape_def( uint64_t category, uint64_t mask, int flags, float density, float friction )
{
	b2ShapeDef def = b2DefaultShapeDef();
	def.filter = make_filter( category, mask );
	def.isSensor = ( flags & ME_SENSOR ) != 0;
	def.enableSensorEvents = ( flags & ME_SENSOR_EVENTS ) != 0;
	def.enableContactEvents = ( flags & ME_CONTACT_EVENTS ) != 0;
	def.enablePreSolveEvents = ( flags & ME_ONE_WAY ) != 0;
	def.density = density;
	def.material.friction = friction;
	return def;
}

ME_API int me_shape_box( MeWorld* w, int body, float hw, float hh, float cx, float cy, uint64_t category,
						 uint64_t mask, int flags, float density, float friction )
{
	b2ShapeDef def = shape_def( category, mask, flags, density, friction );
	b2Polygon box = b2MakeOffsetBox( hw, hh, ( b2Vec2 ){ cx, cy }, b2Rot_identity );
	return add_shape( w, b2CreatePolygonShape( w->bodies[body], &def, &box ), flags );
}

ME_API int me_shape_capsule( MeWorld* w, int body, float x1, float y1, float x2, float y2, float radius,
							 uint64_t category, uint64_t mask, int flags, float density, float friction )
{
	b2ShapeDef def = shape_def( category, mask, flags, density, friction );
	b2Capsule c = { { x1, y1 }, { x2, y2 }, radius };
	return add_shape( w, b2CreateCapsuleShape( w->bodies[body], &def, &c ), flags );
}

ME_API int me_shape_body( MeWorld* w, int shape )
{
	b2BodyId b = b2Shape_GetBody( w->shapes[shape] );
	for ( int i = 0; i < w->bodyCount; ++i )
		if ( B2_ID_EQUALS( w->bodies[i], b ) )
			return i;
	return -1;
}

ME_API int me_ray_cast( MeWorld* w, float x, float y, float dx, float dy, uint64_t category, uint64_t mask,
						float* out5 )
{
	b2RayResult r = b2World_CastRayClosest( w->id, ( b2Vec2 ){ x, y }, ( b2Vec2 ){ dx, dy }, make_query( category, mask ) );
	if ( r.hit == false )
		return -1;
	out5[0] = (float)r.point.x;
	out5[1] = (float)r.point.y;
	out5[2] = r.normal.x;
	out5[3] = r.normal.y;
	out5[4] = r.fraction;
	return shape_handle( r.shapeId );
}

typedef struct
{
	int* out;
	int max, count;
} Collect;

static bool collect_shape( b2ShapeId id, void* context )
{
	Collect* c = (Collect*)context;
	if ( c->count < c->max )
		c->out[c->count] = shape_handle( id );
	c->count += 1;
	return true;
}

ME_API int me_overlap_box( MeWorld* w, float minX, float minY, float maxX, float maxY, uint64_t category,
						   uint64_t mask, int* outShapes, int maxShapes )
{
	b2Vec2 pts[4] = { { minX, minY }, { maxX, minY }, { maxX, maxY }, { minX, maxY } };
	b2ShapeProxy proxy = b2MakeProxy( pts, 4, 0.0f );
	Collect c = { outShapes, maxShapes, 0 };
	b2World_OverlapShape( w->id, ME_ORIGIN & proxy, make_query( category, mask ), collect_shape, &c );
	return c.count;
}

ME_API int me_overlap_aabb_broad( MeWorld* w, float minX, float minY, float maxX, float maxY, uint64_t category,
								  uint64_t mask, int* outShapes, int maxShapes )
{
	b2AABB box = { { minX, minY }, { maxX, maxY } };
	Collect c = { outShapes, maxShapes, 0 };
	b2World_OverlapAABB( w->id, ME_ORIGIN box, make_query( category, mask ), collect_shape, &c );
	return c.count;
}

typedef struct
{
	b2ShapeId shape;
	float fraction;
	b2Vec2 normal;
} CastHit;

#ifdef ME_ORIGIN_API
static float closest_cast( b2ShapeId id, b2Pos point, b2Vec2 normal, float fraction, void* context )
#else
static float closest_cast( b2ShapeId id, b2Vec2 point, b2Vec2 normal, float fraction, void* context )
#endif
{
	(void)point;
	CastHit* h = (CastHit*)context;
	h->shape = id;
	h->fraction = fraction;
	h->normal = normal;
	return fraction;
}

ME_API int me_cast_box( MeWorld* w, float cx, float cy, float hw, float hh, float dx, float dy, uint64_t category,
						uint64_t mask, float* out3 )
{
	b2Vec2 pts[4] = { { cx - hw, cy - hh }, { cx + hw, cy - hh }, { cx + hw, cy + hh }, { cx - hw, cy + hh } };
	b2ShapeProxy proxy = b2MakeProxy( pts, 4, 0.0f );
	CastHit h = { b2_nullShapeId, 1.0f, { 0.0f, 0.0f } };
	b2World_CastShape( w->id, ME_ORIGIN & proxy, ( b2Vec2 ){ dx, dy }, make_query( category, mask ), closest_cast, &h );
	out3[0] = h.fraction;
	out3[1] = h.normal.x;
	out3[2] = h.normal.y;
	return B2_IS_NULL( h.shape ) ? -1 : shape_handle( h.shape );
}

#define ME_MAX_PLANES 16

typedef struct
{
	b2CollisionPlane planes[ME_MAX_PLANES];
	int shapes[ME_MAX_PLANES];
	int count;
	uint64_t oneWayCategory;
} PlaneSet;

static bool collect_plane( b2ShapeId id, const b2PlaneResult* result, void* context )
{
	PlaneSet* s = (PlaneSet*)context;
	if ( ( b2Shape_GetFilter( id ).categoryBits & s->oneWayCategory ) != 0 && result->plane.normal.y < 0.7f )
		return true;
	if ( s->count < ME_MAX_PLANES )
	{
		b2CollisionPlane p = { result->plane, FLT_MAX, 0.0f, true };
		s->planes[s->count] = p;
		s->shapes[s->count] = shape_handle( id );
		s->count += 1;
	}
	return true;
}

ME_API int me_move_capsule( MeWorld* w, int body, float x1, float y1, float x2, float y2, float radius, float dx,
							float dy, uint64_t category, uint64_t mask, uint64_t oneWayCategory, float* outXY2,
							float* outPlanes, int* outShapes, int maxPlanes )
{
	float t[4];
	me_body_transform( w, body, t );
	b2Vec2 p = { t[0], t[1] };
	b2Vec2 target = { p.x + dx, p.y + dy };
	// Moving up: one-way shapes are not obstacles at all.
	b2QueryFilter filter = make_query( category, dy > 0.0f ? ( mask & ~oneWayCategory ) : mask );
	PlaneSet set;
	set.oneWayCategory = oneWayCategory;
	int reported = 0;
	const float tolerance = 0.01f;
	for ( int iteration = 0; iteration < 5; ++iteration )
	{
		set.count = 0;
		b2Capsule mover = { { p.x + x1, p.y + y1 }, { p.x + x2, p.y + y2 }, radius };
		b2World_CollideMover( w->id, ME_ORIGIN & mover, filter, collect_plane, &set );
		b2PlaneSolverResult result = b2SolvePlanes( b2Sub( target, p ), set.planes, set.count );
		float fraction = b2World_CastMover( w->id, ME_ORIGIN & mover, result.ME_PLANE_DELTA, filter );
		b2Vec2 delta = b2MulSV( fraction, result.ME_PLANE_DELTA );
		p = b2Add( p, delta );
		// Report the planes touched in any iteration, once per shape.
		for ( int i = 0; i < set.count; ++i )
		{
			int seen = 0;
			for ( int j = 0; j < reported; ++j )
				seen |= outShapes[j] == set.shapes[i];
			if ( seen == 0 && reported < maxPlanes )
			{
				outPlanes[3 * reported] = set.planes[i].plane.normal.x;
				outPlanes[3 * reported + 1] = set.planes[i].plane.normal.y;
				outPlanes[3 * reported + 2] = set.planes[i].plane.offset;
				outShapes[reported] = set.shapes[i];
				reported += 1;
			}
		}
		if ( b2LengthSquared( delta ) < tolerance * tolerance )
			break;
	}
	me_body_set_position( w, body, p.x, p.y );
	outXY2[0] = p.x;
	outXY2[1] = p.y;
	return reported;
}

ME_API int me_sensor_events( MeWorld* w, int* outBegin, int maxBegin, int* outEnd, int maxEnd, int* outEndCount )
{
	b2SensorEvents e = b2World_GetSensorEvents( w->id );
	int nb = e.beginCount < maxBegin ? e.beginCount : maxBegin;
	for ( int i = 0; i < nb; ++i )
	{
		outBegin[2 * i] = shape_handle( e.beginEvents[i].sensorShapeId );
		outBegin[2 * i + 1] = shape_handle( e.beginEvents[i].visitorShapeId );
	}
	int ne = e.endCount < maxEnd ? e.endCount : maxEnd;
	for ( int i = 0; i < ne; ++i )
	{
		outEnd[2 * i] = shape_handle( e.endEvents[i].sensorShapeId );
		outEnd[2 * i + 1] = shape_handle( e.endEvents[i].visitorShapeId );
	}
	*outEndCount = e.endCount;
	return e.beginCount;
}

ME_API int me_contact_begin_events( MeWorld* w, int* outPairs, float* outNormals, int maxEvents )
{
	b2ContactEvents e = b2World_GetContactEvents( w->id );
	int n = e.beginCount < maxEvents ? e.beginCount : maxEvents;
	for ( int i = 0; i < n; ++i )
	{
		const b2ContactBeginTouchEvent* ev = e.beginEvents + i;
		outPairs[2 * i] = shape_handle( ev->shapeIdA );
		outPairs[2 * i + 1] = shape_handle( ev->shapeIdB );
		b2ContactData d = b2Contact_GetData( ev->contactId );
		outNormals[2 * i] = d.manifold.normal.x;
		outNormals[2 * i + 1] = d.manifold.normal.y;
	}
	return e.beginCount;
}

ME_API void me_counters( MeWorld* w, int* out3 )
{
	b2Counters c = b2World_GetCounters( w->id );
	out3[0] = c.bodyCount;
	out3[1] = b2World_GetAwakeBodyCount( w->id );
	out3[2] = c.contactCount;
}

ME_API void me_one_way_stats( MeWorld* w, int* calls, int* disabled )
{
	*calls = w->preSolveCalls;
	*disabled = w->preSolveDisabled;
}

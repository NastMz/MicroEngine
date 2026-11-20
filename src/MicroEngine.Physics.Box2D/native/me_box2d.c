// me_box2d: implementation of me_box2d.h over Box2D v3.2 (box2d.env). See the header for the rules of the layer.
#include "me_box2d.h"

#include "box2d/box2d.h"

#include <float.h>
#include <math.h>
#include <stdlib.h>
#include <string.h>

// Distances of the box mover, in metres. Box2D's shape cast stops at B2_LINEAR_SLOP from a surface and reports any
// shape closer than that at the start as an initial overlap (fraction 0, no normal), so a box resting at the slop
// could never slide along a floor (ARCH-1 P6). The mover therefore backs off one more slop after each hit: a box rests
// about two slops from what stopped it, and never closer than it started.
#define ME_SLOP B2_LINEAR_SLOP
#define ME_BACKOFF ( 1.0f * ME_SLOP )
#define ME_SKIN ( 3.0f * ME_SLOP )
// The contact probe at the end of a move: a surface within ME_SKIN of the shape counts as touching it.
#define ME_PROBE ( ME_SKIN - ME_SLOP )
// Bounds comparisons: shapes closer than this along an axis count as touching.
#define ME_EPS ( 1e-4f )
// Overlap queries shrink their box by this much so that shapes sharing an edge with it do not count: Box2D's overlap
// test accepts a distance up to a tenth of the slop.
#define ME_TOUCH ( 0.1f * ME_SLOP + 1e-4f )
#define ME_MAX_PLANES 16
#define ME_MOVER_ITERATIONS 5

typedef struct
{
	b2BodyId id;
	int alive;
	int type;
} MeBody;

typedef struct
{
	b2ShapeId id;
	int alive;
	int body;
	int kind;
	int flags;
} MeShape;

typedef struct
{
	b2JointId id;
	int alive;
	int kind;
	int bodyA, bodyB; // bodyA is -1 for a target joint
} MeJoint;

struct MeWorld
{
	b2WorldId id;
	int subSteps;
	MeBody* bodies;
	int bodyCount, bodyCapacity;
	MeShape* shapes;
	int shapeCount, shapeCapacity;
	int* scratch;
	int scratchCapacity;
	int hasOneWay;
	MeJoint* joints;
	int jointCount, jointCapacity;
	b2BodyId ground; // the static body target joints pull from, created with the first one
	int hasGround;
};

static void* grow( void* p, int* capacity, int needed, size_t size )
{
	if ( needed <= *capacity )
		return p;
	int c = *capacity < 16 ? 16 : *capacity;
	while ( c < needed )
		c *= 2;
	void* q = realloc( p, (size_t)c * size );
	if ( q == NULL )
		abort();
	*capacity = c;
	return q;
}

static int shape_handle( b2ShapeId id )
{
	if ( B2_IS_NULL( id ) || b2Shape_IsValid( id ) == false )
		return -1;
	return (int)(intptr_t)b2Shape_GetUserData( id ) - 1;
}

static b2QueryFilter make_query( uint64_t category, uint64_t mask )
{
	b2QueryFilter f = { category, mask };
	return f;
}

static int is_live_shape( MeWorld* w, int shape )
{
	return shape >= 0 && shape < w->shapeCount && w->shapes[shape].alive;
}

static int is_live_body( MeWorld* w, int body )
{
	return body >= 0 && body < w->bodyCount && w->bodies[body].alive;
}

// ---------------------------------------------------------------------------------------------------- one-way rule

// A contact with a one-way platform stays enabled only when the other shape is on its top: the normal from the
// platform to the other shape points up. Runs inside the step (pre-solve), never in managed code. The sign is +1 when
// the platform is shape A (the manifold normal points from A to B), -1 when it is B, 0 when neither is one-way.
static int one_way_sign( MeWorld* w, b2ShapeId a, b2ShapeId b )
{
	int ha = shape_handle( a ), hb = shape_handle( b );
	if ( ha >= 0 && ( w->shapes[ha].flags & ME_ONE_WAY ) )
		return 1;
	if ( hb >= 0 && ( w->shapes[hb].flags & ME_ONE_WAY ) )
		return -1;
	return 0;
}

static void pre_solve( b2ShapeId a, b2ShapeId b, b2Manifold* manifold, void* context )
{
	MeWorld* w = (MeWorld*)context;
	int sign = one_way_sign( w, a, b );
	if ( sign != 0 && (float)sign * manifold->normal.y <= 0.7f )
		manifold->pointCount = 0;
}

static bool pre_continuous( b2ShapeId a, b2ShapeId b, b2Pos point, b2Vec2 normal, void* context )
{
	(void)point;
	MeWorld* w = (MeWorld*)context;
	int sign = one_way_sign( w, a, b );
	return sign == 0 || (float)sign * normal.y > 0.7f;
}

// ---------------------------------------------------------------------------------------------------- world

ME_API int me_api_version( void )
{
	return ME_API_VERSION;
}

ME_API int me_box2d_version( void )
{
	b2Version v = b2GetVersion();
	return v.major * 10000 + v.minor * 100 + v.revision;
}

ME_API float me_move_skin( void )
{
	return ME_SKIN;
}

ME_API MeWorld* me_world_create( float gravityX, float gravityY, int subSteps, int enableSleep )
{
	MeWorld* w = (MeWorld*)calloc( 1, sizeof( MeWorld ) );
	if ( w == NULL )
		return NULL;
	b2WorldDef def = b2DefaultWorldDef();
	def.gravity = ( b2Vec2 ){ gravityX, gravityY };
	def.enableSleep = enableSleep != 0;
	w->id = b2CreateWorld( &def );
	if ( B2_IS_NULL( w->id ) )
	{
		// Box2D keeps at most B2_MAX_WORLDS (128) worlds at a time.
		free( w );
		return NULL;
	}
	w->subSteps = subSteps < 1 ? 1 : subSteps;
	b2World_SetPreSolveCallback( w->id, pre_solve, pre_continuous, w );
	return w;
}

ME_API void me_world_destroy( MeWorld* w )
{
	if ( w == NULL )
		return;
	b2DestroyWorld( w->id );
	free( w->bodies );
	free( w->shapes );
	free( w->scratch );
	free( w->joints );
	free( w );
}

ME_API void me_world_step( MeWorld* w, float timeStep )
{
	b2World_Step( w->id, timeStep, w->subSteps );
}

ME_API void me_world_counters( MeWorld* w, int* out4 )
{
	b2Counters c = b2World_GetCounters( w->id );
	out4[0] = c.bodyCount;
	out4[1] = c.shapeCount;
	out4[2] = c.contactCount;
	out4[3] = b2World_GetAwakeBodyCount( w->id );
}

// ---------------------------------------------------------------------------------------------------- bodies

ME_API int me_body_create( MeWorld* w, int type, float x, float y, float angle, int bullet )
{
	b2BodyDef def = b2DefaultBodyDef();
	def.type = type == ME_STATIC ? b2_staticBody : type == ME_KINEMATIC ? b2_kinematicBody : b2_dynamicBody;
	def.position = ( b2Pos ){ x, y };
	def.rotation = b2MakeRot( angle );
	def.isBullet = bullet != 0 && type == ME_DYNAMIC;
	w->bodies = (MeBody*)grow( w->bodies, &w->bodyCapacity, w->bodyCount + 1, sizeof( MeBody ) );
	MeBody* b = w->bodies + w->bodyCount;
	b->id = b2CreateBody( w->id, &def );
	b->alive = 1;
	b->type = type;
	b2Body_SetUserData( b->id, (void*)(intptr_t)( w->bodyCount + 1 ) );
	return w->bodyCount++;
}

ME_API void me_body_destroy( MeWorld* w, int body )
{
	if ( is_live_body( w, body ) == 0 )
		return;
	for ( int i = 0; i < w->shapeCount; ++i )
		if ( w->shapes[i].alive && w->shapes[i].body == body )
			w->shapes[i].alive = 0;
	for ( int i = 0; i < w->jointCount; ++i )
		if ( w->joints[i].alive && ( w->joints[i].bodyA == body || w->joints[i].bodyB == body ) )
			w->joints[i].alive = 0; // Box2D destroys a body's joints with it
	b2DestroyBody( w->bodies[body].id );
	w->bodies[body].alive = 0;
}

ME_API void me_body_state( MeWorld* w, int body, float* out6 )
{
	b2BodyId id = w->bodies[body].id;
	b2Pos p = b2Body_GetPosition( id );
	b2Vec2 v = b2Body_GetLinearVelocity( id );
	out6[0] = (float)p.x;
	out6[1] = (float)p.y;
	out6[2] = b2Rot_GetAngle( b2Body_GetRotation( id ) );
	out6[3] = v.x;
	out6[4] = v.y;
	out6[5] = b2Body_GetAngularVelocity( id );
}

ME_API void me_body_set_position( MeWorld* w, int body, float x, float y )
{
	b2BodyId id = w->bodies[body].id;
	b2Body_SetTransform( id, ( b2Pos ){ x, y }, b2Body_GetRotation( id ) );
}

ME_API void me_body_set_velocity( MeWorld* w, int body, float vx, float vy )
{
	b2Body_SetLinearVelocity( w->bodies[body].id, ( b2Vec2 ){ vx, vy } );
}

ME_API float me_body_mass( MeWorld* w, int body )
{
	return b2Body_GetMass( w->bodies[body].id );
}

ME_API void me_body_set_rotation( MeWorld* w, int body, float angle )
{
	b2BodyId id = w->bodies[body].id;
	b2Body_SetTransform( id, b2Body_GetPosition( id ), b2MakeRot( angle ) );
}

ME_API void me_body_set_angular_velocity( MeWorld* w, int body, float angularVelocity )
{
	b2Body_SetAngularVelocity( w->bodies[body].id, angularVelocity );
}

ME_API void me_body_apply( MeWorld* w, int body, int kind, float x, float y, float px, float py, int atPoint, int wake )
{
	b2BodyId id = w->bodies[body].id;
	b2Vec2 v = { x, y };
	b2Pos point = { px, py };
	if ( kind == 0 )
	{
		if ( atPoint )
			b2Body_ApplyForce( id, v, point, wake != 0 );
		else
			b2Body_ApplyForceToCenter( id, v, wake != 0 );
	}
	else
	{
		if ( atPoint )
			b2Body_ApplyLinearImpulse( id, v, point, wake != 0 );
		else
			b2Body_ApplyLinearImpulseToCenter( id, v, wake != 0 );
	}
}

ME_API void me_body_apply_angular( MeWorld* w, int body, int kind, float value, int wake )
{
	b2BodyId id = w->bodies[body].id;
	if ( kind == 0 )
		b2Body_ApplyTorque( id, value, wake != 0 );
	else
		b2Body_ApplyAngularImpulse( id, value, wake != 0 );
}

ME_API void me_body_get_dynamics( MeWorld* w, int body, float* out5 )
{
	b2BodyId id = w->bodies[body].id;
	out5[0] = b2Body_GetLinearDamping( id );
	out5[1] = b2Body_GetAngularDamping( id );
	out5[2] = b2Body_GetGravityScale( id );
	out5[3] = b2Body_GetMotionLocks( id ).angularZ ? 1.0f : 0.0f;
	out5[4] = b2Body_IsSleepingEnabled( id ) ? 1.0f : 0.0f;
}

ME_API void me_body_set_dynamics( MeWorld* w, int body, const float* in5 )
{
	b2BodyId id = w->bodies[body].id;
	b2Body_SetLinearDamping( id, in5[0] );
	b2Body_SetAngularDamping( id, in5[1] );
	b2Body_SetGravityScale( id, in5[2] );
	b2MotionLocks locks = b2Body_GetMotionLocks( id );
	if ( locks.angularZ != ( in5[3] != 0.0f ) )
	{
		locks.angularZ = in5[3] != 0.0f;
		b2Body_SetMotionLocks( id, locks );
	}
	if ( b2Body_IsSleepingEnabled( id ) != ( in5[4] != 0.0f ) )
		b2Body_EnableSleeping( id, in5[4] != 0.0f );
}

ME_API int me_body_is_awake( MeWorld* w, int body )
{
	return b2Body_IsAwake( w->bodies[body].id ) ? 1 : 0;
}

ME_API void me_body_set_awake( MeWorld* w, int body, int awake )
{
	b2Body_SetAwake( w->bodies[body].id, awake != 0 );
}

ME_API void me_body_mass_data( MeWorld* w, int body, float* out4 )
{
	b2BodyId id = w->bodies[body].id;
	b2Pos c = b2Body_GetWorldCenter( id );
	out4[0] = b2Body_GetMass( id );
	out4[1] = (float)c.x;
	out4[2] = (float)c.y;
	out4[3] = b2Body_GetRotationalInertia( id );
}

// ---------------------------------------------------------------------------------------------------- shapes

ME_API int me_shape_create( MeWorld* w, int body, int kind, const float* g, int pointCount, float radius,
	uint64_t category, uint64_t mask, int flags, float density, float friction, float restitution )
{
	if ( is_live_body( w, body ) == 0 )
		return -1;
	MeBody* b = w->bodies + body;
	int dynamic = b->type == ME_DYNAMIC;
	b2ShapeDef def = b2DefaultShapeDef();
	def.filter.categoryBits = category;
	def.filter.maskBits = mask;
	def.isSensor = ( flags & ME_SENSOR ) != 0;
	// Sensors always report; a visitor reports when the game left SensorEvents on (both sides need the flag).
	def.enableSensorEvents = def.isSensor || ( flags & ME_SENSOR_EVENTS ) != 0;
	// Contact events come only from pairs with a dynamic body, and Box2D needs the flag on one shape of the pair.
	def.enableContactEvents = dynamic && def.isSensor == false;
	def.enablePreSolveEvents = dynamic && w->hasOneWay;
	if ( density >= 0.0f )
		def.density = density;
	if ( friction >= 0.0f )
		def.material.friction = friction;
	if ( restitution >= 0.0f )
		def.material.restitution = restitution;

	b2ShapeId id;
	switch ( kind )
	{
		case ME_BOX:
		{
			if ( !( g[0] > 0.0f && g[1] > 0.0f ) )
				return -1;
			b2Polygon box = b2MakeOffsetBox( g[0], g[1], ( b2Vec2 ){ g[2], g[3] }, b2Rot_identity );
			id = b2CreatePolygonShape( b->id, &def, &box );
			break;
		}
		case ME_CIRCLE:
		{
			if ( !( radius > 0.0f ) )
				return -1;
			b2Circle circle = { { g[0], g[1] }, radius };
			id = b2CreateCircleShape( b->id, &def, &circle );
			break;
		}
		case ME_CAPSULE:
		{
			if ( !( radius > 0.0f ) )
				return -1;
			b2Capsule capsule = { { g[0], g[1] }, { g[2], g[3] }, radius };
			if ( b2Distance( capsule.center1, capsule.center2 ) < ME_SLOP )
			{
				// Box2D makes a circle of a capsule this short: record it as one, so it is read back as one.
				b2Circle circle = { b2Lerp( capsule.center1, capsule.center2, 0.5f ), radius };
				id = b2CreateCircleShape( b->id, &def, &circle );
				kind = ME_CIRCLE;
				break;
			}
			id = b2CreateCapsuleShape( b->id, &def, &capsule );
			break;
		}
		case ME_POLYGON:
		{
			if ( pointCount < 3 || pointCount > B2_MAX_POLYGON_VERTICES )
				return -1;
			b2Vec2 points[B2_MAX_POLYGON_VERTICES];
			for ( int i = 0; i < pointCount; ++i )
				points[i] = ( b2Vec2 ){ g[2 * i], g[2 * i + 1] };
			b2Hull hull = b2ComputeHull( points, pointCount );
			if ( hull.count == 0 )
				return -1;
			b2Polygon polygon = b2MakePolygon( &hull, 0.0f );
			id = b2CreatePolygonShape( b->id, &def, &polygon );
			break;
		}
		default:
			return -1;
	}

	w->shapes = (MeShape*)grow( w->shapes, &w->shapeCapacity, w->shapeCount + 1, sizeof( MeShape ) );
	MeShape* s = w->shapes + w->shapeCount;
	s->id = id;
	s->alive = 1;
	s->body = body;
	s->kind = kind;
	s->flags = flags;
	b2Shape_SetUserData( id, (void*)(intptr_t)( w->shapeCount + 1 ) );

	if ( ( flags & ME_ONE_WAY ) && w->hasOneWay == 0 )
	{
		// The first one-way platform: dynamic shapes created before it need the pre-solve rule too.
		w->hasOneWay = 1;
		for ( int i = 0; i < w->shapeCount; ++i )
		{
			MeShape* o = w->shapes + i;
			if ( o->alive && w->bodies[o->body].type == ME_DYNAMIC && ( o->flags & ME_SENSOR ) == 0 )
				b2Shape_EnablePreSolveEvents( o->id, true );
		}
	}
	return w->shapeCount++;
}

ME_API void me_shape_destroy( MeWorld* w, int shape )
{
	if ( is_live_shape( w, shape ) == 0 )
		return;
	b2DestroyShape( w->shapes[shape].id, true );
	w->shapes[shape].alive = 0;
}

// The shape's exact world bounds (b2Shape_GetBounds is inflated by Box2D's speculative distance).
static b2AABB exact_bounds( MeWorld* w, int shape )
{
	b2ShapeId id = w->shapes[shape].id;
	b2BodyId body = w->bodies[w->shapes[shape].body].id;
	b2WorldTransform xf = { b2Body_GetPosition( body ), b2Body_GetRotation( body ) };
	switch ( w->shapes[shape].kind )
	{
		case ME_CIRCLE:
		{
			b2Circle c = b2Shape_GetCircle( id );
			return b2ComputeCircleBounds( &c, xf );
		}
		case ME_CAPSULE:
		{
			b2Capsule c = b2Shape_GetCapsule( id );
			return b2ComputeCapsuleBounds( &c, xf );
		}
		default:
		{
			b2Polygon polygon = b2Shape_GetPolygon( id );
			return b2ComputePolygonBounds( &polygon, xf );
		}
	}
}

ME_API void me_shape_bounds( MeWorld* w, int shape, float* out4 )
{
	b2AABB box = exact_bounds( w, shape );
	out4[0] = box.lowerBound.x;
	out4[1] = box.lowerBound.y;
	out4[2] = box.upperBound.x;
	out4[3] = box.upperBound.y;
}

// ---------------------------------------------------------------------------------------------------- joints

static int is_live_joint( MeWorld* w, int joint )
{
	return joint >= 0 && joint < w->jointCount && w->joints[joint].alive;
}

// The frames of a joint: the anchors in each body, both turned to the same world rotation (body A's, or the axis's),
// so that the joint's angle and translation are 0 where it is created.
static void joint_frames( b2JointDef* base, b2BodyId a, b2BodyId b, const float* p, int flags, int axis )
{
	b2Rot qa = b2Body_GetRotation( a ), qb = b2Body_GetRotation( b );
	b2Rot frame = qa;
	if ( axis )
	{
		float length = sqrtf( p[4] * p[4] + p[5] * p[5] );
		b2Vec2 d = length > 0.0f ? ( b2Vec2 ){ p[4] / length, p[5] / length } : ( b2Vec2 ){ 1.0f, 0.0f };
		frame = b2MakeRotFromUnitVector( d );
	}
	base->bodyIdA = a;
	base->bodyIdB = b;
	base->localFrameA.p = b2Body_GetLocalPoint( a, ( b2Pos ){ p[0], p[1] } );
	base->localFrameA.q = b2InvMulRot( qa, frame );
	base->localFrameB.p = b2Body_GetLocalPoint( b, ( b2Pos ){ p[2], p[3] } );
	base->localFrameB.q = b2InvMulRot( qb, frame );
	base->collideConnected = ( flags & 1 ) != 0;
}

ME_API int me_joint_create( MeWorld* w, int kind, int bodyA, int bodyB, const float* p, int flags )
{
	if ( is_live_body( w, bodyB ) == 0 || ( kind != ME_TARGET && is_live_body( w, bodyA ) == 0 ) )
		return -1;
	b2BodyId b = w->bodies[bodyB].id, a;
	if ( kind == ME_TARGET )
	{
		if ( w->hasGround == 0 )
		{
			b2BodyDef def = b2DefaultBodyDef();
			w->ground = b2CreateBody( w->id, &def );
			w->hasGround = 1;
		}
		a = w->ground;
		bodyA = -1;
	}
	else
		a = w->bodies[bodyA].id;

	b2JointId id;
	switch ( kind )
	{
		case ME_REVOLUTE:
		{
			b2RevoluteJointDef d = b2DefaultRevoluteJointDef();
			joint_frames( &d.base, a, b, p, flags, 0 );
			d.enableLimit = p[6] != 0.0f;
			d.lowerAngle = p[7];
			d.upperAngle = p[8];
			d.enableMotor = p[9] != 0.0f;
			d.motorSpeed = p[10];
			d.maxMotorTorque = p[11];
			d.enableSpring = p[12] != 0.0f;
			d.hertz = p[13];
			d.dampingRatio = p[14];
			id = b2CreateRevoluteJoint( w->id, &d );
			break;
		}
		case ME_DISTANCE:
		{
			b2DistanceJointDef d = b2DefaultDistanceJointDef();
			joint_frames( &d.base, a, b, p, flags, 0 );
			float dx = p[2] - p[0], dy = p[3] - p[1];
			d.length = p[15] >= 0.0f ? p[15] : sqrtf( dx * dx + dy * dy );
			if ( d.length < ME_SLOP )
				d.length = ME_SLOP;
			d.enableLimit = p[6] != 0.0f;
			d.minLength = p[7];
			d.maxLength = p[8];
			d.enableMotor = p[9] != 0.0f;
			d.motorSpeed = p[10];
			d.maxMotorForce = p[11];
			d.enableSpring = p[12] != 0.0f;
			d.hertz = p[13];
			d.dampingRatio = p[14];
			id = b2CreateDistanceJoint( w->id, &d );
			break;
		}
		case ME_PRISMATIC:
		{
			b2PrismaticJointDef d = b2DefaultPrismaticJointDef();
			joint_frames( &d.base, a, b, p, flags, 1 );
			d.enableLimit = p[6] != 0.0f;
			d.lowerTranslation = p[7];
			d.upperTranslation = p[8];
			d.enableMotor = p[9] != 0.0f;
			d.motorSpeed = p[10];
			d.maxMotorForce = p[11];
			d.enableSpring = p[12] != 0.0f;
			d.hertz = p[13];
			d.dampingRatio = p[14];
			id = b2CreatePrismaticJoint( w->id, &d );
			break;
		}
		case ME_WELD:
		{
			b2WeldJointDef d = b2DefaultWeldJointDef();
			joint_frames( &d.base, a, b, p, flags, 0 );
			float hertz = p[12] != 0.0f ? p[13] : 0.0f;
			d.linearHertz = hertz;
			d.angularHertz = hertz;
			d.linearDampingRatio = p[14];
			d.angularDampingRatio = p[14];
			id = b2CreateWeldJoint( w->id, &d );
			break;
		}
		case ME_WHEEL:
		{
			b2WheelJointDef d = b2DefaultWheelJointDef();
			joint_frames( &d.base, a, b, p, flags, 1 );
			d.enableLimit = p[6] != 0.0f;
			d.lowerTranslation = p[7];
			d.upperTranslation = p[8];
			d.enableMotor = p[9] != 0.0f;
			d.motorSpeed = p[10];
			d.maxMotorTorque = p[11];
			d.enableSpring = p[12] != 0.0f;
			d.hertz = p[13];
			d.dampingRatio = p[14];
			id = b2CreateWheelJoint( w->id, &d );
			break;
		}
		case ME_TARGET:
		{
			// The ground body is at the origin, not turned: its local frame is the world.
			b2MotorJointDef d = b2DefaultMotorJointDef();
			d.base.bodyIdA = a;
			d.base.bodyIdB = b;
			d.base.localFrameA.p = ( b2Vec2 ){ p[0], p[1] };
			d.base.localFrameB.p = b2Body_GetLocalPoint( b, ( b2Pos ){ p[2], p[3] } );
			d.linearHertz = p[12] != 0.0f ? p[13] : 0.0f;
			d.linearDampingRatio = p[14];
			d.maxSpringForce = p[16];
			d.angularHertz = 0.0f;
			d.maxSpringTorque = 0.0f;
			d.maxVelocityForce = 0.0f;
			d.maxVelocityTorque = 0.0f;
			id = b2CreateMotorJoint( w->id, &d );
			break;
		}
		default:
			return -1;
	}

	w->joints = (MeJoint*)grow( w->joints, &w->jointCapacity, w->jointCount + 1, sizeof( MeJoint ) );
	MeJoint* j = w->joints + w->jointCount;
	j->id = id;
	j->alive = 1;
	j->kind = kind;
	j->bodyA = bodyA;
	j->bodyB = bodyB;
	return w->jointCount++;
}

ME_API void me_joint_destroy( MeWorld* w, int joint )
{
	if ( is_live_joint( w, joint ) == 0 )
		return;
	b2DestroyJoint( w->joints[joint].id );
	w->joints[joint].alive = 0;
}

ME_API void me_joint_state( MeWorld* w, int joint, float* out8 )
{
	memset( out8, 0, 8 * sizeof( float ) );
	if ( is_live_joint( w, joint ) == 0 )
		return;
	MeJoint* j = w->joints + joint;
	b2Pos pa = b2Body_GetWorldPoint( b2Joint_GetBodyA( j->id ), b2Joint_GetLocalFrameA( j->id ).p );
	b2Pos pb = b2Body_GetWorldPoint( b2Joint_GetBodyB( j->id ), b2Joint_GetLocalFrameB( j->id ).p );
	b2Vec2 force = b2Joint_GetConstraintForce( j->id );
	out8[0] = (float)pa.x;
	out8[1] = (float)pa.y;
	out8[2] = (float)pb.x;
	out8[3] = (float)pb.y;
	out8[4] = force.x;
	out8[5] = force.y;
	out8[6] = b2Joint_GetConstraintTorque( j->id );
	if ( j->kind == ME_REVOLUTE )
		out8[7] = b2RevoluteJoint_GetCurrentAngle( j->id );
	else if ( j->kind == ME_DISTANCE )
		out8[7] = b2DistanceJoint_GetCurrentLength( j->id );
	else if ( j->kind == ME_PRISMATIC )
		out8[7] = b2PrismaticJoint_GetCurrentTranslation( j->id );
}

ME_API void me_joint_set_limit( MeWorld* w, int joint, int enable, float lower, float upper )
{
	if ( is_live_joint( w, joint ) == 0 )
		return;
	b2JointId id = w->joints[joint].id;
	switch ( w->joints[joint].kind )
	{
		case ME_REVOLUTE:
			b2RevoluteJoint_SetLimits( id, lower, upper );
			b2RevoluteJoint_EnableLimit( id, enable != 0 );
			break;
		case ME_DISTANCE:
			b2DistanceJoint_SetLimits( id, lower, upper );
			b2DistanceJoint_EnableLimit( id, enable != 0 );
			break;
		case ME_PRISMATIC:
			b2PrismaticJoint_SetLimits( id, lower, upper );
			b2PrismaticJoint_EnableLimit( id, enable != 0 );
			break;
		case ME_WHEEL:
			b2WheelJoint_SetLimits( id, lower, upper );
			b2WheelJoint_EnableLimit( id, enable != 0 );
			break;
		default:
			return;
	}
	b2Joint_WakeBodies( id );
}

ME_API void me_joint_set_motor( MeWorld* w, int joint, int enable, float speed, float maxForce )
{
	if ( is_live_joint( w, joint ) == 0 )
		return;
	b2JointId id = w->joints[joint].id;
	switch ( w->joints[joint].kind )
	{
		case ME_REVOLUTE:
			b2RevoluteJoint_SetTargetMotorSpeed( id, speed );
			b2RevoluteJoint_SetMaxMotorTorque( id, maxForce );
			b2RevoluteJoint_EnableMotor( id, enable != 0 );
			break;
		case ME_DISTANCE:
			b2DistanceJoint_SetTargetMotorSpeed( id, speed );
			b2DistanceJoint_SetMaxMotorForce( id, maxForce );
			b2DistanceJoint_EnableMotor( id, enable != 0 );
			break;
		case ME_PRISMATIC:
			b2PrismaticJoint_SetTargetMotorSpeed( id, speed );
			b2PrismaticJoint_SetMaxMotorForce( id, maxForce );
			b2PrismaticJoint_EnableMotor( id, enable != 0 );
			break;
		case ME_WHEEL:
			b2WheelJoint_SetTargetMotorSpeed( id, speed );
			b2WheelJoint_SetMaxMotorTorque( id, maxForce );
			b2WheelJoint_EnableMotor( id, enable != 0 );
			break;
		default:
			return;
	}
	b2Joint_WakeBodies( id );
}

ME_API void me_joint_set_spring( MeWorld* w, int joint, int enable, float hertz, float dampingRatio )
{
	if ( is_live_joint( w, joint ) == 0 )
		return;
	b2JointId id = w->joints[joint].id;
	switch ( w->joints[joint].kind )
	{
		case ME_REVOLUTE:
			b2RevoluteJoint_SetSpringHertz( id, hertz );
			b2RevoluteJoint_SetSpringDampingRatio( id, dampingRatio );
			b2RevoluteJoint_EnableSpring( id, enable != 0 );
			break;
		case ME_DISTANCE:
			b2DistanceJoint_SetSpringHertz( id, hertz );
			b2DistanceJoint_SetSpringDampingRatio( id, dampingRatio );
			b2DistanceJoint_EnableSpring( id, enable != 0 );
			break;
		case ME_PRISMATIC:
			b2PrismaticJoint_SetSpringHertz( id, hertz );
			b2PrismaticJoint_SetSpringDampingRatio( id, dampingRatio );
			b2PrismaticJoint_EnableSpring( id, enable != 0 );
			break;
		case ME_WELD:
			b2WeldJoint_SetLinearHertz( id, enable ? hertz : 0.0f );
			b2WeldJoint_SetAngularHertz( id, enable ? hertz : 0.0f );
			b2WeldJoint_SetLinearDampingRatio( id, dampingRatio );
			b2WeldJoint_SetAngularDampingRatio( id, dampingRatio );
			break;
		case ME_WHEEL:
			b2WheelJoint_SetSpringHertz( id, hertz );
			b2WheelJoint_SetSpringDampingRatio( id, dampingRatio );
			b2WheelJoint_EnableSpring( id, enable != 0 );
			break;
		case ME_TARGET:
			b2MotorJoint_SetLinearHertz( id, enable ? hertz : 0.0f );
			b2MotorJoint_SetLinearDampingRatio( id, dampingRatio );
			break;
		default:
			return;
	}
	b2Joint_WakeBodies( id );
}

ME_API void me_joint_set_target( MeWorld* w, int joint, float x, float y )
{
	if ( is_live_joint( w, joint ) == 0 || w->joints[joint].kind != ME_TARGET )
		return;
	b2JointId id = w->joints[joint].id;
	b2Transform frame = b2Joint_GetLocalFrameA( id );
	frame.p = ( b2Vec2 ){ x, y };
	b2Joint_SetLocalFrameA( id, frame );
	b2Joint_WakeBodies( id );
}

ME_API void me_joint_set_length( MeWorld* w, int joint, float length )
{
	if ( is_live_joint( w, joint ) == 0 || w->joints[joint].kind != ME_DISTANCE )
		return;
	b2DistanceJoint_SetRestLength( w->joints[joint].id, length );
	b2Joint_WakeBodies( w->joints[joint].id );
}

// ---------------------------------------------------------------------------------------------------- queries

typedef struct
{
	MeWorld* w;
	int queryFlags;
	b2Pos start;
	int shape;
	float fraction;
	b2Vec2 point, normal;
} RayContext;

static float ray_result( b2ShapeId id, b2Pos point, b2Vec2 normal, float fraction, void* context )
{
	RayContext* c = (RayContext*)context;
	int h = shape_handle( id );
	if ( h < 0 )
		return -1.0f;
	int flags = c->w->shapes[h].flags;
	if ( ( flags & ME_SENSOR ) && ( c->queryFlags & ME_QUERY_SENSORS ) == 0 )
		return -1.0f;
	if ( flags & ME_ONE_WAY )
	{
		// A one-way platform is a floor: hit only on its top, never when the query passes one-way platforms.
		if ( ( c->queryFlags & ME_QUERY_PASS_ONE_WAY ) || normal.y < 0.7f )
			return -1.0f;
	}
	if ( b2Shape_TestPoint( id, c->start ) )
		return -1.0f;
	c->shape = h;
	c->fraction = fraction;
	c->point = ( b2Vec2 ){ (float)point.x, (float)point.y };
	c->normal = normal;
	return fraction;
}

ME_API int me_ray_cast( MeWorld* w, float x, float y, float dx, float dy, uint64_t category, uint64_t mask,
	int queryFlags, float* out5 )
{
	RayContext c = { w, queryFlags, { x, y }, -1, 1.0f, { 0.0f, 0.0f }, { 0.0f, 0.0f } };
	b2World_CastRay( w->id, ( b2Pos ){ x, y }, ( b2Vec2 ){ dx, dy }, make_query( category, mask ), ray_result, &c );
	if ( c.shape < 0 )
		return -1;
	out5[0] = c.point.x;
	out5[1] = c.point.y;
	out5[2] = c.normal.x;
	out5[3] = c.normal.y;
	out5[4] = c.fraction;
	return c.shape;
}

typedef struct
{
	MeWorld* w;
	int queryFlags;
	int count;
} OverlapContext;

static bool overlap_result( b2ShapeId id, void* context )
{
	OverlapContext* c = (OverlapContext*)context;
	int h = shape_handle( id );
	if ( h < 0 )
		return true;
	int flags = c->w->shapes[h].flags;
	if ( ( flags & ME_SENSOR ) && ( c->queryFlags & ME_QUERY_SENSORS ) == 0 )
		return true;
	if ( ( flags & ME_ONE_WAY ) && ( c->queryFlags & ME_QUERY_PASS_ONE_WAY ) )
		return true;
	c->w->scratch = (int*)grow( c->w->scratch, &c->w->scratchCapacity, c->count + 1, sizeof( int ) );
	c->w->scratch[c->count++] = h;
	return true;
}

static int compare_int( const void* a, const void* b )
{
	int x = *(const int*)a, y = *(const int*)b;
	return ( x > y ) - ( x < y );
}

ME_API int me_overlap_box( MeWorld* w, float minX, float minY, float maxX, float maxY, uint64_t category,
	uint64_t mask, int queryFlags, int* outShapes, int maxShapes )
{
	minX += ME_TOUCH;
	minY += ME_TOUCH;
	maxX -= ME_TOUCH;
	maxY -= ME_TOUCH;
	if ( !( minX < maxX && minY < maxY ) )
		return 0;
	b2Vec2 points[4] = { { minX, minY }, { maxX, minY }, { maxX, maxY }, { minX, maxY } };
	b2ShapeProxy proxy = b2MakeProxy( points, 4, 0.0f );
	OverlapContext c = { w, queryFlags, 0 };
	b2World_OverlapShape( w->id, b2Pos_zero, &proxy, make_query( category, mask ), overlap_result, &c );
	qsort( w->scratch, (size_t)c.count, sizeof( int ), compare_int );
	for ( int i = 0; i < c.count && i < maxShapes; ++i )
		outShapes[i] = w->scratch[i];
	return c.count;
}

// ---------------------------------------------------------------------------------------------------- moves

typedef struct
{
	int shape;
	b2Vec2 normal;
} Contact;

typedef struct
{
	Contact items[ME_MAX_PLANES * 2];
	int count, total;
} Contacts;

static void add_contact( Contacts* list, int shape, b2Vec2 normal )
{
	for ( int i = 0; i < list->count; ++i )
		if ( list->items[i].shape == shape && list->items[i].normal.x == normal.x && list->items[i].normal.y == normal.y )
			return;
	list->total += 1;
	if ( list->count < (int)( sizeof( list->items ) / sizeof( list->items[0] ) ) )
		list->items[list->count++] = ( Contact ){ shape, normal };
}

// One axis of the box mover: a cast of the moved shape along x or y.
typedef struct
{
	MeWorld* w;
	int body, queryFlags;
	int axis;	  // 0: x, 1: y
	float dir;	  // +1 or -1 along the axis
	b2AABB box;	  // the moved shape's bounds where the cast starts
	int closest;  // nearest blocking shape, or -1
	float fraction;
	Contacts* touching; // probe: every blocking shape, not only the nearest
} AxisCast;

// Whether a shape the cast reached blocks it. A shape Box2D reports at fraction 0 is within the slop at the start: it
// blocks only when it lies ahead along the axis and level with the box across it (neither alongside nor already
// overlapping it, which never block).
static int axis_blocks( AxisCast* c, int h, float fraction )
{
	MeShape* s = c->w->shapes + h;
	if ( s->body == c->body || ( s->flags & ME_SENSOR ) )
		return 0;
	b2AABB a = exact_bounds( c->w, h );
	const b2AABB* m = &c->box;
	if ( s->flags & ME_ONE_WAY )
	{
		if ( ( c->queryFlags & ME_QUERY_PASS_ONE_WAY ) || c->axis != 1 || c->dir > 0.0f )
			return 0;
		if ( m->lowerBound.y < a.upperBound.y - ME_EPS )
			return 0; // it started below the platform's top
	}
	if ( fraction > 0.0f )
		return 1;
	float across = c->axis == 0 ? fmaxf( a.lowerBound.y - m->upperBound.y, m->lowerBound.y - a.upperBound.y )
								: fmaxf( a.lowerBound.x - m->upperBound.x, m->lowerBound.x - a.upperBound.x );
	if ( across >= -ME_EPS )
		return 0;
	float ahead;
	if ( c->axis == 0 )
		ahead = c->dir > 0.0f ? a.lowerBound.x - m->upperBound.x : m->lowerBound.x - a.upperBound.x;
	else
		ahead = c->dir > 0.0f ? a.lowerBound.y - m->upperBound.y : m->lowerBound.y - a.upperBound.y;
	return ahead >= -ME_EPS;
}

static float axis_result( b2ShapeId id, b2Pos point, b2Vec2 normal, float fraction, void* context )
{
	(void)point;
	(void)normal;
	AxisCast* c = (AxisCast*)context;
	int h = shape_handle( id );
	if ( h < 0 || axis_blocks( c, h, fraction ) == 0 )
		return -1.0f;
	b2Vec2 n = c->axis == 0 ? ( b2Vec2 ){ -c->dir, 0.0f } : ( b2Vec2 ){ 0.0f, -c->dir };
	if ( c->touching != NULL )
	{
		add_contact( c->touching, h, n );
		return 1.0f; // keep going: every touching shape
	}
	if ( fraction < c->fraction || c->closest < 0 )
	{
		c->closest = h;
		c->fraction = fraction;
	}
	return fraction;
}

static b2ShapeProxy shape_proxy( MeWorld* w, int shape, b2Vec2 position, b2Rot rotation )
{
	b2ShapeId id = w->shapes[shape].id;
	b2Transform xf = { position, rotation };
	b2Vec2 points[B2_MAX_POLYGON_VERTICES];
	if ( w->shapes[shape].kind == ME_CAPSULE )
	{
		b2Capsule c = b2Shape_GetCapsule( id );
		points[0] = b2TransformPoint( xf, c.center1 );
		points[1] = b2TransformPoint( xf, c.center2 );
		return b2MakeProxy( points, 2, c.radius );
	}
	if ( w->shapes[shape].kind == ME_CIRCLE )
	{
		b2Circle c = b2Shape_GetCircle( id );
		points[0] = b2TransformPoint( xf, c.center );
		return b2MakeProxy( points, 1, c.radius );
	}
	b2Polygon p = b2Shape_GetPolygon( id );
	for ( int i = 0; i < p.count; ++i )
		points[i] = b2TransformPoint( xf, p.vertices[i] );
	return b2MakeProxy( points, p.count, p.radius );
}

static b2AABB proxy_bounds( const b2ShapeProxy* p )
{
	b2AABB box = { { FLT_MAX, FLT_MAX }, { -FLT_MAX, -FLT_MAX } };
	for ( int i = 0; i < p->count; ++i )
	{
		box.lowerBound = b2Min( box.lowerBound, p->points[i] );
		box.upperBound = b2Max( box.upperBound, p->points[i] );
	}
	b2Vec2 r = { p->radius, p->radius };
	box.lowerBound = b2Sub( box.lowerBound, r );
	box.upperBound = b2Add( box.upperBound, r );
	return box;
}

// Casts the shape (at position) along one axis by distance d (signed); returns the distance it can travel.
static float axis_move( MeWorld* w, int body, int shape, b2Vec2 position, b2Rot rotation, int axis, float d,
	uint64_t category, uint64_t mask, int queryFlags, Contacts* contacts )
{
	float dir = d > 0.0f ? 1.0f : -1.0f;
	float length = fabsf( d ) + ME_BACKOFF;
	b2ShapeProxy proxy = shape_proxy( w, shape, position, rotation );
	AxisCast c = { w, body, queryFlags, axis, dir, proxy_bounds( &proxy ), -1, 1.0f, NULL };
	b2Vec2 translation = axis == 0 ? ( b2Vec2 ){ dir * length, 0.0f } : ( b2Vec2 ){ 0.0f, dir * length };
	b2World_CastShape( w->id, b2Pos_zero, &proxy, translation, make_query( category, mask ), axis_result, &c );
	if ( c.closest < 0 )
		return d;
	float travel = c.fraction * length - ME_BACKOFF;
	if ( travel < 0.0f )
		travel = 0.0f;
	if ( travel > fabsf( d ) )
		travel = fabsf( d );
	add_contact( contacts, c.closest, axis == 0 ? ( b2Vec2 ){ -dir, 0.0f } : ( b2Vec2 ){ 0.0f, -dir } );
	return dir * travel;
}

// The surfaces within the skin of the shape at position, in the four axis directions.
static void probe_contacts( MeWorld* w, int body, int shape, b2Vec2 position, b2Rot rotation, uint64_t category,
	uint64_t mask, int queryFlags, Contacts* contacts )
{
	b2ShapeProxy proxy = shape_proxy( w, shape, position, rotation );
	b2AABB box = proxy_bounds( &proxy );
	static const float dirs[4][2] = { { 0.0f, -1.0f }, { 0.0f, 1.0f }, { -1.0f, 0.0f }, { 1.0f, 0.0f } };
	for ( int i = 0; i < 4; ++i )
	{
		int axis = dirs[i][0] != 0.0f ? 0 : 1;
		float dir = axis == 0 ? dirs[i][0] : dirs[i][1];
		AxisCast c = { w, body, queryFlags, axis, dir, box, -1, 1.0f, contacts };
		b2Vec2 translation = { dirs[i][0] * ME_PROBE, dirs[i][1] * ME_PROBE };
		b2World_CastShape( w->id, b2Pos_zero, &proxy, translation, make_query( category, mask ), axis_result, &c );
	}
}

static b2Vec2 move_box( MeWorld* w, int body, int shape, b2Vec2 p, b2Rot q, float dx, float dy, uint64_t category,
	uint64_t mask, int queryFlags, Contacts* contacts )
{
	if ( dx != 0.0f )
		p.x += axis_move( w, body, shape, p, q, 0, dx, category, mask, queryFlags, contacts );
	if ( dy != 0.0f )
		p.y += axis_move( w, body, shape, p, q, 1, dy, category, mask, queryFlags, contacts );
	probe_contacts( w, body, shape, p, q, category, mask, queryFlags, contacts );
	return p;
}

// The capsule mover: Box2D's own algorithm (its "Mover" sample) with the layer's rules for sensors, the body's own
// shapes and one-way platforms applied to the planes and to the cast.
typedef struct
{
	MeWorld* w;
	int body, queryFlags;
	float dy;
	float bottom; // the capsule's lowest point where the move started
	b2CollisionPlane planes[ME_MAX_PLANES];
	int shapes[ME_MAX_PLANES];
	int count;
} PlaneSet;

static int mover_ignores( MeWorld* w, int h, int body, int queryFlags, float dy, float bottom, b2Vec2 normal )
{
	MeShape* s = w->shapes + h;
	if ( s->body == body || ( s->flags & ME_SENSOR ) )
		return 1;
	if ( s->flags & ME_ONE_WAY )
	{
		if ( ( queryFlags & ME_QUERY_PASS_ONE_WAY ) || dy > 0.0f || normal.y < 0.7f )
			return 1;
		b2AABB a = exact_bounds( w, h );
		if ( bottom < a.upperBound.y - ME_SLOP )
			return 1; // it started below the platform's top
	}
	return 0;
}

static bool plane_result( b2ShapeId id, const b2PlaneResult* result, void* context )
{
	PlaneSet* set = (PlaneSet*)context;
	int h = shape_handle( id );
	if ( h < 0 || mover_ignores( set->w, h, set->body, set->queryFlags, set->dy, set->bottom, result->plane.normal ) )
		return true;
	if ( set->count < ME_MAX_PLANES )
	{
		set->planes[set->count] = ( b2CollisionPlane ){ result->plane, FLT_MAX, 0.0f, true };
		set->shapes[set->count] = h;
		set->count += 1;
	}
	return true;
}

typedef struct
{
	MeWorld* w;
	int body, queryFlags;
	float dy, bottom;
	float fraction;
} MoverCast;

static float mover_cast_result( b2ShapeId id, b2Pos point, b2Vec2 normal, float fraction, void* context )
{
	(void)point;
	MoverCast* c = (MoverCast*)context;
	int h = shape_handle( id );
	// Like b2World_CastMover: shapes the capsule already touches are the planes' business, not the cast's.
	if ( h < 0 || fraction <= 0.0f )
		return -1.0f;
	if ( mover_ignores( c->w, h, c->body, c->queryFlags, c->dy, c->bottom, normal ) )
		return -1.0f;
	if ( fraction < c->fraction )
		c->fraction = fraction;
	return fraction;
}

static b2Vec2 move_capsule( MeWorld* w, int body, int shape, b2Vec2 p, float dx, float dy, uint64_t category,
	uint64_t mask, int queryFlags, Contacts* contacts )
{
	b2Capsule local = b2Shape_GetCapsule( w->shapes[shape].id );
	b2Vec2 target = { p.x + dx, p.y + dy };
	b2QueryFilter filter = make_query( category, mask );
	float bottom = fminf( local.center1.y, local.center2.y ) - local.radius + p.y;
	PlaneSet set;
	set.w = w;
	set.body = body;
	set.queryFlags = queryFlags;
	set.dy = dy;
	set.bottom = bottom;
	for ( int iteration = 0; iteration < ME_MOVER_ITERATIONS; ++iteration )
	{
		set.count = 0;
		b2Capsule mover = { b2Add( p, local.center1 ), b2Add( p, local.center2 ), local.radius };
		b2World_CollideMover( w->id, b2Pos_zero, &mover, filter, plane_result, &set );
		b2PlaneSolverResult solved = b2SolvePlanes( b2Sub( target, p ), set.planes, set.count );
		b2Vec2 points[2] = { mover.center1, mover.center2 };
		b2ShapeProxy proxy = b2MakeProxy( points, 2, mover.radius );
		MoverCast cast = { w, body, queryFlags, dy, bottom, 1.0f };
		b2World_CastShape( w->id, b2Pos_zero, &proxy, solved.delta, filter, mover_cast_result, &cast );
		b2Vec2 delta = b2MulSV( cast.fraction, solved.delta );
		p = b2Add( p, delta );
		for ( int i = 0; i < set.count; ++i )
			add_contact( contacts, set.shapes[i], set.planes[i].plane.normal );
		if ( b2LengthSquared( delta ) < ME_SLOP * ME_SLOP )
			break;
	}
	return p;
}

ME_API int me_move( MeWorld* w, int body, int shape, float dx, float dy, uint64_t category, uint64_t mask,
	int queryFlags, float* outXY2, int* outShapes, float* outNormals, int maxContacts )
{
	b2BodyId id = w->bodies[body].id;
	b2Pos start = b2Body_GetPosition( id );
	b2Rot q = b2Body_GetRotation( id );
	b2Vec2 p = { (float)start.x, (float)start.y };
	Contacts contacts = { 0 };
	if ( w->shapes[shape].kind == ME_CAPSULE )
		p = move_capsule( w, body, shape, p, dx, dy, category, mask, queryFlags, &contacts );
	else
		p = move_box( w, body, shape, p, q, dx, dy, category, mask, queryFlags, &contacts );
	b2Body_SetTransform( id, ( b2Pos ){ p.x, p.y }, q );
	outXY2[0] = p.x;
	outXY2[1] = p.y;
	for ( int i = 0; i < contacts.count && i < maxContacts; ++i )
	{
		outShapes[i] = contacts.items[i].shape;
		outNormals[2 * i] = contacts.items[i].normal.x;
		outNormals[2 * i + 1] = contacts.items[i].normal.y;
	}
	return contacts.count;
}

// ---------------------------------------------------------------------------------------------------- events

// A visitor the contract lets sensors see: a live non-sensor shape of a kinematic or dynamic body.
static int visitor_seen( MeWorld* w, int h )
{
	return h >= 0 && ( w->shapes[h].flags & ME_SENSOR ) == 0 && w->bodies[w->shapes[h].body].type != ME_STATIC;
}

ME_API int me_sensor_events( MeWorld* w, int* outBegin, int maxBegin, int* outEnd, int maxEnd, int* outEndCount )
{
	b2SensorEvents e = b2World_GetSensorEvents( w->id );
	int begins = 0, ends = 0;
	for ( int i = 0; i < e.beginCount; ++i )
	{
		int sensor = shape_handle( e.beginEvents[i].sensorShapeId ), visitor = shape_handle( e.beginEvents[i].visitorShapeId );
		if ( sensor < 0 || visitor_seen( w, visitor ) == 0 )
			continue;
		if ( begins < maxBegin )
		{
			outBegin[2 * begins] = sensor;
			outBegin[2 * begins + 1] = visitor;
		}
		begins += 1;
	}
	for ( int i = 0; i < e.endCount; ++i )
	{
		int sensor = shape_handle( e.endEvents[i].sensorShapeId ), visitor = shape_handle( e.endEvents[i].visitorShapeId );
		if ( sensor >= 0 && visitor >= 0 && visitor_seen( w, visitor ) == 0 )
			continue;
		if ( ends < maxEnd )
		{
			outEnd[2 * ends] = sensor;
			outEnd[2 * ends + 1] = visitor;
		}
		ends += 1;
	}
	*outEndCount = ends;
	return begins;
}

ME_API int me_contact_events( MeWorld* w, int* outBegin, float* outNormals, int maxBegin, int* outEnd, int maxEnd,
	int* outEndCount )
{
	b2ContactEvents e = b2World_GetContactEvents( w->id );
	int begins = 0;
	for ( int i = 0; i < e.beginCount; ++i )
	{
		const b2ContactBeginTouchEvent* ev = e.beginEvents + i;
		int a = shape_handle( ev->shapeIdA ), b = shape_handle( ev->shapeIdB );
		if ( a < 0 || b < 0 )
			continue;
		if ( begins < maxBegin )
		{
			b2Vec2 n = { 0.0f, 0.0f };
			if ( b2Contact_IsValid( ev->contactId ) )
				n = b2Contact_GetData( ev->contactId ).manifold.normal;
			outBegin[2 * begins] = a;
			outBegin[2 * begins + 1] = b;
			outNormals[2 * begins] = n.x;
			outNormals[2 * begins + 1] = n.y;
		}
		begins += 1;
	}
	int ends = e.endCount;
	for ( int i = 0; i < e.endCount && i < maxEnd; ++i )
	{
		outEnd[2 * i] = shape_handle( e.endEvents[i].shapeIdA );
		outEnd[2 * i + 1] = shape_handle( e.endEvents[i].shapeIdB );
	}
	*outEndCount = ends;
	return begins;
}

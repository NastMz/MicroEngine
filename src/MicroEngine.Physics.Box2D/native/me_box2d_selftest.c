// me_box2d_selftest: runs every function of the layer once on the machine that built it (under Node for wasm) and
// prints one line per check, then the determinism hash of a benchmark scene. Exit status 0 when every check passed.
// CI runs it for every runtime identifier it can execute and compares the hashes: the same build of Box2D gives the
// same bits everywhere, or the determinism promise does not hold.
//
// Usage: me_box2d_selftest            the checks and the hash
//        me_box2d_selftest bench N S  N boxes, S steps: also prints the mean time per step (informative)
#include "me_box2d.h"

#include "box2d/base.h"

#include <math.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>

static int failures = 0;

static void check( const char* name, int ok, const char* detail )
{
	printf( "%s %s%s%s\n", ok ? "PASS" : "FAIL", name, detail[0] ? ": " : "", detail );
	if ( ok == 0 )
		failures += 1;
}

static char text[256];
#define DETAIL( ... ) ( snprintf( text, sizeof( text ), __VA_ARGS__ ), text )

#define ALL UINT64_MAX
#define STATIC_LAYER 1u
#define ENEMY_LAYER 2u
#define SENSOR_LAYER 4u
#define PLAYER_LAYER 8u

static int box( MeWorld* w, int type, float x0, float x1, float y0, float y1, uint64_t category, uint64_t mask, int flags )
{
	int body = me_body_create( w, type, ( x0 + x1 ) / 2, ( y0 + y1 ) / 2, 0.0f, 0 );
	float g[4] = { ( x1 - x0 ) / 2, ( y1 - y0 ) / 2, 0.0f, 0.0f };
	int shape = me_shape_create( w, body, ME_BOX, g, 0, 0.0f, category, mask, flags, -1.0f, -1.0f, -1.0f );
	return shape;
}

static float player_bottom( MeWorld* w, int player )
{
	float s[6];
	me_body_state( w, player, s );
	return s[1] - 0.2f;
}

static int has_contact( const int* shapes, int count, int shape )
{
	for ( int i = 0; i < count; ++i )
		if ( shapes[i] == shape )
			return 1;
	return 0;
}

// The scene of the managed common suite in metres: ground (top y 0), wall (x 3..3.5), thin floor (x -4.5..-3.5,
// y 1.2..1.25), sensor (x 1.2..1.7), enemy box (x -1.2..-0.7), a kinematic 0.4 × 0.4 box player at x 0, bottom y 0.4.
static void contract_checks( void )
{
	const float skin = me_move_skin();
	const float tol = skin + 1e-4f;
	MeWorld* w = me_world_create( 0.0f, -10.0f, 4, 1 );
	int ground = box( w, ME_STATIC, -10, 10, -0.5f, 0, STATIC_LAYER, ALL, 0 );
	int wall = box( w, ME_STATIC, 3, 3.5f, 0, 2, STATIC_LAYER, ALL, 0 );
	int thin = box( w, ME_STATIC, -4.5f, -3.5f, 1.2f, 1.25f, STATIC_LAYER, ALL, 0 );
	int sensor = box( w, ME_STATIC, 1.2f, 1.7f, 0, 1, SENSOR_LAYER, ALL, ME_SENSOR );
	int enemy = box( w, ME_STATIC, -1.2f, -0.7f, 0, 0.5f, ENEMY_LAYER, ALL, 0 );
	int playerShape = box( w, ME_KINEMATIC, -0.2f, 0.2f, 0.4f, 0.8f, PLAYER_LAYER, STATIC_LAYER | SENSOR_LAYER, ME_SENSOR_EVENTS );
	int player = 5;
	(void)sensor;
	float xy[2], normals[32];
	int shapes[16];
	int n = 0;

	for ( int i = 0; i < 20; ++i )
		n = me_move( w, player, playerShape, 0, -0.1f, PLAYER_LAYER, STATIC_LAYER, 0, xy, shapes, normals, 16 );
	float gap = player_bottom( w, player );
	check( "move.lands", gap >= 0 && gap <= tol && has_contact( shapes, n, ground ), DETAIL( "gap %.5f, skin %.4f, %d contacts", gap, skin, n ) );

	for ( int i = 0; i < 40; ++i )
		n = me_move( w, player, playerShape, 0.1f, 0, PLAYER_LAYER, STATIC_LAYER, 0, xy, shapes, normals, 16 );
	gap = 3.0f - ( xy[0] + 0.2f );
	int wallNormal = 0, onGround = 0;
	for ( int i = 0; i < n; ++i )
	{
		wallNormal |= shapes[i] == wall && normals[2 * i] == -1.0f;
		onGround |= shapes[i] == ground && normals[2 * i + 1] == 1.0f;
	}
	check( "move.blocked-by-wall", gap >= 0 && gap <= tol && wallNormal && onGround, DETAIL( "gap %.5f", gap ) );

	int enters = 0, exits = 0, begin[32], end[32], endCount = 0;
	for ( int i = 0; i < 40; ++i )
	{
		me_move( w, player, playerShape, -0.1f, 0, PLAYER_LAYER, STATIC_LAYER, 0, xy, shapes, normals, 16 );
		me_world_step( w, 1.0f / 60.0f );
		enters += me_sensor_events( w, begin, 16, end, 16, &endCount );
		exits += endCount;
	}
	check( "sensor.enter-exit", enters == 1 && exits == 1, DETAIL( "%d entries, %d exits, x %.3f", enters, exits, xy[0] ) );

	int touchedEnemy = 0;
	for ( int i = 0; i < 20; ++i )
	{
		n = me_move( w, player, playerShape, -0.1f, 0, PLAYER_LAYER, STATIC_LAYER, 0, xy, shapes, normals, 16 );
		touchedEnemy |= has_contact( shapes, n, enemy );
	}
	check( "layers.mask-skips-enemy", touchedEnemy == 0 && xy[0] < -1.5f, DETAIL( "x %.3f", xy[0] ) );

	float ray[5];
	int hit = me_ray_cast( w, -2.0f, 2.0f, 0, -3.0f, STATIC_LAYER, ALL, 0, ray );
	check( "ray.closest", hit == ground && fabsf( ray[1] ) < 1e-4f && ray[3] == 1.0f && fabsf( ray[4] - 2.0f / 3.0f ) < 1e-4f,
		DETAIL( "shape %d, y %.5f, fraction %.5f", hit, ray[1], ray[4] ) );
	hit = me_ray_cast( w, 1.45f, 2.0f, 0, -3.0f, STATIC_LAYER, ALL, 0, ray );
	int hitSensor = me_ray_cast( w, 1.45f, 2.0f, 0, -3.0f, STATIC_LAYER, ALL, ME_QUERY_SENSORS, ray );
	check( "ray.sensors-opt-in", hit == ground && hitSensor == sensor, DETAIL( "%d, %d", hit, hitSensor ) );
	hit = me_ray_cast( w, -2.0f, 0.2f, 6.0f, 0, STATIC_LAYER, ENEMY_LAYER, 0, ray );
	int hitWall = me_ray_cast( w, -2.0f, 0.2f, 6.0f, 0, STATIC_LAYER, STATIC_LAYER, 0, ray );
	check( "layers.ray-mask", hit == enemy && hitWall == wall, DETAIL( "%d, %d", hit, hitWall ) );

	int found[8];
	int none = me_overlap_box( w, 3.51f, 0.2f, 3.8f, 0.6f, STATIC_LAYER, ALL, 0, found, 8 );
	int one = me_overlap_box( w, 3.2f, 0.2f, 3.8f, 0.6f, STATIC_LAYER, ALL, 0, found, 8 );
	int edge = me_overlap_box( w, 3.5f, 0.2f, 3.8f, 0.6f, STATIC_LAYER, ALL, 0, found, 8 );
	check( "overlap.exact", none == 0 && one == 1 && found[0] == wall && edge == 0, DETAIL( "%d, %d, edge %d", none, one, edge ) );

	me_body_set_position( w, player, -4.0f, 3.0f );
	n = me_move( w, player, playerShape, 0, -6.0f, PLAYER_LAYER, STATIC_LAYER, 0, xy, shapes, normals, 16 );
	gap = player_bottom( w, player ) - 1.25f;
	check( "move.no-tunnel", gap >= 0 && gap <= tol && has_contact( shapes, n, thin ), DETAIL( "gap %.5f", gap ) );

	me_body_set_position( w, player, 3.1f, 1.0f );
	me_move( w, player, playerShape, -0.5f, 0, PLAYER_LAYER, STATIC_LAYER, 0, xy, shapes, normals, 16 );
	check( "move.starts-inside", fabsf( xy[0] - 2.6f ) < 1e-4f, DETAIL( "x %.5f", xy[0] ) );

	// A dynamic crate lands on the ground: one begin contact event with an up-pointing normal.
	int crate = box( w, ME_DYNAMIC, 5.0f, 5.5f, 1.0f, 1.5f, STATIC_LAYER, ALL, 0 );
	int contactBegins = 0;
	float contactNormal[2] = { 0, 0 };
	for ( int i = 0; i < 120; ++i )
	{
		me_world_step( w, 1.0f / 60.0f );
		int pairs[16], ends[16], endN = 0;
		float cn[32];
		int b = me_contact_events( w, pairs, cn, 8, ends, 8, &endN );
		for ( int j = 0; j < b && j < 8; ++j )
			if ( ( pairs[2 * j] == crate && pairs[2 * j + 1] == ground ) || ( pairs[2 * j] == ground && pairs[2 * j + 1] == crate ) )
			{
				contactBegins += 1;
				contactNormal[0] = cn[4 * j];
				contactNormal[1] = cn[4 * j + 1];
			}
	}
	float s[6];
	me_body_state( w, 6, s );
	check( "events.contact-begin", contactBegins == 1 && fabsf( fabsf( contactNormal[1] ) - 1.0f ) < 1e-3f && fabsf( s[1] - 0.25f ) < 0.02f,
		DETAIL( "%d begins, normal (%.3f, %.3f), crate y %.4f", contactBegins, contactNormal[0], contactNormal[1], s[1] ) );
	check( "body.mass", fabsf( me_body_mass( w, 6 ) - 0.25f ) < 1e-5f, DETAIL( "%.5f kg", me_body_mass( w, 6 ) ) );

	// Destroying the crate ends its contact; the event names it as destroyed (-1).
	me_shape_destroy( w, crate );
	me_world_step( w, 1.0f / 60.0f );
	int pairs[16], ends[16], endN = 0;
	float cn[32];
	me_contact_events( w, pairs, cn, 8, ends, 8, &endN );
	check( "events.destroyed-shape", endN == 1 && ( ends[0] == -1 || ends[1] == -1 ), DETAIL( "%d ends", endN ) );
	me_body_destroy( w, 6 );

	int counters[4];
	me_world_counters( w, counters );
	check( "world.counters", counters[0] == 6 && counters[1] == 6, DETAIL( "%d bodies, %d shapes", counters[0], counters[1] ) );
	me_world_destroy( w );
}

// One-way platforms for the mover and for dynamic bodies, and the round and polygon shapes.
static void capability_checks( void )
{
	const float tol = me_move_skin() + 1e-4f;
	MeWorld* w = me_world_create( 0.0f, -10.0f, 4, 1 );
	box( w, ME_STATIC, -10, 10, -0.5f, 0, STATIC_LAYER, ALL, 0 );
	int platform = box( w, ME_STATIC, -1.5f, 1.5f, 1.0f, 1.1f, STATIC_LAYER, ALL, ME_ONE_WAY );
	int playerShape = box( w, ME_KINEMATIC, -0.2f, 0.2f, 0.4f, 0.8f, STATIC_LAYER, ALL, ME_SENSOR_EVENTS );
	int player = 2;
	float xy[2], normals[32];
	int shapes[16], n;
	me_move( w, player, playerShape, 0, 1.2f, STATIC_LAYER, ALL, 0, xy, shapes, normals, 16 );
	int rose = fabsf( player_bottom( w, player ) - 1.6f ) < 1e-4f;
	for ( int i = 0; i < 10; ++i )
		n = me_move( w, player, playerShape, 0, -0.1f, STATIC_LAYER, ALL, 0, xy, shapes, normals, 16 );
	float gap = player_bottom( w, player ) - 1.1f;
	check( "oneway.mover", rose && gap >= 0 && gap <= tol && has_contact( shapes, n, platform ), DETAIL( "gap %.5f", gap ) );
	n = me_move( w, player, playerShape, 0, -0.5f, STATIC_LAYER, ALL, ME_QUERY_PASS_ONE_WAY, xy, shapes, normals, 16 );
	check( "oneway.pass", player_bottom( w, player ) < 1.0f, DETAIL( "bottom %.4f", player_bottom( w, player ) ) );
	float ray[5];
	int down = me_ray_cast( w, 0.5f, 3.0f, 0, -2.5f, STATIC_LAYER, ALL, 0, ray );
	int up = me_ray_cast( w, 0.5f, 0.9f, 0, 2.0f, STATIC_LAYER, ALL, 0, ray );
	check( "oneway.rays", down == platform && up == -1, DETAIL( "%d, %d", down, up ) );

	// A dynamic box thrown up through the platform lands on it; one dropped beside it lands on the ground.
	int thrownShape = box( w, ME_DYNAMIC, 0.85f, 1.15f, 0.3f, 0.6f, STATIC_LAYER, ALL, 0 );
	int thrown = 3;
	me_body_set_velocity( w, thrown, 0, 7.0f );
	for ( int i = 0; i < 180; ++i )
		me_world_step( w, 1.0f / 60.0f );
	float s[6];
	me_body_state( w, thrown, s );
	check( "oneway.dynamic", fabsf( s[1] - 1.25f ) < 0.02f, DETAIL( "y %.4f", s[1] ) );
	(void)thrownShape;

	// Circle, capsule and polygon dynamic bodies come to rest on the ground; a rotated box falls flat.
	int circleBody = me_body_create( w, ME_DYNAMIC, 4.0f, 2.0f, 0.0f, 0 );
	float cg[2] = { 0, 0 };
	int circle = me_shape_create( w, circleBody, ME_CIRCLE, cg, 0, 0.25f, STATIC_LAYER, ALL, 0, -1, -1, -1 );
	int polyBody = me_body_create( w, ME_DYNAMIC, 6.0f, 2.0f, 0.3f, 1 );
	float pg[6] = { -0.3f, 0.0f, 0.3f, 0.0f, 0.0f, 0.4f };
	int poly = me_shape_create( w, polyBody, ME_POLYGON, pg, 3, 0, STATIC_LAYER, ALL, 0, 2.0f, 0.4f, 0.1f );
	float bad[6] = { 0, 0, 1, 1, 2, 2 };
	int degenerate = me_shape_create( w, polyBody, ME_POLYGON, bad, 3, 0, STATIC_LAYER, ALL, 0, -1, -1, -1 );
	int capsuleBody = me_body_create( w, ME_KINEMATIC, -5.0f, 1.0f, 0.0f, 0 );
	float kg[4] = { 0, -0.2f, 0, 0.2f };
	int capsule = me_shape_create( w, capsuleBody, ME_CAPSULE, kg, 0, 0.2f, STATIC_LAYER, ALL, ME_SENSOR_EVENTS, -1, -1, -1 );
	for ( int i = 0; i < 240; ++i )
		me_world_step( w, 1.0f / 60.0f );
	float c[6], p[6];
	me_body_state( w, circleBody, c );
	me_body_state( w, polyBody, p );
	float bounds[4];
	me_shape_bounds( w, poly, bounds );
	check( "shapes.round-and-polygon", circle >= 0 && poly >= 0 && degenerate == -1 && fabsf( c[1] - 0.25f ) < 0.02f && fabsf( bounds[1] ) < 0.02f,
		DETAIL( "circle y %.4f, polygon min y %.4f", c[1], bounds[1] ) );
	for ( int i = 0; i < 30; ++i )
		n = me_move( w, capsuleBody, capsule, 0, -0.1f, STATIC_LAYER, ALL, 0, xy, shapes, normals, 16 );
	gap = xy[1] - 0.4f;
	check( "move.capsule", fabsf( gap ) <= tol && n > 0 && normals[1] > 0.7f, DETAIL( "gap %.5f, %d contacts", gap, n ) );
	me_world_destroy( w );
}

static uint64_t hash_bits( uint64_t h, float f )
{
	uint32_t u;
	memcpy( &u, &f, sizeof u );
	for ( int i = 0; i < 4; ++i )
	{
		h ^= ( u >> ( 8 * i ) ) & 0xffu;
		h *= 1099511628211ull;
	}
	return h;
}

// P6's benchmark scene: boxes rained into a static container, 60 Hz, 4 sub-steps; FNV-1a over every body's x, y,
// angle at the given steps.
static uint64_t bench( int count, int steps, double* msPerStep )
{
	MeWorld* w = me_world_create( 0.0f, -10.0f, 4, 0 );
	box( w, ME_STATIC, -6.5f, 6.5f, -1.0f, 0.0f, STATIC_LAYER, ALL, 0 );
	box( w, ME_STATIC, -6.5f, -6.0f, 0.0f, 20.0f, STATIC_LAYER, ALL, 0 );
	box( w, ME_STATIC, 6.0f, 6.5f, 0.0f, 20.0f, STATIC_LAYER, ALL, 0 );
	int first = 3;
	for ( int i = 0; i < count; ++i )
	{
		int column = i % 20, row = i / 20;
		int body = me_body_create( w, ME_DYNAMIC, -5.0f + column * 0.55f, 1.0f + row * 0.6f, 0.05f * ( ( i % 7 ) - 3 ), 0 );
		float g[4] = { 0.25f, 0.25f, 0, 0 };
		me_shape_create( w, body, ME_BOX, g, 0, 0, STATIC_LAYER, ALL, 0, -1, -1, -1 );
	}
	uint64_t h = 14695981039346656037ull;
	clock_t start = clock();
	for ( int step = 1; step <= steps; ++step )
	{
		me_world_step( w, 1.0f / 60.0f );
		if ( step == 1 || step == 10 || step == 60 || step == 120 || step == 300 || step == steps )
			for ( int b = first; b < first + count; ++b )
			{
				float s[6];
				me_body_state( w, b, s );
				h = hash_bits( hash_bits( hash_bits( h, s[0] ), s[1] ), s[2] );
			}
	}
	*msPerStep = 1000.0 * (double)( clock() - start ) / CLOCKS_PER_SEC / steps;
	me_world_destroy( w );
	return h;
}

// Dynamics control and joints: an impulse, the body's dynamics, a pendulum on a revolute joint, a rope, and a joint
// that ends with its body.
static void dynamics_and_joint_checks( void )
{
	MeWorld* w = me_world_create( 0.0f, -10.0f, 4, 1 );
	box( w, ME_STATIC, -0.1f, 0.1f, 2.9f, 3.1f, STATIC_LAYER, ALL, 0 );	  // body 0: the anchor at (0, 3)
	box( w, ME_DYNAMIC, 0.75f, 1.25f, 2.75f, 3.25f, STATIC_LAYER, ALL, 0 ); // body 1: a bob at (1, 3)
	float m[4], s[6], d[5] = { 0.5f, 0.25f, 0.0f, 1.0f, 0.0f }, got[5];
	me_body_mass_data( w, 1, m );
	me_body_apply( w, 1, 1, m[0] * 2.0f, 0.0f, 0.0f, 0.0f, 0, 1 );
	me_body_state( w, 1, s );
	check( "body.impulse", fabsf( s[3] - 2.0f ) < 1e-4f && fabsf( m[1] - 1.0f ) < 1e-4f && m[3] > 0.0f, DETAIL( "vx %.4f, inertia %.5f", s[3], m[3] ) );
	me_body_set_dynamics( w, 1, d );
	me_body_get_dynamics( w, 1, got );
	check( "body.dynamics", memcmp( d, got, sizeof( d ) ) == 0 && me_body_is_awake( w, 1 ), "" );
	float reset[5] = { 0.0f, 0.0f, 1.0f, 0.0f, 1.0f };
	me_body_set_dynamics( w, 1, reset );
	me_body_set_velocity( w, 1, 0.0f, 0.0f );
	me_body_set_angular_velocity( w, 1, 0.0f );

	float p[17] = { 0.0f, 3.0f, 0.0f, 3.0f };
	int pin = me_joint_create( w, ME_REVOLUTE, 0, 1, p, 0 );
	float lowest = 3.0f, worst = 0.0f, j[8];
	for ( int i = 0; i < 120; ++i )
	{
		me_world_step( w, 1.0f / 60.0f );
		me_body_state( w, 1, s );
		float arm = sqrtf( s[0] * s[0] + ( s[1] - 3.0f ) * ( s[1] - 3.0f ) );
		worst = fmaxf( worst, fabsf( arm - 1.0f ) );
		lowest = fminf( lowest, s[1] );
	}
	me_joint_state( w, pin, j );
	check( "joint.revolute", pin == 0 && worst < 0.02f && lowest < 2.2f && fabsf( j[0] - j[2] ) < 0.02f && fabsf( j[1] - j[3] ) < 0.02f,
		DETAIL( "arm off by %.4f, lowest y %.3f, angle %.3f", worst, lowest, j[7] ) );

	box( w, ME_DYNAMIC, 4.75f, 5.25f, 2.25f, 2.75f, STATIC_LAYER, ALL, 0 ); // body 2: hangs from a rope at (5, 3)
	box( w, ME_STATIC, 4.9f, 5.1f, 2.9f, 3.1f, STATIC_LAYER, ALL, 0 );	  // body 3
	float r[17] = { 5.0f, 3.0f, 5.0f, 2.5f, 0.0f, 0.0f, 1.0f, 0.0f, 1.5f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, -1.0f, 0.0f };
	int rope = me_joint_create( w, ME_DISTANCE, 3, 2, r, 0 );
	float longest = 0.0f;
	for ( int i = 0; i < 180; ++i )
	{
		me_world_step( w, 1.0f / 60.0f );
		me_joint_state( w, rope, j );
		longest = fmaxf( longest, j[7] );
	}
	check( "joint.rope", rope == 1 && longest > 1.45f && longest < 1.55f && j[5] != 0.0f, DETAIL( "longest %.4f, force y %.3f", longest, j[5] ) );

	me_body_destroy( w, 1 );
	me_joint_state( w, pin, j );
	me_joint_destroy( w, pin );
	me_joint_set_motor( w, pin, 1, 1.0f, 1.0f );
	check( "joint.ends-with-body", j[0] == 0.0f && j[7] == 0.0f && me_joint_create( w, ME_REVOLUTE, 0, 1, p, 0 ) == -1, "" );
	me_world_destroy( w );
}

// Every hit of a ray, cast shapes, shape and point overlaps, the nearest surface, impacts and contact points.
static void query_checks( void )
{
	MeWorld* w = me_world_create( 0.0f, -10.0f, 4, 1 );
	int ground = box( w, ME_STATIC, -10.0f, 10.0f, -0.5f, 0.0f, STATIC_LAYER, ALL, 0 );
	int wall = box( w, ME_STATIC, 3.0f, 3.5f, 0.0f, 2.0f, STATIC_LAYER, ALL, 0 );
	int far = box( w, ME_STATIC, 6.0f, 6.5f, 0.0f, 2.0f, STATIC_LAYER, ALL, 0 );
	int shapes[8];
	float hits[40], out[8];
	int n = me_ray_cast_all( w, 0.0f, 1.0f, 8.0f, 0.0f, STATIC_LAYER, ALL, 0, shapes, hits, 8 );
	check( "query.ray-all", n == 2 && shapes[0] == wall && shapes[1] == far && fabsf( hits[4] - 0.375f ) < 1e-4f && fabsf( hits[9] - 0.75f ) < 1e-4f,
		DETAIL( "%d hits, fractions %.4f %.4f", n, hits[4], hits[9] ) );

	int hit = me_shape_cast( w, 0.0f, 0.0f, 0.25f, 0.0f, 2.0f, 0.0f, -4.0f, STATIC_LAYER, ALL, 0, out );
	check( "query.cast-circle", hit == ground && out[4] > 0.435f && out[4] < 0.44f && fabsf( out[3] - 1.0f ) < 1e-3f, DETAIL( "fraction %.4f", out[4] ) );
	hit = me_shape_cast( w, 0.2f, 0.4f, 0.0f, 0.0f, 1.0f, 4.0f, 0.0f, STATIC_LAYER, ALL, 0, out );
	check( "query.cast-box", hit == wall && out[4] > 0.69f && out[4] <= 0.7001f && fabsf( out[2] + 1.0f ) < 1e-3f, DETAIL( "fraction %.4f", out[4] ) );
	hit = me_shape_cast( w, 0.2f, 0.4f, 0.0f, 0.0f, 0.2f, 0.0f, -1.0f, STATIC_LAYER, ALL, 0, out );
	check( "query.cast-overlapped", hit == -1, DETAIL( "%d", hit ) );

	me_world_set_ignored( w, &wall, 1 );
	n = me_ray_cast_all( w, 0.0f, 1.0f, 8.0f, 0.0f, STATIC_LAYER, ALL, 0, shapes, hits, 8 );
	hit = me_shape_cast( w, 0.2f, 0.4f, 0.0f, 0.0f, 1.0f, 8.0f, 0.0f, STATIC_LAYER, ALL, 0, out );
	me_world_set_ignored( w, NULL, 0 );
	check( "query.ignored", n == 1 && shapes[0] == far && hit == far && me_ray_cast( w, 0.0f, 1.0f, 8.0f, 0.0f, STATIC_LAYER, ALL, 0, out ) == wall, DETAIL( "%d hits, cast %d", n, hit ) );

	n = me_overlap_shape( w, 0.0f, 0.5f, 0.25f, 2.8f, 0.9f, STATIC_LAYER, ALL, 0, shapes, 8 );
	int near = me_overlap_shape( w, 0.0f, 0.5f, 0.25f, 2.7f, 0.9f, STATIC_LAYER, ALL, 0, shapes + 4, 4 );
	check( "query.overlap-capsule", n == 1 && shapes[0] == wall && near == 0, DETAIL( "%d, %d", n, near ) );
	n = me_overlap_point( w, 3.2f, 1.0f, STATIC_LAYER, ALL, 0, shapes, 8 );
	check( "query.overlap-point", n == 1 && shapes[0] == wall && me_overlap_point( w, 2.9f, 1.0f, STATIC_LAYER, ALL, 0, shapes, 8 ) == 0, DETAIL( "%d", n ) );
	hit = me_closest_point( w, 2.0f, 1.5f, 2.0f, STATIC_LAYER, ALL, 0, out );
	check( "query.closest-point", hit == wall && fabsf( out[0] - 3.0f ) < 1e-4f && fabsf( out[2] - 1.0f ) < 1e-4f && me_closest_point( w, 2.0f, 1.5f, 0.5f, STATIC_LAYER, ALL, 0, out + 4 ) == -1,
		DETAIL( "x %.4f, distance %.4f", out[0], out[2] ) );

	// A crate dropped from two metres: one impact at about 6 m/s, then two contact points that hold it up.
	int crate = box( w, ME_DYNAMIC, -2.25f, -1.75f, 2.0f, 2.5f, STATIC_LAYER, ALL, 0 );
	me_world_set_impact_speed( w, 1.0f );
	int impacts = 0, pairs[8];
	float impact[24] = { 0 }, first[6] = { 0 };
	for ( int i = 0; i < 120; ++i )
	{
		me_world_step( w, 1.0f / 60.0f );
		int k = me_impact_events( w, pairs, impact, 4 );
		if ( k > 0 && impacts == 0 )
			memcpy( first, impact, sizeof( first ) );
		impacts += k;
	}
	check( "events.impact", impacts >= 1 && first[4] > 5.0f && first[4] < 7.0f && first[5] > 0.0f && fabsf( first[1] ) < 0.1f,
		DETAIL( "%d impacts, speed %.3f, impulse %.4f, y %.4f", impacts, first[4], first[5], first[1] ) );
	float points[24];
	n = me_body_contacts( w, 3, pairs, points, 4 );
	check( "body.contacts", n == 2 && pairs[0] == crate && pairs[1] == ground && fabsf( points[3] - 1.0f ) < 1e-3f && points[5] + points[11] > 0.0f,
		DETAIL( "%d points, normal y %.3f, impulse %.5f", n, points[3], points[5] + points[11] ) );
	me_world_destroy( w );
}

// Tile geometry as chains: a mover lands on it and walks its whole length, a dynamic box slides along it without
// catching, a ray hits it from outside, and Box2D's own API is exported next to the layer.
static void tile_checks( void )
{
	const float tol = me_move_skin() + 1e-4f;
	MeWorld* w = me_world_create( 0.0f, -10.0f, 4, 1 );
	int level = me_body_create( w, ME_STATIC, 0.0f, 0.0f, 0.0f, 0 );
	// A floor 8 m long and 0.5 m thick whose top is at y 0, with a step up at x 6, as one loop that keeps the solid
	// on its left; and a one-way platform over it.
	float points[12] = { 0.0f, -0.5f, 8.0f, -0.5f, 8.0f, 0.5f, 6.0f, 0.5f, 6.0f, 0.0f, 0.0f, 0.0f };
	int counts[1] = { 6 }, oneWay = -1;
	float platform[4] = { 1.0f, 1.375f, 3.0f, 1.5f };
	int tiles = me_tiles_create( w, level, points, counts, 1, platform, 1, STATIC_LAYER, ALL, &oneWay );
	int playerShape = box( w, ME_KINEMATIC, 0.8f, 1.2f, 0.4f, 0.8f, STATIC_LAYER, ALL, ME_SENSOR_EVENTS );
	int player = 1;
	float xy[2], normals[32], out[8];
	int shapes[16], n;
	me_move( w, player, playerShape, 0.0f, -1.0f, STATIC_LAYER, ALL, 0, xy, shapes, normals, 16 );
	float gap = player_bottom( w, player );
	n = me_move( w, player, playerShape, 6.0f, 0.0f, STATIC_LAYER, ALL, 0, xy, shapes, normals, 16 );
	check( "tiles.mover", tiles >= 0 && oneWay >= 0 && gap >= 0 && gap <= tol && fabsf( xy[0] - 5.8f ) <= tol && has_contact( shapes, n, tiles ),
		DETAIL( "gap %.5f, x %.4f", gap, xy[0] ) );
	me_body_set_position( w, player, 2.0f, 2.0f );
	me_move( w, player, playerShape, 0.0f, -1.0f, STATIC_LAYER, ALL, 0, xy, shapes, normals, 16 );
	gap = player_bottom( w, player ) - 1.5f;
	check( "tiles.one-way", gap >= 0 && gap <= tol, DETAIL( "gap %.5f", gap ) );

	int hit = me_ray_cast( w, 4.0f, 2.0f, 0.0f, -4.0f, STATIC_LAYER, ALL, 0, out );
	check( "tiles.ray", hit == tiles && fabsf( out[1] ) < 1e-4f && fabsf( out[3] - 1.0f ) < 1e-3f && me_ray_cast( w, 4.0f, -0.25f, 0.0f, 2.0f, STATIC_LAYER, ALL, 0, out ) == -1,
		DETAIL( "hit %d at y %.4f", hit, out[1] ) );

	// A box with no friction slides 5 m in a second along the floor: its speed and its height never change.
	int body = me_body_create( w, ME_DYNAMIC, 0.5f, 0.25f, 0.0f, 0 );
	float g[4] = { 0.25f, 0.25f, 0.0f, 0.0f };
	me_shape_create( w, body, ME_BOX, g, 0, 0.0f, STATIC_LAYER, ALL, 0, -1.0f, 0.0f, -1.0f );
	for ( int i = 0; i < 30; ++i )
		me_world_step( w, 1.0f / 60.0f );
	me_body_set_velocity( w, body, 5.0f, 0.0f );
	float slowest = 5.0f, highest = 0.0f, s[6];
	for ( int i = 0; i < 60; ++i )
	{
		me_world_step( w, 1.0f / 60.0f );
		me_body_state( w, body, s );
		slowest = fminf( slowest, s[3] );
		highest = fmaxf( highest, fabsf( s[4] ) );
	}
	check( "tiles.slide", slowest > 4.99f && highest < 0.05f && fabsf( s[0] - 5.5f ) < 0.05f, DETAIL( "slowest %.4f, up %.4f, x %.4f", slowest, highest, s[0] ) );

	// The ids of Box2D, and one of its own functions called through the library's exports.
	unsigned char id[8] = { 0 };
	int ok = me_native_id( w, 0, 0, id ) && me_native_id( w, 1, body, id ) && me_native_id( w, 2, tiles, id ) == 0;
	b2Version v = b2GetVersion();
	check( "native.ids", ok && v.major * 10000 + v.minor * 100 + v.revision == me_box2d_version(), DETAIL( "Box2D %d.%d.%d", v.major, v.minor, v.revision ) );

	me_shape_destroy( w, tiles );
	me_shape_destroy( w, oneWay );
	hit = me_ray_cast( w, 4.0f, 2.0f, 0.0f, -4.0f, STATIC_LAYER, ALL, 0, out );
	check( "tiles.destroyed", hit == -1, DETAIL( "%d", hit ) );
	me_world_destroy( w );
}

int main( int argc, char** argv )
{
	printf( "me_box2d api %d, Box2D %d, skin %.4f m\n", me_api_version(), me_box2d_version(), me_move_skin() );
	check( "version", me_api_version() == ME_API_VERSION && me_box2d_version() == 30200, DETAIL( "%d", me_box2d_version() ) );
	contract_checks();
	capability_checks();
	dynamics_and_joint_checks();
	query_checks();
	tile_checks();
	double ms;
	uint64_t h = bench( 500, 600, &ms );
	printf( "determinism %016llx (500 boxes, 600 steps)\n", (unsigned long long)h );
	if ( argc >= 4 && strcmp( argv[1], "bench" ) == 0 )
	{
		h = bench( atoi( argv[2] ), atoi( argv[3] ), &ms );
		printf( "bench %s boxes %s steps: %.3f ms/step, hash %016llx\n", argv[2], argv[3], ms, (unsigned long long)h );
	}
	printf( "%s (%d failed)\n", failures ? "FAILED" : "all checks passed", failures );
	return failures ? 1 : 0;
}

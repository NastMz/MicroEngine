using System.Numerics;
using MicroEngine.Kernel;
using MicroEngine.Physics;
using MicroEngine.Scenes;

namespace MicroEngine.Testing.Physics;

/// <summary>
/// The suite of the character controllers (<see cref="CharacterController2D"/>, <see cref="TopDownController2D"/>):
/// they are written over the common contract's <see cref="IPhysicsWorld2D.Move"/>, so every backend runs the same
/// rules. Speeds and distances are suite units (a 12 × 14 character, 16-unit tiles), converted with
/// <see cref="PhysicsConformanceHelpers.Scale"/>.
/// </summary>
public abstract partial class CharacterControllerConformance : PhysicsConformanceHelpers
{
    protected const float Dt = 1 / 60f;
    protected static readonly EntityId HeroOwner = new(1, 1), LevelOwner = new(2, 1), PlatformOwner = new(3, 1);

    /// <summary>The suite's motion: gravity 900, fall clamp 400, run 90, jump 300 (suite units per second).</summary>
    protected CharacterMotion Motion(Func<CharacterMotion, CharacterMotion>? edit = null)
    {
        var motion = new CharacterMotion { Gravity = 900 * Scale, MaxFallSpeed = 400 * Scale, MoveSpeed = 90 * Scale, JumpSpeed = 300 * Scale };
        return edit is null ? motion : edit(motion);
    }

    /// <summary>A world with a floor whose top is at height 0 and a wall at x 100..116.</summary>
    protected IPhysicsWorld2D Level()
    {
        var world = CreateWorld();
        Box(world, BodyDescription.Static(), -300, 300, -16, 0, owner: LevelOwner);
        Box(world, BodyDescription.Static(), 100, 116, 0, 96, owner: LevelOwner);
        return world;
    }

    /// <summary>A 12 × 14 character whose bottom is at <paramref name="bottom"/>.</summary>
    protected CharacterController2D Hero(IPhysicsWorld2D world, float x, float bottom, CharacterMotion? motion = null) =>
        CharacterController2D.Create(world, HeroOwner, At(world, x, bottom + 7), new Vector2(12, 14) * Scale, motion ?? Motion());

    protected float Bottom(CharacterController2D hero) => HeightOf(hero.World, hero.Position) - 7;
    protected float X(CharacterController2D hero) => XOf(hero.Position);
    protected float Rise(CharacterController2D hero) => HeightOf(hero.World, hero.Velocity);
    protected float Run(CharacterController2D hero) => XOf(hero.Velocity);

    /// <summary>Steps the character <paramref name="steps"/> times with one input and returns every event raised.</summary>
    protected static CharacterEvents Steps(CharacterController2D hero, int steps, CharacterInput input = default)
    {
        var events = CharacterEvents.None;
        for (int i = 0; i < steps; i++)
        {
            hero.Step(Dt, input);
            events |= hero.Events;
        }
        return events;
    }

    /// <summary>A character standing on the floor at <paramref name="x"/>.</summary>
    protected CharacterController2D Standing(IPhysicsWorld2D world, float x = 0, CharacterMotion? motion = null)
    {
        var hero = Hero(world, x, 2, motion);
        Steps(hero, 30);
        Assert.IsTrue(hero.IsGrounded, "the character settles on the floor");
        return hero;
    }

    private static ulong Hash(IStateHashed state)
    {
        var hash = new StateHasher();
        state.HashState(hash);
        return hash.Value;
    }

    // ------------------------------------------------------------------ gravity and ground

    [TestMethod]
    public void It_falls_with_gravity_up_to_the_fall_clamp_and_lands_once()
    {
        var world = Level();
        var hero = Hero(world, 0, 300);
        float fastest = 0;
        int landed = 0, steps = 0;
        while (!hero.IsGrounded && steps++ < 300)
        {
            hero.Step(Dt, default);
            fastest = MathF.Min(fastest, Rise(hero));
            if ((hero.Events & CharacterEvents.Landed) != 0) landed++;
        }
        Assert.IsTrue(hero.IsGrounded, $"it lands: bottom {Bottom(hero)}");
        Assert.AreEqual(-400, fastest, 0.01f, "the fall speed stops at the clamp");
        Assert.AreEqual(1, landed, "one landing");
        AssertResting(world, Bottom(hero), "resting on the floor");
        Assert.AreEqual(0, Rise(hero), "no vertical speed on the ground");
        Assert.IsTrue(hero.LastMove.OnGround);
        Assert.IsFalse(hero.Ground.IsNone, "the ground shape is known");
    }

    [TestMethod]
    public void A_standing_character_stays_where_it_is_with_no_events()
    {
        var world = Level();
        var hero = Standing(world);
        var at = hero.Position;
        for (int i = 0; i < 120; i++)
        {
            hero.Step(Dt, default);
            Assert.IsTrue(hero.IsGrounded, $"step {i}");
            Assert.AreEqual(CharacterEvents.None, hero.Events, $"step {i}");
        }
        Assert.IsTrue(Vector2.Distance(at, hero.Position) <= Rounding * Scale, $"{at} -> {hero.Position}");
    }

    [TestMethod]
    public void It_runs_at_its_speed_and_stops_at_a_wall()
    {
        var world = Level();
        var hero = Standing(world);
        Steps(hero, 30, new CharacterInput(1));
        Assert.AreEqual(45, X(hero), 1, "half a second at 90 units a second");
        Assert.AreEqual(90, Run(hero), 0.01f);
        int hits = 0;
        for (int i = 0; i < 120; i++)
        {
            hero.Step(Dt, new CharacterInput(1));
            if ((hero.Events & CharacterEvents.HitWall) != 0) hits++;
        }
        AssertResting(world, 100 - (X(hero) + 6), "flush against the wall");
        Assert.IsTrue(hero.IsOnWall);
        Assert.IsTrue(hero.WallNormal.X < -0.9f, $"the wall faces left: {hero.WallNormal}");
        Assert.AreEqual(0, Run(hero), "no speed into the wall");
        Assert.AreEqual(1, hits, "the wall is reported when it is first touched");
        Steps(hero, 10, new CharacterInput(-1));
        Assert.IsFalse(hero.IsOnWall, "it walks away from the wall");
        Assert.IsTrue(hero.IsGrounded);
    }

    [TestMethod]
    public void Acceleration_and_deceleration_ramp_the_speed()
    {
        var world = Level();
        var hero = Standing(world, -200, Motion(m => m with { Acceleration = 180 * Scale, Deceleration = 360 * Scale }));
        Steps(hero, 15, new CharacterInput(1));
        Assert.AreEqual(45, Run(hero), 0.5f, "a quarter of a second at 180 units per second squared");
        Steps(hero, 60, new CharacterInput(1));
        Assert.AreEqual(90, Run(hero), 0.01f, "the speed stops at MoveSpeed");
        Steps(hero, 6, default);
        Assert.AreEqual(90 - 36, Run(hero), 0.5f, "a tenth of a second of deceleration");
        Steps(hero, 30, default);
        Assert.AreEqual(0, Run(hero));
    }

    // ------------------------------------------------------------------ jumping

    private float Apex(CharacterController2D hero, int heldSteps)
    {
        float apex = Bottom(hero);
        var events = CharacterEvents.None;
        for (int i = 0; i < 90; i++)
        {
            hero.Step(Dt, new CharacterInput(0, JumpPressed: i == 0, JumpHeld: i < heldSteps));
            events |= hero.Events;
            if (i == 0)
            {
                Assert.AreEqual(CharacterEvents.Jumped | CharacterEvents.LeftGround, hero.Events, "the jump step");
                Assert.IsTrue(hero.IsJumping && !hero.IsGrounded);
            }
            apex = MathF.Max(apex, Bottom(hero));
        }
        Assert.IsTrue((events & CharacterEvents.Landed) != 0 && hero.IsGrounded, "it comes back down");
        return apex;
    }

    [TestMethod]
    public void A_held_jump_reaches_the_height_its_speed_and_gravity_give()
    {
        var hero = Standing(Level());
        float apex = Apex(hero, heldSteps: 90);
        Assert.IsTrue(apex is > 44 and < 51, $"300² / (2 × 900) is 50: {apex}");
    }

    [TestMethod]
    public void Releasing_jump_early_makes_a_lower_jump()
    {
        float held = Apex(Standing(Level()), heldSteps: 90);
        float tapped = Apex(Standing(Level()), heldSteps: 3);
        Assert.IsTrue(tapped < held - 10, $"tapped {tapped}, held {held}");
        float fixedHeight = Apex(Standing(Level(), motion: Motion(m => m with { JumpReleaseGravityScale = 1 })), heldSteps: 3);
        Assert.AreEqual(held, fixedHeight, 0.01f, "a scale of 1 turns variable height off");
    }

    /// <summary>A character on a ledge (top at height 40, x -50..0) that walks right until it leaves it.</summary>
    private CharacterController2D OffTheLedge(out int steps)
    {
        var world = Level();
        Box(world, BodyDescription.Static(), -50, 0, 30, 40, owner: LevelOwner);
        var hero = Hero(world, -10, 42);
        Steps(hero, 30);
        Assert.IsTrue(hero.IsGrounded && Bottom(hero) > 39, "standing on the ledge");
        steps = 0;
        while (hero.IsGrounded && steps++ < 60) hero.Step(Dt, new CharacterInput(1));
        Assert.IsFalse(hero.IsGrounded, "it walked off");
        Assert.AreEqual(CharacterEvents.LeftGround, hero.Events, "leaving the ledge is reported, with no jump");
        return hero;
    }

    [TestMethod]
    public void Coyote_time_allows_a_jump_shortly_after_leaving_a_ledge()
    {
        var hero = OffTheLedge(out _);
        Steps(hero, 3, new CharacterInput(1));
        hero.Step(Dt, new CharacterInput(1, JumpPressed: true, JumpHeld: true));
        Assert.IsTrue((hero.Events & CharacterEvents.Jumped) != 0, "0.07 s after the ledge, inside the 0.1 s of coyote time");
        Assert.IsTrue(Rise(hero) > 0);

        var late = OffTheLedge(out _);
        Steps(late, 9, new CharacterInput(1));
        late.Step(Dt, new CharacterInput(1, JumpPressed: true, JumpHeld: true));
        Assert.IsTrue((late.Events & CharacterEvents.Jumped) == 0, "0.17 s after the ledge is too late");
        Assert.IsTrue(Rise(late) < 0);
    }

    [TestMethod]
    public void A_jump_pressed_just_before_landing_is_buffered()
    {
        int landing = 0;
        var probe = Hero(Level(), 0, 60);
        while (!probe.IsGrounded && landing < 200) { probe.Step(Dt, default); landing++; }

        CharacterEvents Fall(int pressAt)
        {
            var hero = Hero(Level(), 0, 60);
            var events = CharacterEvents.None;
            for (int i = 1; i <= landing + 8; i++)
            {
                hero.Step(Dt, new CharacterInput(0, JumpPressed: i == pressAt, JumpHeld: i >= pressAt));
                events |= hero.Events;
            }
            return events;
        }
        Assert.IsTrue((Fall(landing - 3) & CharacterEvents.Jumped) != 0, "pressed 0.05 s before landing: it jumps on landing");
        Assert.IsTrue((Fall(landing - 12) & CharacterEvents.Jumped) == 0, "pressed 0.2 s before landing: forgotten");
    }

    [TestMethod]
    public void A_ceiling_stops_the_rise_and_is_reported()
    {
        var world = Level();
        Box(world, BodyDescription.Static(), -50, 50, 30, 40, owner: LevelOwner);
        var hero = Standing(world);
        bool hit = false;
        for (int i = 0; i < 60 && !hit; i++)
        {
            hero.Step(Dt, new CharacterInput(0, JumpPressed: i == 0, JumpHeld: true));
            hit = (hero.Events & CharacterEvents.HitCeiling) != 0;
        }
        Assert.IsTrue(hit, "the head reaches the ceiling");
        AssertResting(world, 30 - (Bottom(hero) + 14), "under the ceiling");
        Assert.AreEqual(0, Rise(hero), "the rise stops");
        Assert.IsFalse(hero.IsJumping);
        Assert.IsTrue((Steps(hero, 60) & CharacterEvents.Landed) != 0);
    }

    [TestMethod]
    public void Air_jumps_are_counted_and_given_back_on_landing()
    {
        var hero = Standing(Level(), motion: Motion(m => m with { AirJumps = 1 }));
        hero.Step(Dt, new CharacterInput(0, JumpPressed: true, JumpHeld: true));
        Steps(hero, 15, new CharacterInput(0, JumpHeld: true));
        hero.Step(Dt, new CharacterInput(0, JumpPressed: true, JumpHeld: true));
        Assert.IsTrue((hero.Events & CharacterEvents.Jumped) != 0 && Rise(hero) > 250, "one jump in the air");
        Steps(hero, 5, new CharacterInput(0, JumpHeld: true));
        hero.Step(Dt, new CharacterInput(0, JumpPressed: true, JumpHeld: true));
        Assert.IsTrue((hero.Events & CharacterEvents.Jumped) == 0, "no second one");
        Steps(hero, 120);
        Assert.IsTrue(hero.IsGrounded);
        hero.Step(Dt, new CharacterInput(0, JumpPressed: true, JumpHeld: true));
        Steps(hero, 10, new CharacterInput(0, JumpHeld: true));
        hero.Step(Dt, new CharacterInput(0, JumpPressed: true, JumpHeld: true));
        Assert.IsTrue((hero.Events & CharacterEvents.Jumped) != 0, "landing gives the air jump back");
    }

    [TestMethod]
    public void Launch_throws_the_character_whatever_it_stands_on()
    {
        var world = Level();
        var hero = Standing(world);
        hero.Launch(By(world, 0, 500));
        Assert.IsFalse(hero.IsGrounded);
        hero.Step(Dt, default);
        Assert.AreEqual(500 - 15, Rise(hero), 0.01f, "a launch is not cut short by a released jump button");
        Assert.IsTrue(Bottom(hero) > 7);
    }

    // ------------------------------------------------------------------ walls

    [TestMethod]
    public void A_wall_slide_caps_the_fall_and_a_wall_jump_pushes_away()
    {
        var world = Level();
        var hero = Hero(world, 93.9f, 90, Motion(m => m with { WallSlideSpeed = 40 * Scale, WallJumpSpeed = new Vector2(120, 280) * Scale }));
        float fastest = 0;
        for (int i = 0; i < 40; i++)
        {
            hero.Step(Dt, new CharacterInput(1));
            fastest = MathF.Min(fastest, Rise(hero));
        }
        Assert.IsTrue(hero.IsOnWall && !hero.IsGrounded, $"sliding down the wall: bottom {Bottom(hero)}");
        Assert.AreEqual(-40, fastest, 0.01f, "the slide speed");
        hero.Step(Dt, new CharacterInput(1, JumpPressed: true, JumpHeld: true));
        Assert.IsTrue((hero.Events & CharacterEvents.Jumped) != 0);
        Assert.AreEqual(-120, Run(hero), 0.01f, "away from the wall");
        Assert.IsTrue(Rise(hero) > 250);
        float x = X(hero);
        hero.Step(Dt, new CharacterInput(0, JumpHeld: true));
        Assert.IsTrue(X(hero) < x, "it leaves the wall");

        var plain = Hero(Level(), 93.9f, 90);
        Steps(plain, 15, new CharacterInput(1));
        Assert.IsTrue(Rise(plain) < -100, "no slide unless the motion asks for one");
        plain.Step(Dt, new CharacterInput(1, JumpPressed: true));
        Assert.IsTrue((plain.Events & CharacterEvents.Jumped) == 0, "no wall jump unless the motion asks for one");
    }

    // ------------------------------------------------------------------ platforms

    [TestMethod]
    public void Drop_falls_through_a_one_way_platform_and_never_through_solid_ground()
    {
        var world = Level();
        if ((world.Capabilities & PhysicsCapabilities.OneWayPlatforms) == 0) Assert.Inconclusive($"{world.Backend} has no one-way platforms");
        Box(world, BodyDescription.Static(), -50, 50, 30, 34, s => s with { OneWay = true }, LevelOwner);
        var hero = Hero(world, 0, 36);
        Steps(hero, 30);
        Assert.IsTrue(hero.IsGrounded && Bottom(hero) > 33, $"standing on the platform: {Bottom(hero)}");

        hero.Step(Dt, new CharacterInput(0, JumpPressed: true, Drop: true));
        Assert.AreEqual(CharacterEvents.DroppedThrough | CharacterEvents.LeftGround, hero.Events, "drop wins over jump on a platform");
        var events = Steps(hero, 60);
        Assert.IsTrue((events & CharacterEvents.Landed) != 0 && hero.IsGrounded);
        AssertResting(world, Bottom(hero), "it fell through to the floor");

        hero.Step(Dt, new CharacterInput(0, Drop: true));
        Assert.AreEqual(CharacterEvents.None, hero.Events, "solid ground: nothing to drop through");
        Assert.IsTrue(hero.IsGrounded);
        hero.Step(Dt, new CharacterInput(0, JumpPressed: true, JumpHeld: true, Drop: true));
        Assert.IsTrue((hero.Events & CharacterEvents.Jumped) != 0, "on solid ground the jump still happens");
    }

    [TestMethod]
    public void It_is_carried_by_the_platform_it_stands_on()
    {
        var world = Level();
        var (platform, _) = Box(world, BodyDescription.Kinematic(default), -20, 20, 20, 24, owner: PlatformOwner);
        var hero = Hero(world, 0, 26);
        Steps(hero, 30);
        Assert.IsTrue(hero.IsGrounded && Bottom(hero) > 23, "standing on the platform");

        void Carry(float dx, float dh, int steps, string what)
        {
            for (int i = 0; i < steps; i++)
            {
                world.SetPosition(platform, world.PositionOf(platform) + By(world, dx, dh));
                hero.Step(Dt, default);
                Assert.IsTrue(hero.IsGrounded, $"{what}, step {i}: bottom {Bottom(hero)}");
                Assert.AreEqual(CharacterEvents.None, hero.Events, $"{what}, step {i}");
            }
        }
        Carry(1, 0, 60, "sideways");
        Assert.AreEqual(60, X(hero), 0.01f + Tolerance(world), "carried 60 units sideways");
        Carry(0, 0.5f, 40, "up");
        AssertResting(world, Bottom(hero) - 44, "carried 20 units up");
        Carry(0, -0.5f, 40, "down");
        AssertResting(world, Bottom(hero) - 24, "carried back down");
        Assert.AreEqual(60, X(hero), 0.01f + Tolerance(world));
    }

    // ------------------------------------------------------------------ state

    [TestMethod]
    public void The_same_inputs_give_the_same_state_hash_and_the_hash_follows_the_state()
    {
        CharacterInput InputAt(int i) => new(i % 90 < 60 ? 1 : -1, JumpPressed: i % 40 == 5, JumpHeld: i % 40 is >= 5 and < 20);
        var a = Standing(Level());
        var b = Standing(Level());
        ulong before = Hash(a);
        bool changed = false;
        for (int i = 0; i < 300; i++)
        {
            a.Step(Dt, InputAt(i));
            b.Step(Dt, InputAt(i));
            Assert.AreEqual(Hash(a), Hash(b), $"step {i}");
            Assert.AreEqual(a.Position, b.Position, $"step {i}");
            changed |= Hash(a) != before;
        }
        Assert.IsTrue(changed, "the hash changes with the character's state");
        ulong grounded = Hash(a);
        a.Velocity += By(a.World, 1, 0);
        Assert.AreNotEqual(grounded, Hash(a), "velocity is hashed");
    }

    [TestMethod]
    public void Stepping_a_character_allocates_nothing()
    {
        var hero = Standing(Level());
        int i = 0;
        AllocationAssert.None(() =>
        {
            i++;
            hero.Step(Dt, new CharacterInput(i % 60 < 30 ? 1 : -1, JumpPressed: i % 45 == 0, JumpHeld: true));
        }, because: "characters step every fixed step");
    }

    [TestMethod]
    public void Invalid_motion_and_bodies_are_refused()
    {
        var world = Level();
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new CharacterMotion { Gravity = -1 }.Validate());
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new CharacterMotion { MoveSpeed = float.NaN }.Validate());
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new CharacterMotion { AirJumps = -1 }.Validate());
        var (still, _) = Box(world, BodyDescription.Static(), 200, 210, 0, 10);
        Assert.ThrowsExactly<InvalidOperationException>(() => new CharacterController2D(world, still));
        Assert.ThrowsExactly<InvalidOperationException>(() => new CharacterController2D(world, BodyId.None));
        var hero = Standing(world);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => hero.Step(float.NaN, default));
        var at = hero.Position;
        hero.Step(0, new CharacterInput(1, JumpPressed: true));
        Assert.AreEqual(at, hero.Position, "a step of no time moves nothing");
    }

    // ------------------------------------------------------------------ top-down

    [TestMethod]
    public void A_top_down_character_moves_at_its_speed_and_slides_along_walls()
    {
        var world = CreateWorld();
        Box(world, BodyDescription.Static(), 100, 116, -200, 200, owner: LevelOwner);
        var hero = TopDownController2D.Create(world, HeroOwner, At(world, 0, 0), new Vector2(12, 14) * Scale, new TopDownMotion { MoveSpeed = 90 * Scale });
        for (int i = 0; i < 30; i++) hero.Step(Dt, new Vector2(1, 0));
        Assert.AreEqual(45, XOf(hero.Position), 1);
        var diagonal = By(world, 1, 1) / Scale; // longer than 1: counts as full speed
        for (int i = 0; i < 120; i++) hero.Step(Dt, diagonal);
        AssertResting(world, 100 - (XOf(hero.Position) + 6), "flush against the wall");
        Assert.IsTrue(HeightOf(world, hero.Position) > 100, $"it slides along the wall: {hero.Position}");
        Assert.AreEqual(0, XOf(hero.Velocity), 1e-4f, "no speed into the wall");
        Assert.IsTrue(hero.LastMove.HitWall || hero.LastMove.TotalContacts > 0);
        float speed = hero.Velocity.Length() / Scale;
        Assert.IsTrue(speed <= 90.01f, $"a diagonal is no faster: {speed}");
        ulong hash = Hash(hero);
        hero.Step(Dt, new Vector2(-1, 0));
        Assert.AreNotEqual(hash, Hash(hero));
        AllocationAssert.None(() => hero.Step(Dt, new Vector2(0, 1)));
    }

    [TestMethod]
    public void A_top_down_character_accelerates_and_brakes()
    {
        var world = CreateWorld();
        var hero = TopDownController2D.Create(world, HeroOwner, default, new Vector2(12, 14) * Scale,
            new TopDownMotion { MoveSpeed = 90 * Scale, Acceleration = 180 * Scale, Deceleration = 360 * Scale });
        for (int i = 0; i < 15; i++) hero.Step(Dt, new Vector2(1, 0));
        Assert.AreEqual(45, XOf(hero.Velocity), 0.5f);
        for (int i = 0; i < 60; i++) hero.Step(Dt, Vector2.Zero);
        Assert.AreEqual(Vector2.Zero, hero.Velocity);
    }
}

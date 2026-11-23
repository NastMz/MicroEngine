using System.Numerics;
using MicroEngine.Physics;

namespace MicroEngine.Testing.Physics;

// The rules a platformer with a feel of its own needs from the controller: a gravity per phase of the jump, a skid when
// turning, corner correction, a ceiling that sends the character down, a run-up bonus, and solids that depend on how
// the character meets them. The last test puts them together the way a classic platform game does.
public abstract partial class CharacterControllerConformance
{
    /// <summary>The numbers of a classic platform game (suite units): four gravities, a skid, corner correction.</summary>
    private CharacterMotion Classic() => new()
    {
        MoveSpeed = 92 * Scale, Acceleration = 520 * Scale, Deceleration = 620 * Scale, AirControl = 0.8f,
        SkidDeceleration = 1500 * Scale, SkidSpeed = 30 * Scale,
        JumpSpeed = 302 * Scale, RunJumpBonus = 26 * Scale,
        Gravity = 640 * Scale, JumpReleaseGravityScale = 1900f / 640, ApexGravityScale = 380f / 640, ApexSpeed = 48 * Scale, FallGravityScale = 1050f / 640,
        MaxFallSpeed = 270 * Scale, CoyoteTime = 0.1f, JumpBuffer = 0.12f,
        CornerCorrection = 4 * Scale, CeilingReboundSpeed = 20 * Scale,
    };

    [TestMethod]
    public void Gravity_has_a_value_for_each_phase_of_a_jump()
    {
        var world = Level();
        var hero = Standing(world, motion: Classic());
        var held = new CharacterInput(0, JumpPressed: false, JumpHeld: true);
        hero.Step(Dt, new CharacterInput(0, JumpPressed: true, JumpHeld: true));
        int rising = 0, apexUp = 0, apexDown = 0, falling = 0;
        for (int i = 0; i < 200 && !hero.IsGrounded; i++)
        {
            float before = Rise(hero);
            hero.Step(Dt, held);
            if (hero.IsGrounded) break;
            float gravity = (before - Rise(hero)) / Dt;
            if (Rise(hero) <= -270 + 0.01f) continue; // at the fall clamp
            if (before >= 48)
            {
                Assert.AreEqual(640, gravity, 1, $"rising at {before}");
                rising++;
            }
            else if (before > 0)
            {
                Assert.AreEqual(380, gravity, 1, $"near the top, rising at {before}");
                apexUp++;
            }
            else if (before > -48)
            {
                Assert.AreEqual(380, gravity, 1, $"near the top, falling at {before} with jump held");
                apexDown++;
            }
            else
            {
                Assert.AreEqual(1050, gravity, 1, $"falling at {before}");
                falling++;
            }
        }
        Assert.IsTrue(rising > 5 && apexUp > 2 && apexDown > 2 && falling > 2, $"every phase was seen: {rising}, {apexUp}, {apexDown}, {falling}");

        // Released while rising: the release gravity; falling with jump released: the fall gravity from the top on.
        var other = Standing(Level(), motion: Classic());
        other.Step(Dt, new CharacterInput(0, JumpPressed: true, JumpHeld: true));
        Steps(other, 5, held);
        float up = Rise(other);
        other.Step(Dt, default);
        Assert.AreEqual(1900, (up - Rise(other)) / Dt, 1, "released while rising");
        while (Rise(other) > 0) other.Step(Dt, default);
        float down = Rise(other);
        other.Step(Dt, default);
        Assert.AreEqual(1050, (down - Rise(other)) / Dt, 1, "falling with jump released has no floaty top");
    }

    [TestMethod]
    public void Reversing_at_speed_skids_before_it_turns()
    {
        var world = Level();
        var hero = Standing(world, x: -200, motion: Classic());
        Steps(hero, 40, new CharacterInput(1));
        Assert.AreEqual(92, Run(hero), 0.01f, "at full speed");
        Assert.IsFalse(hero.IsSkidding);
        int skidded = 0;
        float previous = Run(hero);
        while (true)
        {
            hero.Step(Dt, new CharacterInput(-1));
            if (!hero.IsSkidding) break;
            Assert.IsTrue((hero.Events & CharacterEvents.Skidded) != 0, "a skidding step says so");
            Assert.AreEqual(1500 * Dt, previous - Run(hero), 0.01f, "it brakes at the skid's deceleration");
            Assert.IsTrue(Run(hero) >= 0, "it does not turn while it skids");
            previous = Run(hero);
            skidded++;
        }
        Assert.IsTrue(skidded is >= 2 and <= 4, $"from 92 to under 30 at 1500 a second squared: {skidded} steps");
        Assert.IsTrue(previous <= 30 + 0.01f, $"the skid ends under the skid speed: {previous}");
        Assert.AreEqual(520 * Dt, previous - Run(hero), 0.01f, "then it accelerates the other way as usual");
        Steps(hero, 40, new CharacterInput(-1));
        Assert.AreEqual(-92, Run(hero), 0.01f);

        // In the air nothing skids, and without a skid deceleration nothing does.
        var plain = Standing(Level(), x: -200, motion: Classic() with { SkidDeceleration = 0 });
        Steps(plain, 40, new CharacterInput(1));
        Assert.AreEqual(CharacterEvents.None, Steps(plain, 3, new CharacterInput(-1)) & CharacterEvents.Skidded);
        Assert.IsFalse(plain.IsSkidding);
    }

    [TestMethod]
    public void A_head_that_clips_a_corner_is_nudged_past_it()
    {
        // A block over the character's right side: its head overlaps the block's width by 3 units.
        IPhysicsWorld2D Scene()
        {
            var world = Level();
            Box(world, BodyDescription.Static(), 3, 40, 30, 46, owner: LevelOwner);
            return world;
        }
        var nudged = Standing(Scene(), motion: Classic());
        var events = Steps(nudged, 1, new CharacterInput(0, JumpPressed: true, JumpHeld: true));
        events |= Steps(nudged, 20, new CharacterInput(0, JumpHeld: true));
        Assert.IsTrue((events & CharacterEvents.CornerCorrected) != 0, $"it was nudged: {events}");
        Assert.AreEqual(CharacterEvents.None, events & CharacterEvents.HitCeiling, "and did not hit its head");
        Assert.IsTrue(Bottom(nudged) > 30, $"it rose past the block: bottom {Bottom(nudged)}");
        Assert.IsTrue(X(nudged) <= -3 + Tolerance(nudged.World) && X(nudged) >= -4 - Tolerance(nudged.World), $"just far enough to clear the corner: x {X(nudged)}");

        var plain = Standing(Scene(), motion: Classic() with { CornerCorrection = 0 });
        events = Steps(plain, 1, new CharacterInput(0, JumpPressed: true, JumpHeld: true));
        events |= Steps(plain, 20, new CharacterInput(0, JumpHeld: true));
        Assert.IsTrue((events & CharacterEvents.HitCeiling) != 0, "without correction the same jump hits the block");
        Assert.AreEqual(0, X(plain), Rounding);

        // Squarely under a block there is nothing to correct: it is a ceiling.
        var under = Level();
        Box(under, BodyDescription.Static(), -40, 40, 30, 46, owner: LevelOwner);
        var blocked = Standing(under, motion: Classic());
        events = Steps(blocked, 1, new CharacterInput(0, JumpPressed: true, JumpHeld: true));
        events |= Steps(blocked, 20, new CharacterInput(0, JumpHeld: true));
        Assert.IsTrue((events & CharacterEvents.HitCeiling) != 0);
        Assert.AreEqual(CharacterEvents.None, events & CharacterEvents.CornerCorrected);
        Assert.AreEqual(0, X(blocked), Rounding, "it was not moved sideways");
    }

    [TestMethod]
    public void A_ceiling_sends_it_down_at_the_rebound_speed_and_a_run_up_jumps_higher()
    {
        var world = Level();
        Box(world, BodyDescription.Static(), -40, 40, 30, 46, owner: LevelOwner);
        var hero = Standing(world, motion: Classic());
        hero.Step(Dt, new CharacterInput(0, JumpPressed: true, JumpHeld: true));
        for (int i = 0; i < 30 && (hero.Events & CharacterEvents.HitCeiling) == 0; i++) hero.Step(Dt, new CharacterInput(0, JumpHeld: true));
        Assert.IsTrue((hero.Events & CharacterEvents.HitCeiling) != 0);
        Assert.AreEqual(-20, Rise(hero), 0.01f, "it leaves the ceiling going down");

        var still = Standing(Level(), x: -200, motion: Classic());
        still.Step(Dt, new CharacterInput(0, JumpPressed: true, JumpHeld: true));
        var running = Standing(Level(), x: -200, motion: Classic());
        Steps(running, 40, new CharacterInput(1));
        running.Step(Dt, new CharacterInput(1, JumpPressed: true, JumpHeld: true));
        // One step of rise gravity has passed since each jump started.
        Assert.AreEqual(302 - 640 * Dt, Rise(still), 0.01f);
        Assert.AreEqual(302 + 26 - 640 * Dt, Rise(running), 0.01f, "at full speed the whole bonus");
    }

    [TestMethod]
    public void It_reports_how_long_it_was_in_the_air_and_how_fast_it_landed()
    {
        var world = Level();
        var hero = Hero(world, 0, 100, Classic());
        Assert.AreEqual(0, hero.AirTime);
        int steps = 0;
        float fastest = 0;
        while ((hero.Events & CharacterEvents.Landed) == 0 && steps < 300)
        {
            hero.Step(Dt, default);
            fastest = MathF.Max(fastest, -Rise(hero));
            steps++;
        }
        Assert.IsTrue(steps > 10, "it fell for a while");
        Assert.AreEqual((steps - 1) * Dt, hero.AirTime, 1e-4f, "on the step it lands, the length of the fall");
        Assert.AreEqual(fastest, hero.LandingSpeed / Scale, 20, $"the speed it came down at: {hero.LandingSpeed}");
        Assert.IsTrue(hero.LandingSpeed > 0);
        hero.Step(Dt, default);
        Assert.AreEqual(0, hero.AirTime, "on the ground again");
        Assert.IsTrue(hero.LandingSpeed > 0, "the last landing stays readable");
    }

    [TestMethod]
    public void A_solid_can_depend_on_how_the_character_meets_it()
    {
        var world = Level();
        if ((world.Capabilities & PhysicsCapabilities.FilterPredicates) == 0) Assert.Inconclusive("the backend has no filter predicates");
        // A hidden block at x 30..46, 30 to 46 above the floor: solid only for a head that comes up from below it.
        var (_, block) = Box(world, BodyDescription.Static(), 30, 46, 30, 46, owner: LevelOwner);
        bool revealed = false;
        var hero = Standing(world, motion: Classic());
        hero.Solid = (character, shape) =>
        {
            if (shape != block || revealed) return true;
            float head = HeightOf(world, character.Position) + 7;
            return HeightOf(world, character.Velocity) > 0 && head <= 30 + Tolerance(world);
        };

        // Falling onto it from above goes through it: the character lands on the floor under it.
        hero.Teleport(At(world, 38, 67));
        var events = Steps(hero, 90);
        Assert.IsTrue(hero.IsGrounded && hero.Ground != block, "it fell through the hidden block");
        AssertResting(world, Bottom(hero), "onto the floor");
        Assert.AreEqual(CharacterEvents.None, events & (CharacterEvents.HitCeiling | CharacterEvents.HitWall), events.ToString());

        // From below it is a ceiling, and the game learns which shape was hit.
        hero.Step(Dt, new CharacterInput(0, JumpPressed: true, JumpHeld: true));
        bool bumped = false;
        for (int i = 0; i < 30 && !bumped; i++)
        {
            hero.Step(Dt, new CharacterInput(0, JumpHeld: true));
            if ((hero.Events & CharacterEvents.HitCeiling) == 0) continue;
            foreach (var contact in hero.LastMove.Contacts) bumped |= contact.Shape == block;
        }
        Assert.IsTrue(bumped, "the head found the hidden block");
        Assert.IsTrue(Bottom(hero) + 14 <= 30 + Tolerance(world), $"it stopped under it: head {Bottom(hero) + 14}");

        // Once revealed it is a block like any other: it can be stood on and blocks from the side.
        revealed = true;
        Steps(hero, 60);
        hero.Teleport(At(world, 0, 7.5f));
        Steps(hero, 10);
        hero.Teleport(At(world, 38, 60));
        hero.Velocity = Vector2.Zero;
        Steps(hero, 60);
        Assert.IsTrue(hero.IsGrounded && hero.Ground == block, "and stands on it");
    }

    [TestMethod]
    public void The_rules_of_a_classic_platformer_fit_together()
    {
        var world = Level();
        // A ledge to clip (its corner 3 units over the character's head line), and a wall to stop at.
        Box(world, BodyDescription.Static(), -197, -150, 34, 50, owner: LevelOwner);
        var hero = Standing(world, x: -200, motion: Classic());
        var seen = CharacterEvents.None;

        // A jump whose head clips the ledge's corner: nudged, not stopped.
        hero.Step(Dt, new CharacterInput(0, JumpPressed: true, JumpHeld: true));
        seen |= hero.Events;
        float top = 0;
        for (int i = 0; i < 120 && !hero.IsGrounded; i++)
        {
            hero.Step(Dt, new CharacterInput(0, JumpHeld: true));
            seen |= hero.Events;
            top = MathF.Max(top, Bottom(hero));
        }
        Assert.IsTrue((seen & CharacterEvents.CornerCorrected) != 0 && (seen & CharacterEvents.HitCeiling) == 0, seen.ToString());
        Assert.IsTrue(top > 60, $"a full jump: {top}");
        Assert.IsTrue(hero.IsGrounded && (seen & CharacterEvents.Landed) != 0);
        Assert.IsTrue(hero.AirTime > 0.5f, $"the fall was timed for the landing: {hero.AirTime}");

        // Run right, reverse (skid), run left, then a short hop: released early, it rises less than half as high.
        seen = Steps(hero, 40, new CharacterInput(1));
        seen |= Steps(hero, 4, new CharacterInput(-1));
        Assert.IsTrue((seen & CharacterEvents.Skidded) != 0);
        Steps(hero, 30);
        Assert.AreEqual(0, Run(hero), 0.01f, "friction stops it");
        float ground = Bottom(hero);
        hero.Step(Dt, new CharacterInput(0, JumpPressed: true, JumpHeld: true));
        float hop = 0;
        for (int i = 0; i < 120 && !hero.IsGrounded; i++)
        {
            hero.Step(Dt, default);
            hop = MathF.Max(hop, Bottom(hero) - ground);
        }
        Assert.IsTrue(hop > 2 && hop < top / 2, $"a tap is a hop: {hop} against {top}");
        Assert.IsTrue(hero.IsGrounded);
        AssertResting(world, Bottom(hero), "back on the floor");
    }
}

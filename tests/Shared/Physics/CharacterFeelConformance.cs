using System.Numerics;
using MicroEngine.Physics;

namespace MicroEngine.Testing.Physics;

// What a platformer with a classic feel needs beyond the rules of CharacterRulesConformance: a run, the air's own
// numbers, momentum, a landing a little early, the order of a jump and the ground's carry, a drop that needs a jump, a
// launch the player shapes, a released jump cut at once, ground the game decides, and a state that can be kept and
// put back. Every rule runs on every backend.
public abstract partial class CharacterControllerConformance
{
    /// <summary>The classic numbers with a run and the air's own numbers.</summary>
    private CharacterMotion Feel() => Classic() with
    {
        RunSpeed = 152 * Scale, RunAcceleration = 600 * Scale, OverspeedDeceleration = 620 * Scale,
        AirAcceleration = 430 * Scale, AirDeceleration = 140 * Scale, AirOverspeedDeceleration = 0,
    };

    [TestMethod]
    public void Run_is_a_second_speed_with_its_own_acceleration()
    {
        var hero = Standing(Level(), x: -250, motion: Feel());
        hero.Step(Dt, new CharacterInput(1));
        Assert.AreEqual(520 * Dt, Run(hero), 0.01f, "walking speeds up at the walk's acceleration");
        Steps(hero, 40, new CharacterInput(1));
        Assert.AreEqual(92, Run(hero), 0.01f, "the walk's speed");
        hero.Step(Dt, new CharacterInput(1, Run: true));
        Assert.AreEqual(92 + 600 * Dt, Run(hero), 0.01f, "running speeds up at the run's acceleration");
        Steps(hero, 40, new CharacterInput(1, Run: true));
        Assert.AreEqual(152, Run(hero), 0.01f, "the run's speed");

        // Without a run speed the run intent changes nothing.
        var plain = Standing(Level(), x: -250, motion: Classic());
        Steps(plain, 60, new CharacterInput(1, Run: true));
        Assert.AreEqual(92, Run(plain), 0.01f);
    }

    [TestMethod]
    public void The_run_up_bonus_is_measured_against_the_top_speed_whether_run_is_held_or_not()
    {
        // At the walk's speed, with and without run held, the jump leaves with the same share of the bonus: 92 / 152.
        float expected = 302 + 26 * (92f / 152) - 640 * Dt;
        var walking = Standing(Level(), x: -250, motion: Feel());
        Steps(walking, 40, new CharacterInput(1));
        walking.Step(Dt, new CharacterInput(1, JumpPressed: true, JumpHeld: true));
        Assert.AreEqual(expected, Rise(walking), 0.01f, "a walking jump");

        var running = Standing(Level(), x: -250, motion: Feel());
        Steps(running, 60, new CharacterInput(1, Run: true));
        running.Step(Dt, new CharacterInput(1, JumpPressed: true, JumpHeld: true, Run: true));
        Assert.AreEqual(302 + 26 - 640 * Dt, Rise(running), 0.01f, "at the run's speed, the whole bonus");
    }

    [TestMethod]
    public void The_air_has_its_own_acceleration_and_drag()
    {
        var hero = Standing(Level(), x: -250, motion: Feel());
        hero.Step(Dt, new CharacterInput(0, JumpPressed: true, JumpHeld: true));
        hero.Step(Dt, new CharacterInput(1, JumpHeld: true));
        Assert.AreEqual(430 * Dt, Run(hero), 0.01f, "steering in the air");
        Steps(hero, 5, new CharacterInput(1, JumpHeld: true));
        float before = Run(hero);
        hero.Step(Dt, new CharacterInput(0, JumpHeld: true));
        Assert.AreEqual(140 * Dt, before - Run(hero), 0.01f, "letting go in the air");
        Assert.IsFalse(hero.IsGrounded);

        // A motion with only the share keeps using it.
        var shared = Standing(Level(), x: -250, motion: Classic());
        shared.Step(Dt, new CharacterInput(0, JumpPressed: true, JumpHeld: true));
        shared.Step(Dt, new CharacterInput(1, JumpHeld: true));
        Assert.AreEqual(520 * 0.8f * Dt, Run(shared), 0.01f);
    }

    [TestMethod]
    public void Speed_above_the_cap_is_braked_on_the_ground_and_kept_in_the_air()
    {
        var input = new CharacterInput(1, Run: true);
        var hero = Standing(Level(), x: -280, motion: Feel());
        Steps(hero, 60, input);
        Assert.AreEqual(152, Run(hero), 0.01f);
        // Run released, still pushing: the ground takes the extra speed away at its own rate.
        hero.Step(Dt, new CharacterInput(1));
        Assert.AreEqual(152 - 620 * Dt, Run(hero), 0.01f, "braked by the overspeed deceleration");
        Steps(hero, 10, new CharacterInput(1));
        Assert.AreEqual(92, Run(hero), 0.01f, "down to the walk's speed and no further");

        // In the air it is kept for the whole jump.
        Steps(hero, 40, input);
        hero.Step(Dt, new CharacterInput(1, JumpPressed: true, JumpHeld: true, Run: true));
        for (int i = 0; i < 20; i++)
        {
            hero.Step(Dt, new CharacterInput(1, JumpHeld: true));
            Assert.IsFalse(hero.IsGrounded, $"in the air at step {i}");
            Assert.AreEqual(152, Run(hero), 0.01f, $"the run's momentum at step {i}");
        }
        // Turning in the air is ordinary air control.
        float before = Run(hero);
        hero.Step(Dt, new CharacterInput(-1, JumpHeld: true));
        Assert.AreEqual(430 * Dt, before - Run(hero), 0.01f);

        // Without the two numbers the acceleration pulls the speed down to the cap, on the ground and in the air.
        var plain = Standing(Level(), x: -280, motion: Feel() with { OverspeedDeceleration = null, AirOverspeedDeceleration = null });
        Steps(plain, 60, input);
        plain.Step(Dt, new CharacterInput(1));
        Assert.AreEqual(152 - 520 * Dt, Run(plain), 0.01f);
        Steps(plain, 60, input);
        plain.Step(Dt, new CharacterInput(1, JumpPressed: true, JumpHeld: true, Run: true));
        plain.Step(Dt, new CharacterInput(1, JumpHeld: true));
        Assert.AreEqual(152 - 430 * Dt, Run(plain), 0.01f);
    }

    [TestMethod]
    public void A_landing_reach_lands_a_step_early_and_never_leaves_the_character_in_the_air()
    {
        int earlier = 0;
        for (int start = 0; start < 12; start++)
        {
            float from = 30 + start * 0.13f;
            var contact = Hero(Level(), 0, from, Classic());
            var reach = Hero(Level(), 0, from, Classic() with { LandingReach = 1 * Scale });
            int landedOnContact = -1, landedInReach = -1;
            for (int i = 0; i < 120; i++)
            {
                contact.Step(Dt, default);
                reach.Step(Dt, default);
                if (landedOnContact < 0 && (contact.Events & CharacterEvents.Landed) != 0) landedOnContact = i;
                if (landedInReach < 0 && (reach.Events & CharacterEvents.Landed) != 0)
                {
                    landedInReach = i;
                    Assert.IsTrue(reach.IsGrounded && reach.Ground != ShapeId.None, "it stands on the ground it reached");
                    Assert.AreEqual(0, Rise(reach), "its fall speed is gone");
                    Assert.IsTrue(Bottom(reach) >= -Rounding && Bottom(reach) <= 1 + Tolerance(reach.World), $"within the reach of the floor: {Bottom(reach)}");
                    Assert.IsTrue(reach.LandingSpeed > 0);
                }
                else if (landedInReach >= 0)
                {
                    Assert.IsTrue(reach.IsGrounded, $"start {from}, step {i}: it stays on the ground while it closes the gap");
                    Assert.AreEqual(CharacterEvents.None, reach.Events, $"start {from}, step {i}");
                }
            }
            Assert.IsTrue(landedOnContact >= 0 && landedInReach >= 0, "both landed");
            Assert.IsTrue(landedInReach <= landedOnContact && landedInReach >= landedOnContact - 1, $"start {from}: {landedInReach} against {landedOnContact}");
            if (landedInReach < landedOnContact) earlier++;
            AssertResting(reach.World, Bottom(reach), $"start {from}: it ends on the floor");
        }
        Assert.IsTrue(earlier >= 2, $"some falls end a step within the reach and land a step early: {earlier} of 12");

        // A jump pressed in the step it landed early is a jump from the ground.
        var hero = Hero(Level(), 0, 30.13f, Classic() with { LandingReach = 1 * Scale, JumpBuffer = 0, CoyoteTime = 0 });
        for (int i = 0; i < 120 && !hero.IsGrounded; i++) hero.Step(Dt, default);
        hero.Step(Dt, new CharacterInput(0, JumpPressed: true, JumpHeld: true));
        Assert.IsTrue((hero.Events & CharacterEvents.Jumped) != 0);

        // The reach does not hold a character over a ledge, and a rising character is not on the ground it leaves.
        var ledge = Standing(Level(), x: 0, motion: Classic() with { LandingReach = 1 * Scale });
        ledge.Step(Dt, new CharacterInput(0, JumpPressed: true, JumpHeld: true));
        Assert.IsFalse(ledge.IsGrounded, "a jump leaves the ground at once");
    }

    [TestMethod]
    public void The_order_decides_whether_a_jump_takes_the_platforms_move_with_it()
    {
        float Jump(CharacterStepOrder order, out CharacterController2D hero)
        {
            var world = Level();
            var (platform, _) = Box(world, BodyDescription.Kinematic(default), -20, 20, 20, 24, owner: PlatformOwner);
            hero = Hero(world, 0, 26, Classic() with { Order = order });
            Steps(hero, 30);
            Assert.IsTrue(hero.IsGrounded && Bottom(hero) > 23, "standing on the platform");
            for (int i = 0; i < 5; i++)
            {
                world.SetPosition(platform, world.PositionOf(platform) + By(world, 2, 0));
                hero.Step(Dt, default);
            }
            Assert.AreEqual(10, X(hero), 0.01f + Tolerance(world), "carried in both orders while it stands");
            world.SetPosition(platform, world.PositionOf(platform) + By(world, 2, 0));
            hero.Step(Dt, new CharacterInput(0, JumpPressed: true, JumpHeld: true));
            Assert.IsTrue((hero.Events & CharacterEvents.Jumped) != 0);
            return X(hero);
        }
        Assert.AreEqual(12, Jump(CharacterStepOrder.CarryThenAct, out var carried), 0.01f + Tolerance(carried.World), "carried, then it jumps");
        Assert.AreEqual(10, Jump(CharacterStepOrder.ActThenCarry, out var left), 0.01f + Tolerance(left.World), "it jumps from where it stood");
    }

    [TestMethod]
    public void With_act_first_a_drop_with_jump_held_starts_with_the_gravity_of_the_air()
    {
        float FirstStep(CharacterStepOrder order)
        {
            var world = Level();
            if ((world.Capabilities & PhysicsCapabilities.OneWayPlatforms) == 0) Assert.Inconclusive($"{world.Backend} has no one-way platforms");
            Box(world, BodyDescription.Static(), -50, 50, 30, 34, s => s with { OneWay = true }, LevelOwner);
            var hero = Hero(world, 0, 36, Classic() with { Order = order });
            Steps(hero, 30);
            hero.Step(Dt, new CharacterInput(0, JumpHeld: true, Drop: true));
            Assert.IsTrue((hero.Events & CharacterEvents.DroppedThrough) != 0);
            return -Rise(hero);
        }
        Assert.AreEqual(1050 * Dt, FirstStep(CharacterStepOrder.CarryThenAct), 0.01f, "the fall's gravity, chosen while it stood");
        Assert.AreEqual(380 * Dt, FirstStep(CharacterStepOrder.ActThenCarry), 0.01f, "the floaty top of a jump held, as in every later step");
    }

    [TestMethod]
    public void A_drop_that_needs_a_jump_takes_the_pressed_or_the_buffered_one()
    {
        IPhysicsWorld2D Scene()
        {
            var world = Level();
            if ((world.Capabilities & PhysicsCapabilities.OneWayPlatforms) == 0) Assert.Inconclusive($"{world.Backend} has no one-way platforms");
            Box(world, BodyDescription.Static(), -50, 50, 30, 34, s => s with { OneWay = true }, LevelOwner);
            return world;
        }
        var motion = Classic() with { DropWithJump = true };
        var hero = Hero(Scene(), 0, 36, motion);
        Steps(hero, 30);
        Assert.AreEqual(CharacterEvents.None, Steps(hero, 20, new CharacterInput(0, Drop: true)), "down alone does nothing");
        Assert.IsTrue(hero.IsGrounded && Bottom(hero) > 33);
        hero.Step(Dt, new CharacterInput(0, JumpPressed: true, JumpHeld: true, Drop: true));
        Assert.AreEqual(CharacterEvents.DroppedThrough | CharacterEvents.LeftGround, hero.Events, "down and jump drops, and does not jump");
        var events = Steps(hero, 60, new CharacterInput(0, Drop: true));
        Assert.IsTrue(hero.IsGrounded && (events & CharacterEvents.Jumped) == 0);
        AssertResting(hero.World, Bottom(hero), "through to the floor");
        hero.Step(Dt, new CharacterInput(0, JumpPressed: true, JumpHeld: true, Drop: true));
        Assert.IsTrue((hero.Events & CharacterEvents.Jumped) != 0, "on solid ground down and jump is a jump");

        // A jump pressed just before landing on the platform with down held: the buffered jump becomes the drop.
        var buffered = Hero(Scene(), 0, 48, motion);
        var seen = CharacterEvents.None;
        bool pressed = false;
        for (int i = 0; i < 120; i++)
        {
            bool press = !pressed && Bottom(buffered) < 37 && Rise(buffered) < 0;
            pressed |= press;
            buffered.Step(Dt, new CharacterInput(0, JumpPressed: press, Drop: true));
            seen |= buffered.Events;
        }
        Assert.IsTrue(pressed, "jump was pressed in the air, just over the platform");
        Assert.IsTrue((seen & CharacterEvents.DroppedThrough) != 0 && (seen & CharacterEvents.Jumped) == 0, $"it dropped and did not jump: {seen}");
        AssertResting(buffered.World, Bottom(buffered), "on the floor under the platform");

        // Without the rule the same input jumps on landing... after dropping at once, since down is a drop by itself.
        var plain = Hero(Scene(), 0, 36, Classic());
        Steps(plain, 30);
        plain.Step(Dt, new CharacterInput(0, Drop: true));
        Assert.IsTrue((plain.Events & CharacterEvents.DroppedThrough) != 0, "by default down alone drops");
    }

    [TestMethod]
    public void A_launch_can_be_one_the_player_cuts_short()
    {
        float GravityAfter(bool cuttable, bool held)
        {
            var hero = Standing(Level(), motion: Classic());
            hero.Launch(By(hero.World, 0, 330), cuttable);
            Assert.AreEqual(cuttable, hero.IsJumping);
            hero.Step(Dt, new CharacterInput(0, JumpHeld: held));
            return (330 - Rise(hero)) / Dt;
        }
        Assert.AreEqual(640, GravityAfter(cuttable: true, held: true), 1, "held: it rises as a held jump");
        Assert.AreEqual(1900, GravityAfter(cuttable: true, held: false), 1, "released: cut short as a jump is");
        Assert.AreEqual(640, GravityAfter(cuttable: false, held: false), 1, "a plain launch is not cut");

        // A launch downward is never a jump.
        var down = Standing(Level(), motion: Classic());
        down.Launch(By(down.World, 0, -10), cuttable: true);
        Assert.IsFalse(down.IsJumping);
    }

    [TestMethod]
    public void A_released_jump_can_be_cut_to_a_share_of_its_speed_at_once()
    {
        // The rule of a run-and-gun hop, written as the game writes it, against the controller step by step.
        const float jump = 300, gravity = 900, fall = 400, share = 0.45f;
        var motion = Motion(m => m with { JumpReleaseGravityScale = 1, JumpReleaseSpeedScale = share });
        for (int release = 1; release <= 12; release += 3)
        {
            var hero = Standing(Level(), motion: motion);
            float v = 0;
            bool jumping = false;
            for (int i = 0; i < 40; i++)
            {
                bool pressed = i == 0, held = i < release;
                if (pressed) { v = jump; jumping = true; }
                if (!held && v > jump * share && jumping) v = jump * share;
                v = MathF.Max(v - gravity * Dt, -fall);
                hero.Step(Dt, new CharacterInput(0, pressed, held));
                if (hero.IsGrounded) break;
                Assert.AreEqual(v, Rise(hero), 0.01f, $"released at step {release}, step {i}");
            }
        }

        // With the default share nothing is cut: only the release gravity shortens the jump.
        var plain = Standing(Level(), motion: Motion());
        plain.Step(Dt, new CharacterInput(0, JumpPressed: true, JumpHeld: true));
        plain.Step(Dt, default);
        Assert.AreEqual(300 - 900 * Dt - 900 * 2.5f * Dt, Rise(plain), 0.01f);
    }

    [TestMethod]
    public void The_state_can_be_kept_and_put_back()
    {
        var world = Level();
        var hero = Standing(world, x: -200, motion: Feel());
        var inputs = new CharacterInput[90];
        for (int i = 0; i < inputs.Length; i++) inputs[i] = new CharacterInput(i < 60 ? 1 : -1, JumpPressed: i % 25 == 10, JumpHeld: i % 25 is >= 10 and < 18, Run: i % 2 == 0);
        for (int i = 0; i < 37; i++) hero.Step(Dt, inputs[i]);

        var kept = hero.Snapshot();
        ulong hash = Hash(hero);
        Assert.AreEqual(hero.Position, kept.Position);
        Assert.AreEqual(hero.Velocity, kept.Velocity);
        Assert.AreEqual(hero.IsGrounded, kept.IsGrounded);
        Assert.AreEqual((int)hero.Events, (int)kept.Events);

        // A planner tries the next half second and takes it back.
        var trace = new List<(Vector2, Vector2, CharacterEvents)>();
        for (int i = 37; i < inputs.Length; i++)
        {
            hero.Step(Dt, inputs[i]);
            trace.Add((hero.Position, hero.Velocity, hero.Events));
        }
        Assert.AreNotEqual(hash, Hash(hero));
        hero.Restore(kept);
        Assert.AreEqual(hash, Hash(hero), "the restored state hashes as the kept one");
        Assert.AreEqual(kept.Position, hero.Position);
        for (int i = 37; i < inputs.Length; i++)
        {
            hero.Step(Dt, inputs[i]);
            Assert.AreEqual(trace[i - 37], (hero.Position, hero.Velocity, hero.Events), $"step {i} repeats");
        }

        // A state built by the game: in the air at a place, moving, with a jump still to cut.
        hero.Restore(new CharacterState { Position = At(world, 0, 50), Velocity = By(world, 30, 100), IsJumping = true });
        Assert.IsFalse(hero.IsGrounded);
        Assert.IsTrue(hero.IsJumping);
        hero.Step(Dt, default);
        Assert.AreEqual(100 - 1900 * Dt, Rise(hero), 0.01f, "the jump it was given is cut by the released button");

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => hero.Restore(kept with { Velocity = new Vector2(float.NaN, 0) }));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => hero.Restore(kept with { Position = new Vector2(0, float.PositiveInfinity) }));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => hero.Restore(kept with { CoyoteTime = -1 }));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => hero.Restore(kept with { JumpBuffer = float.NaN }));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => hero.Restore(kept with { AirJumpsUsed = -1 }));
    }

    [TestMethod]
    public void A_restored_rider_is_still_carried_by_its_platform()
    {
        var world = Level();
        var (platform, _) = Box(world, BodyDescription.Kinematic(default), -20, 20, 20, 24, owner: PlatformOwner);
        var hero = Hero(world, 0, 26);
        Steps(hero, 30);
        var kept = hero.Snapshot();
        Assert.AreEqual(world.BodyOf(kept.Ground), platform);
        hero.Teleport(At(world, 60, 80));
        hero.Restore(kept);
        world.SetPosition(platform, world.PositionOf(platform) + By(world, 3, 0));
        hero.Step(Dt, default);
        Assert.IsTrue(hero.IsGrounded);
        Assert.AreEqual(3, X(hero), 0.01f + Tolerance(world), "the ground it was restored on carries it");
    }

    [TestMethod]
    public void Setting_the_position_keeps_the_ground_and_the_rest_of_the_state()
    {
        var world = Level();
        var hero = Standing(world, x: -200, motion: Classic());
        Steps(hero, 20, new CharacterInput(1));
        var velocity = hero.Velocity;
        // A camera clamp: a little to the side, every step.
        hero.Position += By(world, -1.5f, 0);
        Assert.IsTrue(hero.IsGrounded, "still on the ground");
        Assert.AreEqual(velocity, hero.Velocity);
        hero.Step(Dt, new CharacterInput(1));
        Assert.IsTrue(hero.IsGrounded);
        Assert.AreEqual(CharacterEvents.None, hero.Events, "no landing, no leaving");
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => hero.Position = new Vector2(float.NaN, 0));

        // Teleport is the other thing: it leaves the ground.
        hero.Teleport(hero.Position);
        Assert.IsFalse(hero.IsGrounded);

        // On a moving platform a placement does not undo the carry.
        var (platform, _) = Box(world, BodyDescription.Kinematic(default), -20, 20, 20, 24, owner: PlatformOwner);
        var rider = CharacterController2D.Create(world, PlatformOwner, At(world, 0, 33), new Vector2(12, 14) * Scale, Motion());
        Steps(rider, 30);
        float x = XOf(rider.Position);
        rider.Position += By(world, 1, 0);
        world.SetPosition(platform, world.PositionOf(platform) + By(world, 2, 0));
        rider.Step(Dt, default);
        Assert.AreEqual(x + 3, XOf(rider.Position), 0.01f + Tolerance(world), "placed by 1, then carried by 2");
    }

    [TestMethod]
    public void The_game_can_decide_the_ground()
    {
        // A platform the character does not collide with, and a rule of the game's: it catches a falling character
        // whose feet are within 2 units of its top.
        const ulong PlatformLayer = 1UL << 9;
        var world = Level();
        var (platform, shape) = Box(world, BodyDescription.Kinematic(default), -20, 20, 40, 44, s => s.InLayers(PlatformLayer, CollisionLayers.All), PlatformOwner);
        var hero = CharacterController2D.Create(world, HeroOwner, At(world, 0, 60 + 7), new Vector2(12, 14) * Scale, Motion(), mask: CollisionLayers.All & ~PlatformLayer);

        void Rule()
        {
            float top = HeightOf(world, world.PositionOf(platform)) + 2;
            if (Rise(hero) <= 0 && Bottom(hero) <= top + 2 && Bottom(hero) >= top - 2 && MathF.Abs(X(hero) - XOf(world.PositionOf(platform))) < 20)
                hero.StandOn(shape, At(world, X(hero), top + 7));
        }

        var seen = CharacterEvents.None;
        int landed = 0;
        for (int i = 0; i < 60; i++)
        {
            hero.Step(Dt, default);
            Rule();
            seen |= hero.Events;
            if ((hero.Events & CharacterEvents.Landed) != 0) landed++;
        }
        Assert.IsTrue(hero.IsGrounded && hero.Ground == shape, "it stands on the platform the rule gave it");
        Assert.AreEqual(1, landed, "it landed once");
        Assert.AreEqual(CharacterEvents.None, seen & CharacterEvents.LeftGround, "and never left: the rule holds every step");
        Assert.AreEqual(44, Bottom(hero), 0.01f);
        Assert.AreEqual(0, Rise(hero));
        Assert.AreEqual(0, hero.AirTime, "on the ground");

        // The platform carries it, sideways and up.
        for (int i = 0; i < 20; i++)
        {
            world.SetPosition(platform, world.PositionOf(platform) + By(world, 1, 0.5f));
            hero.Step(Dt, default);
            Rule();
            Assert.IsTrue(hero.IsGrounded, $"step {i}");
            Assert.AreEqual(CharacterEvents.None, hero.Events, $"step {i}");
        }
        Assert.AreEqual(20, X(hero), 0.01f + Tolerance(world));
        Assert.AreEqual(54, Bottom(hero), 0.01f);

        // It jumps from it as from any ground, and falls when the rule no longer holds.
        hero.Step(Dt, new CharacterInput(0, JumpPressed: true, JumpHeld: true));
        Assert.IsTrue((hero.Events & CharacterEvents.Jumped) != 0 && !hero.IsGrounded);
        hero.Teleport(At(world, 60, 30));
        hero.Velocity = Vector2.Zero;
        var events = Steps(hero, 60);
        Assert.IsTrue((events & CharacterEvents.Landed) != 0);
        AssertResting(world, Bottom(hero), "on the floor");

        Assert.ThrowsExactly<InvalidOperationException>(() => hero.StandOn(ShapeId.None, hero.Position));
    }

    [TestMethod]
    public void The_new_values_of_a_motion_are_validated()
    {
        foreach (float bad in new[] { -1f, float.NaN })
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new CharacterMotion { RunSpeed = bad }.Validate());
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new CharacterMotion { RunAcceleration = bad }.Validate());
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new CharacterMotion { AirAcceleration = bad }.Validate());
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new CharacterMotion { AirDeceleration = bad }.Validate());
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new CharacterMotion { OverspeedDeceleration = bad }.Validate());
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new CharacterMotion { AirOverspeedDeceleration = bad }.Validate());
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new CharacterMotion { LandingReach = bad }.Validate());
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new CharacterMotion { JumpReleaseSpeedScale = bad }.Validate());
        }
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new CharacterMotion { RunSpeed = float.PositiveInfinity }.Validate());
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new CharacterMotion { LandingReach = float.PositiveInfinity }.Validate());
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new CharacterMotion { JumpReleaseSpeedScale = 1.5f }.Validate());
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new CharacterMotion { Order = (CharacterStepOrder)7 }.Validate());
        // The reach is a little forgiveness, not a hover: at most what the fastest fall covers in a thirtieth of a second.
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new CharacterMotion { MaxFallSpeed = 400, LandingReach = 60 }.Validate());
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new CharacterMotion { MaxFallSpeed = 270, LandingReach = 9.1f }.Validate());
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new CharacterMotion { MaxFallSpeed = 0, LandingReach = 0.5f }.Validate());
        new CharacterMotion { MaxFallSpeed = 270, LandingReach = 9 }.Validate();
        Assert.AreEqual(400 / 30f, new CharacterMotion().MaxLandingReach, 1e-4f);
        new CharacterMotion { RunAcceleration = float.PositiveInfinity, AirAcceleration = 0, AirOverspeedDeceleration = 0, JumpReleaseSpeedScale = 0 }.Validate();
    }

    [TestMethod]
    public void A_character_can_have_its_origin_at_its_feet()
    {
        var world = Level();
        var size = new Vector2(12, 14) * Scale;
        // The origin 7 units under the box's centre: the feet.
        var hero = CharacterController2D.Create(world, HeroOwner, At(world, 0, 20), size, Motion(), offset: By(world, 0, 7));
        Steps(hero, 60);
        Assert.IsTrue(hero.IsGrounded);
        AssertResting(world, HeightOf(world, hero.Position), "its position is where its feet are");
        Steps(hero, 120, new CharacterInput(1));
        Assert.IsTrue(hero.IsOnWall, "it stops at the wall");
        Assert.AreEqual(100 - 6, X(hero), 0.01f + Tolerance(world), "half its width before the wall");

        var walker = TopDownController2D.Create(world, PlatformOwner, At(world, -50, 40), size, new TopDownMotion { MoveSpeed = 90 * Scale }, offset: By(world, 0, 7));
        for (int i = 0; i < 60; i++) walker.Step(Dt, -world.Up);
        AssertResting(world, HeightOf(world, walker.Position), "a top-down character too");
    }

    [TestMethod]
    public void A_move_says_which_surfaces_stopped_it()
    {
        var world = Level();
        var hero = TopDownController2D.Create(world, HeroOwner, At(world, 80, 7), new Vector2(12, 14) * Scale, new TopDownMotion { MoveSpeed = 90 * Scale });
        // Free: nothing stopped it (it slides over the floor it touches).
        var free = hero.Step(Dt, new Vector2(1, 0));
        Assert.IsFalse(free.StoppedGoing(new Vector2(1, 0)), free.ToString());
        foreach (var contact in free.Contacts) Assert.IsFalse(contact.Stopped, "the floor it slides on did not stop it");

        MoveResult blocked = default;
        for (int i = 0; i < 20; i++) blocked = hero.Step(Dt, new Vector2(1, 0));
        Assert.IsTrue(blocked.HitWall);
        Assert.IsTrue(blocked.StoppedGoing(new Vector2(1, 0)), "the wall stopped the move to the right");
        Assert.IsFalse(blocked.StoppedGoing(new Vector2(-1, 0)));
        Assert.IsFalse(blocked.StoppedGoing(world.Up));
        Assert.IsFalse(blocked.StoppedGoing(Vector2.Zero));
        bool wall = false;
        foreach (var contact in blocked.Contacts)
            if (contact.Stopped)
            {
                Assert.AreEqual(-1, contact.Normal.X, 0.01f, "the stopping surface faces left");
                Assert.AreEqual(LevelOwner, contact.Owner);
                wall = true;
            }
        Assert.IsTrue(wall);

        // Beside the wall and moving along it: it touches the wall, which stops nothing.
        var along = hero.Step(Dt, world.Up);
        Assert.IsTrue(along.HitWall, "it still touches the wall");
        Assert.IsFalse(along.StoppedGoing(new Vector2(1, 0)));
        // Into the floor: the floor stops the move down.
        hero.Step(Dt, -world.Up);
        var down = hero.Step(Dt, -world.Up);
        Assert.IsTrue(down.StoppedGoing(-world.Up), down.ToString());
    }

    /// <summary>A floor whose top is at height <paramref name="oh"/> and a wall 100 units to the right of <paramref name="ox"/>, far from the origin or at it.</summary>
    private IPhysicsWorld2D LevelAt(float ox, float oh)
    {
        var world = CreateWorld();
        Box(world, BodyDescription.Static(), ox - 300, ox + 300, oh - 16, oh, owner: LevelOwner);
        Box(world, BodyDescription.Static(), ox + 100, ox + 116, oh, oh + 96, owner: LevelOwner);
        return world;
    }

    [TestMethod]
    [DataRow(0f, 0f)]
    [DataRow(40000f, 0f)]
    [DataRow(-40000f, 0f)]
    [DataRow(0f, 40000f)]
    [DataRow(0f, -40000f)]
    [DataRow(40000f, 40000f)]
    [DataRow(-40000f, -40000f)]
    public void Far_from_the_origin_a_small_move_into_a_wall_is_still_stopped_and_a_reach_still_lands_early(float ox, float oh)
    {
        var size = new Vector2(12, 14) * Scale;
        foreach (float into in new[] { 0.05f, 0.3f, 2f })
        {
            var world = LevelAt(ox, oh);
            var body = world.CreateBody(HeroOwner, BodyDescription.Kinematic(At(world, ox + 80, oh + 7)));
            world.AddShape(body, ShapeDescription.Box(size));
            world.Move(body, By(world, 0, -5));
            var reached = world.Move(body, By(world, 30, 0));
            Assert.IsTrue(reached.HitWall && reached.StoppedGoing(new Vector2(1, 0)), $"walking into the wall: {reached}");
            var pressed = world.Move(body, By(world, into, 0));
            Assert.IsTrue(pressed.StoppedGoing(new Vector2(1, 0)), $"a move of {into} into the wall it touches is stopped by it: {pressed}");
            Assert.IsFalse(pressed.StoppedGoing(new Vector2(-1, 0)));
            var along = world.Move(body, By(world, 0, 3));
            Assert.IsTrue(along.HitWall && !along.StoppedGoing(new Vector2(1, 0)), $"a move along the wall is not: {along}");
            var away = world.Move(body, By(world, -into, 0));
            Assert.IsFalse(away.StoppedGoing(new Vector2(1, 0)) || away.StoppedGoing(new Vector2(-1, 0)), $"nor one away from it: {away}");
            (world as IDisposable)?.Dispose();
        }

        int earlier = 0;
        for (int start = 0; start < 24; start++)
        {
            float from = 30 + start * 0.13f;
            var contactWorld = LevelAt(ox, oh);
            var reachWorld = LevelAt(ox, oh);
            var contact = CharacterController2D.Create(contactWorld, HeroOwner, At(contactWorld, ox, oh + from + 7), size, Classic());
            var reach = CharacterController2D.Create(reachWorld, HeroOwner, At(reachWorld, ox, oh + from + 7), size, Classic() with { LandingReach = 1 * Scale });
            int landedOnContact = -1, landedInReach = -1;
            for (int i = 0; i < 120; i++)
            {
                contact.Step(Dt, default);
                reach.Step(Dt, default);
                if (landedOnContact < 0 && contact.IsGrounded) landedOnContact = i;
                if (landedInReach < 0 && reach.IsGrounded) landedInReach = i;
                else if (landedInReach >= 0) Assert.IsTrue(reach.IsGrounded, $"start {from}, step {i}");
            }
            Assert.IsTrue(landedOnContact >= 0 && landedInReach >= 0 && landedInReach <= landedOnContact && landedInReach >= landedOnContact - 1, $"start {from}: {landedInReach} against {landedOnContact}");
            if (landedInReach < landedOnContact)
            {
                earlier++;
                continue;
            }
            // Landed in the same step: then the fall ended that step on the floor, not within the reach above it.
        }
        // A fall of 4.5 units a step ends within a unit of the floor in about one fall of four, wherever the level is.
        Assert.IsTrue(earlier is >= 3 and <= 9, $"falls that landed a step early: {earlier} of 24");
    }

    [TestMethod]
    public void Shift_places_a_character_with_collision_and_keeps_its_state()
    {
        var world = Level();
        var hero = Standing(world, x: 80, motion: Classic());
        Steps(hero, 30, new CharacterInput(1));
        Assert.IsTrue(hero.IsOnWall, "against the wall at x 100");
        float x = X(hero);
        var velocity = hero.Velocity;

        // A camera clamp that asks for a place inside the wall: the character stays out of it.
        var into = hero.Shift(By(world, 5, 0));
        Assert.AreEqual(x, X(hero), Tolerance(world), "the wall holds it");
        Assert.IsTrue(into.StoppedGoing(new Vector2(1, 0)));
        Assert.IsTrue(hero.IsGrounded, "still on the ground");
        Assert.AreEqual(velocity, hero.Velocity);
        Steps(hero, 20, new CharacterInput(1));
        Assert.AreEqual(x, X(hero), Tolerance(world), "and it has not gone through in the steps after");

        // Into the floor: the same.
        hero.Shift(By(world, 0, -3));
        Steps(hero, 5);
        AssertResting(world, Bottom(hero), "not under the floor");

        // Away from the wall it goes the whole way, with no events.
        hero.Shift(By(world, -10, 0));
        Assert.AreEqual(x - 10, X(hero), 0.01f + Tolerance(world));
        hero.Step(Dt, default);
        Assert.IsTrue(hero.IsGrounded);
        Assert.AreEqual(CharacterEvents.None, hero.Events & (CharacterEvents.Landed | CharacterEvents.LeftGround));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => hero.Shift(new Vector2(float.NaN, 0)));

        // On a platform that moves, a shift does not undo the carry.
        var (platform, _) = Box(world, BodyDescription.Kinematic(default), -20, 20, 20, 24, owner: PlatformOwner);
        var rider = CharacterController2D.Create(world, PlatformOwner, At(world, 0, 33), new Vector2(12, 14) * Scale, Motion());
        Steps(rider, 30);
        float before = XOf(rider.Position);
        rider.Shift(By(world, 1, 0));
        world.SetPosition(platform, world.PositionOf(platform) + By(world, 2, 0));
        rider.Step(Dt, default);
        Assert.AreEqual(before + 3, XOf(rider.Position), 0.01f + Tolerance(world), "shifted by 1, then carried by 2");
    }

    [TestMethod]
    public void The_motion_is_not_in_the_state_hash()
    {
        var a = Standing(Level(), motion: Classic());
        var b = Standing(Level(), motion: Classic());
        Assert.AreEqual(Hash(a), Hash(b));
        b.Motion = Classic() with { MoveSpeed = 200 * Scale, Gravity = 100 * Scale };
        Assert.AreEqual(Hash(a), Hash(b), "the hash is the character's state; a game that changes the motion hashes what chose it");
    }
}

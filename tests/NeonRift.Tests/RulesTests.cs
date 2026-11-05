using System.Numerics;
using NeonRift.Art;
using NeonRift.Content;
using NeonRift.Gameplay;
using MicroEngine.Core;
using static NeonRift.Tests.Harness;

namespace NeonRift.Tests;

[TestClass]
public sealed class RulesTests
{
    private static void Wait(Stage stage, int steps = 200) => Run(stage, steps, PlayerInput.None);

    /// <summary>Teleports the player and scrolls the camera with her (the screen edge would push her back).</summary>
    private static void MoveTo(Stage stage, float x, float y)
    {
        stage.Camera.Position = stage.Camera.Position with { X = x - 200 };
        stage.Player.Position = new Vector2(x, y);
    }

    [TestMethod]
    public void Each_hit_costs_health_and_grants_a_moment_of_safety()
    {
        var stage = Settled();
        Run(stage, 120, PlayerInput.None); // past the start-of-stage grace
        stage.Spawn(new EnemyShot(stage.Player.Position + new Vector2(0, -20), Vector2.Zero));
        Run(stage, 1, PlayerInput.None);
        Assert.AreEqual(Player.MaxHealth - 1, stage.Player.Health);
        Assert.AreEqual(PlayerState.Alive, stage.Player.State);
        stage.Spawn(new EnemyShot(stage.Player.Position + new Vector2(0, -20), Vector2.Zero));
        Run(stage, 1, PlayerInput.None);
        Assert.AreEqual(Player.MaxHealth - 1, stage.Player.Health, "Invulnerable right after a hit.");
        Run(stage, (int)(Player.HitInvulnerability * 60) + 2, PlayerInput.None);
        stage.Spawn(new EnemyShot(stage.Player.Position + new Vector2(0, -20), Vector2.Zero));
        Run(stage, 1, PlayerInput.None);
        Assert.AreEqual(Player.MaxHealth - 2, stage.Player.Health);
    }

    [TestMethod]
    public void Losing_all_health_costs_a_life_and_the_player_respawns_with_the_default_weapon()
    {
        var session = new Session { Weapon = WeaponKind.Spread };
        var stage = Settled(session: session);
        Assert.AreEqual(WeaponKind.Spread, stage.Player.Weapon.Kind);
        for (int i = 0; i < Player.MaxHealth; i++)
        {
            stage.Player.Invulnerable = 0;
            stage.Player.Hurt();
        }
        Assert.AreEqual(PlayerState.Dying, stage.Player.State);
        Wait(stage);
        Assert.AreEqual(PlayerState.Alive, stage.Player.State);
        Assert.AreEqual(Player.MaxHealth, stage.Player.Health, "A new life starts at full health.");
        Assert.AreEqual(Session.StartLives - 1, session.Lives);
        Assert.AreEqual(WeaponKind.Pulse, stage.Player.Weapon.Kind, "Dying loses the weapon.");
        Assert.IsGreaterThan(0f, stage.Player.Invulnerable, "Respawn grants a moment of safety.");
    }

    [TestMethod]
    public void Losing_the_last_life_ends_the_game()
    {
        var session = new Session { Lives = 0 };
        var stage = Settled(session: session);
        stage.Player.Kill(force: true);
        Wait(stage);
        Assert.AreEqual(StagePhase.GameOver, stage.Phase);
    }

    [TestMethod]
    public void Falling_into_a_pit_kills_even_through_god_mode()
    {
        var def = Flat(extra: b => b.Pit(10).Floor(40, 3));
        var stage = Stage(def);
        stage.Player.God = true;
        MoveTo(stage, (120 + 3) * 16, 0);
        Wait(stage, 120);
        Assert.AreEqual(1, stage.Session.Deaths, $"state {stage.Player.State} at {stage.Player.Position} cam {stage.Camera.Position} ground {stage.GroundBelow(stage.Player.Position.X, 0)}");
    }

    [TestMethod]
    public void Lava_kills_on_touch()
    {
        var def = Flat(extra: b => b.Lava(10, 2).Floor(40, 3));
        var stage = Stage(def);
        MoveTo(stage, (120 + 3) * 16, 100);
        Wait(stage, 60);
        Assert.AreNotEqual(PlayerState.Alive, stage.Player.State);
    }

    [TestMethod]
    public void Barrier_and_capsules_change_the_loadout()
    {
        var session = new Session();
        var stage = Settled(session: session);
        var feet = stage.Player.Position;
        stage.Spawn(new Capsule(feet + new Vector2(0, -20), 'L') { Velocity = Vector2.Zero });
        Run(stage, 1, PlayerInput.None);
        Assert.AreEqual(WeaponKind.Lance, stage.Player.Weapon.Kind);
        Assert.AreEqual(WeaponKind.Lance, session.Weapon);
        stage.Spawn(new Capsule(feet + new Vector2(0, -20), 'B') { Velocity = Vector2.Zero });
        Run(stage, 1, PlayerInput.None);
        Assert.IsGreaterThan(0f, stage.Player.Barrier);
        stage.Spawn(new EnemyShot(stage.Player.Position + new Vector2(0, -20), Vector2.Zero));
        Run(stage, 1, PlayerInput.None);
        Assert.AreEqual(Player.MaxHealth, stage.Player.Health, "The barrier absorbs hits.");
    }

    [TestMethod]
    public void Lingering_brings_troopers_and_advancing_does_not()
    {
        var idle = Settled();
        idle.Player.God = true;
        Run(idle, (int)(Gameplay.Stage.LingerGrace * 60) - 40, PlayerInput.None);
        Assert.IsEmpty(idle.Enemies.OfType<Trooper>().ToList(), "No pressure during the grace period.");
        Run(idle, 60 * 4, PlayerInput.None);
        Assert.IsGreaterThanOrEqualTo(2, idle.Enemies.OfType<Trooper>().Count());

        var moving = Settled();
        moving.Player.God = true;
        Run(moving, 60 * 7, In(right: true));
        Assert.IsEmpty(moving.Enemies.OfType<Trooper>().ToList());
    }

    [TestMethod]
    public void Score_milestones_grant_extra_lives()
    {
        var session = new Session();
        int gained = 0;
        session.LifeGained += () => gained++;
        session.AddScore(Session.ExtraLifeEvery - 1);
        Assert.AreEqual(Session.StartLives, session.Lives);
        session.AddScore(Session.ExtraLifeEvery * 2);
        Assert.AreEqual(Session.StartLives + 2, session.Lives);
        Assert.AreEqual(2, gained);
    }

    [TestMethod]
    public void Continues_restore_lives_and_run_out()
    {
        var session = new Session { Lives = 0, Weapon = WeaponKind.Rapid };
        session.AddScore(500);
        for (int i = 0; i < Session.StartContinues; i++) Assert.IsTrue(session.UseContinue());
        Assert.AreEqual(Session.StartLives, session.Lives);
        Assert.AreEqual(0, session.Score);
        Assert.AreEqual(WeaponKind.Pulse, session.Weapon);
        Assert.IsFalse(session.UseContinue());
    }

    [TestMethod]
    public void Egg_pods_are_armoured_until_they_open()
    {
        var stage = Settled();
        var egg = stage.Spawn(new EggPod(stage.Player.Position + new Vector2(200, 0)));
        Run(stage, 2, PlayerInput.None);
        Assert.IsTrue(egg.Armored);
        egg.Hit(5, egg.Center);
        Assert.AreEqual(12, egg.Health, "Shots clank off the shell.");
        Run(stage, 150, PlayerInput.None);
        Assert.IsTrue(egg.Open);
        egg.Hit(5, egg.Center);
        Assert.AreEqual(7, egg.Health);
    }

    [TestMethod]
    public void Lance_pierces_a_line_of_enemies_and_pulse_stops_at_the_first()
    {
        int Hits(WeaponKind kind)
        {
            var stage = Settled();
            stage.Player.Equip(kind);
            var feet = stage.Player.Position;
            var a = stage.Spawn(new Gunner(feet + new Vector2(60, 0)));
            var b = stage.Spawn(new Gunner(feet + new Vector2(100, 0)));
            Run(stage, 1, In(fire: true));
            Run(stage, 30, PlayerInput.None);
            return (a.Health < 3 ? 1 : 0) + (b.Health < 3 ? 1 : 0);
        }
        Assert.AreEqual(2, Hits(WeaponKind.Lance));
        Assert.AreEqual(1, Hits(WeaponKind.Pulse));
    }

    [TestMethod]
    public void Shooting_a_carrier_drops_its_capsule()
    {
        var stage = Settled();
        var carrier = stage.Spawn(new Carrier(stage.Player.Position + new Vector2(80, -60), 'S'));
        Run(stage, 1, PlayerInput.None);
        carrier.Hit(2, carrier.Center);
        Run(stage, 1, PlayerInput.None);
        Assert.AreEqual('S', stage.Pickups.Single().Letter);
    }

    [TestMethod]
    public void Reaching_the_arena_warns_then_spawns_the_boss_and_its_death_clears_the_stage()
    {
        var stage = Settled(Flat(60));
        stage.Player.God = true;
        Run(stage, 60 * 12, In(right: true));
        Assert.IsTrue(stage.InArena);
        Assert.IsNotNull(stage.Boss, $"Phase {stage.Phase}");
        Assert.AreEqual(StagePhase.Boss, stage.Phase);
        float lockX = stage.Camera.Position.X;
        stage.Boss.Hit(1, stage.Boss.Center);
        while (stage.Boss.Alive) stage.Boss.Hit(1000, stage.Boss.Center);
        Assert.AreEqual(StagePhase.Cleared, stage.Phase);
        Run(stage, (int)(Gameplay.Stage.ClearTime * 60) + 2, PlayerInput.None);
        Assert.IsTrue(stage.Finished);
        Assert.AreEqual(lockX, stage.Camera.Position.X, 0.01f, "The arena camera stays locked.");
    }

    [TestMethod]
    public void Bosses_cannot_be_hurt_while_flying_in()
    {
        var stage = Settled();
        var boss = stage.Spawn(Boss.Create(BossKind.Gunship, stage.Player.Position + new Vector2(200, -100)));
        Assert.IsFalse(boss.Hit(5, boss.Center));
        Assert.AreEqual(boss.MaxHealth, boss.Health);
    }

    [TestMethod]
    public void Every_stage_starts_on_ground_and_its_spawns_are_inside_the_map()
    {
        foreach (var def in Stages.All)
        {
            var stage = Stage(def);
            Assert.IsTrue(stage.GroundBelow(stage.Player.Position.X, 0).HasValue, def.Name);
            Assert.AreEqual(def.PixelWidth - StageBuilder.ArenaColumns * 16, def.ArenaX, 0.01f);
            Assert.IsGreaterThanOrEqualTo(3, def.Checkpoints.Count(), $"{def.Name} checkpoints");
            foreach (var s in def.Spawns)
            {
                Assert.IsTrue(s.X > 0 && s.X < def.ArenaX, $"{def.Name} {s.Kind} at {s.X}");
                if (s.Kind is SpawnKind.Gunner or SpawnKind.Turret or SpawnKind.Egg)
                    Assert.IsTrue(def.Layer.Get((int)(s.X / 16), (int)(s.Y / 16)) is TileId.Solid or TileId.OneWay, $"{def.Name} {s.Kind} at column {s.X / 16} floats.");
            }
        }
    }

    [TestMethod]
    public void The_game_loads_every_asset_and_reaches_play_from_the_title()
    {
        var game = new RiftGame(saveDirectory: Path.Combine(Path.GetTempPath(), "neonrift-tests", Guid.NewGuid().ToString("N")));
        var platform = HeadlessPlatform.For(game.Settings);
        game.InitializeForTest(platform);
        Assert.AreEqual(Sounds.Effects.Length, Sounds.Effects.Count(game.Audio.HasSound));
        Assert.IsTrue(Sounds.Tracks.All(t => game.Audio.HasMusic(t.Name)));
        var atlas = game.Art.AtlasTexture;
        Console.WriteLine($"atlas {atlas.Width}x{atlas.Height}");
        Assert.IsTrue(atlas.Width <= 4096 && atlas.Height <= 4096, $"Atlas {atlas.Width}x{atlas.Height} exceeds common GPU limits.");
        platform.Keys.Set(MicroEngine.Input.Key.Enter, true);
        game.StepForTest();
        platform.Keys.Set(MicroEngine.Input.Key.Enter, false);
        for (int i = 0; i < 60 * 6; i++) game.StepForTest();
        Assert.IsInstanceOfType<Scenes.PlayScene>(game.Scenes.Current);
        for (int i = 0; i < 120; i++) game.StepForTest();
        Assert.IsEmpty(game.MissingSounds, string.Join(", ", game.MissingSounds));
    }
}

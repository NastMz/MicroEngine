using System.Numerics;
using CourtyardKey.Content;
using CourtyardKey.Entities;
using CourtyardKey.Model;
using CourtyardKey.Scenes;
using MicroEngine.Core;
using MicroEngine.Input;

namespace CourtyardKey.Tests;

/// <summary>
/// A bot plays the whole Sunken Courtyard with keyboard input: block puzzle, both small keys,
/// the ambushes, the boomerang, the crystal switch, the boss key, the Moss Warden and the ending.
/// The bot is a poor fighter, so it is given a larger life bar; every progression rule is real.
/// </summary>
[TestClass]
public sealed class DungeonRunTests
{
    [TestMethod]
    public void TheWholeDungeonAndTheEndingCanBeCompleted()
    {
        using var h = new Harness("dungeon");
        h.Step(30);
        var play = h.Play;
        play.Hero.Health.RaiseMax(40, fill: true);

        // Entrance -> courtyard -> block puzzle for the first small key.
        h.GoToTile(9, 1);
        h.WalkIntoNextRoom(Key.Up);
        Assert.AreEqual("dungeon:1,2", play.Room.Key);
        var achievements = h.Game.Achievements;
        Assert.IsFalse(achievements.IsUnlocked(CourtyardAchievements.FirstFoe));
        h.ClearRoom();
        Assert.IsTrue(achievements.IsUnlocked(CourtyardAchievements.FirstFoe), "The first kill earns First Strike.");
        Assert.IsGreaterThan(0, achievements.Progress(CourtyardAchievements.Defender));
        Assert.AreEqual(2, achievements.Progress(CourtyardAchievements.Cartographer), "Entrance and courtyard visited.");
        h.GoToTile(1, 4);
        h.WalkIntoNextRoom(Key.Left);
        Assert.AreEqual("dungeon:0,2", play.Room.Key);
        h.WalkToTile(10, 4.75f);
        h.Hold(Key.Left, 300);
        h.WalkToTile(4.5f, 5.75f);
        h.WalkToTile(3.5f, 5.75f);
        h.Hold(Key.Up, 80);
        h.Step(20);
        Assert.IsTrue(play.RoomSolved, "Block puzzle solved.");
        h.OpenChestAt(13, 3);
        Assert.AreEqual(1, play.Progress.SmallKeys);

        // Unlock the courtyard's north door and take the Trial of Wind.
        h.GoToTile(18, 4);
        h.WalkIntoNextRoom(Key.Right);
        h.GoToTile(9, 1);
        h.Hold(Key.Up, 14);
        Assert.AreEqual(0, play.Progress.SmallKeys, "The key was used on the north door.");
        h.WalkIntoNextRoom(Key.Up);
        Assert.AreEqual("dungeon:1,1", play.Room.Key);
        h.GoToTile(1, 4);
        h.WalkIntoNextRoom(Key.Left);
        Assert.AreEqual("dungeon:0,1", play.Room.Key);
        h.Step(30);
        Assert.IsTrue(play.Doors.All(d => !d.IsOpen), "The ambush shutters slam behind the hero.");
        h.ClearRoom();
        h.Step(10);
        Assert.IsTrue(play.RoomSolved);
        h.OpenChestAt(10, 4);
        Assert.IsTrue(play.Progress.HasBoomerang);
        Assert.IsTrue(achievements.IsUnlocked(CourtyardAchievements.WindCatcher));

        // Back to the courtyard and through the bell chamber ambush for the second key.
        h.GoToTile(18, 4);
        h.WalkIntoNextRoom(Key.Right);
        h.GoToTile(9, 8);
        h.WalkIntoNextRoom(Key.Down);
        h.GoToTile(18, 4);
        h.WalkIntoNextRoom(Key.Right);
        Assert.AreEqual("dungeon:2,2", play.Room.Key);
        h.Step(20);
        h.ClearRoom();
        h.Step(10);
        h.OpenChestAt(10, 4);
        Assert.AreEqual(1, play.Progress.SmallKeys);

        // Pillar hall east lock, then the crystal across the water.
        h.GoToTile(1, 4);
        h.WalkIntoNextRoom(Key.Left);
        h.GoToTile(9, 1);
        h.WalkIntoNextRoom(Key.Up);
        h.GoToTile(18, 4);
        h.Hold(Key.Right, 14);
        Assert.AreEqual(0, play.Progress.SmallKeys);
        h.WalkIntoNextRoom(Key.Right);
        Assert.AreEqual("dungeon:2,1", play.Room.Key);
        Assert.IsFalse(play.Doors.Single(d => d.Side == Dir.Up).IsOpen, "The gallery shutter is closed.");
        h.GoToTile(15, 6);
        h.ThrowBoomerang(Dir.Up);
        Assert.IsTrue(play.RoomSolved, "The boomerang struck the crystal.");
        Assert.IsTrue(play.Doors.Single(d => d.Side == Dir.Up).IsOpen);

        // The Warden's Vault: knights guard the boss key.
        h.GoToTile(9, 1);
        h.WalkIntoNextRoom(Key.Up);
        Assert.AreEqual("dungeon:2,0", play.Room.Key);
        h.ClearRoom();
        h.OpenChestAt(9, 1);
        Assert.IsTrue(play.Progress.HasBossKey);

        // Back to the pillar hall and through the great door.
        h.GoToTile(9, 8);
        h.WalkIntoNextRoom(Key.Down);
        h.GoToTile(1, 4);
        h.WalkIntoNextRoom(Key.Left);
        h.GoToTile(9, 1);
        h.Hold(Key.Up, 14);
        h.WalkIntoNextRoom(Key.Up);
        Assert.AreEqual("dungeon:1,0", play.Room.Key);
        Assert.IsTrue(achievements.IsUnlocked(CourtyardAchievements.Cartographer), "Every chamber has been visited.");

        // The Moss Warden: daze it with the boomerang, strike while it reels.
        var warden = play.Actors.OfType<Warden>().Single();
        h.Step(140);
        for (int round = 0; round < 400 && warden.Alive && warden.State != WardenState.Dying; round++)
        {
            if (warden.Vulnerable)
            {
                // Pick the closest reachable side of the dazed Warden and strike toward it.
                var interior = play.Room.Bounds.Inflate(-20);
                var (spot, facing) = new[]
                {
                    (warden.Position + new Vector2(0, 14), Dir.Up),
                    (warden.Position + new Vector2(0, -30), Dir.Down),
                    (warden.Position + new Vector2(-22, -4), Dir.Right),
                    (warden.Position + new Vector2(22, -4), Dir.Left),
                }.Where(c => interior.Contains(c.Item1)).OrderBy(c => (c.Item1 - play.Hero.Position).Length()).First();
                h.SteerFor(spot, 24);
                h.Face(facing);
                h.Swing();
                continue;
            }
            var aim = warden.Center + new Vector2(0, -8) - play.Hero.Center;
            if (MathF.Abs(aim.X) < 8 && play.ActiveBoomerang is null) { h.ThrowBoomerang(aim.Y < 0 ? Dir.Up : Dir.Down); continue; }
            h.SteerFor(new Vector2(warden.Center.X, play.Room.TileToWorld(0, 8.5f).Y), 20);
        }
        Assert.AreEqual(WardenState.Dying, warden.State, "The Warden falls.");
        Assert.IsTrue(achievements.IsUnlocked(CourtyardAchievements.Warden));
        Assert.IsTrue(new AchievementTracker(CourtyardAchievements.Catalog, new SaveStore(CourtyardKeyGame.GameId, h.SaveDir)).IsUnlocked(CourtyardAchievements.Warden), "Unlocks are on disk.");
        h.WaitUntil(() => play.Actors.OfType<Pickup>().Any(p => p.Kind == PickupKind.HeartContainer), 600, "the heart container");
        int maxBefore = play.Hero.Health.Max;
        var container = play.Actors.OfType<Pickup>().First(p => p.Kind == PickupKind.HeartContainer);
        h.Step(30);
        h.WalkTo(container.Position, 600, 3f);
        h.WaitUntil(() => h.DialogueOpen || play.Hero.Health.Max > maxBefore, 300, "the heart container");
        h.ClearDialogue();
        Assert.AreEqual(maxBefore + 2, play.Hero.Health.Max);
        Assert.IsTrue(play.Progress.Has(PlayScene.BossFlag));

        // Claim the Courtyard Key from its pedestal.
        h.Step(180);
        h.GoToTile(10, 7);
        h.WalkTo(play.Room.TileToWorld(10.5f, 6.7f), 300, 2f);
        h.Face(Dir.Up);
        h.Press(Key.Z);
        h.WaitUntil(() => h.DialogueOpen, 300, "the key caption");
        while (h.DialogueOpen) h.Press(Key.Z);
        Assert.IsTrue(play.Progress.HasCourtyardKey);
        h.WaitUntil(() => h.Game.Scenes.Current is EndingScene, 600, "the ending");
        Assert.IsTrue(h.Game.Saves.Load()!.HasCourtyardKey, "The finished adventure is saved.");
    }
}

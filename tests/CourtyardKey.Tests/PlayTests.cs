using System.Numerics;
using CourtyardKey.Content;
using CourtyardKey.Entities;
using CourtyardKey.Model;
using CourtyardKey.Scenes;
using MicroEngine.Input;

namespace CourtyardKey.Tests;

/// <summary>Plays the real game headlessly through keyboard input.</summary>
[TestClass]
public sealed class PlayTests
{
    [TestMethod]
    public void WalkingOffAnEdgeScrollsToTheNeighbourAndRecordsTheVisit()
    {
        using var h = new Harness("road");
        h.Step(40);
        int entered = 0;
        h.Play.RoomEntered += _ => entered++;
        Assert.AreEqual("overworld:1,1", h.Play.Room.Key);
        Assert.IsFalse(h.Play.Progress.WasVisited("overworld", 0, 1));
        h.WalkToTile(10, 4.8f);
        h.Platform.Keys.Set(Key.Left, true);
        h.WaitUntil(() => h.Play.Mode == PlayMode.Scroll, 400, "scroll start");
        Assert.IsNotNull(h.Play.ScrollingFrom, "The old room stays drawn while scrolling.");
        Assert.AreEqual("overworld:0,1", h.Play.Room.Key, "The new room loads at the start of the scroll.");
        h.WaitUntil(() => h.Play.Mode == PlayMode.Play, 120, "scroll end");
        h.Platform.Keys.Set(Key.Left, false);
        Assert.AreEqual(1, entered);
        Assert.IsNull(h.Play.ScrollingFrom);
        Assert.AreEqual(0, h.Play.LeavingActors.Count, "The previous room's actors are released.");
        Assert.IsTrue(h.Play.Progress.WasVisited("overworld", 0, 1));
        Assert.IsTrue(h.Play.Room.Bounds.Contains(h.Play.Hero.Position), "The hero ends inside the new room.");
        Assert.IsTrue(h.Play.Hero.Position.X > h.Play.Room.Bounds.Right - 32, "Entering from the east edge.");
    }

    [TestMethod]
    public void TheTitleMenuListsTheAchievements()
    {
        using var h = new Harness();
        h.Step(40);
        h.Press(Key.Down);
        h.Press(Key.Down);
        h.Press(Key.Down);
        h.Press(Key.Z);
        var list = h.Game.Scenes.Current as MicroEngine.UI.AchievementsScene;
        Assert.IsNotNull(list, "Achievements is the fourth title option.");
        Assert.AreEqual(CourtyardAchievements.Catalog.Length, h.Game.Achievements.Count);
        h.Press(Key.X);
        Assert.IsInstanceOfType<TitleScene>(h.Game.Scenes.Current);
    }

    [TestMethod]
    public void TheVillageCannotBeLeftBeforeTheElderGivesTheSword()
    {
        using var h = new Harness("village");
        h.Step(40);
        h.WalkToTile(1, 4.8f);
        h.Platform.Keys.Set(Key.Left, true);
        h.WaitUntil(() => h.DialogueOpen, 300, "the elder's warning");
        h.Platform.Keys.Set(Key.Left, false);
        h.ClearDialogue();
        Assert.AreEqual("overworld:1,2", h.Play.Room.Key);
    }

    [TestMethod]
    public void LockedDoorsConsumeOneSmallKeyAndStayOpen()
    {
        using var h = new Harness("dungeon");
        h.Step(40);
        h.WalkToTile(10, 2);
        h.WalkIntoNextRoom(Key.Up);
        Assert.AreEqual("dungeon:1,2", h.Play.Room.Key);
        var door = h.Play.Doors.Single(d => d.Side == Dir.Up);
        Assert.AreEqual(DoorKind.Locked, door.Kind);
        Assert.IsFalse(door.IsOpen);
        h.WalkToTile(10, 2.6f);
        h.Hold(Key.Up, 60);
        Assert.IsFalse(door.IsOpen, "Without a key the door stays shut.");
        h.Play.Progress.SmallKeys = 1;
        h.Hold(Key.Up, 14);
        Assert.IsTrue(door.IsOpen);
        Assert.AreEqual(0, h.Play.Progress.SmallKeys);
        Assert.IsTrue(h.Play.Progress.Has("door:dungeon:1,2:v"));
        h.WalkIntoNextRoom(Key.Up);
        Assert.AreEqual("dungeon:1,1", h.Play.Room.Key);
        var otherSide = h.Play.Doors.Single(d => d.Side == Dir.Down);
        Assert.IsTrue(otherSide.IsOpen, "The same door is open from the other side.");
    }

    [TestMethod]
    public void PushingTheBlockOntoThePlateRevealsTheKeyChest()
    {
        using var h = new Harness("blocks");
        h.Step(40);
        Assert.AreEqual("dungeon:0,2", h.Play.Room.Key);
        Assert.IsFalse(h.Play.Actors.OfType<Chest>().Any(), "The key chest is hidden until the puzzle is solved.");
        var block = h.Play.Actors.OfType<PushBlock>().Single(b => b.Cell == (8, 4));
        // Line up east of the block and lean left: it slides one tile per push until a statue stops it.
        h.WalkToTile(10, 4.75f);
        h.Hold(Key.Left, 300);
        Assert.AreEqual((3, 4), block.Cell);
        Assert.IsFalse(h.Play.RoomSolved);
        // Step around through the pen and push it up onto the plate.
        h.WalkToTile(4.5f, 5.75f);
        h.WalkToTile(3.5f, 5.75f);
        h.Hold(Key.Up, 80);
        h.Step(30);
        Assert.AreEqual((3, 3), block.Cell);
        Assert.IsTrue(h.Play.PlatePressed(3, 3));
        Assert.IsTrue(h.Play.RoomSolved);
        var chest = h.Play.Actors.OfType<Chest>().Single();
        Assert.AreEqual(Item.SmallKey, chest.Contents.Item);
    }

    [TestMethod]
    public void KnightShieldsBlockFrontalSwordStrikesUntilStunned()
    {
        using var h = new Harness("dungeon");
        h.Step(10);
        var play = h.Play;
        var knight = new Knight(play, play.Room.TileToWorld(10, 5));   // constructing an actor adds it to the room
        Assert.AreEqual(Dir.Down, knight.Facing);
        var front = knight.Center + new Vector2(0, 16);
        var side = knight.Center + new Vector2(16, 0);
        Assert.AreEqual(HitOutcome.Blocked, knight.OnHit(new Hit(1, front, HitKind.Sword)));
        Assert.AreEqual(4, knight.Health.Current);
        Assert.AreEqual(HitOutcome.Damaged, knight.OnHit(new Hit(1, side, HitKind.Sword)));
        Assert.AreEqual(3, knight.Health.Current);
        knight.Health.Update(1);
        Assert.AreEqual(HitOutcome.Stunned, knight.OnHit(new Hit(0, front, HitKind.Boomerang)));
        Assert.IsTrue(knight.Stunned);
        Assert.AreEqual(HitOutcome.Damaged, knight.OnHit(new Hit(1, front, HitKind.Sword)), "A stunned knight cannot block.");
    }

    [TestMethod]
    public void TheHeroTakesKnockbackAndIsBrieflyInvulnerable()
    {
        using var h = new Harness("dungeon");
        h.Step(10);
        var hero = h.Play.Hero;
        var start = hero.Position;
        int before = hero.Health.Current;
        Assert.IsTrue(hero.TakeDamage(2, start + new Vector2(-10, -4)));
        Assert.AreEqual(before - 2, hero.Health.Current);
        Assert.IsFalse(hero.TakeDamage(2, start), "Invulnerability frames ignore the follow-up hit.");
        h.Step(15);
        Assert.IsTrue(hero.Position.X > start.X + 10, "Knocked away from the attacker.");
        h.Step((int)(Combat.HeroInvulnerability * 60));
        Assert.IsTrue(hero.TakeDamage(1, start));
    }

    [TestMethod]
    public void TheWardenShrugsOffTheSwordUntilDazed()
    {
        using var h = new Harness("boss");
        h.Step(10);
        var warden = h.Play.Actors.OfType<Warden>().Single();
        h.Step(150);
        var from = warden.Center + new Vector2(0, 20);
        Assert.AreEqual(HitOutcome.Blocked, warden.OnHit(new Hit(1, from, HitKind.Sword)));
        Assert.AreEqual(Warden.MaxHealth, warden.Health.Current);
        Assert.AreEqual(HitOutcome.Stunned, warden.OnHit(new Hit(0, from, HitKind.Boomerang)));
        Assert.IsTrue(warden.Vulnerable);
        Assert.AreEqual(HitOutcome.Damaged, warden.OnHit(new Hit(1, from, HitKind.Sword)));
        Assert.AreEqual(Warden.MaxHealth - 1, warden.Health.Current);
    }

    [TestMethod]
    public void ShopSellsTonicAndOneHeartyPie()
    {
        using var h = new Harness("road");
        h.Step(5);
        var play = h.Play;
        play.Progress.Gems = 100;
        play.Hero.Health.SetCurrent(2);
        ShopScene.BuyTonic(play);
        Assert.AreEqual(play.Hero.Health.Max, play.Hero.Health.Current);
        Assert.AreEqual(100 - ShopScene.TonicPrice, play.Progress.Gems);
        int max = play.Hero.Health.Max;
        ShopScene.BuyPie(play);
        Assert.AreEqual(max + 2, play.Hero.Health.Max);
        int gems = play.Progress.Gems;
        ShopScene.BuyPie(play);
        Assert.AreEqual(gems, play.Progress.Gems, "The pie can only be bought once.");
    }

    [TestMethod]
    public void DeathOffersContinueAtTheLastEntranceKeepingItems()
    {
        using var h = new Harness("dungeon");
        h.Step(20);
        h.WalkToTile(10, 2);
        h.WalkIntoNextRoom(Key.Up);
        h.Play.Progress.SmallKeys = 1;
        h.Play.Hero.TakeDamage(99, h.Play.Hero.Position);
        h.WaitUntil(() => h.Game.Scenes.Current is GameOverScene, 400, "game over");
        h.Step(60);
        h.Press(Key.Z);
        h.Step(5);
        Assert.AreEqual("dungeon:1,3", h.Play.Room.Key, "Continue restarts at the dungeon entrance.");
        Assert.AreEqual(h.Play.Hero.Health.Max, h.Play.Hero.Health.Current);
        Assert.AreEqual(1, h.Play.Progress.SmallKeys);
        Assert.AreEqual(1, h.Play.Progress.Deaths);
        Assert.AreEqual(PlayMode.Play, h.Play.Mode);
    }
}

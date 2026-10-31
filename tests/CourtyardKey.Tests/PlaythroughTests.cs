using CourtyardKey.Content;
using CourtyardKey.Scenes;
using MicroEngine.Input;

namespace CourtyardKey.Tests;

/// <summary>
/// A scripted run of the opening, start to dungeon: title menu, the elder's sword, the road north,
/// the river bridge, the ruins and down the stairs into the Sunken Courtyard.
/// </summary>
[TestClass]
public sealed class PlaythroughTests
{
    [TestMethod]
    public void TheOpeningCanBeCompletedFromTheTitleToTheDungeon()
    {
        using var h = new Harness();
        h.Step(60);
        Assert.IsInstanceOfType<TitleScene>(h.Game.Scenes.Current);
        h.Press(Key.Z); // New Game (Continue is disabled without a save)
        h.WaitUntil(() => h.Game.Play is not null && !h.Game.Scenes.InTransition, 300, "the adventure to start");
        h.Step(40);
        var play = h.Play;
        Assert.AreEqual("overworld:1,2", play.Room.Key);
        Assert.IsFalse(play.Progress.HasSword);

        // Talk to Elder Oswin and receive the sword.
        h.WalkToTile(7.5f, 4.7f);
        h.Hold(Key.Up, 3);
        h.Press(Key.Z);
        Assert.IsTrue(h.DialogueOpen, "The elder starts talking.");
        h.ClearDialogue();
        Assert.IsTrue(play.Progress.HasSword);
        Assert.IsTrue(h.Game.Saves.HasSave, "Receiving the sword saves the game.");

        // North along the old road.
        h.WalkToTile(9.9f, 4.7f);
        h.WalkIntoNextRoom(Key.Up);
        Assert.AreEqual("overworld:1,1", play.Room.Key);
        h.WalkToTile(9.9f, 4.8f);
        h.WalkIntoNextRoom(Key.Right);
        Assert.AreEqual("overworld:2,1", play.Room.Key);

        // Over the bridge and up to the ruins.
        h.WalkToTile(14.9f, 4.8f);
        h.WalkIntoNextRoom(Key.Up);
        Assert.AreEqual("overworld:2,0", play.Room.Key);
        h.WalkToTile(15.6f, 7.0f);
        h.WalkToTile(15.6f, 4.6f);
        h.WalkToTile(14f, 4.6f);
        h.Platform.Keys.Set(Key.Up, true);
        h.WaitUntil(() => play.Area.Id == Dungeon.Id && play.Mode == PlayMode.Play, 600, "the stairs down");
        h.Platform.Keys.Set(Key.Up, false);

        Assert.AreEqual("dungeon:1,3", play.Room.Key);
        Assert.AreEqual(Dungeon.EntranceCheckpoint, play.Progress.Entrance, "The dungeon entrance is the new checkpoint.");
        var saved = h.Game.Saves.Load();
        Assert.IsNotNull(saved);
        Assert.IsTrue(saved.HasSword);
        Assert.AreEqual(Dungeon.Id, saved.Entrance.Area);
        Assert.IsFalse(play.Hero.Health.IsDead);
    }
}

using TrailDuel.Scenes;

namespace TrailDuel.Tests;

/// <summary>Moves can be inspected from the creature summary and from the battle move menu.</summary>
[TestClass]
public sealed class MoveInspectionTests
{
    [TestMethod]
    public void SummaryOpensAMovesPageThatBrowsesMovesAndCreatures()
    {
        using var h = new Harness("summary");
        h.Step(30);
        var summary = (SummaryScene)h.Game.Scenes.Current!;
        Assert.AreEqual(-1, summary.MoveCursor, "The summary opens on the overview.");

        h.Press("down");
        Assert.AreEqual(0, summary.MoveCursor);
        h.Press("down");
        Assert.AreEqual(1, summary.MoveCursor);
        h.Press("up"); h.Press("up");
        Assert.AreEqual(h.State.Party[0].Moves.Count - 1, summary.MoveCursor, "The cursor wraps.");

        h.Press("right");
        Assert.AreEqual(1, summary.CreatureIndex, "Left/right still browse the party on the moves page.");
        Assert.IsTrue(summary.MoveCursor < h.State.Party[1].Moves.Count);

        h.Press("cancel");
        Assert.AreSame(summary, h.Game.Scenes.Current, "Cancel first returns to the overview.");
        Assert.AreEqual(-1, summary.MoveCursor);
        h.Press("cancel");
        Assert.AreNotSame(summary, h.Game.Scenes.Current, "A second cancel closes the summary.");
    }

    [TestMethod]
    public void SummaryDrawsBothPagesAndStaysOpen()
    {
        using var h = new Harness("summary");
        h.Step(30);
        var summary = (SummaryScene)h.Game.Scenes.Current!;
        summary.Draw(new RecordingRenderer());
        h.Press("down");
        summary.Draw(new RecordingRenderer());
        h.Step(60);
        Assert.AreSame(summary, h.Game.Scenes.Current);
    }

    [TestMethod]
    public void BattleMoveMenuTogglesTheMoveCardWithTheActionButton()
    {
        using var h = new Harness("battle");
        var battle = (BattleScene)h.Game.Scenes.Current!;
        for (int i = 0; i < 1200 && !battle.AwaitingChoice; i++) h.Step();
        Assert.IsTrue(battle.AwaitingChoice);
        h.Press("confirm", 6);
        Assert.IsTrue(battle.InMoveMenu);
        Assert.IsFalse(battle.MoveInfoOpen);
        h.Press("action", 6);
        Assert.IsTrue(battle.MoveInfoOpen);
        h.Press("right", 6);
        Assert.AreEqual(1, battle.MoveCursor, "The card follows the cursor; choosing moves still works.");
        h.Press("action", 6);
        Assert.IsFalse(battle.MoveInfoOpen);
        Assert.IsTrue(battle.InMoveMenu, "Toggling the card never picks a move.");
    }

    /// <summary>
    /// The move menu is a grid two wide: a direction moves one cell and stops at the edge (no wrapping), and opposite
    /// directions pressed in the same update cancel out, as in every other list of the game.
    /// </summary>
    [TestMethod]
    public void BattleMoveMenuIsATwoByTwoGridThatStopsAtItsEdges()
    {
        using var h = new Harness("battle");
        var battle = (BattleScene)h.Game.Scenes.Current!;
        for (int i = 0; i < 1200 && !battle.AwaitingChoice; i++) h.Step();
        h.Press("confirm", 6);
        Assert.IsTrue(battle.InMoveMenu);
        Assert.AreEqual(4, battle.Battle.Player.Moves.Count, "the debug start's first creature knows four moves");
        var path = new List<int>();
        foreach (string direction in new[] { "right", "right", "down", "down", "left", "left", "up", "up" })
        {
            h.Press(direction, 6);
            path.Add(battle.MoveCursor);
        }
        CollectionAssert.AreEqual(new[] { 1, 1, 3, 3, 2, 2, 0, 0 }, path);

        h.Game.Input.SetForced("left", true);
        h.Game.Input.SetForced("right", true);
        h.Step(2);
        h.Game.Input.SetForced("left", false);
        h.Game.Input.SetForced("right", false);
        h.Step(4);
        Assert.AreEqual(0, battle.MoveCursor, "left and right together move nothing");
    }
}

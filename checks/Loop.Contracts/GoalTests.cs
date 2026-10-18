using FirstPlayable;
using FirstPlayable.Model;
using MicroEngine.Loop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Loop.Contracts;

[TestClass]
public sealed class GoalTests
{
    private static readonly TimeSpan Step = TimeSpan.FromSeconds(1d / 60);
    private static readonly InputSnapshot Neutral = new([], []);
    private static readonly InputSnapshot Right = new(["right"], []);
    private static GameModel Enabled(bool obstacle = false) => new(new(TimeSpan.FromMilliseconds(100)), obstacle ? [new(180, 260, 64, 40)] : [], true);
    private static (float, float, float, bool, bool) State(GameModel game) => (game.X, game.Y, game.VerticalVelocity, game.Grounded, game.Completed);
    private static int CompleteCourse(GameModel game)
    {
        for (int i = 1; i <= 600; i++)
        {
            game.Update(Step, new(["right"], game.Grounded && game.X < 260 ? ["jump"] : []));
            if (game.Completed) { Assert.IsTrue(game.X > 556); Assert.AreEqual(268f, game.Y); return i; }
        }
        Assert.Fail("Course did not complete in bounded input sequence."); return 0;
    }

    [TestMethod]
    public void ExistingConstructorsStayGoalDisabledAndRetainBoundaryMovement()
    {
        foreach (var game in new[] { new GameModel(), new(new PlatformerOptions(TimeSpan.Zero)), new(new PlatformerOptions(TimeSpan.Zero), []), new(new PlatformerOptions(TimeSpan.Zero), [], false) })
        {
            Assert.IsFalse(game.GoalEnabled); game.Update(TimeSpan.MaxValue, Right); Assert.AreEqual(616f, game.X); Assert.IsFalse(game.Completed);
            game.Update(TimeSpan.FromSeconds(1), new(["left"], [])); Assert.AreEqual(436f, game.X);
            game.Update(Step, new([], ["jump"])); Assert.IsFalse(game.Grounded);
        }
        Assert.AreEqual(new Rect(580, 260, 40, 40), GameModel.Goal); Assert.IsTrue(Enabled().GoalEnabled);
    }

    [TestMethod]
    public void StrictGoalContactRequiresActualGroundedOverlap()
    {
        var game = Enabled(); game.Update(TimeSpan.FromTicks(28_666_666), Right);
        Assert.AreEqual(556f, game.X); Assert.IsFalse(game.Completed);
        game.Update(TimeSpan.FromMilliseconds(1), Right); Assert.IsTrue(game.Completed); Assert.IsTrue(game.Grounded); Assert.AreEqual(0f, game.VerticalVelocity);
        var airborne = Enabled(); airborne.Update(TimeSpan.FromTicks(28_666_666), Right);
        airborne.Update(Step, new(["right"], ["jump"])); Assert.IsTrue(airborne.X > 556); Assert.IsFalse(airborne.Completed); Assert.IsFalse(airborne.Grounded);
        for (int i = 0; i < 100 && !airborne.Completed; i++) airborne.Update(Step, Neutral);
        Assert.IsTrue(airborne.Completed); Assert.AreEqual(268f, airborne.Y);
        var sameEdge = Enabled(); sameEdge.Update(TimeSpan.FromTicks(28_666_666), Right);
        sameEdge.Update(TimeSpan.FromSeconds(1), new(["right"], ["jump"]));
        Assert.IsTrue(sameEdge.Completed); Assert.AreEqual(616f, sameEdge.X); // Landing/finish edge is not also a reset.
    }

    [TestMethod]
    public void CourseCompletionFreezesAndFreshResetConsumesMovementAndBufferedJump()
    {
        var game = Enabled(true); CompleteCourse(game); var completed = State(game); var solids = game.Obstacles;
        game.Update(TimeSpan.MaxValue, new(["right", "jump"], ["Jump", "unknown"])); Assert.AreEqual(completed, State(game));
        game.Update(Step, new(["right"], ["jump", "jump"])); Assert.AreEqual((40f, 268f, 0f, true, false), State(game)); Assert.AreSame(solids, game.Obstacles);
        game.Update(Step, Neutral); Assert.AreEqual((40f, 268f, 0f, true, false), State(game));
        game.Update(Step, new([], ["jump"])); Assert.IsFalse(game.Grounded); Assert.IsTrue(game.VerticalVelocity < 0);
    }

    [TestMethod]
    public void InvalidUpdatesAreAtomicAndResetReplayMatchesFreshConfiguredController()
    {
        var game = Enabled(true); int steps = CompleteCourse(game); var completed = State(game);
        Assert.ThrowsExactly<ArgumentNullException>(() => game.Update(TimeSpan.Zero, null!));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => game.Update(TimeSpan.Zero, Neutral)); Assert.AreEqual(completed, State(game));
        game.Update(Step, new(["left"], ["jump"])); var fresh = Enabled(true);
        Assert.AreEqual(State(fresh), State(game));
        for (int i = 0; i < steps; i++)
        {
            var input = new InputSnapshot(["right"], fresh.Grounded && fresh.X < 260 ? ["jump"] : []);
            fresh.Update(Step, input); game.Update(Step, input); Assert.AreEqual(State(fresh), State(game));
        }
        Assert.IsTrue(game.Completed); Assert.IsTrue(fresh.Completed);
    }

    [TestMethod]
    public void BufferedLandingLaunchDoesNotBecomeGroundedCompletion()
    {
        var game = Enabled(); game.Update(TimeSpan.FromTicks(28_666_666), Right); game.Update(Step, new(["right"], ["jump"]));
        for (int i = 0; i < 100 && !(game.VerticalVelocity > 0 && game.Y > 260); i++) game.Update(Step, Neutral);
        Assert.IsFalse(game.Grounded); Assert.IsFalse(game.Completed);
        game.Update(Step, new([], ["jump"]));
        for (int i = 0; i < 10 && game.VerticalVelocity >= 0; i++) game.Update(Step, Neutral);
        Assert.IsTrue(game.VerticalVelocity < 0); Assert.IsFalse(game.Grounded); Assert.IsFalse(game.Completed);
        for (int i = 0; i < 100 && !game.Completed; i++) game.Update(Step, Neutral);
        Assert.IsTrue(game.Completed);
    }

    private class Canvas : ICanvas
    {
        public readonly List<(Rect, Rgba)> Fills = [];
        public void Clear(Rgba color) => Assert.Fail("Goal must not clear the world.");
        public void Fill(Rect rect, Rgba color) => Fills.Add((rect, color));
    }
    private sealed class Labels : Canvas, ILabelCanvas
    {
        public readonly List<(string, int, int, int, Rgba)> Calls = [];
        public void Label(string text, int x, int y, int size, Rgba color) => Calls.Add((text, x, y, size, color));
    }

    [TestMethod]
    public void GoalPresentationIsPureOptionalAndDisabledHasNoEffects()
    {
        var game = Enabled(true); var pending = new Labels(); var before = State(game); GoalRenderer.Draw(pending, game);
        CollectionAssert.AreEqual(new[] { (GameModel.Goal, new Rgba(70, 200, 210)) }, pending.Fills);
        CollectionAssert.AreEqual(new[] { ("Reach the cyan goal", 20, 20, 20, new Rgba(240, 240, 245)) }, pending.Calls); Assert.AreEqual(before, State(game));
        CompleteCourse(game); before = State(game); var done = new Labels(); GoalRenderer.Draw(done, game);
        CollectionAssert.AreEqual(new[] { (GameModel.Goal, new Rgba(80, 200, 120)) }, done.Fills);
        CollectionAssert.AreEqual(new[] { ("Goal reached - Space to restart", 20, 20, 20, new Rgba(240, 240, 245)) }, done.Calls); Assert.AreEqual(before, State(game));
        var primitive = new Canvas(); GoalRenderer.Draw(primitive, game); CollectionAssert.AreEqual(done.Fills, primitive.Fills);
        var disabled = new Labels(); GoalRenderer.Draw(disabled, new()); Assert.AreEqual(0, disabled.Fills.Count); Assert.AreEqual(0, disabled.Calls.Count);
        Assert.ThrowsExactly<ArgumentNullException>(() => GoalRenderer.Draw(null!, game)); Assert.ThrowsExactly<ArgumentNullException>(() => GoalRenderer.Draw(primitive, null!));
    }
}

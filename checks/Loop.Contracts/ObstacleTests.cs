using FirstPlayable.Model;
using MicroEngine.Loop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Loop.Contracts;

[TestClass]
public sealed class ObstacleTests
{
    private static readonly TimeSpan Step = TimeSpan.FromSeconds(1d / 60);
    private static readonly InputSnapshot Empty = new([], []);
    private static readonly InputSnapshot Right = new(["right"], []);
    private static readonly InputSnapshot Left = new(["left"], []);
    private static readonly InputSnapshot Jump = new([], ["jump"]);
    private static readonly StaticObstacle Box = new(100, 260, 120, 40);
    private static GameModel Model(params StaticObstacle[] solids) => new(new PlatformerOptions(TimeSpan.Zero), solids);
    private static (float, float, float, bool) State(GameModel game) => (game.X, game.Y, game.VerticalVelocity, game.Grounded);

    [TestMethod]
    public void EmptySolidsPreserveMixedBaselineTrace()
    {
        var baseline = new GameModel(); var game = Model();
        for (var i = 0; i < 200; i++)
        {
            var input = new InputSnapshot(i % 2 == 0 ? ["right"] : ["left"], i % 23 == 0 ? ["jump"] : []);
            baseline.Update(Step, input); game.Update(Step, input);
            Assert.AreEqual(State(baseline), State(game));
        }
    }

    [TestMethod]
    [DataRow(float.NaN, 0f, 10f, 10f)]
    [DataRow(0f, float.PositiveInfinity, 10f, 10f)]
    [DataRow(-1f, 0f, 10f, 10f)]
    [DataRow(0f, 0f, 0f, 10f)]
    [DataRow(0f, 0f, 10f, -1f)]
    [DataRow(630f, 0f, 20f, 10f)]
    [DataRow(0f, 290f, 10f, 20f)]
    public void InvalidGeometryRejected(float x, float y, float width, float height)
        => Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new StaticObstacle(x, y, width, height));

    [TestMethod]
    public void ValidationOrderOwnershipAndEnumerationAreExplicit()
    {
        var options = new PlatformerOptions(TimeSpan.Zero);
        Assert.AreEqual("options", Assert.ThrowsExactly<ArgumentNullException>(() => new GameModel(null!, null!)).ParamName);
        Assert.AreEqual("obstacles", Assert.ThrowsExactly<ArgumentNullException>(() => new GameModel(options, null!)).ParamName);
        Assert.AreEqual("obstacles", Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Model(default(StaticObstacle))).ParamName);
        Assert.AreEqual("obstacles", Assert.ThrowsExactly<ArgumentException>(() => Model(new StaticObstacle(40, 268, 24, 32))).ParamName);
        var source = new[] { Box }; var calls = 0;
        IEnumerable<StaticObstacle> Once() { calls++; yield return source[0]; }
        var game = new GameModel(options, Once()); source[0] = new(200, 260, 20, 40);
        Assert.AreEqual(1, calls); Assert.AreEqual(Box, game.Obstacles[0]);
        Assert.ThrowsExactly<NotSupportedException>(() => ((IList<StaticObstacle>)game.Obstacles)[0] = source[0]);
        var error = new ApplicationException("enumeration");
        IEnumerable<StaticObstacle> Broken() { yield return Box; throw error; }
        Assert.AreSame(error, Assert.ThrowsExactly<ApplicationException>(() => new GameModel(options, Broken())));
    }

    [TestMethod]
    public void SweepsChooseNearestWallWithoutTunnelingOrOrderDependence()
    {
        var near = new StaticObstacle(100, 240, 20, 60); var far = new StaticObstacle(150, 240, 20, 60);
        foreach (var solids in new[] { new[] { far, near }, new[] { near, near, far } })
        {
            var game = Model(solids); game.Update(TimeSpan.FromSeconds(2), Right);
            Assert.AreEqual(76f, game.X); Assert.IsTrue(game.Grounded);
            game.Update(Step, Right); Assert.AreEqual(76f, game.X);
            game.Update(Step, Left); Assert.IsTrue(game.X < 76);
        }
        var left = Model(new StaticObstacle(10, 240, 20, 60)); left.Update(TimeSpan.FromSeconds(2), Left);
        Assert.AreEqual(30f, left.X); left.Update(Step, Right); Assert.IsTrue(left.X > 30);
        var corner = Model(new StaticObstacle(100, 200, 20, 68)); corner.Update(TimeSpan.FromSeconds(1), Right);
        Assert.AreEqual(220f, corner.X);
        var edge = Model(new StaticObstacle(64, 268, 10, 32)); edge.Update(Step, Right); Assert.AreEqual(40f, edge.X);
        edge.Update(Step, Left); Assert.IsTrue(edge.X < 40);
    }

    private static void ApproachTop(GameModel game)
    {
        game.Update(Step, new InputSnapshot(["right"], ["jump"]));
        for (var i = 1; i < 20; i++) game.Update(Step, Right);
    }
    private static int UpdatesUntilTop()
    {
        var game = Model(Box); ApproachTop(game);
        for (var i = 1; i <= 100; i++) { game.Update(Step, Empty); if (game.Grounded) { Assert.AreEqual(228f, game.Y); return i; } }
        throw new AssertFailedException("Did not land on raised top.");
    }

    [TestMethod]
    public void TopSupportsJumpAndWalkOffWithoutAirborneRegrounding()
    {
        var game = Model(Box); ApproachTop(game);
        for (var i = 0; i < UpdatesUntilTop(); i++) game.Update(Step, Empty);
        Assert.IsTrue(game.Grounded); Assert.AreEqual(228f, game.Y);
        game.Update(Step, Jump); Assert.IsFalse(game.Grounded); Assert.IsTrue(game.Y < 228);
        game = Model(Box); ApproachTop(game);
        for (var i = 0; i < UpdatesUntilTop(); i++) game.Update(Step, Empty);
        for (var i = 0; i < 41; i++) game.Update(Step, Right);
        Assert.IsFalse(game.Grounded); Assert.IsTrue(game.Y > 228);
        for (var i = 0; i < 40; i++) game.Update(Step, Empty);
        Assert.IsTrue(game.Grounded); Assert.AreEqual(268f, game.Y);
    }

    [TestMethod]
    public void UndersideStopsRiseWithoutGrantingGroundOrBlockingMotionAway()
    {
        var game = Model(new StaticObstacle(40, 220, 24, 16)); game.Update(Step, Jump);
        for (var i = 0; i < 20 && game.VerticalVelocity < 0; i++) game.Update(Step, Empty);
        Assert.AreEqual(236f, game.Y); Assert.AreEqual(0f, game.VerticalVelocity); Assert.IsFalse(game.Grounded);
        game.Update(Step, Empty); Assert.IsTrue(game.Y > 236); Assert.IsFalse(game.Grounded);
    }

    [TestMethod]
    [DataRow(0, false)]
    [DataRow(100, true)]
    public void RaisedContactBufferConsumesOnceAndContinuesUpward(int milliseconds, bool buffered)
    {
        var game = new GameModel(new PlatformerOptions(TimeSpan.FromMilliseconds(milliseconds)), [Box]);
        ApproachTop(game);
        for (var i = 1; i < UpdatesUntilTop(); i++) game.Update(Step, Empty);
        game.Update(Step, Jump); Assert.AreEqual(228f, game.Y); Assert.AreEqual(!buffered, game.Grounded);
        if (buffered)
        {
            Assert.AreEqual(-300f, game.VerticalVelocity);
            game.Update(Step, Empty); Assert.IsTrue(game.Y < 228); Assert.AreEqual(-285f, game.VerticalVelocity, .001f);
            for (var i = 0; i < 100; i++) game.Update(Step, Empty);
            Assert.IsTrue(game.Grounded); Assert.AreEqual(228f, game.Y);
        }
    }

    [TestMethod]
    public void RaisedTopSweepDoesNotTunnelAndExpiredIntentDoesNotLaunch()
    {
        var game = new GameModel(new PlatformerOptions(TimeSpan.FromTicks(Step.Ticks - 1)), [Box]);
        ApproachTop(game);
        for (var i = 2; i < UpdatesUntilTop(); i++) game.Update(Step, Empty);
        game.Update(Step, Jump); game.Update(Step, Empty);
        Assert.IsTrue(game.Grounded); Assert.AreEqual(228f, game.Y); Assert.AreEqual(0f, game.VerticalVelocity);
        var swept = Model(Box); ApproachTop(swept);
        swept.Update(TimeSpan.FromSeconds(2), Empty);
        Assert.AreEqual(228f, swept.Y); Assert.IsTrue(swept.Grounded);
    }
}

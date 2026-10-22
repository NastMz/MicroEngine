using MicroEngine.Loop;
using MicroEngine.TwoD;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Platformer.Model;
using Platformer.Checks;
namespace Platformer.Contracts;

[TestClass]
public sealed class PatrolTests
{
    [TestMethod]
    public void PatrolStartsAndReflectsAtExactBoundsAndOvershoot()
    {
        var game=new LevelGame(2);
        Assert.AreEqual(new Rect(1152,392,40,40),game.EnemyBody);
        for(int i=0;i<16;i++)game.Update(TimeSpan.FromMilliseconds(100),new([],[]));
        Assert.AreEqual(1248f,game.EnemyBody!.Value.X,0.01f);
        game.Update(TimeSpan.FromMilliseconds(100),new([],[]));Assert.AreEqual(1242f,game.EnemyBody!.Value.X,0.01f);
        for(int i=0;i<200;i++)
        {
            game.Update(TimeSpan.FromMilliseconds(73),new([],[]));
            Assert.IsTrue(game.EnemyBody!.Value.X>=1056 && game.EnemyBody.Value.X<=1248);
        }
    }
    [TestMethod]
    public void ContactAtMaximumDeltaFreezesAndRetryRestoresInitialDirection()
    {
        var game=new LevelGame(2);Course.Run(game,anticipateEnemies:false,stopWhen:g=>g.Body.X>=960);
        for(int i=0;i<100&&!game.Grounded;i++)game.Update(Course.Step,new([],[]));
        for(int i=0;i<100&&!game.Dead;i++)game.Update(TimeSpan.FromMilliseconds(100),new(["right"],[]));
        Assert.IsTrue(game.Dead);Assert.IsTrue(StaticWorld.Overlaps(game.Body,game.EnemyBody!.Value));
        Assert.IsFalse(StaticWorld.Overlaps(game.Body,game.Danger));Assert.AreEqual(2,game.CollectedCount);
        var frozen=(game.Body,game.EnemyBody);
        game.Update(Course.Step,new(["right"],[]));Assert.AreEqual(frozen,(game.Body,game.EnemyBody));
        game.Update(Course.Step,new(["right"],["jump"]));
        Assert.AreEqual(new Rect(48,392,28,40),game.Body);Assert.AreEqual(0,game.CollectedCount);
        Assert.AreEqual(new Rect(1152,392,40,40),game.EnemyBody);
        game.Update(TimeSpan.FromMilliseconds(100),new([],[]));Assert.AreEqual(1158f,game.EnemyBody!.Value.X,0.01f);
    }
    [TestMethod]
    public void InvalidInputDoesNotAdvanceEnemyAndFirstLevelHasNone()
    {
        var game=new LevelGame(2);var before=game.EnemyBody;Assert.IsNotNull(before);
        Assert.ThrowsExactly<ArgumentNullException>(()=>game.Update(TimeSpan.Zero,null!));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(()=>game.Update(TimeSpan.FromMilliseconds(101),new([],[])));
        Assert.AreEqual(before,game.EnemyBody);
        var first=new LevelGame();Assert.IsNull(first.EnemyBody);Assert.AreEqual(517,Course.Run(first));
    }
    [TestMethod]
    public void PublicAvoidanceRouteCompletesAndTerminalEnemyRemainsFrozen()
    {
        var game=new LevelGame(2);Assert.IsNotNull(game.EnemyBody);
        Assert.IsTrue(Course.Run(game)<=2000);Assert.IsTrue(game.Completed);
        var enemy=game.EnemyBody;game.Update(Course.Step,new([],[]));Assert.AreEqual(enemy,game.EnemyBody);
    }
}

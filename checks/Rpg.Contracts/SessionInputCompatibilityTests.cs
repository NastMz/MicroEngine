using MicroEngine.Loop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rpg.Model;
using Rpg.Checks;
namespace Rpg.Contracts;

[TestClass]
public sealed class SessionInputCompatibilityTests
{
    [TestMethod]
    public void AddedDesktopSessionActionsDoNotPauseOrRestartLegacyRpg()
    {
        var game=new RpgSession();var reference=new RpgSession();
        game.Update(Course.Step,new(["right"],["pause","restart"]));reference.Update(Course.Step,new(["right"],[]));
        Assert.AreEqual(reference.Body,game.Body);Assert.AreEqual(reference.Mode,game.Mode);
        Course.Open(game);Assert.AreEqual(RpgMode.Battle,game.Mode);
        var before=(game.Body,game.Mode,game.Battle,game.SelectedAction);
        game.Update(Course.Step,new(["pause","restart"],["pause","restart"]));
        Assert.AreEqual(before,(game.Body,game.Mode,game.Battle,game.SelectedAction));
    }
}

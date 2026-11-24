using TrailDuel.Scenes;

namespace TrailDuel.Tests;

/// <summary>
/// The ending's three parts follow the clock when nobody presses anything: the champion card stays 7 seconds, then
/// the credits roll to their end and wait 10 seconds more before going back to the title. The update numbers are the
/// ones the scene has always had at 60 updates a second.
/// </summary>
[TestClass]
public sealed class CreditsFlowTests
{
    /// <summary>The updates at which the card leaves and at which the credits end, pressing confirm from <paramref name="confirmFrom"/> on (never when null).</summary>
    private static (int CardLeft, int Ended) Play(int? confirmFrom)
    {
        using var h = new Harness("town");
        h.Step(30);
        int step = 0, cardLeft = -1, ended = -1;
        var credits = new CreditsScene(h.Game, () => ended = step);
        h.Game.Scenes.Push(credits);
        for (step = 1; step < 20000 && ended < 0; step++)
        {
            if (step == confirmFrom) h.Game.Input.SetForced("confirm", true);
            h.Step();
            if (cardLeft < 0 && !credits.OnChampionCard) cardLeft = step;
        }
        return (cardLeft, ended);
    }

    [TestMethod]
    public void WithoutInputTheCardLeavesAfterSevenSecondsAndTheRollEndsByItself()
    {
        Assert.AreEqual((CardLeftAlone, EndedAlone), Play(confirmFrom: null));
    }

    [TestMethod]
    public void ConfirmSkipsTheCardAfterTwoAndAHalfSecondsAndHurriesTheRoll()
    {
        Assert.AreEqual((CardLeftPressed, EndedPressed), Play(confirmFrom: 200));
    }

    private const int CardLeftAlone = 420, EndedAlone = 3867, CardLeftPressed = 200, EndedPressed = 3647;
}

using NeonRift.Content;
using NeonRift.Gameplay;
using static NeonRift.Tests.Harness;

namespace NeonRift.Tests;

[TestClass]
public sealed class PlaythroughTests
{
    /// <summary>
    /// The autopilot (invulnerable to shots, not to pits or lava) must reach and destroy every boss.
    /// This proves each stage is traversable with the real jump and that every boss can be killed.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    public void Autopilot_clears_the_stage(int index)
    {
        var session = new Session { Lives = 99 };
        var stage = Stage(Stages.All[index], session);
        stage.Player.God = true;
        var bot = new Autopilot();
        int steps = 0;
        var falls = new List<string>();
        var previous = PlayerState.Alive;
        while (!stage.Finished && steps < 60 * 60 * 6)
        {
            Step(stage, bot.Next(stage, Dt));
            if (stage.Player.State == PlayerState.Dying && previous == PlayerState.Alive) falls.Add($"col {stage.Player.Position.X / 16:0}");
            previous = stage.Player.State;
            steps++;
        }
        string turrets = stage.Boss is Dreadnought d ? $", turrets alive {d.Turrets.Count(t => t.Alive)}" : "";
        Assert.IsTrue(stage.Finished, $"Stage {index + 1} not cleared: phase {stage.Phase}, x {stage.Player.Position.X:0}, camera {stage.Camera.Position.X:0}, deaths {session.Deaths}, boss hp {stage.Boss?.Health}{turrets}.");
        Assert.AreEqual(0, session.Deaths, $"The autopilot fell at {string.Join(", ", falls)}; a jump may be impossible.");
    }
}

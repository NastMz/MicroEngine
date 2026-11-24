using MicroEngine.Core;
using TrailDuel;
using TrailDuel.Rules;

namespace TrailDuel.Tests;

/// <summary>Runs the real game headlessly over saves kept in memory, which a second game can be started on.</summary>
internal sealed class Harness : IDisposable
{
    public MemorySaveStorage Saves { get; } = new();
    public TrailDuelGame Game { get; }
    public HeadlessPlatform Platform { get; } = new();

    public Harness(string? debug = null)
    {
        Game = new TrailDuelGame(debug ?? "none");
        Game.InitializeForTest(Platform, 3, Saves);
    }

    public GameState State => Game.State;

    public void Step(int frames = 1)
    {
        for (int i = 0; i < frames; i++) Game.StepForTest();
    }

    /// <summary>Holds an action for a number of frames, then releases it for one frame.</summary>
    public void Hold(string action, int frames)
    {
        Game.Input.SetForced(action, true);
        Step(frames);
        Game.Input.SetForced(action, false);
        Step(1);
    }

    public void Press(string action, int after = 4)
    {
        Game.Input.SetForced(action, true);
        Step(2);
        Game.Input.SetForced(action, false);
        Step(after);
    }

    public void Dispose() { }
}

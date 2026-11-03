using System.Diagnostics.CodeAnalysis;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Platform;

namespace MicroEngine.Testing;

/// <summary>
/// Runs a game headless and deterministic with a state trace (one hash per fixed step, with section hashes), the way
/// the simulation-determinism checks run every demo script twice. Shared by the engine's and the games' test projects
/// (linked source).
/// </summary>
internal static class StateTraceRuns
{
    /// <summary>The repository root (the folder holding MicroEngine.slnx) above the test output.</summary>
    public static string RepositoryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "MicroEngine.slnx"))) return dir.FullName;
        throw new InvalidOperationException("The repository root (MicroEngine.slnx) was not found above the test output.");
    }

    /// <summary>Reads a script of the repository, such as <c>games/MeadowRun/scripts/menus.script</c>.</summary>
    public static InputScript Script(string repositoryPath) =>
        InputScript.Parse(File.ReadAllText(Path.Combine(RepositoryRoot(), repositoryPath)));

    /// <summary>
    /// Runs <paramref name="game"/> headless with <paramref name="script"/> (deterministic time, muted, captures to a
    /// scratch folder that is deleted afterwards) and returns its trace with section hashes. Every frame is also checked:
    /// the state hash after drawing must equal the one before (<see cref="DrawCheckPlatform"/>).
    /// </summary>
    public static TracedRun Run(Game game, InputScript script, int seed = 1)
    {
        string captures = Path.Combine(Path.GetTempPath(), "microengine-state-trace", Guid.NewGuid().ToString("N"));
        var platform = new DrawCheckPlatform(HeadlessPlatform.For(game.Settings), game);
        var trace = new StateTrace { Sections = true };
        try
        {
            var result = GameRunner.Run(game, platform, new RunOptions
            {
                Script = script,
                Seed = seed,
                Mute = true,
                CaptureDirectory = captures,
                StateTrace = trace,
            });
            if (result.Error is not null) throw new AssertFailedException($"The run failed: {result.Error}", result.Error);
            return new TracedRun(trace, result, platform.DrawChanges);
        }
        finally
        {
            platform.Dispose();
            if (Directory.Exists(captures)) Directory.Delete(captures, recursive: true);
        }
    }

    /// <summary>Asserts that two traces are identical, naming the first diverging update and section otherwise.</summary>
    public static void AssertIdentical(StateTrace first, StateTrace second, string what)
    {
        Assert.IsTrue(first.Steps.Count > 0, $"{what}: nothing was recorded.");
        var divergence = StateTrace.FirstDivergence(first, second);
        Assert.IsNull(divergence, $"{what}: {divergence}");
    }

    /// <summary>Asserts that no frame changed the state hash while drawing.</summary>
    public static void AssertDrawingChangesNothing(TracedRun run, string what) =>
        Assert.AreEqual(0, run.DrawChanges.Count, $"{what}: drawing changed the state hash: {string.Join("; ", run.DrawChanges.Take(5))}");
}

/// <summary>A traced run: its trace, its result and the frames whose drawing changed the state hash.</summary>
internal sealed record TracedRun(StateTrace Trace, RunResult Result, IReadOnlyList<StateDivergence> DrawChanges);

/// <summary>
/// A headless platform that hashes the game's state (with sections) when a frame starts drawing and again when it is
/// presented, and records every frame where the two differ: state written while drawing would make traces depend on
/// how often frames are drawn.
/// </summary>
internal sealed class DrawCheckPlatform(HeadlessPlatform inner, Game game) : IPlatform
{
    private StepHash? _before;

    public List<StateDivergence> DrawChanges { get; } = [];
    public HeadlessPlatform Inner => inner;

    public IRenderer BeginFrame()
    {
        // Only once the game runs (a start screen would draw before it exists).
        _before = game.UpdateCount > 0 ? game.ComputeStateHash(sections: true) : null;
        return inner.BeginFrame();
    }

    public void EndFrame()
    {
        inner.EndFrame();
        if (_before is not { } before) return;
        var after = game.ComputeStateHash(sections: true);
        if (StateTrace.FirstDivergence([before], [after]) is { } change) DrawChanges.Add(change);
    }

    public IGraphicsDevice Graphics => inner.Graphics;
    public MicroEngine.Audio.IAudioDevice Audio => inner.Audio;
    public MicroEngine.Input.IInputSource Input => inner.Input;
    public IMonotonicClock Clock => inner.Clock;
    public bool PumpEvents(IEngineEventSink sink) => inner.PumpEvents(sink);
    public ISurface? MainSurface => inner.MainSurface;
    public bool CanBlock => inner.CanBlock;
    public bool TryGetCapability<T>([NotNullWhen(true)] out T? capability) where T : class => inner.TryGetCapability(out capability);
    public Color Letterbox { get => inner.Letterbox; set => inner.Letterbox = value; }
    public void SaveScreenshot(string path) => inner.SaveScreenshot(path);
    public void ToggleFullscreen() => inner.ToggleFullscreen();
    public void Dispose() => inner.Dispose();
}

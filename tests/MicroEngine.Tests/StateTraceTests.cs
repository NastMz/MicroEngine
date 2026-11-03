using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Scenes;
using MicroEngine.Testing;

namespace MicroEngine.Tests;

/// <summary>
/// The per-step simulation state hash of a run (<see cref="RunOptions.StateTrace"/>): what the engine contributes, what a
/// game adds, that drawing never changes it, and that two runs that differ are told apart at the right step and section.
/// </summary>
[TestClass]
public sealed class StateTraceTests
{
    /// <summary>A small game with gameplay state of its own: a walker driven by input and the game's randomness.</summary>
    private sealed class Walker(Func<Scene>? first = null) : Game
    {
        public override GameSettings Settings { get; } = new("walker", 64, 48);
        public int Score { get; set; }
        protected override void Load() => Scenes.Push(first?.Invoke() ?? new Field());
        public override void HashState(StateHasher hash) => hash.Add(Score);
    }

    private class Field : Scene
    {
        public Vector2 Position;
        /// <summary>Written while drawing: presentation only, so it stays out of the hash.</summary>
        public int DrawnFrames;

        public override void Update(float dt)
        {
            Position.X += Input.Axis("left", "right");
            if (Input.Pressed("confirm")) Position.Y += Game.Random.Next(1, 5);
        }

        public override void Draw(IRenderer renderer) => DrawnFrames++;
        public override void HashState(StateHasher hash) => hash.Add(Position);
    }

    /// <summary>A scene that wrongly hashes what drawing writes.</summary>
    private sealed class LeakyField : Field
    {
        public override void HashState(StateHasher hash)
        {
            base.HashState(hash);
            hash.Add(DrawnFrames);
        }
    }

    /// <summary>A scene that takes a value from an unseeded generator into its state at update 40.</summary>
    private sealed class Noisy : Field
    {
        private long _noise;
        public override void Update(float dt)
        {
            base.Update(dt);
            if (Game.UpdateCount == 40) _noise = System.Random.Shared.NextInt64();
        }
        public override void HashState(StateHasher hash)
        {
            base.HashState(hash);
            hash.Add(_noise);
        }
    }

    private static InputScript Walk() => InputScript.Parse("5 hold right 20\n12 press confirm\n30 press confirm\n60 quit");

    [TestMethod]
    public void ARunWithATraceRecordsOneHashPerFixedStep()
    {
        var trace = new StateTrace();
        var result = GameRunner.Run(new Walker(), HeadlessPlatform.For(new GameSettings("walker", 64, 48)), new RunOptions { MaxUpdates = 30, StateTrace = trace });
        Assert.IsNull(result.Error);
        Assert.AreEqual(30, trace.Steps.Count);
        CollectionAssert.AreEqual(Enumerable.Range(1, 30).Select(i => (long)i).ToArray(), trace.Steps.Select(s => s.Update).ToArray());
        Assert.IsTrue(trace.Steps.All(s => s.Sections.Count == 0), "section hashes only when the trace asks for them");
        Assert.AreEqual(trace.Steps.Count, trace.Steps.Select(s => s.Hash).Distinct().Count(), "time advances, so every step differs");
    }

    [TestMethod]
    public void TwoRunsOfTheSameScriptRecordTheSameTrace()
    {
        var first = StateTraceRuns.Run(new Walker(), Walk());
        var second = StateTraceRuns.Run(new Walker(), Walk());
        Assert.AreEqual(60, first.Trace.Steps.Count);
        StateTraceRuns.AssertIdentical(first.Trace, second.Trace, "walker");
    }

    [TestMethod]
    public void TheEngineHashesTimeRandomnessInputAudioAssetsAndScenesThenTheGame()
    {
        var run = StateTraceRuns.Run(new Walker(), Walk());
        CollectionAssert.AreEqual(
            new[] { "engine.time", "engine.random", "engine.input", "engine.audio", "engine.assets", "engine.scenes", "scene[0]:Field", "game" },
            run.Trace.Steps[0].Sections.Select(s => s.Name).ToArray());
    }

    [TestMethod]
    public void AnUnseededRandomIsCaughtAtTheStepAndSectionItEntersTheState()
    {
        var first = StateTraceRuns.Run(new Walker(() => new Noisy()), Walk());
        var second = StateTraceRuns.Run(new Walker(() => new Noisy()), Walk());
        var divergence = StateTrace.FirstDivergence(first.Trace, second.Trace);
        Assert.IsNotNull(divergence, "the check must fail when the simulation is not deterministic");
        Assert.AreEqual(40L, divergence.Update);
        Assert.AreEqual("scene[0]:Noisy", divergence.Section);
    }

    [TestMethod]
    public void ADifferentSeedOrInputIsCaughtInTheEngineSectionThatHoldsIt()
    {
        var reference = StateTraceRuns.Run(new Walker(), Walk());

        // The seed only matters once the game draws: at update 12 (a confirm press) in both the generator and the walker.
        var seeded = StateTrace.FirstDivergence(reference.Trace, StateTraceRuns.Run(new Walker(), Walk(), seed: 2).Trace);
        Assert.AreEqual(1L, seeded!.Update, "the generator's seed is part of its state from the start");
        Assert.AreEqual("engine.random", seeded.Section);

        var pressed = StateTrace.FirstDivergence(reference.Trace,
            StateTraceRuns.Run(new Walker(), InputScript.Parse("5 hold right 20\n12 press confirm\n30 press confirm\n50 press cancel\n60 quit")).Trace);
        // A script command at update N is applied before the step that ends update N + 1.
        Assert.AreEqual(51L, pressed!.Update, pressed.ToString());
        Assert.AreEqual("engine.input", pressed.Section);
    }

    [TestMethod]
    public void TheGameRandomIsSeededAndDrawsWhatRandomWithTheSeedDraws()
    {
        var game = new Walker();
        game.InitializeForTest(HeadlessPlatform.For(game.Settings), seed: 9);
        var expected = new Random(9);
        Assert.IsInstanceOfType<SeededRandom>(game.Random);
        for (int i = 0; i < 20; i++) Assert.AreEqual(expected.Next(100), game.Random.Next(100));
        Assert.AreSame(game.Random, game.Services.Get<Random>());
    }

    [TestMethod]
    public void DrawingNeverChangesTheHash()
    {
        var run = StateTraceRuns.Run(new Walker(), Walk());
        Assert.AreEqual(60L, run.Result.Frames, "one frame drawn per step");
        StateTraceRuns.AssertDrawingChangesNothing(run, "walker");
    }

    [TestMethod]
    public void TheDrawingCheckCatchesStateWrittenWhileDrawing()
    {
        var run = StateTraceRuns.Run(new Walker(() => new LeakyField()), Walk());
        Assert.AreEqual(60, run.DrawChanges.Count, "every frame's drawing changes the leaky scene's hash");
        Assert.AreEqual("scene[0]:LeakyField", run.DrawChanges[0].Section);
    }

    private sealed class Animated : Field
    {
        public override void Enter()
        {
            Tweens.Add(1f, _ => { });
            Coroutines.Start(Pause());
        }

        private static System.Collections.IEnumerator Pause()
        {
            yield return Wait.Seconds(0.5f);
        }
    }

    [TestMethod]
    public void ScenesContributeTheirAgeCoroutinesTweensAndTransitions()
    {
        var game = new Walker(() => new Animated());
        game.InitializeForTest(HeadlessPlatform.For(game.Settings));
        game.StepForTest();
        var before = game.ComputeStateHash(sections: true);
        game.StepForTest();
        var after = game.ComputeStateHash(sections: true);
        Assert.AreEqual(2L, after.Update);
        Assert.AreNotEqual(before.Sections.Single(s => s.Name == "scene[0]:Animated").Hash, after.Sections.Single(s => s.Name == "scene[0]:Animated").Hash);

        var plain = new Walker();
        plain.InitializeForTest(HeadlessPlatform.For(plain.Settings));
        plain.StepForTest();
        var still = plain.ComputeStateHash(sections: true);
        plain.Scenes.Replace(new Field(), Transition.Fade(1f));
        var moving = plain.ComputeStateHash(sections: true);
        Assert.AreNotEqual(still.Sections.Single(s => s.Name == "engine.scenes").Hash, moving.Sections.Single(s => s.Name == "engine.scenes").Hash,
            "a requested transition is scene state");
        Assert.AreEqual(0, moving.Sections.Count(s => s.Name.StartsWith("scene[1]", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void TheHashOfTheSameStepIsTheSameWhetherOrNotSectionsAreAsked()
    {
        var game = new Walker();
        game.InitializeForTest(HeadlessPlatform.For(game.Settings));
        game.StepForTest();
        var whole = game.ComputeStateHash();
        var detailed = game.ComputeStateHash(sections: true);
        Assert.AreEqual(whole.Hash, detailed.Hash);
        Assert.AreEqual(0, whole.Sections.Count);
        Assert.IsTrue(detailed.Sections.Count > 0);
    }

    /// <summary>A game that changes room music and lets its rules read the music playing, as Courtyard Key does.</summary>
    private sealed class Jukebox : Game
    {
        public override GameSettings Settings { get; } = new("jukebox", 64, 48);
        public string? Heard { get; set; }
        protected override void Load()
        {
            var tune = new MicroEngine.Audio.Song(140).Track(MicroEngine.Audio.Instrument.Lead, "C4 E4 G4 C5 . G4 E4 - D4 F4 A4 D5 . A4 F4 -")
                .Track(MicroEngine.Audio.Instrument.Bass, "C2 - . C2 G2 - . G2").Drums("k . s . k k s h");
            Audio.AddMusic("first", tune);
            Audio.AddMusic("second", tune);
            Scenes.Push(new Rooms());
        }
        public override void HashState(StateHasher hash) => hash.Add(Heard);

        private sealed class Rooms : Scene
        {
            public override void Update(float dt)
            {
                if (Game.UpdateCount == 5) Audio.PlayMusic("second", fadeSeconds: 0);
                if (Game.UpdateCount == 20) Audio.PlayMusic("first");
                ((Jukebox)Game).Heard = Audio.CurrentMusic;
            }
            public override void Draw(IRenderer renderer) { }
        }
    }

    [TestMethod]
    [DoNotParallelize]
    public void Music_state_does_not_depend_on_how_fast_songs_render_where_there_are_no_threads()
    {
        bool threads = MicroEngine.Audio.AudioMixer.RenderSongsOnThreads;
        var budget = MicroEngine.Audio.AudioMixer.SliceBudget;
        MicroEngine.Audio.AudioMixer.RenderSongsOnThreads = false; // how browsers render songs
        try
        {
            var script = InputScript.Parse("60 quit");
            MicroEngine.Audio.AudioMixer.SliceBudget = TimeSpan.FromTicks(1);
            var slow = StateTraceRuns.Run(new Jukebox(), script);
            MicroEngine.Audio.AudioMixer.SliceBudget = TimeSpan.FromSeconds(1);
            var fast = StateTraceRuns.Run(new Jukebox(), script);
            StateTraceRuns.AssertIdentical(slow.Trace, fast.Trace, "a slow and a fast machine");
        }
        finally
        {
            MicroEngine.Audio.AudioMixer.RenderSongsOnThreads = threads;
            MicroEngine.Audio.AudioMixer.SliceBudget = budget;
        }
    }
}

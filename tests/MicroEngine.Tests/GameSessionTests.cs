using MicroEngine.Audio;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Scenes;

namespace MicroEngine.Tests;

[TestClass]
public sealed class GameSessionTests
{
    /// <summary>Quits by itself after a number of updates, like a player choosing Quit on the title.</summary>
    private sealed class QuittingGame(int updates) : Game
    {
        public override GameSettings Settings { get; } = new("quits");
        protected override void Load() => Scenes.Push(new QuitScene(this, updates));
    }

    private sealed class QuitScene(Game game, int updates) : Scene
    {
        private int _left = updates;
        public override void Update(float dt) { if (--_left == 0) game.Quit(); }
        public override void Draw(IRenderer renderer) { }
    }

    private sealed class FailingLoadGame : Game
    {
        public override GameSettings Settings { get; } = new("fails");
        protected override void Load() => throw new InvalidOperationException("missing asset");
    }

    /// <summary>A headless platform that records whether the session released it.</summary>
    private sealed class TrackedPlatform : IPlatform
    {
        private readonly HeadlessPlatform _inner = new();
        public bool Disposed { get; private set; }
        public IGraphicsDevice Graphics => _inner.Graphics;
        public IAudioDevice Audio => _inner.Audio;
        public IInputSource Input => _inner.Input;
        public bool CloseRequested => _inner.CloseRequested;
        public double Now => _inner.Now;
        public bool RealTime => _inner.RealTime;
        public void PollEvents() => _inner.PollEvents();
        public IRenderer BeginFrame() => _inner.BeginFrame();
        public void EndFrame() => _inner.EndFrame();
        public void SaveScreenshot(string path) => _inner.SaveScreenshot(path);
        public void ToggleFullscreen() { }
        public void Dispose() { Disposed = true; _inner.Dispose(); }
    }

    private readonly List<TrackedPlatform> _platforms = [];

    private GameSession Session(Func<Game> create) => new(create, _ =>
    {
        var platform = new TrackedPlatform();
        _platforms.Add(platform);
        return platform;
    });

    [TestMethod]
    public void A_run_ends_when_the_game_quits_and_releases_its_platform()
    {
        var session = Session(() => new QuittingGame(30));
        Assert.IsTrue(session.Start());
        Assert.IsTrue(session.IsRunning);
        int frames = 1;
        while (session.Tick()) frames++;
        Assert.IsFalse(session.IsRunning);
        Assert.IsNotNull(session.Result);
        Assert.IsNull(session.Result.Error);
        Assert.AreEqual(30, session.Result.Updates);
        Assert.AreEqual(30, frames, "the frame that reaches the quit reports the end");
        Assert.AreEqual(30, session.Frames);
        Assert.IsTrue(_platforms[0].Disposed, "the window and audio are released when the run ends");
        Assert.IsFalse(session.Tick(), "an ended session stays ended until started again");
    }

    [TestMethod]
    public void Starting_again_builds_a_new_game_on_a_new_platform()
    {
        var games = new List<Game>();
        var session = Session(() => { var g = new QuittingGame(5); games.Add(g); return g; });
        for (int run = 0; run < 2; run++)
        {
            Assert.IsTrue(session.Start(), $"run {run}");
            while (session.Tick()) { }
            Assert.AreEqual(5, session.Result!.Updates, $"run {run} starts from the beginning");
        }
        Assert.AreEqual(2, games.Count);
        Assert.AreNotSame(games[0], games[1]);
        Assert.AreEqual(2, _platforms.Count);
        Assert.IsTrue(_platforms.TrueForAll(p => p.Disposed));
    }

    [TestMethod]
    public void A_game_that_cannot_be_built_reports_the_error_without_a_platform()
    {
        var session = Session(() => throw new FileNotFoundException("Content/title.png"));
        Assert.IsFalse(session.Start());
        Assert.IsFalse(session.IsRunning);
        Assert.IsInstanceOfType<FileNotFoundException>(session.Result!.Error);
        Assert.AreEqual(0, _platforms.Count);
        Assert.IsFalse(session.Tick());
    }

    [TestMethod]
    public void A_game_that_fails_to_load_reports_the_error_and_releases_the_platform()
    {
        var session = Session(() => new FailingLoadGame());
        Assert.IsFalse(session.Start());
        StringAssert.Contains(session.Result!.Error!.Message, "missing asset");
        Assert.IsTrue(_platforms[0].Disposed);
    }

    [TestMethod]
    public void Stop_ends_a_running_game_and_a_second_start_while_running_is_refused()
    {
        var session = Session(() => new QuittingGame(1000));
        Assert.IsTrue(session.Start());
        session.Tick();
        Assert.ThrowsExactly<InvalidOperationException>(() => session.Start());
        session.Stop();
        Assert.IsFalse(session.IsRunning);
        Assert.AreEqual(1, session.Result!.Updates);
        Assert.IsTrue(_platforms[0].Disposed);
        session.Stop();
        Assert.AreEqual(1, _platforms.Count, "stopping twice is harmless");
    }

    [TestMethod]
    public void Run_options_reach_the_loop()
    {
        var session = Session(() => new QuittingGame(1000));
        Assert.IsTrue(session.Start(new RunOptions { MaxUpdates = 12 }));
        while (session.Tick()) { }
        Assert.AreEqual(12, session.Result!.Updates);
    }
}

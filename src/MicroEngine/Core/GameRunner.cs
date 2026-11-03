using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using MicroEngine.Platform;

namespace MicroEngine.Core;

/// <summary>Options shared by every launcher: scripted input, captures and run limits.</summary>
public sealed record RunOptions
{
    /// <summary>Scripted input to play, or null for none.</summary>
    public InputScript? Script { get; init; }
    /// <summary>Directory where script "capture" commands write PNGs.</summary>
    public string CaptureDirectory { get; init; } = "captures";
    /// <summary>Stops after this many updates (0 = unlimited).</summary>
    public long MaxUpdates { get; init; }
    /// <summary>Seed for <see cref="Game.Random"/> (1 by default).</summary>
    public int Seed { get; init; } = 1;
    /// <summary>Sets the sound effect and music volumes to 0 after <c>Load</c>.</summary>
    public bool Mute { get; init; }
    /// <summary>
    /// Whether the game's start screen (<see cref="Core.StartScreen"/>) is drawn while it loads, for runs whose time mode
    /// (<see cref="TimeMode"/>) is not deterministic and games whose identity keeps it on (true by default).
    /// </summary>
    public bool StartScreen { get; init; } = true;
    /// <summary>
    /// Called once, right after the first frame is presented (the start screen, or the game's first frame), from the
    /// loop's thread. Phone hosts use it to keep the system's launch screen up until the game has something to show.
    /// </summary>
    public Action? FirstFrame { get; init; }
    /// <summary>
    /// Whether real-time runs count elapsed time in refresh periods of the display that paces them
    /// (<see cref="IDisplayPacing"/>), which removes the stutter of an irregular wake-up (true by default). False
    /// takes every frame's time as measured (the launcher's <c>--pacing measured</c>), for comparisons.
    /// </summary>
    public bool PaceToDisplay { get; init; } = true;
    /// <summary>Measures every frame and the phases of the start (the launcher's <c>--profile</c>), or null to measure nothing.</summary>
    public FrameProfiler? Profiler { get; init; }
    /// <summary>
    /// The graphics options the run started with, after the launcher's overrides (<see cref="Game.Screen"/> starts from
    /// them); null uses <see cref="GameSettings.Graphics"/>.
    /// </summary>
    public GraphicsSettings? Graphics { get; init; }
    /// <summary>
    /// How the loop advances time (the launcher's <c>--time</c>): <see cref="Kernel.TimeMode.RealTime"/> with the wall
    /// clock, or <see cref="Kernel.TimeMode.Deterministic"/>, one fixed step per frame. It wins over the game's
    /// <see cref="Game.Configure"/>; null (the default) keeps the game's choice, else the composition's
    /// <see cref="RunPreset"/> (the headless platform's: deterministic), else real time. Nothing a platform does depends on
    /// the mode: it belongs to the loop only.
    /// </summary>
    public Kernel.TimeMode? TimeMode { get; init; }
    /// <summary>
    /// Receives the simulation state hash after every fixed step (<see cref="Game.ComputeStateHash"/>), with each
    /// section's hash when <see cref="Kernel.StateTrace.Sections"/> is true; null (the default) hashes nothing. Two
    /// deterministic runs of the same build, script and seed record the same trace;
    /// <see cref="Kernel.StateTrace.FirstDivergence(Kernel.StateTrace, Kernel.StateTrace)"/> names the first update and
    /// section where two differ.
    /// </summary>
    public Kernel.StateTrace? StateTrace { get; init; }
}

/// <summary>
/// Defaults a composition states for the runs it hosts when neither the run (<see cref="RunOptions"/>) nor the game
/// (<see cref="Game.Configure"/>) chooses: a stated preset value, never a platform behaviour. A platform offers it as a
/// capability (<see cref="Platform.IPlatformHost.TryGetCapability{T}"/>); the headless platform's preset is
/// <see cref="Kernel.TimeMode.Deterministic"/>, so tests that run a game headless are reproducible with no option. The
/// platform behaves the same whatever the mode the run ends up with.
/// </summary>
public sealed record RunPreset
{
    /// <summary>The preset for testing compositions: one fixed step per frame.</summary>
    public static RunPreset Testing { get; } = new() { TimeMode = Kernel.TimeMode.Deterministic };
    /// <summary>The time mode of runs that choose none; null leaves the default, real time.</summary>
    public Kernel.TimeMode? TimeMode { get; init; }
}

/// <summary>How a run ended.</summary>
/// <param name="Updates">Fixed updates simulated.</param>
/// <param name="Frames">Frames rendered.</param>
/// <param name="Captures">Paths of the screenshots written by the script, in order.</param>
/// <param name="Error">The exception that stopped the run, or null when it ended normally.</param>
public sealed record RunResult(long Updates, long Frames, IReadOnlyList<string> Captures, Exception? Error);

/// <summary>
/// Runs a game to completion on platforms that own their thread (desktop, headless):
/// <see cref="GameLoop"/> ticked in a blocking loop.
/// </summary>
public static class GameRunner
{
    /// <summary>Starts <paramref name="game"/> on <paramref name="platform"/>, runs frames until it stops, then shuts it down. Never throws for errors raised by the game: they are in <see cref="RunResult.Error"/>.</summary>
    /// <remarks>The platform is not disposed; its owner does that.</remarks>
    public static RunResult Run(Game game, IPlatform platform, RunOptions? options = null)
    {
        var loop = new GameLoop(game, platform, options);
        if (loop.Start())
            while (loop.Tick()) { }
        return loop.Finish();
    }
}

/// <summary>
/// The loop, one frame per <see cref="Tick"/>. It owns time: the platform only supplies a monotonic clock
/// (<see cref="IPlatformHost.Clock"/>), and the engine's <see cref="Kernel.FrameClock"/> turns each frame's time into fixed
/// steps following the game's <see cref="Kernel.TimeSettings"/> (at most <c>MaxStepsPerFrame</c> a frame, frames of at
/// most <c>MaxFrameDelta</c>). Each frame runs the engine's frame schedule: the per-frame phases, the four fixed phases
/// once per step, then the update, drawing and presentation phases. In deterministic time every frame is exactly one
/// step, which makes scripted runs reproducible. Each frame starts with the platform's event pump
/// (<see cref="IPlatformHost.PumpEvents"/>), whose events become the engine's lifecycle events
/// (<see cref="Kernel.IEngineLifecycle"/>) before the frame's steps. Hosts that cannot block, such as a browser driving
/// frames from <c>requestAnimationFrame</c>, call <see cref="Tick"/> themselves; errors are captured, never thrown.
/// </summary>
public sealed class GameLoop
{
    private readonly Game _game;
    private readonly IPlatform _platform;
    private readonly RunOptions _options;
    private readonly List<string> _captures = [];
    private Kernel.Engine? _engine;
    private bool _realTime;
    private double _last;
    private long _frames;
    private bool _running;
    private bool _suspended;
    private bool _closeAccepted;
    private readonly List<Action<GameLoop>> _early = []; // events pumped before the game existed (the start screen)
    private readonly Sink _sink;
    private IDisplayPacing? _pacing;
    private IPreciseDelay? _delay;
    private Exception? _error;
    private RunResult? _result;

    /// <summary>Prepares a loop; nothing runs until <see cref="Start"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="game"/> or <paramref name="platform"/> is null.</exception>
    public GameLoop(Game game, IPlatform platform, RunOptions? options = null) : this(game, platform, options, null) { }

    /// <summary>
    /// Prepares a loop whose engine also gets the host's composition: <paramref name="compose"/> runs after the game's
    /// <see cref="Game.Configure"/> and before the run's own systems (<see cref="GameHost"/> passes the content and engine
    /// changes of its <see cref="Hosting.HostBuilder"/> this way).
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="game"/> or <paramref name="platform"/> is null.</exception>
    public GameLoop(Game game, IPlatform platform, RunOptions? options, Action<Kernel.EngineBuilder>? compose)
    {
        _game = game ?? throw new ArgumentNullException(nameof(game));
        _platform = platform ?? throw new ArgumentNullException(nameof(platform));
        _options = options ?? new RunOptions();
        _compose = compose;
        _sink = new Sink(this);
    }

    private readonly Action<Kernel.EngineBuilder>? _compose;

    /// <summary>The loop's side of the event pump: host events become engine lifecycle events.</summary>
    private sealed class Sink(GameLoop loop) : IEngineEventSink
    {
        private void Deliver(Action<GameLoop> deliver)
        {
            if (loop._engine is null) loop._early.Add(deliver);
            else deliver(loop);
        }

        public void Resized(Kernel.SurfaceResized size) => Deliver(l => l._engine!.Lifecycle.RaiseResized(size));
        public void FocusChanged(bool focused) => Deliver(l => l._engine!.Lifecycle.RaiseFocusChanged(focused));
        public void Suspending() => Deliver(l => l.Suspend());
        public void Resumed() => Deliver(l => l.Resume());
        public void QuitRequested() => Deliver(l => l._closeAccepted |= l._engine!.Lifecycle.RaiseQuitRequested());
    }

    /// <summary>Whether the next <see cref="Tick"/> will run a frame.</summary>
    public bool IsRunning => _running;
    /// <summary>Frames rendered so far.</summary>
    public long Frames => _frames;

    /// <summary>Initializes the game; false when loading failed (the error is in <see cref="Finish"/>).</summary>
    public bool Start()
    {
        if (_result is not null || _running) throw new InvalidOperationException("The loop was already started.");
        try
        {
            var profiler = _options.Profiler;
            // Optional features the loop uses, found once (a host's capabilities do not change while it runs).
            _platform.TryGetCapability(out _pacing);
            _platform.TryGetCapability(out _delay);
            // Before the game exists only the run's choice and the composition's preset are known; the start screen shows
            // only in real time.
            _platform.TryGetCapability<RunPreset>(out var preset);
            _realTime = (_options.TimeMode ?? preset?.TimeMode ?? Kernel.TimeMode.RealTime) == Kernel.TimeMode.RealTime;
            ShowStartScreen();
            profiler?.Phase("start screen");
            _game.Initialize(_platform, _options.Seed, _options.Graphics ?? _game.Settings.Graphics, _options.TimeMode, AddRunSystems);
            _engine = _game.Engine;
            _realTime = _engine.Clock.Mode == Kernel.TimeMode.RealTime;
            profiler?.Phase("load");
            if (profiler is not null) profiler.DisplayRate = _pacing?.DisplayRate ?? 0;
            _platform.Letterbox = _game.Settings.Letterbox;
            if (_options.Mute) _game.Audio.SfxVolume = 0;
            if (_options.Mute) _game.Audio.MusicVolume = 0;
            _last = _platform.Clock.Now;
            _running = true;
            // What happened while the game loaded reaches it now, in order.
            foreach (var deliver in _early) deliver(this);
            _early.Clear();
        }
        catch (Exception e)
        {
            _error = e;
        }
        return _running;
    }

    /// <summary>The host's composition, then the run's own systems: a script's touches and forced actions, just before the engine reads them.</summary>
    private void AddRunSystems(Kernel.EngineBuilder builder)
    {
        _compose?.Invoke(builder);
        if (_options.Script is not { } script) return;
        builder.Schedule.Add(Kernel.FramePhase.Input, "run.script.touches", _ => script.ApplyTouches(_game.UpdateCount, _game.Touch), before: "engine.touch");
        builder.Schedule.Add(Kernel.FramePhase.PreFixed, "run.script.input", _ =>
        {
            script.Apply(_game.UpdateCount, _game.Input);
            script.ApplyPrompts(_game.UpdateCount, _game.Prompts);
        }, before: "engine.input");
    }

    private bool _presented;

    private void Presented()
    {
        if (_presented) return;
        _presented = true;
        _options.FirstFrame?.Invoke();
    }

    /// <summary>Presents the start screen once, so the window shows the game's colour and image while it loads.</summary>
    private void ShowStartScreen()
    {
        if (!_options.StartScreen || !_realTime || AppIdentity.Of(_game) is not { Splash: true } identity) return;
        try
        {
            _platform.PumpEvents(_sink);
            _platform.Letterbox = identity.SplashColor;
            StartScreen.Draw(_platform.BeginFrame(), _platform.Graphics, identity);
            _platform.EndFrame();
            Presented();
        }
        catch (Exception e) when (e is InvalidOperationException or NotSupportedException)
        {
            // A platform that cannot present before the game exists keeps its first frame black; loading goes on.
        }
    }

    /// <summary>Runs one frame. Returns false once the game quit, the platform closed, a limit was hit or an error occurred.</summary>
    public bool Tick()
    {
        if (!_running) return false;
        try
        {
            if (_closeAccepted || _game.QuitRequested) return _running = false;
            _running = !Frame();
        }
        catch (Exception e)
        {
            _error = e;
            _running = false;
        }
        return _running;
    }

    /// <returns>True when the run must stop after this frame.</returns>
    private bool Frame()
    {
        if (!Limit()) return false;
        if (_options.Profiler is { } profiler) return ProfiledFrame(profiler);
        if (!Pump()) return _closeAccepted || _game.QuitRequested;
        var (stop, capture) = Update();
        Render();
        return EndOfFrame(stop, capture);
    }

    private long _frameStart;

    /// <summary><see cref="Frame"/> with each phase timed into <paramref name="profiler"/>.</summary>
    private bool ProfiledFrame(FrameProfiler profiler)
    {
        long start = Stopwatch.GetTimestamp();
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        int collections = GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2);
        double interval = _frameStart == 0 ? 0 : Stopwatch.GetElapsedTime(_frameStart, start).TotalMilliseconds;
        _frameStart = start;
        bool run = Pump();
        long polled = Stopwatch.GetTimestamp();
        if (!run) return _closeAccepted || _game.QuitRequested;
        long updates = _game.UpdateCount;
        var (stop, capture) = Update();
        var engine = _engine!;
        var frame = engine.Clock.Context;
        engine.Run(Kernel.FramePhase.Update, frame);
        engine.Run(Kernel.FramePhase.LateUpdate, frame);
        long updated = Stopwatch.GetTimestamp();
        engine.Run(Kernel.FramePhase.Extract, frame);
        engine.Run(Kernel.FramePhase.Render, frame);
        long rendered = Stopwatch.GetTimestamp();
        engine.Run(Kernel.FramePhase.Present, frame);
        engine.Run(Kernel.FramePhase.EndFrame, frame);
        long presented = Stopwatch.GetTimestamp();
        profiler.Add(new FrameSample(interval,
            Stopwatch.GetElapsedTime(start, polled).TotalMilliseconds,
            Stopwatch.GetElapsedTime(polled, updated).TotalMilliseconds,
            Stopwatch.GetElapsedTime(updated, rendered).TotalMilliseconds,
            Stopwatch.GetElapsedTime(rendered, presented).TotalMilliseconds,
            (int)(_game.UpdateCount - updates),
            _game.Time,
            _game.Time - (1 - _game.FrameBlend) * engine.Clock.FixedDelta,
            GC.GetAllocatedBytesForCurrentThread() - allocated,
            GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2) - collections));
        if (_frames == 0) profiler.Phase("first frame");
        return EndOfFrame(stop, capture);
    }

    /// <summary>
    /// Runs the platform's event pump; its events reach the game as they are delivered. True when the frame runs: the
    /// host is not suspended and no accepted quit request ends the run.
    /// </summary>
    private bool Pump()
    {
        bool run = _platform.PumpEvents(_sink);
        return run && !_suspended && !_closeAccepted;
    }

    // The game's own handling (music, achievements, fingers, OnSuspended/OnResumed) is the first handler.
    private void Suspend()
    {
        if (_suspended) return;
        _suspended = true;
        _engine!.Lifecycle.RaiseSuspending();
    }

    private void Resume()
    {
        if (!_suspended) return;
        _suspended = false;
        // The time spent away is not simulated.
        _last = _platform.Clock.Now;
        _engine!.Clock.DiscardPending();
        _engine.Lifecycle.RaiseResumed();
    }

    /// <summary>Runs the fixed steps the elapsed time calls for; returns whether the run must stop and the capture due.</summary>
    private (bool Stop, string? Capture) Update()
    {
        var engine = _engine!;
        var clock = engine.Clock;
        double elapsed = 0;
        if (_realTime)
        {
            double now = _platform.Clock.Now;
            elapsed = Paced(clock.ClampElapsed(now - _last));
            _last = now;
        }
        clock.BeginFrame(elapsed);
        var frame = clock.Context;
        // Preloaded assets finish within their per-frame budget before the frame's updates; touches follow the frame.
        engine.Run(Kernel.FramePhase.BeginFrame, frame);
        engine.Run(Kernel.FramePhase.Input, frame);

        string? capture = null;
        bool stop = false;
        var script = _options.Script;
        while (clock.TryStep(out var step))
        {
            engine.RunFixedStep(step);
            _options.StateTrace?.Record(_game.ComputeStateHash(_options.StateTrace.Sections));
            if (script?.CaptureAt(_game.UpdateCount) is { } name) capture = name;
            if (script?.QuitAt(_game.UpdateCount) == true || _options.MaxUpdates > 0 && _game.UpdateCount >= _options.MaxUpdates)
            {
                stop = true;
                break;
            }
            if (capture is not null) break;
        }
        clock.EndSteps();
        // Deterministic time leaves nothing between updates: the frame is the update (Lerp at 1 draws it exactly).
        _game.FrameBlend = clock.Blend;
        return (stop, capture);
    }

    private double _drift, _nextFrame;
    private readonly double[] _recent = new double[16]; // the last measured frame times, to check the reported rate
    private double _cadence, _cadenceMedian; // the period frames arrive at when it is not the reported rate's; 0 otherwise
    private int _recentCount;

    /// <summary>
    /// Holds the frame back until the frame limit (<see cref="GameScreen.FrameRateLimit"/>) allows it, on an absolute
    /// schedule so the rate does not drift; false when the frame must be skipped (a platform that cannot block, called
    /// too early: <see cref="IPlatformHost.CanBlock"/> is false). Deterministic runs are never limited.
    /// </summary>
    private bool Limit()
    {
        int limit = _game.Screen.FrameRateLimit;
        if (limit <= 0 || !_realTime) { _nextFrame = 0; return true; }
        double period = 1.0 / limit, now = _platform.Clock.Now;
        // The first limited frame, one late by more than a period (a hitch), or a schedule further ahead than a period
        // (the limit was raised): start a new schedule. The margin keeps a frame that took no time on the schedule.
        if (_nextFrame == 0 || now - _nextFrame > period || _nextFrame - now > period * 1.01) _nextFrame = now;
        else if (now < _nextFrame)
        {
            if (_platform.CanBlock)
            {
                if (_delay is not null) _delay.Delay(_nextFrame - now);
                else FrameTimer.Delay(_nextFrame - now);
            }
            // Within a millisecond the frame is on time; earlier, a page-driven host draws nothing this time.
            else if (_nextFrame - now > 0.001) return false;
        }
        _nextFrame += period;
        return true;
    }

    /// <summary>
    /// The rate frames are paced to: the display's while VSync paces them, else the frame limit's (the loop waits for it
    /// itself, with the same small irregularity around each period); 0 when nothing paces them.
    /// </summary>
    private double PaceRate()
    {
        double display = _pacing?.DisplayRate ?? 0;
        if (display > 0) return display;
        int limit = _game.Screen.FrameRateLimit;
        return limit > 0 && _realTime ? limit : 0;
    }

    /// <summary>
    /// The time a frame stands for. When the platform paces frames to a display (<see cref="IDisplayPacing"/>),
    /// each frame stays on screen for a whole number of refresh periods, however irregularly the loop wakes up around
    /// them; counting elapsed time in those periods keeps that jitter from turning into frames with no step followed by
    /// frames with two, the stutter of a display whose rate matches the update rate. The remainder is carried to the next
    /// frame, so a clock that disagrees with the reported rate still adds up to real time. Frames much shorter than a
    /// period (VSync not honoured) are taken as measured.
    /// </summary>
    private double Paced(double elapsed)
    {
        double rate = _options.PaceToDisplay ? PaceRate() : 0;
        _recent[_recentCount++ % _recent.Length] = elapsed;
        if (rate <= 0) return elapsed;
        double period = 1 / rate, time = elapsed + _drift;
        // A rate the frames do not arrive at (a window composed at another display's rate, a moved window) would count
        // 10 ms frames as 16.7 ms then 3.3 ms: time stays real but motion judders. Trust it only while the median of the
        // recent frames is near its period; otherwise pace to the cadence they do arrive at (measured time would let the
        // wake-up jitter back in). That cadence starts at their median and follows the carried remainder, so a median a
        // little off the true period does not slip a frame now and then. The remainder is carried either way: time stays
        // real.
        bool cadence = false;
        if (_recentCount >= 5)
        {
            Span<double> sorted = stackalloc double[16];
            int n = Math.Min(_recentCount, _recent.Length);
            _recent.AsSpan(0, n).CopyTo(sorted);
            sorted[..n].Sort();
            double median = sorted[n / 2];
            if (median <= 0) { _drift = 0; return elapsed; }
            if (median < period * 0.85 || median > period * 1.15)
            {
                if (_cadence <= 0 || Math.Abs(_cadence - median) > median * 0.15) _cadence = median;
                _cadenceMedian = median;
                period = _cadence;
                cadence = true;
            }
            else _cadence = 0;
        }
        // A short frame keeps the debt of an earlier frame rounded up, or frames faster than the reported rate would
        // each count as a whole period and the game would run fast.
        if (time < period / 2) { _drift = 0; return Math.Max(0, time); }
        double shown = Math.Round(time / period) * period;
        _drift = Math.Clamp(time - shown, -period, period);
        // A remainder that keeps one sign means the cadence is a little off: move it a small step towards the frames.
        if (cadence) _cadence = Math.Clamp(_cadence + _drift * 0.005, _cadenceMedian * 0.9, _cadenceMedian * 1.1);
        return shown;
    }

    /// <summary>The once-per-frame phases after the steps: update at the display's rate, extract, draw, present, end.</summary>
    private void Render()
    {
        var engine = _engine!;
        var frame = engine.Clock.Context;
        engine.Run(Kernel.FramePhase.Update, frame);
        engine.Run(Kernel.FramePhase.LateUpdate, frame);
        engine.Run(Kernel.FramePhase.Extract, frame);
        engine.Run(Kernel.FramePhase.Render, frame);
        engine.Run(Kernel.FramePhase.Present, frame);
        engine.Run(Kernel.FramePhase.EndFrame, frame);
    }

    /// <summary>Counts the frame, writes its capture, and returns whether the run stops after it.</summary>
    private bool EndOfFrame(bool stop, string? capture)
    {
        _frames++;
        Presented();
        if (capture is not null)
        {
            Directory.CreateDirectory(_options.CaptureDirectory);
            var path = Path.Combine(_options.CaptureDirectory, capture);
            _platform.SaveScreenshot(path);
            _captures.Add(path);
        }
        return stop || _closeAccepted || _game.QuitRequested;
    }

    /// <summary>Shuts the game down (once) and reports the run.</summary>
    public RunResult Finish()
    {
        if (_result is not null) return _result;
        _running = false;
        if (_options.Profiler is { } profiler && _game.Assets is { } assets)
        {
            var stats = assets.Statistics;
            profiler.Environment["assets.loaded"] = stats.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
            profiler.Environment["assets.textureMB"] = (stats.TextureBytes / 1048576.0).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            profiler.Environment["assets.loadMs"] = stats.LoadMilliseconds.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
            profiler.Environment["assets.loadsDuringPlay"] = stats.LoadsDuringPlay.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        try { _game.Shutdown(); }
        catch (Exception e) { _error ??= e; }
        return _result = new RunResult(_game.UpdateCount, _frames, _captures, _error);
    }
}

/// <summary>
/// A deterministic input script, keyed by update number. Text format, one command per line:
/// <code>
/// # comment
/// 30 press confirm          # down for 2 updates
/// 60 hold right 90          # down for 90 updates
/// 60 down jump / 75 up jump # explicit edges
/// 200 capture title.png
/// 240 touch 1 dpad 1 0      # finger 1 down on the d-pad, one radius to the right (or moves there)
/// 250 tap 2 jump            # finger 2 taps the "jump" control (lifted 2 updates later)
/// 260 touch 3 0.5,0.5       # finger 3 at the middle of the screen (fractions of its size)
/// 300 lift 1
/// 320 prompts gamepad playstation  # name controls as on a PlayStation pad (keyboard, gamepad, touch or auto)
/// 900 quit
/// </code>
/// "@+N" instead of an absolute number means N updates after the previous command. Touch commands go through the
/// game's <see cref="Input.VirtualControls"/> with the same hit testing as real fingers.
/// </summary>
public sealed class InputScript
{
    private readonly List<(long Update, string Action, bool Down)> _edges = [];
    private readonly Dictionary<long, string> _captures = [];
    private readonly List<(long Update, int Finger, string? Target, Vector2 Offset, bool Lift)> _touches = [];
    private readonly List<(long Update, Input.InputDevice? Device, Input.GamepadStyle? Style)> _prompts = [];
    private long? _quit;

    /// <summary>Parses the text format described above.</summary>
    /// <exception cref="FormatException">A line is invalid; the message names its line number.</exception>
    public static InputScript Parse(string text)
    {
        var script = new InputScript();
        long cursor = 0;
        int lineNumber = 0;
        foreach (var raw in text.Split('\n'))
        {
            lineNumber++;
            var line = raw.Split('#')[0].Trim();
            if (line.Length == 0) continue;
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            try
            {
                long at = parts[0].StartsWith('@')
                    ? cursor + long.Parse(parts[0].AsSpan(2), CultureInfo.InvariantCulture)
                    : long.Parse(parts[0], CultureInfo.InvariantCulture);
                cursor = at;
                switch (parts[1])
                {
                    case "press": script.Hold(at, parts[2], 2); break;
                    case "hold": script.Hold(at, parts[2], long.Parse(parts[3], CultureInfo.InvariantCulture)); break;
                    case "down": script._edges.Add((at, parts[2], true)); break;
                    case "up": script._edges.Add((at, parts[2], false)); break;
                    case "capture": script._captures[at] = parts[2]; break;
                    case "quit": script._quit = at; break;
                    case "touch": script._touches.Add((at, ParseFinger(parts[2]), parts[3], Offset(parts), false)); break;
                    case "tap":
                        script._touches.Add((at, ParseFinger(parts[2]), parts[3], Offset(parts), false));
                        script._touches.Add((at + 2, ParseFinger(parts[2]), null, default, true));
                        break;
                    case "lift": script._touches.Add((at, ParseFinger(parts[2]), null, default, true)); break;
                    case "prompts":
                        Input.InputDevice? device = parts[2] == "auto" ? null : Enum.Parse<Input.InputDevice>(parts[2], ignoreCase: true);
                        Input.GamepadStyle? style = parts.Length > 3 ? Enum.Parse<Input.GamepadStyle>(parts[3], ignoreCase: true) : null;
                        script._prompts.Add((at, device, style));
                        break;
                    default: throw new FormatException($"Unknown command '{parts[1]}'.");
                }
            }
            catch (Exception e) when (e is FormatException or IndexOutOfRangeException or OverflowException or ArgumentException)
            {
                throw new FormatException($"Script line {lineNumber}: '{raw.Trim()}' is invalid. {e.Message}", e);
            }
        }
        return script;
    }

    private static int ParseFinger(string text) => int.Parse(text, CultureInfo.InvariantCulture);

    private static Vector2 Offset(string[] parts) => parts.Length >= 6
        ? new Vector2(float.Parse(parts[4], CultureInfo.InvariantCulture), float.Parse(parts[5], CultureInfo.InvariantCulture))
        : default;

    /// <summary>Puts finger <paramref name="finger"/> on a touch control (offset in its radii) at update <paramref name="at"/>, or moves it there.</summary>
    public InputScript Touch(long at, int finger, string control, Vector2 offset = default)
    {
        _touches.Add((at, finger, control, offset, false));
        return this;
    }

    /// <summary>Lifts finger <paramref name="finger"/> at update <paramref name="at"/>.</summary>
    public InputScript Lift(long at, int finger)
    {
        _touches.Add((at, finger, null, default, true));
        return this;
    }

    /// <summary>Hands the touches scheduled for the update about to run to the game's touch controls.</summary>
    public void ApplyTouches(long update, Input.VirtualControls controls)
    {
        foreach (var (at, finger, target, offset, lift) in _touches)
        {
            if (at != update) continue;
            if (lift) controls.ScriptLift(finger);
            else if (target!.Contains(','))
            {
                var xy = target.Split(',');
                controls.ScriptTouchAt(finger, new Vector2(float.Parse(xy[0], CultureInfo.InvariantCulture), float.Parse(xy[1], CultureInfo.InvariantCulture)));
            }
            else controls.ScriptTouch(finger, target, offset);
        }
    }

    /// <summary>Holds <paramref name="action"/> down from update <paramref name="at"/> for <paramref name="duration"/> updates (at least 1).</summary>
    public InputScript Hold(long at, string action, long duration)
    {
        _edges.Add((at, action, true));
        _edges.Add((at + Math.Max(1, duration), action, false));
        return this;
    }

    /// <summary>Presses <paramref name="action"/> at update <paramref name="at"/> for 2 updates.</summary>
    public InputScript Press(long at, string action) => Hold(at, action, 2);
    /// <summary>Saves a screenshot named <paramref name="fileName"/> after update <paramref name="at"/>.</summary>
    public InputScript Capture(long at, string fileName) { _captures[at] = fileName; return this; }
    /// <summary>Stops the run after update <paramref name="at"/>.</summary>
    public InputScript QuitAfter(long at) { _quit = at; return this; }

    /// <summary>Applies the <c>prompts</c> commands scheduled for the update about to run: which device <paramref name="prompts"/> names controls for.</summary>
    public void ApplyPrompts(long update, Input.ActionPrompts prompts)
    {
        foreach (var (at, device, style) in _prompts)
            if (at == update) (prompts.Override, prompts.PadStyleOverride) = (device, style);
    }

    /// <summary>Applies every edge scheduled for the update about to run.</summary>
    public void Apply(long update, Input.InputMap input)
    {
        foreach (var (at, action, down) in _edges)
            if (at == update) input.SetForced(action, down);
    }

    /// <summary>The screenshot file name scheduled after <paramref name="update"/>, or null.</summary>
    public string? CaptureAt(long update) => _captures.TryGetValue(update, out var name) ? name : null;
    /// <summary>Whether the run must stop after <paramref name="update"/>.</summary>
    public bool QuitAt(long update) => _quit is { } q && update >= q;
    /// <summary>The update after which the script stops the run, or null.</summary>
    public long? QuitUpdate => _quit;
}

/// <summary>Measures wall-clock time for desktop platforms.</summary>
public sealed class MonotonicClock
{
    private readonly Stopwatch _watch = Stopwatch.StartNew();
    /// <summary>Seconds since the clock was created.</summary>
    public double Seconds => _watch.Elapsed.TotalSeconds;
}

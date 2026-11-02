using System.Diagnostics;
using System.Globalization;
using System.Numerics;

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
    /// Whether the game's start screen (<see cref="Core.StartScreen"/>) is drawn while it loads, on platforms that run in
    /// real time and for games whose identity keeps it on (true by default). Headless runs never draw it.
    /// </summary>
    public bool StartScreen { get; init; } = true;
    /// <summary>
    /// Called once, right after the first frame is presented (the start screen, or the game's first frame), from the
    /// loop's thread. Phone hosts use it to keep the system's launch screen up until the game has something to show.
    /// </summary>
    public Action? FirstFrame { get; init; }
    /// <summary>
    /// Whether real-time runs count elapsed time in refresh periods of the display that paces them
    /// (<see cref="IPlatform.DisplayRate"/>), which removes the stutter of an irregular wake-up (true by default). False
    /// takes every frame's time as measured (the launcher's <c>--pacing measured</c>), for comparisons.
    /// </summary>
    public bool PaceToDisplay { get; init; } = true;
    /// <summary>Measures every frame and the phases of the start (the launcher's <c>--profile</c>), or null to measure nothing.</summary>
    public FrameProfiler? Profiler { get; init; }
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
/// The fixed-timestep loop, one frame per <see cref="Tick"/>: input is sampled once per step, the
/// simulation advances in exact steps (at most five per frame to avoid a spiral of death), and
/// rendering happens once per frame. Headless platforms advance exactly one step per frame, which
/// makes scripted runs fully deterministic. Hosts that cannot block, such as a browser driving frames
/// from <c>requestAnimationFrame</c>, call <see cref="Tick"/> themselves; errors are captured, never thrown.
/// </summary>
public sealed class GameLoop
{
    private readonly Game _game;
    private readonly IPlatform _platform;
    private readonly RunOptions _options;
    private readonly List<string> _captures = [];
    private double _step, _accumulator, _last, _lastTouch;
    private long _frames;
    private bool _running;
    private bool _suspended;
    private Exception? _error;
    private RunResult? _result;

    /// <summary>Prepares a loop; nothing runs until <see cref="Start"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="game"/> or <paramref name="platform"/> is null.</exception>
    public GameLoop(Game game, IPlatform platform, RunOptions? options = null)
    {
        _game = game ?? throw new ArgumentNullException(nameof(game));
        _platform = platform ?? throw new ArgumentNullException(nameof(platform));
        _options = options ?? new RunOptions();
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
            ShowStartScreen();
            profiler?.Phase("start screen");
            _game.Initialize(_platform, _options.Seed);
            profiler?.Phase("load");
            if (profiler is not null) profiler.DisplayRate = _platform.DisplayRate;
            _platform.Letterbox = _game.Settings.Letterbox;
            if (_options.Mute) _game.Audio.SfxVolume = 0;
            if (_options.Mute) _game.Audio.MusicVolume = 0;
            _step = 1.0 / _game.Settings.UpdateRate;
            _last = _platform.Now;
            _running = true;
        }
        catch (Exception e)
        {
            _error = e;
        }
        return _running;
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
        if (!_options.StartScreen || !_platform.RealTime || AppIdentity.Of(_game) is not { Splash: true } identity) return;
        try
        {
            _platform.PollEvents();
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
            if (_platform.CloseRequested || _game.QuitRequested) return _running = false;
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
        if (_options.Profiler is { } profiler) return ProfiledFrame(profiler);
        _platform.PollEvents();
        if (FollowSuspension()) return _platform.CloseRequested || _game.QuitRequested;
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
        _platform.PollEvents();
        long polled = Stopwatch.GetTimestamp();
        if (FollowSuspension()) return _platform.CloseRequested || _game.QuitRequested;
        long updates = _game.UpdateCount;
        var (stop, capture) = Update();
        long updated = Stopwatch.GetTimestamp();
        var renderer = _platform.BeginFrame();
        _game.Render(renderer);
        long rendered = Stopwatch.GetTimestamp();
        _platform.EndFrame();
        long presented = Stopwatch.GetTimestamp();
        profiler.Add(new FrameSample(interval,
            Stopwatch.GetElapsedTime(start, polled).TotalMilliseconds,
            Stopwatch.GetElapsedTime(polled, updated).TotalMilliseconds,
            Stopwatch.GetElapsedTime(updated, rendered).TotalMilliseconds,
            Stopwatch.GetElapsedTime(rendered, presented).TotalMilliseconds,
            (int)(_game.UpdateCount - updates),
            _game.Time,
            _game.Time - (1 - _game.FrameBlend) * _step,
            GC.GetAllocatedBytesForCurrentThread() - allocated,
            GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2) - collections));
        if (_frames == 0) profiler.Phase("first frame");
        return EndOfFrame(stop, capture);
    }

    /// <summary>Follows the platform into and out of suspension; true while suspended (nothing runs).</summary>
    private bool FollowSuspension()
    {
        if (_platform.Suspended != _suspended)
        {
            _suspended = !_suspended;
            if (_suspended) _game.Suspend();
            else
            {
                // The time spent away is not simulated.
                _last = _platform.Now;
                _accumulator = 0;
                _game.Resume();
            }
        }
        return _suspended;
    }

    /// <summary>Runs the fixed steps the elapsed time calls for; returns whether the run must stop and the capture due.</summary>
    private (bool Stop, string? Capture) Update()
    {
        if (_platform.RealTime)
        {
            double now = _platform.Now;
            _accumulator += Paced(Math.Min(now - _last, 0.25));
            _last = now;
        }
        else _accumulator += _step;
        _options.Script?.ApplyTouches(_game.UpdateCount, _game.Touch);
        _game.UpdateTouch(_platform.Touch, (float)Math.Clamp(_platform.Now - _lastTouch, 0, 0.25));
        _lastTouch = _platform.Now;

        int steps = 0;
        string? capture = null;
        bool stop = false;
        var script = _options.Script;
        while (_accumulator >= _step - 1e-9 && steps < 5)
        {
            script?.Apply(_game.UpdateCount, _game.Input);
            script?.ApplyPrompts(_game.UpdateCount, _game.Prompts);
            _game.Input.Sample(_platform.Input);
            _game.Step((float)_step);
            _accumulator -= _step;
            steps++;
            if (script?.CaptureAt(_game.UpdateCount) is { } name) capture = name;
            if (script?.QuitAt(_game.UpdateCount) == true || _options.MaxUpdates > 0 && _game.UpdateCount >= _options.MaxUpdates)
            {
                stop = true;
                break;
            }
            if (capture is not null) break;
        }
        if (steps == 5) _accumulator = Math.Min(_accumulator, _step);
        // One step per frame leaves nothing between updates: the frame is the update (Lerp at 1 draws it exactly).
        _game.FrameBlend = _platform.RealTime ? (float)Math.Clamp(_accumulator / _step, 0, 1) : 1;
        return (stop, capture);
    }

    private double _drift;

    /// <summary>
    /// The time a frame stands for. When the platform paces frames to a display (<see cref="IPlatform.DisplayRate"/>),
    /// each frame stays on screen for a whole number of refresh periods, however irregularly the loop wakes up around
    /// them; counting elapsed time in those periods keeps that jitter from turning into frames with no step followed by
    /// frames with two, the stutter of a display whose rate matches the update rate. The remainder is carried to the next
    /// frame, so a clock that disagrees with the reported rate still adds up to real time. Frames much shorter than a
    /// period (VSync not honoured) are taken as measured.
    /// </summary>
    private double Paced(double elapsed)
    {
        double rate = _options.PaceToDisplay ? _platform.DisplayRate : 0;
        if (rate <= 0) return elapsed;
        double period = 1 / rate, time = elapsed + _drift;
        // A short frame keeps the debt of an earlier frame rounded up, or frames faster than the reported rate would
        // each count as a whole period and the game would run fast.
        if (time < period / 2) { _drift = 0; return Math.Max(0, time); }
        double shown = Math.Round(time / period) * period;
        _drift = Math.Clamp(time - shown, -period, period);
        return shown;
    }

    private void Render()
    {
        var renderer = _platform.BeginFrame();
        _game.Render(renderer);
        _platform.EndFrame();
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
        return stop || _platform.CloseRequested || _game.QuitRequested;
    }

    /// <summary>Shuts the game down (once) and reports the run.</summary>
    public RunResult Finish()
    {
        if (_result is not null) return _result;
        _running = false;
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

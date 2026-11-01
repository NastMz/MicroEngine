using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace MicroEngine.Core;

/// <summary>Options shared by every launcher: scripted input, captures and run limits.</summary>
public sealed record RunOptions
{
    public InputScript? Script { get; init; }
    /// <summary>Directory where script "capture" commands write PNGs.</summary>
    public string CaptureDirectory { get; init; } = "captures";
    /// <summary>Stops after this many updates (0 = unlimited).</summary>
    public long MaxUpdates { get; init; }
    public int Seed { get; init; } = 1;
    public bool Mute { get; init; }
}

public sealed record RunResult(long Updates, long Frames, IReadOnlyList<string> Captures, Exception? Error);

/// <summary>
/// Runs a game to completion on platforms that own their thread (desktop, headless):
/// <see cref="GameLoop"/> ticked in a blocking loop.
/// </summary>
public static class GameRunner
{
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
            _game.Initialize(_platform, _options.Seed);
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
        _platform.PollEvents();
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
        if (_suspended) return _platform.CloseRequested || _game.QuitRequested;
        if (_platform.RealTime)
        {
            double now = _platform.Now;
            _accumulator += Math.Min(now - _last, 0.25);
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

        var renderer = _platform.BeginFrame();
        _game.Render(renderer);
        _platform.EndFrame();
        _frames++;
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
    private long? _quit;

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
                    default: throw new FormatException($"Unknown command '{parts[1]}'.");
                }
            }
            catch (Exception e) when (e is FormatException or IndexOutOfRangeException or OverflowException)
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

    public InputScript Hold(long at, string action, long duration)
    {
        _edges.Add((at, action, true));
        _edges.Add((at + Math.Max(1, duration), action, false));
        return this;
    }

    public InputScript Press(long at, string action) => Hold(at, action, 2);
    public InputScript Capture(long at, string fileName) { _captures[at] = fileName; return this; }
    public InputScript QuitAfter(long at) { _quit = at; return this; }

    /// <summary>Applies every edge scheduled for the update about to run.</summary>
    public void Apply(long update, Input.InputMap input)
    {
        foreach (var (at, action, down) in _edges)
            if (at == update) input.SetForced(action, down);
    }

    public string? CaptureAt(long update) => _captures.TryGetValue(update, out var name) ? name : null;
    public bool QuitAt(long update) => _quit is { } q && update >= q;
    public long? QuitUpdate => _quit;
}

/// <summary>Measures wall-clock time for desktop platforms.</summary>
public sealed class MonotonicClock
{
    private readonly Stopwatch _watch = Stopwatch.StartNew();
    public double Seconds => _watch.Elapsed.TotalSeconds;
}

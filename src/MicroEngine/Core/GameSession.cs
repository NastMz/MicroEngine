using MicroEngine.Hosting;

namespace MicroEngine.Core;

/// <summary>
/// Runs one game at a time for hosts that drive frames themselves and outlive a run, such as a browser
/// page: each <see cref="Start(LaunchRequest, Func{RunOptions, RunOptions})"/> builds a fresh game and platform, <see cref="Tick"/> runs one frame, and
/// the end of a run (quit, error, <see cref="Stop"/>) shuts the game down and releases the platform, so the
/// host can show its own screen and start again without reloading.
/// </summary>
/// <remarks>
/// A session made from a composition (<see cref="GameSession(Func{LaunchRequest, GameHost})"/>, the browser host's) decides
/// every run with the launcher model all hosts share (<see cref="GameHost"/>): a run with a script is in deterministic
/// time with seed 1 exactly as on desktop and phones, so its captures are reproducible.
/// </remarks>
public sealed class GameSession
{
    private readonly Func<Game>? _createGame;
    private readonly Func<Game, IPlatform>? _createPlatform;
    private readonly Func<LaunchRequest, GameHost>? _compose;
    private GameLoop? _loop;
    private IPlatform? _platform;

    /// <summary>A session that builds each run's game and platform with the two functions given.</summary>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    public GameSession(Func<Game> createGame, Func<Game, IPlatform> createPlatform)
    {
        _createGame = createGame ?? throw new ArgumentNullException(nameof(createGame));
        _createPlatform = createPlatform ?? throw new ArgumentNullException(nameof(createPlatform));
    }

    /// <summary>
    /// A session whose every run is composed by <paramref name="compose"/> for that run's launch (the game's
    /// <c>Configure</c> entry point with the host's defaults), then decided by the shared launcher model.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="compose"/> is null.</exception>
    public GameSession(Func<LaunchRequest, GameHost> compose) => _compose = compose ?? throw new ArgumentNullException(nameof(compose));

    /// <summary>The game of the current or last run.</summary>
    public Game? Game { get; private set; }
    /// <summary>The platform of the current run; null between runs.</summary>
    public IPlatform? Platform => _platform;
    /// <summary>The composition of the current or last run of a composed session; null otherwise.</summary>
    public GameHost? Host { get; private set; }
    /// <summary>Whether a run is in progress.</summary>
    public bool IsRunning => _loop?.IsRunning == true;
    /// <summary>Frames rendered by the current or last run.</summary>
    public long Frames => _loop?.Frames ?? Result?.Frames ?? 0;
    /// <summary>How the last run ended; null while a run is in progress or before the first one.</summary>
    public RunResult? Result { get; private set; }

    /// <summary>
    /// Builds and initializes a new game with <paramref name="options"/> as they are (a session made from two functions).
    /// False when that failed; the error is in <see cref="Result"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">A run is in progress, or the session was made from a composition (use <see cref="Start(LaunchRequest, Func{RunOptions, RunOptions})"/>).</exception>
    public bool Start(RunOptions? options = null)
    {
        if (_compose is not null) throw new InvalidOperationException("This session is composed: start it with a LaunchRequest.");
        BeginRun();
        try
        {
            Game = _createGame!();
            _platform = _createPlatform!(Game);
        }
        catch (Exception e)
        {
            return Failed(e);
        }
        return StartLoop(new GameLoop(Game, _platform, options));
    }

    /// <summary>
    /// Composes and starts a new run for <paramref name="request"/>: its script, captures, seed and time mode follow the
    /// launcher model (<see cref="GameHost.CreateRunOptions"/>), its platform the composition's catalog and request.
    /// False when that failed; the error is in <see cref="Result"/>.
    /// </summary>
    /// <param name="request">The launch.</param>
    /// <param name="change">Changes the run's options before the run starts (a phone host's first-frame callback), or null.</param>
    /// <exception cref="FormatException">The request's script is invalid (nothing was started).</exception>
    /// <exception cref="InvalidOperationException">A run is in progress, or the session was made from two functions.</exception>
    public bool Start(LaunchRequest request, Func<RunOptions, RunOptions>? change = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (_compose is null) throw new InvalidOperationException("This session builds games from functions: start it with RunOptions.");
        BeginRun();
        GameHost host;
        try { host = Host = _compose(request); }
        catch (Exception e) { return Failed(e); }
        var script = host.ReadScript();
        RunOptions options;
        try
        {
            Game = host.CreateGame();
            options = host.CreateRunOptions(Game, script: script);
            if (change is not null) options = change(options);
            (_platform, _) = host.CreatePlatform(host.CreateRequest(Game, options));
        }
        catch (Exception e)
        {
            return Failed(e);
        }
        return StartLoop(host.CreateLoop(Game, _platform, options));
    }

    private void BeginRun()
    {
        if (_loop is not null) throw new InvalidOperationException("A game is already running; stop it first.");
        Result = null;
    }

    private bool Failed(Exception e)
    {
        Result = new RunResult(0, 0, [], e);
        Release();
        return false;
    }

    private bool StartLoop(GameLoop loop)
    {
        _loop = loop;
        if (_loop.Start()) return true;
        End();
        return false;
    }

    /// <summary>Runs one frame. Returns false once the run has ended (see <see cref="Result"/>).</summary>
    public bool Tick()
    {
        if (_loop is null) return false;
        if (_loop.Tick()) return true;
        End();
        return false;
    }

    /// <summary>Ends a running game (for example when the page is closing). Does nothing otherwise.</summary>
    public void Stop()
    {
        if (_loop is not null) End();
    }

    private void End()
    {
        Result = _loop!.Finish();
        _loop = null;
        Release();
    }

    private void Release()
    {
        try { _platform?.Dispose(); }
        catch (Exception e) when (Result is { Error: null }) { Result = Result with { Error = e }; }
        finally { _platform = null; }
    }
}

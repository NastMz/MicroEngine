namespace MicroEngine.Core;

/// <summary>
/// Runs one game at a time for hosts that drive frames themselves and outlive a run, such as a browser
/// page: each <see cref="Start"/> builds a fresh game and platform, <see cref="Tick"/> runs one frame, and
/// the end of a run (quit, error, <see cref="Stop"/>) shuts the game down and releases the platform, so the
/// host can show its own screen and start again without reloading.
/// </summary>
public sealed class GameSession(Func<Game> createGame, Func<Game, IPlatform> createPlatform)
{
    private readonly Func<Game> _createGame = createGame ?? throw new ArgumentNullException(nameof(createGame));
    private readonly Func<Game, IPlatform> _createPlatform = createPlatform ?? throw new ArgumentNullException(nameof(createPlatform));
    private GameLoop? _loop;
    private IPlatform? _platform;

    /// <summary>The game of the current or last run.</summary>
    public Game? Game { get; private set; }
    /// <summary>Whether a run is in progress.</summary>
    public bool IsRunning => _loop?.IsRunning == true;
    /// <summary>Frames rendered by the current or last run.</summary>
    public long Frames => _loop?.Frames ?? Result?.Frames ?? 0;
    /// <summary>How the last run ended; null while a run is in progress or before the first one.</summary>
    public RunResult? Result { get; private set; }

    /// <summary>Builds and initializes a new game. False when that failed; the error is in <see cref="Result"/>.</summary>
    public bool Start(RunOptions? options = null)
    {
        if (_loop is not null) throw new InvalidOperationException("A game is already running; stop it first.");
        Result = null;
        try
        {
            Game = _createGame();
            _platform = _createPlatform(Game);
        }
        catch (Exception e)
        {
            Result = new RunResult(0, 0, [], e);
            Release();
            return false;
        }
        _loop = new GameLoop(Game, _platform, options);
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

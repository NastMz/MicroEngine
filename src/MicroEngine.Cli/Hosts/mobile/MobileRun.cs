using MicroEngine.Core;

/// <summary>What a mobile host was asked to run: an input script (its text), developer environment variables
/// (the games' desktop shortcuts, such as MEADOW_START) and where captures go.</summary>
internal sealed record MobileLaunch(string? ScriptName, string? Script, IReadOnlyDictionary<string, string> Environment, string CaptureDirectory)
{
    /// <summary>Parses "NAME=value;NAME=value" as given on the command line of a test.</summary>
    public static IReadOnlyDictionary<string, string> ParseEnvironment(string? pairs)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in (pairs ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            int eq = pair.IndexOf('=');
            if (eq <= 0) throw new FormatException($"Environment entry '{pair}' is not NAME=value.");
            result[pair[..eq]] = pair[(eq + 1)..];
        }
        return result;
    }
}

/// <summary>
/// One run of the selected game (<see cref="GameEntry"/>) for a mobile host, with the same options as
/// <c>DesktopLauncher --script</c>: a scripted run has a fixed step, seed 1 and writes its captures. Every line it
/// logs starts with "microengine:" so a test can find it in the device log.
/// </summary>
internal static class MobileRun
{
    public static int Run(MobileLaunch launch, Func<Game, bool, IPlatform> createPlatform, Action<string> log, Action? firstFrame = null)
    {
        foreach (var (name, value) in launch.Environment) Environment.SetEnvironmentVariable(name, value);
        RunOptions options;
        try
        {
            options = launch.Script is { } text
                ? new RunOptions { Script = InputScript.Parse(text), Seed = 1, CaptureDirectory = launch.CaptureDirectory, FirstFrame = firstFrame }
                : new RunOptions { Seed = Environment.TickCount & 0x7fffffff, FirstFrame = firstFrame };
        }
        catch (FormatException e)
        {
            log($"microengine: error: {launch.ScriptName}: {e.Message}");
            return 2;
        }
        log($"microengine: start {GameEntry.Id} script={launch.ScriptName ?? "none"} on {System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier}");
        Game game;
        IPlatform platform;
        try
        {
            game = GameEntry.Create();
            platform = createPlatform(game, launch.Script is not null);
        }
        catch (Exception e)
        {
            log($"microengine: error: {e}");
            return 1;
        }
        RunResult result;
        bool audio;
        // The active scene, logged when it changes, so a test can tell that input sent by the system reached the game
        // without depending on a screenshot. A diagnostic read from a timer thread: only a reference is read.
        string? scene = null, probe = null;
        bool suspended = false, controlsLogged = false;
        // MICROENGINE_PROBE=1 (a test's --env) also logs where the touch controls are on this screen, once, and the
        // actions down with the scene's own description whenever they change, so a check can tell that touches moved
        // the player rather than only that a scene changed.
        bool probing = Environment.GetEnvironmentVariable("MICROENGINE_PROBE") == "1";
        using var watch = new Timer(_ =>
        {
            string? now = game.Scenes?.Current?.GetType().Name;
            if (now is not null && now != scene) log($"microengine: scene {scene = now}");
            if (platform.Suspended != suspended) log($"microengine: {((suspended = !suspended) ? "suspended" : "resumed")}");
            if (!probing || game.Touch is not { } touch || game.Input is not { } input) return;
            try
            {
                if (!controlsLogged && touch.PixelsPerDp > 0 && touch.Layout.Controls.Count > 0 && touch.Geometry(touch.Layout.Controls[0].Id).Radius > 0)
                {
                    controlsLogged = true;
                    foreach (var c in touch.Layout.Controls)
                    {
                        var (center, radius) = touch.Geometry(c.Id);
                        log(FormattableString.Invariant($"microengine: control {c.Id} {center.X:0} {center.Y:0} {radius:0}"));
                    }
                }
                var down = string.Join(',', input.Actions.Concat(touch.Actions).Distinct().Where(input.Down));
                string line = $"{now} [{down}] {game.Scenes?.Current?.Describe()}";
                if (line != probe) log($"microengine: probe {probe = line}");
            }
            catch (InvalidOperationException) { /* collections changed under the diagnostic read; next tick */ }
        }, null, 0, 100);
        using (platform)
        {
            audio = platform.Audio.IsAvailable;
            result = GameRunner.Run(game, platform, options);
        }
        watch.Change(Timeout.Infinite, Timeout.Infinite);
        foreach (var capture in result.Captures) log($"microengine: capture {Path.GetFileName(capture)}");
        if (result.Error is { } error)
        {
            log($"microengine: error: {error}");
            return 1;
        }
        log($"microengine: done {result.Updates} updates, {result.Frames} frames, {result.Captures.Count} captures, audio {(audio ? "available" : "unavailable")}");
        return 0;
    }
}

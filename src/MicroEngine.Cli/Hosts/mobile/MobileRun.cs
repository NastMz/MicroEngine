using MicroEngine.Core;
using MicroEngine.Hosting;
using MicroEngine.Platform;
using MicroEngine.Sdl;

/// <summary>
/// One run of the selected game for a mobile host, composed by the game's own <c>Configure</c> entry point
/// (<see cref="GameEntry"/>) through the launcher model every host shares (<see cref="GameHost"/>): a scripted run is in
/// deterministic time, does not wait for the display, has seed 1 and writes its captures, exactly as
/// <c>DesktopLauncher --script</c>. The phone always asks for a sound device (package checks prove one opens). Every
/// line it logs starts with "microengine:" so a test can find it in the device log.
/// </summary>
internal static class MobileRun
{
    public static int Run(MobileLaunch launch, HostKind kind, IContentSource? content, Action<string> log, Action? firstFrame = null)
    {
        // A test's developer variables (the games' desktop shortcuts, such as MEADOW_START) reach the game the way a
        // desktop shell's or the page's do: as the host environment's variables, which game code reads.
        var environment = launch.CreateEnvironment(kind);
        var request = new LaunchRequest { ScriptText = launch.Script, ScriptName = launch.ScriptName, CaptureDirectory = launch.CaptureDirectory, Audio = "device" };
        GameHost host;
        try
        {
            host = GameHost.Create(environment, request, GameEntry.Configure, defaults =>
            {
                defaults.UsePlatforms(new PlatformCatalog().Add(SdlPlatform.Profile));
                defaults.Content = content;
            });
        }
        catch (Exception e)
        {
            log($"microengine: error: {e}");
            return 1;
        }
        InputScript? script;
        try { script = host.ReadScript(); }
        catch (FormatException e)
        {
            log($"microengine: error: {launch.ScriptName}: {e.Message}");
            return 2;
        }
        log($"microengine: start {GameEntry.Id} script={launch.ScriptName ?? "none"} on {System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier}");
        Game game;
        IPlatform platform;
        RunOptions options;
        try
        {
            game = host.CreateGame();
            options = host.CreateRunOptions(game, script: script) with { FirstFrame = firstFrame };
            (platform, _) = host.CreatePlatform(host.CreateRequest(game, options));
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
        bool lifecycleLogged = false, controlsLogged = false;
        // MICROENGINE_PROBE=1 (a test's --env) also logs where the touch controls are on this screen, once, and the
        // actions down with the scene's own description whenever they change, so a check can tell that touches moved
        // the player rather than only that a scene changed.
        bool probing = environment.Variable("MICROENGINE_PROBE") == "1";
        using var watch = new Timer(_ =>
        {
            string? now = game.Scenes?.Current?.GetType().Name;
            if (now is not null && now != scene) log($"microengine: scene {scene = now}");
            // The game's lifecycle exists once it started; suspensions are then logged as the loop raises them.
            if (!lifecycleLogged && game.Scenes is not null)
            {
                lifecycleLogged = true;
                game.Lifecycle.Suspending += () => log("microengine: suspended");
                game.Lifecycle.Resumed += () => log("microengine: resumed");
            }
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
            result = host.Run(game, platform, options);
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

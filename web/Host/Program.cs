using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using MicroEngine.Core;
using MicroEngine.Raylib;

[assembly: SupportedOSPlatform("browser")]

// Main only brings the runtime up; the page starts the game (WebHost.Start) once the player asks to play.
Console.WriteLine($"{GameEntry.Title}: ready");

/// <summary>
/// The page's bridge to the game (wwwroot/main.js): start a run, one frame per animation frame, and how the run
/// ended. The game is whichever web/games.props entry this package was built for (<see cref="GameEntry"/>).
/// </summary>
public static partial class WebHost
{
    private static readonly GameSession Session = BrowserLauncher.CreateSession(GameEntry.Create);

    /// <summary>
    /// Starts a new run; returns "" on success, otherwise the error. In development packages a non-empty
    /// <paramref name="script"/> plays scripted input with captures written to /captures, as --script does on desktop.
    /// </summary>
    [JSExport]
    public static string Start(string script)
    {
        RunOptions options;
        try
        {
            options = GameEntry.DevHooks && script.Length > 0
                ? new RunOptions { Script = InputScript.Parse(script), Seed = 1, CaptureDirectory = "/captures" }
                : new RunOptions { Seed = Environment.TickCount & 0x7fffffff };
        }
        catch (FormatException e) { return e.Message; }
        return Session.Start(options) ? "" : Describe(Session.Result?.Error);
    }

    /// <summary>Runs one frame; false once the run has ended (then see <see cref="Outcome"/>).</summary>
    [JSExport]
    public static bool Frame() => Session.Tick();

    /// <summary>How the last run ended: "" when the game quit normally, otherwise the error.</summary>
    [JSExport]
    public static string Outcome() => Session.Result?.Error is { } e ? Describe(e) : "";

    /// <summary>Ends a running game, saving what it saves on shutdown (the page is being closed).</summary>
    [JSExport]
    public static void Stop() => Session.Stop();

    /// <summary>
    /// A touch or pen pointer on the canvas (Pointer Events): phase 0 down, 1 move, 2 up, 3 cancel; position in canvas
    /// pixels. It reaches the game's on-screen controls through the raylib platform's touch screen.
    /// </summary>
    [JSExport]
    public static void Pointer(double id, int phase, double x, double y) => BrowserTouch.Pointer((long)id, phase, (float)x, (float)y);

    /// <summary>The page's safe-area insets in canvas pixels, canvas pixels per CSS pixel, and whether touch is the main input.</summary>
    [JSExport]
    public static void Screen(double left, double top, double right, double bottom, double pixelsPerDp, bool touchFirst) =>
        BrowserTouch.Screen((float)left, (float)top, (float)right, (float)bottom, (float)pixelsPerDp, touchFirst);

    /// <summary>The page was hidden or shown again: a hidden page suspends the game (music paused, fingers released).</summary>
    [JSExport]
    public static void Visibility(bool hidden) => BrowserLifecycle.Hidden = hidden;

    /// <summary>Diagnostics for tests and validation: "updates frames" of the current or last run.</summary>
    [JSExport]
    public static string Stats() => $"{Session.Game?.UpdateCount ?? 0} {Session.Frames}";

    /// <summary>
    /// Development diagnostics: the current scene's type, the input actions that are down, and the touch controls'
    /// state ("touch=shown:2" = drawn, two fingers down).
    /// </summary>
    [JSExport]
    public static string Probe()
    {
        if (!GameEntry.DevHooks || !Session.IsRunning || Session.Game is not { } game) return "";
        var input = game.Input;
        var actions = input.Actions.Concat(game.Touch.Actions).Distinct().Where(input.Down);
        return $"{game.Scenes.Current?.GetType().Name} {string.Join(',', actions)} touch={(game.Touch.Shown ? "shown" : "hidden")}:{game.Touch.FingerCount} {game.Scenes.Current?.Describe()}";
    }

    /// <summary>
    /// Development diagnostics: the touch controls as last laid out, one per line, "id x y radius visible" in canvas
    /// pixels, then "frame x y width height shown": where the game frame was drawn.
    /// </summary>
    [JSExport]
    public static string Controls()
    {
        if (!GameEntry.DevHooks || !Session.IsRunning || Session.Game is not { } game) return "";
        var touch = game.Touch;
        var lines = new List<string>();
        foreach (var control in touch.Layout.Controls)
        {
            var (c, r) = touch.Geometry(control.Id);
            lines.Add(FormattableString.Invariant($"{control.Id} {c.X:0.#} {c.Y:0.#} {r:0.#} {(touch.IsVisible(control.Id) ? 1 : 0)}"));
        }
        var f = touch.LastFrame;
        lines.Add(FormattableString.Invariant($"frame {f.X:0.#} {f.Y:0.#} {f.Width:0.#} {f.Height:0.#} {(touch.Shown ? 1 : 0)}"));
        return string.Join('\n', lines);
    }

    /// <summary>Development diagnostics: the PNG captures scripted runs wrote, by file name.</summary>
    [JSExport]
    public static string[] Captures() => GameEntry.DevHooks && Directory.Exists("/captures")
        ? [.. Directory.GetFiles("/captures", "*.png").Select(Path.GetFileName).OfType<string>().Order(StringComparer.Ordinal)]
        : [];

    /// <summary>Development diagnostics: one capture as base64 PNG data.</summary>
    [JSExport]
    public static string CaptureData(string name) => GameEntry.DevHooks && !name.Contains('/') && !name.Contains('\\')
        ? Convert.ToBase64String(File.ReadAllBytes(Path.Combine("/captures", name)))
        : "";

    private static string Describe(Exception? e)
    {
        if (e is null) return "";
        Console.Error.WriteLine(e);
        return $"{e.GetType().Name}: {e.Message}";
    }
}

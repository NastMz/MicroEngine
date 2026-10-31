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

    /// <summary>Diagnostics for tests and validation: "updates frames" of the current or last run.</summary>
    [JSExport]
    public static string Stats() => $"{Session.Game?.UpdateCount ?? 0} {Session.Frames}";

    /// <summary>Development diagnostics: the current scene's type and the input actions that are down.</summary>
    [JSExport]
    public static string Probe()
    {
        if (!GameEntry.DevHooks || !Session.IsRunning || Session.Game is not { } game) return "";
        var input = game.Input;
        return $"{game.Scenes.Current?.GetType().Name} {string.Join(',', input.Actions.Where(input.Down))}";
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

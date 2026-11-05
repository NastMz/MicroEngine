using System.Globalization;
using MicroEngine.Kernel;

namespace MicroEngine.Hosting;

/// <summary>
/// What one launch of a game asks for, whichever host received it: a desktop command line (<see cref="Parse"/>), a
/// browser page's query string, a phone's launch extras or a test. Every host turns its input into this record and
/// hands it to the same launcher model, so the rules that follow from it are written once: a run with a script is in
/// deterministic time unless <see cref="Time"/> says otherwise (<see cref="EffectiveTime"/>) and has seed 1 unless
/// <see cref="Seed"/> says otherwise (<see cref="ResolveSeed"/>).
/// </summary>
/// <remarks>
/// Options that name a value of a module the hosting layer does not know (the GPU, the window mode, where sound may go)
/// are kept as the words of the command line, already checked against the words allowed; the engine maps them to its
/// own types.
/// </remarks>
public sealed record LaunchRequest
{
    /// <summary>The words <see cref="Audio"/> accepts.</summary>
    public static IReadOnlyList<string> AudioWords { get; } = ["auto", "device", "silent"];
    /// <summary>The words <see cref="Gpu"/> accepts.</summary>
    public static IReadOnlyList<string> GpuWords { get; } = ["auto", "high-performance", "power-saving"];
    /// <summary>The words <see cref="Window"/> accepts.</summary>
    public static IReadOnlyList<string> WindowWords { get; } = ["windowed", "fullscreen", "exclusive"];

    /// <summary>The one-line usage text desktop launchers print after an argument error.</summary>
    public const string Usage = "Usage: [--script FILE] [--captures DIR] [--platform NAME] [--list-platforms] [--headless] [--hidden] [--time realtime|deterministic] [--frames N] [--seed N] [--scale N] [--mute] [--report] [--audio auto|device|silent] [--audio-capture FILE] [--gpu auto|high-performance|power-saving] [--renderer NAME[,NAME...]] [--graphics NAME] [--vsync on|off] [--fps-limit N] [--window windowed|fullscreen|exclusive] [--display N] [--pacing display|measured] [--profile FILE] [--state-trace FILE]";

    /// <summary><c>--script</c>: the input script file to read, or null. Hosts that hold the text itself use <see cref="ScriptText"/>.</summary>
    public string? ScriptPath { get; init; }
    /// <summary>The input script's text, for hosts that receive it rather than a file (a browser page, a phone's bundled script); wins over <see cref="ScriptPath"/>.</summary>
    public string? ScriptText { get; init; }
    /// <summary>The script's name for messages: the file name of <see cref="ScriptPath"/>, or the one the host gave with <see cref="ScriptText"/>.</summary>
    public string? ScriptName { get; init; }
    /// <summary>Whether the run plays an input script.</summary>
    public bool HasScript => ScriptText is not null || ScriptPath is not null;
    /// <summary><c>--captures</c>: where script captures are written ("captures" by default).</summary>
    public string CaptureDirectory { get; init; } = "captures";
    /// <summary><c>--platform</c>: the platform profile to run on, or null to let the host choose.</summary>
    public string? Platform { get; init; }
    /// <summary><c>--list-platforms</c>: print the host's platforms and exit.</summary>
    public bool ListPlatforms { get; init; }
    /// <summary><c>--headless</c>: run without a window, the same as <c>--platform headless</c>.</summary>
    public bool Headless { get; init; }
    /// <summary><c>--hidden</c>: render into a hidden window; it implies nothing about time or sound.</summary>
    public bool Hidden { get; init; }
    /// <summary><c>--time</c>: how the loop advances time, or null to leave it to <see cref="EffectiveTime"/>'s rule.</summary>
    public TimeMode? Time { get; init; }
    /// <summary><c>--frames</c>: stop after this many updates; 0 (the default) runs until the game quits.</summary>
    public long Frames { get; init; }
    /// <summary><c>--seed</c>: the seed of the game's randomness, or null for <see cref="ResolveSeed"/>'s rule.</summary>
    public int? Seed { get; init; }
    /// <summary><c>--scale</c>: the window scale, or null for the game's.</summary>
    public int? Scale { get; init; }
    /// <summary><c>--mute</c>: no sound.</summary>
    public bool Mute { get; init; }
    /// <summary><c>--report</c>: print the platform that runs and whether it has audio before the game starts.</summary>
    public bool Report { get; init; }
    /// <summary><c>--audio</c>: where sound may go, one of <see cref="AudioWords"/> ("auto" by default).</summary>
    public string Audio { get; init; } = "auto";
    /// <summary><c>--audio-capture</c>: the WAV file to write what would have been heard to, or null.</summary>
    public string? AudioCapturePath { get; init; }
    /// <summary><c>--gpu</c>: one of <see cref="GpuWords"/>, or null to keep the game's choice.</summary>
    public string? Gpu { get; init; }
    /// <summary><c>--renderer</c>: graphics APIs to try first, or null to keep the game's choice.</summary>
    public IReadOnlyList<string>? Renderers { get; init; }
    /// <summary><c>--graphics</c>: the graphics device to draw with ("sdl-renderer", "sdl-gpu"...), or null to keep the game's choice.</summary>
    public string? Graphics { get; init; }
    /// <summary><c>--vsync</c>: on or off, or null to keep the game's choice.</summary>
    public bool? VSync { get; init; }
    /// <summary><c>--fps-limit</c>: the frame limit (0 for none), or null to keep the game's choice.</summary>
    public int? FrameRateLimit { get; init; }
    /// <summary><c>--window</c>: one of <see cref="WindowWords"/>, or null to keep the game's choice.</summary>
    public string? Window { get; init; }
    /// <summary><c>--display</c>: the display to open on, counted from 0, or null to keep the game's choice.</summary>
    public int? Display { get; init; }
    /// <summary><c>--pacing</c>: false for <c>measured</c>, true (the default) for <c>display</c>.</summary>
    public bool PaceToDisplay { get; init; } = true;
    /// <summary><c>--profile</c>: the file to write frame measurements to, or null to measure nothing.</summary>
    public string? ProfilePath { get; init; }
    /// <summary>
    /// <c>--state-trace</c>: the file to write the run's per-step state hashes to, one line per fixed step with the hash
    /// of each section (the kernel's <c>StateTrace</c> text format), or null to hash nothing.
    /// </summary>
    public string? StateTracePath { get; init; }

    /// <summary>
    /// The time mode the run asks the loop for: <see cref="Time"/> when given, else <see cref="TimeMode.Deterministic"/>
    /// for a run with a script, else null (the game's choice, then the composition's preset, then real time). The rule is
    /// the same on every host: a scripted run in a browser or on a phone advances one fixed step per frame exactly as on
    /// desktop, so its captures are reproducible.
    /// </summary>
    public TimeMode? EffectiveTime => Time ?? (HasScript ? TimeMode.Deterministic : null);

    /// <summary>Whether the run asks for deterministic time (<see cref="EffectiveTime"/>).</summary>
    public bool IsDeterministic => EffectiveTime == TimeMode.Deterministic;

    /// <summary>The seed of the run: <see cref="Seed"/> when given, else 1 for a run with a script, else <paramref name="clockSeed"/>.</summary>
    public int ResolveSeed(int clockSeed) => Seed ?? (HasScript ? 1 : clockSeed);

    /// <summary>
    /// Parses a desktop command line (the options of <see cref="Usage"/>). A script file must exist when
    /// <paramref name="fileExists"/> (by default <see cref="File.Exists"/>) says so.
    /// </summary>
    /// <exception cref="FormatException">An argument is unknown, misses its value or has an invalid one, <c>--headless</c> contradicts <c>--platform</c>, or the script file does not exist.</exception>
    public static LaunchRequest Parse(IReadOnlyList<string> args, Func<string, bool>? fileExists = null)
    {
        ArgumentNullException.ThrowIfNull(args);
        var request = new LaunchRequest();
        for (int i = 0; i < args.Count; i++)
        {
            string Next() => i + 1 < args.Count ? args[++i] : throw new FormatException($"Missing value for {args[i]}.");
            int Int()
            {
                string option = args[i], value = Next();
                return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) && v > 0 ? v
                    : throw new FormatException($"{option} needs a positive integer, not '{value}'.");
            }
            int Count()
            {
                string option = args[i], value = Next();
                return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) && v >= 0 ? v
                    : throw new FormatException($"{option} needs 0 or a positive integer, not '{value}'.");
            }
            string Word(IReadOnlyList<string> allowed, string list)
            {
                string option = args[i], value = Next();
                return allowed.Contains(value) ? value : throw new FormatException($"{option} needs {list}, not '{value}'.");
            }
            request = args[i] switch
            {
                "--script" => request with { ScriptPath = Next() },
                "--captures" => request with { CaptureDirectory = Next() },
                "--platform" => request with { Platform = Next() },
                "--list-platforms" => request with { ListPlatforms = true },
                "--headless" => request with { Headless = true },
                "--hidden" => request with { Hidden = true },
                "--mute" => request with { Mute = true },
                "--report" => request with { Report = true },
                "--frames" => request with { Frames = Int() },
                "--seed" => request with { Seed = Int() },
                "--scale" => request with { Scale = Int() },
                "--audio" => request with { Audio = Word(AudioWords, "auto, device or silent") },
                "--audio-capture" => request with { AudioCapturePath = Next() },
                "--profile" => request with { ProfilePath = Next() },
                "--state-trace" => request with { StateTracePath = Next() },
                "--gpu" => request with { Gpu = Word(GpuWords, "auto, high-performance or power-saving") },
                "--renderer" => request with { Renderers = RendererNames(Next()) },
                "--graphics" => request with { Graphics = Next() },
                "--vsync" => request with { VSync = Word(["on", "off"], "on or off") == "on" },
                "--fps-limit" => request with { FrameRateLimit = Count() },
                "--display" => request with { Display = Count() },
                "--window" => request with { Window = Word(WindowWords, "windowed, fullscreen or exclusive") },
                "--time" => request with { Time = Word(["realtime", "deterministic"], "realtime or deterministic") == "realtime" ? TimeMode.RealTime : TimeMode.Deterministic },
                "--pacing" => request with { PaceToDisplay = Word(["display", "measured"], "display or measured") == "display" },
                _ => throw new FormatException($"Unknown argument '{args[i]}'."),
            };
        }
        if (request.Headless && request.Platform is { } platform && !string.Equals(platform, HeadlessName, StringComparison.OrdinalIgnoreCase))
            throw new FormatException($"--headless and --platform {platform} contradict each other.");
        if (request.ScriptPath is { } script && !(fileExists ?? File.Exists)(script)) throw new FormatException($"Script '{script}' not found.");
        return request with { ScriptName = request.ScriptPath is { } path ? Path.GetFileName(path) : null };
    }

    /// <summary>The name of the windowless platform <c>--headless</c> stands for.</summary>
    public const string HeadlessName = "headless";

    private static string[] RendererNames(string value)
    {
        var names = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return names.Length > 0 ? names : throw new FormatException("--renderer needs at least one name.");
    }
}

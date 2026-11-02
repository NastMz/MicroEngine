using System.Globalization;

namespace MicroEngine.Core;

/// <summary>
/// The command line every desktop game accepts, on any backend. Supported arguments:
/// <list type="bullet">
/// <item><c>--script FILE</c> play scripted input (see <see cref="InputScript"/>); implies a fixed step.</item>
/// <item><c>--captures DIR</c> directory for script captures (default ./captures).</item>
/// <item><c>--platform NAME</c> run on that platform of the catalog (default: <c>MICROENGINE_PLATFORM</c>, else the
/// first one that can run here); <c>--list-platforms</c> prints them and exits.</item>
/// <item><c>--headless</c> run without a window (no captures are written); the same as <c>--platform headless</c>.</item>
/// <item><c>--hidden</c> render in a hidden window (captures still work).</item>
/// <item><c>--frames N</c> stop after N updates; <c>--seed N</c>; <c>--scale N</c>; <c>--mute</c>.</item>
/// <item><c>--report</c> print the platform that runs and whether it has audio before the game starts (package checks).</item>
/// <item><c>--audio auto|device|silent</c> where sound may go (<see cref="AudioOutput"/>); by default hidden and
/// scripted runs open no physical device, so checks and tests are never heard.</item>
/// <item><c>--audio-capture FILE</c> play nothing aloud and write what would have been heard as a WAV
/// (<see cref="Audio.CaptureAudioDevice"/>).</item>
/// <item><c>--pacing display|measured</c> count frame time in refresh periods of the display (the default) or as
/// measured (<see cref="RunOptions.PaceToDisplay"/>).</item>
/// <item><c>--profile FILE</c> measure every frame and the start phases (<see cref="FrameProfiler"/>), print a summary and
/// write the results as JSON, or CSV when FILE ends in .csv.</item>
/// </list>
/// Returns a process exit code: 0 on success, 1 on a game or platform error, 2 on bad arguments.
/// </summary>
public static class GameLauncher
{
    /// <summary>The one-line usage text printed after an argument error.</summary>
    public const string Usage = "Usage: [--script FILE] [--captures DIR] [--platform NAME] [--list-platforms] [--headless] [--hidden] [--frames N] [--seed N] [--scale N] [--mute] [--report] [--audio auto|device|silent] [--audio-capture FILE] [--pacing display|measured] [--profile FILE]";

    /// <param name="configure">Changes the platform request before the platform is built: an audio device or input
    /// sources of the host's own (see <see cref="PlatformRequest"/>).</param>
    public static int Run(Game game, string[] args, PlatformCatalog catalog, Func<PlatformRequest, PlatformRequest>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(catalog);
        LaunchArguments parsed;
        try { parsed = LaunchArguments.Parse(args); }
        catch (FormatException e)
        {
            Console.Error.WriteLine(e.Message);
            Console.Error.WriteLine(Usage);
            return 2;
        }
        if (parsed.ListPlatforms)
        {
            foreach (var p in catalog.Profiles)
                Console.WriteLine($"{p.Name}{(p.UnsupportedReason is { } why ? $" (unavailable: {why})" : "")}: {p.Description}");
            return 0;
        }

        var profiler = parsed.ProfilePath is null ? null : new FrameProfiler();
        var options = new RunOptions
        {
            Profiler = profiler,
            PaceToDisplay = parsed.PaceToDisplay,
            Script = parsed.ScriptPath is { } path ? InputScript.Parse(File.ReadAllText(path)) : null,
            CaptureDirectory = parsed.CaptureDirectory,
            MaxUpdates = parsed.Frames,
            Seed = parsed.Seed,
            Mute = parsed.Mute,
        };
        var request = new PlatformRequest(game.Settings)
        {
            Scale = parsed.Scale,
            Hidden = parsed.Hidden,
            FixedStep = parsed.ScriptPath is not null || parsed.Hidden,
            Mute = parsed.Mute,
            AudioOutput = parsed.Audio,
            Identity = AppIdentity.Of(game),
        };
        Audio.CaptureAudioDevice? capture = null;
        if (parsed.AudioCapturePath is not null)
            request = request with { Audio = () => capture = new Audio.CaptureAudioDevice(game.Settings.UpdateRate) };
        if (configure is not null) request = configure(request);
        IPlatform platform;
        PlatformProfile? profile = null;
        try
        {
            string? name = parsed.Headless ? HeadlessPlatform.Profile.Name : parsed.Platform ?? Environment.GetEnvironmentVariable("MICROENGINE_PLATFORM");
            profile = parsed.Headless && catalog.Find(HeadlessPlatform.Profile.Name) is null ? HeadlessPlatform.Profile : catalog.Resolve(name);
            platform = profile.Create(request);
            profiler?.Phase("platform");
        }
        catch (Exception e) when (e is InvalidOperationException or PlatformConfigurationException)
        {
            Console.Error.WriteLine($"error: {e.Message}");
            return 1;
        }
        // A backend's native library that this system cannot load (missing, or built for a newer C library): the
        // loader's message names the cause, so it is reported instead of crashing.
        catch (DllNotFoundException e)
        {
            Console.Error.WriteLine($"error: platform '{profile?.Name}' could not load its native library. {e.Message}");
            return 1;
        }
        using var owned = platform;
        if (parsed.Report) foreach (var line in Report(profile, platform)) Console.WriteLine(line);
        var result = GameRunner.Run(game, platform, options);
        // Described after the run, so facts the platform learns while running (such as late start steps) are included.
        if (profiler is not null) Describe(profiler, profile, platform, game.Settings);
        foreach (var shot in result.Captures) Console.WriteLine($"capture: {shot}");
        if (capture is not null && parsed.AudioCapturePath is { } wav)
        {
            File.WriteAllBytes(wav, capture.Output.ToWav());
            Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"audio capture: {wav} ({capture.Duration:0.00} s, {capture.Played.Count} sounds)"));
        }
        if (profiler is not null && parsed.ProfilePath is { } report)
        {
            int warmup = Math.Min(ProfileWarmupFrames, (int)Math.Max(0, profiler.FrameCount - 1));
            profiler.Save(report, warmup);
            foreach (var (phase, ms) in profiler.Phases) Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"phase: {phase} {ms:0.0} ms"));
            Console.WriteLine(profiler.Summarize(warmup));
            Console.WriteLine($"profile: {report}");
        }
        if (result.Error is { } error)
        {
            Console.Error.WriteLine($"error: {error}");
            return 1;
        }
        Console.WriteLine($"done: {result.Updates} updates, {result.Frames} frames");
        return 0;
    }

    /// <summary>Frames of a profiled run left out of its summary: the first second or so, while the JIT compiles and caches warm up.</summary>
    public const int ProfileWarmupFrames = 60;

    /// <summary>Records the facts a profile is compared by: machine, system, runtime, platform and canvas.</summary>
    private static void Describe(FrameProfiler profiler, PlatformProfile profile, IPlatform platform, GameSettings settings)
    {
        var env = profiler.Environment;
        env["os"] = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
        env["architecture"] = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString();
        env["runtime"] = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
        env["processors"] = Environment.ProcessorCount.ToString(CultureInfo.InvariantCulture);
        env["gc"] = System.Runtime.GCSettings.IsServerGC ? "server" : "workstation";
        env["platform"] = Report(profile, platform)[0]["platform: ".Length..];
        env["realTime"] = platform.RealTime ? "true" : "false";
        env["canvas"] = $"{settings.VirtualWidth}x{settings.VirtualHeight}";
        env["updateRate"] = settings.UpdateRate.ToString(CultureInfo.InvariantCulture);
        env["engine"] = typeof(Game).Assembly.GetName().Version?.ToString() ?? "";
        if (platform is IPlatformDiagnostics diagnostics)
            foreach (var (key, value) in diagnostics.Diagnostics) env[key] = value;
    }

    /// <summary>
    /// What <c>--report</c> prints: the profile that runs, with what the platform says about itself when it overrides
    /// <see cref="object.ToString"/> (SDL names its graphics API), and whether an audio device is open.
    /// </summary>
    public static IReadOnlyList<string> Report(PlatformProfile profile, IPlatform platform)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(platform);
        string? self = platform.ToString();
        string detail = self is null || self == platform.GetType().ToString() ? "" : $" ({self})";
        return [$"platform: {profile.Name}{detail}", $"audio: {(platform.Audio.IsAvailable ? "available" : "unavailable")}"];
    }
}

/// <summary>The parsed launcher command line (see <see cref="GameLauncher"/>).</summary>
/// <param name="ScriptPath"><c>--script</c>: the input script file, or null.</param>
/// <param name="CaptureDirectory"><c>--captures</c>: where script captures are written ("captures" by default).</param>
/// <param name="Headless"><c>--headless</c>: run without a window.</param>
/// <param name="Hidden"><c>--hidden</c>: render into a hidden window.</param>
/// <param name="Frames"><c>--frames</c>: stop after this many updates; 0 runs until the game quits.</param>
/// <param name="Seed"><c>--seed</c>: the seed of <see cref="Game.Random"/>. Without it, a run with a script uses 1 and any other run a clock-based seed.</param>
/// <param name="Scale"><c>--scale</c>: the window scale, or null for <see cref="GameSettings.Scale"/>.</param>
/// <param name="Mute"><c>--mute</c>: no sound.</param>
public sealed record LaunchArguments(string? ScriptPath, string CaptureDirectory, bool Headless, bool Hidden, long Frames, int Seed, int? Scale, bool Mute)
{
    /// <summary>The platform named with <c>--platform</c>, or null for the default.</summary>
    public string? Platform { get; init; }
    /// <summary><c>--list-platforms</c>: print the catalog and exit.</summary>
    public bool ListPlatforms { get; init; }
    /// <summary><c>--report</c>: print the platform and its audio state before running.</summary>
    public bool Report { get; init; }
    /// <summary><c>--audio</c>: where sound may go.</summary>
    public AudioOutput Audio { get; init; }
    /// <summary><c>--audio-capture</c>: the WAV file to write what would have been heard to, or null.</summary>
    public string? AudioCapturePath { get; init; }
    /// <summary><c>--pacing</c>: false for <c>measured</c>, true (the default) for <c>display</c>.</summary>
    public bool PaceToDisplay { get; init; } = true;
    /// <summary><c>--profile</c>: the file to write frame measurements to, or null to measure nothing.</summary>
    public string? ProfilePath { get; init; }

    /// <summary>Parses command-line arguments.</summary>
    /// <exception cref="FormatException">An argument is unknown, misses its value or has an invalid one, <c>--headless</c> contradicts <c>--platform</c>, or the script file does not exist.</exception>
    public static LaunchArguments Parse(IReadOnlyList<string> args)
    {
        string? script = null, captures = "captures", platform = null, audioCapture = null, profile = null;
        var audio = AudioOutput.Automatic;
        bool paceToDisplay = true;
        bool headless = false, hidden = false, mute = false, list = false, report = false;
        long frames = 0;
        int? seed = null;
        int? scale = null;
        for (int i = 0; i < args.Count; i++)
        {
            string Next() => i + 1 < args.Count ? args[++i] : throw new FormatException($"Missing value for {args[i]}.");
            int Int()
            {
                string option = args[i], value = Next();
                return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) && v > 0 ? v
                    : throw new FormatException($"{option} needs a positive integer, not '{value}'.");
            }
            switch (args[i])
            {
                case "--script": script = Next(); break;
                case "--captures": captures = Next(); break;
                case "--platform": platform = Next(); break;
                case "--list-platforms": list = true; break;
                case "--headless": headless = true; break;
                case "--hidden": hidden = true; break;
                case "--mute": mute = true; break;
                case "--report": report = true; break;
                case "--frames": frames = Int(); break;
                case "--seed": seed = Int(); break;
                case "--scale": scale = Int(); break;
                case "--audio":
                    audio = Next() switch
                    {
                        "auto" => AudioOutput.Automatic,
                        "device" => AudioOutput.Device,
                        "silent" => AudioOutput.Silent,
                        var other => throw new FormatException($"--audio needs auto, device or silent, not '{other}'."),
                    };
                    break;
                case "--audio-capture": audioCapture = Next(); break;
                case "--profile": profile = Next(); break;
                case "--pacing":
                    paceToDisplay = Next() switch
                    {
                        "display" => true,
                        "measured" => false,
                        var other => throw new FormatException($"--pacing needs display or measured, not '{other}'."),
                    };
                    break;
                default: throw new FormatException($"Unknown argument '{args[i]}'.");
            }
        }
        if (headless && platform is not null && !string.Equals(platform, HeadlessPlatform.Profile.Name, StringComparison.OrdinalIgnoreCase))
            throw new FormatException($"--headless and --platform {platform} contradict each other.");
        if (script is not null && !File.Exists(script)) throw new FormatException($"Script '{script}' not found.");
        return new LaunchArguments(script, captures, headless, hidden, frames, seed ?? (script is null ? Environment.TickCount & 0x7fffffff : 1), scale, mute)
        {
            Platform = platform,
            ListPlatforms = list,
            Report = report,
            Audio = audio,
            AudioCapturePath = audioCapture,
            ProfilePath = profile,
            PaceToDisplay = paceToDisplay,
        };
    }
}

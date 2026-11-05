using MicroEngine.Hosting;
using MicroEngine.Platform;

namespace MicroEngine.Core;

/// <summary>
/// The command line every desktop game accepts, on any backend: a thin caller of the launcher model every host shares
/// (<see cref="GameHost"/>, <see cref="LaunchRequest"/>). Supported arguments:
/// <list type="bullet">
/// <item><c>--script FILE</c> play scripted input (see <see cref="InputScript"/>); implies deterministic time unless
/// <c>--time</c> says otherwise.</item>
/// <item><c>--captures DIR</c> directory for script captures (default ./captures).</item>
/// <item><c>--platform NAME</c> run on that platform of the catalog (default: <c>MICROENGINE_PLATFORM</c>, else the
/// first one that can run here); <c>--list-platforms</c> prints them and exits.</item>
/// <item><c>--headless</c> run without a window (no captures are written); the same as <c>--platform headless</c>. A
/// headless run is in deterministic time unless <c>--time</c> says otherwise (the headless platform's
/// <see cref="RunPreset"/>).</item>
/// <item><c>--hidden</c> render in a hidden window (captures still work); it implies nothing about time or sound.</item>
/// <item><c>--time realtime|deterministic</c> how the loop advances time (<see cref="Kernel.TimeMode"/>): with the
/// wall clock, or one fixed step per frame; by default the game's choice, else the platform's <see cref="RunPreset"/>
/// (headless: deterministic), else real time; a script means deterministic.</item>
/// <item><c>--frames N</c> stop after N updates; <c>--seed N</c>; <c>--scale N</c>; <c>--mute</c>.</item>
/// <item><c>--report</c> print the platform that runs and whether it has audio before the game starts (package checks).</item>
/// <item><c>--audio auto|device|silent</c> where sound may go (<see cref="AudioOutput"/>); <c>auto</c> (the default)
/// asks for no physical device when the run is in deterministic time (scripts, <c>--time deterministic</c>), so checks
/// are never heard, and for one otherwise (the headless platform has none).</item>
/// <item><c>--audio-capture FILE</c> play nothing aloud and write what would have been heard as a WAV
/// (<see cref="Audio.CaptureAudioDevice"/>).</item>
/// <item><c>--gpu auto|high-performance|power-saving</c> the GPU to ask for on a machine with two
/// (<see cref="GpuPreference"/>).</item>
/// <item><c>--renderer NAME[,NAME...]</c> graphics APIs to try first (<see cref="GraphicsSettings.Renderers"/>).</item>
/// <item><c>--graphics NAME</c> the graphics device to draw with, on platforms that offer several: SDL's
/// <c>sdl-renderer</c> or <c>sdl-gpu</c> (<see cref="GraphicsSettings.Device"/>).</item>
/// <item><c>--vsync on|off</c> wait for the display's refresh or not (<see cref="GraphicsSettings.VSync"/>).</item>
/// <item><c>--fps-limit N</c> draw at most N frames per second; 0 removes the game's limit
/// (<see cref="GraphicsSettings.FrameRateLimit"/>).</item>
/// <item><c>--window windowed|fullscreen|exclusive</c> the window mode (<see cref="GraphicsSettings.WindowMode"/>).</item>
/// <item><c>--display N</c> open on display N, counted from 0 (<see cref="GraphicsSettings.Display"/>).</item>
/// <item><c>--pacing display|measured</c> count frame time in refresh periods of the display (the default) or as
/// measured (<see cref="RunOptions.PaceToDisplay"/>).</item>
/// <item><c>--profile FILE</c> measure every frame and the start phases (<see cref="FrameProfiler"/>), print a summary and
/// write the results as JSON, or CSV when FILE ends in .csv.</item>
/// <item><c>--state-trace FILE</c> hash the simulation state after every fixed step (<see cref="RunOptions.StateTrace"/>)
/// and write one line per step with each section's hash (<see cref="Kernel.StateTrace"/>'s text format): two runs of the
/// same build, script and seed write the same file, and <see cref="Kernel.StateTrace.FirstDivergence(Kernel.StateTrace, Kernel.StateTrace)"/>
/// names where two traces part.</item>
/// </list>
/// A run in deterministic time also asks the platform, through explicit request options, not to wait for the display
/// (<see cref="PlatformRequest.WaitForDisplay"/>) and for no sound device (<c>--audio auto</c>); a run in real time
/// whose game shows a start screen asks it to start gamepads after the first frame
/// (<see cref="PlatformRequest.StartGamepadsAfterFirstFrame"/>). The platform itself never knows the time mode.
/// Browser pages, phone apps and tests apply the same rules through <see cref="GameHost"/>.
/// Returns a process exit code: 0 on success, 1 on a game or platform error, 2 on bad arguments.
/// </summary>
public static class GameLauncher
{
    /// <summary>The one-line usage text printed after an argument error.</summary>
    public const string Usage = LaunchRequest.Usage;

    /// <summary>Runs <paramref name="game"/> with the command line <paramref name="args"/> on the platforms of <paramref name="catalog"/>.</summary>
    /// <param name="configure">Changes the platform request before the platform is built: an audio device or input
    /// sources of the host's own (see <see cref="PlatformRequest"/>).</param>
    public static int Run(Game game, string[] args, PlatformCatalog catalog, Func<PlatformRequest, PlatformRequest>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(catalog);
        return Run(args, host =>
        {
            host.UsePlatforms(catalog).UseGame(() => game);
            if (configure is not null) host.ConfigureRequest(configure);
        });
    }

    /// <summary>
    /// Runs the game a composition chooses (a game's <c>Configure</c> entry point, <see cref="GameHost"/>) with the
    /// command line <paramref name="args"/> as a desktop process. <paramref name="defaults"/> (the host's own platforms
    /// and content) runs before <paramref name="configure"/>, which may replace any of them.
    /// </summary>
    /// <returns>The process exit code: 0 on success, 1 on a game or platform error, 2 on bad arguments.</returns>
    public static int Run(string[] args, Action<HostBuilder> configure, Action<HostBuilder>? defaults = null)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(configure);
        var environment = new HostEnvironment(HostKind.Desktop);
        LaunchRequest request;
        try { request = LaunchRequest.Parse(args); }
        catch (FormatException e)
        {
            environment.Error.WriteLine(e.Message);
            environment.Error.WriteLine(Usage);
            return 2;
        }
        GameHost host;
        try { host = GameHost.Create(environment, request, configure, defaults); }
        catch (Exception e) when (e is InvalidOperationException or PlatformConfigurationException)
        {
            environment.Error.WriteLine($"error: {e.Message}");
            return 1;
        }
        return host.Run();
    }

    /// <summary>
    /// The platforms tried, in order, when nobody named one: every platform of the catalog that can run here except the
    /// headless one (a player who sees no window would not know why), or the catalog's default when that leaves none.
    /// </summary>
    /// <exception cref="PlatformConfigurationException">The catalog has no platform that can run here.</exception>
    public static IReadOnlyList<PlatformProfile> Fallbacks(PlatformCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var (profiles, error) = ProfileSelection.Fallbacks(catalog.Profiles);
        return error is null ? profiles : throw new PlatformConfigurationException(error);
    }

    /// <summary>
    /// The platforms tried when the game prefers <paramref name="backend"/> (<see cref="GraphicsSettings.Backend"/>) and
    /// nobody named one: that platform first, then the <see cref="Fallbacks"/>. A preference this catalog lacks or that
    /// cannot run here is reported on standard error and skipped.
    /// </summary>
    /// <exception cref="PlatformConfigurationException">The catalog has no platform that can run here.</exception>
    public static IReadOnlyList<PlatformProfile> Preferred(PlatformCatalog catalog, string? backend)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var choice = ProfileSelection.Preferred(catalog.Profiles, backend);
        if (choice.Error is { } error) throw new PlatformConfigurationException(error);
        foreach (var note in choice.Notes) Console.Error.WriteLine(note);
        return choice.Candidates;
    }

    /// <summary>Frames of a profiled run left out of its summary: the first second or so, while the JIT compiles and caches warm up.</summary>
    public const int ProfileWarmupFrames = 60;

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

/// <summary>
/// The launcher command line with the engine's own types (see <see cref="GameLauncher"/>): a typed view of a
/// <see cref="LaunchRequest"/>, with the seed and the time mode resolved by its rules.
/// </summary>
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
    /// <summary><c>--gpu</c>: the GPU to ask for, or null to keep the game's choice.</summary>
    public GpuPreference? Gpu { get; init; }
    /// <summary><c>--renderer</c>: graphics APIs to try first, or null to keep the game's choice.</summary>
    public IReadOnlyList<string>? Renderers { get; init; }
    /// <summary><c>--graphics</c>: the graphics device to draw with, or null to keep the game's choice.</summary>
    public string? Graphics { get; init; }
    /// <summary><c>--vsync</c>: on or off, or null to keep the game's choice.</summary>
    public bool? VSync { get; init; }
    /// <summary><c>--fps-limit</c>: the frame limit (0 for none), or null to keep the game's choice.</summary>
    public int? FrameRateLimit { get; init; }
    /// <summary><c>--window</c>: the window mode, or null to keep the game's choice.</summary>
    public WindowMode? WindowMode { get; init; }
    /// <summary><c>--display</c>: the display to open on, or null to keep the game's choice.</summary>
    public int? Display { get; init; }

    /// <summary>The game's graphics options with the ones given on the command line in their place.</summary>
    public GraphicsSettings ApplyTo(GraphicsSettings graphics)
    {
        ArgumentNullException.ThrowIfNull(graphics);
        return graphics with
        {
            Backend = Platform ?? graphics.Backend,
            Renderers = Renderers ?? graphics.Renderers,
            Device = Graphics ?? graphics.Device,
            VSync = VSync ?? graphics.VSync,
            FrameRateLimit = FrameRateLimit ?? graphics.FrameRateLimit,
            Gpu = Gpu ?? graphics.Gpu,
            WindowMode = WindowMode ?? graphics.WindowMode,
            Display = Display ?? graphics.Display,
        };
    }
    /// <summary>
    /// <c>--time</c>: how the loop advances time, or null to leave it to the game. A script means
    /// <see cref="Kernel.TimeMode.Deterministic"/> unless <c>--time</c> is given (<see cref="LaunchRequest.EffectiveTime"/>);
    /// <c>--hidden</c> changes nothing; the headless platform's <see cref="RunPreset"/> makes its runs deterministic when
    /// this is null.
    /// </summary>
    public Kernel.TimeMode? Time { get; init; }
    /// <summary><c>--pacing</c>: false for <c>measured</c>, true (the default) for <c>display</c>.</summary>
    public bool PaceToDisplay { get; init; } = true;
    /// <summary><c>--profile</c>: the file to write frame measurements to, or null to measure nothing.</summary>
    public string? ProfilePath { get; init; }
    /// <summary><c>--state-trace</c>: the file to write the per-step state hashes to (<see cref="RunOptions.StateTrace"/>), or null.</summary>
    public string? StateTracePath { get; init; }

    /// <summary>Parses command-line arguments (<see cref="LaunchRequest.Parse"/>).</summary>
    /// <exception cref="FormatException">An argument is unknown, misses its value or has an invalid one, <c>--headless</c> contradicts <c>--platform</c>, or the script file does not exist.</exception>
    public static LaunchArguments Parse(IReadOnlyList<string> args) => From(LaunchRequest.Parse(args), Environment.TickCount & 0x7fffffff);

    /// <summary>The typed view of <paramref name="request"/>; a run that asks for no seed and plays no script gets <paramref name="clockSeed"/>.</summary>
    public static LaunchArguments From(LaunchRequest request, int clockSeed)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new LaunchArguments(request.ScriptPath, request.CaptureDirectory, request.Headless, request.Hidden, request.Frames, request.ResolveSeed(clockSeed), request.Scale, request.Mute)
        {
            Platform = request.Platform,
            ListPlatforms = request.ListPlatforms,
            Report = request.Report,
            Audio = request.Audio switch { "device" => AudioOutput.Device, "silent" => AudioOutput.Silent, _ => AudioOutput.Automatic },
            AudioCapturePath = request.AudioCapturePath,
            ProfilePath = request.ProfilePath,
            StateTracePath = request.StateTracePath,
            PaceToDisplay = request.PaceToDisplay,
            Time = request.EffectiveTime,
            Gpu = request.Gpu switch { null => null, "high-performance" => GpuPreference.HighPerformance, "power-saving" => GpuPreference.PowerSaving, _ => GpuPreference.Automatic },
            Renderers = request.Renderers,
            Graphics = request.Graphics,
            VSync = request.VSync,
            FrameRateLimit = request.FrameRateLimit,
            WindowMode = request.Window switch { null => null, "fullscreen" => Core.WindowMode.Fullscreen, "exclusive" => Core.WindowMode.ExclusiveFullscreen, _ => Core.WindowMode.Windowed },
            Display = request.Display,
        };
    }
}

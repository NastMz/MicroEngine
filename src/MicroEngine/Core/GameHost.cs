using System.Globalization;
using MicroEngine.Hosting;
using MicroEngine.Kernel;
using MicroEngine.Platform;

namespace MicroEngine.Core;

/// <summary>
/// The game's side of a <see cref="HostBuilder"/>: choosing the game, the platforms a host offers and changes to the
/// platform request. A game's <c>Configure</c> entry point calls these; every host (desktop, browser, phones, tests)
/// reads them through <see cref="GameHost"/>.
/// </summary>
public static class HostBuilderExtensions
{
    /// <summary>The game this composition runs: <paramref name="create"/> builds a new one for every run.</summary>
    /// <returns>The builder, for chaining.</returns>
    public static HostBuilder UseGame(this HostBuilder host, Func<Game> create)
    {
        ArgumentNullException.ThrowIfNull(create);
        Parts(host).CreateGame = create;
        return host;
    }

    /// <summary>
    /// The platforms the host offers, in order of preference (filled with the host's own before the game's
    /// <c>Configure</c> runs; add a profile to offer it with <c>--platform</c>).
    /// </summary>
    public static PlatformCatalog Platforms(this HostBuilder host) => Parts(host).Catalog;

    /// <summary>Replaces the platforms the host offers.</summary>
    /// <returns>The builder, for chaining.</returns>
    public static HostBuilder UsePlatforms(this HostBuilder host, PlatformCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        Parts(host).Catalog = catalog;
        return host;
    }

    /// <summary>
    /// Changes the platform request before the platform is built (an audio device or input sources of the host's own,
    /// <see cref="PlatformRequest"/>); changes apply in the order added.
    /// </summary>
    /// <returns>The builder, for chaining.</returns>
    public static HostBuilder ConfigureRequest(this HostBuilder host, Func<PlatformRequest, PlatformRequest> change)
    {
        ArgumentNullException.ThrowIfNull(change);
        Parts(host).RequestChanges.Add(change);
        return host;
    }

    /// <summary>
    /// A save store for <paramref name="gameId"/> on the composition's medium (<see cref="HostBuilder.Storage"/>), else the
    /// platform's default (<see cref="SaveStore.DefaultStorage(string)"/>).
    /// </summary>
    public static SaveStore SaveStore(this HostBuilder host, string gameId)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentException.ThrowIfNullOrWhiteSpace(gameId);
        return new SaveStore(host.Storage?.Invoke(gameId) ?? Core.SaveStore.DefaultStorage(gameId));
    }

    internal static GameParts Parts(HostBuilder host)
    {
        ArgumentNullException.ThrowIfNull(host);
        if (!host.Services.TryGet<GameParts>(out var parts)) host.Services.Add(parts = new GameParts());
        return parts;
    }

    /// <summary>What the engine keeps in a composition.</summary>
    internal sealed class GameParts
    {
        public Func<Game>? CreateGame;
        public PlatformCatalog Catalog = new();
        public readonly List<Func<PlatformRequest, PlatformRequest>> RequestChanges = [];
    }
}

/// <summary>
/// The one launch path of every host: desktop (<see cref="GameLauncher"/>), browser pages, Android and iOS apps and
/// tests compose the same way and decide a run the same way. A host states its defaults (its platforms, where content
/// files are), the game's <c>Configure</c> entry point composes the rest (<see cref="HostBuilder"/>), and the
/// <see cref="LaunchRequest"/> decides the run: a script means deterministic time and seed 1 on every host; a run in
/// deterministic time asks the platform not to wait for the display and for no sound device unless the launch asks for
/// one; a real-time run whose game shows a start screen starts gamepads after the first frame.
/// </summary>
/// <example>
/// A test that runs a game headless through the same path as the desktop launcher:
/// <code>
/// var host = GameHost.Create(new HostEnvironment(HostKind.Test), new LaunchRequest { Frames = 60 },
///     h => h.UseGame(() => new MyGame()), h => h.UsePlatforms(new PlatformCatalog().Add(HeadlessPlatform.Profile)));
/// var result = host.Play();
/// </code>
/// </example>
public sealed class GameHost
{
    private readonly HostEnvironment _environment;
    private readonly HostBuilderExtensions.GameParts _parts;

    private GameHost(HostBuilder builder)
    {
        Builder = builder;
        _environment = builder.Environment;
        _parts = HostBuilderExtensions.Parts(builder);
    }

    /// <summary>The composition: the host's defaults, then the game's <c>Configure</c>.</summary>
    public HostBuilder Builder { get; }
    /// <summary>What this launch asked for.</summary>
    public LaunchRequest Request => Builder.Request;
    /// <summary>The platforms this host offers, in order of preference.</summary>
    public PlatformCatalog Platforms => _parts.Catalog;

    /// <summary>
    /// Composes a host: <paramref name="defaults"/> (the host's own platforms, content and storage) first, then
    /// <paramref name="configure"/> (the game's <c>Configure</c> entry point), which may replace any of them.
    /// </summary>
    /// <exception cref="InvalidOperationException">The composition chose no game (<see cref="HostBuilderExtensions.UseGame"/>).</exception>
    public static GameHost Create(HostEnvironment environment, LaunchRequest request, Action<HostBuilder> configure, Action<HostBuilder>? defaults = null)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new HostBuilder(environment, request);
        HostBuilderExtensions.Parts(builder);
        defaults?.Invoke(builder);
        configure(builder);
        if (HostBuilderExtensions.Parts(builder).CreateGame is null)
            throw new InvalidOperationException("The composition chose no game: its Configure entry point must call host.UseGame(() => new MyGame()).");
        return new GameHost(builder);
    }

    /// <summary>A new game, built by the composition's <see cref="HostBuilderExtensions.UseGame"/>.</summary>
    public Game CreateGame() => _parts.CreateGame!() ?? throw new InvalidOperationException("The composition's game factory returned null.");

    /// <summary>The launch's input script, parsed: <see cref="LaunchRequest.ScriptText"/>, else the file <see cref="LaunchRequest.ScriptPath"/>; null without one.</summary>
    /// <exception cref="FormatException">The script is invalid.</exception>
    public InputScript? ReadScript() => Request.ScriptText is { } text ? InputScript.Parse(text)
        : Request.ScriptPath is { } path ? InputScript.Parse(File.ReadAllText(path)) : null;

    /// <summary>The typed launch options (<see cref="LaunchArguments"/>), with the seed resolved by the launch's rule.</summary>
    public LaunchArguments Arguments => _arguments ??= LaunchArguments.From(Request, _environment.ClockSeed());
    private LaunchArguments? _arguments;

    /// <summary>
    /// The run's options for <paramref name="game"/>: the graphics options with the launch's overrides, the script, its
    /// captures, the frame limit, the seed, mute, pacing and the time mode (<see cref="LaunchRequest.EffectiveTime"/>).
    /// </summary>
    /// <exception cref="FormatException">The script is invalid.</exception>
    public RunOptions CreateRunOptions(Game game, FrameProfiler? profiler = null, InputScript? script = null)
    {
        ArgumentNullException.ThrowIfNull(game);
        var arguments = Arguments;
        return new RunOptions
        {
            Graphics = arguments.ApplyTo(game.Settings.Graphics),
            Profiler = profiler,
            PaceToDisplay = arguments.PaceToDisplay,
            Script = script ?? ReadScript(),
            CaptureDirectory = arguments.CaptureDirectory,
            MaxUpdates = arguments.Frames,
            Seed = arguments.Seed,
            Mute = arguments.Mute,
            TimeMode = arguments.Time,
        };
    }

    /// <summary>
    /// What the run asks of its platform: the launch's window options, no wait for the display and no sound device in
    /// deterministic time (unless the launch asks for a device), gamepads after the first frame for a real-time run with
    /// a start screen, the game's identity; then the composition's request changes
    /// (<see cref="HostBuilderExtensions.ConfigureRequest"/>), in order.
    /// </summary>
    public PlatformRequest CreateRequest(Game game, RunOptions options)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(options);
        var arguments = Arguments;
        bool deterministic = options.TimeMode == TimeMode.Deterministic;
        var identity = AppIdentity.Of(game);
        var request = new PlatformRequest(game.Settings with { Graphics = options.Graphics ?? game.Settings.Graphics })
        {
            Scale = arguments.Scale,
            Hidden = arguments.Hidden,
            WaitForDisplay = !deterministic,
            StartGamepadsAfterFirstFrame = !deterministic && !arguments.Hidden && options.StartScreen && identity is { Splash: true },
            Mute = arguments.Mute,
            AudioOutput = arguments.Audio == AudioOutput.Automatic && deterministic ? AudioOutput.Silent : arguments.Audio,
            Identity = identity,
        };
        foreach (var change in _parts.RequestChanges) request = change(request) ?? throw new InvalidOperationException("A request change returned null.");
        return request;
    }

    /// <summary>The platforms the run tries, in order (<see cref="ProfileSelection.Choose{TProfile}"/>); notes about the choice go to the host's error output.</summary>
    /// <exception cref="PlatformConfigurationException">The launch named a platform that is unknown or cannot run here, or none can.</exception>
    public IReadOnlyList<PlatformProfile> Candidates(GraphicsSettings graphics)
    {
        ArgumentNullException.ThrowIfNull(graphics);
        var choice = ProfileSelection.Choose(Platforms.Profiles, Request, _environment, graphics.Backend, HeadlessPlatform.Profile);
        if (choice.Error is { } error) throw new PlatformConfigurationException(error);
        foreach (var note in choice.Notes) _environment.Error.WriteLine(note);
        return choice.Candidates;
    }

    /// <summary>
    /// Builds the platform for <paramref name="request"/>: the first candidate that starts, reporting each one that could
    /// not (a missing native library, no display) before trying the next.
    /// </summary>
    /// <exception cref="PlatformConfigurationException">No candidate could start; the message is the last one's reason.</exception>
    public (IPlatform Platform, PlatformProfile Profile) CreatePlatform(PlatformRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var candidates = Candidates(request.Settings.Graphics);
        for (int i = 0; ; i++)
        {
            var profile = candidates[i];
            string failure;
            try { return (profile.Create(request), profile); }
            catch (Exception e) when (e is InvalidOperationException or PlatformConfigurationException) { failure = e.Message; }
            // A backend's native library that this system cannot load (missing, or built for a newer C library): the
            // loader's message names the cause, so it is reported instead of crashing.
            catch (DllNotFoundException e) { failure = $"platform '{profile.Name}' could not load its native library. {e.Message}"; }
            if (i == candidates.Count - 1) throw new PlatformConfigurationException(failure);
            _environment.Error.WriteLine($"platform: {profile.Name} could not start ({failure}); trying {candidates[i + 1].Name}");
        }
    }

    /// <summary>A loop for one run of <paramref name="game"/> on <paramref name="platform"/>, with the composition applied to its engine.</summary>
    public GameLoop CreateLoop(Game game, IPlatform platform, RunOptions options) => new(game, platform, options, Compose);

    /// <summary>Runs <paramref name="game"/> on <paramref name="platform"/> to completion with the composition applied; the platform is not disposed.</summary>
    public RunResult Run(Game game, IPlatform platform, RunOptions options)
    {
        var loop = CreateLoop(game, platform, options);
        if (loop.Start())
            while (loop.Tick()) { }
        return loop.Finish();
    }

    /// <summary>
    /// Runs a new game of the composition to completion on the platform the launch chooses, for hosts that own their
    /// thread and tests; the platform is disposed after the run. Never throws for errors raised by the game or a
    /// platform that could not start: they are in <see cref="RunResult.Error"/>.
    /// </summary>
    /// <exception cref="FormatException">The launch's script is invalid.</exception>
    public RunResult Play(Action<RunOptions>? inspect = null)
    {
        var script = ReadScript();
        Game game;
        IPlatform platform;
        RunOptions options;
        try
        {
            game = CreateGame();
            options = CreateRunOptions(game, script: script);
            (platform, _) = CreatePlatform(CreateRequest(game, options));
        }
        catch (Exception e) when (e is not FormatException) { return new RunResult(0, 0, [], e); }
        inspect?.Invoke(options);
        using (platform) return Run(game, platform, options);
    }

    /// <summary>
    /// Runs the launch as a desktop process does (<see cref="GameLauncher"/>): lists the platforms, or builds the game
    /// and its platform, reports and profiles as asked, runs it, writes the audio capture and prints the outcome.
    /// </summary>
    /// <returns>The process exit code: 0 on success, 1 on a game or platform error, 2 on an invalid script.</returns>
    public int Run()
    {
        var output = _environment.Out;
        var errors = _environment.Error;
        var arguments = Arguments;
        if (arguments.ListPlatforms)
        {
            foreach (var p in Platforms.Profiles)
                output.WriteLine($"{p.Name}{(p.UnsupportedReason is { } why ? $" (unavailable: {why})" : "")}: {p.Description}");
            return 0;
        }
        InputScript? script;
        try { script = ReadScript(); }
        catch (FormatException e)
        {
            errors.WriteLine(e.Message);
            return 2;
        }
        var profiler = arguments.ProfilePath is null ? null : new FrameProfiler();
        Game game;
        RunOptions options;
        PlatformRequest request;
        try
        {
            game = CreateGame();
            options = CreateRunOptions(game, profiler, script);
            request = CreateRequest(game, options);
        }
        catch (InvalidOperationException e)
        {
            errors.WriteLine($"error: {e.Message}");
            return 1;
        }
        // Applied after the composition's changes: the option is explicit, so it replaces an audio device the host supplies.
        Audio.CaptureAudioDevice? capture = null;
        if (arguments.AudioCapturePath is not null)
            request = request with { Audio = () => capture = new Audio.CaptureAudioDevice(game.Settings.UpdateRate) };
        IPlatform platform;
        PlatformProfile profile;
        try { (platform, profile) = CreatePlatform(request); }
        catch (PlatformConfigurationException e)
        {
            errors.WriteLine($"error: {e.Message}");
            return 1;
        }
        profiler?.Phase("platform");
        using var owned = platform;
        if (arguments.Report) foreach (var line in GameLauncher.Report(profile, platform)) output.WriteLine(line);
        var result = Run(game, platform, options);
        // Described after the run, so facts the platform learns while running (such as late start steps) are included.
        if (profiler is not null) Describe(profiler, profile, platform, game);
        foreach (var shot in result.Captures) output.WriteLine($"capture: {shot}");
        if (capture is not null && arguments.AudioCapturePath is { } wav)
        {
            File.WriteAllBytes(wav, capture.Output.ToWav());
            output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"audio capture: {wav} ({capture.Duration:0.00} s, {capture.Played.Count} sounds)"));
        }
        if (profiler is not null && arguments.ProfilePath is { } report)
        {
            int warmup = Math.Min(GameLauncher.ProfileWarmupFrames, (int)Math.Max(0, profiler.FrameCount - 1));
            profiler.Save(report, warmup);
            foreach (var (phase, ms) in profiler.Phases) output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"phase: {phase} {ms:0.0} ms"));
            output.WriteLine(profiler.Summarize(warmup));
            output.WriteLine($"profile: {report}");
        }
        if (result.Error is { } error)
        {
            errors.WriteLine($"error: {error}");
            return 1;
        }
        output.WriteLine($"done: {result.Updates} updates, {result.Frames} frames");
        return 0;
    }

    /// <summary>The composition's part of every engine: its content (when the game names no folder) and its engine changes.</summary>
    private void Compose(EngineBuilder builder)
    {
        if (Builder.Content is { } content && builder.Services.TryGet<AssetCache>(out var assets)) assets.UseComposedContent(content);
        foreach (var configure in Builder.EngineConfiguration) configure(builder);
    }

    /// <summary>Records the facts a profile is compared by: machine, system, runtime, platform and canvas.</summary>
    private static void Describe(FrameProfiler profiler, PlatformProfile profile, IPlatform platform, Game game)
    {
        var settings = game.Settings;
        var env = profiler.Environment;
        env["os"] = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
        env["architecture"] = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString();
        env["runtime"] = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
        env["processors"] = Environment.ProcessorCount.ToString(CultureInfo.InvariantCulture);
        env["gc"] = System.Runtime.GCSettings.IsServerGC ? "server" : "workstation";
        env["platform"] = GameLauncher.Report(profile, platform)[0]["platform: ".Length..];
        env["realTime"] = game.EngineOrNull?.Time.Mode == TimeMode.Deterministic ? "false" : "true";
        env["canvas"] = $"{settings.VirtualWidth}x{settings.VirtualHeight}";
        env["updateRate"] = settings.UpdateRate.ToString(CultureInfo.InvariantCulture);
        env["engine"] = typeof(Game).Assembly.GetName().Version?.ToString() ?? "";
        if (platform.TryGetCapability<IPlatformDiagnostics>(out var diagnostics))
            foreach (var (key, value) in diagnostics.Diagnostics) env[key] = value;
    }

    /// <summary>
    /// A session for hosts that drive frames themselves and outlive a run (a browser page): each
    /// <see cref="GameSession.Start(LaunchRequest, Func{RunOptions, RunOptions})"/> composes again with <paramref name="compose"/> for that launch.
    /// </summary>
    public static GameSession CreateSession(Func<LaunchRequest, GameHost> compose) => new(compose);
}

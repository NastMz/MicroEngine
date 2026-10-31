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
/// </list>
/// Returns a process exit code: 0 on success, 1 on a game or platform error, 2 on bad arguments.
/// </summary>
public static class GameLauncher
{
    public const string Usage = "Usage: [--script FILE] [--captures DIR] [--platform NAME] [--list-platforms] [--headless] [--hidden] [--frames N] [--seed N] [--scale N] [--mute]";

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

        var options = new RunOptions
        {
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
        };
        if (configure is not null) request = configure(request);
        IPlatform platform;
        try
        {
            string? name = parsed.Headless ? HeadlessPlatform.Profile.Name : parsed.Platform ?? Environment.GetEnvironmentVariable("MICROENGINE_PLATFORM");
            var profile = parsed.Headless && catalog.Find(HeadlessPlatform.Profile.Name) is null ? HeadlessPlatform.Profile : catalog.Resolve(name);
            platform = profile.Create(request);
        }
        catch (Exception e) when (e is InvalidOperationException or PlatformConfigurationException)
        {
            Console.Error.WriteLine($"error: {e.Message}");
            return 1;
        }
        using var owned = platform;
        var result = GameRunner.Run(game, platform, options);
        foreach (var capture in result.Captures) Console.WriteLine($"capture: {capture}");
        if (result.Error is { } error)
        {
            Console.Error.WriteLine($"error: {error}");
            return 1;
        }
        Console.WriteLine($"done: {result.Updates} updates, {result.Frames} frames");
        return 0;
    }
}

public sealed record LaunchArguments(string? ScriptPath, string CaptureDirectory, bool Headless, bool Hidden, long Frames, int Seed, int? Scale, bool Mute)
{
    /// <summary>The platform named with <c>--platform</c>, or null for the default.</summary>
    public string? Platform { get; init; }
    public bool ListPlatforms { get; init; }

    public static LaunchArguments Parse(IReadOnlyList<string> args)
    {
        string? script = null, captures = "captures", platform = null;
        bool headless = false, hidden = false, mute = false, list = false;
        long frames = 0;
        int? seed = null;
        int? scale = null;
        for (int i = 0; i < args.Count; i++)
        {
            string Next() => i + 1 < args.Count ? args[++i] : throw new FormatException($"Missing value for {args[i]}.");
            int Int() => int.TryParse(Next(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) && v > 0 ? v : throw new FormatException($"{args[i]} needs a positive integer.");
            switch (args[i])
            {
                case "--script": script = Next(); break;
                case "--captures": captures = Next(); break;
                case "--platform": platform = Next(); break;
                case "--list-platforms": list = true; break;
                case "--headless": headless = true; break;
                case "--hidden": hidden = true; break;
                case "--mute": mute = true; break;
                case "--frames": frames = Int(); break;
                case "--seed": seed = Int(); break;
                case "--scale": scale = Int(); break;
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
        };
    }
}

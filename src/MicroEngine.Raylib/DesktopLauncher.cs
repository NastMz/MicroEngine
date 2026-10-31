using System.Globalization;
using MicroEngine.Core;

namespace MicroEngine.Raylib;

/// <summary>
/// Standard entry point for desktop games. Supported arguments:
/// <list type="bullet">
/// <item><c>--script FILE</c> play scripted input (see <see cref="InputScript"/>); implies a fixed step.</item>
/// <item><c>--captures DIR</c> directory for script captures (default ./captures).</item>
/// <item><c>--headless</c> run without a window (no captures are written).</item>
/// <item><c>--hidden</c> render in a hidden window (captures still work).</item>
/// <item><c>--frames N</c> stop after N updates; <c>--seed N</c>; <c>--scale N</c>; <c>--mute</c>.</item>
/// </list>
/// Returns a process exit code: 0 on success, 1 on a game error, 2 on bad arguments.
/// </summary>
public static class DesktopLauncher
{
    public static int Run(Game game, string[] args)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(args);
        LaunchArguments parsed;
        try { parsed = LaunchArguments.Parse(args); }
        catch (FormatException e)
        {
            Console.Error.WriteLine(e.Message);
            Console.Error.WriteLine("Usage: [--script FILE] [--captures DIR] [--headless] [--hidden] [--frames N] [--seed N] [--scale N] [--mute]");
            return 2;
        }

        var options = new RunOptions
        {
            Script = parsed.ScriptPath is { } path ? InputScript.Parse(File.ReadAllText(path)) : null,
            CaptureDirectory = parsed.CaptureDirectory,
            MaxUpdates = parsed.Frames,
            Seed = parsed.Seed,
            Mute = parsed.Mute,
        };
        IPlatform platform;
        try
        {
            platform = parsed.Headless
                ? HeadlessPlatform.For(game.Settings)
                : new RaylibPlatform(game.Settings, new DesktopOptions(parsed.Scale, parsed.Hidden, FixedStep: parsed.ScriptPath is not null || parsed.Hidden, parsed.Mute));
        }
        catch (InvalidOperationException e)
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
    public static LaunchArguments Parse(IReadOnlyList<string> args)
    {
        string? script = null, captures = "captures";
        bool headless = false, hidden = false, mute = false;
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
                case "--headless": headless = true; break;
                case "--hidden": hidden = true; break;
                case "--mute": mute = true; break;
                case "--frames": frames = Int(); break;
                case "--seed": seed = Int(); break;
                case "--scale": scale = Int(); break;
                default: throw new FormatException($"Unknown argument '{args[i]}'.");
            }
        }
        if (script is not null && !File.Exists(script)) throw new FormatException($"Script '{script}' not found.");
        return new LaunchArguments(script, captures, headless, hidden, frames, seed ?? (script is null ? Environment.TickCount & 0x7fffffff : 1), scale, mute);
    }
}

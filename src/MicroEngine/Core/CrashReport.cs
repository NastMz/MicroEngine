using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace MicroEngine.Core;

/// <summary>
/// A text file that describes a run that stopped on an error, for a tester to send: the game and engine versions, the
/// operating system and runtime, the platform's facts (<see cref="IPlatformDiagnostics"/>), the last measured frame and
/// its slowest systems when <see cref="Game.Diagnostics"/> measured, and the error with its stack trace. The desktop
/// launcher writes one when a game's run fails and names it on standard error.
/// </summary>
public static class CrashReport
{
    /// <summary>The report's text for <paramref name="error"/>, which stopped <paramref name="game"/>.</summary>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    public static string Describe(Game game, Exception error)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(error);
        var text = new StringBuilder();
        void Line(string line) => text.Append(line).Append('\n');
        var identity = AppIdentity.Of(game);
        var engine = typeof(Game).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
        Line("MicroEngine crash report");
        Line($"time: {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)} UTC");
        Line(identity is null ? $"game: {game.Settings.Title}" : $"game: {identity.Title} {identity.Version}{(identity.Id is { } id ? $" ({id})" : "")}");
        Line($"engine: {engine.Split('+')[0]}");
        Line($"runtime: {RuntimeInformation.FrameworkDescription} on {RuntimeInformation.OSDescription} ({RuntimeInformation.ProcessArchitecture})");
        if (game.TryGetCapability(out IPlatformDiagnostics? platform))
            foreach (var (key, value) in platform.Diagnostics.OrderBy(p => p.Key, StringComparer.Ordinal)) Line($"platform {key}: {value}");
        var frame = game.Diagnostics.LastFrame;
        if (frame.Frame == 0) Line("last frame: not measured (Game.Diagnostics was off)");
        else
        {
            Line(string.Create(CultureInfo.InvariantCulture,
                $"last frame: #{frame.Frame}, {frame.Total:0.00} ms (update {frame.Update:0.00}, render {frame.Render:0.00}, present {frame.Present:0.00}), {frame.Steps} steps, {frame.Allocated} B allocated, {frame.Collections} collections, {frame.Graphics.DrawCalls} draw calls, {frame.Audio.ActiveVoices} voices"));
            foreach (var system in game.Diagnostics.Systems.OrderByDescending(s => s.Milliseconds).Take(5))
                Line(string.Create(CultureInfo.InvariantCulture, $"  {system.Name} ({system.Phase}): {system.Milliseconds:0.000} ms"));
        }
        Line("error:");
        text.Append(error.ToString().ReplaceLineEndings("\n")).Append('\n');
        return text.ToString();
    }

    /// <summary>Writes the report into <paramref name="directory"/> (created if needed) as <c>crash-&lt;UTC time&gt;.txt</c> and returns its path.</summary>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    /// <exception cref="IOException">The file cannot be written.</exception>
    /// <exception cref="UnauthorizedAccessException">The folder is not writable.</exception>
    public static string Write(Game game, Exception error, string directory)
    {
        ArgumentNullException.ThrowIfNull(directory);
        var text = Describe(game, error);
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"crash-{DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture)}.txt");
        File.WriteAllText(path, text);
        return path;
    }
}

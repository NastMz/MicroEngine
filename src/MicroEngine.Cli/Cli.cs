using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace MicroEngine.Cli;

/// <summary>
/// A failure the user can act on: the message says what went wrong and what to do. It is printed as one
/// "error: ..." block with exit code 1 (2 for a wrong command line), never as a stack trace.
/// </summary>
public sealed class CliException(string message, int exitCode = 1) : Exception(message)
{
    public int ExitCode { get; } = exitCode;
}

/// <summary>Command-line options after the command name: positional values, <c>--name value</c>, <c>--flag</c>, and
/// everything after <c>--</c> passed through untouched.</summary>
public sealed class Options
{
    private readonly Dictionary<string, string?> _named = new(StringComparer.Ordinal);
    private readonly HashSet<string> _used = new(StringComparer.Ordinal);

    public List<string> Positional { get; } = [];
    public List<string> PassThrough { get; } = [];

    /// <param name="flags">Options that take no value.</param>
    public static Options Parse(IEnumerable<string> args, params string[] flags)
    {
        var options = new Options();
        var list = args.ToList();
        for (int i = 0; i < list.Count; i++)
        {
            string a = list[i];
            if (a == "--") { options.PassThrough.AddRange(list.Skip(i + 1)); break; }
            if (a.StartsWith("--", StringComparison.Ordinal) || (a.Length == 2 && a[0] == '-' && char.IsLetter(a[1])))
            {
                string name = a.TrimStart('-');
                if (name == "c") name = "configuration";
                if (name == "o") name = "output";
                if (flags.Contains(name)) options._named[name] = null;
                else if (i + 1 < list.Count) options._named[name] = list[++i];
                else throw new CliException($"option {a} needs a value.", 2);
            }
            else options.Positional.Add(a);
        }
        return options;
    }

    public string? Get(string name) { _used.Add(name); return _named.TryGetValue(name, out var v) ? v : null; }
    public bool Has(string name) { _used.Add(name); return _named.ContainsKey(name); }

    /// <summary>Fails on options the command did not read, so a typo is never silently ignored.</summary>
    public void RejectUnknown(string command)
    {
        var unknown = _named.Keys.Where(k => !_used.Contains(k)).ToList();
        if (unknown.Count > 0)
            throw new CliException($"unknown option{(unknown.Count > 1 ? "s" : "")} {string.Join(", ", unknown.Select(u => "--" + u))} for '{command}' (see microengine {command} --help).", 2);
    }
}

/// <summary>Runs the programs the tool drives (.NET, codesign, keytool...) and turns their failures into
/// <see cref="CliException"/>s that keep the program's own last lines of output.</summary>
public static class Tool
{
    /// <summary>Set by <c>--verbose</c>: every command line is printed and output streams through.</summary>
    public static bool Verbose { get; set; }

    /// <summary>The <c>dotnet</c> to use: MICROENGINE_DOTNET, else the one on PATH.</summary>
    public static string Dotnet => Environment.GetEnvironmentVariable("MICROENGINE_DOTNET") is { Length: > 0 } d ? d : "dotnet";

    /// <summary>Whether <paramref name="program"/> can be found on PATH (or is a path that exists).</summary>
    public static bool Exists(string program)
    {
        if (Path.IsPathRooted(program)) return File.Exists(program);
        var exts = OperatingSystem.IsWindows() ? (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE").Split(';') : [""];
        return (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator)
            .Any(dir => exts.Any(ext => File.Exists(Path.Combine(dir, program + ext))));
    }

    /// <summary>Runs a program and returns its standard output; fails with its last output lines when it fails.</summary>
    /// <param name="secrets">Values that must never appear in what the tool prints (passwords).</param>
    public static string Run(string program, IEnumerable<string> args, string? cwd = null, string what = "", IReadOnlyDictionary<string, string>? env = null, IEnumerable<string>? secrets = null)
    {
        var list = args.ToList();
        var start = new ProcessStartInfo(program) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, WorkingDirectory = cwd ?? Environment.CurrentDirectory };
        foreach (var a in list) start.ArgumentList.Add(a);
        if (env is not null) foreach (var (k, v) in env) start.Environment[k] = v;
        start.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        start.Environment["DOTNET_NOLOGO"] = "1";
        var hidden = (secrets ?? []).Where(s => s.Length > 0).ToList();
        string Mask(string s) => hidden.Aggregate(s, (acc, secret) => acc.Replace(secret, "***"));
        if (Verbose) Console.Error.WriteLine($"> {Mask(program)} {Mask(string.Join(' ', list.Select(Quote)))}");
        Process process;
        try { process = Process.Start(start)!; }
        catch (System.ComponentModel.Win32Exception)
        {
            throw new CliException($"'{program}' was not found{(what.Length > 0 ? $" ({what})" : "")}. Run 'microengine doctor' to see what is missing and how to install it.");
        }
        var output = new StringBuilder();
        var errors = new StringBuilder();
        process.OutputDataReceived += (_, e) => { if (e.Data is null) return; lock (output) output.AppendLine(e.Data); if (Verbose) Console.Error.WriteLine(Mask(e.Data)); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is null) return; lock (errors) errors.AppendLine(e.Data); if (Verbose) Console.Error.WriteLine(Mask(e.Data)); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            var lines = (output + "\n" + errors).Split('\n').Select(l => l.TrimEnd()).Where(l => l.Length > 0).ToList();
            // MSBuild repeats its errors at the end; those lines say what failed.
            var important = lines.Where(l => l.Contains(" error ", StringComparison.Ordinal) || l.StartsWith("error", StringComparison.OrdinalIgnoreCase)).Distinct().TakeLast(12).ToList();
            var shown = important.Count > 0 ? important : lines.TakeLast(15).ToList();
            throw new CliException($"{(what.Length > 0 ? what : Path.GetFileName(program))} failed (exit code {process.ExitCode}):\n  {Mask(string.Join("\n  ", shown))}\nRun again with --verbose for the full output.");
        }
        return output.ToString();
    }

    /// <summary>Runs a program attached to this console (the game itself, an interactive build).</summary>
    public static int Attach(string program, IEnumerable<string> args, string? cwd = null)
    {
        var start = new ProcessStartInfo(program) { UseShellExecute = false, WorkingDirectory = cwd ?? Environment.CurrentDirectory };
        foreach (var a in args) start.ArgumentList.Add(a);
        if (Verbose) Console.Error.WriteLine($"> {program} {string.Join(' ', start.ArgumentList.Select(Quote))}");
        try
        {
            using var process = Process.Start(start)!;
            process.WaitForExit();
            return process.ExitCode;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            throw new CliException($"'{program}' was not found. Run 'microengine doctor'.");
        }
    }

    private static string Quote(string a) => a.Contains(' ') ? $"\"{a}\"" : a;

    /// <summary>This tool's version (the SDK version it belongs to).</summary>
    public static string Version => typeof(Tool).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "0.0.0";

    /// <summary>The folder holding the tool's host templates and notices.</summary>
    public static string Home => AppContext.BaseDirectory;
}

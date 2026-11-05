using MicroEngine.Hosting;

/// <summary>What a mobile host was asked to run: an input script (its text), developer environment variables
/// (the games' desktop shortcuts, such as MEADOW_START) and where captures go.</summary>
internal sealed record MobileLaunch(string? ScriptName, string? Script, IReadOnlyDictionary<string, string> Environment, string CaptureDirectory)
{
    /// <summary>Parses "NAME=value;NAME=value" as given on the command line of a test.</summary>
    public static IReadOnlyDictionary<string, string> ParseEnvironment(string? pairs)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in (pairs ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            int eq = pair.IndexOf('=');
            if (eq <= 0) throw new FormatException($"Environment entry '{pair}' is not NAME=value.");
            result[pair[..eq]] = pair[(eq + 1)..];
        }
        return result;
    }

    /// <summary>
    /// The host environment the game is composed with: its developer variables are this launch's pairs (a test's
    /// <c>--env</c>), then the process's environment for names the launch does not set. The process environment is never
    /// written, so game code reads the pairs through <c>HostEnvironment.Variable</c> as it reads a desktop shell's.
    /// </summary>
    public HostEnvironment CreateEnvironment(HostKind kind) => new(kind)
    {
        Variables = name => Environment.TryGetValue(name, out var value) ? value : System.Environment.GetEnvironmentVariable(name),
    };
}

namespace MicroEngine.Hosting;

/// <summary>The kind of program hosting a game; the composition may differ by kind (a browser has no files to save to).</summary>
public enum HostKind
{
    /// <summary>A desktop process (Windows, Linux, macOS) started from its command line.</summary>
    Desktop,
    /// <summary>A browser page that drives frames from <c>requestAnimationFrame</c> and cannot block.</summary>
    Browser,
    /// <summary>An Android app.</summary>
    Android,
    /// <summary>An iOS app.</summary>
    IOS,
    /// <summary>A test or tool that runs games in its own process.</summary>
    Test,
}

/// <summary>
/// What a host knows about the process it runs in, handed to the composition instead of being read from process-wide
/// state: its kind, its developer variables (environment variables on desktop; the page's or the launch's variables
/// elsewhere), where it reports and where its files are. Tests build one with their own variables and writers.
/// </summary>
public sealed class HostEnvironment
{
    /// <summary>An environment of <paramref name="kind"/> over this process: its environment variables, the console and its base directory.</summary>
    public HostEnvironment(HostKind kind) => Kind = kind;

    /// <summary>The kind of host.</summary>
    public HostKind Kind { get; }
    /// <summary>Reads a developer variable by name, or null; the process's environment variables by default.</summary>
    public Func<string, string?> Variables { get; init; } = Environment.GetEnvironmentVariable;
    /// <summary>Where the host prints results (the console's standard output by default, as it was when this was created).</summary>
    public TextWriter Out { get; init; } = Console.Out;
    /// <summary>Where the host prints errors (the console's standard error by default, as it was when this was created).</summary>
    public TextWriter Error { get; init; } = Console.Error;
    /// <summary>The folder the host's program runs from (<see cref="AppContext.BaseDirectory"/> by default).</summary>
    public string BaseDirectory { get; init; } = AppContext.BaseDirectory;
    /// <summary>
    /// Where the desktop launcher writes the crash report of a run that fails (<c>Core.CrashReport</c>), or null for
    /// the game's save folder (<c>SaveStore.DefaultDirectory</c>).
    /// </summary>
    public string? CrashDirectory { get; init; }
    /// <summary>A seed for runs that ask for none, from the clock by default (<see cref="LaunchRequest.ResolveSeed"/>).</summary>
    public Func<int> ClockSeed { get; init; } = () => Environment.TickCount & 0x7fffffff;

    /// <summary>The developer variable <paramref name="name"/>, or null when it is unset or blank.</summary>
    public string? Variable(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return Variables(name) is { } value && !string.IsNullOrWhiteSpace(value) ? value : null;
    }
}

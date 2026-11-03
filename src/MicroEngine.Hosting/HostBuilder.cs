using MicroEngine.Kernel;
using MicroEngine.Platform;

namespace MicroEngine.Hosting;

/// <summary>
/// The composition of one host, configured once by the game's <c>Configure</c> entry point and reused by every host
/// that runs it: the desktop launcher (<c>Program.cs</c>), the browser page, the Android and iOS apps and tests. The
/// host fills its defaults first (its platforms, where content files are, where saves go), then calls the game's
/// <c>Configure</c>, which may replace any of them and adds what the game needs (the game itself, platforms of its own,
/// engine services). Nothing here is process-wide: two hosts in one process (an editor, parallel tests) never share a
/// composition.
/// </summary>
/// <remarks>
/// The engine's modules keep their own parts in <see cref="Services"/>; the game façade adds methods to choose the game
/// and change the platform catalog and request (<c>UseGame</c>, <c>Platforms</c>, <c>ConfigureRequest</c> in
/// <c>MicroEngine.Core</c>).
/// </remarks>
/// <example>
/// <code>
/// public sealed class MainGame : Game
/// {
///     public static void Configure(HostBuilder host) => host.UseGame(() => new MainGame());
///     ...
/// }
/// </code>
/// </example>
public sealed class HostBuilder
{
    private readonly List<Action<EngineBuilder>> _engine = [];

    /// <summary>A composition for one launch of <paramref name="request"/> in <paramref name="environment"/>.</summary>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    public HostBuilder(HostEnvironment environment, LaunchRequest request)
    {
        Environment = environment ?? throw new ArgumentNullException(nameof(environment));
        Request = request ?? throw new ArgumentNullException(nameof(request));
    }

    /// <summary>What the host knows about its process: its kind, developer variables and output.</summary>
    public HostEnvironment Environment { get; }
    /// <summary>The kind of host composing (<see cref="HostEnvironment.Kind"/>).</summary>
    public HostKind Kind => Environment.Kind;
    /// <summary>What this launch asked for (script, captures, platform, time...).</summary>
    public LaunchRequest Request { get; }
    /// <summary>The composition's own parts, by type: the engine's modules keep theirs here; a game may add its own.</summary>
    public ServiceRegistry Services { get; } = new();

    /// <summary>
    /// Where the game's content files come from when the game names no folder of its own; null (the default) for the
    /// <c>Content</c> folder next to the program. The desktop host sets a macOS bundle's resources, the Android app the
    /// folder it unpacked its assets to, the iOS app its bundle's folder.
    /// </summary>
    public IContentSource? Content { get; set; }

    /// <summary>
    /// The save medium for a game id, for games that ask the composition for it; null (the default) for the platform's
    /// default (files in the user's data folder, the page's localStorage in browsers).
    /// </summary>
    public Func<string, IStorage>? Storage { get; set; }

    /// <summary>Changes applied to every engine this host composes, after the game's own <c>Game.Configure</c>.</summary>
    public IReadOnlyList<Action<EngineBuilder>> EngineConfiguration => _engine;

    /// <summary>Adds a change to every engine this host composes (services, systems, settings), applied in the order added.</summary>
    /// <returns>This builder, for chaining.</returns>
    public HostBuilder ConfigureEngine(Action<EngineBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _engine.Add(configure);
        return this;
    }
}

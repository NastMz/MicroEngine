using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Hosting;

namespace Pathfinding;

/// <summary>
/// A top-down room of tiles where everything that moves finds its own way: the map is data, what each agent wants is
/// this game's rule, and the routes come from the navigation package.
/// </summary>
public sealed class PathfindingGame : Game
{
    /// <summary>The game's composition, called by every build (desktop Program.cs, browser, Android, iOS).</summary>
    public static void Configure(HostBuilder host) => host.UseGame(() => new PathfindingGame());

    /// <inheritdoc/>
    public override GameSettings Settings { get; } = new("Pathfinding") { Background = Color.Hex("1a1c2c") };

    /// <inheritdoc/>
    protected override void Load() => Scenes.Push(new RoomScene());
}

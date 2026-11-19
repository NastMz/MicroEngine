using System.Numerics;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Hosting;

namespace EcsSwarm;

/// <summary>
/// A beacon the player moves and a swarm that chases it. The swarm is twenty thousand rows of a component store; the
/// beacon and the carriers are entities of the scene's world. The game is an ordinary MicroEngine game: nothing in its
/// composition mentions the store.
/// </summary>
public sealed class SwarmGame : Game
{
    /// <summary>The game's composition, called by every build (desktop Program.cs, browser, Android, iOS).</summary>
    public static void Configure(HostBuilder host) => host.UseGame(() => new SwarmGame());

    /// <inheritdoc/>
    public override GameSettings Settings { get; } = new("ECS Swarm") { Background = Color.Hex("1a1c2c") };

    /// <summary>A two-pixel dot: every member of the swarm is drawn with it, in one batch.</summary>
    public Sprite Dot { get; private set; } = null!;

    /// <inheritdoc/>
    protected override void Load()
    {
        var dot = PixelImage.FromMap(Art.Palette("w=#f4f4f4"), "ww", "ww");
        Dot = new SpriteAtlasBuilder().Add("dot", dot, new Vector2(1, 1)).Build(Graphics)["dot"];
        Scenes.Push(new SwarmScene());
    }
}

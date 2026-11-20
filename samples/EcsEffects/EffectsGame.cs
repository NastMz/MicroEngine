using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Hosting;

namespace EcsEffects;

/// <summary>
/// An arena of walkers and crates, and a caster the player moves that shoots fire, ice or plain bolts. The game is an
/// ordinary MicroEngine game: its one scene keeps its objects in a component store.
/// </summary>
public sealed class EffectsGame : Game
{
    /// <summary>The game's composition, called by every build (desktop Program.cs, browser, Android, iOS).</summary>
    public static void Configure(HostBuilder host) => host.UseGame(() => new EffectsGame());

    /// <inheritdoc/>
    public override GameSettings Settings { get; } = new("ECS Effects") { Background = Color.Hex("1a1c2c") };

    /// <inheritdoc/>
    protected override void Load() => Scenes.Push(new ArenaScene());
}

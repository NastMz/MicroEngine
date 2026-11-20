using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Hosting;
using MicroEngine.Physics.Box2D;

namespace PhysicsPlayground;

/// <summary>
/// A room of dynamic bodies and joints. The game is an ordinary MicroEngine game; the one line that makes its scenes'
/// physics Box2D is in <see cref="Load"/>.
/// </summary>
public sealed class PlaygroundGame : Game
{
    /// <summary>The game's composition, called by every build.</summary>
    public static void Configure(HostBuilder host) => host.UseGame(() => new PlaygroundGame());

    /// <inheritdoc/>
    public override GameSettings Settings { get; } = new("Physics playground") { Background = Color.Hex("1a1c2c") };

    /// <inheritdoc/>
    protected override void Load()
    {
        #region backend
        // Every scene's physics world is Box2D, in the engine's pixel space: 16 pixels to the metre, y down.
        PhysicsBackend = () => new Box2DPhysicsWorld2D(new Box2DWorldOptions { UnitsPerMeter = 16, YDown = true });
        #endregion
        Scenes.Push(new PlaygroundScene());
    }
}

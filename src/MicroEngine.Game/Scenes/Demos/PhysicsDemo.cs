using MicroEngine.Core.Graphics;
using MicroEngine.Core.Input;
using MicroEngine.Core.Logging;
using MicroEngine.Core.Math;
using MicroEngine.Core.Scenes;

namespace MicroEngine.Game.Scenes.Demos;

/// <summary>
/// Placeholder for Physics & Collisions demo.
/// Will showcase RigidBody, Colliders, and CollisionLayers when implemented.
/// </summary>
public sealed class PhysicsDemo : Scene
{
    private readonly IInputBackend _inputBackend;
    private readonly IRenderBackend _renderBackend;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PhysicsDemo"/> class.
    /// </summary>
    public PhysicsDemo()
        : base("PhysicsDemo")
    {
        _inputBackend = Program.InputBackend;
        _renderBackend = Program.RenderBackend;
        _logger = Program.Logger;
    }

    /// <inheritdoc/>
    public override void OnLoad()
    {
        base.OnLoad();
        _logger.Info("PhysicsDemo", "Physics demo loaded (placeholder)");
    }

    /// <inheritdoc/>
    public override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);

        if (_inputBackend.IsKeyPressed(Key.Escape))
        {
            Program.SceneManager.PopScene();
        }
    }

    /// <inheritdoc/>
    public override void OnRender()
    {
        _renderBackend.Clear(new Color(30, 40, 50, 255));
        _renderBackend.DrawText("Physics Demo - Coming Soon", new Vector2(200, 250), 24, Color.White);
        _renderBackend.DrawText("Will showcase collision layers and physics", new Vector2(220, 300), 16, new Color(180, 180, 180, 255));
        _renderBackend.DrawText("[ESC] Back to Menu", new Vector2(10, 580), 14, new Color(150, 150, 150, 255));
    }

    /// <inheritdoc/>
    public override void OnUnload()
    {
        base.OnUnload();
        _logger.Info("PhysicsDemo", "Physics demo unloaded");
    }
}

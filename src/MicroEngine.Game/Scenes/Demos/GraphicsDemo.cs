using MicroEngine.Core.Graphics;
using MicroEngine.Core.Input;
using MicroEngine.Core.Logging;
using MicroEngine.Core.Math;
using MicroEngine.Core.Scenes;

namespace MicroEngine.Game.Scenes.Demos;

/// <summary>
/// Placeholder for Graphics & Camera demo.
/// Will showcase sprites, camera, and batching when assets are available.
/// </summary>
public sealed class GraphicsDemo : Scene
{
    private readonly IInputBackend _inputBackend;
    private readonly IRenderBackend _renderBackend;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphicsDemo"/> class.
    /// </summary>
    public GraphicsDemo()
        : base("GraphicsDemo")
    {
        _inputBackend = Program.InputBackend;
        _renderBackend = Program.RenderBackend;
        _logger = Program.Logger;
    }

    /// <inheritdoc/>
    public override void OnLoad()
    {
        base.OnLoad();
        _logger.Info("GraphicsDemo", "Graphics demo loaded (placeholder)");
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
        _renderBackend.Clear(new Color(40, 30, 50, 255));
        _renderBackend.DrawText("Graphics Demo - Coming Soon", new Vector2(200, 250), 24, Color.White);
        _renderBackend.DrawText("Requires sprite assets", new Vector2(280, 300), 16, new Color(180, 180, 180, 255));
        _renderBackend.DrawText("[ESC] Back to Menu", new Vector2(10, 580), 14, new Color(150, 150, 150, 255));
    }

    /// <inheritdoc/>
    public override void OnUnload()
    {
        base.OnUnload();
        _logger.Info("GraphicsDemo", "Graphics demo unloaded");
    }
}

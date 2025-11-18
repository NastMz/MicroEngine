using MicroEngine.Core.Graphics;
using MicroEngine.Core.Input;
using MicroEngine.Core.Logging;
using MicroEngine.Core.Math;
using MicroEngine.Core.Scenes;

namespace MicroEngine.Game.Scenes.Demos;

/// <summary>
/// Placeholder for Input Mapping demo.
/// Will showcase InputMap when fully implemented.
/// </summary>
public sealed class InputDemo : Scene
{
    private readonly IInputBackend _inputBackend;
    private readonly IRenderBackend2D _renderBackend;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InputDemo"/> class.
    /// </summary>
    public InputDemo()
        : base("InputDemo")
    {
        _inputBackend = Program.InputBackend;
        _renderBackend = Program.RenderBackend;
        _logger = Program.Logger;
    }

    /// <inheritdoc/>
    public override void OnLoad()
    {
        base.OnLoad();
        _logger.Info("InputDemo", "Input demo loaded (placeholder)");
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
        _renderBackend.Clear(new Color(50, 30, 40, 255));
        _renderBackend.DrawText("Input Mapping Demo - Coming Soon", new Vector2(180, 250), 24, Color.White);
        _renderBackend.DrawText("Will showcase InputMap and key rebinding", new Vector2(220, 300), 16, new Color(180, 180, 180, 255));
        _renderBackend.DrawText("[ESC] Back to Menu", new Vector2(10, 580), 14, new Color(150, 150, 150, 255));
    }

    /// <inheritdoc/>
    public override void OnUnload()
    {
        base.OnUnload();
        _logger.Info("InputDemo", "Input demo unloaded");
    }
}

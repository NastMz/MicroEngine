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
    private IInputBackend _inputBackend = null!;
    private IRenderBackend2D _renderBackend = null!;
    private ILogger _logger = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="InputDemo"/> class.
    /// </summary>
    public InputDemo()
        : base("InputDemo")
    {
    }

    /// <inheritdoc/>
    public override void OnLoad(SceneContext context)
    {
        base.OnLoad(context);
        _inputBackend = context.InputBackend;
        _renderBackend = context.RenderBackend;
        _logger = context.Logger;
        _logger.Info("InputDemo", "Input demo loaded (placeholder)");
    }

    /// <inheritdoc/>
    public override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);

        if (_inputBackend.IsKeyPressed(Key.Escape))
        {
            PopScene();
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

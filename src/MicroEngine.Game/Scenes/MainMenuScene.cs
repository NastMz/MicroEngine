using MicroEngine.Core;
using MicroEngine.Core.Graphics;
using MicroEngine.Core.Input;
using MicroEngine.Core.Logging;
using MicroEngine.Core.Math;
using MicroEngine.Core.Scenes;
using MicroEngine.Game.Scenes.Demos;

namespace MicroEngine.Game.Scenes;

/// <summary>
/// Main menu scene that provides navigation to all demo scenes.
/// </summary>
public sealed class MainMenuScene : Scene
{
    private IInputBackend _inputBackend = null!;
    private IRenderBackend2D _renderBackend = null!;
    private ILogger _logger = null!;

    private const string ENGINE_VERSION = "v0.4.9-alpha";
    private const int MENU_X = 250;
    private const int MENU_Y = 150;
    private const int LINE_HEIGHT = 30;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainMenuScene"/> class.
    /// </summary>
    public MainMenuScene()
        : base("MainMenu")
    {
    }

    /// <inheritdoc/>
    public override void OnLoad(SceneContext context)
    {
        base.OnLoad(context);
        _inputBackend = context.InputBackend;
        _renderBackend = context.RenderBackend;
        _logger = context.Logger;
        _logger.Info("MainMenu", "Main menu loaded");
    }

    /// <inheritdoc/>
    public override void OnUpdate(float deltaTime)
    {
        if (_inputBackend.IsKeyPressed(Key.One))
        {
            LoadDemo<EcsBasicsDemo>();
        }
        else if (_inputBackend.IsKeyPressed(Key.Two))
        {
            LoadDemo<GraphicsDemo>();
        }
        else if (_inputBackend.IsKeyPressed(Key.Three))
        {
            LoadDemo<PhysicsDemo>();
        }
        else if (_inputBackend.IsKeyPressed(Key.Four))
        {
            LoadDemo<InputDemo>();
        }
        else if (_inputBackend.IsKeyPressed(Key.Five))
        {
            LoadDemo<TilemapDemo>();
        }
    }

    /// <inheritdoc/>
    public override void OnRender()
    {
        _renderBackend.Clear(new Color(20, 20, 30, 255));

        var titlePos = new Vector2(MENU_X, MENU_Y);
        _renderBackend.DrawText($"MicroEngine {ENGINE_VERSION}", titlePos, 24, Color.White);

        var subtitlePos = new Vector2(MENU_X, MENU_Y + 30);
        _renderBackend.DrawText("Demo Showcase", subtitlePos, 20, new Color(180, 180, 180, 255));

        var separatorPos = new Vector2(MENU_X - 50, MENU_Y + 70);
        _renderBackend.DrawText("═══════════════════════════════════════", separatorPos, 16, new Color(100, 100, 100, 255));

        var optionY = MENU_Y + 110;
        _renderBackend.DrawText("Select a demo:", new Vector2(MENU_X, optionY), 18, new Color(200, 200, 200, 255));

        optionY += LINE_HEIGHT + 10;
        _renderBackend.DrawText("[1] ECS Basics", new Vector2(MENU_X + 20, optionY), 16, new Color(100, 200, 255, 255));

        optionY += LINE_HEIGHT;
        _renderBackend.DrawText("[2] Graphics & Camera", new Vector2(MENU_X + 20, optionY), 16, new Color(100, 200, 255, 255));

        optionY += LINE_HEIGHT;
        _renderBackend.DrawText("[3] Physics & Collisions", new Vector2(MENU_X + 20, optionY), 16, new Color(100, 200, 255, 255));

        optionY += LINE_HEIGHT;
        _renderBackend.DrawText("[4] Input Mapping", new Vector2(MENU_X + 20, optionY), 16, new Color(100, 200, 255, 255));

        optionY += LINE_HEIGHT;
        _renderBackend.DrawText("[5] Tilemap System", new Vector2(MENU_X + 20, optionY), 16, new Color(100, 200, 255, 255));

        optionY += LINE_HEIGHT + 10;
        _renderBackend.DrawText("[X] Exit (close window)", new Vector2(MENU_X + 20, optionY), 16, new Color(255, 100, 100, 255));

        var bottomSeparatorPos = new Vector2(MENU_X - 50, optionY + 40);
        _renderBackend.DrawText("═══════════════════════════════════════", bottomSeparatorPos, 16, new Color(100, 100, 100, 255));
    }

    /// <inheritdoc/>
    public override void OnUnload()
    {
        _logger.Info("MainMenu", "Main menu unloaded");
    }

    private void LoadDemo<T>() where T : Scene, new()
    {
        var demo = new T();
        PushScene(demo);
    }
}

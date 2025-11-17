using MicroEngine.Core.Logging;
using MicroEngine.Core.Scenes;

namespace MicroEngine.Game.Scenes;

/// <summary>
/// Demo scene that demonstrates the engine capabilities.
/// </summary>
internal sealed class DemoScene : Scene
{
    private const string LOG_CATEGORY = "DemoScene";
    private readonly ILogger _logger;
    private int _fixedUpdateCount;
    private int _updateCount;

    public DemoScene(ILogger logger) : base("DemoScene")
    {
        _logger = logger;
    }

    public override void OnLoad()
    {
        base.OnLoad();
        _logger.Info(LOG_CATEGORY, "Demo scene loaded");
        _fixedUpdateCount = 0;
        _updateCount = 0;
    }

    public override void OnFixedUpdate(float fixedDeltaTime)
    {
        _fixedUpdateCount++;

        if (_fixedUpdateCount % 60 == 0)
        {
            _logger.Debug(LOG_CATEGORY, $"Fixed updates: {_fixedUpdateCount}");
        }
    }

    public override void OnUpdate(float deltaTime)
    {
        _updateCount++;

        if (_updateCount % 60 == 0)
        {
            _logger.Debug(LOG_CATEGORY, $"Updates: {_updateCount}, Delta: {deltaTime:F4}s");
        }
    }

    public override void OnRender()
    {
        // Rendering will be implemented when backends are ready
    }

    public override void OnUnload()
    {
        _logger.Info(LOG_CATEGORY, $"Demo scene unloaded. Fixed updates: {_fixedUpdateCount}, Updates: {_updateCount}");
        base.OnUnload();
    }
}

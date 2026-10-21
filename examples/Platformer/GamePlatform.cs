using MicroEngine.Loop;
using Platformer.Model;
namespace Platformer;

// The game owns forgiving-input cancellation; the wrapped provider retains all native ownership.
public sealed class GamePlatform : IFramePlatform
{
    private readonly IFramePlatform _inner;
    private readonly Action _clear;
    public GamePlatform(IFramePlatform inner, LevelGame game)
    {
        ArgumentNullException.ThrowIfNull(inner); ArgumentNullException.ThrowIfNull(game);
        (_inner, _clear) = (inner, game.ClearJumpBuffer);
    }
    public static GamePlatform ForCampaign(IFramePlatform inner, CampaignGame game)
    {
        ArgumentNullException.ThrowIfNull(game);
        return new GamePlatform(inner, (Action)game.ClearJumpBuffer);
    }
    private GamePlatform(IFramePlatform inner, Action clear)
    {
        ArgumentNullException.ThrowIfNull(inner);
        (_inner, _clear) = (inner, clear);
    }
    public PlatformFrame Poll()
    {
        var frame = _inner.Poll();
        if (!frame.Active || frame.CloseRequested) _clear();
        return frame;
    }
    public void Begin() => _inner.Begin();
    public ICanvas Canvas => _inner.Canvas;
    public void End() => _inner.End();
    public void Dispose() { _clear(); _inner.Dispose(); }
}

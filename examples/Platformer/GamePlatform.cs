using MicroEngine.Loop;
using Platformer.Model;
namespace Platformer;

// The game owns forgiving-input cancellation; the wrapped provider retains all native ownership.
public sealed class GamePlatform : IFramePlatform
{
    private readonly IFramePlatform _inner;
    private readonly LevelGame _game;
    public GamePlatform(IFramePlatform inner, LevelGame game)
    {
        ArgumentNullException.ThrowIfNull(inner); ArgumentNullException.ThrowIfNull(game);
        (_inner, _game) = (inner, game);
    }
    public PlatformFrame Poll()
    {
        var frame = _inner.Poll();
        if (!frame.Active || frame.CloseRequested) _game.ClearJumpBuffer();
        return frame;
    }
    public void Begin() => _inner.Begin();
    public ICanvas Canvas => _inner.Canvas;
    public void End() => _inner.End();
    public void Dispose() { _game.ClearJumpBuffer(); _inner.Dispose(); }
}

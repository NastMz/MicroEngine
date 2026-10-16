using FirstPlayable.Model;
using MicroEngine.Loop;

namespace FirstPlayable;

// The sample owns input forgiveness and its cancellation; the engine owns neither policy.
internal sealed class ControllerPlatform : IFramePlatform
{
    private readonly IFramePlatform _inner;
    private readonly GameModel _controller;

    internal ControllerPlatform(IFramePlatform inner, GameModel controller)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(controller);
        (_inner, _controller) = (inner, controller);
    }

    public PlatformFrame Poll()
    {
        var frame = _inner.Poll();
        if (!frame.Active || frame.CloseRequested) _controller.ClearJumpBuffer();
        return frame;
    }
    public void Begin() => _inner.Begin();
    public ICanvas Canvas => _inner.Canvas;
    public void End() => _inner.End();
    public void Dispose() => _inner.Dispose();
}

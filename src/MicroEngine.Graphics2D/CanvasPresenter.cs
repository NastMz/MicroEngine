using System.Numerics;
using MicroEngine.Graphics.Devices;

namespace MicroEngine.Graphics;

/// <summary>
/// Draws a game's canvas into the window as its <see cref="Presentation"/> says, prescale included
/// (<see cref="Presentation.Prescale"/>): the window cleared to the letterbox colour, the canvas into its frame, and the
/// UI view over both. With a prescale the canvas is first enlarged by a whole factor with nearest sampling into a target
/// this presenter owns, and that enlargement is what the presentation's upscale sampler samples. Platforms built on a
/// graphics device present their frames with it.
/// </summary>
public sealed class CanvasPresenter : IDisposable
{
    private readonly Devices.IGraphicsDevice _device;
    private readonly Renderer2D _batcher;
    private RenderTarget? _enlarged;

    /// <summary>Creates a presenter drawing with <paramref name="device"/>.</summary>
    public CanvasPresenter(Devices.IGraphicsDevice device)
    {
        _device = device ?? throw new ArgumentNullException(nameof(device));
        _batcher = new Renderer2D(device);
    }

    /// <summary>The size of the enlarged canvas the last frame was sampled from, or null when it needed none.</summary>
    public (int Width, int Height)? PrescaledSize { get; private set; }

    /// <summary>
    /// Draws <paramref name="canvas"/> into the window's target: the window cleared to <paramref name="letterbox"/>
    /// (opaque), the canvas copied (its alpha ignored) into <paramref name="frame"/>, then <paramref name="ui"/> (a
    /// premultiplied target of the window's size, or null) over everything. Returns false, drawing nothing, when the
    /// window has no target this frame (minimised); the caller then skips presenting.
    /// </summary>
    /// <param name="presentation">The game's presentation: its upscale sampler and prescale.</param>
    /// <param name="canvas">The canvas the game drew.</param>
    /// <param name="ui">The UI view of the frame, in window pixels, or null when the frame drew none.</param>
    /// <param name="frame">Where the canvas lands in the window, in window pixels (<see cref="Presentation.Fit(Vector2, RectF, int, int)"/>).</param>
    /// <param name="letterbox">The colour of the window around the frame.</param>
    public bool Present(Presentation presentation, RenderTarget canvas, RenderTarget? ui, RectF frame, Color letterbox)
    {
        ArgumentNullException.ThrowIfNull(presentation);
        ArgumentNullException.ThrowIfNull(canvas);
        if (!_device.TryAcquireSurfaceTarget(out var surface)) return false;

        var source = canvas;
        var sampler = presentation.Upscale;
        PrescaledSize = null;
        if (frame.Width > 0 && frame.Height > 0)
        {
            var (px, py) = presentation.PrescaleFor(frame, canvas.Width, canvas.Height, _device.Capabilities.Limits.MaxRenderTargetSize);
            int width = canvas.Width * px, height = canvas.Height * py;
            // An enlargement of exactly the frame's size is the nearest picture: sample the canvas itself with nearest.
            if (width == (int)frame.Width && height == (int)frame.Height && frame.Width == width && frame.Height == height) sampler = SamplerDescription.PointClamp;
            else if (px > 1 || py > 1)
            {
                source = Enlarge(canvas, width, height);
                PrescaledSize = (width, height);
            }
        }

        _batcher.Begin(new BatchSettings(surface, Blend: BlendState.Opaque, Sampler: sampler,
            Clear: PremultipliedColor.FromStraight(letterbox with { A = 255 })));
        if (frame.Width > 0 && frame.Height > 0)
            _batcher.Draw(source.Color!, new RectF(0, 0, source.Width, source.Height),
                Matrix3x2.CreateScale(frame.Width / source.Width, frame.Height / source.Height) * Matrix3x2.CreateTranslation(frame.X, frame.Y), ColorF.White);
        if (ui is not null)
        {
            // The UI view over the window, one UI target pixel per window pixel, premultiplied over the world.
            _batcher.PushState(new RenderState(BlendState.AlphaPremultiplied, SamplerDescription.PointClamp, null));
            _batcher.Draw(ui.Color!, new RectF(0, 0, ui.Width, ui.Height), Matrix3x2.Identity, ColorF.White);
            _batcher.PopState();
        }
        _batcher.End();
        return true;
    }

    private RenderTarget Enlarge(RenderTarget canvas, int width, int height)
    {
        if (_enlarged is null || _enlarged.Width != width || _enlarged.Height != height)
        {
            _enlarged?.Dispose();
            _enlarged = _device.CreateRenderTarget(new RenderTargetDescription(width, height, Name: "canvas-prescaled"));
        }
        _batcher.Begin(new BatchSettings(_enlarged, Blend: BlendState.Opaque, Sampler: SamplerDescription.PointClamp));
        _batcher.Draw(canvas.Color!, new RectF(0, 0, canvas.Width, canvas.Height),
            Matrix3x2.CreateScale((float)width / canvas.Width, (float)height / canvas.Height), ColorF.White);
        _batcher.End();
        return _enlarged;
    }

    /// <summary>Releases the enlarged canvas and the batcher.</summary>
    public void Dispose()
    {
        _enlarged?.Dispose();
        _enlarged = null;
        _batcher.Dispose();
    }
}

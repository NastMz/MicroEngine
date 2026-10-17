using MicroEngine.Loop;

namespace FirstPlayable;

internal sealed class AvatarImage : IDisposable
{
    private static readonly PixelImage Source = new(2, 2, new Rgba[]
    {
        new(70, 200, 210), new(255, 255, 255), new(0, 0, 0, 0), new(24, 28, 40)
    });
    private IImageResource? _image;
    private bool _disposed;

    public void Draw(ICanvas canvas, float x, float y)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!float.IsFinite(x)) throw new ArgumentOutOfRangeException(nameof(x));
        if (!float.IsFinite(y)) throw new ArgumentOutOfRangeException(nameof(y));
        if (canvas is not IImageCanvas images) return;
        _image ??= images.CreateImage(Source);
        images.DrawImage(_image, new(x + 8, y + 14, 8, 8));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _image?.Dispose();
    }
}

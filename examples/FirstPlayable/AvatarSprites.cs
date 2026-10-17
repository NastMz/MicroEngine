using FirstPlayable.Model;
using MicroEngine.Loop;

namespace FirstPlayable;

internal sealed class AvatarSprites : IDisposable
{
    private static readonly PixelImage Source = BuildSheet();
    private readonly AvatarImage _fallback = new();
    private IImageResource? _image;
    private bool _disposed;

    public void Draw(ICanvas canvas, float x, float y, AvatarAnimation animation)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(animation);
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!float.IsFinite(x)) throw new ArgumentOutOfRangeException(nameof(x));
        if (!float.IsFinite(y)) throw new ArgumentOutOfRangeException(nameof(y));
        if (canvas is not IImageRegionCanvas regions)
        {
            AvatarRenderer.Draw(canvas, x, y, animation);
            _fallback.Draw(canvas, x, y);
            return;
        }
        _image ??= regions.CreateImage(Source);
        int pose = animation.Pose switch { AvatarPose.Idle => 0, AvatarPose.Walk => 1 + animation.WalkFrame, _ => 3 };
        int cell = (animation.FacingLeft ? 4 : 0) + pose;
        regions.DrawImageRegion(_image, new(cell * 24, 0, 24, 32), new(x, y, 24, 32));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _fallback.Dispose();
        _image?.Dispose();
    }

    private static PixelImage BuildSheet()
    {
        var canvas = new SheetCanvas();
        for (int cell = 0; cell < 8; cell++)
        {
            var animation = new AvatarAnimation(); int direction = cell < 4 ? 1 : -1;
            animation.Update(TimeSpan.FromTicks(1), direction, cell % 4 != 3);
            if (cell % 4 == 0) animation.Update(TimeSpan.FromTicks(1), 0, true);
            if (cell % 4 == 2) animation.Update(TimeSpan.FromMilliseconds(120), direction, true);
            int x = cell * 24;
            AvatarRenderer.Draw(canvas, x, 0, animation);
            // Source-over for the existing badge: skip its transparent quadrant, retaining the torso.
            canvas.Fill(new(x + 8, 14, 4, 4), new(70, 200, 210));
            canvas.Fill(new(x + 12, 14, 4, 4), new(255, 255, 255));
            canvas.Fill(new(x + 12, 18, 4, 4), new(24, 28, 40));
        }
        return new(192, 32, canvas.Pixels);
    }

    // Private raster authoring for this integer-aligned source art, not an engine rasterizer.
    private sealed class SheetCanvas : ICanvas
    {
        internal readonly Rgba[] Pixels = new Rgba[192 * 32];
        public void Clear(Rgba color) => Array.Fill(Pixels, color);
        public void Fill(Rect rectangle, Rgba color)
        {
            for (int y = (int)rectangle.Y; y < rectangle.Y + rectangle.Height; y++)
            for (int x = (int)rectangle.X; x < rectangle.X + rectangle.Width; x++) Pixels[y * 192 + x] = color;
        }
    }
}

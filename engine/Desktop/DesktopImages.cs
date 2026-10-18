using MicroEngine.Loop;

namespace MicroEngine.Desktop;

public sealed partial class DesktopPlatform
{
    private readonly List<ImageResource> _images = [];

    private IImageResource CreateImage(PixelImage image)
    {
        CheckDrawing();
        FrameApi.Require(image is not null, "Image data is required.");
        if (image!.Width > 256 || image.Height > 256 || _images.Count >= 32)
            throw new NotSupportedException("Desktop supports images up to 256x256 and 32 live image resources.");
        var handle = _api.CreateImage(image)
            ?? throw new InvalidOperationException("Native image creation returned no resource.");
        try
        {
            var resource = new ImageResource(this, handle, image.Width, image.Height);
            _images.Add(resource);
            return resource;
        }
        catch (Exception registration)
        {
            try { _api.ReleaseImage(handle); }
            catch (Exception cleanup) { throw new AggregateException(registration, cleanup); }
            throw;
        }
    }

    private void DrawImage(IImageResource image, Rect destination)
    {
        CheckDrawing();
        FrameApi.Require(image is ImageResource resource && resource.Owner == this && !resource.Released,
            "Image resource must be live and belong to this provider.");
        FrameApi.Require(float.IsFinite(destination.X) && float.IsFinite(destination.Y)
            && float.IsFinite(destination.Width) && float.IsFinite(destination.Height)
            && destination.Width >= 0 && destination.Height >= 0,
            "Image destination must be finite with nonnegative sizes.");
        if (destination.Width == 0 || destination.Height == 0) return;
        _api.DrawImage(((ImageResource)image).Handle, destination);
    }

    private void DrawImageRegion(IImageResource image, Rect source, Rect destination)
    {
        CheckDrawing();
        FrameApi.Require(image is ImageResource resource && resource.Owner == this && !resource.Released,
            "Image resource must be live and belong to this provider.");
        var lease = (ImageResource)image;
        FrameApi.Require(float.IsFinite(source.X) && float.IsFinite(source.Y)
            && float.IsFinite(source.Width) && float.IsFinite(source.Height)
            && source.X % 1 == 0 && source.Y % 1 == 0 && source.Width % 1 == 0 && source.Height % 1 == 0
            && source.X >= 0 && source.Y >= 0 && source.Width > 0 && source.Height > 0
            && source.X < lease.Width && source.Y < lease.Height
            && source.Width <= lease.Width - source.X && source.Height <= lease.Height - source.Y,
            "Source must be a positive integer pixel region within its image.");
        FrameApi.Require(float.IsFinite(destination.X) && float.IsFinite(destination.Y)
            && float.IsFinite(destination.Width) && float.IsFinite(destination.Height)
            && destination.Width >= 0 && destination.Height >= 0,
            "Image destination must be finite with nonnegative sizes.");
        if (destination.Width == 0 || destination.Height == 0) return;
        _api.DrawImageRegion(lease.Handle, source, destination);
    }

    private sealed class ImageResource(DesktopPlatform owner, object handle, int width, int height) : IImageResource
    {
        internal DesktopPlatform Owner => owner;
        internal object Handle => handle;
        internal bool Released { get; private set; }
        public int Width => width;
        public int Height => height;

        public void Dispose()
        {
            owner.CheckThread();
            if (Released) return;
            FrameApi.Require(!owner._drawing, "Live images cannot be disposed during drawing.");
            Release();
        }

        internal void Release()
        {
            Released = true;
            owner._images.Remove(this);
            owner._api.ReleaseImage(handle);
        }
    }
}

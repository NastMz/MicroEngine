using System.Runtime.CompilerServices;
using MicroEngine.Loop;

[assembly: InternalsVisibleTo("Desktop.Contracts")]

namespace MicroEngine.Desktop;

public sealed class DesktopWindowOptions
{
    public int Width { get; }
    public int Height { get; }
    public string Title { get; }
    public int MaxImageDimension { get; } = 256;
    public int MaxImageResources { get; } = 32;
    public DesktopWindowOptions(int width, int height, string title, int maxImageDimension, int maxImageResources) : this(width, height, title)
    {
        if (maxImageDimension is < 1 or > 8192) throw new ArgumentOutOfRangeException(nameof(maxImageDimension));
        if (maxImageResources is < 1 or > 256) throw new ArgumentOutOfRangeException(nameof(maxImageResources));
        MaxImageDimension = maxImageDimension; MaxImageResources = maxImageResources;
    }
    public DesktopWindowOptions(int width, int height, string title)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        (Width, Height, Title) = (width, height, title);
    }
}

internal readonly record struct NativeFrame(TimeSpan Elapsed, bool Active, bool Close, string[] Held, string[] Pressed);
internal interface IDesktopApi
{
    void Init(DesktopWindowOptions options);
    bool Ready { get; }
    NativeFrame Read();
    void Begin();
    void End();
    void Clear(Rgba color);
    void Fill(Rect rectangle, Rgba color);
    void Label(string text, int x, int y, int pixelSize, Rgba color);
    object CreateImage(PixelImage image);
    void DrawImage(object image, Rect destination);
    void DrawImageRegion(object image, Rect source, Rect destination);
    void ReleaseImage(object image);
    void Close();
}

// One instance per process in production; isolated instances let tests exercise poisoned state safely.
internal sealed class WindowReservation
{
    private int _taken;
    internal void Take()
    {
        FrameApi.Require(Interlocked.CompareExchange(ref _taken, 1, 0) == 0,
            "Only one Desktop window may own native state; prior cleanup may be uncertain.");
    }
    internal void Release() => Interlocked.Exchange(ref _taken, 0);
}

public sealed partial class DesktopPlatform : IFramePlatform
{
    private static readonly WindowReservation ProcessReservation = new();
    private readonly IDesktopApi _api;
    private readonly int _maxImageDimension;
    private readonly int _maxImageResources;
    private readonly WindowReservation _reservation;
    private readonly int _owner = Environment.CurrentManagedThreadId;
    private readonly ICanvas _canvas;
    private bool _disposed;
    private bool _drawing;
    private bool _active = true;

    public DesktopPlatform(DesktopWindowOptions options) : this(options, new RaylibApi(), ProcessReservation) { }

    internal DesktopPlatform(DesktopWindowOptions options, IDesktopApi api, WindowReservation reservation)
    {
        ArgumentNullException.ThrowIfNull(options);
        _maxImageDimension = options.MaxImageDimension; _maxImageResources = options.MaxImageResources;
        (_api, _reservation, _canvas) = (api, reservation, new CanvasView(this));
        _reservation.Take();
        try
        {
            _api.Init(options);
            if (!_api.Ready) throw new InvalidOperationException("Native window initialization did not become ready.");
        }
        catch (Exception initialization)
        {
            try { _api.Close(); }
            catch (Exception cleanup) { throw new AggregateException(initialization, cleanup); }
            _reservation.Release();
            throw;
        }
    }

    public PlatformFrame Poll()
    {
        CheckAccess();
        FrameApi.Require(!_drawing, "Poll cannot run during drawing.");
        var frame = _api.Read();
        var elapsed = !frame.Active || frame.Active != _active ? TimeSpan.Zero : frame.Elapsed;
        _active = frame.Active;
        return new(elapsed, frame.Active ? new InputSnapshot(frame.Held, frame.Pressed) : new InputSnapshot([], []), frame.Active, frame.Close);
    }

    public void Begin()
    {
        CheckAccess();
        FrameApi.Require(!_drawing, "Drawing is already active.");
        _api.Begin();
        _drawing = true;
    }

    public ICanvas Canvas { get { CheckDrawing(); return _canvas; } }

    public void End()
    {
        CheckDrawing();
        _drawing = false;
        _api.End();
    }

    public void Dispose()
    {
        CheckThread();
        if (_disposed) return;
        _disposed = true;
        List<Exception> errors = [];
        if (_drawing)
        {
            _drawing = false;
            try { _api.End(); } catch (Exception error) { errors.Add(error); }
        }
        while (_images.Count > 0)
        {
            try { _images[^1].Release(); } catch (Exception error) { errors.Add(error); }
        }
        try { _api.Close(); _reservation.Release(); }
        catch (Exception error) { errors.Add(error); } // Failed close retains the poisoned reservation.
        if (errors.Count == 1)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(errors[0]).Throw();
        if (errors.Count > 1) throw new AggregateException(errors);
    }

    private void CheckThread()
    {
        FrameApi.Require(Environment.CurrentManagedThreadId == _owner,
            "Desktop operations belong to the window's creating thread.");
    }

    private void CheckAccess()
    {
        CheckThread();
        FrameApi.Require(!_disposed, "The Desktop window is disposed.");
    }

    private void CheckDrawing()
    {
        CheckAccess();
        FrameApi.Require(_drawing, "Canvas operations require committed drawing.");
    }

    private sealed class CanvasView(DesktopPlatform owner) : ILabelCanvas, IImageRegionCanvas
    {
        public void Clear(Rgba color) { owner.CheckDrawing(); owner._api.Clear(color); }
        public void Fill(Rect rectangle, Rgba color)
        {
            owner.CheckDrawing();
            FrameApi.Require(float.IsFinite(rectangle.X) && float.IsFinite(rectangle.Y)
                && float.IsFinite(rectangle.Width) && float.IsFinite(rectangle.Height)
                && rectangle.Width >= 0 && rectangle.Height >= 0,
                "Rectangle coordinates must be finite and sizes nonnegative.");
            owner._api.Fill(rectangle, color);
        }
        public void Label(string text, int x, int y, int pixelSize, Rgba color)
        {
            owner.CheckDrawing();
            FrameApi.Require(text is not null && text.Length <= 80
                && text.All(character => character is >= ' ' and <= '~')
                && pixelSize is >= 10 and <= 64,
                "Labels require at most 80 printable ASCII characters and pixel size 10..64.");
            if (!string.IsNullOrEmpty(text)) owner._api.Label(text, x, y, pixelSize, color);
        }
        public IImageResource CreateImage(PixelImage image) => owner.CreateImage(image);
        public void DrawImage(IImageResource image, Rect destination) => owner.DrawImage(image, destination);
        public void DrawImageRegion(IImageResource image, Rect source, Rect destination) => owner.DrawImageRegion(image, source, destination);
    }
}

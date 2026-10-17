using System.Runtime.CompilerServices;
using MicroEngine.Loop;

[assembly: InternalsVisibleTo("Desktop.Contracts")]

namespace MicroEngine.Desktop;

public sealed class DesktopWindowOptions
{
    public int Width { get; }
    public int Height { get; }
    public string Title { get; }
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

public sealed class DesktopPlatform : IFramePlatform
{
    private static readonly WindowReservation ProcessReservation = new();
    private readonly IDesktopApi _api;
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
        Exception? endError = null;
        if (_drawing)
        {
            _drawing = false;
            try { _api.End(); } catch (Exception error) { endError = error; }
        }
        try { _api.Close(); }
        catch (Exception closeError)
        {
            // Keep the process reservation held: native state may still exist.
            if (endError is not null) throw new AggregateException(endError, closeError);
            throw;
        }
        _reservation.Release();
        if (endError is not null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(endError).Throw();
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

    private sealed class CanvasView(DesktopPlatform owner) : ILabelCanvas
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
    }
}

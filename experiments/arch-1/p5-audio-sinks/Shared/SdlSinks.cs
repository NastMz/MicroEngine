using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SDL;
using static SDL.SDL3;

namespace P5.Audio;

/// <summary>
/// The two SDL3 sinks. Both open the default playback device with <c>SDL_OpenAudioDeviceStream</c> (one stream bound
/// to the device, F32 stereo at 48 kHz on our side; SDL converts to the device format).
/// <list type="bullet">
/// <item><b>Pull</b>: a get callback on the stream; SDL calls it on its device thread with the bytes it is missing,
/// and the sink renders exactly that many frames.</item>
/// <item><b>Push</b>: no producer callback; <see cref="IAudioSink.Pump"/> (main thread) tops the stream up to a target. An
/// observer get callback only measures starvation: the bytes SDL needed that were not queued.</item>
/// </list>
/// The callbacks are passed as <c>nint</c> through our own imports: on .NET browser-wasm the interpreter cannot call a
/// P/Invoke whose parameter is a function pointer (P4 finding 2), and the same import works everywhere.
/// </summary>
public abstract unsafe class SdlSink : IAudioSink
{
    protected const int MaxFrames = 1 << 15;
    protected SDL_AudioStream* Stream;
    protected readonly float[] Buffer;
    protected AudioRender? Render;
    private GCHandle _self;
    private long _starvedFrames;
    public long Callbacks;

    protected SdlSink(AudioFormat format)
    {
        Format = format;
        Buffer = new float[MaxFrames * format.Channels];
    }

    public AudioFormat Format { get; }
    public abstract AudioSinkDriver Driver { get; }
    public int DevicePeriodFrames { get; private set; }
    public int DeviceRate { get; private set; }
    public virtual int TargetBufferedFrames => 0;
    public long StarvedFrames => Interlocked.Read(ref _starvedFrames);
    private long _putFrames;
    /// <summary>Frames handed to SDL minus frames still queued in the stream.</summary>
    public long PlayedFrames => Interlocked.Read(ref _putFrames) - QueuedFrames;
    protected void Put(Span<float> span)
    {
        fixed (float* p = span) SDL_PutAudioStreamData(Stream, (IntPtr)p, span.Length * sizeof(float));
        Interlocked.Add(ref _putFrames, span.Length / Format.Channels);
    }
    /// <summary>
    /// Push sinks only: also install the starvation observer. It is managed code on SDL's device thread, so a garbage
    /// collection suspends that thread while it runs; off by default so the measured path stays native (P5_OBSERVE=1).
    /// </summary>
    public static bool Observe => Environment.GetEnvironmentVariable("P5_OBSERVE") == "1";
    public string? Error { get; protected set; }
    public int FrameBytes => Format.Channels * sizeof(float);

    [DllImport("SDL3", EntryPoint = "SDL_OpenAudioDeviceStream")]
    private static extern SDL_AudioStream* OpenStream(uint device, SDL_AudioSpec* spec, nint callback, nint userdata);
    [DllImport("SDL3", EntryPoint = "SDL_SetAudioStreamGetCallback")]
    private static extern byte SetGetCallback(SDL_AudioStream* stream, nint callback, nint userdata);

    /// <summary>Opens the device stream; <paramref name="callback"/> is the producer (pull) or the observer (push).</summary>
    protected bool Open(delegate* unmanaged[Cdecl]<nint, SDL_AudioStream*, int, int, void> callback, bool asProducer)
    {
        if (!SDL_InitSubSystem(SDL_InitFlags.SDL_INIT_AUDIO)) { Error = "SDL_InitSubSystem: " + SDL_GetError(); return false; }
        _self = GCHandle.Alloc(this);
        var spec = new SDL_AudioSpec { format = SDL_AudioFormat.SDL_AUDIO_F32LE, channels = Format.Channels, freq = Format.SampleRate };
        Stream = OpenStream(0xFFFFFFFFu /* SDL_AUDIO_DEVICE_DEFAULT_PLAYBACK */, &spec,
            asProducer ? (nint)callback : 0, GCHandle.ToIntPtr(_self));
        if (Stream == null) { Error = "SDL_OpenAudioDeviceStream: " + SDL_GetError(); return false; }
        if (!asProducer && Observe) SetGetCallback(Stream, (nint)callback, GCHandle.ToIntPtr(_self));
        SDL_AudioSpec device;
        int frames;
        // The device period in our frames (SDL reports it at the device rate, e.g. 882 at 44.1 kHz on Android).
        if (SDL_GetAudioDeviceFormat(SDL_GetAudioStreamDevice(Stream), &device, &frames))
        {
            DeviceRate = device.freq;
            DevicePeriodFrames = (int)((long)frames * Format.SampleRate / Math.Max(1, device.freq));
        }
        return true;
    }

    public void Start(AudioRender render)
    {
        Render = render;
        if (Stream != null) SDL_ResumeAudioStreamDevice(Stream);
    }

    public virtual void Pump() { }

    protected static SdlSink From(nint userdata) => (SdlSink)GCHandle.FromIntPtr(userdata).Target!;
    protected void AddStarved(long frames) => Interlocked.Add(ref _starvedFrames, frames);

    public int QueuedFrames => Stream == null ? 0 : SDL_GetAudioStreamQueued(Stream) / FrameBytes;

    public virtual string Describe() =>
        $"sdl-{(Driver == AudioSinkDriver.AudioThread ? "pull" : "push")} driver={SDL_GetCurrentAudioDriver()} devicePeriod={DevicePeriodFrames} deviceRate={DeviceRate}";

    public void Dispose()
    {
        if (Stream != null) { SDL_DestroyAudioStream(Stream); Stream = null; }
        if (_self.IsAllocated) _self.Free();
    }
}

/// <summary>Pull: SDL's device thread asks for frames; the mixer renders them right there.</summary>
public sealed unsafe class SdlPullSink : SdlSink
{
    private SdlPullSink(AudioFormat format) : base(format) { }
    public override AudioSinkDriver Driver => AudioSinkDriver.AudioThread;

    public static SdlPullSink Create(AudioFormat format)
    {
        var sink = new SdlPullSink(format);
        sink.Open(&OnGet, asProducer: true);
        return sink;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnGet(nint userdata, SDL_AudioStream* stream, int additional, int total)
    {
        var sink = (SdlPullSink)From(userdata);
        Interlocked.Increment(ref sink.Callbacks);
        int frames = Math.Min(additional / sink.FrameBytes, MaxFrames);
        if (frames <= 0 || sink.Render is null) return;
        var span = sink.Buffer.AsSpan(0, frames * sink.Format.Channels);
        sink.Render(span, SDL_GetAudioStreamQueued(stream) / sink.FrameBytes);
        sink.Put(span);
    }
}

/// <summary>Push: the main thread keeps <see cref="TargetBufferedFrames"/> queued in SDL's stream.</summary>
public sealed unsafe class SdlPushSink : SdlSink
{
    private readonly int _target;
    private SdlPushSink(AudioFormat format, int targetFrames) : base(format) => _target = targetFrames;
    public override AudioSinkDriver Driver => AudioSinkDriver.HostPump;
    public override int TargetBufferedFrames => _target;

    public static SdlPushSink Create(AudioFormat format, int targetFrames)
    {
        var sink = new SdlPushSink(format, targetFrames);
        sink.Open(&OnObserve, asProducer: false);
        return sink;
    }

    public override void Pump()
    {
        if (Stream == null || Render is null) return;
        int queued = QueuedFrames;
        int frames = Math.Min(_target - queued, MaxFrames);
        if (frames <= 0) return;
        var span = Buffer.AsSpan(0, frames * Format.Channels);
        Render(span, queued);
        Put(span);
    }

    // Observer only: SDL reports how many bytes it still needs beyond what is queued; that is the underrun.
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnObserve(nint userdata, SDL_AudioStream* stream, int additional, int total)
    {
        var sink = (SdlPushSink)From(userdata);
        Interlocked.Increment(ref sink.Callbacks);
        if (additional > 0) sink.AddStarved(additional / sink.FrameBytes);
    }

    public override string Describe() => base.Describe() + $" target={_target}" + (Observe ? " observer" : "");
}

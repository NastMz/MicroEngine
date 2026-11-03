namespace P5.Audio;

/// <summary>
/// The third schedule: a pumped sink driven by its own managed thread instead of the main loop. The thread tops SDL's
/// stream up to the target every couple of milliseconds; SDL's device thread then reads the queue in native code only,
/// so a pause of managed code (main-thread stall or garbage collection) is absorbed by the queued audio. For the engine
/// this is still the same contract: the sink calls <see cref="AudioRender"/> on a thread it owns.
/// </summary>
public sealed class FeederSink : IAudioSink
{
    private readonly SdlPushSink _inner;
    private Thread? _thread;
    private volatile bool _stop;

    public FeederSink(SdlPushSink inner) => _inner = inner;

    public AudioFormat Format => _inner.Format;
    public AudioSinkDriver Driver => AudioSinkDriver.AudioThread;
    public int DevicePeriodFrames => _inner.DevicePeriodFrames;
    public int TargetBufferedFrames => _inner.TargetBufferedFrames;
    public long StarvedFrames => _inner.StarvedFrames;
    public long PlayedFrames => _inner.PlayedFrames;

    public void Start(AudioRender render)
    {
        _inner.Start(render);
        _thread = new Thread(() =>
        {
            while (!_stop) { _inner.Pump(); Thread.Sleep(2); }
        }) { IsBackground = true, Name = "P5 audio feeder", Priority = ThreadPriority.Highest };
        _thread.Start();
    }

    public void Pump() { }
    public string Describe() => _inner.Describe().Replace("sdl-push", "sdl-feeder");

    public void Dispose()
    {
        _stop = true;
        _thread?.Join();
        _inner.Dispose();
    }
}

using System.Runtime.InteropServices;
using MicroEngine.Audio;
using MicroEngine.Kernel;
using MicroEngine.Platform;
using Rl = Raylib_cs.Raylib;

namespace MicroEngine.Raylib;

/// <summary>
/// raylib audio: the engine's mixer (<see cref="MixerAudioDevice"/>, the same voices, priorities and music on every
/// backend) playing through a raylib audio stream topped up by a feeder thread. Files are decoded by the engine
/// (<see cref="AudioDecoders"/>), not by raylib, so they sound the same as on every other backend.
/// </summary>
internal sealed class RaylibAudioDevice : MixerAudioDevice
{
    private RaylibAudioDevice(IAudioSink sink) : base(sink) { }

    public static IAudioDevice TryCreate() => TryCreate(new ConsoleLog());

    /// <summary>Opens raylib's audio device, or returns a silent device and writes why to <paramref name="log"/> when there is none.</summary>
    public static IAudioDevice TryCreate(ILog log)
    {
        ArgumentNullException.ThrowIfNull(log);
        try
        {
            Rl.InitAudioDevice();
            if (Rl.IsAudioDeviceReady()) return new RaylibAudioDevice(new RaylibFeederSink());
            log.Write(LogLevel.Warning, "audio", "no device available, continuing silently");
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
        {
            log.Write(LogLevel.Warning, "audio", $"{e.Message}");
        }
        return new SilentAudioDevice();
    }
}

/// <summary>
/// The desktop raylib sink: one raylib audio stream (48 kHz stereo float, two buffers of 2048 frames) and a feeder thread
/// that refills each buffer raylib has played. raylib guards its stream buffers with its audio lock, so the feeder never
/// races the game's thread.
/// </summary>
internal sealed unsafe class RaylibFeederSink : IAudioSink
{
    private const int Frames = 2048;
    private readonly Raylib_cs.AudioStream _stream;
    private readonly float[] _buffer = new float[Frames * 2];
    private Thread? _thread;
    private volatile bool _stop;

    public RaylibFeederSink()
    {
        Rl.SetAudioStreamBufferSizeDefault(Frames);
        _stream = Rl.LoadAudioStream(48000, 32, 2);
        Rl.SetAudioStreamBufferSizeDefault(0);
    }

    public AudioFormat Format => new(48000, 2);
    public AudioSinkSchedule Schedule => AudioSinkSchedule.SinkThread;
    public int LatencyFrames => 2 * Frames;

    public void Start(AudioRender render)
    {
        ArgumentNullException.ThrowIfNull(render);
        _thread = new Thread(() =>
        {
            while (!_stop)
            {
                while (!_stop && Rl.IsAudioStreamProcessed(_stream))
                {
                    render(_buffer);
                    fixed (float* data = _buffer) Rl.UpdateAudioStream(_stream, data, Frames);
                }
                Thread.Sleep(2);
            }
        }) { IsBackground = true, Name = "MicroEngine audio feeder", Priority = ThreadPriority.Highest };
        // Two silent buffers queued first, then the feeder keeps them full.
        Rl.PlayAudioStream(_stream);
        _thread.Start();
    }

    public void Pump() { }

    public void Dispose()
    {
        _stop = true;
        _thread?.Join();
        Rl.UnloadAudioStream(_stream);
        Rl.CloseAudioDevice();
    }
}

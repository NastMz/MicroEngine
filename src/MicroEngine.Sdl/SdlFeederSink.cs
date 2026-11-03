using MicroEngine.Platform;
using SDL;
using static SDL.SDL3;

namespace MicroEngine.Sdl;

/// <summary>
/// The default SDL audio sink (desktop, Android, iOS): a thread the sink owns keeps a target amount of audio queued in an
/// SDL audio stream bound to the playback device, and SDL's device thread only reads that queue in native code. A pause
/// of managed code shorter than the target (a long frame, a garbage collection) is not heard.
/// </summary>
/// <remarks>Schedule <see cref="AudioSinkSchedule.SinkThread"/>: the feeder renders, <see cref="Pump"/> does nothing.</remarks>
internal sealed unsafe class SdlFeederSink : IAudioSink
{
    private const int Chunk = 256;
    private readonly SDL_AudioDeviceID _device;
    private readonly SDL_AudioStream* _stream;
    private readonly int _targetBytes;
    private readonly float[] _buffer;
    private Thread? _thread;
    private volatile bool _stop;

    /// <param name="device">An open playback device; the sink closes it when disposed.</param>
    /// <param name="format">The format the mixer renders (SDL converts it to the device's).</param>
    /// <param name="targetMilliseconds">Audio kept queued: longer survives longer pauses, shorter sounds sooner.</param>
    public SdlFeederSink(SDL_AudioDeviceID device, AudioFormat format, double targetMilliseconds)
    {
        _device = device;
        Format = format;
        var spec = new SDL_AudioSpec { format = SDL_AudioFormat.SDL_AUDIO_F32LE, channels = format.Channels, freq = format.SampleRate };
        _stream = SDL_CreateAudioStream(&spec, null);
        if (_stream == null || !SDL_BindAudioStream(device, _stream)) throw new InvalidOperationException($"SDL audio stream: {SDL_GetError()}");
        int target = Math.Max(Chunk, (int)(targetMilliseconds / 1000 * format.SampleRate));
        _targetBytes = target * format.Channels * sizeof(float);
        int period = 0;
        SDL_AudioSpec deviceSpec;
        if (SDL_GetAudioDeviceFormat(device, &deviceSpec, &period) && deviceSpec.freq > 0) period = (int)((long)period * format.SampleRate / deviceSpec.freq);
        LatencyFrames = target + Math.Max(0, period);
        _buffer = new float[Chunk * format.Channels];
    }

    public AudioFormat Format { get; }
    public AudioSinkSchedule Schedule => AudioSinkSchedule.SinkThread;
    public int LatencyFrames { get; }

    public void Start(AudioRender render)
    {
        ArgumentNullException.ThrowIfNull(render);
        _thread = new Thread(() =>
        {
            while (!_stop)
            {
                while (!_stop && SDL_GetAudioStreamQueued(_stream) < _targetBytes)
                {
                    render(_buffer);
                    fixed (float* p = _buffer) SDL_PutAudioStreamData(_stream, (nint)p, _buffer.Length * sizeof(float));
                }
                Thread.Sleep(2);
            }
        }) { IsBackground = true, Name = "MicroEngine audio feeder", Priority = ThreadPriority.Highest };
        _thread.Start();
    }

    public void Pump() { }

    /// <summary>Stops the feeder (render is not called after this returns), then the stream and the device.</summary>
    public void Dispose()
    {
        _stop = true;
        _thread?.Join();
        SDL_DestroyAudioStream(_stream);
        SDL_CloseAudioDevice(_device);
        SDL_QuitSubSystem(SDL_InitFlags.SDL_INIT_AUDIO);
    }
}

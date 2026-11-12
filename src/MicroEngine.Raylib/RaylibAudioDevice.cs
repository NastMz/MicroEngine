using System.Runtime.InteropServices;
using MicroEngine.Audio;
using MicroEngine.Kernel;
using MicroEngine.Platform;
using Rl = Raylib_cs.Raylib;

namespace MicroEngine.Raylib;

/// <summary>
/// raylib audio: the engine's mixer (<see cref="MixerAudioDevice"/>, the same voices, priorities and music on every
/// backend) playing through a raylib audio stream topped up by a feeder thread. raylib also decodes MP3 music here (to
/// memory, when it is loaded).
/// </summary>
internal sealed class RaylibAudioDevice : MixerAudioDevice
{
    private RaylibAudioDevice(IAudioSink sink) : base(sink) => AddDecoder("mp3", DecodeMp3);

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

    /// <summary>MP3 through raylib's decoder, whole, into memory; streamed to the mixer from there.</summary>
    private static unsafe IAudioStream DecodeMp3(byte[] encoded)
    {
        var type = stackalloc sbyte[] { (sbyte)'.', (sbyte)'m', (sbyte)'p', (sbyte)'3', 0 };
        Raylib_cs.Wave wave;
        fixed (byte* data = encoded) wave = Rl.LoadWaveFromMemory(type, data, encoded.Length);
        if (wave.FrameCount == 0 || wave.Data == null) throw new InvalidDataException("raylib could not decode it as mp3.");
        try
        {
            Rl.WaveFormat(ref wave, (int)wave.SampleRate, 32, (int)Math.Min(2, wave.Channels));
            var samples = new float[wave.FrameCount * wave.Channels];
            Marshal.Copy((nint)wave.Data, samples, 0, samples.Length);
            return new ClipStream(new AudioClip(samples, (int)wave.SampleRate, (int)wave.Channels));
        }
        finally { Rl.UnloadWave(wave); }
    }

    /// <summary>A decoded clip read as a stream.</summary>
    private sealed class ClipStream(AudioClip clip) : IAudioStream
    {
        private int _position;
        public int Channels => clip.Channels;
        public int SampleRate => clip.SampleRate;
        public int Read(Span<float> buffer)
        {
            int n = Math.Min(buffer.Length / Channels * Channels, clip.Samples.Length - _position);
            clip.Samples.AsSpan(_position, n).CopyTo(buffer);
            _position += n;
            return n;
        }
        public void Rewind() => _position = 0;
        public void Dispose() { }
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

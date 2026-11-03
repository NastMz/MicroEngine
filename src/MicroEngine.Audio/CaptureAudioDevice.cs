using MicroEngine.Platform;

namespace MicroEngine.Audio;

/// <summary>
/// An audio device that plays nothing aloud and records what would have been heard: the engine's own mixer
/// (<see cref="MixerAudioDevice"/>) rendering into an offline sink, one game step per <see cref="Update"/> (the game calls
/// it once per update), so the same run always produces the same samples. Tests compare its <see cref="Output"/> (waves,
/// durations, silences, amplitudes); the launcher's <c>--audio-capture FILE</c> writes it as a WAV.
/// </summary>
/// <remarks>
/// It is the mixer every backend plays through, with the same voice limit, priorities and stealing, panning, resampling,
/// looping, pause, music volume and decoding (WAV and OGG Vorbis music streams); only the device's own latency and
/// resampling to its hardware rate are left out. Sounds start at the first frame of the step that played them.
/// </remarks>
public sealed class CaptureAudioDevice : MixerAudioDevice
{
    private readonly OfflineAudioSink _offline;

    /// <summary>Creates a capture device.</summary>
    /// <param name="updateRate">Updates per second of the game (<c>GameSettings.UpdateRate</c>); each <see cref="Update"/> captures 1/<paramref name="updateRate"/> seconds.</param>
    /// <param name="sampleRate">Samples per second of the capture.</param>
    /// <exception cref="ArgumentOutOfRangeException">A rate is not positive.</exception>
    public CaptureAudioDevice(int updateRate = 60, int sampleRate = AudioClip.DefaultSampleRate)
        : this(new OfflineAudioSink(new AudioFormat(Positive(sampleRate), 2), Positive(updateRate)), updateRate) { }

    private CaptureAudioDevice(OfflineAudioSink offline, int updateRate)
        : base(new StepSink(offline), new AudioEngineSettings { UpdateRate = updateRate })
    {
        _offline = offline;
        Engine.Deterministic = true;
    }

    private static int Positive(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
        return value;
    }

    /// <summary>Samples per second of <see cref="Output"/>.</summary>
    public int SampleRate => _offline.Format.SampleRate;
    /// <summary>False: nothing reaches a speaker.</summary>
    public override bool IsAvailable => false;
    /// <summary>The sounds played, in order: name, the second of the capture it started at, volume, pitch and pan.</summary>
    public List<(string Name, double Time, float Volume, float Pitch, float Pan)> Played { get; } = [];
    /// <summary>Seconds captured so far.</summary>
    public double Duration => _offline.Frames / (double)SampleRate;
    /// <summary>Everything captured so far, as an interleaved stereo clip (samples clamped to -1..1).</summary>
    public AudioClip Output => new([.. _offline.Samples.Select(s => Math.Clamp(s, -1f, 1f))], SampleRate, 2);

    /// <summary>Plays the sound and records it in <see cref="Played"/>.</summary>
    public override void Play(SoundHandle sound, float volume = 1, float pitch = 1, float pan = 0.5f)
    {
        Played.Add((sound.Name, Duration, volume, pitch, pan));
        base.Play(sound, volume, pitch, pan);
    }

    /// <summary>Captures the next game step (1/updateRate seconds) of sound.</summary>
    public override void Update()
    {
        base.Update();
        Engine.Pump();
        _offline.Pump();
    }

    /// <summary>Decodes streamed music ahead; the capture itself advances only with <see cref="Update"/>.</summary>
    public override void Pump() => Engine.Pump();

    /// <summary>The offline sink seen by the mixer: it renders only when the capture advances a step.</summary>
    private sealed class StepSink(OfflineAudioSink inner) : IAudioSink
    {
        public AudioFormat Format => inner.Format;
        public AudioSinkSchedule Schedule => AudioSinkSchedule.HostPump;
        public int LatencyFrames => 0;
        public void Start(AudioRender render) => inner.Start(render);
        public void Pump() { }
        public void Dispose() => inner.Dispose();
    }
}

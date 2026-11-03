// ARCH-1 P5 prototype: the IAudioSink shape under test. Not engine code; the engine is not changed.
namespace P5.Audio;

/// <summary>Interleaved 32-bit float PCM at <see cref="SampleRate"/> with <see cref="Channels"/> samples per frame.</summary>
public readonly record struct AudioFormat(int SampleRate, int Channels)
{
    public double FramesToMs(double frames) => frames * 1000.0 / SampleRate;
}

/// <summary>
/// Fills the whole buffer with interleaved frames (<c>buffer.Length / Channels</c> frames). <paramref name="aheadFrames"/>
/// is how many frames are already queued in front of the first frame of this buffer (sink queue, not the device period),
/// so the mixer can tell when a sound will be heard. Never called concurrently; must not block, allocate or throw.
/// </summary>
public delegate void AudioRender(Span<float> buffer, int aheadFrames);

/// <summary>Who calls <see cref="AudioRender"/>.</summary>
public enum AudioSinkDriver
{
    /// <summary>The sink's own thread (desktop and phone audio threads) calls it when the device needs frames; <see cref="IAudioSink.Pump"/> does nothing.</summary>
    AudioThread,
    /// <summary>The host calls <see cref="IAudioSink.Pump"/> once per frame on the main thread, which renders up to the sink's buffered target (browsers, offline rendering).</summary>
    HostPump,
}

/// <summary>
/// The contract under test: one pull callback for every sink. Thread-driven sinks call it on their audio thread; pumped
/// sinks call it from <see cref="Pump"/>, so the mixer is written once and never knows which one it serves.
/// </summary>
public interface IAudioSink : IDisposable
{
    AudioFormat Format { get; }
    AudioSinkDriver Driver { get; }
    /// <summary>Frames the device consumes per period (its own buffer, latency the sink cannot remove).</summary>
    int DevicePeriodFrames { get; }
    /// <summary>Frames a pumped sink keeps queued ahead of the device (0 for thread-driven sinks).</summary>
    int TargetBufferedFrames { get; }
    void Start(AudioRender render);
    /// <summary>Pumped sinks: render until <see cref="TargetBufferedFrames"/> are queued. Thread-driven sinks: no-op.</summary>
    void Pump();
    /// <summary>Frames the device asked for but the sink could not provide (underrun), when the sink can tell.</summary>
    long StarvedFrames { get; }
    /// <summary>Frames the device has taken from this sink so far (rendered minus still queued): the playback clock.</summary>
    long PlayedFrames { get; }
    string Describe();
}

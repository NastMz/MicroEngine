namespace MicroEngine.Platform;

/// <summary>
/// The PCM format a sink plays: interleaved 32-bit float samples, <see cref="Channels"/> per frame, at
/// <see cref="SampleRate"/> frames per second.
/// </summary>
/// <param name="SampleRate">Frames per second.</param>
/// <param name="Channels">Samples per frame (2 for stereo).</param>
public readonly record struct AudioFormat(int SampleRate, int Channels);

/// <summary>
/// Fills all of <paramref name="buffer"/> (<c>buffer.Length / Channels</c> frames of the sink's
/// <see cref="AudioFormat"/>). Called by one thread at a time; which thread depends on
/// <see cref="IAudioSink.Schedule"/>. It never blocks, allocates, takes a lock the main thread holds, or throws.
/// </summary>
/// <param name="buffer">The interleaved samples to write.</param>
public delegate void AudioRender(Span<float> buffer);

/// <summary>Who calls a sink's <see cref="AudioRender"/>.</summary>
public enum AudioSinkSchedule
{
    /// <summary>
    /// The sink calls <see cref="AudioRender"/> on a thread it owns, and <see cref="IAudioSink.Pump"/> does nothing
    /// (SDL3 platforms: a feeder thread keeping 60 ms queued in SDL's audio stream).
    /// </summary>
    SinkThread,
    /// <summary>
    /// The host calls <see cref="IAudioSink.Pump"/> once per frame on the main thread, which calls
    /// <see cref="AudioRender"/> (browsers: an AudioWorklet queue filled up to 100 ms ahead; tests: an offline sink
    /// rendering exactly one simulation step per pump).
    /// </summary>
    HostPump,
}

/// <summary>
/// A PCM audio output, version 1 of the contract: the engine's one mixer renders, the sink plays. The sink chooses the
/// thread that renders (<see cref="Schedule"/>).
/// </summary>
/// <remarks>
/// <para>Threading rules, part of the contract:</para>
/// <list type="number">
/// <item><see cref="AudioRender"/> runs on exactly one thread at a time, the sink's choice (its own thread, the
/// browser's main thread, a test thread); the mixer is written once, thread-agnostic.</item>
/// <item>Game code reaches the mixer only through a single-producer, single-consumer command queue; rendering never
/// takes a lock shared with the main thread.</item>
/// <item>Rendering never allocates and never touches files: sounds and music are decoded from memory already loaded,
/// and streamed music is decoded ahead outside rendering.</item>
/// <item>The host calls <see cref="Pump"/> once per frame after the simulation steps, on every host; it costs nothing on
/// <see cref="AudioSinkSchedule.SinkThread"/> sinks, so the loop does not branch on the platform.</item>
/// <item><see cref="IDisposable.Dispose"/> returns only after the sink has stopped calling the render function.</item>
/// <item>Page-driven hosts start the sink inside the user gesture that starts the game, and suspend audio while the
/// page is hidden.</item>
/// </list>
/// <para>
/// Determinism rule for the engine mixer: a sound starts at an exact output frame, its simulation step plus a fixed
/// delay of at least <see cref="LatencyFrames"/>, so the output does not depend on the block sizes a sink asks for.
/// </para>
/// </remarks>
public interface IAudioSink : IDisposable
{
    /// <summary>The format the sink plays.</summary>
    AudioFormat Format { get; }
    /// <summary>Who calls the render function.</summary>
    AudioSinkSchedule Schedule { get; }
    /// <summary>Frames rendered ahead of the device (queue target plus device period): the mixer's minimum scheduling delay.</summary>
    int LatencyFrames { get; }
    /// <summary>Starts calling <paramref name="render"/>. Page-driven hosts call it inside the user gesture that starts the game.</summary>
    void Start(AudioRender render);
    /// <summary>
    /// <see cref="AudioSinkSchedule.HostPump"/> sinks render until their queue holds its target;
    /// <see cref="AudioSinkSchedule.SinkThread"/> sinks do nothing. Main thread, once per host frame, after the steps.
    /// </summary>
    void Pump();
}

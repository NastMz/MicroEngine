using MicroEngine.Platform;

namespace MicroEngine.Audio;

/// <summary>
/// A sound playing in an <see cref="AudioEngine"/>: a voice slot and the generation of the sound that took it. Once the
/// sound ends, is stopped or is stolen by the voice limit, the handle is stale and calls with it do nothing.
/// <c>default</c> is <see cref="None"/>, which <see cref="AudioEngine.Play(AudioClip, in PlayParameters)"/> also
/// returns for a sound the voice policy refused.
/// </summary>
/// <param name="Index">The voice slot.</param>
/// <param name="Generation">The generation of the sound in the slot; 0 only for <see cref="None"/>.</param>
public readonly record struct VoiceHandle(int Index, uint Generation)
{
    /// <summary>No sound.</summary>
    public static VoiceHandle None => default;
    /// <summary>Whether this is <see cref="None"/>.</summary>
    public bool IsNone => Generation == 0;
}

/// <summary>How a sound plays.</summary>
public readonly record struct PlayParameters
{
    /// <summary>Creates the defaults: full volume, original pitch, centred, once, priority 0, on the "sfx" bus.</summary>
    public PlayParameters() { }
    /// <summary>Volume, 0..1 (clamped); a value that is not a number counts as 1.</summary>
    public float Volume { get; init; } = 1;
    /// <summary>
    /// Playback speed factor, 1 for the original pitch (clamped to 0.01..100; a value that is not a number counts as 1).
    /// Streams play at their own pitch.
    /// </summary>
    public float Pitch { get; init; } = 1;
    /// <summary>
    /// Stereo position, 0 (left) to 1 (right); 0.5 is centred, as is a value that is not a number. Balance panning: the
    /// far side is attenuated.
    /// </summary>
    public float Pan { get; init; } = 0.5f;
    /// <summary>Whether the sound restarts when it reaches its end.</summary>
    public bool Loop { get; init; }
    /// <summary>
    /// The voice policy's rank: when every voice is busy, a new sound steals the oldest sound of the lowest priority, if
    /// that priority is not higher than its own; otherwise the new sound is refused.
    /// </summary>
    public int Priority { get; init; }
    /// <summary>The bus the sound plays through ("sfx" by default; the engine also has "music" and "master").</summary>
    public string Bus { get; init; } = AudioEngine.SfxBus;
    /// <summary>
    /// Seconds the sound takes to rise from silence to <see cref="Volume"/>, frame by frame (0, the default: it starts
    /// at its volume). A value that is not a number counts as 0.
    /// </summary>
    public float FadeIn { get; init; }
    /// <summary>
    /// Where in the sound it starts, in seconds from its beginning (0, the default); clamped to its length. A value
    /// that is not a number, or negative, counts as 0. A stream starts there only when it can seek
    /// (<see cref="ISeekableAudioStream"/>); another stream starts at its beginning.
    /// </summary>
    public double StartAt { get; init; }
    /// <summary>
    /// Seconds from the current step's frame to the sound's first frame (0, the default: it starts with the step),
    /// exact to the output frame: a sound scheduled on a beat. A value that is not a number, or negative, counts as 0.
    /// </summary>
    public double Delay { get; init; }
    /// <summary>
    /// With <see cref="Loop"/>, the part of the sound that repeats: it plays from where it starts to the region's end,
    /// then goes back to the region's start each time (an intro played once, then a loop). Null (the default) for the
    /// region the sound carries (<see cref="AudioClip.LoopRegion"/>, a stream's
    /// <see cref="ISeekableAudioStream.LoopRegion"/>), and the whole sound when it carries none.
    /// </summary>
    public LoopRegion? LoopRegion { get; init; }
}

/// <summary>
/// The part of a sound that repeats when it loops, in frames of the sound itself (not of the output):
/// <see cref="Start"/> is the first frame of the loop and <see cref="End"/> the frame after its last. What comes before
/// <see cref="Start"/> plays once.
/// </summary>
/// <param name="Start">The first frame of the loop; negative counts as 0.</param>
/// <param name="End">The frame after the loop's last one; 0, or anything not after <paramref name="Start"/> or past the sound's end, means the sound's end.</param>
public readonly record struct LoopRegion(long Start, long End = 0)
{
    /// <summary>A region given in seconds of a sound sampled at <paramref name="sampleRate"/>; an end of 0 or less means the sound's end. Values that are not numbers count as 0.</summary>
    public static LoopRegion FromSeconds(double start, double end, int sampleRate) =>
        new(ToFrames(start, sampleRate), ToFrames(end, sampleRate));

    private static long ToFrames(double seconds, int rate) =>
        seconds > 0 && rate > 0 ? (long)Math.Min(Math.Round(seconds * rate), int.MaxValue) : 0;

    /// <summary>
    /// The region within a sound of <paramref name="frames"/> frames: the whole sound when the region is empty or
    /// starts past its end. For a sound of unknown length (0 frames) an end of 0 stays "the end".
    /// </summary>
    internal (long Start, long End) Within(long frames)
    {
        long start = Math.Clamp(Start, 0, int.MaxValue), end = Math.Clamp(End, 0, int.MaxValue);
        if (frames > 0)
        {
            if (end <= start || end > frames) end = frames;
            if (start >= end) start = 0;
        }

        else if (end <= start) end = 0;
        return (start, end);
    }
}

/// <summary>Settings of an <see cref="AudioEngine"/>.</summary>
public sealed record AudioEngineSettings
{
    /// <summary>Sounds that can play at once (32 by default); the voice policy decides beyond that.</summary>
    public int MaxVoices { get; init; } = 32;
    /// <summary>Simulation steps per second: the engine stamps each sound with the output frame of its step.</summary>
    public int UpdateRate { get; init; } = 60;
    /// <summary>
    /// Frames between a step and the sounds it starts; raised to the sink's <see cref="IAudioSink.LatencyFrames"/>, so
    /// every sound starts at an exact frame whatever block sizes the sink renders. Null for the sink's latency.
    /// </summary>
    public int? DelayFrames { get; init; }
    /// <summary>Output frames of streamed music decoded ahead (by <see cref="AudioEngine.Pump"/>); half a second by default.</summary>
    public double StreamAheadSeconds { get; init; } = 0.5;
    /// <summary>
    /// Seconds of a compressed stream decoded and dropped per <see cref="AudioEngine.Pump"/> on the way to a place
    /// sought (0.25 by default): a seek further than this is spread over several host frames instead of holding one
    /// up (<see cref="AudioEngine.IsSeeking"/>).
    /// </summary>
    public double StreamSeekSeconds { get; init; } = 0.25;
    /// <summary>
    /// Whether the limiter after "master" (<see cref="AudioEngine.Limiter"/>) starts switched on: a mix that goes over
    /// full scale is then turned down instead of clipped, and a mix within full scale is not touched. Null (the
    /// default) leaves it to whoever creates the mixer: the devices games play through
    /// (<see cref="MixerAudioDevice"/>) switch it on; an <see cref="AudioEngine"/> created directly renders exactly what
    /// it is told and leaves it off.
    /// </summary>
    public bool? Limiter { get; init; }
}

/// <summary>
/// Streamed audio (music decoded while it plays). The engine reads it only in <see cref="AudioEngine.Pump"/>, on the main
/// thread outside the simulation step, ahead of the sink.
/// </summary>
public interface IAudioStream : IDisposable
{
    /// <summary>1 or 2.</summary>
    int Channels { get; }
    /// <summary>Frames per second.</summary>
    int SampleRate { get; }
    /// <summary>Fills <paramref name="buffer"/> with interleaved samples; returns how many it wrote, 0 at the end.</summary>
    int Read(Span<float> buffer);
    /// <summary>Goes back to the start (looping).</summary>
    void Rewind();
}

/// <summary>
/// A stream that knows its length and can be read from any frame: the mixer can then start it anywhere
/// (<see cref="PlayParameters.StartAt"/>), move it while it plays (<see cref="AudioEngine.Seek"/>), repeat a part of it
/// (<see cref="LoopRegion"/>) and tell how long it is. Like <see cref="IAudioStream.Read"/>, <see cref="Seek"/> is called
/// on the main thread only.
/// </summary>
public interface ISeekableAudioStream : IAudioStream
{
    /// <summary>The stream's length in frames; 0 when it is not known.</summary>
    long Frames { get; }
    /// <summary>Makes <paramref name="frame"/> (0 to <see cref="Frames"/>) the next frame <see cref="IAudioStream.Read"/> gives.</summary>
    void Seek(long frame);
    /// <summary>The loop region the file itself names (its metadata), or null.</summary>
    LoopRegion? LoopRegion => null;
}

/// <summary>
/// A named mix stage: the sounds and buses under it are mixed together, pass through its effects
/// (<see cref="Add"/>) and are scaled by its <see cref="Volume"/>. Buses form a tree under "master": a bus is created
/// under a parent that already exists and is never moved, so there is no cycle.
/// </summary>
/// <remarks>
/// A bus without effects costs nothing: its sounds are added straight into the bus above it. With an effect, the bus is
/// mixed into a buffer of its own first (a submix), processed there, scaled by its volume and added to the bus above.
/// </remarks>
public sealed class AudioBus
{
    private readonly List<AudioEffect> _effects = [];
    private float _volume = 1;

    internal AudioBus(AudioEngine? engine, List<AudioBus> siblings, int index, string name, AudioBus? parent) =>
        (Engine, Siblings, Index, Name, Parent) = (engine, siblings, index, name, parent);

    /// <summary>The mixer that renders the bus; null for the buses of a device without one (headless), which only keep what they are told.</summary>
    internal AudioEngine? Engine { get; }
    /// <summary>Every bus of the same mixer, this one included.</summary>
    internal List<AudioBus> Siblings { get; }
    internal int Index { get; }
    /// <summary>The bus's name.</summary>
    public string Name { get; }
    /// <summary>The bus it plays into; null for "master".</summary>
    public AudioBus? Parent { get; }
    /// <summary>
    /// Gain of everything on the bus, clamped to 0..4; it changes at the frame of the current step, at once (use
    /// <see cref="FadeTo"/> for a change without a step in the sound). A value that is not a number is ignored.
    /// </summary>
    public float Volume
    {
        get => _volume;
        set
        {
            if (float.IsNaN(value)) return;
            _volume = Math.Clamp(value, 0, 4);
            Engine?.SetBusVolume(Index, _volume, 0);
        }
    }

    /// <summary>
    /// Moves <see cref="Volume"/> to <paramref name="volume"/> (clamped to 0..4) over <paramref name="seconds"/>, frame
    /// by frame from the current step's frame; <see cref="Volume"/> reads the new value at once. A volume that is not a
    /// number is ignored; 0 seconds or less changes it at once.
    /// </summary>
    public void FadeTo(float volume, float seconds)
    {
        if (float.IsNaN(volume)) return;
        _volume = Math.Clamp(volume, 0, 4);
        Engine?.SetBusVolume(Index, _volume, float.IsNaN(seconds) ? 0 : seconds);
    }

    /// <summary>The bus's effects, in the order the mix passes through them.</summary>
    public IReadOnlyList<AudioEffect> Effects => _effects;

    /// <summary>Adds <paramref name="effect"/> at the end of the chain, from the current step's frame; returns the bus, to add another.</summary>
    /// <exception cref="InvalidOperationException">The effect is already on a bus, or has played on another mixer.</exception>
    /// <exception cref="ArgumentException">The effect listens to a bus that does not exist, to this bus or to one this bus plays into.</exception>
    public AudioBus Add(AudioEffect effect) => Insert(_effects.Count, effect);

    /// <summary>Adds <paramref name="effect"/> at <paramref name="index"/> of the chain (0: first), from the current step's frame.</summary>
    /// <exception cref="InvalidOperationException">The effect is already on a bus, or has played on another mixer.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not in 0..<c>Effects.Count</c>.</exception>
    /// <exception cref="ArgumentException">The effect listens to a bus that does not exist, to this bus or to one this bus plays into.</exception>
    public AudioBus Insert(int index, AudioEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, _effects.Count);
        if (effect.Bus is not null) throw new InvalidOperationException($"The effect is already on bus '{effect.Bus.Name}'; remove it from that bus first.");
        // The mixer that prepared an effect may still be rendering it after it left its bus (until the chain without
        // it reaches its frame): another mixer's thread must never process it at the same time.
        if (Engine is not null && effect.Owner is not null && effect.Owner != Engine)
            throw new InvalidOperationException("The effect has played on another mixer, which may still be processing it; create a new effect for this one.");
        _effects.Insert(index, effect);
        effect.Bus = this;
        try { GraphChanged(); }
        catch
        {
            _effects.RemoveAt(index);
            effect.Bus = null;
            throw;
        }
        return this;
    }

    /// <summary>Takes <paramref name="effect"/> out of the chain, from the current step's frame (what it still held, a reverb's tail, is not heard).</summary>
    /// <returns>Whether it was on this bus.</returns>
    public bool Remove(AudioEffect effect)
    {
        if (effect is null || !_effects.Remove(effect)) return false;
        effect.Bus = null;
        Engine?.Forget(effect);
        GraphChanged();
        return true;
    }

    /// <summary>Takes every effect out of the chain.</summary>
    public void ClearEffects()
    {
        if (_effects.Count == 0) return;
        foreach (var effect in _effects)
        {
            effect.Bus = null;
            Engine?.Forget(effect);
        }
        _effects.Clear();
        GraphChanged();
    }

    /// <summary>Checks the buses' effects (the buses they listen to) and tells the mixer, if any, that the chains changed.</summary>
    /// <exception cref="ArgumentException">An effect listens to a bus that does not exist, to its own bus or to one its bus plays into, or two buses listen to each other.</exception>
    internal void GraphChanged()
    {
        int[] order = AudioEngine.Order(Siblings);
        Engine?.PostGraph(order);
    }
}

/// <summary>
/// The engine's one mixer: game code plays sounds here on the main thread, a sink renders them. Every backend plays
/// through it, so voices, priorities, stealing, buses and fades behave the same everywhere, and an offline sink renders
/// exactly what a device would play.
/// </summary>
/// <remarks>
/// <para>
/// Time is counted in simulation steps (<see cref="Advance"/>, once per fixed step). A sound starts at an exact output
/// frame: its step's frame plus <see cref="DelayFrames"/> (at least the sink's latency), so the output does not depend
/// on the block sizes the sink asks for. The voice policy runs on the main thread, against that timeline, never against
/// what the sink has rendered: which sound plays is the same on every backend and in every run.
/// </para>
/// <para>
/// In real time (not <see cref="Deterministic"/>) the steps' frames follow what the device is playing: what the sink
/// has rendered, less its latency. A step's sounds therefore start in the frames the sink renders next, one step
/// after the previous step's, and are heard one sink latency after their update. A step that runs late moves the
/// timeline forward to what can still be rendered; after a slow frame, whose catch-up steps leave it ahead, it comes
/// back by a sixteenth of a step per host frame until it is within one step again. Nothing moves it back: sounds of
/// consecutive steps keep their order. A sink that renders nothing for a quarter of a second of steps, over several
/// host frames, has stopped: the timeline goes on without it and the frames it did not play are skipped
/// (<see cref="SkippedFrames"/>), so when it renders again it plays what the game is at, not what it missed.
/// </para>
/// <para>
/// Commands reach the render function through a single-producer, single-consumer queue; rendering never allocates,
/// locks or reads files. Music streams are decoded ahead in <see cref="Pump"/>, outside the simulation step.
/// </para>
/// </remarks>
public sealed class AudioEngine : IDisposable
{
    /// <summary>The bus every other bus plays into.</summary>
    public const string MasterBus = "master";
    /// <summary>The default bus of sounds.</summary>
    public const string SfxBus = "sfx";
    /// <summary>The bus music plays through.</summary>
    public const string MusicBus = "music";
    private const int MaxBuses = 32, QueueSize = 4096;
    /// <summary>Changes to one sound, or volumes of one bus, that can wait for their frames at once, each for its own.</summary>
    private const int WaitingChanges = 8;
    /// <summary>Changes to effects and bus fades that can wait for their frames at once; beyond that the oldest take effect early.</summary>
    private const int WaitingEvents = 256;
    /// <summary>The most frames mixed at once: the length of a bus's submix buffer.</summary>
    private const int ChunkFrames = 1024;

    private readonly IAudioSink _sink;
    private readonly int _channels, _rate, _updateRate;
    private readonly Logical[] _logical;
    private readonly Voice[] _voices, _incoming;
    private readonly bool[] _hasIncoming;
    // The sink's thread: the volumes waiting for their frames, WaitingChanges for each bus, oldest first.
    private readonly Change[] _busWaiting = new Change[MaxBuses * WaitingChanges];
    private readonly int[] _busChanges = new int[MaxBuses];
    // Main thread: buses whose volume the queue had no room for, and how many voices and buses wait to be sent.
    private readonly bool[] _busUnsent = new bool[MaxBuses];
    private int _unsent;
    private readonly Command[] _queue = new Command[QueueSize];
    private int _head, _tail;
    private readonly List<AudioBus> _buses = [];
    private readonly Dictionary<string, AudioBus> _busNames = new(StringComparer.Ordinal);
    private readonly int[] _busParent = new int[MaxBuses];
    private readonly float[] _busVolume = new float[MaxBuses], _busGain = new float[MaxBuses];
    private readonly List<StreamFeed> _feeds = [];
    // The mix graph. Main thread: each bus's submix buffer once it needed one, the effects the render function has been
    // told of, whether the latest chains could not be queued, and the buses whose volume has been faded (their volume
    // then always travels with the fades, in order).
    private readonly float[]?[] _buffers = new float[MaxBuses][];
    private readonly HashSet<AudioEffect> _sentEffects = [];
    private readonly bool[] _busFaded = new bool[MaxBuses];
    private bool _graphUnsent;
    private int _unsentEffects;
    // The sink's thread: the chains it renders, the timed changes waiting for their frames (a ring, oldest first), and
    // for each segment where each bus is mixed: into itself (a submix) or into the nearest submix above it.
    private Graph _graph = Graph.Empty;
    private readonly Event[] _events = new Event[WaitingEvents];
    private int _eventHead, _eventCount;
    private readonly bool[] _busSubmix = new bool[MaxBuses];
    private readonly int[] _busDestination = new int[MaxBuses];
    private readonly float[] _busAbove = new float[MaxBuses], _busFadeTarget = new float[MaxBuses], _busFadeStep = new float[MaxBuses];
    private readonly long[] _busFadeLeft = new long[MaxBuses];
    private long _step, _offset, _played, _position;
    // Frames the output did not play: the main thread adds to _skipped, the sink's thread follows it with _skew.
    private long _skipped, _skew;
    // The largest block the sink has asked for (written by its thread): a sink is not taken as stopped between two blocks.
    private int _largestBlock;
    // Real time: the sink's latency, the smallest lead of the timeline over the device in the steps of the current host
    // frame, whether a host frame ended since, and when the sink last rendered anything: what it had rendered, the step
    // that saw it, and the host frames ended since.
    private readonly int _sinkLatency;
    private readonly double _seekSeconds;
    private bool _pumped;
    private long _frameLead = long.MaxValue, _progressRendered = -1, _progressStep;
    private int _progressPumps;
    private bool _disposed;
    // RenderAllocations: written by the rendering thread only.
    private readonly bool _sinkThread;
    private long _renders, _renderBytes, _renderEnd;
    private int _renderThread = -1;

    /// <summary>Creates the mixer and starts <paramref name="sink"/> rendering it.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The sink's format is not 1 or 2 channels at a positive rate, or a setting is out of range.</exception>
    public AudioEngine(IAudioSink sink, AudioEngineSettings? settings = null)
    {
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        settings ??= new AudioEngineSettings();
        Format = sink.Format;
        if (Format.Channels is not (1 or 2) || Format.SampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sink), $"Unsupported sink format {Format}.");
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.MaxVoices, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.UpdateRate, 1);
        (_channels, _rate, _updateRate) = (Format.Channels, Format.SampleRate, settings.UpdateRate);
        MaxVoices = settings.MaxVoices;
        _sinkLatency = Math.Max(0, sink.LatencyFrames);
        DelayFrames = Math.Max(_sinkLatency, settings.DelayFrames ?? 0);
        StreamAheadFrames = Math.Max(1024, (int)(settings.StreamAheadSeconds * _rate));
        _seekSeconds = settings.StreamSeekSeconds > 0 ? Math.Min(settings.StreamSeekSeconds, 3600) : 0.25;
        _logical = new Logical[MaxVoices];
        _voices = new Voice[MaxVoices];
        _incoming = new Voice[MaxVoices];
        _hasIncoming = new bool[MaxVoices];
        CreateBus(MasterBus, null);
        CreateBus(SfxBus, _buses[0]);
        CreateBus(MusicBus, _buses[0]);
        // The limiter is not in master's chain: it comes after master's volume, last. It is prepared and given its
        // values here, before the sink renders anything.
        Limiter = new Effects.Limiter { Bypass = settings.Limiter != true };
        Limiter.PrepareFor(_rate, _channels);
        for (int i = 0; i < Limiter.ParameterCount; i++) Limiter.Deliver(i, Limiter.ParameterValue(i));
        Limiter.Bypassed = Limiter.Bypass;
        Limiter.Bus = _buses[0];
        Limiter.Owner = this;
        _sentEffects.Add(Limiter);
        _sinkThread = sink.Schedule == AudioSinkSchedule.SinkThread;
        sink.Start(CountedRender);
    }

    /// <summary>
    /// Renders the sink asked for so far and the bytes the rendering thread allocated, counted exactly on that thread
    /// (<see cref="GC.GetAllocatedBytesForCurrentThread"/>): during each render, and for a
    /// <see cref="AudioSinkSchedule.SinkThread"/> sink also from the end of one render to the start of the next on the
    /// same thread, which is the sink's own loop (an SDL feeder queueing the block and sleeping). Tests read it from any
    /// thread to check that the audio thread makes no garbage.
    /// </summary>
    internal RenderAllocationCount RenderAllocations => new(Volatile.Read(ref _renders), Volatile.Read(ref _renderBytes));

    /// <summary>The render function the sink is given: <see cref="Render"/>, counted (<see cref="RenderAllocations"/>).</summary>
    private void CountedRender(Span<float> buffer)
    {
        long start = GC.GetAllocatedBytesForCurrentThread();
        int thread = Environment.CurrentManagedThreadId;
        long loop = _sinkThread && thread == _renderThread ? start - _renderEnd : 0;
        Render(buffer);
        long end = GC.GetAllocatedBytesForCurrentThread();
        (_renderThread, _renderEnd) = (thread, end);
        Volatile.Write(ref _renderBytes, _renderBytes + loop + (end - start));
        Volatile.Write(ref _renders, _renders + 1);
    }

    /// <summary>The sink's format, which the mixer renders.</summary>
    public AudioFormat Format { get; }
    /// <summary>Sounds that can play at once.</summary>
    public int MaxVoices { get; }
    /// <summary>Frames between a step and the sounds it starts.</summary>
    public int DelayFrames { get; }
    /// <summary>Output frames of each music stream decoded ahead.</summary>
    public int StreamAheadFrames { get; }
    /// <summary>
    /// Whether the timeline follows the simulation only (true: the same sounds in every run, as deterministic runs need)
    /// or also catches up with the sink after the game fell behind it (false, the default for real-time play).
    /// </summary>
    public bool Deterministic { get; set; }
    /// <summary>Simulation steps counted by <see cref="Advance"/>.</summary>
    public long Steps => _step;
    /// <summary>
    /// The output frame the current step's sounds start at (the step's frame plus <see cref="DelayFrames"/>). In real
    /// time, a sound or change moves it forward when the sink has already rendered past it.
    /// </summary>
    public long Now => StepFrame(_step) + _offset + DelayFrames;

    /// <summary>
    /// Real time, at every sound or change: when the call comes later than the timeline expected (the device is already
    /// past the step's frame), the timeline moves forward to the device, so the sound starts in the frames the sink
    /// renders next instead of in frames it has rendered. It is checked at each call, not once per step: a call made
    /// between a frame's last step and the next frame's first one (while drawing) comes a frame before that step's.
    /// </summary>
    private void Anchor()
    {
        if (Deterministic) return;
        long behind = RenderedFrames + _skipped - _sinkLatency - (StepFrame(_step) + _offset);
        if (behind > 0) _offset += behind;
    }
    /// <summary>Sounds playing now on the mixer's timeline.</summary>
    public int ActiveVoices
    {
        get
        {
            int count = 0;
            for (int i = 0; i < _logical.Length; i++) if (Busy(i)) count++;
            return count;
        }
    }
    /// <summary>Sounds the voice policy refused because every voice played something of higher priority.</summary>
    public int RefusedSounds { get; private set; }
    /// <summary>Frames rendered so far (read from any thread).</summary>
    public long RenderedFrames => Volatile.Read(ref _position);
    /// <summary>
    /// Frames the output did not play and the mixer skipped. In real time, a sink that renders nothing during a quarter
    /// of a second of steps (or twice the time of its largest block, if that is longer), over several host frames, has
    /// stopped (a browser holding its audio suspended, a device
    /// that went away): from then on the sounds go on with the game, unheard, and when the sink renders again it joins
    /// them where they are. A sound that ended meanwhile is never heard, one still within its length goes on from where
    /// the game is, loops stay in step; streams, which are not decoded while nobody hears them, go on from where they
    /// stopped. Always 0 in <see cref="Deterministic"/> runs.
    /// </summary>
    public long SkippedFrames => Volatile.Read(ref _skipped);
    /// <summary>The buses, "master", "sfx" and "music" first.</summary>
    public IReadOnlyList<AudioBus> Buses => _buses;

    /// <summary>The first output frame of simulation step <paramref name="step"/>.</summary>
    public long StepFrame(long step) => step * _rate / _updateRate;

    // ---- Buses

    /// <summary>The bus named <paramref name="name"/>.</summary>
    /// <exception cref="KeyNotFoundException">No such bus.</exception>
    public AudioBus Bus(string name) => _busNames.TryGetValue(name, out var bus) ? bus : throw new KeyNotFoundException($"No audio bus is named '{name}'.");

    /// <summary>Creates a bus playing into <paramref name="parent"/>.</summary>
    /// <exception cref="ArgumentException">A bus already has that name.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="parent"/> does not exist.</exception>
    /// <exception cref="InvalidOperationException">The engine has 32 buses already.</exception>
    public AudioBus AddBus(string name, string parent = MasterBus)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (_busNames.ContainsKey(name)) throw new ArgumentException($"An audio bus is already named '{name}'.", nameof(name));
        var up = Bus(parent);
        if (_buses.Count == MaxBuses) throw new InvalidOperationException($"An audio engine has at most {MaxBuses} buses.");
        return CreateBus(name, up);
    }

    private AudioBus CreateBus(string name, AudioBus? parent)
    {
        var bus = new AudioBus(this, _buses, _buses.Count, name, parent);
        // Written here, read by the render function, and handed over by the queue all the same: nothing is mixed
        // through a bus before a command that names it (a sound started on it, its volume, a chain), every such
        // command is posted after these writes, and posting is a volatile write the render function reads before it
        // drains, so it sees them first. Until then the render function only derives per-segment values from the
        // slot, for a bus nothing plays on, and derives them again at the next segment.
        _busParent[bus.Index] = parent?.Index ?? -1;
        _busVolume[bus.Index] = 1;
        _buses.Add(bus);
        _busNames.Add(name, bus);
        return bus;
    }

    /// <summary>
    /// The limiter after "master": it keeps the mix within full scale instead of letting the device clip it, and leaves
    /// every sample as it is while the mix stays within its threshold. <see cref="AudioEngineSettings.Limiter"/> says
    /// whether it starts on (a game's device has it on); <see cref="AudioEffect.Bypass"/> switches it.
    /// </summary>
    public Effects.Limiter Limiter { get; }

    /// <summary>
    /// Streams that ended because their decoder threw while it was read or sought (a damaged file): each plays what it
    /// had decoded, then ends like a sound that is over. Counted once per stream.
    /// </summary>
    public int FailedStreams { get; private set; }

    /// <summary>
    /// Raised once, on the main thread during <see cref="Pump"/> (or the call that sought it), for each stream counted
    /// in <see cref="FailedStreams"/>: the voice that played it (already ended) and what its decoder threw.
    /// </summary>
    public event Action<VoiceHandle, Exception>? StreamFailed;

    private void ReportFailure(VoiceHandle voice, Exception error)
    {
        FailedStreams++;
        StreamFailed?.Invoke(voice, error);
    }

    /// <summary>Effects that threw while rendering and were taken out of the mix (<see cref="AudioEffect.Faulted"/>).</summary>
    public int FaultedEffects => Volatile.Read(ref _faulted);
    private int _faulted;

    internal void SetBusVolume(int bus, float volume, float seconds)
    {
        Anchor();
        long frames = Frames(seconds);
        if (frames > 0 && !_busFaded[bus])
        {
            // A fade is rendered in the bus's own buffer, gain by gain: the render function gets the buffer first.
            _busFaded[bus] = true;
            PostGraph(Order(_buses));
        }
        bool sent = Flush() && TryPost(_busFaded[bus]
            ? new Command { Op = Op.BusFade, Slot = bus, Volume = volume, Fade = frames, Frame = Now }
            : new Command { Op = Op.Bus, Slot = bus, Volume = volume, Frame = Now });
        if (sent) return;
        DroppedCommands++;
        if (!_busUnsent[bus]) _unsent++;
        _busUnsent[bus] = true;
    }

    // ---- Effects (main thread)

    /// <summary>
    /// The order buses are processed in: a bus after the buses that play into it and after the buses its effects
    /// listen to.
    /// </summary>
    /// <exception cref="ArgumentException">An effect listens to a bus that does not exist, to its own bus or to one its bus plays into, or buses listen to each other in a circle.</exception>
    internal static int[] Order(List<AudioBus> buses)
    {
        int count = buses.Count;
        // needs[b]: buses that must be processed before b, as a bit set (at most 32 buses).
        Span<uint> needs = stackalloc uint[count];
        for (int b = 0; b < count; b++)
        {
            var bus = buses[b];
            if (bus.Parent is { } parent) needs[parent.Index] |= 1u << b;
            foreach (var effect in bus.Effects)
            {
                if (effect.Key is not { } name) continue;
                AudioBus? key = null;
                foreach (var other in buses) if (other.Name == name) key = other;
                if (key is null) throw new ArgumentException($"An effect on bus '{bus.Name}' listens to bus '{name}', which does not exist.");
                for (var up = bus; up is not null; up = up.Parent)
                    if (up == key) throw new ArgumentException($"An effect on bus '{bus.Name}' cannot listen to '{name}': '{bus.Name}' plays into it, so its mix is not known yet.");
                needs[b] |= 1u << key.Index;
            }
        }
        var order = new int[count];
        uint done = 0;
        for (int placed = 0; placed < count;)
        {
            int before = placed;
            for (int b = 0; b < count; b++)
                if ((done & 1u << b) == 0 && (needs[b] & ~done) == 0)
                {
                    order[placed++] = b;
                    done |= 1u << b;
                }
            if (placed == before) throw new ArgumentException("Audio buses listen to each other in a circle: an effect cannot listen to a bus that, through its own effects or the buses under it, waits for this one.");
        }
        return order;
    }

    /// <summary>Hands the buses' chains, as they are now, to the render function, from the current step's frame.</summary>
    internal void PostGraph(int[] order)
    {
        Anchor();
        if (Flush()) SendGraph(order);
        else if (!_graphUnsent)
        {
            DroppedCommands++;
            _graphUnsent = true;
            _unsent++;
        }
    }

    /// <summary>Queues the chains as they are now; false, keeping them to send later (<see cref="Flush"/>), when the queue is full.</summary>
    private bool SendGraph(int[] order)
    {
        var graph = new Graph { Order = order };
        List<AudioEffect>? entering = null;
        for (int b = 0; b < _buses.Count; b++)
        {
            var effects = _buses[b].Effects;
            if (effects.Count == 0) continue;
            graph.Submix[b] = true;
            var chain = graph.Chains[b] = new AudioEffect[effects.Count];
            var keys = graph.Keys[b] = new int[effects.Count];
            for (int i = 0; i < chain.Length; i++)
            {
                var effect = chain[i] = effects[i];
                keys[i] = effect.Key is { } name ? _busNames[name].Index : -1;
                if (keys[i] >= 0) graph.Submix[keys[i]] = true;
                if (_sentEffects.Contains(effect)) continue;
                // Its buffers are made here, on the game's thread, before the render function ever sees it, and only
                // then: an effect that comes back to a chain may still be rendered by the chain it left, so nothing
                // on this thread touches it again. From here on it is this mixer's.
                if (effect.Owner is null)
                {
                    effect.PrepareFor(_rate, _channels);
                    effect.Owner = this;
                }
                (entering ??= []).Add(effect);
            }
        }
        for (int b = 1; b < _buses.Count; b++)
        {
            if (graph.Submix[b] || _busFaded[b]) _buffers[b] ??= new float[ChunkFrames * _channels];
            graph.Buffers[b] = _buffers[b];
        }
        if (entering is not null)
        {
            graph.Entering = [.. entering];
            graph.EnteringBypass = new bool[entering.Count];
            graph.EnteringValues = new float[entering.Count][];
            for (int i = 0; i < entering.Count; i++)
            {
                var effect = entering[i];
                graph.EnteringBypass[i] = effect.Bypass;
                var values = graph.EnteringValues[i] = new float[effect.ParameterCount];
                for (int p = 0; p < values.Length; p++) values[p] = effect.ParameterValue(p);
            }
        }
        if (!TryPost(new Command { Op = Op.Graph, Graph = graph, Frame = Now }))
        {
            // The sink stopped reading: the chains as they are then are sent when it reads again (Flush).
            DroppedCommands++;
            if (!_graphUnsent) _unsent++;
            _graphUnsent = true;
            return false;
        }
        if (_graphUnsent) _unsent--;
        _graphUnsent = false;
        _sentEffects.Clear();
        _sentEffects.Add(Limiter);
        for (int b = 0; b < _buses.Count; b++) foreach (var effect in _buses[b].Effects) _sentEffects.Add(effect);
        return true;
    }

    internal void PostParameter(AudioEffect effect, int index, float value)
    {
        // An effect the render function has not heard of yet gets its values with the chain it arrives in.
        if (!_sentEffects.Contains(effect)) return;
        Anchor();
        if (Flush() && TryPost(new Command { Op = Op.Parameter, Effect = effect, Slot = index, Volume = value, Frame = Now })) return;
        KeepEffect(effect);
    }

    internal void PostBypass(AudioEffect effect, bool bypass)
    {
        if (!_sentEffects.Contains(effect)) return;
        Anchor();
        if (Flush() && TryPost(new Command { Op = Op.Bypass, Effect = effect, Paused = bypass, Frame = Now })) return;
        KeepEffect(effect);
    }

    /// <summary>An effect left its bus: the render function is no longer taken to know it, and changes to it that waited for room in the queue are of no use.</summary>
    internal void Forget(AudioEffect effect)
    {
        // Whatever chain it enters next, it enters as new (cleared, with its values), also when the chain without it
        // could not be queued and the render function never saw it leave.
        _sentEffects.Remove(effect);
        if (!effect.Unsent) return;
        effect.Unsent = false;
        _unsentEffects--;
        _unsent--;
    }

    /// <summary>The queue had no room for a change to <paramref name="effect"/>: all its values are sent when it has.</summary>
    private void KeepEffect(AudioEffect effect)
    {
        DroppedCommands++;
        if (effect.Unsent) return;
        effect.Unsent = true;
        _unsentEffects++;
        _unsent++;
    }

    // ---- Playing

    /// <summary>
    /// Plays <paramref name="clip"/> from the current step's frame; returns its handle, or <see cref="VoiceHandle.None"/>
    /// when the voice policy refused it or the sink stopped reading (<see cref="DroppedCommands"/>).
    /// </summary>
    /// <exception cref="KeyNotFoundException">The bus does not exist.</exception>
    public VoiceHandle Play(AudioClip clip, in PlayParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(clip);
        double step = (double)clip.SampleRate / _rate * Speed(parameters.Pitch);
        long frames = clip.Samples.Length / clip.Channels;
        double from = StartFrame(parameters.StartAt, clip.SampleRate, frames);
        long length = parameters.Loop || frames == 0 ? (frames == 0 ? 0 : long.MaxValue) : (long)Math.Ceiling((frames - from) / step);
        var region = frames == 0 ? default : (parameters.LoopRegion ?? clip.LoopRegion ?? default).Within(frames);
        return Start(parameters, length, clip, null, step, from, frames, clip.SampleRate, region);
    }

    /// <summary>The frame of a sound a time in seconds names: within 0 and its length (when known), 0 for a value that is not a number.</summary>
    private static double StartFrame(double seconds, int sampleRate, long frames)
    {
        if (!(seconds > 0)) return 0;
        double frame = Math.Min(seconds * sampleRate, int.MaxValue);
        return frames > 0 ? Math.Min(frame, frames) : frame;
    }

    /// <summary>Plays <paramref name="stream"/> (music decoded while it plays); the engine disposes it when the sound ends or is stopped.</summary>
    /// <exception cref="KeyNotFoundException">The bus does not exist.</exception>
    public VoiceHandle Play(IAudioStream stream, in PlayParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (stream.Channels is not (1 or 2) || stream.SampleRate <= 0) throw new ArgumentException("A stream has 1 or 2 channels at a positive rate.", nameof(stream));
        var seekable = stream as ISeekableAudioStream;
        long frames = Math.Max(0, seekable?.Frames ?? 0);
        // Only a stream that can seek starts elsewhere than its beginning or repeats a part of itself.
        long from = seekable is null ? 0 : (long)StartFrame(parameters.StartAt, stream.SampleRate, frames);
        var region = seekable is null ? default : (parameters.LoopRegion ?? seekable.LoopRegion ?? default).Within(frames);
        // A stream is decoded in pieces, and each time round a loop it is put back at the region's start: a region of
        // a few frames would be all seeking and no sound. Shorter than a tenth of a second, it is the whole stream.
        if (region.End > 0 && region.End - region.Start < Math.Max(1, stream.SampleRate / 10)) region = (0, frames);
        var exact = stream as ExactSeekStream;
        // Far into a compressed stream: the way there is decoded over the next host frames, and the sound starts
        // when it is reached.
        bool later = exact is not null && exact.CostOf(from) > SeekBudget(stream);
        try
        {
            if (later) exact!.BeginSeek(from);
            else if (from > 0) seekable!.Seek(from);
            if (parameters.Loop && region.Start > 0) exact?.PrepareLoop(region.Start, region.End);
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            // A damaged file: there is no sound to play.
            stream.Dispose();
            ReportFailure(VoiceHandle.None, error);
            return VoiceHandle.None;
        }
        var feed = new StreamFeed(stream, parameters.Loop, _rate, _channels, StreamAheadFrames, from, region.Start, region.End) { Hold = later };
        var handle = Start(parameters, long.MaxValue, null, feed, (double)stream.SampleRate / _rate, from, frames, stream.SampleRate, region);
        if (handle.IsNone) { stream.Dispose(); return handle; }
        feed.Slot = handle.Index;
        feed.Generation = handle.Generation;
        _feeds.Add(feed);
        if (later)
        {
            ref var l = ref _logical[handle.Index];
            (l.Held, l.Seeking, l.SeekTarget) = (true, true, from);
            Mark(ref l);
        }
        return handle;
    }

    /// <summary>The frames of <paramref name="stream"/> decoded and dropped in one go on the way to a place sought.</summary>
    private long SeekBudget(IAudioStream stream) => Math.Max(1, (long)(_seekSeconds * stream.SampleRate));

    /// <summary>
    /// Whether <paramref name="voice"/> is on its way to a place that <see cref="Seek"/> or
    /// <see cref="PlayParameters.StartAt"/> named and has not reached it yet. Only compressed streams take time to get
    /// there (their decoders reach a place by decoding up to it, a little at each <see cref="Pump"/>): a stream sought
    /// while it plays goes on playing where it was until then, and one started in its middle is silent until then.
    /// </summary>
    public bool IsSeeking(VoiceHandle voice) => IsPlaying(voice) && _logical[voice.Index].Seeking;

    /// <summary>A stream's decoder threw: it is reported once, and the stream ends with what it had decoded.</summary>
    private void Fail(StreamFeed feed, Exception error)
    {
        if (feed.Reported) return;
        feed.Reported = true;
        feed.Fail(error);
        ReportFailure(new VoiceHandle(feed.Slot, feed.Generation), error);
    }

    private VoiceHandle Start(in PlayParameters p, long length, AudioClip? clip, StreamFeed? feed, double step, double from, long frames, int sampleRate, (long Start, long End) region)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var bus = Bus(p.Bus ?? SfxBus);
        Anchor();
        // A sound takes a voice only when the render function will hear of it: with the queue full it is refused, so no
        // handle ever names a sound that is not there (its calls would reach whatever still sounds in that voice).
        if (!Flush() || Full)
        {
            DroppedCommands++;
            return VoiceHandle.None;
        }
        long now = Now, at = now + Frames(p.Delay);
        int slot = Choose(now, p.Priority);
        if (slot < 0)
        {
            RefusedSounds++;
            return VoiceHandle.None;
        }
        // A sound that takes the voice of one still playing replaces it at its own first frame. When that frame is
        // later than this step's (a scheduled sound), the one it took is silenced now: its handle is gone, and a
        // handle that is gone names nothing that still sounds.
        if (at > now && Busy(slot)) TryPost(new Command { Op = Op.Stop, Slot = slot, Frame = now });
        Release(slot);
        ref var l = ref _logical[slot];
        l.Generation = l.Generation == uint.MaxValue ? 1 : l.Generation + 1;
        l.Active = true;
        l.Start = at;
        l.End = length == long.MaxValue ? long.MaxValue : at + length;
        l.Priority = p.Priority;
        l.Order = ++_played;
        l.Paused = false;
        l.Feed = feed;
        l.Loop = p.Loop;
        l.Frames = frames;
        l.Rate = (double)sampleRate / _rate;
        l.SampleRate = sampleRate;
        l.Step = step;
        l.StepRamp = 0;
        l.Position = l.FeedBase = from;
        l.PositionFrame = at;
        (l.LoopStart, l.LoopEnd) = region;
        (l.Held, l.Seeking, l.MotionCount) = (false, false, 0);
        Mark(ref l);
        l.Limit = long.MaxValue;
        l.Volume = float.IsNaN(p.Volume) ? 1 : Math.Clamp(p.Volume, 0, 1);
        (l.Left, l.Right) = Balance(p.Pan);
        TryPost(new Command
        {
            Op = Op.Start, Slot = slot, Frame = at, Clip = clip, Feed = feed, Step = step, Loop = p.Loop, Bus = bus.Index,
            Volume = l.Volume, Left = l.Left, Right = l.Right, Fade = Frames(p.FadeIn), Position = from,
            LoopStart = region.Start, LoopEnd = region.End,
        });
        return new VoiceHandle(slot, l.Generation);
    }

    /// <summary>A free voice, or the one the policy steals; -1 when the new sound is refused.</summary>
    /// <remarks>
    /// A voice is free when its sound is over at <paramref name="at"/>, the current step's frame, also for a sound that
    /// starts later: a voice holds one sound and at most one waiting to replace it, so a sound scheduled after another
    /// takes a voice of its own and holds it while it waits.
    /// </remarks>
    private int Choose(long at, int priority)
    {
        int victim = -1;
        for (int i = 0; i < _logical.Length; i++)
        {
            ref var l = ref _logical[i];
            if (!l.Active || l.End <= at) return i;
            if (victim < 0 || l.Priority < _logical[victim].Priority || l.Priority == _logical[victim].Priority && l.Order < _logical[victim].Order)
                victim = i;
        }
        return _logical[victim].Priority <= priority ? victim : -1;
    }

    /// <summary>The longest ramp or fade, in frames (over twelve hours at 48 kHz): a longer or endless time is this long, so frame counts never overflow.</summary>
    private const long LongestRamp = int.MaxValue;

    /// <summary>The frames of a time in seconds, at least one for any time above zero and at most <see cref="LongestRamp"/>; 0 for none, or for a value that is not a number.</summary>
    private long Frames(double seconds) => seconds > 0 ? Math.Clamp((long)Math.Round(seconds * _rate), 1, LongestRamp) : 0;

    /// <summary>
    /// Brings the timeline's view of a sound (where it is in its clip, and its speed) to frame <paramref name="now"/>:
    /// it advanced at its speed since the last change, through what is left of a pitch ramp, unless paused.
    /// </summary>
    private static void Settle(ref Logical l, long now)
    {
        long frames = Math.Max(0, now - l.PositionFrame);
        if (!l.Paused && !l.Held && frames > 0)
        {
            long ramp = Math.Min(frames, l.StepRamp);
            if (ramp > 0)
            {
                // Each frame of a ramp plays one step faster than the one before: the sum of those speeds.
                double delta = (l.StepTarget - l.Step) / l.StepRamp;
                l.Position += ramp * l.Step + delta * ramp * (ramp + 1) / 2;
                l.StepRamp -= ramp;
                l.Step = l.StepRamp == 0 ? l.StepTarget : l.Step + delta * ramp;
            }
            l.Position += (frames - ramp) * l.Step;
            // A loop goes back to the start of its region each time it reaches the region's end.
            if (l.Loop && l.LoopEnd > l.LoopStart && l.Position >= l.LoopEnd)
                l.Position = l.LoopStart + (l.Position - l.LoopStart) % (l.LoopEnd - l.LoopStart);
        }
        l.PositionFrame = Math.Max(now, l.PositionFrame);
    }

    /// <summary>
    /// Records how the sound moves from now on (where it is, its speed, whether it stands still): the timeline keeps
    /// the last few of these, so it can tell where the sound was at an earlier frame, which is where the player still
    /// hears it (<see cref="HeardPosition"/>). Called after every change to a sound's motion.
    /// </summary>
    private static void Mark(ref Logical l)
    {
        var motion = new Motion { Frame = l.PositionFrame, Position = l.Position, Step = l.Step, StepTarget = l.StepTarget, StepRamp = l.StepRamp, Still = l.Paused || l.Held };
        Span<Motion> motions = l.Motions;
        // Changes of one frame are one change.
        if (l.MotionCount > 0 && motions[l.MotionCount - 1].Frame >= motion.Frame)
        {
            motions[l.MotionCount - 1] = motion;
            return;
        }
        if (l.MotionCount == MotionsKept)
        {
            for (int i = 1; i < MotionsKept; i++) motions[i - 1] = motions[i];
            l.MotionCount--;
        }
        motions[l.MotionCount++] = motion;
    }

    /// <summary>
    /// Where a sound is in its source, in source frames, at output frame <paramref name="frame"/>: on the motion it
    /// had at that frame, so an earlier frame gives where it was then, across pauses, pitch changes and seeks. Before
    /// the first motion kept (a sound that has not started) it is counted back from there at the sound's speed.
    /// </summary>
    private static double SourceFrame(in Logical l, long frame)
    {
        var at = l;
        ReadOnlySpan<Motion> motions = at.Motions;
        int index = at.MotionCount - 1;
        if (index < 0)
        {
            Settle(ref at, frame);
            return at.Position;
        }
        while (index > 0 && motions[index].Frame > frame) index--;
        var motion = motions[index];
        if (frame < motion.Frame) return motion.Still ? motion.Position : motion.Position - (motion.Frame - frame) * motion.Step;
        (at.PositionFrame, at.Position, at.Step, at.StepTarget, at.StepRamp) = (motion.Frame, motion.Position, motion.Step, motion.StepTarget, motion.StepRamp);
        (at.Paused, at.Held) = (motion.Still, false);
        Settle(ref at, frame);
        return at.Frames > 0 ? Math.Min(at.Position, at.Frames) : at.Position;
    }

    /// <summary>
    /// Where <paramref name="voice"/> is in its sound, in seconds from the sound's beginning, at the current step's
    /// frame on the mixer's timeline: 0 right after it starts, what <see cref="Seek"/> was given right after a seek.
    /// A loop comes back to the start of its region. It follows the simulation's steps, so a deterministic run reads
    /// the same positions every time. 0 for a handle that is not playing. A stream that does not know its length
    /// (one that is not an <see cref="ISeekableAudioStream"/>) counts on through its loops.
    /// </summary>
    public double Position(VoiceHandle voice) => PositionAt(voice, 0);

    /// <summary>
    /// The position of <paramref name="voice"/> the player hears now: <see cref="Position"/> less the output's delay
    /// (<see cref="DelayFrames"/>, at least the sink's latency), which is how long a frame takes from the mixer to the
    /// speaker. What a game shows in step with the sound reads this: a note crossing a line, a cutscene's cue. It is
    /// negative while the sound's first frame is still on its way, by the time left until it is heard.
    /// </summary>
    /// <remarks>
    /// It is where the sound was one delay ago, exactly: it stands still that long after a pause and moves on that
    /// long after a resume, a pitch change reaches it that much later, and it never goes back while the sound plays
    /// on. After <see cref="Seek"/> it goes on along the old place until the seek is heard, then jumps to the new one
    /// (and after a loop's seam, back to the region's start). The timeline keeps the last eight changes of a sound's
    /// motion: with more than that within one delay, the oldest are forgotten and the heard position is approximate
    /// until the delay has passed.
    /// </remarks>
    public double HeardPosition(VoiceHandle voice) => PositionAt(voice, DelayFrames);

    private double PositionAt(VoiceHandle voice, long before)
    {
        Anchor();
        if (!IsPlaying(voice)) return 0;
        ref var l = ref _logical[voice.Index];
        return SourceFrame(l, Now - before) / l.SampleRate;
    }

    /// <summary>The length of the sound <paramref name="voice"/> plays, in seconds; 0 for a handle that is not playing and for a stream that does not know its length.</summary>
    public double Duration(VoiceHandle voice) => IsPlaying(voice) ? (double)_logical[voice.Index].Frames / _logical[voice.Index].SampleRate : 0;

    /// <summary>
    /// Moves <paramref name="voice"/> to <paramref name="seconds"/> from its sound's beginning, from the current step's
    /// frame: clamped to the sound's length (a sound that does not loop then ends there), and brought into the loop
    /// region when it loops and the time is past the region's end. A paused sound stays paused, at the new place.
    /// </summary>
    /// <returns>
    /// Whether it moved: false for a handle that is not playing, for a stream that cannot seek
    /// (one that is not an <see cref="ISeekableAudioStream"/>), and when the sink stopped reading
    /// (<see cref="DroppedCommands"/>). A time that is not a number moves nothing and returns true.
    /// </returns>
    public bool Seek(VoiceHandle voice, double seconds)
    {
        Anchor();
        if (!IsPlaying(voice)) return false;
        if (double.IsNaN(seconds)) return true;
        ref var l = ref _logical[voice.Index];
        var seekable = l.Feed?.Source as ISeekableAudioStream;
        if (l.Feed is not null && seekable is null) return false;
        if (!Flush() || Full)
        {
            DroppedCommands++;
            return false;
        }
        long now = Now;
        Settle(ref l, now);
        double target = StartFrame(seconds, l.SampleRate, l.Frames);
        if (l.Loop && l.LoopEnd > l.LoopStart && target >= l.LoopEnd) target = l.LoopStart + (target - l.LoopStart) % (l.LoopEnd - l.LoopStart);
        if (seekable is null)
        {
            l.Position = target;
            if (!l.Loop && l.Frames > 0)
            {
                // From where the sound is on the timeline, which for a sound that has not started is its start.
                long left = FramesLeft(l);
                if (l.Paused) l.Remaining = left;
                else l.End = Math.Min(l.PositionFrame + left, l.Limit);
            }
            Mark(ref l);
            TryPost(new Command { Op = Op.Seek, Slot = voice.Index, Frame = now, Position = target });
            return true;
        }
        target = Math.Floor(target);
        var feed = l.Feed!;
        try
        {
            if (seekable is ExactSeekStream exact && exact.CostOf((long)target) > SeekBudget(exact))
            {
                // Too far to decode in one go: the way there is spread over the next host frames (Pump), and the
                // sound goes on where it is until it is reached.
                exact.BeginSeek((long)target);
                (l.Seeking, l.SeekTarget) = (true, target);
                return true;
            }
            seekable.Seek((long)target);
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            Fail(feed, error);
            Stop(voice);
            return false;
        }
        ArriveAt(ref l, voice.Index, target, now);
        return true;
    }

    /// <summary>
    /// A stream's decoder is at <paramref name="target"/>: what was decoded ahead belongs to the old place, so a new
    /// feed takes the stream from the new one, decoded here so the sound goes on without a gap, and the render
    /// function changes to it at frame <paramref name="now"/>. The old feed is never filled again.
    /// </summary>
    private void ArriveAt(ref Logical l, int slot, double target, long now)
    {
        Settle(ref l, now);
        var old = l.Feed!;
        var feed = new StreamFeed(old.Source, old.Loop, _rate, _channels, StreamAheadFrames, (long)target, l.LoopStart, l.LoopEnd) { Slot = old.Slot, Generation = old.Generation, Reported = old.Reported };
        int index = _feeds.IndexOf(old);
        if (index >= 0) _feeds[index] = feed;
        else _feeds.Add(feed);
        l.Feed = feed;
        feed.Fill();
        (l.Position, l.FeedBase, l.Held, l.Seeking) = (target, target, false, false);
        if (l.Paused) l.Remaining = long.MaxValue;
        else l.End = l.Limit;
        Mark(ref l);
        TryPost(new Command { Op = Op.Seek, Slot = slot, Frame = now, Position = target, Feed = feed });
    }

    /// <summary>Frames a clip that does not loop still has to play, from where <see cref="Settle"/> left it.</summary>
    private static long FramesLeft(in Logical l)
    {
        double remaining = l.Frames - l.Position;
        if (l.StepRamp > 0)
        {
            long ramp = l.StepRamp;
            double delta = (l.StepTarget - l.Step) / ramp, covered = ramp * l.Step + delta * ramp * (ramp + 1) / 2;
            if (covered < remaining) return ramp + Math.Max(0, (long)Math.Ceiling((remaining - covered) / l.StepTarget));
            // It ends inside the ramp: the frame where the sum of the speeds reaches what is left.
            double a = delta / 2, b = l.Step + delta / 2;
            double frames = Math.Abs(a) < 1e-18 ? remaining / b : (-b + Math.Sqrt(Math.Max(0, b * b + 4 * a * remaining))) / (2 * a);
            return Math.Max(0, (long)Math.Ceiling(frames));
        }
        return Math.Max(0, (long)Math.Ceiling(remaining / l.Step));
    }

    /// <summary>The speed factor of a pitch: clamped, and 1 for a value that is not a number (which no clamp catches).</summary>
    private static float Speed(float pitch) => float.IsNaN(pitch) ? 1 : Math.Clamp(pitch, 0.01f, 100f);

    private static (float Left, float Right) Balance(float pan)
    {
        pan = float.IsNaN(pan) ? 0.5f : Math.Clamp(pan, 0, 1);
        return (Math.Min(1, 2 * (1 - pan)), Math.Min(1, 2 * pan));
    }

    /// <summary>Whether <paramref name="voice"/> is still playing (or paused, or fading out) on the mixer's timeline.</summary>
    public bool IsPlaying(VoiceHandle voice) => Owns(voice) && Busy(voice.Index);

    private bool Owns(VoiceHandle voice) => !voice.IsNone && (uint)voice.Index < (uint)_logical.Length && _logical[voice.Index].Generation == voice.Generation;

    private bool Busy(int slot) => _logical[slot].Active && _logical[slot].End > Now;

    /// <summary>Stops <paramref name="voice"/> at the current step's frame, fading it out over <paramref name="fadeSeconds"/>.</summary>
    /// <returns>Whether it was playing.</returns>
    public bool Stop(VoiceHandle voice, double fadeSeconds = 0)
    {
        Anchor();
        if (!IsPlaying(voice)) return false;
        long fade = Frames(fadeSeconds);
        ref var l = ref _logical[voice.Index];
        l.Limit = Math.Min(l.Limit, Now + fade);
        l.End = Math.Min(l.End, l.Limit);
        (l.StopFrame, l.StopFade) = (Now, fade);
        if (fade == 0) Release(voice.Index);
        Send(new Command { Op = Op.Stop, Slot = voice.Index, Frame = Now, Fade = fade }, Unsent.Stop);
        return true;
    }

    /// <summary>
    /// Changes the volume (0..1) and pan (0..1) of <paramref name="voice"/> from the current step's frame; null, or a
    /// value that is not a number, keeps a value. With <paramref name="seconds"/>, the values move there over that
    /// time, frame by frame, instead of at once (a ramp stands still while the sound is paused).
    /// </summary>
    /// <returns>Whether it was playing.</returns>
    public bool Set(VoiceHandle voice, float? volume = null, float? pan = null, double seconds = 0)
    {
        Anchor();
        if (!IsPlaying(voice)) return false;
        var (left, right) = pan is { } p && !float.IsNaN(p) ? Balance(p) : (-1f, -1f);
        float gain = volume is { } v && !float.IsNaN(v) ? Math.Clamp(v, 0, 1) : -1;
        if (gain < 0 && left < 0) return true;
        ref var l = ref _logical[voice.Index];
        if (gain >= 0) l.Volume = gain;
        if (left >= 0) (l.Left, l.Right) = (left, right);
        Send(new Command { Op = Op.Set, Slot = voice.Index, Frame = Now, Volume = gain, Left = left, Right = right, Fade = Frames(seconds) }, Unsent.Values);
        return true;
    }

    /// <summary>
    /// Changes the pitch of <paramref name="voice"/> from the current step's frame: the playback speed factor, 1 for the
    /// clip's own pitch, clamped to 0.01..100 as when a sound starts. The sound goes on from where it is, and the
    /// mixer's timeline moves its end accordingly. Streams play at their own pitch. A pitch that is not a number
    /// changes nothing. With <paramref name="seconds"/>, the speed moves there over that time, frame by frame (a
    /// glide), instead of at once.
    /// </summary>
    /// <returns>Whether it was playing and is a clip; false for a stale handle or a stream.</returns>
    public bool SetPitch(VoiceHandle voice, float pitch, double seconds = 0)
    {
        Anchor();
        if (!IsPlaying(voice)) return false;
        ref var l = ref _logical[voice.Index];
        if (l.Feed is not null) return false;
        if (float.IsNaN(pitch)) return true;
        long now = Now;
        // Where the sound is on the timeline: it advanced at the old speed since the last change, unless paused.
        Settle(ref l, now);
        double speed = l.Rate * Speed(pitch);
        long ramp = Frames(seconds);
        if (ramp > 0) (l.StepTarget, l.StepRamp) = (speed, ramp);
        else (l.Step, l.StepRamp) = (speed, 0);
        if (!l.Loop && l.Frames > 0)
        {
            long left = FramesLeft(l);
            if (l.Paused) l.Remaining = left;
            else l.End = Math.Min(l.PositionFrame + left, l.Limit);
        }
        Mark(ref l);
        Send(new Command { Op = Op.Set, Slot = voice.Index, Frame = now, Volume = -1, Left = -1, Right = -1, Step = speed, Fade = ramp }, Unsent.Values);
        return true;
    }

    /// <summary>Pauses (true) or resumes (false) <paramref name="voice"/> from the current step's frame; a paused sound keeps its voice.</summary>
    /// <returns>Whether it was playing.</returns>
    public bool Pause(VoiceHandle voice, bool paused)
    {
        Anchor();
        if (!IsPlaying(voice)) return false;
        ref var l = ref _logical[voice.Index];
        if (l.Paused == paused) return true;
        long now = Now;
        // A sound paused before its start frame (the timeline went back to a sink that had stopped) has played nothing.
        Settle(ref l, now);
        if (paused)
        {
            l.Remaining = l.End == long.MaxValue ? long.MaxValue : l.End - Math.Max(now, l.Start);
            l.End = long.MaxValue;
        }
        else l.End = l.Remaining == long.MaxValue ? long.MaxValue : now + l.Remaining;
        l.Paused = paused;
        Mark(ref l);
        Send(new Command { Op = Op.Pause, Slot = voice.Index, Frame = now, Paused = paused }, Unsent.Pause);
        return true;
    }

    // ---- Time

    /// <summary>Ends a simulation step: sounds played from now on start one step later. The game calls it once per fixed step.</summary>
    public void Advance()
    {
        Flush();
        if (!Deterministic) Follow();
        _step++;
        if (Deterministic) return;
        for (int i = 0; i < _logical.Length; i++) if (_logical[i].Active && _logical[i].End <= Now) Release(i);
    }

    /// <summary>
    /// Real time, as a step ends: keeps the steps' frames on the device's position, which is what the sink has rendered
    /// less its latency (what it renders now is heard that much later), so a step's sounds start in the frames the sink
    /// renders next and the sink's latency is not waited for twice.
    /// </summary>
    private void Follow()
    {
        int stepFrames = _rate / _updateRate;
        long rendered = RenderedFrames;
        if (rendered != _progressRendered) (_progressRendered, _progressStep, _progressPumps) = (rendered, _step, 0);
        // How far ahead of the device the step that ends was. Behind it (the game fell behind): catch up.
        long lead = StepFrame(_step) + _offset - (rendered + _skipped - _sinkLatency);
        if (lead < 0)
        {
            _offset -= lead;
            lead = 0;
        }
        else if (_progressPumps >= 2 && _step - _progressStep >= 2 && lead > stepFrames / 2
            && StepFrame(_step) - StepFrame(_progressStep) >= Math.Max(_rate / 4, 2L * Volatile.Read(ref _largestBlock)))
        {
            // The sink rendered nothing during a quarter of a second of steps (and twice the time of the largest block
            // it ever asked for, so a sink that renders in long blocks is not stopped between two of them), spread
            // over several host frames: it has not started or it stopped (a page whose audio is suspended, a device
            // that went away). The timeline does not wait for it: the frames it did not play are skipped, at this step
            // and at each one until it renders again, so it then plays what the game is at, half a step ahead, and
            // nothing of what it missed. The catch-up steps of one slow frame never get here, however many they are
            // and however long a step is: they are steps of a single host frame.
            SkipOutput(lead - stepFrames / 2);
            lead = stepFrames / 2;
        }
        if (_pumped)
        {
            // The first step of a host frame: the catch-up steps of a slow frame are legitimately ahead of the device,
            // each by one more step, but the frames after it are not. While even the least advanced step of the last
            // frame, and this one, are more than a step ahead, the timeline comes back a sixteenth of a step: steps
            // stay in order and almost evenly spaced (about a millisecond closer at 60 steps per second).
            long excess = Math.Min(lead, _frameLead) - stepFrames;
            if (excess > 0) _offset -= Math.Min(excess, Math.Clamp(stepFrames / 16, 1, Math.Max(1, stepFrames - 1)));
            _frameLead = long.MaxValue;
            _pumped = false;
        }
        _frameLead = Math.Min(_frameLead, lead);
    }

    /// <summary>
    /// Skips <paramref name="frames"/> the output did not play: the render function passes them without mixing
    /// (<see cref="Skip"/>). Streams are not decoded for frames nobody hears, so they stay where they are and last
    /// that much longer on the timeline.
    /// </summary>
    private void SkipOutput(long frames)
    {
        long from = RenderedFrames + _skipped;
        for (int i = 0; i < _logical.Length; i++)
        {
            ref var l = ref _logical[i];
            if (!l.Active || l.Feed is null || l.Paused) continue;
            long held = frames - Math.Max(0, l.Start - from);
            if (held <= 0) continue;
            l.Start += held;
            l.PositionFrame += held;
            Mark(ref l);
            if (l.End != long.MaxValue) l.End = Math.Min(l.End + held, l.Limit);
        }
        Volatile.Write(ref _skipped, _skipped + frames);
    }

    /// <summary>
    /// The host frame's audio work, on the main thread outside the simulation step: decodes music streams ahead, frees
    /// streams that ended, then pumps the sink (which renders here when it is pumped by the host).
    /// </summary>
    public void Pump()
    {
        if (_disposed) return;
        _pumped = true;
        if (_progressPumps < int.MaxValue) _progressPumps++;
        Flush();
        for (int i = _feeds.Count - 1; i >= 0; i--)
        {
            var feed = _feeds[i];
            ref var l = ref _logical[feed.Slot];
            if (l.Generation != feed.Generation || !l.Active)
            {
                _feeds.RemoveAt(i);
                feed.Source.Dispose();
                continue;
            }
            if (l.Seeking)
            {
                // On its way to a place sought: a piece of the way per host frame, and the change once it is there
                // (when the queue has room to tell the render function).
                if (!Flush() || Full) continue;
                bool there;
                try { there = ((ExactSeekStream)feed.Source).ContinueSeek(SeekBudget(feed.Source)); }
                catch (Exception error) when (error is not OutOfMemoryException)
                {
                    l.Seeking = false;
                    Fail(feed, error);
                    Stop(new VoiceHandle(feed.Slot, feed.Generation));
                    continue;
                }
                if (there)
                {
                    ArriveAt(ref l, feed.Slot, l.SeekTarget, Now);
                    feed = l.Feed!;
                }
            }
            feed.Fill();
            // The way back to a loop's start, a piece per host frame.
            if (feed.Error is null && feed.Source is ExactSeekStream looping)
            {
                try { looping.Idle(2 * SeekBudget(looping)); }
                catch (Exception error) when (error is not OutOfMemoryException) { feed.Fail(error); }
            }
            if (feed.Error is { } failure)
            {
                Fail(feed, failure);
                // What it decoded is still played; the sound is over when the render function has played it.
                if (!l.Paused) l.End = Math.Min(l.End, Math.Max(Now, RenderedFrames + _skipped + feed.Buffered));
                continue;
            }
            // Its end is known once decoding reaches it; the mixer's timeline learns it here.
            if (feed.Finished && l.End == long.MaxValue && !l.Paused)
            {
                // What the timeline has played of this feed so far (pauses and seeks taken into account) against what
                // the feed holds in all.
                long now = Math.Max(Now, l.PositionFrame);
                Settle(ref l, now);
                long played = (long)Math.Round((l.Position - l.FeedBase) / l.Step);
                l.End = Math.Min(now + Math.Max(0, feed.Produced - played), l.Limit);
            }
        }
        _sink.Pump();
    }

    private void Release(int slot)
    {
        ref var l = ref _logical[slot];
        l.Active = false;
        l.Feed = null;
    }

    private bool Full => (_head + 1) % QueueSize == Volatile.Read(ref _tail);

    private bool TryPost(in Command command)
    {
        if (Full) return false;
        _queue[_head] = command;
        Volatile.Write(ref _head, (_head + 1) % QueueSize);
        return true;
    }

    /// <summary>
    /// Queues a change to a playing voice. When the queue is full (the sink stopped reading), the change is not lost:
    /// the main thread keeps the voice's latest values, pause and stop, and <see cref="Flush"/> sends them once there is
    /// room, so a sink that renders again plays what the game last asked for.
    /// </summary>
    private void Send(in Command command, Unsent kind)
    {
        if (Flush() && TryPost(command)) return;
        DroppedCommands++;
        ref var l = ref _logical[command.Slot];
        if (l.Unsent == Unsent.None) _unsent++;
        l.Unsent |= kind;
    }

    /// <summary>
    /// Sends what could not be queued, oldest kind first for each voice (values, pause, stop), before anything newer is
    /// queued; false while the queue is still full. Nothing to do, and nothing read, unless a call found the queue full.
    /// </summary>
    private bool Flush()
    {
        if (_unsent == 0) return true;
        long now = Now;
        for (int slot = 0; slot < _logical.Length; slot++)
        {
            ref var l = ref _logical[slot];
            if (l.Unsent == Unsent.None) continue;
            // The values of a sound that ended or was stopped at once are of no use; its voice is taken again only after
            // what is left here is sent.
            if ((l.Unsent & Unsent.Values) != 0 && l.Active
                && !TryPost(new Command { Op = Op.Set, Slot = slot, Frame = now, Volume = l.Volume, Left = l.Left, Right = l.Right, Step = l.Feed is not null ? 0 : l.StepRamp > 0 ? l.StepTarget : l.Step })) return false;
            l.Unsent &= ~Unsent.Values;
            // A resume is sent even when the sound has since ended on the timeline: the render function still holds it
            // paused, and plays what is left as it would have had the resume been queued.
            if ((l.Unsent & Unsent.Pause) != 0 && !TryPost(new Command { Op = Op.Pause, Slot = slot, Frame = now, Paused = l.Paused })) return false;
            l.Unsent &= ~Unsent.Pause;
            if ((l.Unsent & Unsent.Stop) != 0 && !TryPost(new Command { Op = Op.Stop, Slot = slot, Frame = l.StopFrame, Fade = l.StopFade })) return false;
            l.Unsent = Unsent.None;
            _unsent--;
        }
        // The chains before anything that depends on them: a faded bus's buffer, an effect's values.
        if (_graphUnsent && !SendGraph(Order(_buses))) return false;
        for (int bus = 0; bus < _buses.Count; bus++)
        {
            if (!_busUnsent[bus]) continue;
            if (!TryPost(new Command { Op = _busFaded[bus] ? Op.BusFade : Op.Bus, Slot = bus, Volume = _buses[bus].Volume, Frame = now })) return false;
            _busUnsent[bus] = false;
            _unsent--;
        }
        if (_unsentEffects > 0)
        {
            if (!FlushEffect(Limiter, now)) return false;
            for (int bus = 0; bus < _buses.Count; bus++)
                foreach (var effect in _buses[bus].Effects) if (!FlushEffect(effect, now)) return false;
        }
        return true;
    }

    /// <summary>Sends every value of an effect whose changes the queue had no room for; false while it still has none.</summary>
    private bool FlushEffect(AudioEffect effect, long now)
    {
        if (!effect.Unsent) return true;
        // Room for all of it or nothing, so a part is never sent twice.
        int free = (Volatile.Read(ref _tail) - _head - 1 + QueueSize) % QueueSize;
        if (free < effect.ParameterCount + 1) return false;
        for (int p = 0; p < effect.ParameterCount; p++)
            TryPost(new Command { Op = Op.Parameter, Effect = effect, Slot = p, Volume = effect.ParameterValue(p), Frame = now });
        TryPost(new Command { Op = Op.Bypass, Effect = effect, Paused = effect.Bypass, Frame = now });
        effect.Unsent = false;
        _unsentEffects--;
        _unsent--;
        return true;
    }

    /// <summary>
    /// Calls that could not be queued for the render function because the sink had stopped reading (the queue was
    /// full). No change is lost: the latest volume, pan and pitch of a sound, its pause and its stop, and bus volumes
    /// are sent when the sink reads again. A sound that could not be queued was refused
    /// (<see cref="VoiceHandle.None"/>).
    /// </summary>
    public int DroppedCommands { get; private set; }

    /// <summary>Stops the sink (it no longer renders after this returns) and the streams.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _sink.Dispose();
        foreach (var feed in _feeds) feed.Source.Dispose();
        _feeds.Clear();
    }

    // ---- Rendering (the sink's thread)

    /// <summary>The render function the sink calls (<see cref="AudioRender"/>): mixes the next frames into <paramref name="buffer"/>.</summary>
    public void Render(Span<float> buffer)
    {
        buffer.Clear();
        int frames = buffer.Length / _channels;
        // Read before the queue: everything the game asked for before it skipped frames is then in the queue already,
        // stamped with its frame, and is passed with them.
        long skipped = Volatile.Read(ref _skipped);
        long rendered = _position, start = rendered + _skew;
        if (frames > _largestBlock) Volatile.Write(ref _largestBlock, frames);
        Drain(start);
        if (skipped != _skew)
        {
            long to = start + (skipped - _skew);
            Skip(start, to);
            (start, _skew) = (to, skipped);
        }
        // Bus volumes, effect chains and their parameters change at their exact frames: the block is mixed in segments
        // between those frames, none longer than a submix buffer.
        int done = 0;
        while (done < frames)
        {
            long segmentStart = start + done, segmentEnd = Math.Min(start + frames, segmentStart + ChunkFrames);
            for (int b = 0; b < MaxBuses; b++)
            {
                int first = b * WaitingChanges;
                while (_busChanges[b] > 0 && _busWaiting[first].At <= segmentStart)
                {
                    _busVolume[b] = _busWaiting[first].Volume;
                    Array.Copy(_busWaiting, first + 1, _busWaiting, first, --_busChanges[b]);
                }
                if (_busChanges[b] > 0) segmentEnd = Math.Min(segmentEnd, _busWaiting[first].At);
            }
            while (_eventCount > 0 && _events[_eventHead].At <= segmentStart) ApplyEvent();
            if (_eventCount > 0) segmentEnd = Math.Min(segmentEnd, _events[_eventHead].At);
            var graph = _graph;
            // Where each bus is mixed. A bus with effects, one an effect listens to and one whose volume is fading are
            // mixed in a buffer of their own (a submix); any other bus is not mixed at all: its sounds go straight into
            // the nearest submix above it (or the output), scaled by the volumes on the way, as they always did.
            for (int b = 0; b < MaxBuses; b++)
            {
                if (_busFadeLeft[b] > 0) segmentEnd = Math.Min(segmentEnd, segmentStart + _busFadeLeft[b]);
                int parent = _busParent[b];
                bool submix = graph.Submix[b] || _busFadeLeft[b] > 0;
                if (submix && b > 0 && graph.Buffers[b] is null)
                {
                    // The chains always arrive before a fade and carry its buffer; should one ever not, the fade ends
                    // here instead of the render function failing.
                    (_busFadeLeft[b], _busVolume[b], submix) = (0, _busFadeTarget[b], false);
                }
                _busSubmix[b] = submix;
                _busAbove[b] = parent < 0 || _busSubmix[parent] ? 1 : _busGain[parent];
                _busGain[b] = submix ? 1 : _busVolume[b] * (parent >= 0 ? _busAbove[b] : 1);
                _busDestination[b] = submix ? b : parent < 0 ? 0 : _busDestination[parent];
            }
            int count = (int)(segmentEnd - segmentStart);
            var segment = buffer.Slice(done * _channels, count * _channels);
            for (int b = 1; b < MaxBuses; b++)
                if (_busSubmix[b]) graph.Buffers[b].AsSpan(0, segment.Length).Clear();
            for (int v = 0; v < _voices.Length; v++)
                if (_voices[v].Active || _hasIncoming[v]) Mix(v, graph, segment, segmentStart, count);
            var order = graph.Order;
            for (int i = 0; i < order.Length; i++)
                if (_busSubmix[order[i]]) Submix(graph, order[i], segment);
            if (!Limiter.Bypassed && !Limiter.Failed) Run(Limiter, segment, default, false);
            done += count;
        }
        Volatile.Write(ref _position, rendered + frames);
    }

    /// <summary>
    /// Finishes a bus mixed in its own buffer: its effects in order, then its volume (gain by gain while it fades), then
    /// into the nearest submix above it. "master" is finished in the output itself.
    /// </summary>
    private void Submix(Graph graph, int bus, Span<float> output)
    {
        var mix = bus == 0 ? output : graph.Buffers[bus].AsSpan(0, output.Length);
        if (graph.Chains[bus] is { } chain)
        {
            var keys = graph.Keys[bus]!;
            for (int i = 0; i < chain.Length; i++)
            {
                var effect = chain[i];
                if (effect.Bypassed || effect.Failed) continue;
                int key = keys[i];
                Run(effect, mix, key < 0 ? default : graph.Buffers[key].AsSpan(0, output.Length), key >= 0);
            }
        }
        if (_busFadeLeft[bus] > 0)
        {
            // The segment ends with the fade at the latest, so every frame here takes one step of it.
            // Each frame's volume is counted from the end of the fade backwards: no error builds up over a long fade.
            float volume = _busVolume[bus], target = _busFadeTarget[bus], step = _busFadeStep[bus];
            long left = _busFadeLeft[bus];
            for (int i = 0; i < mix.Length; i += _channels)
            {
                volume = target - step * --left;
                for (int c = 0; c < _channels; c++) mix[i + c] *= volume;
            }
            _busFadeLeft[bus] = left;
            _busVolume[bus] = volume;
        }
        else if (_busVolume[bus] != 1)
        {
            float volume = _busVolume[bus];
            for (int i = 0; i < mix.Length; i++) mix[i] *= volume;
        }
        if (bus == 0) return;
        int into = _busDestination[_busParent[bus]];
        var above = into == 0 ? output : graph.Buffers[into].AsSpan(0, output.Length);
        float gain = _busAbove[bus];
        for (int i = 0; i < mix.Length; i++) above[i] += mix[i] * gain;
    }

    /// <summary>Runs an effect over a segment. One that throws takes no further part in the mix: nothing escapes the render function.</summary>
    private void Run(AudioEffect effect, Span<float> mix, ReadOnlySpan<float> key, bool keyed)
    {
        try
        {
            if (keyed) effect.Run(mix, key);
            else effect.Run(mix);
        }
        catch (Exception e)
        {
            Fail(effect, e);
        }
    }

    private void Fail(AudioEffect effect, Exception error)
    {
        effect.Failed = true;
        effect.Fault(error);
        Volatile.Write(ref _faulted, _faulted + 1);
    }

    /// <summary>Applies the oldest waiting change to the graph.</summary>
    private void ApplyEvent()
    {
        ref var e = ref _events[_eventHead];
        switch (e.Op)
        {
            case Op.Graph:
                var graph = _graph = e.Graph!;
                for (int i = 0; i < graph.Entering.Length; i++)
                {
                    var entering = graph.Entering[i];
                    entering.Bypassed = graph.EnteringBypass[i];
                    try
                    {
                        entering.Clear();
                        var values = graph.EnteringValues[i];
                        for (int p = 0; p < values.Length; p++) entering.Deliver(p, values[p]);
                    }
                    catch (Exception error)
                    {
                        Fail(entering, error);
                    }
                }
                break;
            case Op.Parameter:
                try { e.Effect!.Deliver(e.Index, e.Value); }
                catch (Exception error) { Fail(e.Effect!, error); }
                break;
            case Op.Bypass:
                var effect = e.Effect!;
                if (effect.Bypassed && !e.Flag)
                {
                    try { effect.Clear(); }
                    catch (Exception error) { Fail(effect, error); }
                }
                effect.Bypassed = e.Flag;
                break;
            case Op.BusFade:
                // A fade from where the volume is, also in the middle of another fade; no frames: at once.
                _busFadeLeft[e.Index] = e.Frames;
                _busFadeTarget[e.Index] = e.Value;
                if (e.Frames > 0) _busFadeStep[e.Index] = (e.Value - _busVolume[e.Index]) / e.Frames;
                else _busVolume[e.Index] = e.Value;
                break;
        }
        e.Graph = null;
        e.Effect = null;
        _eventHead = (_eventHead + 1) % WaitingEvents;
        _eventCount--;
    }

    /// <summary>Keeps a change to the graph until its frame; with no room left, the oldest one takes effect now.</summary>
    private ref Event Wait(long at)
    {
        if (_eventCount == WaitingEvents) ApplyEvent();
        ref var e = ref _events[(_eventHead + _eventCount++) % WaitingEvents];
        e = new Event { At = at };
        return ref e;
    }

    /// <summary>
    /// Passes the frames from <paramref name="from"/> to <paramref name="to"/> without mixing them (the output did not
    /// play them): every voice ends up as if they had been rendered. Sounds that start, change, stop or end in them do
    /// so at their frames; a clip moves on by the frames it would have played; a stream stays where it is.
    /// </summary>
    private void Skip(long from, long to)
    {
        // Changes to the graph in those frames take effect; a fade that was running in them is over.
        while (_eventCount > 0 && _events[_eventHead].At < to) ApplyEvent();
        for (int b = 0; b < MaxBuses; b++)
        {
            if (_busFadeLeft[b] == 0) continue;
            _busFadeLeft[b] = 0;
            _busVolume[b] = _busFadeTarget[b];
        }
        for (int slot = 0; slot < _voices.Length; slot++)
        {
            ref var voice = ref _voices[slot];
            if (_hasIncoming[slot] && _incoming[slot].Start < to)
            {
                voice = _incoming[slot];
                _hasIncoming[slot] = false;
                _incoming[slot] = default;
            }
            long pos = Math.Max(from, voice.Start);
            while (voice.Active && pos < to)
            {
                while (voice.Changes > 0 && pos >= voice.Pending[0].At) ApplyPending(ref voice);
                // A ramp that was running in frames nobody heard is over.
                if (voice.Ramping) FinishRamps(ref voice);
                long end = voice.StopAt == long.MaxValue ? long.MaxValue : voice.StopAt + voice.Fade;
                if (pos >= end) { Silence(ref voice); break; }
                long next = Math.Min(to, end);
                if (voice.Changes > 0) next = Math.Min(next, voice.Pending[0].At);
                if (!voice.Paused && voice.Clip is { } clip)
                {
                    int length = clip.Samples.Length / clip.Channels;
                    voice.Position += (next - pos) * voice.Step;
                    if (voice.Loop && voice.LoopEnd > voice.LoopStart)
                    {
                        if (voice.Position >= voice.LoopEnd) voice.Position = voice.LoopStart + (voice.Position - voice.LoopStart) % (voice.LoopEnd - voice.LoopStart);
                    }
                    else if (voice.Position >= length) { Silence(ref voice); break; }
                }
                pos = next;
            }
        }
    }

    /// <summary>The newest sound of a voice: the one waiting for its start frame, if any.</summary>
    private ref Voice Target(int slot) => ref _hasIncoming[slot] ? ref _incoming[slot] : ref _voices[slot];

    private void Mix(int slot, Graph graph, Span<float> output, long start, int frames)
    {
        ref var voice = ref _voices[slot];
        float busGain = _busGain[voice.Bus];
        int into = _busDestination[voice.Bus];
        var buffer = into == 0 ? output : graph.Buffers[into].AsSpan(0, output.Length);
        for (int f = 0; f < frames; f++)
        {
            long pos = start + f;
            // A sound that took this voice replaces the one playing at its own start frame, not before.
            if (_hasIncoming[slot] && pos >= _incoming[slot].Start)
            {
                voice = _incoming[slot];
                _hasIncoming[slot] = false;
                _incoming[slot] = default;
                busGain = _busGain[voice.Bus];
                into = _busDestination[voice.Bus];
                buffer = into == 0 ? output : graph.Buffers[into].AsSpan(0, output.Length);
            }
            if (!voice.Active)
            {
                if (!_hasIncoming[slot]) return;
                continue;
            }
            if (pos < voice.Start) continue;
            while (voice.Changes > 0 && pos >= voice.Pending[0].At) ApplyPending(ref voice);
            if (pos >= voice.StopAt + voice.Fade) { Silence(ref voice); continue; }
            if (voice.Paused) continue;
            if (voice.Ramping) Ramp(ref voice);
            float gain = voice.Volume * busGain;
            if (pos >= voice.StopAt) gain *= 1f - (float)(pos - voice.StopAt) / voice.Fade;
            float l, r;
            if (voice.Clip is { } clip)
            {
                if (!SampleClip(ref voice, clip, out l, out r)) { Silence(ref voice); continue; }
            }
            else if (!voice.Feed!.TryRead(out l, out r))
            {
                if (voice.Feed.Finished) Silence(ref voice);
                continue; // decoding fell behind: silence, the stream resumes where it was
            }
            if (_channels == 2)
            {
                buffer[f * 2] += l * gain * voice.Left;
                buffer[f * 2 + 1] += r * gain * voice.Right;
            }
            else buffer[f] += (l + r) * 0.5f * gain;
        }
    }

    private static bool SampleClip(ref Voice voice, AudioClip clip, out float left, out float right)
    {
        var samples = clip.Samples;
        int channels = clip.Channels;
        int length = samples.Length / channels;
        // A loop goes back to the start of its region (the whole clip unless the sound names a part of it); the frame
        // before the region's end is held for the fraction of a frame that reaches past it.
        if (voice.Loop)
        {
            if (voice.Position >= voice.LoopEnd) voice.Position = voice.LoopStart + (voice.Position - voice.LoopStart) % (voice.LoopEnd - voice.LoopStart);
            length = voice.LoopEnd;
        }
        int i = (int)voice.Position;
        if (i >= length) { left = right = 0; return false; }
        float t = (float)(voice.Position - i);
        int j = Math.Min(i + 1, length - 1);
        float l0 = samples[i * channels], l1 = samples[j * channels];
        left = l0 + (l1 - l0) * t;
        if (channels == 2)
        {
            float r0 = samples[i * 2 + 1], r1 = samples[j * 2 + 1];
            right = r0 + (r1 - r0) * t;
        }
        else right = left;
        voice.Position += voice.Step;
        return true;
    }

    private static void Silence(ref Voice voice)
    {
        voice.Active = false;
        voice.Clip = null;
        voice.Feed = null;
    }

    /// <summary>Applies the oldest waiting change of <paramref name="voice"/>; the others move up.</summary>
    private static void ApplyPending(ref Voice voice)
    {
        ref var change = ref voice.Pending[0];
        if (change.Volume >= 0)
        {
            voice.VolumeLeft = change.VolumeRamp;
            if (change.VolumeRamp > 0) (voice.VolumeTarget, voice.VolumeStep) = (change.Volume, (change.Volume - voice.Volume) / change.VolumeRamp);
            else voice.Volume = change.Volume;
        }
        if (change.Left >= 0)
        {
            voice.PanLeft = change.PanRamp;
            if (change.PanRamp > 0)
            {
                (voice.LeftTarget, voice.RightTarget) = (change.Left, change.Right);
                (voice.LeftStep, voice.RightStep) = ((change.Left - voice.Left) / change.PanRamp, (change.Right - voice.Right) / change.PanRamp);
            }
            else (voice.Left, voice.Right) = (change.Left, change.Right);
        }
        if (change.Pause != 0) voice.Paused = change.Pause > 0;
        if (change.Feed is { } feed) voice.Feed = feed;
        else if (change.Seek >= 0 && voice.Clip is not null) voice.Position = change.Seek;
        if (change.Step > 0)
        {
            voice.StepLeft = change.StepRamp;
            if (change.StepRamp > 0) (voice.StepTarget, voice.StepDelta) = (change.Step, (change.Step - voice.Step) / change.StepRamp);
            else voice.Step = change.Step;
        }
        voice.Ramping = voice.VolumeLeft > 0 || voice.PanLeft > 0 || voice.StepLeft > 0;
        voice.Changes--;
        for (int i = 0; i < voice.Changes; i++) voice.Pending[i] = voice.Pending[i + 1];
        // The place left free keeps no stream alive.
        voice.Pending[voice.Changes].Feed = null;
    }

    /// <summary>One frame of the ramps of <paramref name="voice"/>: each value takes one step, and its target exactly at the last one.</summary>
    private static void Ramp(ref Voice voice)
    {
        if (voice.VolumeLeft > 0)
        {
            // From the end backwards, not step upon step: no error builds up over a long ramp.
            voice.Volume = voice.VolumeTarget - voice.VolumeStep * --voice.VolumeLeft;
        }
        if (voice.PanLeft > 0)
        {
            voice.PanLeft--;
            voice.Left = voice.LeftTarget - voice.LeftStep * voice.PanLeft;
            voice.Right = voice.RightTarget - voice.RightStep * voice.PanLeft;
        }
        if (voice.StepLeft > 0)
        {
            voice.Step = voice.StepTarget - voice.StepDelta * --voice.StepLeft;
        }
        voice.Ramping = voice.VolumeLeft > 0 || voice.PanLeft > 0 || voice.StepLeft > 0;
    }

    private static void FinishRamps(ref Voice voice)
    {
        if (voice.VolumeLeft > 0) voice.Volume = voice.VolumeTarget;
        if (voice.PanLeft > 0) (voice.Left, voice.Right) = (voice.LeftTarget, voice.RightTarget);
        if (voice.StepLeft > 0) voice.Step = voice.StepTarget;
        voice.VolumeLeft = voice.PanLeft = voice.StepLeft = 0;
        voice.Ramping = false;
    }

    /// <summary>
    /// The waiting change of <paramref name="voice"/> for frame <paramref name="at"/>: the newest one when it is for that
    /// frame (a step's volume, pan, pitch and pause wait together), or a new one after it. Each waits for its own frame,
    /// so the changes of several steps can wait at once (a delay longer than a step, the steps after a slow frame).
    /// A voice keeps <see cref="WaitingChanges"/> apart; with more, one of them joins the one after it
    /// (<see cref="Thin"/>).
    /// </summary>
    private static ref Change Waiting(ref Voice voice, long at)
    {
        Span<Change> pending = voice.Pending;
        if (voice.Changes > 0)
        {
            ref var newest = ref pending[voice.Changes - 1];
            // Frames only go forward; a change for an earlier frame than the newest joins it.
            if (at <= newest.At) return ref newest;
            if (voice.Changes == WaitingChanges)
            {
                int join = Thin(pending, at);
                // The newest joins the new change: it keeps its values, which the new one's replace, and moves to its frame.
                if (join == WaitingChanges - 1)
                {
                    newest.At = at;
                    return ref newest;
                }
                ref var from = ref pending[join];
                ref var into = ref pending[join + 1];
                if (into.Volume < 0) (into.Volume, into.VolumeRamp) = (from.Volume, from.VolumeRamp);
                if (into.Left < 0) (into.Left, into.Right, into.PanRamp) = (from.Left, from.Right, from.PanRamp);
                if (into.Pause == 0) into.Pause = from.Pause;
                if (into.Step <= 0) (into.Step, into.StepRamp) = (from.Step, from.StepRamp);
                if (into.Seek < 0) (into.Seek, into.Feed) = (from.Seek, from.Feed);
                voice.Changes--;
                for (int k = join; k < voice.Changes; k++) pending[k] = pending[k + 1];
                pending[voice.Changes].Feed = null;
            }
        }
        ref var added = ref pending[voice.Changes++];
        added = new Change { At = at, Volume = -1, Left = -1, Seek = -1 };
        return ref added;
    }

    /// <summary>
    /// With <see cref="WaitingChanges"/> changes waiting and one more for frame <paramref name="at"/>: which of them
    /// joins the one after it (the new one, for the last), taking effect at that later frame. Never the oldest, which
    /// always takes effect at its own frame, so changes that keep arriving cannot hold the others back; and the one
    /// whose neighbours are closest, so the frames left stay spread over the whole wait: what is heard is then never
    /// more than about a quarter of the wait behind what was asked for, never ahead of it, and the last change asked
    /// for keeps its frame.
    /// </summary>
    private static int Thin(ReadOnlySpan<Change> waiting, long at)
    {
        int join = 1;
        long closest = long.MaxValue;
        for (int i = 1; i < waiting.Length; i++)
        {
            long span = (i + 1 < waiting.Length ? waiting[i + 1].At : at) - waiting[i - 1].At;
            if (span < closest) (closest, join) = (span, i);
        }
        return join;
    }

    private void Drain(long blockStart)
    {
        int tail = _tail, head = Volatile.Read(ref _head);
        while (tail != head)
        {
            ref var c = ref _queue[tail];
            long at = Math.Max(c.Frame, blockStart); // a late command takes effect at once
            switch (c.Op)
            {
                case Op.Start:
                    var started = new Voice
                    {
                        Active = true, Clip = c.Clip, Feed = c.Feed, Step = c.Step, Loop = c.Loop, Bus = c.Bus, Volume = c.Volume,
                        Left = c.Left, Right = c.Right, Start = at, StopAt = long.MaxValue, Position = c.Position,
                        LoopStart = (int)c.LoopStart, LoopEnd = (int)c.LoopEnd,
                    };
                    if (c.Fade > 0)
                    {
                        // A fade-in: from silence to its volume, one step per frame.
                        (started.Volume, started.VolumeTarget, started.VolumeStep) = (0, c.Volume, c.Volume / c.Fade);
                        (started.VolumeLeft, started.Ramping) = ((int)Math.Min(c.Fade, int.MaxValue), true);
                    }
                    if (_voices[c.Slot].Active && at > blockStart)
                    {
                        _incoming[c.Slot] = started;
                        _hasIncoming[c.Slot] = true;
                    }
                    else
                    {
                        _voices[c.Slot] = started;
                        _hasIncoming[c.Slot] = false;
                        _incoming[c.Slot] = default;
                    }
                    break;
                case Op.Stop:
                    ref var stopping = ref Target(c.Slot);
                    stopping.StopAt = Math.Min(stopping.StopAt, at);
                    stopping.Fade = c.Fade;
                    break;
                case Op.Set:
                case Op.Pause:
                case Op.Seek:
                    // Changes of one frame (a step's volume, pan and pitch) wait together for it; those of other
                    // steps wait each for its own frame.
                    ref var change = ref Waiting(ref Target(c.Slot), at);
                    if (c.Op == Op.Set)
                    {
                        int ramp = (int)Math.Min(c.Fade, int.MaxValue);
                        if (c.Volume >= 0) (change.Volume, change.VolumeRamp) = (c.Volume, ramp);
                        if (c.Left >= 0) (change.Left, change.Right, change.PanRamp) = (c.Left, c.Right, ramp);
                        if (c.Step > 0) (change.Step, change.StepRamp) = (c.Step, ramp);
                    }
                    else if (c.Op == Op.Seek) (change.Seek, change.Feed) = (c.Position, c.Feed);
                    else change.Pause = c.Paused ? 1 : -1;
                    break;
                case Op.Bus:
                    // As for a voice: the volumes of several steps wait each for its frame; beyond what a bus keeps
                    // apart, one of them gives way to the one after it, never the oldest (Thin).
                    int waiting = _busChanges[c.Slot], first = c.Slot * WaitingChanges;
                    if (waiting > 0 && at <= _busWaiting[first + waiting - 1].At) _busWaiting[first + waiting - 1].Volume = c.Volume;
                    else
                    {
                        if (waiting == WaitingChanges)
                        {
                            int gone = first + Thin(_busWaiting.AsSpan(first, WaitingChanges), at);
                            waiting--;
                            Array.Copy(_busWaiting, gone + 1, _busWaiting, gone, first + waiting - gone);
                        }
                        _busWaiting[first + waiting] = new Change { At = at, Volume = c.Volume };
                        _busChanges[c.Slot] = waiting + 1;
                    }
                    break;
                case Op.BusFade:
                    ref var fade = ref Wait(at);
                    (fade.Op, fade.Index, fade.Value, fade.Frames) = (Op.BusFade, c.Slot, c.Volume, c.Fade);
                    break;
                case Op.Graph:
                    ref var chains = ref Wait(at);
                    (chains.Op, chains.Graph) = (Op.Graph, c.Graph);
                    break;
                case Op.Parameter:
                    ref var parameter = ref Wait(at);
                    (parameter.Op, parameter.Effect, parameter.Index, parameter.Value) = (Op.Parameter, c.Effect, c.Slot, c.Volume);
                    break;
                case Op.Bypass:
                    ref var bypass = ref Wait(at);
                    (bypass.Op, bypass.Effect, bypass.Flag) = (Op.Bypass, c.Effect, c.Paused);
                    break;
            }
            c.Clip = null;
            c.Feed = null;
            c.Graph = null;
            c.Effect = null;
            tail = (tail + 1) % QueueSize;
        }
        Volatile.Write(ref _tail, tail);
    }

    private enum Op : byte { Start, Stop, Set, Pause, Seek, Bus, BusFade, Graph, Parameter, Bypass }

    /// <summary>What of a voice the queue had no room for, kept on the main thread until it has.</summary>
    [Flags]
    private enum Unsent : byte { None = 0, Values = 1, Pause = 2, Stop = 4 }

    private struct Command
    {
        public Op Op;
        public int Slot, Bus;
        public long Frame, Fade, LoopStart, LoopEnd;
        public AudioClip? Clip;
        public StreamFeed? Feed;
        public Graph? Graph;
        public AudioEffect? Effect;
        public double Step, Position;
        public float Volume, Left, Right;
        public bool Loop, Paused;
    }

    /// <summary>
    /// The buses' effect chains as the render function sees them, made on the main thread and never changed after: a
    /// new one replaces it at its frame. Indexed by bus.
    /// </summary>
    private sealed class Graph
    {
        public static readonly Graph Empty = new();
        public readonly AudioEffect[]?[] Chains = new AudioEffect[MaxBuses][];
        /// <summary>For each effect of a chain, the bus it listens to, or -1.</summary>
        public readonly int[]?[] Keys = new int[MaxBuses][];
        /// <summary>The submix buffers of the buses that have one ("master" mixes in the output itself).</summary>
        public readonly float[]?[] Buffers = new float[MaxBuses][];
        /// <summary>Buses mixed in their own buffer: they have effects, or an effect listens to them.</summary>
        public readonly bool[] Submix = new bool[MaxBuses];
        /// <summary>Every bus, each after the buses that play into it and the ones its effects listen to.</summary>
        public int[] Order = [];
        /// <summary>Effects the render function has not seen: it clears them and gives them these values.</summary>
        public AudioEffect[] Entering = [];
        public bool[] EnteringBypass = [];
        public float[][] EnteringValues = [];
    }

    /// <summary>A change to the graph waiting for its frame.</summary>
    private struct Event
    {
        public long At, Frames;
        public Op Op;
        public int Index;
        public float Value;
        public bool Flag;
        public Graph? Graph;
        public AudioEffect? Effect;
    }

    /// <summary>A voice as the sink's thread renders it.</summary>
    private struct Voice
    {
        public bool Active, Loop, Paused;
        public AudioClip? Clip;
        public StreamFeed? Feed;
        public double Position, Step;
        public float Volume, Left, Right;
        public int Bus;
        // The frames of the clip a loop repeats: from LoopStart to before LoopEnd.
        public int LoopStart, LoopEnd;
        public long Start, StopAt, Fade;
        // Ramps: what each value still has to move per frame, where it ends, and the frames left.
        public bool Ramping;
        public float VolumeStep, VolumeTarget, LeftStep, RightStep, LeftTarget, RightTarget;
        public double StepDelta, StepTarget;
        public int VolumeLeft, PanLeft, StepLeft;
        // The changes waiting for their frames, oldest first.
        public int Changes;
        public WaitingList Pending;
    }

    /// <summary>A change to a voice waiting for its frame: the values it sets (a negative volume or left gain, no pause and no step keep what the voice has).</summary>
    private struct Change
    {
        public long At;
        public float Volume, Left, Right;
        public int Pause;
        public double Step;
        // A seek: the clip frame to go on from (negative: none), or the feed that takes a stream from its new place.
        public double Seek;
        public StreamFeed? Feed;
        // Frames each value takes to get there; 0: at once.
        public int VolumeRamp, PanRamp, StepRamp;
    }

    /// <summary>The waiting changes of a voice, inside it: nothing is allocated for them while rendering.</summary>
    [System.Runtime.CompilerServices.InlineArray(WaitingChanges)]
    private struct WaitingList
    {
        private Change _first;
    }

    /// <summary>Changes of a sound's motion the timeline remembers.</summary>
    private const int MotionsKept = 8;

    /// <summary>How a sound moves from a frame on: where it is there, its speed and what is left of a pitch ramp, or that it stands still.</summary>
    private struct Motion
    {
        public long Frame, StepRamp;
        public double Position, Step, StepTarget;
        public bool Still;
    }

    [System.Runtime.CompilerServices.InlineArray(MotionsKept)]
    private struct MotionList
    {
        private Motion _first;
    }

    /// <summary>A voice as the main thread's policy sees it.</summary>
    private struct Logical
    {
        public uint Generation;
        public bool Active, Paused, Loop;
        public long Start, End, Remaining, Order;
        public int Priority;
        public StreamFeed? Feed;
        // Live pitch: the clip's frames, its rate over the output's, its speed now, and its position at PositionFrame.
        public long Frames, PositionFrame, Limit;
        public double Rate, Step, Position;
        // The sound's own rate, the frames a loop repeats (LoopEnd 0: not known, it never wraps here), and where in
        // the stream its current feed began.
        public int SampleRate;
        public long LoopStart, LoopEnd;
        public double FeedBase;
        // A stream on its way to a place sought (Seeking, to SeekTarget), and one that stands still until it gets
        // there because it has not begun (Held).
        public bool Held, Seeking;
        public double SeekTarget;
        // How the sound moved lately, oldest first: where it was at an earlier frame is read from these.
        public int MotionCount;
        public MotionList Motions;
        // A pitch ramp: the speed it ends at and the frames it still takes from PositionFrame (0: none).
        public double StepTarget;
        public long StepRamp;
        // What the render function was last asked for, to send again if the queue had no room for it.
        public float Volume, Left, Right;
        public long StopFrame, StopFade;
        public Unsent Unsent;
    }
}

/// <summary>What <see cref="AudioEngine.RenderAllocations"/> counted.</summary>
/// <param name="Renders">Render calls so far.</param>
/// <param name="Bytes">Bytes the rendering thread allocated.</param>
internal readonly record struct RenderAllocationCount(long Renders, long Bytes);

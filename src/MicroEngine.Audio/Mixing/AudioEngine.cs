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
/// A named mix stage: the sounds and buses under it are scaled by its <see cref="Volume"/>. Buses form a tree under
/// "master": a bus is created under a parent that already exists and is never moved, so there is no cycle.
/// </summary>
public sealed class AudioBus
{
    private readonly AudioEngine _engine;
    private float _volume = 1;

    internal AudioBus(AudioEngine engine, int index, string name, AudioBus? parent) => (_engine, Index, Name, Parent) = (engine, index, name, parent);

    internal int Index { get; }
    /// <summary>The bus's name.</summary>
    public string Name { get; }
    /// <summary>The bus it plays into; null for "master".</summary>
    public AudioBus? Parent { get; }
    /// <summary>
    /// Gain of everything on the bus, clamped to 0..4; it changes at the frame of the current step. A value that is not
    /// a number is ignored.
    /// </summary>
    public float Volume
    {
        get => _volume;
        set
        {
            if (float.IsNaN(value)) return;
            _volume = Math.Clamp(value, 0, 4);
            _engine.SetBusVolume(Index, _volume);
        }
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
    private long _step, _offset, _played, _position;
    // Frames the output did not play: the main thread adds to _skipped, the sink's thread follows it with _skew.
    private long _skipped, _skew;
    // The largest block the sink has asked for (written by its thread): a sink is not taken as stopped between two blocks.
    private int _largestBlock;
    // Real time: the sink's latency, the smallest lead of the timeline over the device in the steps of the current host
    // frame, whether a host frame ended since, and when the sink last rendered anything: what it had rendered, the step
    // that saw it, and the host frames ended since.
    private readonly int _sinkLatency;
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
        _logical = new Logical[MaxVoices];
        _voices = new Voice[MaxVoices];
        _incoming = new Voice[MaxVoices];
        _hasIncoming = new bool[MaxVoices];
        CreateBus(MasterBus, null);
        CreateBus(SfxBus, _buses[0]);
        CreateBus(MusicBus, _buses[0]);
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
        var bus = new AudioBus(this, _buses.Count, name, parent);
        _busParent[bus.Index] = parent?.Index ?? -1;
        _busVolume[bus.Index] = 1;
        _buses.Add(bus);
        _busNames.Add(name, bus);
        return bus;
    }

    internal void SetBusVolume(int bus, float volume)
    {
        Anchor();
        if (Flush() && TryPost(new Command { Op = Op.Bus, Slot = bus, Volume = volume, Frame = Now })) return;
        DroppedCommands++;
        if (!_busUnsent[bus]) _unsent++;
        _busUnsent[bus] = true;
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
        long length = parameters.Loop || frames == 0 ? (frames == 0 ? 0 : long.MaxValue) : (long)Math.Ceiling(frames / step);
        return Start(parameters, length, clip, null, step);
    }

    /// <summary>Plays <paramref name="stream"/> (music decoded while it plays); the engine disposes it when the sound ends or is stopped.</summary>
    /// <exception cref="KeyNotFoundException">The bus does not exist.</exception>
    public VoiceHandle Play(IAudioStream stream, in PlayParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (stream.Channels is not (1 or 2) || stream.SampleRate <= 0) throw new ArgumentException("A stream has 1 or 2 channels at a positive rate.", nameof(stream));
        var feed = new StreamFeed(stream, parameters.Loop, _rate, _channels, StreamAheadFrames);
        var handle = Start(parameters, long.MaxValue, null, feed, 1);
        if (handle.IsNone) { stream.Dispose(); return handle; }
        feed.Slot = handle.Index;
        feed.Generation = handle.Generation;
        _feeds.Add(feed);
        return handle;
    }

    private VoiceHandle Start(in PlayParameters p, long length, AudioClip? clip, StreamFeed? feed, double step)
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
        long at = Now;
        int slot = Choose(at, p.Priority);
        if (slot < 0)
        {
            RefusedSounds++;
            return VoiceHandle.None;
        }
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
        l.Frames = clip is null ? 0 : clip.Samples.Length / clip.Channels;
        l.Rate = clip is null ? 1 : (double)clip.SampleRate / _rate;
        l.Step = step;
        l.Position = 0;
        l.PositionFrame = at;
        l.Limit = long.MaxValue;
        l.Volume = float.IsNaN(p.Volume) ? 1 : Math.Clamp(p.Volume, 0, 1);
        (l.Left, l.Right) = Balance(p.Pan);
        TryPost(new Command
        {
            Op = Op.Start, Slot = slot, Frame = at, Clip = clip, Feed = feed, Step = step, Loop = p.Loop, Bus = bus.Index,
            Volume = l.Volume, Left = l.Left, Right = l.Right,
        });
        return new VoiceHandle(slot, l.Generation);
    }

    /// <summary>A free voice, or the one the policy steals; -1 when the new sound is refused.</summary>
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
        long fade = Math.Max(0, (long)Math.Round(fadeSeconds * _rate));
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
    /// value that is not a number, keeps a value.
    /// </summary>
    /// <returns>Whether it was playing.</returns>
    public bool Set(VoiceHandle voice, float? volume = null, float? pan = null)
    {
        Anchor();
        if (!IsPlaying(voice)) return false;
        var (left, right) = pan is { } p && !float.IsNaN(p) ? Balance(p) : (-1f, -1f);
        float gain = volume is { } v && !float.IsNaN(v) ? Math.Clamp(v, 0, 1) : -1;
        if (gain < 0 && left < 0) return true;
        ref var l = ref _logical[voice.Index];
        if (gain >= 0) l.Volume = gain;
        if (left >= 0) (l.Left, l.Right) = (left, right);
        Send(new Command { Op = Op.Set, Slot = voice.Index, Frame = Now, Volume = gain, Left = left, Right = right }, Unsent.Values);
        return true;
    }

    /// <summary>
    /// Changes the pitch of <paramref name="voice"/> from the current step's frame: the playback speed factor, 1 for the
    /// clip's own pitch, clamped to 0.01..100 as when a sound starts. The sound goes on from where it is, and the
    /// mixer's timeline moves its end accordingly. Streams play at their own pitch. A pitch that is not a number
    /// changes nothing.
    /// </summary>
    /// <returns>Whether it was playing and is a clip; false for a stale handle or a stream.</returns>
    public bool SetPitch(VoiceHandle voice, float pitch)
    {
        Anchor();
        if (!IsPlaying(voice)) return false;
        ref var l = ref _logical[voice.Index];
        if (l.Feed is not null) return false;
        if (float.IsNaN(pitch)) return true;
        long now = Now;
        // Where the sound is on the timeline: it advanced at the old speed since the last change, unless paused.
        if (!l.Paused) l.Position += Math.Max(0, now - l.PositionFrame) * l.Step;
        l.PositionFrame = Math.Max(now, l.PositionFrame);
        l.Step = l.Rate * Speed(pitch);
        if (!l.Loop && l.Frames > 0)
        {
            long left = Math.Max(0, (long)Math.Ceiling((l.Frames - l.Position) / l.Step));
            if (l.Paused) l.Remaining = left;
            else l.End = Math.Min(now + left, l.Limit);
        }
        Send(new Command { Op = Op.Set, Slot = voice.Index, Frame = now, Volume = -1, Left = -1, Right = -1, Step = l.Step }, Unsent.Values);
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
        if (paused) l.Position += Math.Max(0, now - l.PositionFrame) * l.Step;
        l.PositionFrame = Math.Max(now, l.PositionFrame);
        if (paused)
        {
            l.Remaining = l.End == long.MaxValue ? long.MaxValue : l.End - Math.Max(now, l.Start);
            l.End = long.MaxValue;
        }
        else l.End = l.Remaining == long.MaxValue ? long.MaxValue : now + l.Remaining;
        l.Paused = paused;
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
            feed.Fill();
            // Its end is known once decoding reaches it; the mixer's timeline learns it here.
            if (feed.Finished && l.End == long.MaxValue && !l.Paused) l.End = l.Start + feed.Produced;
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
                && !TryPost(new Command { Op = Op.Set, Slot = slot, Frame = now, Volume = l.Volume, Left = l.Left, Right = l.Right, Step = l.Feed is null ? l.Step : 0 })) return false;
            l.Unsent &= ~Unsent.Values;
            // A resume is sent even when the sound has since ended on the timeline: the render function still holds it
            // paused, and plays what is left as it would have had the resume been queued.
            if ((l.Unsent & Unsent.Pause) != 0 && !TryPost(new Command { Op = Op.Pause, Slot = slot, Frame = now, Paused = l.Paused })) return false;
            l.Unsent &= ~Unsent.Pause;
            if ((l.Unsent & Unsent.Stop) != 0 && !TryPost(new Command { Op = Op.Stop, Slot = slot, Frame = l.StopFrame, Fade = l.StopFade })) return false;
            l.Unsent = Unsent.None;
            _unsent--;
        }
        for (int bus = 0; bus < _buses.Count; bus++)
        {
            if (!_busUnsent[bus]) continue;
            if (!TryPost(new Command { Op = Op.Bus, Slot = bus, Volume = _buses[bus].Volume, Frame = now })) return false;
            _busUnsent[bus] = false;
            _unsent--;
        }
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
        // Bus volumes change at their exact frames: the block is mixed in segments between those frames.
        int done = 0;
        while (done < frames)
        {
            long segmentStart = start + done, segmentEnd = start + frames;
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
            for (int b = 0; b < MaxBuses; b++)
                _busGain[b] = _busVolume[b] * (_busParent[b] >= 0 ? _busGain[_busParent[b]] : 1);
            int count = (int)(segmentEnd - segmentStart);
            var segment = buffer.Slice(done * _channels, count * _channels);
            for (int v = 0; v < _voices.Length; v++)
                if (_voices[v].Active || _hasIncoming[v]) Mix(v, segment, segmentStart, count);
            done += count;
        }
        Volatile.Write(ref _position, rendered + frames);
    }

    /// <summary>
    /// Passes the frames from <paramref name="from"/> to <paramref name="to"/> without mixing them (the output did not
    /// play them): every voice ends up as if they had been rendered. Sounds that start, change, stop or end in them do
    /// so at their frames; a clip moves on by the frames it would have played; a stream stays where it is.
    /// </summary>
    private void Skip(long from, long to)
    {
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
                long end = voice.StopAt == long.MaxValue ? long.MaxValue : voice.StopAt + voice.Fade;
                if (pos >= end) { Silence(ref voice); break; }
                long next = Math.Min(to, end);
                if (voice.Changes > 0) next = Math.Min(next, voice.Pending[0].At);
                if (!voice.Paused && voice.Clip is { } clip)
                {
                    int length = clip.Samples.Length / clip.Channels;
                    voice.Position += (next - pos) * voice.Step;
                    if (voice.Loop && length > 0) voice.Position %= length;
                    else if (voice.Position >= length) { Silence(ref voice); break; }
                }
                pos = next;
            }
        }
    }

    /// <summary>The newest sound of a voice: the one waiting for its start frame, if any.</summary>
    private ref Voice Target(int slot) => ref _hasIncoming[slot] ? ref _incoming[slot] : ref _voices[slot];

    private void Mix(int slot, Span<float> buffer, long start, int frames)
    {
        ref var voice = ref _voices[slot];
        float busGain = _busGain[voice.Bus];
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
        if (voice.Loop && voice.Position >= length) voice.Position %= length;
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
        if (change.Volume >= 0) voice.Volume = change.Volume;
        if (change.Left >= 0) (voice.Left, voice.Right) = (change.Left, change.Right);
        if (change.Pause != 0) voice.Paused = change.Pause > 0;
        if (change.Step > 0) voice.Step = change.Step;
        voice.Changes--;
        for (int i = 0; i < voice.Changes; i++) voice.Pending[i] = voice.Pending[i + 1];
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
                if (into.Volume < 0) into.Volume = from.Volume;
                if (into.Left < 0) (into.Left, into.Right) = (from.Left, from.Right);
                if (into.Pause == 0) into.Pause = from.Pause;
                if (into.Step <= 0) into.Step = from.Step;
                voice.Changes--;
                for (int k = join; k < voice.Changes; k++) pending[k] = pending[k + 1];
            }
        }
        ref var added = ref pending[voice.Changes++];
        added = new Change { At = at, Volume = -1, Left = -1 };
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
                        Left = c.Left, Right = c.Right, Start = at, StopAt = long.MaxValue,
                    };
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
                    // Changes of one frame (a step's volume, pan and pitch) wait together for it; those of other
                    // steps wait each for its own frame.
                    ref var change = ref Waiting(ref Target(c.Slot), at);
                    if (c.Op == Op.Set)
                    {
                        if (c.Volume >= 0) change.Volume = c.Volume;
                        if (c.Left >= 0) (change.Left, change.Right) = (c.Left, c.Right);
                        if (c.Step > 0) change.Step = c.Step;
                    }
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
            }
            c.Clip = null;
            c.Feed = null;
            tail = (tail + 1) % QueueSize;
        }
        Volatile.Write(ref _tail, tail);
    }

    private enum Op : byte { Start, Stop, Set, Pause, Bus }

    /// <summary>What of a voice the queue had no room for, kept on the main thread until it has.</summary>
    [Flags]
    private enum Unsent : byte { None = 0, Values = 1, Pause = 2, Stop = 4 }

    private struct Command
    {
        public Op Op;
        public int Slot, Bus;
        public long Frame, Fade;
        public AudioClip? Clip;
        public StreamFeed? Feed;
        public double Step;
        public float Volume, Left, Right;
        public bool Loop, Paused;
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
        public long Start, StopAt, Fade;
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
    }

    /// <summary>The waiting changes of a voice, inside it: nothing is allocated for them while rendering.</summary>
    [System.Runtime.CompilerServices.InlineArray(WaitingChanges)]
    private struct WaitingList
    {
        private Change _first;
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

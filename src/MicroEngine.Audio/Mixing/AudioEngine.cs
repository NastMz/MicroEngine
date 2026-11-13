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
    /// <summary>Volume, 0..1 (clamped).</summary>
    public float Volume { get; init; } = 1;
    /// <summary>Playback speed factor, 1 for the original pitch (clamped to 0.01..100). Streams play at their own pitch.</summary>
    public float Pitch { get; init; } = 1;
    /// <summary>Stereo position, 0 (left) to 1 (right); 0.5 is centred. Balance panning: the far side is attenuated.</summary>
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
    /// <summary>Gain of everything on the bus, clamped to 0..4; it changes at the frame of the current step.</summary>
    public float Volume
    {
        get => _volume;
        set
        {
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

    private readonly IAudioSink _sink;
    private readonly int _channels, _rate, _updateRate;
    private readonly Logical[] _logical;
    private readonly Voice[] _voices, _incoming;
    private readonly bool[] _hasIncoming;
    private readonly long[] _busPendingAt = new long[MaxBuses];
    private readonly float[] _busPendingVolume = new float[MaxBuses];
    private readonly Command[] _queue = new Command[QueueSize];
    private int _head, _tail;
    private readonly List<AudioBus> _buses = [];
    private readonly Dictionary<string, AudioBus> _busNames = new(StringComparer.Ordinal);
    private readonly int[] _busParent = new int[MaxBuses];
    private readonly float[] _busVolume = new float[MaxBuses], _busGain = new float[MaxBuses];
    private readonly List<StreamFeed> _feeds = [];
    private long _step, _offset, _played, _position;
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
        DelayFrames = Math.Max(Math.Max(0, sink.LatencyFrames), settings.DelayFrames ?? 0);
        StreamAheadFrames = Math.Max(1024, (int)(settings.StreamAheadSeconds * _rate));
        _logical = new Logical[MaxVoices];
        _voices = new Voice[MaxVoices];
        _incoming = new Voice[MaxVoices];
        _hasIncoming = new bool[MaxVoices];
        Array.Fill(_busPendingAt, long.MaxValue);
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
    /// <summary>The output frame the current step's sounds start at (the step's frame plus <see cref="DelayFrames"/>).</summary>
    public long Now => StepFrame(_step) + _offset + DelayFrames;
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

    internal void SetBusVolume(int bus, float volume) => Post(new Command { Op = Op.Bus, Slot = bus, Volume = volume, Frame = Now });

    // ---- Playing

    /// <summary>Plays <paramref name="clip"/> from the current step's frame; returns its handle, or <see cref="VoiceHandle.None"/> when the voice policy refused it.</summary>
    /// <exception cref="KeyNotFoundException">The bus does not exist.</exception>
    public VoiceHandle Play(AudioClip clip, in PlayParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(clip);
        float pitch = Math.Clamp(parameters.Pitch, 0.01f, 100f);
        double step = (double)clip.SampleRate / _rate * pitch;
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
        var (left, right) = Balance(p.Pan);
        Post(new Command
        {
            Op = Op.Start, Slot = slot, Frame = at, Clip = clip, Feed = feed, Step = step, Loop = p.Loop, Bus = bus.Index,
            Volume = Math.Clamp(p.Volume, 0, 1), Left = left, Right = right,
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

    private static (float Left, float Right) Balance(float pan)
    {
        pan = Math.Clamp(pan, 0, 1);
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
        if (!IsPlaying(voice)) return false;
        long fade = Math.Max(0, (long)Math.Round(fadeSeconds * _rate));
        ref var l = ref _logical[voice.Index];
        l.Limit = Math.Min(l.Limit, Now + fade);
        l.End = Math.Min(l.End, l.Limit);
        if (fade == 0) Release(voice.Index);
        Post(new Command { Op = Op.Stop, Slot = voice.Index, Frame = Now, Fade = fade });
        return true;
    }

    /// <summary>Changes the volume (0..1) and pan (0..1) of <paramref name="voice"/> from the current step's frame; null keeps a value.</summary>
    /// <returns>Whether it was playing.</returns>
    public bool Set(VoiceHandle voice, float? volume = null, float? pan = null)
    {
        if (!IsPlaying(voice)) return false;
        var (left, right) = pan is { } p ? Balance(p) : (-1f, -1f);
        Post(new Command { Op = Op.Set, Slot = voice.Index, Frame = Now, Volume = volume is { } v ? Math.Clamp(v, 0, 1) : -1, Left = left, Right = right });
        return true;
    }

    /// <summary>
    /// Changes the pitch of <paramref name="voice"/> from the current step's frame: the playback speed factor, 1 for the
    /// clip's own pitch, clamped to 0.01..100 as when a sound starts. The sound goes on from where it is, and the
    /// mixer's timeline moves its end accordingly. Streams play at their own pitch.
    /// </summary>
    /// <returns>Whether it was playing and is a clip; false for a stale handle or a stream.</returns>
    public bool SetPitch(VoiceHandle voice, float pitch)
    {
        if (!IsPlaying(voice)) return false;
        ref var l = ref _logical[voice.Index];
        if (l.Feed is not null) return false;
        long now = Now;
        // Where the sound is on the timeline: it advanced at the old speed since the last change, unless paused.
        if (!l.Paused) l.Position += (now - l.PositionFrame) * l.Step;
        l.PositionFrame = now;
        l.Step = l.Rate * Math.Clamp(pitch, 0.01f, 100f);
        if (!l.Loop && l.Frames > 0)
        {
            long left = Math.Max(0, (long)Math.Ceiling((l.Frames - l.Position) / l.Step));
            if (l.Paused) l.Remaining = left;
            else l.End = Math.Min(now + left, l.Limit);
        }
        Post(new Command { Op = Op.Set, Slot = voice.Index, Frame = now, Volume = -1, Left = -1, Right = -1, Step = l.Step });
        return true;
    }

    /// <summary>Pauses (true) or resumes (false) <paramref name="voice"/> from the current step's frame; a paused sound keeps its voice.</summary>
    /// <returns>Whether it was playing.</returns>
    public bool Pause(VoiceHandle voice, bool paused)
    {
        if (!IsPlaying(voice)) return false;
        ref var l = ref _logical[voice.Index];
        if (l.Paused == paused) return true;
        long now = Now;
        if (paused) l.Position += (now - l.PositionFrame) * l.Step;
        l.PositionFrame = now;
        if (paused)
        {
            l.Remaining = l.End == long.MaxValue ? long.MaxValue : l.End - now;
            l.End = long.MaxValue;
        }
        else l.End = l.Remaining == long.MaxValue ? long.MaxValue : now + l.Remaining;
        l.Paused = paused;
        Post(new Command { Op = Op.Pause, Slot = voice.Index, Frame = now, Paused = paused });
        return true;
    }

    // ---- Time

    /// <summary>Ends a simulation step: sounds played from now on start one step later. The game calls it once per fixed step.</summary>
    public void Advance()
    {
        _step++;
        if (Deterministic) return;
        // Real time: when the sink has already played past this step's frame (the game fell behind), catch up so sounds
        // keep their delay; when it is far behind (a sink that has not started), wait for it.
        long rendered = RenderedFrames, frame = StepFrame(_step) + _offset;
        if (frame < rendered || frame > rendered + _rate) _offset += rendered - frame;
        for (int i = 0; i < _logical.Length; i++) if (_logical[i].Active && _logical[i].End <= Now) Release(i);
    }

    /// <summary>
    /// The host frame's audio work, on the main thread outside the simulation step: decodes music streams ahead, frees
    /// streams that ended, then pumps the sink (which renders here when it is pumped by the host).
    /// </summary>
    public void Pump()
    {
        if (_disposed) return;
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

    private void Post(in Command command)
    {
        int next = (_head + 1) % QueueSize;
        if (next == Volatile.Read(ref _tail))
        {
            DroppedCommands++;
            return;
        }
        _queue[_head] = command;
        Volatile.Write(ref _head, next);
    }

    /// <summary>Commands lost because the sink stopped reading them (its queue was full).</summary>
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
        long start = _position;
        Drain(start);
        // Bus volumes change at their exact frames: the block is mixed in segments between those frames.
        int done = 0;
        while (done < frames)
        {
            long segmentStart = start + done, segmentEnd = start + frames;
            for (int b = 0; b < MaxBuses; b++)
            {
                long at = _busPendingAt[b];
                if (at == long.MaxValue) continue;
                if (at <= segmentStart)
                {
                    _busVolume[b] = _busPendingVolume[b];
                    _busPendingAt[b] = long.MaxValue;
                }
                else segmentEnd = Math.Min(segmentEnd, at);
            }
            for (int b = 0; b < MaxBuses; b++)
                _busGain[b] = _busVolume[b] * (_busParent[b] >= 0 ? _busGain[_busParent[b]] : 1);
            int count = (int)(segmentEnd - segmentStart);
            var segment = buffer.Slice(done * _channels, count * _channels);
            for (int v = 0; v < _voices.Length; v++)
                if (_voices[v].Active || _hasIncoming[v]) Mix(v, segment, segmentStart, count);
            done += count;
        }
        Volatile.Write(ref _position, start + frames);
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
            if (voice.PendingAt != long.MaxValue && pos >= voice.PendingAt) ApplyPending(ref voice);
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

    private static void ApplyPending(ref Voice voice)
    {
        if (voice.PendingVolume >= 0) voice.Volume = voice.PendingVolume;
        if (voice.PendingLeft >= 0) (voice.Left, voice.Right) = (voice.PendingLeft, voice.PendingRight);
        if (voice.PendingPause != 0) voice.Paused = voice.PendingPause > 0;
        if (voice.PendingStep > 0) voice.Step = voice.PendingStep;
        voice.PendingAt = long.MaxValue;
        voice.PendingVolume = voice.PendingLeft = -1;
        voice.PendingPause = 0;
        voice.PendingStep = 0;
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
                        Left = c.Left, Right = c.Right, Start = at, StopAt = long.MaxValue, PendingAt = long.MaxValue,
                        PendingVolume = -1, PendingLeft = -1,
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
                    ref var changing = ref Target(c.Slot);
                    // Changes of one frame (a step's volume, pan and pitch) wait together for it; an earlier one is due.
                    if (changing.PendingAt != long.MaxValue && changing.PendingAt != at) ApplyPending(ref changing);
                    changing.PendingAt = at;
                    if (c.Op == Op.Set)
                    {
                        if (c.Volume >= 0) changing.PendingVolume = c.Volume;
                        if (c.Left >= 0) (changing.PendingLeft, changing.PendingRight) = (c.Left, c.Right);
                        if (c.Step > 0) changing.PendingStep = c.Step;
                    }
                    else changing.PendingPause = c.Paused ? 1 : -1;
                    break;
                case Op.Bus:
                    if (_busPendingAt[c.Slot] != long.MaxValue) _busVolume[c.Slot] = _busPendingVolume[c.Slot];
                    _busPendingAt[c.Slot] = at;
                    _busPendingVolume[c.Slot] = c.Volume;
                    break;
            }
            c.Clip = null;
            c.Feed = null;
            tail = (tail + 1) % QueueSize;
        }
        Volatile.Write(ref _tail, tail);
    }

    private enum Op : byte { Start, Stop, Set, Pause, Bus }

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
        public long PendingAt;
        public float PendingVolume, PendingLeft, PendingRight;
        public int PendingPause;
        public double PendingStep;
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
    }
}

/// <summary>What <see cref="AudioEngine.RenderAllocations"/> counted.</summary>
/// <param name="Renders">Render calls so far.</param>
/// <param name="Bytes">Bytes the rendering thread allocated.</param>
internal readonly record struct RenderAllocationCount(long Renders, long Bytes);

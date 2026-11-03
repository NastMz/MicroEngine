using System.Diagnostics;

namespace P5.Audio;

/// <summary>
/// A small managed mixer standing in for the engine mixer: a continuous bed tone (so a dropout is audible and
/// measurable) plus short tone "sounds" posted from the main thread through a single-producer/single-consumer ring.
/// Render runs on whatever thread the sink uses; nothing it does allocates or locks.
/// </summary>
public sealed class ProbeMixer
{
    private const int MaxVoices = 32;
    private const int RingSize = 256;

    private struct Voice
    {
        public bool Active;
        public double Phase, Step;
        public float Gain;
        public long Start, End;   // absolute output frames
        public int Id;
        public long PostedTicks;
        public bool Reported;
    }

    private struct Command
    {
        public float Frequency, Gain;
        public int Frames, Id;
        public long AtFrame;       // < 0: start at the next rendered block
        public long PostedTicks;
    }

    private readonly Voice[] _voices = new Voice[MaxVoices];
    private readonly Command[] _ring = new Command[RingSize];
    private int _head, _tail;      // producer writes _head, consumer writes _tail
    private long _position;        // frames rendered so far
    private double _bedPhase;
    private readonly int _rate, _channels;
    private readonly double _bedStep;

    public ProbeMixer(AudioFormat format, float bedGain = 0.15f, float bedFrequency = 330)
    {
        _rate = format.SampleRate;
        _channels = format.Channels;
        BedGain = bedGain;
        _bedStep = 2 * Math.PI * bedFrequency / _rate;
    }

    public float BedGain { get; set; }
    public RenderStats Stats { get; } = new();
    public LatencyLog Latency { get; } = new();
    /// <summary>Frames rendered so far (the audio clock as the mixer sees it).</summary>
    public long Position => Volatile.Read(ref _position);
    /// <summary>
    /// Measurement aid: the last channel carries the output frame number instead of audio ((frame mod 2^20) + 1) / 2^20,
    /// exact in float32), so a tap at the output can tell inserted silence, repeated or skipped audio from the signal.
    /// </summary>
    public bool ClockChannel { get; set; }
    /// <summary>Extra voices for the CPU-cost run: each is a continuous tone.</summary>
    public int LoadVoices { get; set; }

    /// <summary>Main thread: start a tone. <paramref name="atFrame"/> stamps it to an exact output frame (deterministic); -1 = next block.</summary>
    public bool Play(float frequency, float gain, int frames, int id, long atFrame = -1)
    {
        int head = _head;
        int next = (head + 1) % RingSize;
        if (next == Volatile.Read(ref _tail)) return false; // full
        _ring[head] = new Command { Frequency = frequency, Gain = gain, Frames = frames, Id = id, AtFrame = atFrame, PostedTicks = Stopwatch.GetTimestamp() };
        Volatile.Write(ref _head, next);
        return true;
    }

    /// <summary>The <see cref="AudioRender"/> the sinks call.</summary>
    public void Render(Span<float> buffer, int aheadFrames)
    {
        long t0 = Stopwatch.GetTimestamp();
        int frames = buffer.Length / _channels;
        long start = _position;
        Drain(start);
        for (int f = 0; f < frames; f++)
        {
            long pos = start + f;
            double sample = BedGain * Math.Sin(_bedPhase);
            _bedPhase += _bedStep;
            if (_bedPhase > 2 * Math.PI) _bedPhase -= 2 * Math.PI;
            for (int v = 0; v < LoadVoices; v++) sample += 0.001 * Math.Sin(_bedPhase * (v + 2));
            for (int v = 0; v < MaxVoices; v++)
            {
                ref var voice = ref _voices[v];
                if (!voice.Active || pos < voice.Start) continue;
                if (pos >= voice.End) { voice.Active = false; continue; }
                if (!voice.Reported)
                {
                    voice.Reported = true;
                    Latency.Add(voice.Id, Stopwatch.GetTimestamp() - voice.PostedTicks, aheadFrames + f);
                }
                sample += voice.Gain * Math.Sin(voice.Phase);
                voice.Phase += voice.Step;
            }
            float s = (float)sample;
            for (int c = 0; c < _channels; c++) buffer[f * _channels + c] = s;
            if (ClockChannel) buffer[f * _channels + _channels - 1] = ((pos & 0xFFFFF) + 1) / 1048576f;
        }
        Volatile.Write(ref _position, start + frames);
        Stats.Record(t0, Stopwatch.GetTimestamp(), frames, aheadFrames);
    }

    private void Drain(long blockStart)
    {
        int tail = _tail;
        int head = Volatile.Read(ref _head);
        while (tail != head)
        {
            ref var c = ref _ring[tail];
            int slot = 0;
            long oldest = long.MaxValue;
            for (int v = 0; v < MaxVoices; v++)
            {
                if (!_voices[v].Active) { slot = v; oldest = long.MinValue; break; }
                if (_voices[v].Start < oldest) { oldest = _voices[v].Start; slot = v; } // steal the oldest: deterministic
            }
            long at = c.AtFrame >= 0 ? Math.Max(c.AtFrame, blockStart) : blockStart;
            _voices[slot] = new Voice
            {
                Active = true, Phase = 0, Step = 2 * Math.PI * c.Frequency / _rate, Gain = c.Gain,
                Start = at, End = at + c.Frames, Id = c.Id, PostedTicks = c.PostedTicks,
            };
            tail = (tail + 1) % RingSize;
        }
        Volatile.Write(ref _tail, tail);
    }
}

/// <summary>Per-render records in preallocated arrays (no allocation on the audio thread).</summary>
public sealed class RenderStats
{
    private const int Capacity = 1 << 16;
    public readonly long[] Start = new long[Capacity];
    public readonly long[] End = new long[Capacity];
    public readonly int[] Frames = new int[Capacity];
    public readonly int[] Ahead = new int[Capacity];
    public readonly int[] Thread = new int[Capacity];
    private int _count;
    public int Count => Math.Min(Volatile.Read(ref _count), Capacity);

    public void Record(long start, long end, int frames, int ahead)
    {
        int i = _count;
        if (i < Capacity)
        {
            Start[i] = start; End[i] = end; Frames[i] = frames; Ahead[i] = ahead;
            Thread[i] = Environment.CurrentManagedThreadId;
        }
        Volatile.Write(ref _count, i + 1);
    }
}

/// <summary>When each posted sound was first rendered: wall time from post to render, and frames queued in front of it.</summary>
public sealed class LatencyLog
{
    private const int Capacity = 4096;
    public readonly int[] Id = new int[Capacity];
    public readonly long[] WaitTicks = new long[Capacity];
    public readonly int[] AheadFrames = new int[Capacity];
    private int _count;
    public int Count => Math.Min(Volatile.Read(ref _count), Capacity);

    public void Add(int id, long waitTicks, int aheadFrames)
    {
        int i = _count;
        if (i < Capacity) { Id[i] = id; WaitTicks[i] = waitTicks; AheadFrames[i] = aheadFrames; }
        Volatile.Write(ref _count, i + 1);
    }
}

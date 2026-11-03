using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace P5.Audio;

/// <summary>
/// The measured run, driven one frame at a time so a blocking loop (desktop, Android) and the browser's
/// requestAnimationFrame drive it the same way. Timeline: steady play, a 100 ms main-thread stall at 2 s, a 300 ms stall
/// at 4 s, a forced full garbage collection over a large heap at 5.5 s, end at 7 s. A short tone is posted every 250 ms
/// (the "sound effect" whose latency is measured); the bed tone never stops, so any gap is a dropout.
/// </summary>
public sealed class Scenario
{
    public static readonly (double At, string Name)[] Phases = [(0, "steady"), (2.0, "stall100"), (4.0, "stall300"), (5.5, "gc")];
    private readonly IAudioSink _sink;
    private readonly ProbeMixer _mixer;
    private readonly Stopwatch _clock = new();
    private readonly double _seconds;
    private int _nextSound = 1;
    private double _nextSoundAt = 0.25;
    private bool _stall100, _stall300, _gc;
    private readonly long[] _starvedByPhase = new long[Phases.Length];
    private long _lastStarved;
    // Playback deficit: wall time that passed minus audio the device took from the sink, per phase. Over a phase it is
    // the audio that was not played (device starved, or its thread was not running), within one device period.
    private readonly double[] _deficitMsByPhase = new double[Phases.Length];
    private double _lastWallMs = double.NaN;
    private long _lastPlayed;
    private readonly double[] _frameMsByPhase = new double[Phases.Length];
    private double _lastFrame;
    public double Stall100Ms, Stall300Ms, GcMs, GcPauseMs;
    public int GcCount;
    public int MainThreadId { get; } = Environment.CurrentManagedThreadId;
    public long Frames;
    public long StartTicks { get; private set; }
    private object? _garbage;

    public Scenario(IAudioSink sink, ProbeMixer mixer, double seconds = 7)
    {
        _sink = sink; _mixer = mixer; _seconds = seconds;
    }

    public void Start()
    {
        _sink.Start(_mixer.Render);
        _clock.Start();
        StartTicks = Stopwatch.GetTimestamp();
    }

    /// <summary>One host frame. Returns false when the run is over.</summary>
    public bool Frame()
    {
        double t = _clock.Elapsed.TotalSeconds;
        int phase = PhaseOf(t);
        Frames++;
        long played = _sink.PlayedFrames;
        if (!double.IsNaN(_lastWallMs) && t > 0.5) // after start-up fill
            _deficitMsByPhase[phase] += (t * 1000 - _lastWallMs) - _sink.Format.FramesToMs(played - _lastPlayed);
        _lastWallMs = t * 1000;
        _lastPlayed = played;
        if (_lastFrame > 0) _frameMsByPhase[phase] = Math.Max(_frameMsByPhase[phase], (t - _lastFrame) * 1000);
        _lastFrame = t;
        if (!_stall100 && t >= 2.0) { _stall100 = true; Stall100Ms = Busy(100); }
        if (!_stall300 && t >= 4.0) { _stall300 = true; Stall300Ms = Busy(300); }
        if (!_gc && t >= 5.5)
        {
            _gc = true;
            var before = GC.GetTotalPauseDuration();
            int gen0 = GC.CollectionCount(0);
            GcMs = CollectBigHeap();
            GcPauseMs = (GC.GetTotalPauseDuration() - before).TotalMilliseconds;
            GcCount = GC.CollectionCount(0) - gen0;
        }
        while (t >= _nextSoundAt)
        {
            _mixer.Play(880, 0.2f, _sink.Format.SampleRate / 12, _nextSound++);
            _nextSoundAt += 0.25;
        }
        _sink.Pump();
        long starved = _sink.StarvedFrames;
        _starvedByPhase[phase] += starved - _lastStarved;
        _lastStarved = starved;
        return t < _seconds;
    }

    private static int PhaseOf(double t)
    {
        int p = 0;
        for (int i = 0; i < Phases.Length; i++) if (t >= Phases[i].At) p = i;
        return p;
    }

    /// <summary>A long frame: the main thread does not return for <paramref name="ms"/> (a load hitch, a slow level build).</summary>
    private static double Busy(double ms)
    {
        var w = Stopwatch.StartNew();
        double x = 0;
        while (w.Elapsed.TotalMilliseconds < ms) x += Math.Sqrt(x + 1);
        return w.Elapsed.TotalMilliseconds + (x < 0 ? 1 : 0);
    }

private sealed class Node { public Node? Next; public object? Payload; }

    /// <summary>
    /// A level load seen by the collector: a live graph of small objects (P5_GC_NODES, default 1.5 million, half with a
    /// payload) is built while 100 MB of short-lived arrays churn, then a blocking compacting gen-2 collection runs
    /// while the graph is still alive (it must be traced and moved). Every collection suspends every managed thread.
    /// </summary>
    private double CollectBigHeap()
    {
        int count = int.TryParse(Environment.GetEnvironmentVariable("P5_GC_NODES"), out int n) ? n : 1_500_000;
        Node? head = null;
        for (int i = 0; i < count; i++) head = new Node { Next = head, Payload = i % 2 == 0 ? new byte[32] : null };
        for (int i = 0; i < 100_000; i++) _garbage = new byte[1000];
        _garbage = head;
        var g = Stopwatch.StartNew();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        double ms = g.Elapsed.TotalMilliseconds;
        _garbage = null;
        GC.KeepAlive(head);
        return ms;
    }

    /// <summary>One JSON object with everything measured.</summary>
    public string Report(string platform, string extra = "")
    {
        var fmt = _sink.Format;
        var s = _mixer.Stats;
        int n = s.Count;
        var sb = new StringBuilder();
        sb.Append('{');
        Field(sb, "platform", platform);
        Field(sb, "sink", _sink.Describe());
        Field(sb, "driverKind", _sink.Driver.ToString());
        Field(sb, "rate", fmt.SampleRate);
        Field(sb, "devicePeriodFrames", _sink.DevicePeriodFrames);
        Field(sb, "devicePeriodMs", fmt.FramesToMs(_sink.DevicePeriodFrames));
        Field(sb, "targetBufferedMs", fmt.FramesToMs(_sink.TargetBufferedFrames));
        Field(sb, "hostFrames", Frames);
        Field(sb, "renderCalls", n);
        var threads = new HashSet<int>();
        for (int i = 0; i < n; i++) threads.Add(s.Thread[i]);
        Field(sb, "renderThreads", string.Join(",", threads));
        Field(sb, "renderOnMainThread", threads.Contains(MainThreadId));
        Field(sb, "mainThread", MainThreadId);
        // Per phase: render call intervals (thread-driven sinks: a late call is a dropout risk), frames per call, cost.
        sb.Append("\"phases\":{");
        for (int p = 0; p < Phases.Length; p++)
        {
            double from = Phases[p].At, to = p + 1 < Phases.Length ? Phases[p + 1].At : double.MaxValue;
            var gaps = new List<double>(); var cost = new List<double>(); var frames = new List<double>();
            for (int i = 0; i < n; i++)
            {
                double at = Ms(s.Start[i] - StartTicks) / 1000;
                if (at < Math.Max(from, 0.5) || at >= to) continue;   // the first 0.5 s is device start-up
                if (i > 0) gaps.Add(Ms(s.Start[i] - s.Start[i - 1]));
                cost.Add(Ms(s.End[i] - s.Start[i]));
                frames.Add(s.Frames[i]);
            }
            sb.Append('"').Append(Phases[p].Name).Append("\":{");
            Field(sb, "calls", cost.Count);
            Field(sb, "gapMsMedian", Pct(gaps, 0.5)); Field(sb, "gapMsMax", Pct(gaps, 1));
            Field(sb, "framesPerCallMedian", Pct(frames, 0.5)); Field(sb, "framesPerCallMax", Pct(frames, 1));
            Field(sb, "renderMsMedian", Pct(cost, 0.5)); Field(sb, "renderMsMax", Pct(cost, 1));
            // A thread-driven sink renders when the device asks; a call later than two device periods means the device
            // ran dry (the excess is the estimated gap). Pumped sinks are measured by starvedFrames instead.
            double late = 0; int lateCalls = 0;
            if (_sink.Driver == AudioSinkDriver.AudioThread && _sink.TargetBufferedFrames == 0 && _sink.DevicePeriodFrames > 0)
            {
                double limit = 2 * fmt.FramesToMs(_sink.DevicePeriodFrames);
                foreach (double g in gaps) if (g > limit) { lateCalls++; late += g - limit; }
            }
            Field(sb, "lateCalls", lateCalls);
            Field(sb, "lateExcessMs", late);
            Field(sb, "lostMs", _deficitMsByPhase[p]);
            Field(sb, "starvedFrames", _starvedByPhase[p]);
            Field(sb, "starvedMs", fmt.FramesToMs(_starvedByPhase[p]));
            Field(sb, "maxHostFrameMs", _frameMsByPhase[p], last: true);
            sb.Append(p + 1 < Phases.Length ? "}," : "}");
        }
        sb.Append("},");
        // Sound latency: wall time from Play to the render that contains its first frame, plus the frames queued in
        // front of it and the device period (the part of the path the sink knows; the OS mixer adds its own).
        var l = _mixer.Latency;
        var wait = new List<double>(); var total = new List<double>();
        for (int i = 0; i < l.Count; i++)
        {
            double w = Ms(l.WaitTicks[i]);
            wait.Add(w);
            total.Add(w + fmt.FramesToMs(l.AheadFrames[i] + _sink.DevicePeriodFrames));
        }
        Field(sb, "sounds", l.Count);
        Field(sb, "soundWaitMsMedian", Pct(wait, 0.5)); Field(sb, "soundWaitMsMax", Pct(wait, 1));
        Field(sb, "soundLatencyMsMedian", Pct(total, 0.5)); Field(sb, "soundLatencyMsMax", Pct(total, 1));
        Field(sb, "stall100Ms", Stall100Ms); Field(sb, "stall300Ms", Stall300Ms); Field(sb, "gcMs", GcMs); Field(sb, "gcPhasePauseMs", GcPauseMs); Field(sb, "gcPhaseGen0Count", GcCount);
        Field(sb, "starvedTotalFrames", _sink.StarvedFrames, last: extra.Length == 0);
        sb.Append(extra);
        sb.Append('}');
        return sb.ToString();
    }

    private static double Ms(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;

    private static double Pct(List<double> v, double q)
    {
        if (v.Count == 0) return double.NaN;
        v.Sort();
        return v[Math.Clamp((int)Math.Ceiling(q * v.Count) - 1, 0, v.Count - 1)];
    }

    public static void Field(StringBuilder sb, string name, object value, bool last = false)
    {
        sb.Append('"').Append(name).Append("\":");
        switch (value)
        {
            case string str: sb.Append('"').Append(str.Replace("\\", "\\\\").Replace("\"", "\\\"")).Append('"'); break;
            case bool b: sb.Append(b ? "true" : "false"); break;
            case double d: sb.Append(double.IsFinite(d) ? Math.Round(d, 3).ToString(CultureInfo.InvariantCulture) : "null"); break;
            default: sb.Append(Convert.ToString(value, CultureInfo.InvariantCulture)); break;
        }
        if (!last) sb.Append(',');
    }
}

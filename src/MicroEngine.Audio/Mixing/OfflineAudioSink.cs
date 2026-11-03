using MicroEngine.Platform;

namespace MicroEngine.Audio;

/// <summary>
/// The test sink: renders into memory with no device and no clock. Each <see cref="Pump"/> renders exactly one
/// simulation step of frames (the step's share of the rate, so 44100 Hz at 60 steps alternates 735-frame steps exactly),
/// in blocks of at most the block size, so the output depends only on what the game played, never on time.
/// </summary>
public sealed class OfflineAudioSink : IAudioSink
{
    private readonly int _updateRate, _blockFrames;
    private readonly float[] _block;
    private readonly List<float> _output = [];
    private AudioRender? _render;
    private long _steps;

    /// <summary>Creates an offline sink.</summary>
    /// <param name="format">The format to render.</param>
    /// <param name="updateRate">Simulation steps per second: one pump renders one step.</param>
    /// <param name="blockFrames">The largest block asked of the mixer at once.</param>
    /// <param name="latencyFrames">The latency it reports (0: sounds start at their step's first frame).</param>
    /// <exception cref="ArgumentOutOfRangeException">A count is out of range.</exception>
    public OfflineAudioSink(AudioFormat format, int updateRate = 60, int blockFrames = 1024, int latencyFrames = 0)
    {
        if (format.Channels is not (1 or 2) || format.SampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(format));
        ArgumentOutOfRangeException.ThrowIfLessThan(updateRate, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(blockFrames, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(latencyFrames);
        (Format, _updateRate, _blockFrames, LatencyFrames) = (format, updateRate, blockFrames, latencyFrames);
        _block = new float[blockFrames * format.Channels];
    }

    /// <inheritdoc/>
    public AudioFormat Format { get; }
    /// <summary><see cref="AudioSinkSchedule.HostPump"/>: rendering happens in <see cref="Pump"/>.</summary>
    public AudioSinkSchedule Schedule => AudioSinkSchedule.HostPump;
    /// <inheritdoc/>
    public int LatencyFrames { get; }
    /// <summary>Frames rendered so far.</summary>
    public long Frames => _output.Count / Format.Channels;
    /// <summary>Everything rendered so far, interleaved.</summary>
    public IReadOnlyList<float> Samples => _output;

    /// <inheritdoc/>
    public void Start(AudioRender render) => _render = render;

    /// <summary>Renders the next simulation step of frames.</summary>
    public void Pump()
    {
        if (_render is null) return;
        long end = (_steps + 1) * Format.SampleRate / _updateRate;
        _steps++;
        long left = end - Frames;
        while (left > 0)
        {
            int n = (int)Math.Min(left, _blockFrames);
            var span = _block.AsSpan(0, n * Format.Channels);
            _render(span);
            foreach (var s in span) _output.Add(s);
            left -= n;
        }
    }

    /// <summary>Stops rendering.</summary>
    public void Dispose() => _render = null;
}

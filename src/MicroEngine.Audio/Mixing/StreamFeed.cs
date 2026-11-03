namespace MicroEngine.Audio;

/// <summary>
/// A playing stream decoded ahead: the main thread (<see cref="AudioEngine.Pump"/>) decodes, converts to the output
/// rate and channels and writes into a ring; the sink's thread reads one frame at a time. Single producer, single
/// consumer, no lock, no allocation while reading.
/// </summary>
internal sealed class StreamFeed
{
    private readonly float[] _ring; // stereo output frames
    private readonly int _capacity;
    private long _written, _read;   // frames; the producer writes _written, the consumer _read
    private readonly float[] _decoded;
    private int _decodedFrames;
    private readonly double _ratio;
    private double _position;       // source frame of the next output frame, relative to _decoded
    private bool _sourceDone;
    private volatile bool _finished;

    public StreamFeed(IAudioStream source, bool loop, int outRate, int outChannels, int aheadFrames)
    {
        Source = source;
        Loop = loop;
        _capacity = aheadFrames;
        _ring = new float[aheadFrames * 2];
        _ratio = (double)source.SampleRate / outRate;
        _decoded = new float[4096 * source.Channels];
    }

    public IAudioStream Source { get; }
    public bool Loop { get; }
    public int Slot;
    public uint Generation;
    /// <summary>Output frames written so far (main thread).</summary>
    public long Produced => _written;
    /// <summary>Whether the stream ended and everything decoded was written (it never ends while looping).</summary>
    public bool Finished => _finished;

    /// <summary>Main thread: decodes until the ring holds its target.</summary>
    public void Fill()
    {
        if (_finished) return;
        int channels = Source.Channels;
        while (_written - Volatile.Read(ref _read) < _capacity)
        {
            // The two source frames around the next output frame.
            int i = (int)_position;
            while (i + 1 >= _decodedFrames && !_sourceDone)
            {
                if (!Decode(channels)) break;
                i = (int)_position;
            }
            if (i >= _decodedFrames)
            {
                _finished = true;
                return;
            }
            float t = (float)(_position - i);
            int j = Math.Min(i + 1, _decodedFrames - 1);
            float l0 = _decoded[i * channels], l1 = _decoded[j * channels];
            float r0 = channels == 2 ? _decoded[i * 2 + 1] : l0, r1 = channels == 2 ? _decoded[j * 2 + 1] : l1;
            long slot = _written % _capacity;
            _ring[slot * 2] = l0 + (l1 - l0) * t;
            _ring[slot * 2 + 1] = r0 + (r1 - r0) * t;
            Volatile.Write(ref _written, _written + 1);
            _position += _ratio;
        }
    }

    /// <summary>Keeps the last decoded frame and appends a new chunk; false when the source has ended (and does not loop).</summary>
    private bool Decode(int channels)
    {
        // Keep the last frame (the interpolation still needs it) at the front, then decode after it.
        int keep = 0;
        if (_decodedFrames > 0)
        {
            int last = _decodedFrames - 1;
            Array.Copy(_decoded, last * channels, _decoded, 0, channels);
            _position -= last;
            keep = 1;
        }
        int read = Source.Read(_decoded.AsSpan(keep * channels));
        if (read == 0 && Loop)
        {
            Source.Rewind();
            read = Source.Read(_decoded.AsSpan(keep * channels));
        }
        _decodedFrames = keep + read / channels;
        if (read == 0) _sourceDone = true;
        return read > 0;
    }

    /// <summary>Sink thread: the next frame, or false when none is decoded yet (or the stream ended).</summary>
    public bool TryRead(out float left, out float right)
    {
        long read = _read;
        if (read >= Volatile.Read(ref _written))
        {
            left = right = 0;
            return false;
        }
        long slot = read % _capacity;
        left = _ring[slot * 2];
        right = _ring[slot * 2 + 1];
        Volatile.Write(ref _read, read + 1);
        return true;
    }
}

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

    // Looping: the source frames that repeat (_loopEnd 0: up to the end) and the source frame the next read gives.
    private readonly long _loopStart, _loopEnd;
    private long _sourceFrame;

    public StreamFeed(IAudioStream source, bool loop, int outRate, int outChannels, int aheadFrames, long from = 0, long loopStart = 0, long loopEnd = 0)
    {
        Source = source;
        Loop = loop;
        (_sourceFrame, _loopStart, _loopEnd) = (from, loopStart, loopEnd);
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
                // The buffer moved, also when nothing more came: the last frame is then its only one, still to be played.
                bool more = Decode(channels);
                i = (int)_position;
                if (!more) break;

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
        var into = _decoded.AsSpan(keep * channels);
        int read = 0;
        // A loop reads up to the end of its region, then goes back to the region's start; twice at most, so a stream
        // that gives nothing there ends instead of spinning.
        for (int attempt = 0; attempt < 2 && read == 0; attempt++)
        {
            var room = into;
            if (Loop && _loopEnd > 0)
            {
                long left = _loopEnd - _sourceFrame;
                room = left <= 0 ? default : into[..(int)Math.Min(into.Length, left * channels)];
            }
            if (!room.IsEmpty) read = Source.Read(room);
            if (read > 0 || !Loop) break;
            if (_loopStart > 0 && Source is ISeekableAudioStream seekable)
            {
                seekable.Seek(_loopStart);
                _sourceFrame = _loopStart;
            }
            else
            {
                Source.Rewind();
                _sourceFrame = 0;
            }
        }
        _sourceFrame += read / channels;
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

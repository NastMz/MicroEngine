namespace MicroEngine.Audio;

/// <summary>
/// The audio formats the engine decodes, by file extension: one list for every way a file reaches the mixer (sound
/// effects decoded whole, music streamed while it plays, the "sound" and "music" asset kinds) and for every platform.
/// "wav" (PCM), "ogg" (Vorbis) and "mp3" are built in and decoded in managed code; <see cref="Add"/> registers
/// another, with no change to the mixer.
/// </summary>
/// <remarks>
/// The list is the process's: a decoder is a function over a file's bytes with no state of the game in it, and files
/// are decoded from the asset loaders' worker threads as well as from the game's. Register formats when the game
/// starts, before it loads content. Reads are safe from any thread.
/// </remarks>
/// <example>
/// <code>
/// AudioDecoders.Add("raw", bytes => new RawStream(bytes));     // from now on "sfx/hit.raw" loads like any sound
/// Audio.AddSoundFile("hit", "Content/sfx/hit.raw");
/// </code>
/// </example>
public static class AudioDecoders
{
    private static volatile Dictionary<string, Func<byte[], IAudioStream>> s_decoders = new(StringComparer.Ordinal)
    {
        ["wav"] = bytes => new ClipStream(AudioClip.FromWav(bytes)),
        ["ogg"] = VorbisStream.Open,
        ["mp3"] = Mp3Stream.Open,
    };
    private static readonly Lock s_write = new();

    /// <summary>The formats that can be decoded, lower case without the dot, in alphabetical order.</summary>
    public static IReadOnlyList<string> Formats => [.. s_decoders.Keys.Order(StringComparer.Ordinal)];

    /// <summary>Whether <paramref name="format"/> (a file extension, with or without its dot, in any case) can be decoded.</summary>
    public static bool Supports(string? format) => format is not null && s_decoders.ContainsKey(Normalize(format));

    /// <summary>
    /// Registers a format, replacing a decoder already registered for it (a built-in one too).
    /// </summary>
    /// <param name="format">The file extension, with or without its dot; compared without regard to case.</param>
    /// <param name="open">
    /// Opens a stream over a whole file's bytes; it throws <see cref="InvalidDataException"/> for bytes it cannot
    /// decode. It is called once to check a file when it is registered, once for each time the file plays as a stream,
    /// and from any thread. A stream that also implements <see cref="ISeekableAudioStream"/> can be sought and loop
    /// over a region.
    /// </param>
    /// <exception cref="ArgumentException"><paramref name="format"/> is null, empty or white space.</exception>
    public static void Add(string format, Func<byte[], IAudioStream> open)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(format);
        ArgumentNullException.ThrowIfNull(open);
        lock (s_write) s_decoders = new(s_decoders, StringComparer.Ordinal) { [Normalize(format)] = open };
    }

    /// <summary>Takes a format out of the list; false when it was not in it. Files of that format already loaded keep playing.</summary>
    public static bool Remove(string format)
    {
        ArgumentNullException.ThrowIfNull(format);
        lock (s_write)
        {
            var decoders = new Dictionary<string, Func<byte[], IAudioStream>>(s_decoders, StringComparer.Ordinal);
            if (!decoders.Remove(Normalize(format))) return false;
            s_decoders = decoders;
            return true;
        }
    }

    internal static string Normalize(string format) => format.Trim().TrimStart('.').ToLowerInvariant();

    internal static bool TryGet(string format, out Func<byte[], IAudioStream> open) => s_decoders.TryGetValue(Normalize(format), out open!);

    /// <summary>Opens a stream that decodes <paramref name="encoded"/>, a whole file of <paramref name="format"/>, as it is read.</summary>
    /// <exception cref="NotSupportedException">No decoder is registered for the format, or the file is of a kind its decoder does not read (a WAV that is not PCM).</exception>
    /// <exception cref="InvalidDataException">The bytes cannot be decoded as that format, or have other than 1 or 2 channels.</exception>
    public static IAudioStream Open(byte[] encoded, string format)
    {
        ArgumentNullException.ThrowIfNull(encoded);
        ArgumentNullException.ThrowIfNull(format);
        if (!TryGet(format, out var open))
            throw new NotSupportedException($"No audio decoder is registered for '{Normalize(format)}' (registered: {string.Join(", ", Formats)}).");
        return Open(encoded, format, open);
    }

    /// <summary>Opens a stream with <paramref name="open"/>, reporting every way it can refuse the bytes as <see cref="InvalidDataException"/>.</summary>
    internal static IAudioStream Open(byte[] encoded, string format, Func<byte[], IAudioStream> open)
    {
        IAudioStream stream;
        try
        {
            stream = open(encoded) ?? throw new InvalidDataException("the decoder returned no stream.");
        }
        catch (Exception e) when (e is ArgumentException or InvalidDataException or EndOfStreamException or IndexOutOfRangeException or FormatException or OverflowException)
        {
            throw new InvalidDataException($"The audio could not be decoded as {Normalize(format)}: {e.Message}", e);
        }
        if (stream.Channels is 1 or 2 && stream.SampleRate > 0) return stream;
        int channels = stream.Channels, rate = stream.SampleRate;
        stream.Dispose();
        throw new InvalidDataException($"The audio could not be decoded as {Normalize(format)}: {channels} channels at {rate} Hz (1 or 2 channels at a positive rate are supported).");
    }

    /// <summary>
    /// Decodes a whole file into memory: a sound effect, or music that should not be decoded while it plays. The clip
    /// carries the loop region the file names, if any.
    /// </summary>
    /// <exception cref="NotSupportedException">No decoder is registered for the format, or the WAV is not PCM.</exception>
    /// <exception cref="InvalidDataException">The bytes cannot be decoded as that format.</exception>
    public static AudioClip Decode(byte[] encoded, string format) => Decode(encoded, format, null);

    internal static AudioClip Decode(byte[] encoded, string format, string? name)
    {
        ArgumentNullException.ThrowIfNull(encoded);
        ArgumentNullException.ThrowIfNull(format);
        using var stream = Open(encoded, format);
        // A decoded clip read as a stream (WAV) is the clip itself.
        if (stream is ClipStream { Clip: var whole }) return name is null ? whole : new AudioClip(whole.Samples, whole.SampleRate, whole.Channels) { LoopRegion = whole.LoopRegion, Name = name };
        int channels = stream.Channels;
        var seekable = stream as ISeekableAudioStream;
        long known = Math.Max(0, seekable?.Frames ?? 0);
        // The length a file declares is a guess until it is read: no more is taken on trust than its bytes could hold.
        long trusted = Math.Min(known, Math.Max(4096, (long)encoded.Length * 32));
        var samples = new float[Math.Max(4096, Math.Min(trusted, int.MaxValue / 8) * channels + channels)];
        int count = 0;
        try
        {
            while (true)
            {
                if (count == samples.Length) Array.Resize(ref samples, checked(samples.Length * 2));
                int read = stream.Read(samples.AsSpan(count));
                if (read <= 0) break;
                count += read;
            }
        }
        catch (Exception e) when (e is ArgumentException or InvalidDataException or EndOfStreamException or IndexOutOfRangeException or OverflowException)
        {
            throw new InvalidDataException($"The audio could not be decoded as {Normalize(format)}: {e.Message}", e);
        }
        count -= count % channels;
        if (count != samples.Length) Array.Resize(ref samples, count);
        return new AudioClip(samples, stream.SampleRate, channels) { LoopRegion = seekable?.LoopRegion, Name = name };
    }
}

/// <summary>How a music file is kept once registered (<see cref="AudioMixer.AddMusic(string, EncodedAudio, bool, AudioLoadMode)"/>).</summary>
public enum AudioLoadMode
{
    /// <summary>
    /// The file's bytes are kept and decoded a little ahead of what plays: the least memory, a small cost in every
    /// frame while it plays, and its own pitch. The default for music.
    /// </summary>
    Streamed,
    /// <summary>
    /// The file is decoded whole when it is registered: about ten megabytes per minute of stereo at 44.1 kHz, nothing
    /// to decode while it plays, and it can change pitch, seek to any sample at once and play many times over at no
    /// cost. For short pieces and ones played often.
    /// </summary>
    Decoded,
}

/// <summary>A decoded clip read as a stream.</summary>
internal sealed class ClipStream(AudioClip clip) : ISeekableAudioStream
{
    private int _position;
    public AudioClip Clip => clip;
    public int Channels => clip.Channels;
    public int SampleRate => clip.SampleRate;
    public long Frames => clip.Samples.Length / clip.Channels;
    public LoopRegion? LoopRegion => clip.LoopRegion;
    public int Read(Span<float> buffer)
    {
        int n = Math.Min(buffer.Length / Channels * Channels, clip.Samples.Length - _position);
        clip.Samples.AsSpan(_position, n).CopyTo(buffer);
        _position += n;
        return n;
    }
    public void Rewind() => _position = 0;
    public void Seek(long frame) => _position = (int)Math.Clamp(frame, 0, Frames) * Channels;
    public void Dispose() { }
}

/// <summary>MPEG audio (layers 1 to 3) decoded in managed code (NLayer), the same on every backend.</summary>
/// <remarks>
/// After its own seek the decoder gives silence and then a few hundred samples that differ from a straight read (a
/// frame of MP3 depends on the ones before it), so seeks go through <see cref="ExactSeekStream"/> here too.
/// </remarks>
internal sealed class Mp3Stream : IAudioStream
{
    private readonly NLayer.MpegFile _file;

    private Mp3Stream(NLayer.MpegFile file) => _file = file;

    public static IAudioStream Open(byte[] encoded)
    {
        var first = OpenFile(encoded);
        return new ExactSeekStream(first, () => OpenFile(encoded), first.Frames, null);
    }

    private static Mp3Stream OpenFile(byte[] encoded)
    {
        // An ID3v2 tag in front of the sound is skipped here, by its declared size: the decoder would allocate that size
        // on the tag's word, and a tag larger than the file is a damaged file.
        int start = 0;
        if (encoded.Length >= 10 && encoded[0] == 'I' && encoded[1] == 'D' && encoded[2] == '3')
        {
            long size = (encoded[6] & 0x7F) << 21 | (encoded[7] & 0x7F) << 14 | (encoded[8] & 0x7F) << 7 | (encoded[9] & 0x7F);
            long end = 10 + size + ((encoded[5] & 0x10) != 0 ? 10 : 0);
            if (end >= encoded.Length) throw new InvalidDataException("its ID3 tag is larger than the file.");
            start = (int)end;
        }
        NLayer.MpegFile file;
        try
        {
            file = new NLayer.MpegFile(new MemoryStream(encoded, start, encoded.Length - start, writable: false));
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            throw new InvalidDataException(e.Message, e);
        }
        if (file.SampleRate <= 0 || file.Channels is not (1 or 2))
        {
            file.Dispose();
            throw new InvalidDataException("no MPEG audio frame was found.");
        }
        return new Mp3Stream(file);
    }

    public int Channels => _file.Channels;
    public int SampleRate => _file.SampleRate;
    // The decoder counts positions in bytes of its float samples.
    public long Frames => Math.Max(0, _file.Length) / (sizeof(float) * Channels);
    public int Read(Span<float> buffer) => _file.ReadSamples(buffer[..(buffer.Length / Channels * Channels)]);
    public void Rewind() => _file.Position = 0;
    public void Dispose() => _file.Dispose();
}

/// <summary>A decoder whose own seek lands near a frame, on either side of it: enough to find the exact frame by comparing with sound already decoded.</summary>
internal interface ICoarseSeek
{
    /// <summary>Goes to about <paramref name="frame"/>; what is read next begins within <see cref="ExactSeekStream.CoarseMargin"/> frames of it.</summary>
    void SeekNear(long frame);
}

/// <summary>
/// Makes any decoder seek to the exact frame, by reading: a seek forward decodes and drops what lies between, a seek
/// backward opens the file again and does the same from its start.
/// </summary>
/// <remarks>
/// <para>A loop that keeps going back to the same frame does not pay for it at the seam. A second decoder (the spare)
/// is brought to the loop's start a piece per host frame (<see cref="Idle"/>) while the loop plays. And
/// once the sound at the loop's start has been decoded in passing, a decoder that can seek coarsely
/// (<see cref="ICoarseSeek"/>) is put near it and then exactly on it by finding that sound in what it decodes: the
/// intro is not decoded again at all. When that sound is not found exactly once (silence, a pure tone), the spare is
/// decoded from the start as before.</para>
/// <para>A seek the game asks for can be spread over several calls (<see cref="BeginSeek"/>,
/// <see cref="ContinueSeek"/>): a third decoder is brought to the place while this one goes on being read.</para>
/// </remarks>
internal sealed class ExactSeekStream : ISeekableAudioStream
{
    /// <summary>How far before a frame a coarse seek aims, and so how far off a decoder's own seek may land (NVorbis was measured up to 512 frames off).</summary>
    internal const int CoarseMargin = 4096;
    /// <summary>Frames of sound kept from the loop's start to recognise it by.</summary>
    private const int ReferenceFrames = 2048;

    private readonly Func<IAudioStream> _open;
    private readonly float[] _dropped;
    private IAudioStream _current;
    private long _position;
    // Sound already decoded for the current decoder, ahead of what it gives next (after a coarse seek was made exact).
    private float[]? _ahead;
    private int _aheadAt, _aheadEnd;
    // The spare: the decoder being brought to the frame a loop goes back to, where it is, and the loop's end.
    private IAudioStream? _spare;
    private long _sparePosition, _spareTarget = -1, _loopEnd;
    private float[]? _spareAhead;
    private int _spareAheadAt, _spareAheadEnd;
    // The sound at the loop's start, kept as it is decoded in passing.
    private float[]? _reference;
    private long _referenceAt = -1;
    private int _referenceCount;
    // A seek spread over several calls.
    private IAudioStream? _pending;
    private long _pendingPosition, _pendingTarget = -1;

    public ExactSeekStream(IAudioStream first, Func<IAudioStream> open, long frames, LoopRegion? loop)
    {
        (_current, _open, Frames, LoopRegion) = (first, open, Math.Max(0, frames), loop);
        (Channels, SampleRate) = (first.Channels, first.SampleRate);
        _dropped = new float[2048 * Math.Max(1, Channels)];
    }

    public int Channels { get; }
    public int SampleRate { get; }
    public long Frames { get; }
    public LoopRegion? LoopRegion { get; }
    /// <summary>Decoders opened after the first one, and spares placed by a coarse seek instead of by decoding from the start.</summary>
    internal int Reopened { get; private set; }
    /// <inheritdoc cref="Reopened"/>
    internal int Calibrated { get; private set; }

    private IAudioStream Open()
    {
        Reopened++;
        return _open();
    }

    public int Read(Span<float> buffer)
    {
        return ReadCurrent(buffer[..(buffer.Length / Channels * Channels)]);
    }

    /// <summary>The next samples of the current decoder, kept as the loop's reference when they are its start.</summary>
    private int ReadCurrent(Span<float> buffer)
    {
        int read;
        if (_aheadAt < _aheadEnd)
        {
            read = Math.Min(buffer.Length, _aheadEnd - _aheadAt);
            _ahead.AsSpan(_aheadAt, read).CopyTo(buffer);
            _aheadAt += read;
            if (_aheadAt == _aheadEnd) _ahead = null;
        }
        else read = _current.Read(buffer);
        int frames = read / Channels;
        if (_referenceAt >= 0 && _referenceCount < ReferenceFrames)
        {
            // The part of what was just read that lies in the reference, in order: it fills from its first frame.
            long from = Math.Max(_position, _referenceAt + _referenceCount), to = Math.Min(_position + frames, _referenceAt + ReferenceFrames);
            if (from == _referenceAt + _referenceCount && to > from)
            {
                _reference ??= new float[ReferenceFrames * Channels];
                buffer.Slice((int)(from - _position) * Channels, (int)(to - from) * Channels).CopyTo(_reference.AsSpan(_referenceCount * Channels));
                _referenceCount += (int)(to - from);
            }
        }
        _position += frames;
        return read;
    }

    /// <summary>
    /// Tells the stream it will loop from <paramref name="start"/> when it reaches <paramref name="end"/> (0: its
    /// end), so the way back is prepared from now on instead of from the first time round.
    /// </summary>
    public void PrepareLoop(long start, long end)
    {
        _loopEnd = end > 0 ? end : Frames;
        if (start <= 0 || start == _spareTarget) return;
        Watch(start);
        StartSpare(start);
    }

    /// <summary>Keeps the sound at <paramref name="frame"/> the next time it is decoded in passing.</summary>
    private void Watch(long frame)
    {
        if (_referenceAt == frame) return;
        (_referenceAt, _referenceCount) = (frame, 0);
    }

    public void Rewind() => Seek(0);

    /// <summary>The frames that would be decoded and dropped to reach <paramref name="frame"/> now.</summary>
    public long CostOf(long frame)
    {
        frame = Clamp(frame);
        if (frame >= _position) return frame - _position;
        return _spare is not null && _spareTarget == frame ? frame - _sparePosition : frame;
    }

    private long Clamp(long frame) => Math.Max(0, Frames > 0 ? Math.Min(frame, Frames) : frame);

    public void Seek(long frame)
    {
        frame = Clamp(frame);
        CancelSeek();
        if (frame == _position) return;
        bool back = frame < _position;
        if (back && _spare is not null && _spareTarget == frame)
        {
            // The way back was prepared; what is left of it, if anything, is done here.
            if (_sparePosition < frame) _sparePosition += Drop(_spare, frame - _sparePosition);
            _current.Dispose();
            (_current, _spare, _position) = (_spare, null, _sparePosition);
            (_ahead, _aheadAt, _aheadEnd, _spareAhead) = (_spareAhead, _spareAheadAt, _spareAheadEnd, null);
            _spareTarget = -1;
        }
        else
        {
            if (back)
            {
                _current.Dispose();
                _current = Open();
                (_position, _ahead, _aheadAt, _aheadEnd) = (0, null, 0, 0);
            }
            DropCurrent(frame - _position);
        }
        if (!back || _position == 0) return;
        // It went back to the middle of the file: a loop will do so again.
        Watch(_position);
        StartSpare(_position);
    }

    /// <summary>Starts bringing a decoder to <paramref name="frame"/> while this one goes on being read; nothing changes until <see cref="ContinueSeek"/> says it is there.</summary>
    public void BeginSeek(long frame)
    {
        CancelSeek();
        _pendingTarget = Clamp(frame);
        (_pending, _pendingPosition) = (Open(), 0);
    }

    /// <summary>Whether a seek begun with <see cref="BeginSeek"/> is still on its way.</summary>
    public bool Seeking => _pendingTarget >= 0;

    /// <summary>
    /// Decodes and drops up to <paramref name="budget"/> frames on the way to the place <see cref="BeginSeek"/> named.
    /// Returns true when it is reached: the stream then reads from there.
    /// </summary>
    public bool ContinueSeek(long budget)
    {
        if (_pending is null) return true;
        long wanted = Math.Min(_pendingTarget - _pendingPosition, Math.Max(1, budget));
        long dropped = Drop(_pending, wanted);
        _pendingPosition += dropped;
        if (_pendingPosition < _pendingTarget && dropped == wanted) return false;
        _current.Dispose();
        (_current, _pending, _position) = (_pending, null, _pendingPosition);
        (_ahead, _aheadAt, _aheadEnd, _pendingTarget) = (null, 0, 0, -1);
        return true;
    }

    private void CancelSeek()
    {
        _pending?.Dispose();
        (_pending, _pendingTarget) = (null, -1);
    }

    /// <summary>A decoder on its way to <paramref name="frame"/>, for the next time the stream goes back there.</summary>
    private void StartSpare(long frame)
    {
        _spare?.Dispose();
        (_spare, _spareAhead, _spareTarget, _sparePosition) = (null, null, frame, 0);
        if (frame > 2 * CoarseMargin && _referenceAt == frame && _referenceCount == ReferenceFrames && PlaceSpare(frame)) return;
        _spare = Open();
    }

    /// <summary>
    /// Puts a decoder near <paramref name="frame"/> by its own seek and then exactly on it: the frame is where the
    /// sound kept from the loop's start begins in what the decoder gives. False when the decoder cannot seek, or
    /// when that sound is not found exactly once (it is then not known where the decoder is).
    /// </summary>
    private bool PlaceSpare(long frame)
    {
        var reference = _reference.AsSpan(0, ReferenceFrames * Channels);
        float peak = 0;
        foreach (float sample in reference) peak = Math.Max(peak, Math.Abs(sample));
        if (!(peak > 1e-4f)) return false;
        var decoder = Open();
        if (decoder is not ICoarseSeek coarse)
        {
            decoder.Dispose();
            return false;
        }
        var decoded = new float[(2 * CoarseMargin + ReferenceFrames) * Channels];
        int count = 0, found = -1, matches = 0;
        try
        {
            coarse.SeekNear(frame - CoarseMargin);
            while (count < decoded.Length)
            {
                int read = decoder.Read(decoded.AsSpan(count));
                if (read <= 0) break;
                count += read;
            }
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            decoder.Dispose();
            return false;
        }
        for (int at = 0; at + reference.Length <= count && matches < 2; at += Channels)
        {
            bool same = true;
            for (int i = 0; i < reference.Length && same; i++) same = Math.Abs(decoded[at + i] - reference[i]) <= 1e-6f;
            if (!same) continue;
            matches++;
            found = at;
        }
        if (matches != 1)
        {
            decoder.Dispose();
            return false;
        }
        Calibrated++;
        (_spare, _sparePosition) = (decoder, frame);
        (_spareAhead, _spareAheadAt, _spareAheadEnd) = (decoded, found, count - count % Channels);
        return true;
    }

    /// <summary>
    /// Brings the spare up to <paramref name="budget"/> frames closer to the frame the loop goes back to. Called
    /// once per host frame by whoever plays the stream, so the way back is ready before the loop's end comes and no
    /// single frame pays for it.
    /// </summary>
    public void Idle(long budget)
    {
        if (_spare is null || _sparePosition >= _spareTarget) return;
        _sparePosition += Drop(_spare, Math.Min(_spareTarget - _sparePosition, Math.Max(1, budget)));
    }

    /// <summary>Decodes and drops up to <paramref name="frames"/> frames of a decoder; returns how many there were.</summary>
    private long Drop(IAudioStream stream, long frames)
    {
        long dropped = 0;
        while (dropped < frames)
        {
            int want = (int)Math.Min(_dropped.Length / Channels, frames - dropped);
            int read = stream.Read(_dropped.AsSpan(0, want * Channels)) / Channels;
            if (read <= 0) break;
            dropped += read;
        }
        return dropped;
    }

    private void DropCurrent(long frames)
    {
        while (frames > 0)
        {
            int want = (int)Math.Min(_dropped.Length / Channels, frames);
            int read = ReadCurrent(_dropped.AsSpan(0, want * Channels)) / Channels;
            if (read <= 0) break;
            frames -= read;
        }
    }

    public void Dispose()
    {
        _current.Dispose();
        _spare?.Dispose();
        _pending?.Dispose();
        (_spare, _pending) = (null, null);
    }
}

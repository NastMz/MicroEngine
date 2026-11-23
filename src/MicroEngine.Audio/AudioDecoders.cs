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
    /// <exception cref="NotSupportedException">No decoder is registered for the format.</exception>
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
        catch (Exception e) when (e is ArgumentException or InvalidDataException or EndOfStreamException or NotSupportedException or IndexOutOfRangeException or FormatException)
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
        var samples = new float[Math.Max(4096, Math.Min(known, int.MaxValue / 4) * channels + channels)];
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
        NLayer.MpegFile file;
        try
        {
            file = new NLayer.MpegFile(new MemoryStream(encoded, writable: false));
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

/// <summary>
/// Makes any decoder seek to the exact frame, by reading: a seek forward decodes and drops what lies between, a seek
/// backward opens the file again and does the same from its start. A loop that keeps going back to the same frame is
/// served by a second decoder brought to that frame a little at each read, so going back costs nothing when it comes.
/// </summary>
internal sealed class ExactSeekStream : ISeekableAudioStream
{
    private readonly Func<IAudioStream> _open;
    private readonly float[] _dropped;
    private IAudioStream _current;
    private long _position;
    // The decoder being brought to the frame a loop goes back to, where it is and where it is going.
    private IAudioStream? _spare;
    private long _sparePosition, _spareTarget;

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

    public int Read(Span<float> buffer)
    {
        int read = _current.Read(buffer[..(buffer.Length / Channels * Channels)]);
        _position += read / Channels;
        // As much again for the spare as was read: it is ready long before the loop comes round.
        if (_spare is not null && _sparePosition < _spareTarget)
            _sparePosition += Drop(_spare, Math.Min(_spareTarget - _sparePosition, read / Channels + 1024));
        return read;
    }

    public void Rewind() => Seek(0);

    public void Seek(long frame)
    {
        frame = Math.Max(0, Frames > 0 ? Math.Min(frame, Frames) : frame);
        if (frame == _position) return;
        bool back = frame < _position;
        if (back && _spare is not null && _spareTarget == frame && _sparePosition == frame)
        {
            _current.Dispose();
            (_current, _spare) = (_spare, null);
        }
        else
        {
            if (back)
            {
                _current.Dispose();
                _current = _open();
                _position = 0;
            }
            frame = _position + Drop(_current, frame - _position);
        }
        _position = frame;
        if (!back || frame == 0) return;
        // It went back to the middle of the file: a loop will do so again.
        _spare?.Dispose();
        (_spare, _sparePosition, _spareTarget) = (_open(), 0, frame);
    }

    /// <summary>Decodes and drops up to <paramref name="frames"/> frames; returns how many there were.</summary>
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

    public void Dispose()
    {
        _current.Dispose();
        _spare?.Dispose();
        _spare = null;
    }
}

namespace MicroEngine.Audio;

/// <summary>Uncompressed audio: interleaved float samples in -1..1.</summary>
public sealed class AudioClip
{
    public const int DefaultSampleRate = 44100;
    public float[] Samples { get; }
    public int SampleRate { get; }
    public int Channels { get; }
    public double Duration => Samples.Length / (double)(SampleRate * Channels);

    public AudioClip(float[] samples, int sampleRate = DefaultSampleRate, int channels = 1)
    {
        ArgumentNullException.ThrowIfNull(samples);
        if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate));
        if (channels is not (1 or 2)) throw new ArgumentOutOfRangeException(nameof(channels));
        if (samples.Length % channels != 0) throw new ArgumentException("Sample count must be a multiple of the channel count.", nameof(samples));
        Samples = samples;
        SampleRate = sampleRate;
        Channels = channels;
    }

    public float Peak => Samples.Length == 0 ? 0 : Samples.Max(MathF.Abs);

    /// <summary>Decodes an uncompressed PCM WAV (8, 16 or 24-bit, mono or stereo).</summary>
    public static AudioClip FromWav(ReadOnlySpan<byte> data)
    {
        if (data.Length < 12 || !data[..4].SequenceEqual("RIFF"u8) || !data.Slice(8, 4).SequenceEqual("WAVE"u8))
            throw new InvalidDataException("Not a WAV file.");
        int channels = 0, rate = 0, bits = 0, format = 0;
        int p = 12;
        while (p + 8 <= data.Length)
        {
            var id = data.Slice(p, 4);
            int size = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(data[(p + 4)..]);
            if (size < 0) throw new InvalidDataException("Corrupt WAV chunk.");
            var body = data.Slice(p + 8, Math.Min(size, data.Length - p - 8));
            if (id.SequenceEqual("fmt "u8))
            {
                format = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(body);
                channels = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(body[2..]);
                rate = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(body[4..]);
                bits = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(body[14..]);
                // WAVE_FORMAT_EXTENSIBLE carries the real format in its sub-format GUID.
                if (format == 0xFFFE && body.Length >= 26) format = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(body[24..]);
            }
            else if (id.SequenceEqual("data"u8))
            {
                if (format != 1) throw new NotSupportedException($"WAV format {format} is not supported; use PCM.");
                if (channels is not (1 or 2)) throw new NotSupportedException($"WAV with {channels} channels is not supported.");
                int width = bits / 8;
                if (bits is not (8 or 16 or 24)) throw new NotSupportedException($"{bits}-bit WAV is not supported.");
                int count = body.Length / width / channels * channels;
                var samples = new float[count];
                for (int i = 0; i < count; i++)
                {
                    var s = body.Slice(i * width, width);
                    samples[i] = bits switch
                    {
                        8 => (s[0] - 128) / 128f,
                        16 => System.Buffers.Binary.BinaryPrimitives.ReadInt16LittleEndian(s) / 32768f,
                        _ => ((s[2] << 24 | s[1] << 16 | s[0] << 8) >> 8) / 8388608f,
                    };
                }
                return new AudioClip(samples, rate, channels);
            }
            p += 8 + size + (size & 1);
        }
        throw new InvalidDataException("WAV file has no data chunk.");
    }

    /// <summary>Encodes 16-bit PCM WAV bytes, the interchange format backends load from memory.</summary>
    public byte[] ToWav()
    {
        using var stream = new MemoryStream();
        using var w = new BinaryWriter(stream);
        int dataBytes = Samples.Length * 2;
        w.Write("RIFF"u8); w.Write(36 + dataBytes); w.Write("WAVE"u8);
        w.Write("fmt "u8); w.Write(16); w.Write((short)1); w.Write((short)Channels);
        w.Write(SampleRate); w.Write(SampleRate * Channels * 2); w.Write((short)(Channels * 2)); w.Write((short)16);
        w.Write("data"u8); w.Write(dataBytes);
        foreach (var s in Samples) w.Write((short)Math.Clamp(MathF.Round(s * 32767f), -32768f, 32767f));
        w.Flush();
        return stream.ToArray();
    }
}

/// <summary>An opaque handle to a loaded sound effect.</summary>
public sealed class SoundHandle(int id, string name)
{
    public int Id { get; } = id;
    public string Name { get; } = name;
}

/// <summary>An opaque handle to loaded, loopable music.</summary>
public sealed class MusicHandle(int id, string name)
{
    public int Id { get; } = id;
    public string Name { get; } = name;
}

/// <summary>Backend audio device. Implementations must tolerate missing hardware by becoming silent.</summary>
public interface IAudioDevice
{
    bool IsAvailable { get; }
    SoundHandle LoadSound(string name, AudioClip clip);
    MusicHandle LoadMusic(string name, AudioClip clip, bool loop = true);
    /// <summary>Streams compressed music (e.g. "ogg", "mp3", "wav") that the device decodes itself.</summary>
    MusicHandle LoadMusicStream(string name, byte[] encoded, string format, bool loop = true);
    void Play(SoundHandle sound, float volume = 1f, float pitch = 1f, float pan = 0.5f);
    void PlayMusic(MusicHandle music, float volume = 1f);
    void StopMusic();
    void SetMusicVolume(float volume);
    void PauseMusic(bool paused);
    MusicHandle? CurrentMusic { get; }
    /// <summary>Called once per frame so streaming music keeps playing.</summary>
    void Update();
}

/// <summary>Silent device used headlessly and as a fallback when no audio hardware is present.</summary>
public sealed class SilentAudioDevice : IAudioDevice
{
    private int _next;
    public bool IsAvailable => false;
    public List<string> Played { get; } = [];
    public MusicHandle? CurrentMusic { get; private set; }
    public SoundHandle LoadSound(string name, AudioClip clip) => new(_next++, name);
    public MusicHandle LoadMusic(string name, AudioClip clip, bool loop = true) => new(_next++, name);
    public MusicHandle LoadMusicStream(string name, byte[] encoded, string format, bool loop = true) => new(_next++, name);
    public void Play(SoundHandle sound, float volume = 1f, float pitch = 1f, float pan = 0.5f) => Played.Add(sound.Name);
    public void PlayMusic(MusicHandle music, float volume = 1f) => CurrentMusic = music;
    public void StopMusic() => CurrentMusic = null;
    public void SetMusicVolume(float volume) { }
    public void PauseMusic(bool paused) { }
    public void Update() { }
}

/// <summary>
/// Game-facing audio: a sound bank keyed by name plus music with fades.
/// Games register synthesized clips once and then call <see cref="Play"/> by name.
/// </summary>
public sealed class AudioMixer
{
    private readonly IAudioDevice _device;
    private readonly Dictionary<string, SoundHandle> _sounds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, MusicHandle> _music = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _lastPlayedFrame = new(StringComparer.Ordinal);
    private readonly Random _random = new(7);
    private float _musicVolume = 1f, _fade = 1f, _fadeSpeed;
    private string? _pendingMusic;
    private long _frame;

    public AudioMixer(IAudioDevice device) => _device = device ?? throw new ArgumentNullException(nameof(device));

    public IAudioDevice Device => _device;
    public float SfxVolume { get; set; } = 0.8f;
    public float MusicVolume { get => _musicVolume; set { _musicVolume = Math.Clamp(value, 0, 1); Apply(); } }
    public string? CurrentMusic => _device.CurrentMusic?.Name;

    public void AddSound(string name, AudioClip clip) => _sounds[name] = _device.LoadSound(name, clip);
    public void AddMusic(string name, AudioClip clip, bool loop = true) => _music[name] = _device.LoadMusic(name, clip, loop);
    public bool HasSound(string name) => _sounds.ContainsKey(name);
    public bool HasMusic(string name) => _music.ContainsKey(name);

    /// <summary>Registers a WAV file as a sound effect.</summary>
    public void AddSoundFile(string name, string path) => AddSound(name, AudioClip.FromWav(File.ReadAllBytes(path)));

    /// <summary>Registers a music file. WAV is decoded here; other formats are streamed by the device.</summary>
    public void AddMusicFile(string name, string path, bool loop = true)
    {
        var bytes = File.ReadAllBytes(path);
        var format = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
        _music[name] = format == "wav"
            ? _device.LoadMusic(name, AudioClip.FromWav(bytes), loop)
            : _device.LoadMusicStream(name, bytes, format, loop);
    }

    /// <summary>Plays a sound. <paramref name="pitchJitter"/> randomizes pitch (+/-) to avoid repetition fatigue.
    /// The same sound is not retriggered within one frame, preventing volume spikes.</summary>
    public void Play(string name, float volume = 1f, float pitch = 1f, float pitchJitter = 0f, float pan = 0.5f)
    {
        if (!_sounds.TryGetValue(name, out var handle)) throw new KeyNotFoundException($"Sound '{name}' is not registered.");
        if (_lastPlayedFrame.TryGetValue(name, out var last) && last == _frame) return;
        _lastPlayedFrame[name] = (int)_frame;
        float p = pitch + (pitchJitter > 0 ? (float)(_random.NextDouble() * 2 - 1) * pitchJitter : 0);
        _device.Play(handle, Math.Clamp(volume * SfxVolume, 0, 1), p, Math.Clamp(pan, 0, 1));
    }

    /// <summary>Plays a sound if it is registered; returns false otherwise (useful for optional cues).</summary>
    public bool TryPlay(string name, float volume = 1f, float pitch = 1f, float pitchJitter = 0f, float pan = 0.5f)
    {
        if (!_sounds.ContainsKey(name)) return false;
        Play(name, volume, pitch, pitchJitter, pan);
        return true;
    }

    /// <summary>Switches music. With a fade, the current track fades out before the new one starts.</summary>
    public void PlayMusic(string name, float fadeSeconds = 0.4f)
    {
        if (!_music.ContainsKey(name)) throw new KeyNotFoundException($"Music '{name}' is not registered.");
        if (_device.CurrentMusic?.Name == name && _pendingMusic is null) return;
        if (_device.CurrentMusic is null || fadeSeconds <= 0) { StartMusic(name); return; }
        _pendingMusic = name;
        _fadeSpeed = -1f / fadeSeconds;
    }

    public void StopMusic(float fadeSeconds = 0.4f)
    {
        if (fadeSeconds <= 0) { _device.StopMusic(); _pendingMusic = null; return; }
        _pendingMusic = "";
        _fadeSpeed = -1f / fadeSeconds;
    }

    public void PauseMusic(bool paused) => _device.PauseMusic(paused);

    private void StartMusic(string name)
    {
        _pendingMusic = null;
        _fade = 0.05f;
        _fadeSpeed = 4f;
        _device.PlayMusic(_music[name], _musicVolume * _fade);
    }

    private void Apply() => _device.SetMusicVolume(_musicVolume * _fade);

    public void Update(float dt)
    {
        _frame++;
        if (_fadeSpeed != 0)
        {
            _fade = Math.Clamp(_fade + _fadeSpeed * dt, 0, 1);
            if (_fadeSpeed < 0 && _fade <= 0)
            {
                _fadeSpeed = 0;
                if (_pendingMusic is { Length: > 0 } next) StartMusic(next);
                else { _device.StopMusic(); _pendingMusic = null; _fade = 1; }
            }
            else if (_fadeSpeed > 0 && _fade >= 1) _fadeSpeed = 0;
            Apply();
        }
        _device.Update();
    }
}

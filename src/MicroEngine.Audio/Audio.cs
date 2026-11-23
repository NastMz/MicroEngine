using MicroEngine.Audio.Retro;

namespace MicroEngine.Audio;

/// <summary>Uncompressed audio: interleaved float samples in -1..1.</summary>
public sealed class AudioClip
{
    /// <summary>Sample rate used by the synthesizer and by default for new clips: 44100 Hz.</summary>
    public const int DefaultSampleRate = 44100;
    /// <summary>The samples, interleaved by channel (left, right, left... for stereo), each in -1..1. The array is not copied.</summary>
    public float[] Samples { get; }
    /// <summary>Samples per second per channel.</summary>
    public int SampleRate { get; }
    /// <summary>1 (mono) or 2 (stereo).</summary>
    public int Channels { get; }
    /// <summary>Length of the clip in seconds.</summary>
    public double Duration => Samples.Length / (double)(SampleRate * Channels);
    /// <summary>
    /// The part of the clip that repeats when it plays looping (an intro before it plays once), or null for the whole
    /// clip. <see cref="FromWav"/> reads it from the file's sampler chunk; <see cref="PlayParameters.LoopRegion"/>
    /// replaces it for one play.
    /// </summary>
    public LoopRegion? LoopRegion { get; init; }
    /// <summary>A name for diagnostics (the content path of a clip loaded from a file), or null.</summary>
    public string? Name { get; init; }
    // The mixer that last played the clip without registering it, and at which of its updates: a clip is not played
    // twice in one update.
    internal AudioMixer? PlayedBy;
    internal long PlayedFrame;

    /// <summary>Wraps existing samples in a clip without copying them.</summary>
    /// <param name="samples">Interleaved samples in -1..1; the length must be a multiple of <paramref name="channels"/>.</param>
    /// <param name="sampleRate">Samples per second per channel; must be positive.</param>
    /// <param name="channels">1 or 2.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="sampleRate"/> is not positive or <paramref name="channels"/> is not 1 or 2.</exception>
    /// <exception cref="ArgumentException">The sample count is not a multiple of the channel count.</exception>
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

    /// <summary>The largest absolute sample value (0 for an empty clip). Computed on every read.</summary>
    public float Peak => Samples.Length == 0 ? 0 : Samples.Max(MathF.Abs);

    /// <summary>
    /// Decodes an uncompressed PCM WAV (8, 16 or 24-bit, mono or stereo). The first loop of a sampler chunk ("smpl"),
    /// which audio editors write for loop points, becomes the clip's <see cref="LoopRegion"/> when it is at least two
    /// frames long and starts within the sound.
    /// </summary>
    /// <exception cref="InvalidDataException">The bytes are not a WAV file, or it is cut short.</exception>
    /// <exception cref="NotSupportedException">The WAV is not PCM, has more than two channels or another sample size.</exception>
    public static AudioClip FromWav(ReadOnlySpan<byte> data)
    {
        if (data.Length < 12 || !data[..4].SequenceEqual("RIFF"u8) || !data.Slice(8, 4).SequenceEqual("WAVE"u8))
            throw new InvalidDataException("Not a WAV file.");
        int channels = 0, rate = 0, bits = 0, format = 0;
        float[]? decoded = null;
        long loopStart = -1, loopEnd = -1;
        int p = 12;
        while (p + 8 <= data.Length)
        {
            var id = data.Slice(p, 4);
            int size = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(data[(p + 4)..]);
            if (size < 0) throw new InvalidDataException("Corrupt WAV chunk.");
            var body = data.Slice(p + 8, Math.Min(size, data.Length - p - 8));
            if (id.SequenceEqual("fmt "u8))
            {
                if (body.Length < 16) throw new InvalidDataException("Corrupt WAV format chunk.");
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
                decoded ??= samples;
            }
            else if (id.SequenceEqual("smpl"u8) && body.Length >= 36 + 24
                && System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(body[28..]) > 0)
            {
                // The first loop: its first frame and its last one (inclusive).
                long start = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(body[(36 + 8)..]);
                long end = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(body[(36 + 12)..]);
                if (loopStart < 0) (loopStart, loopEnd) = (start, end);
            }
            // A chunk the file cuts short is the last one.
            if (size > data.Length - p - 8) break;
            p += 8 + size + (size & 1);
        }
        if (decoded is null) throw new InvalidDataException("WAV file has no data chunk.");
        // A sampler loop that starts and ends on one frame, or lies past the sound, is a marker and not a loop.
        long frames = decoded.Length / channels;
        LoopRegion? loop = loopEnd > loopStart && loopStart < frames ? new LoopRegion(loopStart, Math.Min(loopEnd + 1, frames)) : null;
        return new AudioClip(decoded, rate, channels) { LoopRegion = loop };

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
    /// <summary>Identifier assigned by the audio device that loaded the sound.</summary>
    public int Id { get; } = id;
    /// <summary>The name the sound was registered under.</summary>
    public string Name { get; } = name;
}

/// <summary>An opaque handle to loaded, loopable music.</summary>
public sealed class MusicHandle(int id, string name)
{
    /// <summary>Identifier assigned by the audio device that loaded the music.</summary>
    public int Id { get; } = id;
    /// <summary>The name the music was registered under.</summary>
    public string Name { get; } = name;
}

/// <summary>Backend audio device. Implementations must tolerate missing hardware by becoming silent.</summary>
public interface IAudioDevice
{
    /// <summary>Whether sound actually reaches a speaker. False for a silent device (no hardware, headless, muted host).</summary>
    bool IsAvailable { get; }
    /// <summary>Uploads a sound effect and returns its handle.</summary>
    SoundHandle LoadSound(string name, AudioClip clip);
    /// <summary>Uploads decoded music and returns its handle.</summary>
    /// <param name="name">Name reported by <see cref="MusicHandle.Name"/>.</param>
    /// <param name="clip">The music samples.</param>
    /// <param name="loop">Whether the music restarts when it reaches its end.</param>
    MusicHandle LoadMusic(string name, AudioClip clip, bool loop = true);
    /// <summary>Streams compressed music (e.g. "ogg", "mp3", "wav") that the device decodes itself.</summary>
    /// <remarks>Formats depend on the device: the SDL device streams wav and ogg; the raylib device also decodes mp3.</remarks>
    MusicHandle LoadMusicStream(string name, byte[] encoded, string format, bool loop = true);
    /// <summary>Plays a sound effect once, mixed with any sounds already playing.</summary>
    /// <param name="sound">A handle from <see cref="LoadSound"/>.</param>
    /// <param name="volume">Volume, 0..1.</param>
    /// <param name="pitch">Playback speed factor; 1 is the original pitch.</param>
    /// <param name="pan">Stereo position, 0 (left) to 1 (right); 0.5 is centred.</param>
    void Play(SoundHandle sound, float volume = 1f, float pitch = 1f, float pan = 0.5f);
    /// <summary>Starts <paramref name="music"/> from the beginning, replacing the music that was playing.</summary>
    void PlayMusic(MusicHandle music, float volume = 1f);
    /// <summary>Stops the music immediately.</summary>
    void StopMusic();
    /// <summary>Sets the volume of the current music, 0..1.</summary>
    void SetMusicVolume(float volume);
    /// <summary>Pauses (true) or resumes (false) the current music.</summary>
    void PauseMusic(bool paused);
    /// <summary>The music started last and not stopped, or null.</summary>
    MusicHandle? CurrentMusic { get; }
    /// <summary>Called once per frame so streaming music keeps playing.</summary>
    void Update();
    /// <summary>
    /// Plays a clip that was not loaded into the device (<see cref="LoadSound"/>), which then holds it only while it
    /// plays. A device that does not implement it plays nothing.
    /// </summary>
    void Play(AudioClip clip, float volume = 1f, float pitch = 1f, float pan = 0.5f) { }
    /// <summary>Releases what <see cref="LoadSound"/> kept for <paramref name="sound"/>; the handle plays nothing afterwards. A device that does not implement it keeps the sound.</summary>
    void UnloadSound(SoundHandle sound) { }
    /// <summary>Releases what <see cref="LoadMusic"/> or <see cref="LoadMusicStream"/> kept for <paramref name="music"/>; the handle plays nothing afterwards. A device that does not implement it keeps the music.</summary>
    void UnloadMusic(MusicHandle music) { }
}

/// <summary>Silent device used headlessly and as a fallback when no audio hardware is present.</summary>
public sealed class SilentAudioDevice : IAudioDevice
{
    private int _next;
    /// <summary>Always false: nothing is heard.</summary>
    public bool IsAvailable => false;
    /// <summary>Names of the sounds played, in order, for checks in tests.</summary>
    public List<string> Played { get; } = [];
    /// <inheritdoc/>
    public MusicHandle? CurrentMusic { get; private set; }
    /// <inheritdoc/>
    public SoundHandle LoadSound(string name, AudioClip clip) => new(_next++, name);
    /// <inheritdoc/>
    public MusicHandle LoadMusic(string name, AudioClip clip, bool loop = true) => new(_next++, name);
    /// <inheritdoc/>
    public MusicHandle LoadMusicStream(string name, byte[] encoded, string format, bool loop = true) => new(_next++, name);
    /// <summary>Records the sound's name in <see cref="Played"/>.</summary>
    public void Play(SoundHandle sound, float volume = 1f, float pitch = 1f, float pan = 0.5f) => Played.Add(sound.Name);
    /// <summary>Records the clip's <see cref="AudioClip.Name"/> ("" for a clip without one) in <see cref="Played"/>.</summary>
    public void Play(AudioClip clip, float volume = 1f, float pitch = 1f, float pan = 0.5f) => Played.Add(clip?.Name ?? "");
    /// <inheritdoc/>
    public void PlayMusic(MusicHandle music, float volume = 1f) => CurrentMusic = music;
    /// <inheritdoc/>
    public void StopMusic() => CurrentMusic = null;
    /// <inheritdoc/>
    public void SetMusicVolume(float volume) { }
    /// <inheritdoc/>
    public void PauseMusic(bool paused) { }
    /// <inheritdoc/>
    public void Update() { }
}

/// <summary>
/// Game-facing audio: a sound bank keyed by name plus music with fades.
/// Games register synthesized clips once and then call <see cref="Play(string, float, float, float, float)"/> by name.
/// </summary>
/// <remarks>
/// Every game has one in <c>Game.Audio</c>. Sounds and music are registered under names that are compared
/// ordinally (case-sensitive); registering a name again replaces it. On a device without hardware every call still works
/// and nothing is heard.
/// </remarks>
/// <example>
/// <code>
/// // In Game.Load (Synth, SfxParams and Wave are in MicroEngine.Audio.Retro):
/// Audio.AddSound("jump", Synth.Render(new SfxParams { Wave = Wave.Square, Frequency = 520, EndFrequency = 880 }));
/// Audio.AddMusicFile("theme", "Content/theme.ogg");
/// Audio.PlayMusic("theme");
///
/// // In a scene's Update:
/// if (Input.Pressed("jump")) Audio.Play("jump", pitchJitter: 0.05f);
/// </code>
/// </example>
public sealed class AudioMixer
{
    private readonly IAudioDevice _device;
    private readonly Dictionary<string, SoundHandle> _sounds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, MusicHandle> _music = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (Task<AudioClip> Render, bool Loop)> _songs = new(StringComparer.Ordinal);
    // Single-threaded hosts (browsers): songs render a slice at a time from Update, in the order they were registered.
    private readonly List<(string Name, IEnumerator<AudioClip?> Slices, bool Loop)> _sliced = [];

    /// <summary>Whether songs render on the thread pool (true) or in slices from <see cref="Update"/> (browsers).</summary>
    internal static bool RenderSongsOnThreads { get; set; } = !OperatingSystem.IsBrowser();
    /// <summary>The time <see cref="Update"/> spends rendering songs in slices, per call.</summary>
    internal static TimeSpan SliceBudget { get; set; } = TimeSpan.FromMilliseconds(4);
    /// <summary>Songs still rendering in slices.</summary>
    internal int SlicedSongs => _sliced.Count;
    // A song asked to play while it still renders in slices: it starts once rendered, instead of freezing the game.
    private (string Name, bool FadeIn)? _waiting;
    private readonly Dictionary<string, int> _lastPlayedFrame = new(StringComparer.Ordinal);
    private readonly Random _random = new(7);
    private float _musicVolume = 1f, _fade = 1f, _fadeSpeed;
    private string? _pendingMusic;
    private long _frame;

    /// <summary>Creates a mixer over <paramref name="device"/>. Games use the one in <c>Game.Audio</c>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="device"/> is null.</exception>
    public AudioMixer(IAudioDevice device)
    {
        _device = device ?? throw new ArgumentNullException(nameof(device));
        if (Engine is { } engine) engine.StreamFailed += StreamFailed;
    }

    private static readonly MicroEngine.Kernel.ILog DefaultLog = new MicroEngine.Kernel.ConsoleLog();
    // The name of the music the device was last told to play, for the report of a file that turns out damaged.
    private string? _startedMusic;

    /// <summary>A stream's decoder threw while it played: said once, as a warning; the sound has ended.</summary>
    private void StreamFailed(VoiceHandle voice, Exception error)
    {
        string what = !voice.IsNone && voice == MusicVoice && _startedMusic is not null ? $"music '{_startedMusic}'" : "a streamed sound";
        (Log ?? DefaultLog).Write(MicroEngine.Kernel.LogLevel.Warning, "audio",
            $"{what} stopped: its file could not be decoded any further ({error.GetType().Name}: {error.Message}). The file is damaged; export it again.");
    }

    /// <summary>
    /// Where the mixer reports what it could not play (a music file that turns out damaged while it plays): the
    /// engine's log when the mixer is a game's, standard error otherwise.
    /// </summary>
    public MicroEngine.Kernel.ILog? Log { get; set; }

    /// <summary>
    /// Music and streamed voices that ended because their file could not be decoded any further (see
    /// <see cref="AudioEngine.FailedStreams"/>); each is also written once to <see cref="Log"/>.
    /// </summary>
    public int FailedStreams => Engine?.FailedStreams ?? 0;

    /// <summary>The audio device the mixer plays through.</summary>
    public IAudioDevice Device => _device;
    /// <summary>
    /// Volume multiplier for sound effects, 0..1 (default 0.8). Each played volume is multiplied by it and clamped to
    /// 0..1. A value that is not a number is ignored.
    /// </summary>
    public float SfxVolume { get => _sfxVolume; set { if (!float.IsNaN(value)) _sfxVolume = value; } }
    private float _sfxVolume = 0.8f;
    /// <summary>
    /// Music volume, clamped to 0..1 (default 1). Applies immediately to the playing music, combined with any fade. A
    /// value that is not a number is ignored.
    /// </summary>
    public float MusicVolume
    {
        get => _musicVolume;
        set
        {
            if (float.IsNaN(value)) return;
            _musicVolume = Math.Clamp(value, 0, 1);
            Apply();
        }
    }
    /// <summary>Name of the music playing (also while it fades out, and while a song asked for still renders in slices), or null. It follows the game's requests and the music's end on the simulation's timeline, never rendering speed.</summary>
    public string? CurrentMusic => _waiting?.Name ?? _device.CurrentMusic?.Name;

    /// <summary>
    /// The voices of the device's mixer: how many sounds play now, how many can play at once and how many the voice
    /// policy refused so far. Zeros for a device without the engine's mixer (<see cref="SilentAudioDevice"/>).
    /// </summary>
    public AudioStatistics Statistics => _device is MixerAudioDevice { Engine: var engine }
        ? new AudioStatistics(engine.ActiveVoices, engine.MaxVoices, engine.RefusedSounds)
        : default;
    /// <summary>
    /// Registers <paramref name="clip"/> as the sound effect <paramref name="name"/>, replacing any sound with that
    /// name (which is released as <see cref="RemoveSound"/> releases it).
    /// </summary>
    public void AddSound(string name, AudioClip clip)
    {
        var handle = _device.LoadSound(name, clip);
        if (_sounds.TryGetValue(name, out var old)) _device.UnloadSound(old);
        _sounds[name] = handle;
        _soundSeconds[name] = clip.Duration;
    }

    private readonly Dictionary<string, double> _soundSeconds = new(StringComparer.Ordinal);

    /// <summary>The length in seconds of the registered sound <paramref name="name"/>, at its own pitch; the same on every device.</summary>
    /// <exception cref="KeyNotFoundException">No sound is registered under <paramref name="name"/>.</exception>
    public double SoundDuration(string name) =>
        _soundSeconds.TryGetValue(name, out double seconds) ? seconds : throw new KeyNotFoundException($"Sound '{name}' is not registered.");

    /// <summary>
    /// Registers a sound file of any format in <see cref="AudioDecoders"/> ("wav", "ogg", "mp3" and what the game
    /// registered) as the sound effect <paramref name="name"/>, decoded whole now.
    /// </summary>
    /// <exception cref="NotSupportedException">No decoder is registered for the format.</exception>
    /// <exception cref="InvalidDataException">The bytes cannot be decoded as their format.</exception>
    public void AddSound(string name, EncodedAudio sound)
    {
        ArgumentNullException.ThrowIfNull(sound);
        AddSound(name, AudioDecoders.Decode(sound.Data, sound.Format, name));
    }

    /// <summary>
    /// Unregisters the sound effect <paramref name="name"/>: the mixer and the device no longer hold its samples, so
    /// they are freed once nothing else holds them (an asset scope, the game's own field). A voice playing it plays on
    /// to its end.
    /// </summary>
    /// <returns>Whether a sound had that name.</returns>
    public bool RemoveSound(string name)
    {
        if (!_sounds.Remove(name, out var handle)) return false;
        _device.UnloadSound(handle);
        _lastPlayedFrame.Remove(name);
        _soundSeconds.Remove(name);
        return true;
    }

    /// <summary>
    /// Unregisters the music <paramref name="name"/>: the mixer and the device no longer hold its samples or its
    /// file's bytes. If it is the music playing (or about to play) it stops at once; a voice playing it beside the
    /// music plays on to its end.
    /// </summary>
    /// <returns>Whether music had that name.</returns>
    public bool RemoveMusic(string name)
    {
        if (!HasMusic(name)) return false;
        if (_waiting?.Name == name) _waiting = null;
        if (_pendingMusic == name) _pendingMusic = "";
        _songs.Remove(name);
        _sliced.RemoveAll(s => s.Name == name);
        if (_device.CurrentMusic?.Name == name) _device.StopMusic();
        if (_music.Remove(name, out var handle)) _device.UnloadMusic(handle);
        return true;
    }

    /// <summary>Puts <paramref name="handle"/> under <paramref name="name"/>, releasing the music that had the name.</summary>
    private void Register(string name, MusicHandle handle)
    {
        if (_music.TryGetValue(name, out var old) && !ReferenceEquals(old, handle)) _device.UnloadMusic(old);
        _music[name] = handle;
    }
    /// <summary>Registers decoded music, such as a rendered <see cref="Song"/>, under <paramref name="name"/>.</summary>
    /// <param name="name">Name used by <see cref="PlayMusic"/>.</param>
    /// <param name="clip">The music samples.</param>
    /// <param name="loop">Whether the music restarts when it reaches its end.</param>
    public void AddMusic(string name, AudioClip clip, bool loop = true)
    {
        _songs.Remove(name);
        _sliced.RemoveAll(s => s.Name == name);
        Register(name, _device.LoadMusic(name, clip, loop));
    }

    /// <summary>
    /// Registers a <see cref="Song"/> under <paramref name="name"/> without waiting for it to be rendered: songs render
    /// in the background, side by side, and each is handed to the device the first time it plays (waiting then only for
    /// that song if it is not ready yet). Registering every song of a game this way keeps their synthesis out of
    /// <c>Load</c>. Where the runtime has no background threads (browsers), songs render a few milliseconds per
    /// <see cref="Pump"/> (once per frame, outside the simulation step) instead, one after another in the order they were
    /// registered; a song asked to play before
    /// it is ready goes first and starts once rendered (<see cref="CurrentMusic"/> already names it).
    /// </summary>
    /// <param name="name">Name used by <see cref="PlayMusic"/>.</param>
    /// <param name="song">The song; do not change it after registering it.</param>
    /// <param name="loop">Whether the music restarts when it reaches its end.</param>
    /// <exception cref="InvalidOperationException">The song has no steps.</exception>
    /// <exception cref="FormatException">A track has an invalid note or a drum track an unknown drum.</exception>
    public void AddMusic(string name, Song song, bool loop = true)
    {
        ArgumentNullException.ThrowIfNull(song);
        // Mistakes in the song are reported here, in Load, not later when it first plays.
        song.Validate();
        if (_music.Remove(name, out var old)) _device.UnloadMusic(old);
        _sliced.RemoveAll(s => s.Name == name);
        _songs.Remove(name);
        // Without threads (a browser) the song renders a few milliseconds per update instead of all at once in Load.
        if (RenderSongsOnThreads) _songs[name] = (Task.Run(() => song.Render()), loop);
        else _sliced.Add((name, song.RenderInSlices(), loop));
    }

    /// <summary>The handle of music <paramref name="name"/>, handing a background-rendered song to the device first if needed.</summary>
    private MusicHandle Music(string name)
    {
        int sliced = _sliced.FindIndex(s => s.Name == name);
        if (sliced >= 0)
        {
            // Needed now (a direct request for the handle): the rest of it renders at once.
            var (_, slices, loop) = _sliced[sliced];
            _sliced.RemoveAt(sliced);
            AudioClip? clip = null;
            while (clip is null && slices.MoveNext()) clip = slices.Current;
            Register(name, _device.LoadMusic(name, clip!, loop));
        }
        if (_songs.Remove(name, out var song))
        {
            AudioClip clip;
            try { clip = song.Render.GetAwaiter().GetResult(); }
            catch (Exception e) { throw new InvalidOperationException($"Music '{name}' could not be rendered: {e.Message}", e); }
            Register(name, _device.LoadMusic(name, clip, song.Loop));
        }
        return _music[name];
    }
    /// <summary>Whether a sound effect is registered under <paramref name="name"/>.</summary>
    public bool HasSound(string name) => _sounds.ContainsKey(name);
    /// <summary>Whether music is registered under <paramref name="name"/>.</summary>
    public bool HasMusic(string name) => _music.ContainsKey(name) || _songs.ContainsKey(name) || _sliced.Exists(s => s.Name == name);

    /// <summary>
    /// Registers a sound file as a sound effect, decoded whole now: WAV (8, 16 or 24-bit PCM), OGG Vorbis, MP3 and any
    /// format registered in <see cref="AudioDecoders"/>, chosen by the file's extension (WAV without one).
    /// </summary>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="NotSupportedException">No decoder is registered for the extension, or the WAV is not 8, 16 or 24-bit PCM, mono or stereo.</exception>
    /// <exception cref="InvalidDataException">The file cannot be decoded as its format.</exception>
    public void AddSoundFile(string name, string path) =>
        AddSound(name, AudioDecoders.Decode(File.ReadAllBytes(path), Path.GetExtension(path) is { Length: > 1 } e ? e : "wav", name));

    /// <summary>
    /// Registers a music file: WAV, OGG Vorbis, MP3 and any format registered in <see cref="AudioDecoders"/>, the same
    /// on every platform, chosen by the file's extension (WAV without one).
    /// </summary>
    /// <param name="name">Name used by <see cref="PlayMusic"/>.</param>
    /// <param name="path">The file.</param>
    /// <param name="loop">Whether the music restarts when it reaches its end (over its loop region, if the file names one).</param>
    /// <param name="mode">Decoded while it plays (the default) or whole, now. WAV is always decoded whole.</param>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="NotSupportedException">No decoder is registered for the extension.</exception>
    /// <exception cref="InvalidDataException">The file cannot be decoded as its format.</exception>
    public void AddMusicFile(string name, string path, bool loop = true, AudioLoadMode mode = AudioLoadMode.Streamed) =>
        AddMusic(name, new EncodedAudio(File.ReadAllBytes(path), Path.GetExtension(path) is { Length: > 1 } e ? e : "wav"), loop, mode);

    /// <summary>
    /// Registers a music file loaded from content (<c>Assets.Music(path)</c>) under <paramref name="name"/>.
    /// </summary>
    /// <param name="name">Name used by <see cref="PlayMusic"/>.</param>
    /// <param name="music">The file's bytes and format.</param>
    /// <param name="loop">Whether the music restarts when it reaches its end (over its loop region, if the file names one).</param>
    /// <param name="mode">
    /// <see cref="AudioLoadMode.Streamed"/> (the default) keeps the bytes and decodes them while the music plays;
    /// <see cref="AudioLoadMode.Decoded"/> decodes the whole file now. WAV is always decoded whole; a format only the
    /// device decodes (not in <see cref="AudioDecoders"/>) is always streamed.
    /// </param>
    /// <exception cref="NotSupportedException">No decoder is registered for the format.</exception>
    /// <exception cref="InvalidDataException">The bytes cannot be decoded as their format.</exception>
    public void AddMusic(string name, EncodedAudio music, bool loop = true, AudioLoadMode mode = AudioLoadMode.Streamed)
    {
        ArgumentNullException.ThrowIfNull(music);
        var handle = music.Format == "wav" || mode == AudioLoadMode.Decoded && AudioDecoders.Supports(music.Format)
            ? _device.LoadMusic(name, AudioDecoders.Decode(music.Data, music.Format, name), loop)
            : _device.LoadMusicStream(name, music.Data, music.Format, loop);
        _songs.Remove(name);
        _sliced.RemoveAll(s => s.Name == name);
        Register(name, handle);
    }

    /// <summary>Plays a sound. <paramref name="pitchJitter"/> randomizes pitch (+/-) to avoid repetition fatigue.
    /// The same sound is not retriggered within one frame, preventing volume spikes.</summary>
    /// <exception cref="KeyNotFoundException">No sound is registered under <paramref name="name"/>; use <see cref="TryPlay"/> for optional sounds.</exception>
    public void Play(string name, float volume = 1f, float pitch = 1f, float pitchJitter = 0f, float pan = 0.5f)
    {
        if (!_sounds.TryGetValue(name, out var handle)) throw new KeyNotFoundException($"Sound '{name}' is not registered.");
        if (_lastPlayedFrame.TryGetValue(name, out var last) && last == _frame) return;
        _lastPlayedFrame[name] = (int)_frame;
        float p = pitch + (pitchJitter > 0 ? (float)(_random.NextDouble() * 2 - 1) * pitchJitter : 0);
        _device.Play(handle, Math.Clamp(volume * SfxVolume, 0, 1), p, Math.Clamp(pan, 0, 1));
    }

    /// <summary>
    /// Plays a clip that is not registered under a name: one a game holds itself, or one from <c>Assets.Sound(path)</c>
    /// held by an asset scope. Nothing keeps the clip after it has played, so releasing the scope frees it. Volume,
    /// pitch, jitter and pan are those of <see cref="Play(string, float, float, float, float)"/>, and the same clip
    /// is not retriggered within one update.
    /// </summary>
    public void Play(AudioClip clip, float volume = 1f, float pitch = 1f, float pitchJitter = 0f, float pan = 0.5f)
    {
        ArgumentNullException.ThrowIfNull(clip);
        if (ReferenceEquals(clip.PlayedBy, this) && clip.PlayedFrame == _frame) return;
        (clip.PlayedBy, clip.PlayedFrame) = (this, _frame);
        float p = pitch + (pitchJitter > 0 ? (float)(_random.NextDouble() * 2 - 1) * pitchJitter : 0);
        _device.Play(clip, Math.Clamp(volume * SfxVolume, 0, 1), p, Math.Clamp(pan, 0, 1));
    }

    /// <summary>
    /// Plays a clip that is not registered under a name as a voice, exactly as <paramref name="parameters"/> say (see
    /// <see cref="PlayVoice(string, in PlayParameters)"/>). Nothing keeps the clip once the voice ends.
    /// </summary>
    /// <returns>The voice, or <see cref="VoiceHandle.None"/> as <see cref="PlayVoice(string, in PlayParameters)"/> returns it.</returns>
    /// <exception cref="KeyNotFoundException">The bus does not exist.</exception>
    public VoiceHandle PlayVoice(AudioClip clip, in PlayParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(clip);
        RequireBus(parameters.Bus ?? AudioEngine.SfxBus);
        if (_device is MixerAudioDevice mixer) return mixer.PlayVoice(clip, parameters);
        _device.Play(clip, Math.Clamp(parameters.Volume, 0, 1), parameters.Pitch, Math.Clamp(parameters.Pan, 0, 1));
        return VoiceHandle.None;
    }

    /// <summary>Plays a sound if it is registered; returns false otherwise (useful for optional cues).</summary>
    public bool TryPlay(string name, float volume = 1f, float pitch = 1f, float pitchJitter = 0f, float pan = 0.5f)
    {
        if (!_sounds.ContainsKey(name)) return false;
        Play(name, volume, pitch, pitchJitter, pan);
        return true;
    }

    // ---- Voices: sounds played for a handle, on the engine's mixer

    /// <summary>The engine's mixer behind the device; null for a device without it (<see cref="SilentAudioDevice"/>).</summary>
    private AudioEngine? Engine => (_device as MixerAudioDevice)?.Engine;

    /// <summary>
    /// Plays a registered sound as a voice and returns its handle, to change or stop it while it plays: a loop that
    /// follows something (an engine, a siren), a sound to cut short, a sound on a bus of its own or with a priority.
    /// It plays exactly as <paramref name="parameters"/> say: <see cref="SfxVolume"/> and the once-per-frame rule of
    /// <see cref="Play(string, float, float, float, float)"/> do not apply (scale voices with their bus, <see cref="SetBusVolume"/>).
    /// </summary>
    /// <returns>
    /// The voice, or <see cref="VoiceHandle.None"/> when every voice is busy with sounds of higher priority or the device
    /// has no mixer (headless, muted, no hardware); calls with that handle do nothing.
    /// </returns>
    /// <exception cref="KeyNotFoundException">No sound is registered under <paramref name="name"/>, or the bus does not exist.</exception>
    public VoiceHandle PlayVoice(string name, in PlayParameters parameters)
    {
        if (!_sounds.TryGetValue(name, out var handle)) throw new KeyNotFoundException($"Sound '{name}' is not registered.");
        RequireBus(parameters.Bus ?? AudioEngine.SfxBus);
        if (_device is MixerAudioDevice mixer) return mixer.PlayVoice(handle, parameters);
        // No mixer: the device still hears of the sound (a silent device lists it for tests); there is no voice.
        _device.Play(handle, Math.Clamp(parameters.Volume, 0, 1), parameters.Pitch, Math.Clamp(parameters.Pan, 0, 1));
        return VoiceHandle.None;
    }

    /// <summary>
    /// Plays registered music as one more voice, beside the track <see cref="PlayMusic"/> plays and without replacing
    /// it: ambience under the music, layers of one piece faded in and out. It plays exactly as
    /// <paramref name="parameters"/> say (name the "music" bus to keep it with the music; it loops when they say so,
    /// whatever the music was registered with); <see cref="MusicVolume"/> and music fades do not apply. Streamed music
    /// (OGG) keeps its own pitch.
    /// </summary>
    /// <returns>The voice, or <see cref="VoiceHandle.None"/> as <see cref="PlayVoice(string, in PlayParameters)"/> returns it.</returns>
    /// <exception cref="KeyNotFoundException">No music is registered under <paramref name="name"/>, or the bus does not exist.</exception>
    public VoiceHandle PlayMusicVoice(string name, in PlayParameters parameters)
    {
        if (!HasMusic(name)) throw new KeyNotFoundException($"Music '{name}' is not registered.");
        RequireBus(parameters.Bus ?? AudioEngine.SfxBus);
        return _device is MixerAudioDevice mixer ? mixer.PlayMusicVoice(Music(name), parameters) : VoiceHandle.None;
    }

    /// <summary>Whether <paramref name="voice"/> is still playing (or paused, or fading out).</summary>
    public bool IsPlaying(VoiceHandle voice) => Engine?.IsPlaying(voice) ?? false;

    /// <summary>Stops <paramref name="voice"/>, fading it out over <paramref name="fadeSeconds"/> (0: at once).</summary>
    /// <returns>Whether it was playing; false for a handle whose sound already ended, was stopped or lost its voice.</returns>
    public bool Stop(VoiceHandle voice, float fadeSeconds = 0f) => Engine?.Stop(voice, fadeSeconds) ?? false;

    /// <summary>Pauses (true) or resumes (false) <paramref name="voice"/>; a paused sound keeps its voice.</summary>
    /// <returns>Whether it was playing.</returns>
    public bool Pause(VoiceHandle voice, bool paused) => Engine?.Pause(voice, paused) ?? false;

    /// <summary>
    /// Changes a playing voice from this update on; null, or a value that is not a number, keeps a value. Called every
    /// update it follows something that moves: the pan and volume of a sound source crossing the screen, the pitch of
    /// an engine.
    /// </summary>
    /// <param name="voice">A handle from <see cref="PlayVoice(string, in PlayParameters)"/> or <see cref="PlayMusicVoice"/>.</param>
    /// <param name="volume">Volume, 0..1.</param>
    /// <param name="pan">Stereo position, 0 (left) to 1 (right).</param>
    /// <param name="pitch">Playback speed factor, 1 for the original pitch (0.01..100); ignored by streamed music.</param>
    /// <param name="seconds">
    /// The time the values take to get there, frame by frame: a fade, a sweep across the screen, a glide. 0 (the
    /// default) changes them at once, which is what a value set again every update needs.
    /// </param>
    /// <returns>Whether it was playing.</returns>
    public bool Set(VoiceHandle voice, float? volume = null, float? pan = null, float? pitch = null, float seconds = 0)
    {
        if (Engine is not { } engine || !engine.IsPlaying(voice)) return false;
        if (volume is not null || pan is not null) engine.Set(voice, volume, pan, seconds);
        if (pitch is { } p) engine.SetPitch(voice, p, seconds);
        return true;
    }

    // ---- Time: where a sound is, and moving it

    /// <summary>
    /// Where <paramref name="voice"/> is in its sound, in seconds from the sound's beginning: 0 as it starts, what
    /// <see cref="Seek"/> was given after a seek, back at the start of its loop region each time it loops. It follows
    /// the game's steps, so a scripted run reads the same values every time. 0 for a voice that is not playing.
    /// </summary>
    public double Position(VoiceHandle voice) => Engine?.Position(voice) ?? 0;

    /// <summary>
    /// The position of <paramref name="voice"/> the player hears at this update: <see cref="Position"/> less
    /// <see cref="OutputLatency"/>. Read it for anything shown or judged against the sound (a note reaching a line, a
    /// cutscene's cue). Negative while the sound's first frame is still on its way to the speaker.
    /// </summary>
    public double HeardPosition(VoiceHandle voice) => Engine?.HeardPosition(voice) ?? 0;

    /// <summary>The length in seconds of the sound <paramref name="voice"/> plays; 0 for a voice that is not playing or a stream that does not know its length.</summary>
    public double Duration(VoiceHandle voice) => Engine?.Duration(voice) ?? 0;

    /// <summary>
    /// Moves <paramref name="voice"/> to <paramref name="seconds"/> from its sound's beginning, from this update on
    /// (clamped to the sound's length; a time that is not a number changes nothing).
    /// </summary>
    /// <returns>Whether it moved: false for a voice that is not playing and for a stream whose decoder cannot seek.</returns>
    public bool Seek(VoiceHandle voice, double seconds) => Engine?.Seek(voice, seconds) ?? false;

    /// <summary>
    /// Whether <paramref name="voice"/> is still on its way to a place <see cref="Seek"/> or
    /// <see cref="PlayParameters.StartAt"/> named. Only streamed OGG and MP3 take time to get somewhere far: a stream
    /// sought while it plays goes on where it was until then (and <see cref="Position"/> with it), and one started in
    /// its middle is silent until then. Sounds and decoded music are there at once.
    /// </summary>
    public bool IsSeeking(VoiceHandle voice) => Engine?.IsSeeking(voice) ?? false;

    /// <summary>Whether the music is still on its way to the place <see cref="SeekMusic"/> named (see <see cref="IsSeeking"/>).</summary>
    public bool MusicSeeking => Engine?.IsSeeking(MusicVoice) ?? false;

    /// <summary>
    /// Seconds between the mixer writing a frame and the player hearing it, as the device reports it (0 on a silent
    /// device and when rendering offline). <see cref="HeardPosition"/> and <see cref="MusicHeardPosition"/> already
    /// take it into account.
    /// </summary>
    public double OutputLatency => Engine is { } engine ? (double)engine.DelayFrames / engine.Format.SampleRate : 0;

    // The music's clock on a device without voices: the time it has played, so a game that follows its music runs
    // the same headless.
    private double _silentMusicTime;

    private VoiceHandle MusicVoice => _device is MixerAudioDevice mixer ? mixer.MusicVoice : VoiceHandle.None;

    /// <summary>
    /// Where the music <see cref="PlayMusic"/> plays is, in seconds from its beginning (see <see cref="Position"/>);
    /// 0 with no music. Save it to bring the music back to the same place with <see cref="SeekMusic"/>. On a device
    /// without voices (<see cref="SupportsVoices"/>) it is the time the music has played, which does not come back
    /// when the music loops.
    /// </summary>
    public double MusicPosition => Engine is { } engine ? engine.Position(MusicVoice) : _device.CurrentMusic is null ? 0 : _silentMusicTime;

    /// <summary>
    /// The position of the music the player hears at this update (see <see cref="HeardPosition"/>): what a rhythm game
    /// judges input against and a cutscene cues its pictures from.
    /// </summary>
    public double MusicHeardPosition => Engine is { } engine ? engine.HeardPosition(MusicVoice) : MusicPosition;

    /// <summary>The length of the music playing, in seconds; 0 with no music, on a device without voices and for a format whose decoder does not know it.</summary>
    public double MusicDuration => Engine?.Duration(MusicVoice) ?? 0;

    /// <summary>
    /// Moves the music playing to <paramref name="seconds"/> from its beginning, from this update on (clamped to its
    /// length; a time that is not a number changes nothing).
    /// </summary>
    /// <returns>Whether it moved: false with no music and for a format whose decoder cannot seek.</returns>
    public bool SeekMusic(double seconds)
    {
        if (Engine is { } engine) return engine.Seek(MusicVoice, seconds);
        if (_device.CurrentMusic is null) return false;
        if (!double.IsNaN(seconds)) _silentMusicTime = Math.Max(0, seconds);
        return true;
    }

    // ---- Buses

    // The buses of a device without the engine's mixer: they keep what they are told (volumes, effects), so the same
    // calls work and read back headless, with the same checks.
    private List<AudioBus>? _silentBuses;
    private List<AudioBus> SilentBuses
    {
        get
        {
            if (_silentBuses is not null) return _silentBuses;
            var buses = _silentBuses = [];
            buses.Add(new AudioBus(null, buses, 0, AudioEngine.MasterBus, null));
            buses.Add(new AudioBus(null, buses, 1, AudioEngine.SfxBus, buses[0]));
            buses.Add(new AudioBus(null, buses, 2, AudioEngine.MusicBus, buses[0]));
            return buses;
        }
    }

    private AudioBus? FindBus(string name)
    {
        IReadOnlyList<AudioBus> buses = Engine is { } engine ? engine.Buses : SilentBuses;
        for (int i = 0; i < buses.Count; i++) if (buses[i].Name == name) return buses[i];
        return null;
    }

    /// <summary>
    /// Whether sounds play as voices on this device (<see cref="PlayVoice(string, in PlayParameters)"/>, <see cref="PlayMusicVoice"/>): true for
    /// every device that plays through the engine's mixer, false for a silent one (headless, muted, no hardware), where
    /// those calls return <see cref="VoiceHandle.None"/>. Buses and their effects can be set up either way.
    /// </summary>
    public bool SupportsVoices => Engine is not null;

    /// <summary>Whether a bus is named <paramref name="name"/>. "master", "sfx" (sounds, by default) and "music" always exist.</summary>
    public bool HasBus(string name) => FindBus(name) is not null;

    private void RequireBus(string name) => Bus(name);

    /// <summary>
    /// The bus named <paramref name="name"/>: its <see cref="AudioBus.Volume"/> and <see cref="AudioBus.FadeTo"/>, and
    /// its effects (<see cref="AudioBus.Add"/>): a filter, a reverb, a compressor that ducks it under another bus.
    /// </summary>
    /// <exception cref="KeyNotFoundException">No such bus.</exception>
    public AudioBus Bus(string name) => FindBus(name) ?? throw new KeyNotFoundException($"No audio bus is named '{name}'.");

    /// <summary>
    /// The limiter after "master": on a device that plays, a mix louder than full scale is turned down instead of
    /// clipped. <see cref="AudioEffect.Bypass"/> switches it off.
    /// </summary>
    public Effects.Limiter Limiter => Engine?.Limiter ?? (_silentLimiter ??= new Effects.Limiter());
    private Effects.Limiter? _silentLimiter;

    /// <summary>
    /// Adds a bus playing into <paramref name="parent"/> and returns it: a group of sounds with one volume and one
    /// chain of effects ("ambience", "voices", "ui"), named by <see cref="PlayParameters.Bus"/>. At most 32 buses exist.
    /// </summary>
    /// <exception cref="ArgumentException">A bus already has that name, or the name is empty.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="parent"/> does not exist.</exception>
    /// <exception cref="InvalidOperationException">There are 32 buses already.</exception>
    public AudioBus AddBus(string name, string parent = AudioEngine.MasterBus)
    {
        if (Engine is { } engine) return engine.AddBus(name, parent);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (HasBus(name)) throw new ArgumentException($"An audio bus is already named '{name}'.", nameof(name));
        var up = Bus(parent);
        var buses = SilentBuses;
        if (buses.Count == 32) throw new InvalidOperationException("An audio engine has at most 32 buses.");
        buses.Add(new AudioBus(null, buses, buses.Count, name, up));
        return buses[^1];
    }

    /// <summary>The volume of bus <paramref name="name"/>.</summary>
    /// <exception cref="KeyNotFoundException">No such bus.</exception>
    public float BusVolume(string name) => Bus(name).Volume;

    /// <summary>
    /// Sets the volume of everything playing through bus <paramref name="name"/> and the buses under it, clamped to
    /// 0..4, from this update on: "master" for the whole game, "sfx" for every sound (those of <see cref="Play(string, float, float, float, float)"/> too,
    /// on top of <see cref="SfxVolume"/>), "music" for the music track and the voices played on it. A value that is
    /// not a number is ignored. With <paramref name="fadeSeconds"/> the volume moves there over that time instead of
    /// at once.
    /// </summary>
    /// <exception cref="KeyNotFoundException">No such bus.</exception>
    public void SetBusVolume(string name, float volume, float fadeSeconds = 0)
    {
        var bus = Bus(name);
        if (fadeSeconds > 0) bus.FadeTo(volume, fadeSeconds);
        else bus.Volume = volume;
    }

    /// <summary>
    /// Switches music. With a fade, the current track fades out before the new one starts; with
    /// <paramref name="crossfade"/>, the new track starts now and rises while the current one falls, both over that
    /// many seconds.
    /// </summary>
    /// <param name="name">The music, as registered.</param>
    /// <param name="fadeSeconds">Seconds the current track takes to fade out before the new one starts (0.4 by default; 0 or less switches at once). Not used with a crossfade.</param>
    /// <param name="crossfade">
    /// Seconds over which the two tracks overlap, the new one rising in a straight line as the old one falls (0, the
    /// default: no overlap). On a device without voices (<see cref="SupportsVoices"/>), and for a song that is still
    /// being rendered, the music switches as without it.
    /// </param>
    /// <remarks>With a fade, music starting from silence rises over about a quarter of a second; with <paramref name="fadeSeconds"/> 0 or less it starts at full volume at once. Asking for the music that is already playing does nothing; asking for it while it fades out (towards another track or silence) cancels the fade and brings it back, without restarting it.</remarks>
    /// <exception cref="KeyNotFoundException">No music is registered under <paramref name="name"/>.</exception>
    /// <exception cref="InvalidOperationException">A song registered with <see cref="AddMusic(string, Song, bool)"/>
    /// failed while rendering (its content is checked when registered, so only the machine can fail it, for example out
    /// of memory); its name is then unregistered. With a fade, this surfaces from <see cref="Update"/> when the song is
    /// due to start.</exception>
    public void PlayMusic(string name, float fadeSeconds = 0.4f, float crossfade = 0f)
    {
        if (!HasMusic(name)) throw new KeyNotFoundException($"Music '{name}' is not registered.");
        if (_waiting?.Name == name) return;
        if (_device.CurrentMusic?.Name == name)
        {
            if (_pendingMusic is null) return;
            // The track is fading out towards another one (or silence): keep it, and fade it back in from where it is.
            _pendingMusic = null;
            _fadeSpeed = fadeSeconds > 0 ? 1f / fadeSeconds : 0;
            if (fadeSeconds <= 0) { _fade = 1; Apply(); }
            return;
        }
        if (crossfade > 0 && _device is MixerAudioDevice mixer && !_sliced.Exists(s => s.Name == name))
        {
            // Both tracks at once: the device fades the one playing out and this one in, frame by frame.
            _pendingMusic = null;
            _waiting = null;
            (_fade, _fadeSpeed) = (1, 0);
            _startedMusic = name;
            mixer.CrossfadeMusic(Music(name), _musicVolume, crossfade);
            return;
        }
        if (fadeSeconds <= 0) { StartMusic(name, fadeIn: false); return; }
        if (_device.CurrentMusic is null) { StartMusic(name); return; }
        _pendingMusic = name;
        _fadeSpeed = -1f / fadeSeconds;
    }

    /// <summary>Stops the music, fading it out over <paramref name="fadeSeconds"/>; 0 or less stops it at once.</summary>
    public void StopMusic(float fadeSeconds = 0.4f)
    {
        _waiting = null;
        if (fadeSeconds <= 0) { _device.StopMusic(); _pendingMusic = null; return; }
        _pendingMusic = "";
        _fadeSpeed = -1f / fadeSeconds;
    }

    /// <summary>Pauses (true) or resumes (false) the music and records the state in <see cref="MusicPaused"/>.</summary>
    public void PauseMusic(bool paused)
    {
        MusicPaused = paused;
        _device.PauseMusic(paused);
    }

    /// <summary>Whether music was paused with <see cref="PauseMusic"/>.</summary>
    public bool MusicPaused { get; private set; }

    private void StartMusic(string name, bool fadeIn = true)
    {
        _pendingMusic = null;
        _waiting = null;
        int sliced = _sliced.FindIndex(s => s.Name == name);
        if (sliced >= 0)
        {
            // Still rendering without threads: it goes first and starts when ready; meanwhile nothing plays.
            var song = _sliced[sliced];
            _sliced.RemoveAt(sliced);
            _sliced.Insert(0, song);
            _waiting = (name, fadeIn);
            _device.StopMusic();
            return;
        }
        _fade = fadeIn ? 0.05f : 1f;
        _fadeSpeed = fadeIn ? 4f : 0f;
        _silentMusicTime = 0;
        _startedMusic = name;
        _device.PlayMusic(Music(name), _musicVolume * _fade);
    }

    private void Apply() => _device.SetMusicVolume(_musicVolume * _fade);

    /// <summary>
    /// Renders pending songs for up to <see cref="SliceBudget"/>, handing each finished one to the device; in a
    /// deterministic run, only a song the game is waiting for renders, all of it, whatever the budget.
    /// </summary>
    private void RenderSlices()
    {
        // Deterministic runs render only a song the game waits for, all of it: whether a song is ready then depends on
        // the game's requests, never on how fast the machine renders.
        if (Deterministic && _waiting is null) return;
        long due = System.Diagnostics.Stopwatch.GetTimestamp() + (long)(SliceBudget.TotalSeconds * System.Diagnostics.Stopwatch.Frequency);
        while (_sliced.Count > 0 && (System.Diagnostics.Stopwatch.GetTimestamp() < due || Deterministic && _waiting is not null))
        {
            var (name, slices, loop) = _sliced[0];
            if (!slices.MoveNext()) { _sliced.RemoveAt(0); continue; }
            if (slices.Current is { } clip)
            {
                _sliced.RemoveAt(0);
                Register(name, _device.LoadMusic(name, clip, loop));
                if (_waiting is { } waiting
 && waiting.Name == name) StartMusic(name, waiting.FadeIn);
            }
        }
    }

    /// <summary>
    /// The host frame's audio work, outside the simulation step: renders songs that render in slices (browsers), then
    /// lets the device decode streamed music ahead and pump its sink. The game calls it once per frame; call it yourself
    /// only for a mixer you created.
    /// </summary>
    public void Pump()
    {
        if (_sliced.Count > 0) RenderSlices();
        if (_device is MixerAudioDevice mixer) mixer.Pump();
    }

    /// <summary>
    /// Whether the run is deterministic (scripted runs, tests): the device's timeline then follows the simulation only,
    /// and songs that render in slices render only when the game waits for one, finished in the next <see cref="Pump"/>
    /// whatever <see cref="SliceBudget"/>, so the
    /// same steps give the same sound. The game sets it from its time mode.
    /// </summary>
    public bool Deterministic
    {
        get => _deterministic;
        set
        {
            _deterministic = value;
            if (_device is MixerAudioDevice mixer && mixer is not CaptureAudioDevice) mixer.Engine.Deterministic = value;
        }
    }
    private bool _deterministic;

    /// <summary>Advances music fades and ends the device's simulation step. The game calls it once per update; call it yourself only for a mixer you created.</summary>
    /// <param name="dt">Elapsed time in seconds.</param>
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
        if (!MusicPaused && !double.IsNaN(dt) && dt > 0) _silentMusicTime += dt;
        _device.Update();

    }
}

/// <summary>What <see cref="AudioMixer.Statistics"/> reports about the mixer's voices.</summary>
/// <param name="ActiveVoices">Sounds playing now, music included.</param>
/// <param name="MaxVoices">Sounds that can play at once.</param>
/// <param name="RefusedSounds">Sounds the voice policy refused since the start because every voice played something of higher priority.</param>
public readonly record struct AudioStatistics(int ActiveVoices, int MaxVoices, int RefusedSounds);

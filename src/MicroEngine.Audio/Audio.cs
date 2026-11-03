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
/// Games register synthesized clips once and then call <see cref="Play"/> by name.
/// </summary>
/// <remarks>
/// Every game has one in <c>Game.Audio</c>. Sounds and music are registered under names that are compared
/// ordinally (case-sensitive); registering a name again replaces it. On a device without hardware every call still works
/// and nothing is heard.
/// </remarks>
/// <example>
/// <code>
/// // In Game.Load:
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
    public AudioMixer(IAudioDevice device) => _device = device ?? throw new ArgumentNullException(nameof(device));

    /// <summary>The audio device the mixer plays through.</summary>
    public IAudioDevice Device => _device;
    /// <summary>Volume multiplier for sound effects, 0..1 (default 0.8). Each played volume is multiplied by it and clamped to 0..1.</summary>
    public float SfxVolume { get; set; } = 0.8f;
    /// <summary>Music volume, clamped to 0..1 (default 1). Applies immediately to the playing music, combined with any fade.</summary>
    public float MusicVolume { get => _musicVolume; set { _musicVolume = Math.Clamp(value, 0, 1); Apply(); } }
    /// <summary>Name of the music playing (also while it fades out, and while a song asked for still renders in slices), or null. It follows the game's requests and the music's end on the simulation's timeline, never rendering speed.</summary>
    public string? CurrentMusic => _waiting?.Name ?? _device.CurrentMusic?.Name;

    /// <summary>Registers <paramref name="clip"/> as the sound effect <paramref name="name"/>, replacing any sound with that name.</summary>
    public void AddSound(string name, AudioClip clip) => _sounds[name] = _device.LoadSound(name, clip);
    /// <summary>Registers decoded music, such as a rendered <see cref="Song"/>, under <paramref name="name"/>.</summary>
    /// <param name="name">Name used by <see cref="PlayMusic"/>.</param>
    /// <param name="clip">The music samples.</param>
    /// <param name="loop">Whether the music restarts when it reaches its end.</param>
    public void AddMusic(string name, AudioClip clip, bool loop = true)
    {
        _songs.Remove(name);
        _sliced.RemoveAll(s => s.Name == name);
        _music[name] = _device.LoadMusic(name, clip, loop);
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
        _music.Remove(name);
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
            _music[name] = _device.LoadMusic(name, clip!, loop);
        }
        if (_songs.Remove(name, out var song))
        {
            AudioClip clip;
            try { clip = song.Render.GetAwaiter().GetResult(); }
            catch (Exception e) { throw new InvalidOperationException($"Music '{name}' could not be rendered: {e.Message}", e); }
            _music[name] = _device.LoadMusic(name, clip, song.Loop);
        }
        return _music[name];
    }
    /// <summary>Whether a sound effect is registered under <paramref name="name"/>.</summary>
    public bool HasSound(string name) => _sounds.ContainsKey(name);
    /// <summary>Whether music is registered under <paramref name="name"/>.</summary>
    public bool HasMusic(string name) => _music.ContainsKey(name) || _songs.ContainsKey(name) || _sliced.Exists(s => s.Name == name);

    /// <summary>Registers a WAV file as a sound effect.</summary>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="NotSupportedException">The WAV is not 8, 16 or 24-bit PCM, mono or stereo.</exception>
    public void AddSoundFile(string name, string path) => AddSound(name, AudioClip.FromWav(File.ReadAllBytes(path)));

    /// <summary>Registers a music file. WAV is decoded here; other formats are streamed by the device.</summary>
    /// <remarks>Formats other than wav depend on the audio device: the SDL device streams ogg; the raylib device also decodes mp3.</remarks>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    public void AddMusicFile(string name, string path, bool loop = true)
    {
        var bytes = File.ReadAllBytes(path);
        var format = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
        _songs.Remove(name);
        _sliced.RemoveAll(s => s.Name == name);
        _music[name] = format == "wav"
            ? _device.LoadMusic(name, AudioClip.FromWav(bytes), loop)
            : _device.LoadMusicStream(name, bytes, format, loop);
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

    /// <summary>Plays a sound if it is registered; returns false otherwise (useful for optional cues).</summary>
    public bool TryPlay(string name, float volume = 1f, float pitch = 1f, float pitchJitter = 0f, float pan = 0.5f)
    {
        if (!_sounds.ContainsKey(name)) return false;
        Play(name, volume, pitch, pitchJitter, pan);
        return true;
    }

    /// <summary>Switches music. With a fade, the current track fades out before the new one starts.</summary>
    /// <remarks>With a fade, music starting from silence rises over about a quarter of a second; with <paramref name="fadeSeconds"/> 0 or less it starts at full volume at once. Asking for the music that is already playing does nothing; asking for it while it fades out (towards another track or silence) cancels the fade and brings it back, without restarting it.</remarks>
    /// <exception cref="KeyNotFoundException">No music is registered under <paramref name="name"/>.</exception>
    /// <exception cref="InvalidOperationException">A song registered with <see cref="AddMusic(string, Song, bool)"/>
    /// failed while rendering (its content is checked when registered, so only the machine can fail it, for example out
    /// of memory); its name is then unregistered. With a fade, this surfaces from <see cref="Update"/> when the song is
    /// due to start.</exception>
    public void PlayMusic(string name, float fadeSeconds = 0.4f)
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
                _music[name] = _device.LoadMusic(name, clip, loop);
                if (_waiting is { } waiting && waiting.Name == name) StartMusic(name, waiting.FadeIn);
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
        _device.Update();
    }
}

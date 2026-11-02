namespace MicroEngine.Audio;

/// <summary>
/// An audio device that plays nothing aloud and records what would have been heard: it mixes sound effects and music
/// into stereo samples, advancing one game step per <see cref="Update"/> (the game calls it once per update), so the same
/// run always produces the same samples. Tests compare its <see cref="Output"/> (waves, durations, silences, amplitudes);
/// the launcher's <c>--audio-capture FILE</c> writes it as a WAV.
/// </summary>
/// <remarks>
/// It reproduces what the engine asks of a device (volume, pitch, balance panning as the SDL and raylib devices apply it,
/// looping, pause, music volume), not the exact resampling or timing of a sound card. Compressed music streams
/// (<see cref="LoadMusicStream"/>) other than WAV are tracked but not decoded, so they are captured as silence.
/// </remarks>
public sealed class CaptureAudioDevice : IAudioDevice
{
    private readonly List<AudioClip> _sounds = [];
    private readonly List<(AudioClip? Clip, bool Loop)> _music = [];
    private readonly List<Voice> _voices = [];
    private readonly List<float> _output = [];
    private readonly double _step;
    private double _due;
    private int _currentMusic = -1;
    private double _musicPosition;
    private float _musicVolume = 1;
    private bool _musicPaused;

    /// <summary>Creates a capture device.</summary>
    /// <param name="updateRate">Updates per second of the game (<see cref="Core.GameSettings.UpdateRate"/>); each <see cref="Update"/> captures 1/<paramref name="updateRate"/> seconds.</param>
    /// <param name="sampleRate">Samples per second of the capture.</param>
    /// <exception cref="ArgumentOutOfRangeException">A rate is not positive.</exception>
    public CaptureAudioDevice(int updateRate = 60, int sampleRate = AudioClip.DefaultSampleRate)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(updateRate, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(sampleRate, 1);
        _step = 1.0 / updateRate;
        SampleRate = sampleRate;
    }

    /// <summary>Samples per second of <see cref="Output"/>.</summary>
    public int SampleRate { get; }
    /// <summary>False: nothing reaches a speaker.</summary>
    public bool IsAvailable => false;
    /// <inheritdoc/>
    public MusicHandle? CurrentMusic { get; private set; }
    /// <summary>The sounds played, in order: name, the second of the capture it started at, volume, pitch and pan.</summary>
    public List<(string Name, double Time, float Volume, float Pitch, float Pan)> Played { get; } = [];
    /// <summary>Seconds captured so far.</summary>
    public double Duration => _output.Count / 2.0 / SampleRate;
    /// <summary>Everything captured so far, as an interleaved stereo clip (samples clamped to -1..1).</summary>
    public AudioClip Output => new([.. _output.Select(s => Math.Clamp(s, -1f, 1f))], SampleRate, 2);

    /// <inheritdoc/>
    public SoundHandle LoadSound(string name, AudioClip clip)
    {
        ArgumentNullException.ThrowIfNull(clip);
        _sounds.Add(clip);
        return new SoundHandle(_sounds.Count - 1, name);
    }

    /// <inheritdoc/>
    public MusicHandle LoadMusic(string name, AudioClip clip, bool loop = true)
    {
        ArgumentNullException.ThrowIfNull(clip);
        _music.Add((clip, loop));
        return new MusicHandle(_music.Count - 1, name);
    }

    /// <inheritdoc/>
    public MusicHandle LoadMusicStream(string name, byte[] encoded, string format, bool loop = true)
    {
        ArgumentNullException.ThrowIfNull(encoded);
        _music.Add((format.TrimStart('.').Equals("wav", StringComparison.OrdinalIgnoreCase) ? AudioClip.FromWav(encoded) : null, loop));
        return new MusicHandle(_music.Count - 1, name);
    }

    /// <inheritdoc/>
    public void Play(SoundHandle sound, float volume = 1, float pitch = 1, float pan = 0.5f)
    {
        Played.Add((sound.Name, Duration, volume, pitch, pan));
        pan = Math.Clamp(pan, 0, 1);
        // Balance panning, as the SDL device (SdlAudioDevice.Pan) and raylib's miniaudio apply it.
        _voices.Add(new Voice(_sounds[sound.Id], Math.Clamp(volume, 0, 1), Math.Clamp(pitch, 0.01f, 100f), Math.Min(1, 2 * (1 - pan)), Math.Min(1, 2 * pan)));
    }

    /// <inheritdoc/>
    public void PlayMusic(MusicHandle music, float volume = 1)
    {
        _currentMusic = music.Id;
        _musicPosition = 0;
        _musicVolume = volume;
        _musicPaused = false;
        CurrentMusic = music;
    }

    /// <inheritdoc/>
    public void StopMusic()
    {
        _currentMusic = -1;
        CurrentMusic = null;
    }

    /// <inheritdoc/>
    public void SetMusicVolume(float volume) => _musicVolume = volume;
    /// <inheritdoc/>
    public void PauseMusic(bool paused) => _musicPaused = paused;

    /// <summary>Captures the next game step (1/updateRate seconds) of sound.</summary>
    public void Update()
    {
        _due += _step * SampleRate;
        int frames = (int)_due;
        _due -= frames;
        for (int i = 0; i < frames; i++)
        {
            float left = 0, right = 0;
            for (int v = _voices.Count - 1; v >= 0; v--)
            {
                var voice = _voices[v];
                if (!Sample(voice.Clip, voice.Position, out float l, out float r)) { _voices.RemoveAt(v); continue; }
                left += l * voice.Volume * voice.Left;
                right += r * voice.Volume * voice.Right;
                voice.Position += voice.Pitch * voice.Clip.SampleRate / SampleRate;
            }
            if (_currentMusic >= 0 && !_musicPaused && _music[_currentMusic].Clip is { } clip)
            {
                double length = clip.Samples.Length / clip.Channels;
                if (_music[_currentMusic].Loop && _musicPosition >= length) _musicPosition %= length;
                if (Sample(clip, _musicPosition, out float l, out float r))
                {
                    left += l * _musicVolume;
                    right += r * _musicVolume;
                    _musicPosition += (double)clip.SampleRate / SampleRate;
                }
                else StopMusic();
            }
            _output.Add(left);
            _output.Add(right);
        }
    }

    /// <summary>The clip at a fractional frame position (linear interpolation); false past its end.</summary>
    private static bool Sample(AudioClip clip, double position, out float left, out float right)
    {
        int frames = clip.Samples.Length / clip.Channels;
        int i = (int)position;
        if (i >= frames) { left = right = 0; return false; }
        float t = (float)(position - i);
        int j = Math.Min(i + 1, frames - 1);
        float At(int frame, int channel) => clip.Samples[frame * clip.Channels + (clip.Channels == 2 ? channel : 0)];
        left = At(i, 0) + (At(j, 0) - At(i, 0)) * t;
        right = At(i, 1) + (At(j, 1) - At(i, 1)) * t;
        return true;
    }

    private sealed class Voice(AudioClip clip, float volume, float pitch, float left, float right)
    {
        public AudioClip Clip { get; } = clip;
        public float Volume { get; } = volume;
        public float Pitch { get; } = pitch;
        public float Left { get; } = left;
        public float Right { get; } = right;
        public double Position { get; set; }
    }
}

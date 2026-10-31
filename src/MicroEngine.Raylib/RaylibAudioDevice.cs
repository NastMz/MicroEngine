using System.Runtime.InteropServices;
using MicroEngine.Audio;
using Rl = Raylib_cs.Raylib;

namespace MicroEngine.Raylib;

/// <summary>
/// raylib audio. Each sound keeps a small pool of aliases so rapid repeats overlap instead of
/// cutting each other off. Music is streamed from an unmanaged copy of the rendered WAV that
/// lives as long as the stream (raylib reads it lazily).
/// </summary>
internal sealed unsafe class RaylibAudioDevice : IAudioDevice, IDisposable
{
    private const int Voices = 4;
    private readonly List<(Raylib_cs.Sound Main, Raylib_cs.Sound[] Aliases)> _sounds = [];
    private readonly List<(Raylib_cs.Music Music, nint Memory)> _music = [];
    private readonly int[] _next = new int[1024];
    private int _current = -1;
    private float _musicVolume = 1;

    private RaylibAudioDevice() { }

    public static IAudioDevice TryCreate()
    {
        try
        {
            Rl.InitAudioDevice();
            if (Rl.IsAudioDeviceReady()) return new RaylibAudioDevice();
            Console.Error.WriteLine("audio: no device available, continuing silently");
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
        {
            Console.Error.WriteLine($"audio: {e.Message}");
        }
        return new SilentAudioDevice();
    }

    public bool IsAvailable => true;
    public MusicHandle? CurrentMusic { get; private set; }

    public SoundHandle LoadSound(string name, AudioClip clip)
    {
        var wave = Rl.LoadWaveFromMemory(".wav", clip.ToWav());
        var sound = Rl.LoadSoundFromWave(wave);
        Rl.UnloadWave(wave);
        var aliases = new Raylib_cs.Sound[Voices - 1];
        for (int i = 0; i < aliases.Length; i++) aliases[i] = Rl.LoadSoundAlias(sound);
        _sounds.Add((sound, aliases));
        return new SoundHandle(_sounds.Count - 1, name);
    }

    public MusicHandle LoadMusic(string name, AudioClip clip, bool loop = true) => LoadMusicStream(name, clip.ToWav(), "wav", loop);

    public MusicHandle LoadMusicStream(string name, byte[] encoded, string format, bool loop = true)
    {
        var memory = (nint)NativeMemory.Alloc((nuint)encoded.Length);
        Marshal.Copy(encoded, 0, memory, encoded.Length);
        var extension = "." + format.TrimStart('.').ToLowerInvariant();
        var type = stackalloc sbyte[extension.Length + 1];
        for (int i = 0; i < extension.Length; i++) type[i] = (sbyte)extension[i];
        type[extension.Length] = 0;
        var music = Rl.LoadMusicStreamFromMemory(type, (byte*)memory, encoded.Length);
        if (music.FrameCount == 0)
        {
            NativeMemory.Free((void*)memory);
            throw new InvalidDataException($"Music '{name}' could not be decoded as {format}.");
        }
        music.Looping = loop;
        _music.Add((music, memory));
        return new MusicHandle(_music.Count - 1, name);
    }

    public void Play(SoundHandle sound, float volume = 1, float pitch = 1, float pan = 0.5f)
    {
        var (main, aliases) = _sounds[sound.Id];
        int slot = _next[sound.Id % _next.Length]++ % Voices;
        var voice = slot == 0 ? main : aliases[slot - 1];
        Rl.SetSoundVolume(voice, volume);
        Rl.SetSoundPitch(voice, pitch);
        Rl.SetSoundPan(voice, 1 - pan); // raylib pan: 1 = left, 0 = right
        Rl.PlaySound(voice);
    }

    public void PlayMusic(MusicHandle music, float volume = 1)
    {
        StopMusic();
        _current = music.Id;
        CurrentMusic = music;
        _musicVolume = volume;
        var m = _music[_current].Music;
        Rl.SetMusicVolume(m, volume);
        Rl.PlayMusicStream(m);
    }

    public void StopMusic()
    {
        if (_current >= 0) Rl.StopMusicStream(_music[_current].Music);
        _current = -1;
        CurrentMusic = null;
    }

    public void SetMusicVolume(float volume)
    {
        _musicVolume = volume;
        if (_current >= 0) Rl.SetMusicVolume(_music[_current].Music, volume);
    }

    public void PauseMusic(bool paused)
    {
        if (_current < 0) return;
        if (paused) Rl.PauseMusicStream(_music[_current].Music);
        else Rl.ResumeMusicStream(_music[_current].Music);
    }

    public void Update()
    {
        if (_current >= 0) Rl.UpdateMusicStream(_music[_current].Music);
    }

    public void Dispose()
    {
        StopMusic();
        foreach (var (main, aliases) in _sounds)
        {
            foreach (var a in aliases) Rl.UnloadSoundAlias(a);
            Rl.UnloadSound(main);
        }
        foreach (var (music, memory) in _music)
        {
            Rl.UnloadMusicStream(music);
            NativeMemory.Free((void*)memory);
        }
        Rl.CloseAudioDevice();
    }
}

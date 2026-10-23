namespace MicroEngine.Desktop;

internal interface IDesktopAudioApi
{
    void Init(); bool Ready { get; }
    object Load(byte[] encoded);
    void Volume(object sound,float volume);
    void Play(object sound); void Stop(object sound); void Unload(object sound); void Close();
}
internal sealed class AudioReservation
{
    private int _taken;
    internal void Take()
    {
        if(Interlocked.CompareExchange(ref _taken,1,0)!=0)throw new InvalidOperationException("Audio device already reserved or cleanup uncertain.");
    }
    internal void Release()=>Interlocked.Exchange(ref _taken,0);
}

internal sealed class RaylibAudioApi : IDesktopAudioApi
{
    public bool Ready=>Raylib_cs.Raylib.IsAudioDeviceReady();
    public void Init()
    {
        if(!OperatingSystem.IsWindows())throw new PlatformNotSupportedException("Desktop audio is validated on Windows only.");
        Raylib_cs.Raylib.InitAudioDevice();
    }
    public object Load(byte[] encoded)
    {
        var wave=Raylib_cs.Raylib.LoadWaveFromMemory(".wav",encoded);
        try
        {
            if(!Raylib_cs.Raylib.IsWaveValid(wave))throw new InvalidDataException("Native WAV decode failed.");
            var sound=Raylib_cs.Raylib.LoadSoundFromWave(wave);
            if(!Raylib_cs.Raylib.IsSoundValid(sound))
            {
                Raylib_cs.Raylib.UnloadSound(sound);throw new IOException("Native sound creation failed.");
            }
            return sound;
        }
        finally{Raylib_cs.Raylib.UnloadWave(wave);}
    }
    public void Volume(object sound,float volume)=>Raylib_cs.Raylib.SetSoundVolume((Raylib_cs.Sound)sound,volume);
    public void Play(object sound)=>Raylib_cs.Raylib.PlaySound((Raylib_cs.Sound)sound);
    public void Stop(object sound)=>Raylib_cs.Raylib.StopSound((Raylib_cs.Sound)sound);
    public void Unload(object sound)=>Raylib_cs.Raylib.UnloadSound((Raylib_cs.Sound)sound);
    public void Close()=>Raylib_cs.Raylib.CloseAudioDevice();
}

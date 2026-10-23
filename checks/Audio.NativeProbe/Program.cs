using MicroEngine.Desktop;
using Raylib_cs;
try
{
    if(Raylib.IsWindowReady())throw new InvalidOperationException("Audio probe must not own a window.");
    var waves=new Dictionary<string,byte[]>(StringComparer.Ordinal);
    foreach(var name in new[]{"jump_1.wav","button.wav","playerhurt.wav","spring.wav"})
        waves.Add(name,File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Assets",name)));
        Raylib.InitAudioDevice();
    if(!Raylib.IsAudioDeviceReady())throw new AudioDeviceUnavailableException();
    try
    {
        bool rejected=false;
        try{using var foreign=new DesktopOneShotAudio(waves);}catch(InvalidOperationException){rejected=true;}
        if(!rejected||!Raylib.IsAudioDeviceReady())throw new InvalidOperationException("Foreign native audio ownership was not preserved.");
    }
    finally{Raylib.CloseAudioDevice();}
    for(int run=0;run<2;run++)
    {
        using(var audio=new DesktopOneShotAudio(waves))
        {
            if(!Raylib.IsAudioDeviceReady()||Raylib.IsWindowReady())throw new InvalidOperationException("Standalone audio initialization failed.");
            foreach(var cue in waves.Keys){audio.Play(cue);Thread.Sleep(30);}
            audio.StopAll();
            try{audio.Play("unknown");throw new InvalidOperationException("Unknown cue accepted.");}catch(KeyNotFoundException){}
        }
        if(Raylib.IsAudioDeviceReady())throw new InvalidOperationException("Audio device remained open.");
    }
    Console.WriteLine("audio:device-load-play-stop-reopen:verified");return 0;
}
catch(AudioDeviceUnavailableException error){Console.Error.WriteLine($"audio:hardware-unavailable:{error.Message}");return 3;}
catch(Exception error){Console.Error.WriteLine(error);return 1;}

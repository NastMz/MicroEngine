using System.Runtime.InteropServices;
using P5.Audio;
using static SDL.SDL3;

// ARCH-1 P5 desktop probe. One JSON line per run on stdout.
//   pull                    SDL pull sink (SDL's device thread calls the mixer)
//   push <targetMs>         SDL push sink (the main loop tops the stream up to targetMs)
//   feeder <targetMs>       SDL push sink topped up by a sink-owned thread
//   offline                 determinism of offline rendering (no device)
//   cost <voices> <frames>  mixer render cost per call on this machine
// SDL_AUDIO_DRIVER (environment) picks SDL's driver; P5_FRAMES_HINT sets SDL_HINT_AUDIO_DEVICE_SAMPLE_FRAMES.
var mode = args.Length > 0 ? args[0] : "pull";
switch (mode)
{
    case "offline":
        Console.WriteLine(Determinism.Run());
        return 0;
    case "cost":
        Console.WriteLine(Cost.Run(new AudioFormat(48000, 2), args.Length > 1 ? int.Parse(args[1]) : 16, args.Length > 2 ? int.Parse(args[2]) : 1024));
        return 0;
}
if (Environment.GetEnvironmentVariable("P5_FRAMES_HINT") is { Length: > 0 } hint) SDL_SetHint(SDL_HINT_AUDIO_DEVICE_SAMPLE_FRAMES, hint);
string report = Runner.Run(mode, args.Length > 1 ? int.Parse(args[1]) : 50,
    $"desktop {Environment.OSVersion.VersionString} {RuntimeInformation.FrameworkDescription} GC={(System.Runtime.GCSettings.IsServerGC ? "server" : "workstation")}");
Console.WriteLine(report);
SDL_Quit();
return report.StartsWith("{\"error\"") ? 1 : 0;

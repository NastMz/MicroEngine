using System.Runtime.CompilerServices;

// The audio types moved to MicroEngine.Audio.dll with their namespace unchanged, so source compiles as before; these
// forwards keep code compiled against an earlier MicroEngine.dll finding them.
[assembly: TypeForwardedTo(typeof(MicroEngine.Audio.AudioClip))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Audio.SoundHandle))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Audio.MusicHandle))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Audio.IAudioDevice))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Audio.SilentAudioDevice))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Audio.AudioMixer))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Audio.EncodedAudio))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Audio.CaptureAudioDevice))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Audio.Instrument))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Audio.Song))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Audio.Wave))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Audio.SfxParams))]
[assembly: TypeForwardedTo(typeof(MicroEngine.Audio.Synth))]

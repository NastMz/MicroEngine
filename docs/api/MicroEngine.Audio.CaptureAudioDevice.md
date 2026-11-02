# Class CaptureAudioDevice

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Audio](MicroEngine.Audio.md)  
Assembly: MicroEngine.dll  
Guide: [Audio](../manual/audio.md)  

An audio device that plays nothing aloud and records what would have been heard: it mixes sound effects and music
into stereo samples, advancing one game step per [Update](MicroEngine.Audio.CaptureAudioDevice.md#MicroEngine_Audio_CaptureAudioDevice_Update) (the game calls it once per update), so the same
run always produces the same samples. Tests compare its [Output](MicroEngine.Audio.CaptureAudioDevice.md#MicroEngine_Audio_CaptureAudioDevice_Output) (waves, durations, silences, amplitudes);
the launcher's <code>--audio-capture FILE</code> writes it as a WAV.

```csharp
public sealed class CaptureAudioDevice : IAudioDevice
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[CaptureAudioDevice](MicroEngine.Audio.CaptureAudioDevice.md)

#### Implements

[IAudioDevice](MicroEngine.Audio.IAudioDevice.md)

## Remarks

It reproduces what the engine asks of a device (volume, pitch, balance panning as the SDL and raylib devices apply it,
looping, pause, music volume), not the exact resampling or timing of a sound card. Compressed music streams
([LoadMusicStream](MicroEngine.Audio.CaptureAudioDevice.md#MicroEngine_Audio_CaptureAudioDevice_LoadMusicStream_System_String_System_Byte___System_String_System_Boolean_)) other than WAV are tracked but not decoded, so they are captured as silence.

## Constructors

### <a id="MicroEngine_Audio_CaptureAudioDevice__ctor_System_Int32_System_Int32_"></a> CaptureAudioDevice\(int, int\)

Creates a capture device.

```csharp
public CaptureAudioDevice(int updateRate = 60, int sampleRate = 44100)
```

#### Parameters

`updateRate` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Updates per second of the game ([UpdateRate](MicroEngine.Core.GameSettings.md#MicroEngine_Core_GameSettings_UpdateRate)); each [Update](MicroEngine.Audio.CaptureAudioDevice.md#MicroEngine_Audio_CaptureAudioDevice_Update) captures 1/<code class="paramref">updateRate</code> seconds.

`sampleRate` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Samples per second of the capture.

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

A rate is not positive.

## Properties

### <a id="MicroEngine_Audio_CaptureAudioDevice_CurrentMusic"></a> CurrentMusic

The music started last and not stopped, or null.

```csharp
public MusicHandle? CurrentMusic { get; }
```

#### Property Value

 [MusicHandle](MicroEngine.Audio.MusicHandle.md)?

### <a id="MicroEngine_Audio_CaptureAudioDevice_Duration"></a> Duration

Seconds captured so far.

```csharp
public double Duration { get; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Audio_CaptureAudioDevice_IsAvailable"></a> IsAvailable

False: nothing reaches a speaker.

```csharp
public bool IsAvailable { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_CaptureAudioDevice_Output"></a> Output

Everything captured so far, as an interleaved stereo clip (samples clamped to -1..1).

```csharp
public AudioClip Output { get; }
```

#### Property Value

 [AudioClip](MicroEngine.Audio.AudioClip.md)

### <a id="MicroEngine_Audio_CaptureAudioDevice_Played"></a> Played

The sounds played, in order: name, the second of the capture it started at, volume, pitch and pan.

```csharp
public List<(string Name, double Time, float Volume, float Pitch, float Pan)> Played { get; }
```

#### Property Value

 [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<\([string](https://learn.microsoft.com/dotnet/api/system.string) [Name](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.string,system.double,system.single,system.single,system.single\-.name), [double](https://learn.microsoft.com/dotnet/api/system.double) [Time](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.string,system.double,system.single,system.single,system.single\-.time), [float](https://learn.microsoft.com/dotnet/api/system.single) [Volume](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.string,system.double,system.single,system.single,system.single\-.volume), [float](https://learn.microsoft.com/dotnet/api/system.single) [Pitch](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.string,system.double,system.single,system.single,system.single\-.pitch), [float](https://learn.microsoft.com/dotnet/api/system.single) [Pan](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.string,system.double,system.single,system.single,system.single\-.pan)\)\>

### <a id="MicroEngine_Audio_CaptureAudioDevice_SampleRate"></a> SampleRate

Samples per second of [Output](MicroEngine.Audio.CaptureAudioDevice.md#MicroEngine_Audio_CaptureAudioDevice_Output).

```csharp
public int SampleRate { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Audio_CaptureAudioDevice_LoadMusic_System_String_MicroEngine_Audio_AudioClip_System_Boolean_"></a> LoadMusic\(string, AudioClip, bool\)

Uploads decoded music and returns its handle.

```csharp
public MusicHandle LoadMusic(string name, AudioClip clip, bool loop = true)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

Name reported by [Name](MicroEngine.Audio.MusicHandle.md#MicroEngine_Audio_MusicHandle_Name).

`clip` [AudioClip](MicroEngine.Audio.AudioClip.md)

The music samples.

`loop` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether the music restarts when it reaches its end.

#### Returns

 [MusicHandle](MicroEngine.Audio.MusicHandle.md)

### <a id="MicroEngine_Audio_CaptureAudioDevice_LoadMusicStream_System_String_System_Byte___System_String_System_Boolean_"></a> LoadMusicStream\(string, byte\[\], string, bool\)

Streams compressed music (e.g. "ogg", "mp3", "wav") that the device decodes itself.

```csharp
public MusicHandle LoadMusicStream(string name, byte[] encoded, string format, bool loop = true)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`encoded` [byte](https://learn.microsoft.com/dotnet/api/system.byte)\[\]

`format` [string](https://learn.microsoft.com/dotnet/api/system.string)

`loop` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Returns

 [MusicHandle](MicroEngine.Audio.MusicHandle.md)

#### Remarks

Formats depend on the device: the SDL device streams wav and ogg; the raylib device also decodes mp3.

### <a id="MicroEngine_Audio_CaptureAudioDevice_LoadSound_System_String_MicroEngine_Audio_AudioClip_"></a> LoadSound\(string, AudioClip\)

Uploads a sound effect and returns its handle.

```csharp
public SoundHandle LoadSound(string name, AudioClip clip)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`clip` [AudioClip](MicroEngine.Audio.AudioClip.md)

#### Returns

 [SoundHandle](MicroEngine.Audio.SoundHandle.md)

### <a id="MicroEngine_Audio_CaptureAudioDevice_PauseMusic_System_Boolean_"></a> PauseMusic\(bool\)

Pauses (true) or resumes (false) the current music.

```csharp
public void PauseMusic(bool paused)
```

#### Parameters

`paused` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_CaptureAudioDevice_Play_MicroEngine_Audio_SoundHandle_System_Single_System_Single_System_Single_"></a> Play\(SoundHandle, float, float, float\)

Plays a sound effect once, mixed with any sounds already playing.

```csharp
public void Play(SoundHandle sound, float volume = 1, float pitch = 1, float pan = 0.5)
```

#### Parameters

`sound` [SoundHandle](MicroEngine.Audio.SoundHandle.md)

A handle from [LoadSound](MicroEngine.Audio.IAudioDevice.md#MicroEngine_Audio_IAudioDevice_LoadSound_System_String_MicroEngine_Audio_AudioClip_).

`volume` [float](https://learn.microsoft.com/dotnet/api/system.single)

Volume, 0..1.

`pitch` [float](https://learn.microsoft.com/dotnet/api/system.single)

Playback speed factor; 1 is the original pitch.

`pan` [float](https://learn.microsoft.com/dotnet/api/system.single)

Stereo position, 0 (left) to 1 (right); 0.5 is centred.

### <a id="MicroEngine_Audio_CaptureAudioDevice_PlayMusic_MicroEngine_Audio_MusicHandle_System_Single_"></a> PlayMusic\(MusicHandle, float\)

Starts <code class="paramref">music</code> from the beginning, replacing the music that was playing.

```csharp
public void PlayMusic(MusicHandle music, float volume = 1)
```

#### Parameters

`music` [MusicHandle](MicroEngine.Audio.MusicHandle.md)

`volume` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_CaptureAudioDevice_SetMusicVolume_System_Single_"></a> SetMusicVolume\(float\)

Sets the volume of the current music, 0..1.

```csharp
public void SetMusicVolume(float volume)
```

#### Parameters

`volume` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_CaptureAudioDevice_StopMusic"></a> StopMusic\(\)

Stops the music immediately.

```csharp
public void StopMusic()
```

### <a id="MicroEngine_Audio_CaptureAudioDevice_Update"></a> Update\(\)

Captures the next game step (1/updateRate seconds) of sound.

```csharp
public void Update()
```

</div>

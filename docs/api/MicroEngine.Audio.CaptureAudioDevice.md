# Class CaptureAudioDevice

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Audio](MicroEngine.Audio.md)  
Assembly: MicroEngine.Audio.dll  
Guide: [Audio](../manual/audio.md)  

An audio device that plays nothing aloud and records what would have been heard: the engine's own mixer
([MixerAudioDevice](MicroEngine.Audio.MixerAudioDevice.md)) rendering into an offline sink, one game step per [Update](MicroEngine.Audio.CaptureAudioDevice.md#MicroEngine_Audio_CaptureAudioDevice_Update) (the game calls
it once per update), so the same run always produces the same samples. Tests compare its [Output](MicroEngine.Audio.CaptureAudioDevice.md#MicroEngine_Audio_CaptureAudioDevice_Output) (waves,
durations, silences, amplitudes); the launcher's <code>--audio-capture FILE</code> writes it as a WAV.

```csharp
public sealed class CaptureAudioDevice : MixerAudioDevice, IAudioDevice, IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[MixerAudioDevice](MicroEngine.Audio.MixerAudioDevice.md) ← 
[CaptureAudioDevice](MicroEngine.Audio.CaptureAudioDevice.md)

#### Implements

[IAudioDevice](MicroEngine.Audio.IAudioDevice.md), 
[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

#### Inherited Members

[MixerAudioDevice.LoadSound\(string, AudioClip\)](MicroEngine.Audio.MixerAudioDevice.md\#MicroEngine\_Audio\_MixerAudioDevice\_LoadSound\_System\_String\_MicroEngine\_Audio\_AudioClip\_), 
[MixerAudioDevice.LoadMusic\(string, AudioClip, bool\)](MicroEngine.Audio.MixerAudioDevice.md\#MicroEngine\_Audio\_MixerAudioDevice\_LoadMusic\_System\_String\_MicroEngine\_Audio\_AudioClip\_System\_Boolean\_), 
[MixerAudioDevice.LoadMusicStream\(string, byte\[\], string, bool\)](MicroEngine.Audio.MixerAudioDevice.md\#MicroEngine\_Audio\_MixerAudioDevice\_LoadMusicStream\_System\_String\_System\_Byte\_\_\_System\_String\_System\_Boolean\_), 
[MixerAudioDevice.Play\(SoundHandle, float, float, float\)](MicroEngine.Audio.MixerAudioDevice.md\#MicroEngine\_Audio\_MixerAudioDevice\_Play\_MicroEngine\_Audio\_SoundHandle\_System\_Single\_System\_Single\_System\_Single\_), 
[MixerAudioDevice.PlayVoice\(SoundHandle, in PlayParameters\)](MicroEngine.Audio.MixerAudioDevice.md\#MicroEngine\_Audio\_MixerAudioDevice\_PlayVoice\_MicroEngine\_Audio\_SoundHandle\_MicroEngine\_Audio\_PlayParameters\_\_), 
[MixerAudioDevice.PlayMusicVoice\(MusicHandle, in PlayParameters\)](MicroEngine.Audio.MixerAudioDevice.md\#MicroEngine\_Audio\_MixerAudioDevice\_PlayMusicVoice\_MicroEngine\_Audio\_MusicHandle\_MicroEngine\_Audio\_PlayParameters\_\_), 
[MixerAudioDevice.PlayMusic\(MusicHandle, float\)](MicroEngine.Audio.MixerAudioDevice.md\#MicroEngine\_Audio\_MixerAudioDevice\_PlayMusic\_MicroEngine\_Audio\_MusicHandle\_System\_Single\_), 
[MixerAudioDevice.StopMusic\(\)](MicroEngine.Audio.MixerAudioDevice.md\#MicroEngine\_Audio\_MixerAudioDevice\_StopMusic), 
[MixerAudioDevice.SetMusicVolume\(float\)](MicroEngine.Audio.MixerAudioDevice.md\#MicroEngine\_Audio\_MixerAudioDevice\_SetMusicVolume\_System\_Single\_), 
[MixerAudioDevice.PauseMusic\(bool\)](MicroEngine.Audio.MixerAudioDevice.md\#MicroEngine\_Audio\_MixerAudioDevice\_PauseMusic\_System\_Boolean\_), 
[MixerAudioDevice.Update\(\)](MicroEngine.Audio.MixerAudioDevice.md\#MicroEngine\_Audio\_MixerAudioDevice\_Update), 
[MixerAudioDevice.Pump\(\)](MicroEngine.Audio.MixerAudioDevice.md\#MicroEngine\_Audio\_MixerAudioDevice\_Pump), 
[MixerAudioDevice.Dispose\(\)](MicroEngine.Audio.MixerAudioDevice.md\#MicroEngine\_Audio\_MixerAudioDevice\_Dispose), 
[MixerAudioDevice.Engine](MicroEngine.Audio.MixerAudioDevice.md\#MicroEngine\_Audio\_MixerAudioDevice\_Engine), 
[MixerAudioDevice.IsAvailable](MicroEngine.Audio.MixerAudioDevice.md\#MicroEngine\_Audio\_MixerAudioDevice\_IsAvailable), 
[MixerAudioDevice.MusicFormats](MicroEngine.Audio.MixerAudioDevice.md\#MicroEngine\_Audio\_MixerAudioDevice\_MusicFormats), 
[MixerAudioDevice.CurrentMusic](MicroEngine.Audio.MixerAudioDevice.md\#MicroEngine\_Audio\_MixerAudioDevice\_CurrentMusic), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring), 
[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode)

## Remarks

It is the mixer every backend plays through, with the same voice limit, priorities and stealing, panning, resampling,
looping, pause, music volume and decoding (WAV and OGG Vorbis music streams); only the device's own latency and
resampling to its hardware rate are left out. Sounds start at the first frame of the step that played them.

## Constructors

### <a id="MicroEngine_Audio_CaptureAudioDevice__ctor_System_Int32_System_Int32_"></a> CaptureAudioDevice\(int, int\)

Creates a capture device.

```csharp
public CaptureAudioDevice(int updateRate = 60, int sampleRate = 44100)
```

#### Parameters

`updateRate` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Updates per second of the game (<code>GameSettings.UpdateRate</code>); each [Update](MicroEngine.Audio.CaptureAudioDevice.md#MicroEngine_Audio_CaptureAudioDevice_Update) captures 1/<code class="paramref">updateRate</code> seconds.

`sampleRate` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Samples per second of the capture.

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

A rate is not positive.

## Properties

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
public override bool IsAvailable { get; }
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

### <a id="MicroEngine_Audio_CaptureAudioDevice_Play_MicroEngine_Audio_SoundHandle_System_Single_System_Single_System_Single_"></a> Play\(SoundHandle, float, float, float\)

Plays the sound and records it in [Played](MicroEngine.Audio.CaptureAudioDevice.md#MicroEngine_Audio_CaptureAudioDevice_Played).

```csharp
public override void Play(SoundHandle sound, float volume = 1, float pitch = 1, float pan = 0.5)
```

#### Parameters

`sound` [SoundHandle](MicroEngine.Audio.SoundHandle.md)

`volume` [float](https://learn.microsoft.com/dotnet/api/system.single)

`pitch` [float](https://learn.microsoft.com/dotnet/api/system.single)

`pan` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_CaptureAudioDevice_PlayVoice_MicroEngine_Audio_SoundHandle_MicroEngine_Audio_PlayParameters__"></a> PlayVoice\(SoundHandle, in PlayParameters\)

Plays the sound as a voice and records it in [Played](MicroEngine.Audio.CaptureAudioDevice.md#MicroEngine_Audio_CaptureAudioDevice_Played).

```csharp
public override VoiceHandle PlayVoice(SoundHandle sound, in PlayParameters parameters)
```

#### Parameters

`sound` [SoundHandle](MicroEngine.Audio.SoundHandle.md)

`parameters` [PlayParameters](MicroEngine.Audio.PlayParameters.md)

#### Returns

 [VoiceHandle](MicroEngine.Audio.VoiceHandle.md)

### <a id="MicroEngine_Audio_CaptureAudioDevice_Pump"></a> Pump\(\)

Decodes streamed music ahead; the capture itself advances only with [Update](MicroEngine.Audio.CaptureAudioDevice.md#MicroEngine_Audio_CaptureAudioDevice_Update).

```csharp
public override void Pump()
```

### <a id="MicroEngine_Audio_CaptureAudioDevice_Update"></a> Update\(\)

Captures the next game step (1/updateRate seconds) of sound.

```csharp
public override void Update()
```

</div>

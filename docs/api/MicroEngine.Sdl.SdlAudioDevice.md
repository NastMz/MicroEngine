# Class SdlAudioDevice

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Sdl](MicroEngine.Sdl.md)  
Assembly: MicroEngine.Sdl.dll  
Guide: [Backends and platforms](../manual/backends.md)  

SDL3 audio: the engine's mixer ([MixerAudioDevice](MicroEngine.Audio.MixerAudioDevice.md), the same voices, priorities and music on every
backend) playing through SDL's default playback device. A feeder thread keeps [DefaultLatencyMilliseconds](MicroEngine.Sdl.SdlAudioDevice.md#MicroEngine_Sdl_SdlAudioDevice_DefaultLatencyMilliseconds)
of audio queued in an SDL audio stream (48 kHz stereo float), so main-thread stalls and garbage collections shorter
than that are not heard. It works without a window, so a host can use it with another platform. In a browser SDL's
own audio is not used: the mixer plays through the page's AudioWorklet queue, filled once per frame
([BrowserAudioSink](MicroEngine.Sdl.md)).

```csharp
public sealed class SdlAudioDevice : MixerAudioDevice, IAudioDevice, IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[MixerAudioDevice](MicroEngine.Audio.MixerAudioDevice.md) ← 
[SdlAudioDevice](MicroEngine.Sdl.SdlAudioDevice.md)

#### Implements

[IAudioDevice](MicroEngine.Audio.IAudioDevice.md), 
[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

#### Inherited Members

[MixerAudioDevice.LoadSound\(string, AudioClip\)](MicroEngine.Audio.MixerAudioDevice.md#MicroEngine_Audio_MixerAudioDevice_LoadSound_System_String_MicroEngine_Audio_AudioClip_), 
[MixerAudioDevice.LoadMusic\(string, AudioClip, bool\)](MicroEngine.Audio.MixerAudioDevice.md#MicroEngine_Audio_MixerAudioDevice_LoadMusic_System_String_MicroEngine_Audio_AudioClip_System_Boolean_), 
[MixerAudioDevice.LoadMusicStream\(string, byte\[\], string, bool\)](MicroEngine.Audio.MixerAudioDevice.md#MicroEngine_Audio_MixerAudioDevice_LoadMusicStream_System_String_System_Byte___System_String_System_Boolean_), 
[MixerAudioDevice.Play\(SoundHandle, float, float, float\)](MicroEngine.Audio.MixerAudioDevice.md#MicroEngine_Audio_MixerAudioDevice_Play_MicroEngine_Audio_SoundHandle_System_Single_System_Single_System_Single_), 
[MixerAudioDevice.PlayMusic\(MusicHandle, float\)](MicroEngine.Audio.MixerAudioDevice.md#MicroEngine_Audio_MixerAudioDevice_PlayMusic_MicroEngine_Audio_MusicHandle_System_Single_), 
[MixerAudioDevice.StopMusic\(\)](MicroEngine.Audio.MixerAudioDevice.md#MicroEngine_Audio_MixerAudioDevice_StopMusic), 
[MixerAudioDevice.SetMusicVolume\(float\)](MicroEngine.Audio.MixerAudioDevice.md#MicroEngine_Audio_MixerAudioDevice_SetMusicVolume_System_Single_), 
[MixerAudioDevice.PauseMusic\(bool\)](MicroEngine.Audio.MixerAudioDevice.md#MicroEngine_Audio_MixerAudioDevice_PauseMusic_System_Boolean_), 
[MixerAudioDevice.Update\(\)](MicroEngine.Audio.MixerAudioDevice.md#MicroEngine_Audio_MixerAudioDevice_Update), 
[MixerAudioDevice.Pump\(\)](MicroEngine.Audio.MixerAudioDevice.md#MicroEngine_Audio_MixerAudioDevice_Pump), 
[MixerAudioDevice.Dispose\(\)](MicroEngine.Audio.MixerAudioDevice.md#MicroEngine_Audio_MixerAudioDevice_Dispose), 
[MixerAudioDevice.Engine](MicroEngine.Audio.MixerAudioDevice.md#MicroEngine_Audio_MixerAudioDevice_Engine), 
[MixerAudioDevice.IsAvailable](MicroEngine.Audio.MixerAudioDevice.md#MicroEngine_Audio_MixerAudioDevice_IsAvailable), 
[MixerAudioDevice.MusicFormats](MicroEngine.Audio.MixerAudioDevice.md#MicroEngine_Audio_MixerAudioDevice_MusicFormats), 
[MixerAudioDevice.CurrentMusic](MicroEngine.Audio.MixerAudioDevice.md#MicroEngine_Audio_MixerAudioDevice_CurrentMusic), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring), 
[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode)

## Fields

### <a id="MicroEngine_Sdl_SdlAudioDevice_DefaultLatencyMilliseconds"></a> DefaultLatencyMilliseconds

Audio kept queued by default, in milliseconds: longer than the longest collection measured on desktop.

```csharp
public const double DefaultLatencyMilliseconds = 60
```

#### Field Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

## Methods

### <a id="MicroEngine_Sdl_SdlAudioDevice_TryCreate"></a> TryCreate\(\)

Opens the default playback device, or returns a silent device (and says why) when there is none.

```csharp
public static IAudioDevice TryCreate()
```

#### Returns

 [IAudioDevice](MicroEngine.Audio.IAudioDevice.md)

### <a id="MicroEngine_Sdl_SdlAudioDevice_TryCreate_MicroEngine_Kernel_ILog_"></a> TryCreate\(ILog\)

Opens the default playback device, or returns a silent device and writes why to <code class="paramref">log</code> when there is none.

```csharp
public static IAudioDevice TryCreate(ILog log)
```

#### Parameters

`log` [ILog](MicroEngine.Kernel.ILog.md)

#### Returns

 [IAudioDevice](MicroEngine.Audio.IAudioDevice.md)

### <a id="MicroEngine_Sdl_SdlAudioDevice_TryCreate_System_String_"></a> TryCreate\(string\)

Opens the default playback device of SDL's audio driver <code class="paramref">driver</code>: "dummy" runs SDL's whole audio
path without any device or sound, for tests; "disk" writes the mixed output to a file (SDL's
<code>SDL_AUDIO_DISK_OUTPUT_FILE</code> hint). Returns a silent device (and says why) when the driver is unavailable
or SDL's audio was already started with another driver.

```csharp
public static IAudioDevice TryCreate(string driver)
```

#### Parameters

`driver` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [IAudioDevice](MicroEngine.Audio.IAudioDevice.md)

### <a id="MicroEngine_Sdl_SdlAudioDevice_TryCreate_System_String_System_Double_"></a> TryCreate\(string?, double\)

Opens the default playback device (of <code class="paramref">driver</code>, or SDL's choice when null) keeping
<code class="paramref">latencyMilliseconds</code> of audio queued: lower plays sounds sooner, higher survives longer pauses.

```csharp
public static IAudioDevice TryCreate(string? driver, double latencyMilliseconds)
```

#### Parameters

`driver` [string](https://learn.microsoft.com/dotnet/api/system.string)?

`latencyMilliseconds` [double](https://learn.microsoft.com/dotnet/api/system.double)

#### Returns

 [IAudioDevice](MicroEngine.Audio.IAudioDevice.md)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">latencyMilliseconds</code> is not between 5 and 1000.

</div>

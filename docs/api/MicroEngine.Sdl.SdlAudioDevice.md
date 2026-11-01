# Class SdlAudioDevice

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Sdl](MicroEngine.Sdl.md)  
Assembly: MicroEngine.Sdl.dll  
Guide: [Backends and platforms](../manual/backends.md)  

SDL3 audio. SDL mixes every audio stream bound to a device, so the device keeps a pool of streams (voices) for sound
effects, each converting its clip's rate and channels and applying pitch (frequency ratio) and volume (gain); pan is
applied when the clip is copied into its voice. Music is one stream topped up from [Update](MicroEngine.Sdl.SdlAudioDevice.md#MicroEngine_Sdl_SdlAudioDevice_Update), decoded from
PCM or OGG Vorbis (NVorbis) in managed code. It works without a window, so a host can use it with another platform.

```csharp
public sealed class SdlAudioDevice : IAudioDevice, IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SdlAudioDevice](MicroEngine.Sdl.SdlAudioDevice.md)

#### Implements

[IAudioDevice](https://github.com/NastMz/MicroEngine/blob/6aa7502bc7854ca56ec44cb5b9c7e43cd9d8563d/src/MicroEngine/Audio/Audio.cs), 
[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

## Properties

### <a id="MicroEngine_Sdl_SdlAudioDevice_CurrentMusic"></a> CurrentMusic

The music started last and not stopped, or null.

```csharp
public MusicHandle? CurrentMusic { get; }
```

#### Property Value

 [MusicHandle](https://github.com/NastMz/MicroEngine/blob/6aa7502bc7854ca56ec44cb5b9c7e43cd9d8563d/src/MicroEngine/Audio/Audio.cs)?

### <a id="MicroEngine_Sdl_SdlAudioDevice_IsAvailable"></a> IsAvailable

Always true: a device that could not open is replaced by a silent one in [TryCreate](MicroEngine.Sdl.SdlAudioDevice.md#MicroEngine_Sdl_SdlAudioDevice_TryCreate).

```csharp
public bool IsAvailable { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="MicroEngine_Sdl_SdlAudioDevice_Dispose"></a> Dispose\(\)

Stops playback and closes the audio device.

```csharp
public void Dispose()
```

### <a id="MicroEngine_Sdl_SdlAudioDevice_LoadMusic_System_String_MicroEngine_Audio_AudioClip_System_Boolean_"></a> LoadMusic\(string, AudioClip, bool\)

Uploads decoded music and returns its handle.

```csharp
public MusicHandle LoadMusic(string name, AudioClip clip, bool loop = true)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

Name reported by [Name](MicroEngine.Audio.MusicHandle.md#MicroEngine_Audio_MusicHandle_Name).

`clip` [AudioClip](https://github.com/NastMz/MicroEngine/blob/6aa7502bc7854ca56ec44cb5b9c7e43cd9d8563d/src/MicroEngine/Audio/Audio.cs)

The music samples.

`loop` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether the music restarts when it reaches its end.

#### Returns

 [MusicHandle](https://github.com/NastMz/MicroEngine/blob/6aa7502bc7854ca56ec44cb5b9c7e43cd9d8563d/src/MicroEngine/Audio/Audio.cs)

### <a id="MicroEngine_Sdl_SdlAudioDevice_LoadMusicStream_System_String_System_Byte___System_String_System_Boolean_"></a> LoadMusicStream\(string, byte\[\], string, bool\)

Loads compressed music for streaming. The SDL device decodes "wav" (PCM) and "ogg" (Vorbis).

```csharp
public MusicHandle LoadMusicStream(string name, byte[] encoded, string format, bool loop = true)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`encoded` [byte](https://learn.microsoft.com/dotnet/api/system.byte)\[\]

`format` [string](https://learn.microsoft.com/dotnet/api/system.string)

`loop` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Returns

 [MusicHandle](https://github.com/NastMz/MicroEngine/blob/6aa7502bc7854ca56ec44cb5b9c7e43cd9d8563d/src/MicroEngine/Audio/Audio.cs)

#### Exceptions

 [NotSupportedException](https://learn.microsoft.com/dotnet/api/system.notsupportedexception)

Any other format, such as mp3.

### <a id="MicroEngine_Sdl_SdlAudioDevice_LoadSound_System_String_MicroEngine_Audio_AudioClip_"></a> LoadSound\(string, AudioClip\)

Uploads a sound effect and returns its handle.

```csharp
public SoundHandle LoadSound(string name, AudioClip clip)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`clip` [AudioClip](https://github.com/NastMz/MicroEngine/blob/6aa7502bc7854ca56ec44cb5b9c7e43cd9d8563d/src/MicroEngine/Audio/Audio.cs)

#### Returns

 [SoundHandle](https://github.com/NastMz/MicroEngine/blob/6aa7502bc7854ca56ec44cb5b9c7e43cd9d8563d/src/MicroEngine/Audio/Audio.cs)

### <a id="MicroEngine_Sdl_SdlAudioDevice_PauseMusic_System_Boolean_"></a> PauseMusic\(bool\)

Pauses (true) or resumes (false) the current music.

```csharp
public void PauseMusic(bool paused)
```

#### Parameters

`paused` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Sdl_SdlAudioDevice_Play_MicroEngine_Audio_SoundHandle_System_Single_System_Single_System_Single_"></a> Play\(SoundHandle, float, float, float\)

Plays a sound effect once, mixed with any sounds already playing.

```csharp
public void Play(SoundHandle sound, float volume = 1, float pitch = 1, float pan = 0.5)
```

#### Parameters

`sound` [SoundHandle](https://github.com/NastMz/MicroEngine/blob/6aa7502bc7854ca56ec44cb5b9c7e43cd9d8563d/src/MicroEngine/Audio/Audio.cs)

A handle from [LoadSound](MicroEngine.Audio.IAudioDevice.md#MicroEngine_Audio_IAudioDevice_LoadSound_System_String_MicroEngine_Audio_AudioClip_).

`volume` [float](https://learn.microsoft.com/dotnet/api/system.single)

Volume, 0..1.

`pitch` [float](https://learn.microsoft.com/dotnet/api/system.single)

Playback speed factor; 1 is the original pitch.

`pan` [float](https://learn.microsoft.com/dotnet/api/system.single)

Stereo position, 0 (left) to 1 (right); 0.5 is centred.

### <a id="MicroEngine_Sdl_SdlAudioDevice_PlayMusic_MicroEngine_Audio_MusicHandle_System_Single_"></a> PlayMusic\(MusicHandle, float\)

Starts <code class="paramref">music</code> from the beginning, replacing the music that was playing.

```csharp
public void PlayMusic(MusicHandle music, float volume = 1)
```

#### Parameters

`music` [MusicHandle](https://github.com/NastMz/MicroEngine/blob/6aa7502bc7854ca56ec44cb5b9c7e43cd9d8563d/src/MicroEngine/Audio/Audio.cs)

`volume` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Sdl_SdlAudioDevice_SetMusicVolume_System_Single_"></a> SetMusicVolume\(float\)

Sets the volume of the current music, 0..1.

```csharp
public void SetMusicVolume(float volume)
```

#### Parameters

`volume` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Sdl_SdlAudioDevice_StopMusic"></a> StopMusic\(\)

Stops the music immediately.

```csharp
public void StopMusic()
```

### <a id="MicroEngine_Sdl_SdlAudioDevice_TryCreate"></a> TryCreate\(\)

Opens the default playback device, or returns a silent device (and says why) when there is none.

```csharp
public static IAudioDevice TryCreate()
```

#### Returns

 [IAudioDevice](https://github.com/NastMz/MicroEngine/blob/6aa7502bc7854ca56ec44cb5b9c7e43cd9d8563d/src/MicroEngine/Audio/Audio.cs)

### <a id="MicroEngine_Sdl_SdlAudioDevice_Update"></a> Update\(\)

Keeps about a quarter of a second of music queued.

```csharp
public void Update()
```

</div>

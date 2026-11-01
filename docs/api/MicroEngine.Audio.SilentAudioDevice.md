# Class SilentAudioDevice

Namespace: [MicroEngine.Audio](MicroEngine.Audio.md)  
Assembly: MicroEngine.dll  
Guide: [Audio](../manual/audio.md)  

Silent device used headlessly and as a fallback when no audio hardware is present.

```csharp
public sealed class SilentAudioDevice : IAudioDevice
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SilentAudioDevice](MicroEngine.Audio.SilentAudioDevice.md)

#### Implements

[IAudioDevice](MicroEngine.Audio.IAudioDevice.md)

## Constructors

### <a id="MicroEngine_Audio_SilentAudioDevice__ctor"></a> SilentAudioDevice\(\)

```csharp
public SilentAudioDevice()
```

## Properties

### <a id="MicroEngine_Audio_SilentAudioDevice_CurrentMusic"></a> CurrentMusic

The music started last and not stopped, or null.

```csharp
public MusicHandle? CurrentMusic { get; }
```

#### Property Value

 [MusicHandle](MicroEngine.Audio.MusicHandle.md)?

### <a id="MicroEngine_Audio_SilentAudioDevice_IsAvailable"></a> IsAvailable

Always false: nothing is heard.

```csharp
public bool IsAvailable { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_SilentAudioDevice_Played"></a> Played

Names of the sounds played, in order, for checks in tests.

```csharp
public List<string> Played { get; }
```

#### Property Value

 [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

## Methods

### <a id="MicroEngine_Audio_SilentAudioDevice_LoadMusic_System_String_MicroEngine_Audio_AudioClip_System_Boolean_"></a> LoadMusic\(string, AudioClip, bool\)

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

### <a id="MicroEngine_Audio_SilentAudioDevice_LoadMusicStream_System_String_System_Byte___System_String_System_Boolean_"></a> LoadMusicStream\(string, byte\[\], string, bool\)

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

### <a id="MicroEngine_Audio_SilentAudioDevice_LoadSound_System_String_MicroEngine_Audio_AudioClip_"></a> LoadSound\(string, AudioClip\)

Uploads a sound effect and returns its handle.

```csharp
public SoundHandle LoadSound(string name, AudioClip clip)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`clip` [AudioClip](MicroEngine.Audio.AudioClip.md)

#### Returns

 [SoundHandle](MicroEngine.Audio.SoundHandle.md)

### <a id="MicroEngine_Audio_SilentAudioDevice_PauseMusic_System_Boolean_"></a> PauseMusic\(bool\)

Pauses (true) or resumes (false) the current music.

```csharp
public void PauseMusic(bool paused)
```

#### Parameters

`paused` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_SilentAudioDevice_Play_MicroEngine_Audio_SoundHandle_System_Single_System_Single_System_Single_"></a> Play\(SoundHandle, float, float, float\)

Records the sound's name in [Played](MicroEngine.Audio.SilentAudioDevice.md#MicroEngine_Audio_SilentAudioDevice_Played).

```csharp
public void Play(SoundHandle sound, float volume = 1, float pitch = 1, float pan = 0.5)
```

#### Parameters

`sound` [SoundHandle](MicroEngine.Audio.SoundHandle.md)

`volume` [float](https://learn.microsoft.com/dotnet/api/system.single)

`pitch` [float](https://learn.microsoft.com/dotnet/api/system.single)

`pan` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_SilentAudioDevice_PlayMusic_MicroEngine_Audio_MusicHandle_System_Single_"></a> PlayMusic\(MusicHandle, float\)

Starts <code class="paramref">music</code> from the beginning, replacing the music that was playing.

```csharp
public void PlayMusic(MusicHandle music, float volume = 1)
```

#### Parameters

`music` [MusicHandle](MicroEngine.Audio.MusicHandle.md)

`volume` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_SilentAudioDevice_SetMusicVolume_System_Single_"></a> SetMusicVolume\(float\)

Sets the volume of the current music, 0..1.

```csharp
public void SetMusicVolume(float volume)
```

#### Parameters

`volume` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_SilentAudioDevice_StopMusic"></a> StopMusic\(\)

Stops the music immediately.

```csharp
public void StopMusic()
```

### <a id="MicroEngine_Audio_SilentAudioDevice_Update"></a> Update\(\)

Called once per frame so streaming music keeps playing.

```csharp
public void Update()
```

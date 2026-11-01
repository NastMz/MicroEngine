# Interface IAudioDevice

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Audio](MicroEngine.Audio.md)  
Assembly: MicroEngine.dll  
Guide: [Audio](../manual/audio.md)  

Backend audio device. Implementations must tolerate missing hardware by becoming silent.

```csharp
public interface IAudioDevice
```

## Properties

### <a id="MicroEngine_Audio_IAudioDevice_CurrentMusic"></a> CurrentMusic

The music started last and not stopped, or null.

```csharp
MusicHandle? CurrentMusic { get; }
```

#### Property Value

 [MusicHandle](MicroEngine.Audio.MusicHandle.md)?

### <a id="MicroEngine_Audio_IAudioDevice_IsAvailable"></a> IsAvailable

Whether sound actually reaches a speaker. False for a silent device (no hardware, headless, muted host).

```csharp
bool IsAvailable { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="MicroEngine_Audio_IAudioDevice_LoadMusic_System_String_MicroEngine_Audio_AudioClip_System_Boolean_"></a> LoadMusic\(string, AudioClip, bool\)

Uploads decoded music and returns its handle.

```csharp
MusicHandle LoadMusic(string name, AudioClip clip, bool loop = true)
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

### <a id="MicroEngine_Audio_IAudioDevice_LoadMusicStream_System_String_System_Byte___System_String_System_Boolean_"></a> LoadMusicStream\(string, byte\[\], string, bool\)

Streams compressed music (e.g. "ogg", "mp3", "wav") that the device decodes itself.

```csharp
MusicHandle LoadMusicStream(string name, byte[] encoded, string format, bool loop = true)
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

### <a id="MicroEngine_Audio_IAudioDevice_LoadSound_System_String_MicroEngine_Audio_AudioClip_"></a> LoadSound\(string, AudioClip\)

Uploads a sound effect and returns its handle.

```csharp
SoundHandle LoadSound(string name, AudioClip clip)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`clip` [AudioClip](MicroEngine.Audio.AudioClip.md)

#### Returns

 [SoundHandle](MicroEngine.Audio.SoundHandle.md)

### <a id="MicroEngine_Audio_IAudioDevice_PauseMusic_System_Boolean_"></a> PauseMusic\(bool\)

Pauses (true) or resumes (false) the current music.

```csharp
void PauseMusic(bool paused)
```

#### Parameters

`paused` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_IAudioDevice_Play_MicroEngine_Audio_SoundHandle_System_Single_System_Single_System_Single_"></a> Play\(SoundHandle, float, float, float\)

Plays a sound effect once, mixed with any sounds already playing.

```csharp
void Play(SoundHandle sound, float volume = 1, float pitch = 1, float pan = 0.5)
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

### <a id="MicroEngine_Audio_IAudioDevice_PlayMusic_MicroEngine_Audio_MusicHandle_System_Single_"></a> PlayMusic\(MusicHandle, float\)

Starts <code class="paramref">music</code> from the beginning, replacing the music that was playing.

```csharp
void PlayMusic(MusicHandle music, float volume = 1)
```

#### Parameters

`music` [MusicHandle](MicroEngine.Audio.MusicHandle.md)

`volume` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_IAudioDevice_SetMusicVolume_System_Single_"></a> SetMusicVolume\(float\)

Sets the volume of the current music, 0..1.

```csharp
void SetMusicVolume(float volume)
```

#### Parameters

`volume` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_IAudioDevice_StopMusic"></a> StopMusic\(\)

Stops the music immediately.

```csharp
void StopMusic()
```

### <a id="MicroEngine_Audio_IAudioDevice_Update"></a> Update\(\)

Called once per frame so streaming music keeps playing.

```csharp
void Update()
```

</div>

# Class MixerAudioDevice

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Audio](MicroEngine.Audio.md)  
Assembly: MicroEngine.Audio.dll  
Guide: [Audio](../manual/audio.md)  

An [IAudioDevice](MicroEngine.Audio.IAudioDevice.md) over the engine's one mixer ([AudioEngine](MicroEngine.Audio.AudioEngine.md)) and a PCM sink: every backend
device is one of these with its own sink, so voices, priorities, stealing, music and fades behave the same
everywhere. Music decodes in managed code: WAV, OGG Vorbis (streamed, decoded ahead outside the simulation step), and
formats a backend adds ([AddDecoder](MicroEngine.Audio.MixerAudioDevice.md#MicroEngine_Audio_MixerAudioDevice_AddDecoder_System_String_System_Func_System_Byte___MicroEngine_Audio_IAudioStream__)).

```csharp
public class MixerAudioDevice : IAudioDevice, IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[MixerAudioDevice](MicroEngine.Audio.MixerAudioDevice.md)

#### Derived

[CaptureAudioDevice](MicroEngine.Audio.CaptureAudioDevice.md)

#### Implements

[IAudioDevice](MicroEngine.Audio.IAudioDevice.md), 
[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

## Remarks

Sound effects play on the "sfx" bus with priority 0, music on the "music" bus with the highest priority, so the voice
limit ([MaxVoices](MicroEngine.Audio.AudioEngineSettings.md#MicroEngine_Audio_AudioEngineSettings_MaxVoices)) never steals the music. [Update](MicroEngine.Audio.MixerAudioDevice.md#MicroEngine_Audio_MixerAudioDevice_Update) ends a simulation
step; [Pump](MicroEngine.Audio.MixerAudioDevice.md#MicroEngine_Audio_MixerAudioDevice_Pump) is the host frame's audio work.

## Constructors

### <a id="MicroEngine_Audio_MixerAudioDevice__ctor_MicroEngine_Platform_IAudioSink_MicroEngine_Audio_AudioEngineSettings_"></a> MixerAudioDevice\(IAudioSink, AudioEngineSettings?\)

A device mixing into <code class="paramref">sink</code>, which starts rendering now.

```csharp
public MixerAudioDevice(IAudioSink sink, AudioEngineSettings? settings = null)
```

#### Parameters

`sink` [IAudioSink](MicroEngine.Platform.IAudioSink.md)

`settings` [AudioEngineSettings](MicroEngine.Audio.AudioEngineSettings.md)?

## Properties

### <a id="MicroEngine_Audio_MixerAudioDevice_CurrentMusic"></a> CurrentMusic

The music started last, until it is stopped or (when it does not loop) ends on the mixer's timeline.

```csharp
public MusicHandle? CurrentMusic { get; }
```

#### Property Value

 [MusicHandle](MicroEngine.Audio.MusicHandle.md)?

### <a id="MicroEngine_Audio_MixerAudioDevice_Engine"></a> Engine

The mixer, for what [IAudioDevice](MicroEngine.Audio.IAudioDevice.md) does not cover: buses, handles, priorities.

```csharp
public AudioEngine Engine { get; }
```

#### Property Value

 [AudioEngine](MicroEngine.Audio.AudioEngine.md)

### <a id="MicroEngine_Audio_MixerAudioDevice_IsAvailable"></a> IsAvailable

True: the sink plays. A backend whose device could not open uses a [SilentAudioDevice](MicroEngine.Audio.SilentAudioDevice.md) instead.

```csharp
public virtual bool IsAvailable { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_MixerAudioDevice_MusicFormats"></a> MusicFormats

The formats [LoadMusicStream](MicroEngine.Audio.MixerAudioDevice.md#MicroEngine_Audio_MixerAudioDevice_LoadMusicStream_System_String_System_Byte___System_String_System_Boolean_) decodes, lower case: "wav", "ogg" and any a backend added.

```csharp
public IReadOnlyList<string> MusicFormats { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

## Methods

### <a id="MicroEngine_Audio_MixerAudioDevice_AddDecoder_System_String_System_Func_System_Byte___MicroEngine_Audio_IAudioStream__"></a> AddDecoder\(string, Func<byte\[\], IAudioStream\>\)

Adds a music format the device decodes (a backend's native decoder, for example).

```csharp
protected void AddDecoder(string format, Func<byte[], IAudioStream> open)
```

#### Parameters

`format` [string](https://learn.microsoft.com/dotnet/api/system.string)

The file extension without the dot.

`open` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[byte](https://learn.microsoft.com/dotnet/api/system.byte)\[\], [IAudioStream](MicroEngine.Audio.IAudioStream.md)\>

Opens a stream over the file's bytes; throws `InvalidDataException` for bytes it cannot decode.

### <a id="MicroEngine_Audio_MixerAudioDevice_Dispose"></a> Dispose\(\)

Stops the sink and releases the streams.

```csharp
public void Dispose()
```

### <a id="MicroEngine_Audio_MixerAudioDevice_Dispose_System_Boolean_"></a> Dispose\(bool\)

Stops the mixer; a backend also closes its device here.

```csharp
protected virtual void Dispose(bool disposing)
```

#### Parameters

`disposing` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_MixerAudioDevice_LoadMusic_System_String_MicroEngine_Audio_AudioClip_System_Boolean_"></a> LoadMusic\(string, AudioClip, bool\)

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

### <a id="MicroEngine_Audio_MixerAudioDevice_LoadMusicStream_System_String_System_Byte___System_String_System_Boolean_"></a> LoadMusicStream\(string, byte\[\], string, bool\)

Loads music to stream: "wav" is decoded now; "ogg" and the formats a backend added are decoded while they play.

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

#### Exceptions

 [NotSupportedException](https://learn.microsoft.com/dotnet/api/system.notsupportedexception)

The format is not one of [MusicFormats](MicroEngine.Audio.MixerAudioDevice.md#MicroEngine_Audio_MixerAudioDevice_MusicFormats).

 [InvalidDataException](https://learn.microsoft.com/dotnet/api/system.io.invaliddataexception)

The bytes cannot be decoded as that format.

### <a id="MicroEngine_Audio_MixerAudioDevice_LoadSound_System_String_MicroEngine_Audio_AudioClip_"></a> LoadSound\(string, AudioClip\)

Uploads a sound effect and returns its handle.

```csharp
public SoundHandle LoadSound(string name, AudioClip clip)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`clip` [AudioClip](MicroEngine.Audio.AudioClip.md)

#### Returns

 [SoundHandle](MicroEngine.Audio.SoundHandle.md)

### <a id="MicroEngine_Audio_MixerAudioDevice_PauseMusic_System_Boolean_"></a> PauseMusic\(bool\)

Pauses (true) or resumes (false) the current music.

```csharp
public void PauseMusic(bool paused)
```

#### Parameters

`paused` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_MixerAudioDevice_Play_MicroEngine_Audio_SoundHandle_System_Single_System_Single_System_Single_"></a> Play\(SoundHandle, float, float, float\)

Plays a sound effect once, mixed with any sounds already playing.

```csharp
public virtual void Play(SoundHandle sound, float volume = 1, float pitch = 1, float pan = 0.5)
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

### <a id="MicroEngine_Audio_MixerAudioDevice_PlayMusic_MicroEngine_Audio_MusicHandle_System_Single_"></a> PlayMusic\(MusicHandle, float\)

Starts <code class="paramref">music</code> from the beginning, replacing the music that was playing.

```csharp
public void PlayMusic(MusicHandle music, float volume = 1)
```

#### Parameters

`music` [MusicHandle](MicroEngine.Audio.MusicHandle.md)

`volume` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_MixerAudioDevice_Pump"></a> Pump\(\)

The host frame's audio work: decodes streamed music ahead and pumps the sink.

```csharp
public virtual void Pump()
```

### <a id="MicroEngine_Audio_MixerAudioDevice_SetMusicVolume_System_Single_"></a> SetMusicVolume\(float\)

Sets the volume of the current music, 0..1.

```csharp
public void SetMusicVolume(float volume)
```

#### Parameters

`volume` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_MixerAudioDevice_StopMusic"></a> StopMusic\(\)

Stops the music immediately.

```csharp
public void StopMusic()
```

### <a id="MicroEngine_Audio_MixerAudioDevice_Update"></a> Update\(\)

Ends a simulation step (the game calls it once per update through [Update](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_Update_System_Single_)).

```csharp
public virtual void Update()
```

</div>

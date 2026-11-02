# Class AudioMixer

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Audio](MicroEngine.Audio.md)  
Assembly: MicroEngine.dll  
Guide: [Audio](../manual/audio.md)  

Game-facing audio: a sound bank keyed by name plus music with fades.
Games register synthesized clips once and then call [Play](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_Play_System_String_System_Single_System_Single_System_Single_System_Single_) by name.

```csharp
public sealed class AudioMixer
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[AudioMixer](MicroEngine.Audio.AudioMixer.md)

## Examples

```csharp
// In Game.Load:
Audio.AddSound("jump", Synth.Render(new SfxParams { Wave = Wave.Square, Frequency = 520, EndFrequency = 880 }));
Audio.AddMusicFile("theme", "Content/theme.ogg");
Audio.PlayMusic("theme");

// In a scene's Update:
if (Input.Pressed("jump")) Audio.Play("jump", pitchJitter: 0.05f);
```

## Remarks

Every game has one in [Audio](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Audio). Sounds and music are registered under names that are compared
ordinally (case-sensitive); registering a name again replaces it. On a device without hardware every call still works
and nothing is heard.

## Constructors

### <a id="MicroEngine_Audio_AudioMixer__ctor_MicroEngine_Audio_IAudioDevice_"></a> AudioMixer\(IAudioDevice\)

Creates a mixer over <code class="paramref">device</code>. Games use the one in [Audio](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Audio).

```csharp
public AudioMixer(IAudioDevice device)
```

#### Parameters

`device` [IAudioDevice](MicroEngine.Audio.IAudioDevice.md)

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

<code class="paramref">device</code> is null.

## Properties

### <a id="MicroEngine_Audio_AudioMixer_CurrentMusic"></a> CurrentMusic

Name of the music the device is playing (also while it fades out), or null.

```csharp
public string? CurrentMusic { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Audio_AudioMixer_Device"></a> Device

The audio device the mixer plays through.

```csharp
public IAudioDevice Device { get; }
```

#### Property Value

 [IAudioDevice](MicroEngine.Audio.IAudioDevice.md)

### <a id="MicroEngine_Audio_AudioMixer_MusicPaused"></a> MusicPaused

Whether music was paused with [PauseMusic](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_PauseMusic_System_Boolean_).

```csharp
public bool MusicPaused { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_AudioMixer_MusicVolume"></a> MusicVolume

Music volume, clamped to 0..1 (default 1). Applies immediately to the playing music, combined with any fade.

```csharp
public float MusicVolume { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_AudioMixer_SfxVolume"></a> SfxVolume

Volume multiplier for sound effects, 0..1 (default 0.8). Each played volume is multiplied by it and clamped to 0..1.

```csharp
public float SfxVolume { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_Audio_AudioMixer_AddMusic_System_String_MicroEngine_Audio_AudioClip_System_Boolean_"></a> AddMusic\(string, AudioClip, bool\)

Registers decoded music, such as a rendered [Song](MicroEngine.Audio.Song.md), under <code class="paramref">name</code>.

```csharp
public void AddMusic(string name, AudioClip clip, bool loop = true)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

Name used by [PlayMusic](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_PlayMusic_System_String_System_Single_).

`clip` [AudioClip](MicroEngine.Audio.AudioClip.md)

The music samples.

`loop` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether the music restarts when it reaches its end.

### <a id="MicroEngine_Audio_AudioMixer_AddMusicFile_System_String_System_String_System_Boolean_"></a> AddMusicFile\(string, string, bool\)

Registers a music file. WAV is decoded here; other formats are streamed by the device.

```csharp
public void AddMusicFile(string name, string path, bool loop = true)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

`loop` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Remarks

Formats other than wav depend on the audio device: the SDL device streams ogg; the raylib device also decodes mp3.

#### Exceptions

 [FileNotFoundException](https://learn.microsoft.com/dotnet/api/system.io.filenotfoundexception)

The file does not exist.

### <a id="MicroEngine_Audio_AudioMixer_AddSound_System_String_MicroEngine_Audio_AudioClip_"></a> AddSound\(string, AudioClip\)

Registers <code class="paramref">clip</code> as the sound effect <code class="paramref">name</code>, replacing any sound with that name.

```csharp
public void AddSound(string name, AudioClip clip)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`clip` [AudioClip](MicroEngine.Audio.AudioClip.md)

### <a id="MicroEngine_Audio_AudioMixer_AddSoundFile_System_String_System_String_"></a> AddSoundFile\(string, string\)

Registers a WAV file as a sound effect.

```csharp
public void AddSoundFile(string name, string path)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Exceptions

 [FileNotFoundException](https://learn.microsoft.com/dotnet/api/system.io.filenotfoundexception)

The file does not exist.

 [NotSupportedException](https://learn.microsoft.com/dotnet/api/system.notsupportedexception)

The WAV is not 8, 16 or 24-bit PCM, mono or stereo.

### <a id="MicroEngine_Audio_AudioMixer_HasMusic_System_String_"></a> HasMusic\(string\)

Whether music is registered under <code class="paramref">name</code>.

```csharp
public bool HasMusic(string name)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_AudioMixer_HasSound_System_String_"></a> HasSound\(string\)

Whether a sound effect is registered under <code class="paramref">name</code>.

```csharp
public bool HasSound(string name)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_AudioMixer_PauseMusic_System_Boolean_"></a> PauseMusic\(bool\)

Pauses (true) or resumes (false) the music and records the state in [MusicPaused](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_MusicPaused).

```csharp
public void PauseMusic(bool paused)
```

#### Parameters

`paused` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_AudioMixer_Play_System_String_System_Single_System_Single_System_Single_System_Single_"></a> Play\(string, float, float, float, float\)

Plays a sound. <code class="paramref">pitchJitter</code> randomizes pitch (+/-) to avoid repetition fatigue.
            The same sound is not retriggered within one frame, preventing volume spikes.

```csharp
public void Play(string name, float volume = 1, float pitch = 1, float pitchJitter = 0, float pan = 0.5)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`volume` [float](https://learn.microsoft.com/dotnet/api/system.single)

`pitch` [float](https://learn.microsoft.com/dotnet/api/system.single)

`pitchJitter` [float](https://learn.microsoft.com/dotnet/api/system.single)

`pan` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [KeyNotFoundException](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception)

No sound is registered under <code class="paramref">name</code>; use [TryPlay](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_TryPlay_System_String_System_Single_System_Single_System_Single_System_Single_) for optional sounds.

### <a id="MicroEngine_Audio_AudioMixer_PlayMusic_System_String_System_Single_"></a> PlayMusic\(string, float\)

Switches music. With a fade, the current track fades out before the new one starts.

```csharp
public void PlayMusic(string name, float fadeSeconds = 0.4)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`fadeSeconds` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Remarks

With a fade, music starting from silence rises over about a quarter of a second; with <code class="paramref">fadeSeconds</code> 0 or less it starts at full volume at once. Asking for the music that is already playing does nothing; asking for it while it fades out (towards another track or silence) cancels the fade and brings it back, without restarting it.

#### Exceptions

 [KeyNotFoundException](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception)

No music is registered under <code class="paramref">name</code>.

### <a id="MicroEngine_Audio_AudioMixer_StopMusic_System_Single_"></a> StopMusic\(float\)

Stops the music, fading it out over <code class="paramref">fadeSeconds</code>; 0 or less stops it at once.

```csharp
public void StopMusic(float fadeSeconds = 0.4)
```

#### Parameters

`fadeSeconds` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_AudioMixer_TryPlay_System_String_System_Single_System_Single_System_Single_System_Single_"></a> TryPlay\(string, float, float, float, float\)

Plays a sound if it is registered; returns false otherwise (useful for optional cues).

```csharp
public bool TryPlay(string name, float volume = 1, float pitch = 1, float pitchJitter = 0, float pan = 0.5)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`volume` [float](https://learn.microsoft.com/dotnet/api/system.single)

`pitch` [float](https://learn.microsoft.com/dotnet/api/system.single)

`pitchJitter` [float](https://learn.microsoft.com/dotnet/api/system.single)

`pan` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_AudioMixer_Update_System_Single_"></a> Update\(float\)

Advances music fades and lets the device stream. The game calls it once per update; call it yourself only for a mixer you created.

```csharp
public void Update(float dt)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

Elapsed time in seconds.

</div>

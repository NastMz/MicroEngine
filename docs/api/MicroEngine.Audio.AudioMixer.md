# Class AudioMixer

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Audio](MicroEngine.Audio.md)  
Assembly: MicroEngine.Audio.dll  
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
// In Game.Load (Synth, SfxParams and Wave are in MicroEngine.Audio.Retro):
Audio.AddSound("jump", Synth.Render(new SfxParams { Wave = Wave.Square, Frequency = 520, EndFrequency = 880 }));
Audio.AddMusicFile("theme", "Content/theme.ogg");
Audio.PlayMusic("theme");

// In a scene's Update:
if (Input.Pressed("jump")) Audio.Play("jump", pitchJitter: 0.05f);
```

## Remarks

Every game has one in <code>Game.Audio</code>. Sounds and music are registered under names that are compared
ordinally (case-sensitive); registering a name again replaces it. On a device without hardware every call still works
and nothing is heard.

## Constructors

### <a id="MicroEngine_Audio_AudioMixer__ctor_MicroEngine_Audio_IAudioDevice_"></a> AudioMixer\(IAudioDevice\)

Creates a mixer over <code class="paramref">device</code>. Games use the one in <code>Game.Audio</code>.

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

Name of the music playing (also while it fades out, and while a song asked for still renders in slices), or null. It follows the game's requests and the music's end on the simulation's timeline, never rendering speed.

```csharp
public string? CurrentMusic { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Audio_AudioMixer_Deterministic"></a> Deterministic

Whether the run is deterministic (scripted runs, tests): the device's timeline then follows the simulation only,
and songs that render in slices render only when the game waits for one, finished in the next [Pump](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_Pump)
whatever [SliceBudget](MicroEngine.Audio.AudioMixer.md), so the
same steps give the same sound. The game sets it from its time mode.

```csharp
public bool Deterministic { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

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

### <a id="MicroEngine_Audio_AudioMixer_Statistics"></a> Statistics

The voices of the device's mixer: how many sounds play now, how many can play at once and how many the voice
policy refused so far. Zeros for a device without the engine's mixer ([SilentAudioDevice](MicroEngine.Audio.SilentAudioDevice.md)).

```csharp
public AudioStatistics Statistics { get; }
```

#### Property Value

 [AudioStatistics](MicroEngine.Audio.AudioStatistics.md)

## Methods

### <a id="MicroEngine_Audio_AudioMixer_AddBus_System_String_System_String_"></a> AddBus\(string, string\)

Adds a bus playing into <code class="paramref">parent</code>: a group of sounds with one volume ("ambience", "voices",
"ui"), named by [Bus](MicroEngine.Audio.PlayParameters.md#MicroEngine_Audio_PlayParameters_Bus). At most 32 buses exist.

```csharp
public void AddBus(string name, string parent = "master")
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`parent` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

A bus already has that name, or the name is empty.

 [KeyNotFoundException](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception)

<code class="paramref">parent</code> does not exist.

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

There are 32 buses already.

### <a id="MicroEngine_Audio_AudioMixer_AddMusic_System_String_MicroEngine_Audio_AudioClip_System_Boolean_"></a> AddMusic\(string, AudioClip, bool\)

Registers decoded music, such as a rendered [Song](MicroEngine.Audio.Retro.Song.md), under <code class="paramref">name</code>.

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

### <a id="MicroEngine_Audio_AudioMixer_AddMusic_System_String_MicroEngine_Audio_Retro_Song_System_Boolean_"></a> AddMusic\(string, Song, bool\)

Registers a [Song](MicroEngine.Audio.Retro.Song.md) under <code class="paramref">name</code> without waiting for it to be rendered: songs render
in the background, side by side, and each is handed to the device the first time it plays (waiting then only for
that song if it is not ready yet). Registering every song of a game this way keeps their synthesis out of
<code>Load</code>. Where the runtime has no background threads (browsers), songs render a few milliseconds per
[Pump](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_Pump) (once per frame, outside the simulation step) instead, one after another in the order they were
registered; a song asked to play before
it is ready goes first and starts once rendered ([CurrentMusic](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_CurrentMusic) already names it).

```csharp
public void AddMusic(string name, Song song, bool loop = true)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

Name used by [PlayMusic](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_PlayMusic_System_String_System_Single_).

`song` [Song](MicroEngine.Audio.Retro.Song.md)

The song; do not change it after registering it.

`loop` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether the music restarts when it reaches its end.

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The song has no steps.

 [FormatException](https://learn.microsoft.com/dotnet/api/system.formatexception)

A track has an invalid note or a drum track an unknown drum.

### <a id="MicroEngine_Audio_AudioMixer_AddMusic_System_String_MicroEngine_Audio_EncodedAudio_System_Boolean_"></a> AddMusic\(string, EncodedAudio, bool\)

Registers a music file loaded from content (<code>Assets.Music(path)</code>) under <code class="paramref">name</code>: WAV is
decoded here, other formats are decoded by the device while they play.

```csharp
public void AddMusic(string name, EncodedAudio music, bool loop = true)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`music` [EncodedAudio](MicroEngine.Audio.EncodedAudio.md)

`loop` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Exceptions

 [NotSupportedException](https://learn.microsoft.com/dotnet/api/system.notsupportedexception)

The device does not decode that format (MP3 outside raylib, for example).

 [InvalidDataException](https://learn.microsoft.com/dotnet/api/system.io.invaliddataexception)

The bytes cannot be decoded as their format.

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

Every device decodes OGG Vorbis; the raylib device also decodes mp3.

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

### <a id="MicroEngine_Audio_AudioMixer_BusVolume_System_String_"></a> BusVolume\(string\)

The volume of bus <code class="paramref">name</code>.

```csharp
public float BusVolume(string name)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [KeyNotFoundException](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception)

No such bus.

### <a id="MicroEngine_Audio_AudioMixer_HasBus_System_String_"></a> HasBus\(string\)

Whether a bus is named <code class="paramref">name</code>. "master", "sfx" (sounds, by default) and "music" always exist.

```csharp
public bool HasBus(string name)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

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

### <a id="MicroEngine_Audio_AudioMixer_IsPlaying_MicroEngine_Audio_VoiceHandle_"></a> IsPlaying\(VoiceHandle\)

Whether <code class="paramref">voice</code> is still playing (or paused, or fading out).

```csharp
public bool IsPlaying(VoiceHandle voice)
```

#### Parameters

`voice` [VoiceHandle](MicroEngine.Audio.VoiceHandle.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_AudioMixer_Pause_MicroEngine_Audio_VoiceHandle_System_Boolean_"></a> Pause\(VoiceHandle, bool\)

Pauses (true) or resumes (false) <code class="paramref">voice</code>; a paused sound keeps its voice.

```csharp
public bool Pause(VoiceHandle voice, bool paused)
```

#### Parameters

`voice` [VoiceHandle](MicroEngine.Audio.VoiceHandle.md)

`paused` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether it was playing.

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

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

A song registered with [AddMusic](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_AddMusic_System_String_MicroEngine_Audio_AudioClip_System_Boolean_)
            failed while rendering (its content is checked when registered, so only the machine can fail it, for example out
            of memory); its name is then unregistered. With a fade, this surfaces from [Update](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_Update_System_Single_) when the song is
            due to start.

### <a id="MicroEngine_Audio_AudioMixer_PlayMusicVoice_System_String_MicroEngine_Audio_PlayParameters__"></a> PlayMusicVoice\(string, in PlayParameters\)

Plays registered music as one more voice, beside the track [PlayMusic](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_PlayMusic_System_String_System_Single_) plays and without replacing
it: ambience under the music, layers of one piece faded in and out. It plays exactly as
<code class="paramref">parameters</code> say (name the "music" bus to keep it with the music; it loops when they say so,
whatever the music was registered with); [MusicVolume](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_MusicVolume) and music fades do not apply. Streamed music
(OGG) keeps its own pitch.

```csharp
public VoiceHandle PlayMusicVoice(string name, in PlayParameters parameters)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`parameters` [PlayParameters](MicroEngine.Audio.PlayParameters.md)

#### Returns

 [VoiceHandle](MicroEngine.Audio.VoiceHandle.md)

The voice, or [None](MicroEngine.Audio.VoiceHandle.md#MicroEngine_Audio_VoiceHandle_None) as [PlayVoice](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_PlayVoice_System_String_MicroEngine_Audio_PlayParameters__) returns it.

#### Exceptions

 [KeyNotFoundException](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception)

No music is registered under <code class="paramref">name</code>, or the bus does not exist.

### <a id="MicroEngine_Audio_AudioMixer_PlayVoice_System_String_MicroEngine_Audio_PlayParameters__"></a> PlayVoice\(string, in PlayParameters\)

Plays a registered sound as a voice and returns its handle, to change or stop it while it plays: a loop that
follows something (an engine, a siren), a sound to cut short, a sound on a bus of its own or with a priority.
It plays exactly as <code class="paramref">parameters</code> say: [SfxVolume](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_SfxVolume) and the once-per-frame rule of
[Play](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_Play_System_String_System_Single_System_Single_System_Single_System_Single_) do not apply (scale voices with their bus, [SetBusVolume](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_SetBusVolume_System_String_System_Single_)).

```csharp
public VoiceHandle PlayVoice(string name, in PlayParameters parameters)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`parameters` [PlayParameters](MicroEngine.Audio.PlayParameters.md)

#### Returns

 [VoiceHandle](MicroEngine.Audio.VoiceHandle.md)

The voice, or [None](MicroEngine.Audio.VoiceHandle.md#MicroEngine_Audio_VoiceHandle_None) when every voice is busy with sounds of higher priority or the device
has no mixer (headless, muted, no hardware); calls with that handle do nothing.

#### Exceptions

 [KeyNotFoundException](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception)

No sound is registered under <code class="paramref">name</code>, or the bus does not exist.

### <a id="MicroEngine_Audio_AudioMixer_Pump"></a> Pump\(\)

The host frame's audio work, outside the simulation step: renders songs that render in slices (browsers), then
lets the device decode streamed music ahead and pump its sink. The game calls it once per frame; call it yourself
only for a mixer you created.

```csharp
public void Pump()
```

### <a id="MicroEngine_Audio_AudioMixer_Set_MicroEngine_Audio_VoiceHandle_System_Nullable_System_Single__System_Nullable_System_Single__System_Nullable_System_Single__"></a> Set\(VoiceHandle, float?, float?, float?\)

Changes a playing voice from this update on; null keeps a value. Called every update it follows something that
moves: the pan and volume of a sound source crossing the screen, the pitch of an engine.

```csharp
public bool Set(VoiceHandle voice, float? volume = null, float? pan = null, float? pitch = null)
```

#### Parameters

`voice` [VoiceHandle](MicroEngine.Audio.VoiceHandle.md)

A handle from [PlayVoice](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_PlayVoice_System_String_MicroEngine_Audio_PlayParameters__) or [PlayMusicVoice](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_PlayMusicVoice_System_String_MicroEngine_Audio_PlayParameters__).

`volume` [float](https://learn.microsoft.com/dotnet/api/system.single)?

Volume, 0..1.

`pan` [float](https://learn.microsoft.com/dotnet/api/system.single)?

Stereo position, 0 (left) to 1 (right).

`pitch` [float](https://learn.microsoft.com/dotnet/api/system.single)?

Playback speed factor, 1 for the original pitch (0.01..100); ignored by streamed music.

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether it was playing.

### <a id="MicroEngine_Audio_AudioMixer_SetBusVolume_System_String_System_Single_"></a> SetBusVolume\(string, float\)

Sets the volume of everything playing through bus <code class="paramref">name</code> and the buses under it, clamped to
0..4, from this update on: "master" for the whole game, "sfx" for every sound (those of [Play](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_Play_System_String_System_Single_System_Single_System_Single_System_Single_) too,
on top of [SfxVolume](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_SfxVolume)), "music" for the music track and the voices played on it.

```csharp
public void SetBusVolume(string name, float volume)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`volume` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [KeyNotFoundException](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception)

No such bus.

### <a id="MicroEngine_Audio_AudioMixer_Stop_MicroEngine_Audio_VoiceHandle_System_Single_"></a> Stop\(VoiceHandle, float\)

Stops <code class="paramref">voice</code>, fading it out over <code class="paramref">fadeSeconds</code> (0: at once).

```csharp
public bool Stop(VoiceHandle voice, float fadeSeconds = 0)
```

#### Parameters

`voice` [VoiceHandle](MicroEngine.Audio.VoiceHandle.md)

`fadeSeconds` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether it was playing; false for a handle whose sound already ended, was stopped or lost its voice.

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

Advances music fades and ends the device's simulation step. The game calls it once per update; call it yourself only for a mixer you created.

```csharp
public void Update(float dt)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

Elapsed time in seconds.

</div>

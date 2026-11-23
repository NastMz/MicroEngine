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

### <a id="MicroEngine_Audio_AudioMixer_FailedStreams"></a> FailedStreams

Music and streamed voices that ended because their file could not be decoded any further (see
[FailedStreams](MicroEngine.Audio.AudioEngine.md#MicroEngine_Audio_AudioEngine_FailedStreams)); each is also written once to [Log](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_Log).

```csharp
public int FailedStreams { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Audio_AudioMixer_Limiter"></a> Limiter

The limiter after "master": on a device that plays, a mix louder than full scale is turned down instead of
clipped. [Bypass](MicroEngine.Audio.AudioEffect.md#MicroEngine_Audio_AudioEffect_Bypass) switches it off.

```csharp
public Limiter Limiter { get; }
```

#### Property Value

 [Limiter](MicroEngine.Audio.Effects.Limiter.md)

### <a id="MicroEngine_Audio_AudioMixer_Log"></a> Log

Where the mixer reports what it could not play (a music file that turns out damaged while it plays): the
engine's log when the mixer is a game's, standard error otherwise.

```csharp
public ILog? Log { get; set; }
```

#### Property Value

 [ILog](MicroEngine.Kernel.ILog.md)?

### <a id="MicroEngine_Audio_AudioMixer_MusicDuration"></a> MusicDuration

The length of the music playing, in seconds; 0 with no music, on a device without voices and for a format whose decoder does not know it.

```csharp
public double MusicDuration { get; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Audio_AudioMixer_MusicHeardPosition"></a> MusicHeardPosition

The position of the music the player hears at this update (see [HeardPosition](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_HeardPosition_MicroEngine_Audio_VoiceHandle_)): what a rhythm game
judges input against and a cutscene cues its pictures from.

```csharp
public double MusicHeardPosition { get; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Audio_AudioMixer_MusicPaused"></a> MusicPaused

Whether music was paused with [PauseMusic](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_PauseMusic_System_Boolean_).

```csharp
public bool MusicPaused { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_AudioMixer_MusicPosition"></a> MusicPosition

Where the music [PlayMusic](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_PlayMusic_System_String_System_Single_System_Single_) plays is, in seconds from its beginning (see [Position](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_Position_MicroEngine_Audio_VoiceHandle_));
0 with no music. Save it to bring the music back to the same place with [SeekMusic](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_SeekMusic_System_Double_). On a device
without voices ([SupportsVoices](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_SupportsVoices)) it is the time the music has played, which does not come back
when the music loops.

```csharp
public double MusicPosition { get; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Audio_AudioMixer_MusicSeeking"></a> MusicSeeking

Whether the music is still on its way to the place [SeekMusic](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_SeekMusic_System_Double_) named (see [IsSeeking](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_IsSeeking_MicroEngine_Audio_VoiceHandle_)).

```csharp
public bool MusicSeeking { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_AudioMixer_MusicVolume"></a> MusicVolume

Music volume, clamped to 0..1 (default 1). Applies immediately to the playing music, combined with any fade. A
value that is not a number is ignored.

```csharp
public float MusicVolume { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_AudioMixer_OutputLatency"></a> OutputLatency

Seconds between the mixer writing a frame and the player hearing it, as the device reports it (0 on a silent
device and when rendering offline). [HeardPosition](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_HeardPosition_MicroEngine_Audio_VoiceHandle_) and [MusicHeardPosition](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_MusicHeardPosition) already
take it into account.

```csharp
public double OutputLatency { get; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Audio_AudioMixer_SfxVolume"></a> SfxVolume

Volume multiplier for sound effects, 0..1 (default 0.8). Each played volume is multiplied by it and clamped to
0..1. A value that is not a number is ignored.

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

### <a id="MicroEngine_Audio_AudioMixer_SupportsVoices"></a> SupportsVoices

Whether sounds play as voices on this device ([PlayVoice](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_PlayVoice_MicroEngine_Audio_AudioClip_MicroEngine_Audio_PlayParameters__), [PlayMusicVoice](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_PlayMusicVoice_System_String_MicroEngine_Audio_PlayParameters__)): true for
every device that plays through the engine's mixer, false for a silent one (headless, muted, no hardware), where
those calls return [None](MicroEngine.Audio.VoiceHandle.md#MicroEngine_Audio_VoiceHandle_None). Buses and their effects can be set up either way.

```csharp
public bool SupportsVoices { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="MicroEngine_Audio_AudioMixer_AddBus_System_String_System_String_"></a> AddBus\(string, string\)

Adds a bus playing into <code class="paramref">parent</code> and returns it: a group of sounds with one volume and one
chain of effects ("ambience", "voices", "ui"), named by [Bus](MicroEngine.Audio.PlayParameters.md#MicroEngine_Audio_PlayParameters_Bus). At most 32 buses exist.

```csharp
public AudioBus AddBus(string name, string parent = "master")
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`parent` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [AudioBus](MicroEngine.Audio.AudioBus.md)

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

Name used by [PlayMusic](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_PlayMusic_System_String_System_Single_System_Single_).

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

Name used by [PlayMusic](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_PlayMusic_System_String_System_Single_System_Single_).

`song` [Song](MicroEngine.Audio.Retro.Song.md)

The song; do not change it after registering it.

`loop` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether the music restarts when it reaches its end.

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The song has no steps.

 [FormatException](https://learn.microsoft.com/dotnet/api/system.formatexception)

A track has an invalid note or a drum track an unknown drum.

### <a id="MicroEngine_Audio_AudioMixer_AddMusic_System_String_MicroEngine_Audio_EncodedAudio_System_Boolean_MicroEngine_Audio_AudioLoadMode_"></a> AddMusic\(string, EncodedAudio, bool, AudioLoadMode\)

Registers a music file loaded from content (<code>Assets.Music(path)</code>) under <code class="paramref">name</code>.

```csharp
public void AddMusic(string name, EncodedAudio music, bool loop = true, AudioLoadMode mode = AudioLoadMode.Streamed)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

Name used by [PlayMusic](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_PlayMusic_System_String_System_Single_System_Single_).

`music` [EncodedAudio](MicroEngine.Audio.EncodedAudio.md)

The file's bytes and format.

`loop` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether the music restarts when it reaches its end (over its loop region, if the file names one).

`mode` [AudioLoadMode](MicroEngine.Audio.AudioLoadMode.md)

[Streamed](MicroEngine.Audio.AudioLoadMode.md) (the default) keeps the bytes and decodes them while the music plays;
[Decoded](MicroEngine.Audio.AudioLoadMode.md) decodes the whole file now. WAV is always decoded whole; a format only the
device decodes (not in [AudioDecoders](MicroEngine.Audio.AudioDecoders.md)) is always streamed.

#### Exceptions

 [NotSupportedException](https://learn.microsoft.com/dotnet/api/system.notsupportedexception)

No decoder is registered for the format.

 [InvalidDataException](https://learn.microsoft.com/dotnet/api/system.io.invaliddataexception)

The bytes cannot be decoded as their format.

### <a id="MicroEngine_Audio_AudioMixer_AddMusicFile_System_String_System_String_System_Boolean_MicroEngine_Audio_AudioLoadMode_"></a> AddMusicFile\(string, string, bool, AudioLoadMode\)

Registers a music file: WAV, OGG Vorbis, MP3 and any format registered in [AudioDecoders](MicroEngine.Audio.AudioDecoders.md), the same
on every platform, chosen by the file's extension (WAV without one).

```csharp
public void AddMusicFile(string name, string path, bool loop = true, AudioLoadMode mode = AudioLoadMode.Streamed)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

Name used by [PlayMusic](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_PlayMusic_System_String_System_Single_System_Single_).

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

The file.

`loop` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether the music restarts when it reaches its end (over its loop region, if the file names one).

`mode` [AudioLoadMode](MicroEngine.Audio.AudioLoadMode.md)

Decoded while it plays (the default) or whole, now. WAV is always decoded whole.

#### Exceptions

 [FileNotFoundException](https://learn.microsoft.com/dotnet/api/system.io.filenotfoundexception)

The file does not exist.

 [NotSupportedException](https://learn.microsoft.com/dotnet/api/system.notsupportedexception)

No decoder is registered for the extension.

 [InvalidDataException](https://learn.microsoft.com/dotnet/api/system.io.invaliddataexception)

The file cannot be decoded as its format.

### <a id="MicroEngine_Audio_AudioMixer_AddSound_System_String_MicroEngine_Audio_AudioClip_"></a> AddSound\(string, AudioClip\)

Registers <code class="paramref">clip</code> as the sound effect <code class="paramref">name</code>, replacing any sound with that
name (which is released as [RemoveSound](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_RemoveSound_System_String_) releases it).

```csharp
public void AddSound(string name, AudioClip clip)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`clip` [AudioClip](MicroEngine.Audio.AudioClip.md)

### <a id="MicroEngine_Audio_AudioMixer_AddSound_System_String_MicroEngine_Audio_EncodedAudio_"></a> AddSound\(string, EncodedAudio\)

Registers a sound file of any format in [AudioDecoders](MicroEngine.Audio.AudioDecoders.md) ("wav", "ogg", "mp3" and what the game
registered) as the sound effect <code class="paramref">name</code>, decoded whole now.

```csharp
public void AddSound(string name, EncodedAudio sound)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`sound` [EncodedAudio](MicroEngine.Audio.EncodedAudio.md)

#### Exceptions

 [NotSupportedException](https://learn.microsoft.com/dotnet/api/system.notsupportedexception)

No decoder is registered for the format.

 [InvalidDataException](https://learn.microsoft.com/dotnet/api/system.io.invaliddataexception)

The bytes cannot be decoded as their format.

### <a id="MicroEngine_Audio_AudioMixer_AddSoundFile_System_String_System_String_"></a> AddSoundFile\(string, string\)

Registers a sound file as a sound effect, decoded whole now: WAV (8, 16 or 24-bit PCM), OGG Vorbis, MP3 and any
format registered in [AudioDecoders](MicroEngine.Audio.AudioDecoders.md), chosen by the file's extension (WAV without one).

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

No decoder is registered for the extension, or the WAV is not 8, 16 or 24-bit PCM, mono or stereo.

 [InvalidDataException](https://learn.microsoft.com/dotnet/api/system.io.invaliddataexception)

The file cannot be decoded as its format.

### <a id="MicroEngine_Audio_AudioMixer_Bus_System_String_"></a> Bus\(string\)

The bus named <code class="paramref">name</code>: its [Volume](MicroEngine.Audio.AudioBus.md#MicroEngine_Audio_AudioBus_Volume) and [FadeTo](MicroEngine.Audio.AudioBus.md#MicroEngine_Audio_AudioBus_FadeTo_System_Single_System_Single_), and
its effects ([Add](MicroEngine.Audio.AudioBus.md#MicroEngine_Audio_AudioBus_Add_MicroEngine_Audio_AudioEffect_)): a filter, a reverb, a compressor that ducks it under another bus.

```csharp
public AudioBus Bus(string name)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [AudioBus](MicroEngine.Audio.AudioBus.md)

#### Exceptions

 [KeyNotFoundException](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception)

No such bus.

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

### <a id="MicroEngine_Audio_AudioMixer_Duration_MicroEngine_Audio_VoiceHandle_"></a> Duration\(VoiceHandle\)

The length in seconds of the sound <code class="paramref">voice</code> plays; 0 for a voice that is not playing or a stream that does not know its length.

```csharp
public double Duration(VoiceHandle voice)
```

#### Parameters

`voice` [VoiceHandle](MicroEngine.Audio.VoiceHandle.md)

#### Returns

 [double](https://learn.microsoft.com/dotnet/api/system.double)

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

### <a id="MicroEngine_Audio_AudioMixer_HeardPosition_MicroEngine_Audio_VoiceHandle_"></a> HeardPosition\(VoiceHandle\)

The position of <code class="paramref">voice</code> the player hears at this update: [Position](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_Position_MicroEngine_Audio_VoiceHandle_) less
[OutputLatency](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_OutputLatency). Read it for anything shown or judged against the sound (a note reaching a line, a
cutscene's cue). Negative while the sound's first frame is still on its way to the speaker.

```csharp
public double HeardPosition(VoiceHandle voice)
```

#### Parameters

`voice` [VoiceHandle](MicroEngine.Audio.VoiceHandle.md)

#### Returns

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Audio_AudioMixer_IsPlaying_MicroEngine_Audio_VoiceHandle_"></a> IsPlaying\(VoiceHandle\)

Whether <code class="paramref">voice</code> is still playing (or paused, or fading out).

```csharp
public bool IsPlaying(VoiceHandle voice)
```

#### Parameters

`voice` [VoiceHandle](MicroEngine.Audio.VoiceHandle.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_AudioMixer_IsSeeking_MicroEngine_Audio_VoiceHandle_"></a> IsSeeking\(VoiceHandle\)

Whether <code class="paramref">voice</code> is still on its way to a place [Seek](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_Seek_MicroEngine_Audio_VoiceHandle_System_Double_) or
[StartAt](MicroEngine.Audio.PlayParameters.md#MicroEngine_Audio_PlayParameters_StartAt) named. Only streamed OGG and MP3 take time to get somewhere far: a stream
sought while it plays goes on where it was until then (and [Position](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_Position_MicroEngine_Audio_VoiceHandle_) with it), and one started in
its middle is silent until then. Sounds and decoded music are there at once.

```csharp
public bool IsSeeking(VoiceHandle voice)
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

### <a id="MicroEngine_Audio_AudioMixer_Play_MicroEngine_Audio_AudioClip_System_Single_System_Single_System_Single_System_Single_"></a> Play\(AudioClip, float, float, float, float\)

Plays a clip that is not registered under a name: one a game holds itself, or one from <code>Assets.Sound(path)</code>
held by an asset scope. Nothing keeps the clip after it has played, so releasing the scope frees it. Volume,
pitch, jitter and pan are those of [Play](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_Play_System_String_System_Single_System_Single_System_Single_System_Single_), and the same clip
is not retriggered within one update.

```csharp
public void Play(AudioClip clip, float volume = 1, float pitch = 1, float pitchJitter = 0, float pan = 0.5)
```

#### Parameters

`clip` [AudioClip](MicroEngine.Audio.AudioClip.md)

`volume` [float](https://learn.microsoft.com/dotnet/api/system.single)

`pitch` [float](https://learn.microsoft.com/dotnet/api/system.single)

`pitchJitter` [float](https://learn.microsoft.com/dotnet/api/system.single)

`pan` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_AudioMixer_PlayMusic_System_String_System_Single_System_Single_"></a> PlayMusic\(string, float, float\)

Switches music. With a fade, the current track fades out before the new one starts; with
<code class="paramref">crossfade</code>, the new track starts now and rises while the current one falls, both over that
many seconds.

```csharp
public void PlayMusic(string name, float fadeSeconds = 0.4, float crossfade = 0)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

The music, as registered.

`fadeSeconds` [float](https://learn.microsoft.com/dotnet/api/system.single)

Seconds the current track takes to fade out before the new one starts (0.4 by default; 0 or less switches at once). Not used with a crossfade.

`crossfade` [float](https://learn.microsoft.com/dotnet/api/system.single)

Seconds over which the two tracks overlap, the new one rising in a straight line as the old one falls (0, the
default: no overlap). On a device without voices ([SupportsVoices](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_SupportsVoices)), and for a song that is still
being rendered, the music switches as without it.

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

Plays registered music as one more voice, beside the track [PlayMusic](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_PlayMusic_System_String_System_Single_System_Single_) plays and without replacing
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

The voice, or [None](MicroEngine.Audio.VoiceHandle.md#MicroEngine_Audio_VoiceHandle_None) as [PlayVoice](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_PlayVoice_MicroEngine_Audio_AudioClip_MicroEngine_Audio_PlayParameters__) returns it.

#### Exceptions

 [KeyNotFoundException](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception)

No music is registered under <code class="paramref">name</code>, or the bus does not exist.

### <a id="MicroEngine_Audio_AudioMixer_PlayVoice_MicroEngine_Audio_AudioClip_MicroEngine_Audio_PlayParameters__"></a> PlayVoice\(AudioClip, in PlayParameters\)

Plays a clip that is not registered under a name as a voice, exactly as <code class="paramref">parameters</code> say (see
[PlayVoice](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_PlayVoice_MicroEngine_Audio_AudioClip_MicroEngine_Audio_PlayParameters__)). Nothing keeps the clip once the voice ends.

```csharp
public VoiceHandle PlayVoice(AudioClip clip, in PlayParameters parameters)
```

#### Parameters

`clip` [AudioClip](MicroEngine.Audio.AudioClip.md)

`parameters` [PlayParameters](MicroEngine.Audio.PlayParameters.md)

#### Returns

 [VoiceHandle](MicroEngine.Audio.VoiceHandle.md)

The voice, or [None](MicroEngine.Audio.VoiceHandle.md#MicroEngine_Audio_VoiceHandle_None) as [PlayVoice](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_PlayVoice_MicroEngine_Audio_AudioClip_MicroEngine_Audio_PlayParameters__) returns it.

#### Exceptions

 [KeyNotFoundException](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception)

The bus does not exist.

### <a id="MicroEngine_Audio_AudioMixer_PlayVoice_System_String_MicroEngine_Audio_PlayParameters__"></a> PlayVoice\(string, in PlayParameters\)

Plays a registered sound as a voice and returns its handle, to change or stop it while it plays: a loop that
follows something (an engine, a siren), a sound to cut short, a sound on a bus of its own or with a priority.
It plays exactly as <code class="paramref">parameters</code> say: [SfxVolume](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_SfxVolume) and the once-per-frame rule of
[Play](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_Play_System_String_System_Single_System_Single_System_Single_System_Single_) do not apply (scale voices with their bus, [SetBusVolume](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_SetBusVolume_System_String_System_Single_System_Single_)).

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

### <a id="MicroEngine_Audio_AudioMixer_Position_MicroEngine_Audio_VoiceHandle_"></a> Position\(VoiceHandle\)

Where <code class="paramref">voice</code> is in its sound, in seconds from the sound's beginning: 0 as it starts, what
[Seek](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_Seek_MicroEngine_Audio_VoiceHandle_System_Double_) was given after a seek, back at the start of its loop region each time it loops. It follows
the game's steps, so a scripted run reads the same values every time. 0 for a voice that is not playing.

```csharp
public double Position(VoiceHandle voice)
```

#### Parameters

`voice` [VoiceHandle](MicroEngine.Audio.VoiceHandle.md)

#### Returns

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Audio_AudioMixer_Pump"></a> Pump\(\)

The host frame's audio work, outside the simulation step: renders songs that render in slices (browsers), then
lets the device decode streamed music ahead and pump its sink. The game calls it once per frame; call it yourself
only for a mixer you created.

```csharp
public void Pump()
```

### <a id="MicroEngine_Audio_AudioMixer_RemoveMusic_System_String_"></a> RemoveMusic\(string\)

Unregisters the music <code class="paramref">name</code>: the mixer and the device no longer hold its samples or its
file's bytes. If it is the music playing (or about to play) it stops at once; a voice playing it beside the
music plays on to its end.

```csharp
public bool RemoveMusic(string name)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether music had that name.

### <a id="MicroEngine_Audio_AudioMixer_RemoveSound_System_String_"></a> RemoveSound\(string\)

Unregisters the sound effect <code class="paramref">name</code>: the mixer and the device no longer hold its samples, so
they are freed once nothing else holds them (an asset scope, the game's own field). A voice playing it plays on
to its end.

```csharp
public bool RemoveSound(string name)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether a sound had that name.

### <a id="MicroEngine_Audio_AudioMixer_Seek_MicroEngine_Audio_VoiceHandle_System_Double_"></a> Seek\(VoiceHandle, double\)

Moves <code class="paramref">voice</code> to <code class="paramref">seconds</code> from its sound's beginning, from this update on
(clamped to the sound's length; a time that is not a number changes nothing).

```csharp
public bool Seek(VoiceHandle voice, double seconds)
```

#### Parameters

`voice` [VoiceHandle](MicroEngine.Audio.VoiceHandle.md)

`seconds` [double](https://learn.microsoft.com/dotnet/api/system.double)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether it moved: false for a voice that is not playing and for a stream whose decoder cannot seek.

### <a id="MicroEngine_Audio_AudioMixer_SeekMusic_System_Double_"></a> SeekMusic\(double\)

Moves the music playing to <code class="paramref">seconds</code> from its beginning, from this update on (clamped to its
length; a time that is not a number changes nothing).

```csharp
public bool SeekMusic(double seconds)
```

#### Parameters

`seconds` [double](https://learn.microsoft.com/dotnet/api/system.double)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether it moved: false with no music and for a format whose decoder cannot seek.

### <a id="MicroEngine_Audio_AudioMixer_Set_MicroEngine_Audio_VoiceHandle_System_Nullable_System_Single__System_Nullable_System_Single__System_Nullable_System_Single__System_Single_"></a> Set\(VoiceHandle, float?, float?, float?, float\)

Changes a playing voice from this update on; null, or a value that is not a number, keeps a value. Called every
update it follows something that moves: the pan and volume of a sound source crossing the screen, the pitch of
an engine.

```csharp
public bool Set(VoiceHandle voice, float? volume = null, float? pan = null, float? pitch = null, float seconds = 0)
```

#### Parameters

`voice` [VoiceHandle](MicroEngine.Audio.VoiceHandle.md)

A handle from [PlayVoice](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_PlayVoice_MicroEngine_Audio_AudioClip_MicroEngine_Audio_PlayParameters__) or [PlayMusicVoice](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_PlayMusicVoice_System_String_MicroEngine_Audio_PlayParameters__).

`volume` [float](https://learn.microsoft.com/dotnet/api/system.single)?

Volume, 0..1.

`pan` [float](https://learn.microsoft.com/dotnet/api/system.single)?

Stereo position, 0 (left) to 1 (right).

`pitch` [float](https://learn.microsoft.com/dotnet/api/system.single)?

Playback speed factor, 1 for the original pitch (0.01..100); ignored by streamed music.

`seconds` [float](https://learn.microsoft.com/dotnet/api/system.single)

The time the values take to get there, frame by frame: a fade, a sweep across the screen, a glide. 0 (the
default) changes them at once, which is what a value set again every update needs.

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether it was playing.

### <a id="MicroEngine_Audio_AudioMixer_SetBusVolume_System_String_System_Single_System_Single_"></a> SetBusVolume\(string, float, float\)

Sets the volume of everything playing through bus <code class="paramref">name</code> and the buses under it, clamped to
0..4, from this update on: "master" for the whole game, "sfx" for every sound (those of [Play](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_Play_System_String_System_Single_System_Single_System_Single_System_Single_) too,
on top of [SfxVolume](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_SfxVolume)), "music" for the music track and the voices played on it. A value that is
not a number is ignored. With <code class="paramref">fadeSeconds</code> the volume moves there over that time instead of
at once.

```csharp
public void SetBusVolume(string name, float volume, float fadeSeconds = 0)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`volume` [float](https://learn.microsoft.com/dotnet/api/system.single)

`fadeSeconds` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [KeyNotFoundException](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception)

No such bus.

### <a id="MicroEngine_Audio_AudioMixer_SoundDuration_System_String_"></a> SoundDuration\(string\)

The length in seconds of the registered sound <code class="paramref">name</code>, at its own pitch; the same on every device.

```csharp
public double SoundDuration(string name)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [double](https://learn.microsoft.com/dotnet/api/system.double)

#### Exceptions

 [KeyNotFoundException](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception)

No sound is registered under <code class="paramref">name</code>.

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

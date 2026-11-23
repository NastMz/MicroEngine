# Class AudioEngine

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Audio](MicroEngine.Audio.md)  
Assembly: MicroEngine.Audio.dll  
Guide: [Audio](../manual/audio.md)  

The engine's one mixer: game code plays sounds here on the main thread, a sink renders them. Every backend plays
through it, so voices, priorities, stealing, buses and fades behave the same everywhere, and an offline sink renders
exactly what a device would play.

```csharp
public sealed class AudioEngine : IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[AudioEngine](MicroEngine.Audio.AudioEngine.md)

#### Implements

[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

## Remarks

<p>
Time is counted in simulation steps ([Advance](MicroEngine.Audio.AudioEngine.md#MicroEngine_Audio_AudioEngine_Advance), once per fixed step). A sound starts at an exact output
frame: its step's frame plus [DelayFrames](MicroEngine.Audio.AudioEngine.md#MicroEngine_Audio_AudioEngine_DelayFrames) (at least the sink's latency), so the output does not depend
on the block sizes the sink asks for. The voice policy runs on the main thread, against that timeline, never against
what the sink has rendered: which sound plays is the same on every backend and in every run.
</p>
<p>
In real time (not [Deterministic](MicroEngine.Audio.AudioEngine.md#MicroEngine_Audio_AudioEngine_Deterministic)) the steps' frames follow what the device is playing: what the sink
has rendered, less its latency. A step's sounds therefore start in the frames the sink renders next, one step
after the previous step's, and are heard one sink latency after their update. A step that runs late moves the
timeline forward to what can still be rendered; after a slow frame, whose catch-up steps leave it ahead, it comes
back by a sixteenth of a step per host frame until it is within one step again. Nothing moves it back: sounds of
consecutive steps keep their order. A sink that renders nothing for a quarter of a second of steps, over several
host frames, has stopped: the timeline goes on without it and the frames it did not play are skipped
([SkippedFrames](MicroEngine.Audio.AudioEngine.md#MicroEngine_Audio_AudioEngine_SkippedFrames)), so when it renders again it plays what the game is at, not what it missed.
</p>
<p>
Commands reach the render function through a single-producer, single-consumer queue; rendering never allocates,
locks or reads files. Music streams are decoded ahead in [Pump](MicroEngine.Audio.AudioEngine.md#MicroEngine_Audio_AudioEngine_Pump), outside the simulation step.
</p>

## Constructors

### <a id="MicroEngine_Audio_AudioEngine__ctor_MicroEngine_Platform_IAudioSink_MicroEngine_Audio_AudioEngineSettings_"></a> AudioEngine\(IAudioSink, AudioEngineSettings?\)

Creates the mixer and starts <code class="paramref">sink</code> rendering it.

```csharp
public AudioEngine(IAudioSink sink, AudioEngineSettings? settings = null)
```

#### Parameters

`sink` [IAudioSink](MicroEngine.Platform.IAudioSink.md)

`settings` [AudioEngineSettings](MicroEngine.Audio.AudioEngineSettings.md)?

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The sink's format is not 1 or 2 channels at a positive rate, or a setting is out of range.

## Fields

### <a id="MicroEngine_Audio_AudioEngine_MasterBus"></a> MasterBus

The bus every other bus plays into.

```csharp
public const string MasterBus = "master"
```

#### Field Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Audio_AudioEngine_MusicBus"></a> MusicBus

The bus music plays through.

```csharp
public const string MusicBus = "music"
```

#### Field Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Audio_AudioEngine_SfxBus"></a> SfxBus

The default bus of sounds.

```csharp
public const string SfxBus = "sfx"
```

#### Field Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Properties

### <a id="MicroEngine_Audio_AudioEngine_ActiveVoices"></a> ActiveVoices

Sounds playing now on the mixer's timeline.

```csharp
public int ActiveVoices { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Audio_AudioEngine_Buses"></a> Buses

The buses, "master", "sfx" and "music" first.

```csharp
public IReadOnlyList<AudioBus> Buses { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[AudioBus](MicroEngine.Audio.AudioBus.md)\>

### <a id="MicroEngine_Audio_AudioEngine_DelayFrames"></a> DelayFrames

Frames between a step and the sounds it starts.

```csharp
public int DelayFrames { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Audio_AudioEngine_Deterministic"></a> Deterministic

Whether the timeline follows the simulation only (true: the same sounds in every run, as deterministic runs need)
or also catches up with the sink after the game fell behind it (false, the default for real-time play).

```csharp
public bool Deterministic { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_AudioEngine_DroppedCommands"></a> DroppedCommands

Calls that could not be queued for the render function because the sink had stopped reading (the queue was
full). No change is lost: the latest volume, pan and pitch of a sound, its pause and its stop, and bus volumes
are sent when the sink reads again. A sound that could not be queued was refused
([None](MicroEngine.Audio.VoiceHandle.md#MicroEngine_Audio_VoiceHandle_None)).

```csharp
public int DroppedCommands { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Audio_AudioEngine_FailedStreams"></a> FailedStreams

Streams that ended because their decoder threw while it was read or sought (a damaged file): each plays what it
had decoded, then ends like a sound that is over. Counted once per stream.

```csharp
public int FailedStreams { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Audio_AudioEngine_FaultedEffects"></a> FaultedEffects

Effects that threw while rendering and were taken out of the mix ([Faulted](MicroEngine.Audio.AudioEffect.md#MicroEngine_Audio_AudioEffect_Faulted)).

```csharp
public int FaultedEffects { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Audio_AudioEngine_Format"></a> Format

The sink's format, which the mixer renders.

```csharp
public AudioFormat Format { get; }
```

#### Property Value

 [AudioFormat](MicroEngine.Platform.AudioFormat.md)

### <a id="MicroEngine_Audio_AudioEngine_Limiter"></a> Limiter

The limiter after "master": it keeps the mix within full scale instead of letting the device clip it, and leaves
every sample as it is while the mix stays within its threshold. [Limiter](MicroEngine.Audio.AudioEngineSettings.md#MicroEngine_Audio_AudioEngineSettings_Limiter) says
whether it starts on (a game's device has it on); [Bypass](MicroEngine.Audio.AudioEffect.md#MicroEngine_Audio_AudioEffect_Bypass) switches it.

```csharp
public Limiter Limiter { get; }
```

#### Property Value

 [Limiter](MicroEngine.Audio.Effects.Limiter.md)

### <a id="MicroEngine_Audio_AudioEngine_MaxVoices"></a> MaxVoices

Sounds that can play at once.

```csharp
public int MaxVoices { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Audio_AudioEngine_Now"></a> Now

The output frame the current step's sounds start at (the step's frame plus [DelayFrames](MicroEngine.Audio.AudioEngine.md#MicroEngine_Audio_AudioEngine_DelayFrames)). In real
time, a sound or change moves it forward when the sink has already rendered past it.

```csharp
public long Now { get; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Audio_AudioEngine_RefusedSounds"></a> RefusedSounds

Sounds the voice policy refused because every voice played something of higher priority.

```csharp
public int RefusedSounds { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Audio_AudioEngine_RenderedFrames"></a> RenderedFrames

Frames rendered so far (read from any thread).

```csharp
public long RenderedFrames { get; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Audio_AudioEngine_SkippedFrames"></a> SkippedFrames

Frames the output did not play and the mixer skipped. In real time, a sink that renders nothing during a quarter
of a second of steps (or twice the time of its largest block, if that is longer), over several host frames, has
stopped (a browser holding its audio suspended, a device
that went away): from then on the sounds go on with the game, unheard, and when the sink renders again it joins
them where they are. A sound that ended meanwhile is never heard, one still within its length goes on from where
the game is, loops stay in step; streams, which are not decoded while nobody hears them, go on from where they
stopped. Always 0 in [Deterministic](MicroEngine.Audio.AudioEngine.md#MicroEngine_Audio_AudioEngine_Deterministic) runs.

```csharp
public long SkippedFrames { get; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Audio_AudioEngine_Steps"></a> Steps

Simulation steps counted by [Advance](MicroEngine.Audio.AudioEngine.md#MicroEngine_Audio_AudioEngine_Advance).

```csharp
public long Steps { get; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Audio_AudioEngine_StreamAheadFrames"></a> StreamAheadFrames

Output frames of each music stream decoded ahead.

```csharp
public int StreamAheadFrames { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Audio_AudioEngine_AddBus_System_String_System_String_"></a> AddBus\(string, string\)

Creates a bus playing into <code class="paramref">parent</code>.

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

A bus already has that name.

 [KeyNotFoundException](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception)

<code class="paramref">parent</code> does not exist.

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The engine has 32 buses already.

### <a id="MicroEngine_Audio_AudioEngine_Advance"></a> Advance\(\)

Ends a simulation step: sounds played from now on start one step later. The game calls it once per fixed step.

```csharp
public void Advance()
```

### <a id="MicroEngine_Audio_AudioEngine_Bus_System_String_"></a> Bus\(string\)

The bus named <code class="paramref">name</code>.

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

### <a id="MicroEngine_Audio_AudioEngine_Dispose"></a> Dispose\(\)

Stops the sink (it no longer renders after this returns) and the streams.

```csharp
public void Dispose()
```

### <a id="MicroEngine_Audio_AudioEngine_Duration_MicroEngine_Audio_VoiceHandle_"></a> Duration\(VoiceHandle\)

The length of the sound <code class="paramref">voice</code> plays, in seconds; 0 for a handle that is not playing and for a stream that does not know its length.

```csharp
public double Duration(VoiceHandle voice)
```

#### Parameters

`voice` [VoiceHandle](MicroEngine.Audio.VoiceHandle.md)

#### Returns

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Audio_AudioEngine_HeardPosition_MicroEngine_Audio_VoiceHandle_"></a> HeardPosition\(VoiceHandle\)

The position of <code class="paramref">voice</code> the player hears now: [Position](MicroEngine.Audio.AudioEngine.md#MicroEngine_Audio_AudioEngine_Position_MicroEngine_Audio_VoiceHandle_) less the output's delay
([DelayFrames](MicroEngine.Audio.AudioEngine.md#MicroEngine_Audio_AudioEngine_DelayFrames), at least the sink's latency), which is how long a frame takes from the mixer to the
speaker. What a game shows in step with the sound reads this: a note crossing a line, a cutscene's cue. It is
negative while the sound's first frame is still on its way, by the time left until it is heard.

```csharp
public double HeardPosition(VoiceHandle voice)
```

#### Parameters

`voice` [VoiceHandle](MicroEngine.Audio.VoiceHandle.md)

#### Returns

 [double](https://learn.microsoft.com/dotnet/api/system.double)

#### Remarks

It is where the sound was one delay ago, exactly: it stands still that long after a pause and moves on that
long after a resume, a pitch change reaches it that much later, and it never goes back while the sound plays
on. After [Seek](MicroEngine.Audio.AudioEngine.md#MicroEngine_Audio_AudioEngine_Seek_MicroEngine_Audio_VoiceHandle_System_Double_) it goes on along the old place until the seek is heard, then jumps to the new one
(and after a loop's seam, back to the region's start). The timeline keeps the last eight changes of a sound's
motion: with more than that within one delay, the oldest are forgotten and the heard position is approximate
until the delay has passed.

### <a id="MicroEngine_Audio_AudioEngine_IsPlaying_MicroEngine_Audio_VoiceHandle_"></a> IsPlaying\(VoiceHandle\)

Whether <code class="paramref">voice</code> is still playing (or paused, or fading out) on the mixer's timeline.

```csharp
public bool IsPlaying(VoiceHandle voice)
```

#### Parameters

`voice` [VoiceHandle](MicroEngine.Audio.VoiceHandle.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_AudioEngine_IsSeeking_MicroEngine_Audio_VoiceHandle_"></a> IsSeeking\(VoiceHandle\)

Whether <code class="paramref">voice</code> is on its way to a place that [Seek](MicroEngine.Audio.AudioEngine.md#MicroEngine_Audio_AudioEngine_Seek_MicroEngine_Audio_VoiceHandle_System_Double_) or
[StartAt](MicroEngine.Audio.PlayParameters.md#MicroEngine_Audio_PlayParameters_StartAt) named and has not reached it yet. Only compressed streams take time to get
there (their decoders reach a place by decoding up to it, a little at each [Pump](MicroEngine.Audio.AudioEngine.md#MicroEngine_Audio_AudioEngine_Pump)): a stream sought
while it plays goes on playing where it was until then, and one started in its middle is silent until then.

```csharp
public bool IsSeeking(VoiceHandle voice)
```

#### Parameters

`voice` [VoiceHandle](MicroEngine.Audio.VoiceHandle.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_AudioEngine_Pause_MicroEngine_Audio_VoiceHandle_System_Boolean_"></a> Pause\(VoiceHandle, bool\)

Pauses (true) or resumes (false) <code class="paramref">voice</code> from the current step's frame; a paused sound keeps its voice.

```csharp
public bool Pause(VoiceHandle voice, bool paused)
```

#### Parameters

`voice` [VoiceHandle](MicroEngine.Audio.VoiceHandle.md)

`paused` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether it was playing.

### <a id="MicroEngine_Audio_AudioEngine_Play_MicroEngine_Audio_AudioClip_MicroEngine_Audio_PlayParameters__"></a> Play\(AudioClip, in PlayParameters\)

Plays <code class="paramref">clip</code> from the current step's frame; returns its handle, or [None](MicroEngine.Audio.VoiceHandle.md#MicroEngine_Audio_VoiceHandle_None)
when the voice policy refused it or the sink stopped reading ([DroppedCommands](MicroEngine.Audio.AudioEngine.md#MicroEngine_Audio_AudioEngine_DroppedCommands)).

```csharp
public VoiceHandle Play(AudioClip clip, in PlayParameters parameters)
```

#### Parameters

`clip` [AudioClip](MicroEngine.Audio.AudioClip.md)

`parameters` [PlayParameters](MicroEngine.Audio.PlayParameters.md)

#### Returns

 [VoiceHandle](MicroEngine.Audio.VoiceHandle.md)

#### Exceptions

 [KeyNotFoundException](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception)

The bus does not exist.

### <a id="MicroEngine_Audio_AudioEngine_Play_MicroEngine_Audio_IAudioStream_MicroEngine_Audio_PlayParameters__"></a> Play\(IAudioStream, in PlayParameters\)

Plays <code class="paramref">stream</code> (music decoded while it plays); the engine disposes it when the sound ends or is stopped.

```csharp
public VoiceHandle Play(IAudioStream stream, in PlayParameters parameters)
```

#### Parameters

`stream` [IAudioStream](MicroEngine.Audio.IAudioStream.md)

`parameters` [PlayParameters](MicroEngine.Audio.PlayParameters.md)

#### Returns

 [VoiceHandle](MicroEngine.Audio.VoiceHandle.md)

#### Exceptions

 [KeyNotFoundException](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception)

The bus does not exist.

### <a id="MicroEngine_Audio_AudioEngine_Position_MicroEngine_Audio_VoiceHandle_"></a> Position\(VoiceHandle\)

Where <code class="paramref">voice</code> is in its sound, in seconds from the sound's beginning, at the current step's
frame on the mixer's timeline: 0 right after it starts, what [Seek](MicroEngine.Audio.AudioEngine.md#MicroEngine_Audio_AudioEngine_Seek_MicroEngine_Audio_VoiceHandle_System_Double_) was given right after a seek.
A loop comes back to the start of its region. It follows the simulation's steps, so a deterministic run reads
the same positions every time. 0 for a handle that is not playing. A stream that does not know its length
(one that is not an [ISeekableAudioStream](MicroEngine.Audio.ISeekableAudioStream.md)) counts on through its loops.

```csharp
public double Position(VoiceHandle voice)
```

#### Parameters

`voice` [VoiceHandle](MicroEngine.Audio.VoiceHandle.md)

#### Returns

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Audio_AudioEngine_Pump"></a> Pump\(\)

The host frame's audio work, on the main thread outside the simulation step: decodes music streams ahead, frees
streams that ended, then pumps the sink (which renders here when it is pumped by the host).

```csharp
public void Pump()
```

### <a id="MicroEngine_Audio_AudioEngine_Render_System_Span_System_Single__"></a> Render\(Span<float\>\)

The render function the sink calls ([AudioRender](MicroEngine.Platform.AudioRender.md)): mixes the next frames into <code class="paramref">buffer</code>.

```csharp
public void Render(Span<float> buffer)
```

#### Parameters

`buffer` [Span](https://learn.microsoft.com/dotnet/api/system.span\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

### <a id="MicroEngine_Audio_AudioEngine_Seek_MicroEngine_Audio_VoiceHandle_System_Double_"></a> Seek\(VoiceHandle, double\)

Moves <code class="paramref">voice</code> to <code class="paramref">seconds</code> from its sound's beginning, from the current step's
frame: clamped to the sound's length (a sound that does not loop then ends there), and brought into the loop
region when it loops and the time is past the region's end. A paused sound stays paused, at the new place.

```csharp
public bool Seek(VoiceHandle voice, double seconds)
```

#### Parameters

`voice` [VoiceHandle](MicroEngine.Audio.VoiceHandle.md)

`seconds` [double](https://learn.microsoft.com/dotnet/api/system.double)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether it moved: false for a handle that is not playing, for a stream that cannot seek
(one that is not an [ISeekableAudioStream](MicroEngine.Audio.ISeekableAudioStream.md)), and when the sink stopped reading
([DroppedCommands](MicroEngine.Audio.AudioEngine.md#MicroEngine_Audio_AudioEngine_DroppedCommands)). A time that is not a number moves nothing and returns true.

### <a id="MicroEngine_Audio_AudioEngine_Set_MicroEngine_Audio_VoiceHandle_System_Nullable_System_Single__System_Nullable_System_Single__System_Double_"></a> Set\(VoiceHandle, float?, float?, double\)

Changes the volume (0..1) and pan (0..1) of <code class="paramref">voice</code> from the current step's frame; null, or a
value that is not a number, keeps a value. With <code class="paramref">seconds</code>, the values move there over that
time, frame by frame, instead of at once (a ramp stands still while the sound is paused).

```csharp
public bool Set(VoiceHandle voice, float? volume = null, float? pan = null, double seconds = 0)
```

#### Parameters

`voice` [VoiceHandle](MicroEngine.Audio.VoiceHandle.md)

`volume` [float](https://learn.microsoft.com/dotnet/api/system.single)?

`pan` [float](https://learn.microsoft.com/dotnet/api/system.single)?

`seconds` [double](https://learn.microsoft.com/dotnet/api/system.double)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether it was playing.

### <a id="MicroEngine_Audio_AudioEngine_SetPitch_MicroEngine_Audio_VoiceHandle_System_Single_System_Double_"></a> SetPitch\(VoiceHandle, float, double\)

Changes the pitch of <code class="paramref">voice</code> from the current step's frame: the playback speed factor, 1 for the
clip's own pitch, clamped to 0.01..100 as when a sound starts. The sound goes on from where it is, and the
mixer's timeline moves its end accordingly. Streams play at their own pitch. A pitch that is not a number
changes nothing. With <code class="paramref">seconds</code>, the speed moves there over that time, frame by frame (a
glide), instead of at once.

```csharp
public bool SetPitch(VoiceHandle voice, float pitch, double seconds = 0)
```

#### Parameters

`voice` [VoiceHandle](MicroEngine.Audio.VoiceHandle.md)

`pitch` [float](https://learn.microsoft.com/dotnet/api/system.single)

`seconds` [double](https://learn.microsoft.com/dotnet/api/system.double)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether it was playing and is a clip; false for a stale handle or a stream.

### <a id="MicroEngine_Audio_AudioEngine_StepFrame_System_Int64_"></a> StepFrame\(long\)

The first output frame of simulation step <code class="paramref">step</code>.

```csharp
public long StepFrame(long step)
```

#### Parameters

`step` [long](https://learn.microsoft.com/dotnet/api/system.int64)

#### Returns

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Audio_AudioEngine_Stop_MicroEngine_Audio_VoiceHandle_System_Double_"></a> Stop\(VoiceHandle, double\)

Stops <code class="paramref">voice</code> at the current step's frame, fading it out over <code class="paramref">fadeSeconds</code>.

```csharp
public bool Stop(VoiceHandle voice, double fadeSeconds = 0)
```

#### Parameters

`voice` [VoiceHandle](MicroEngine.Audio.VoiceHandle.md)

`fadeSeconds` [double](https://learn.microsoft.com/dotnet/api/system.double)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether it was playing.

### <a id="MicroEngine_Audio_AudioEngine_StreamFailed"></a> StreamFailed

Raised once, on the main thread during [Pump](MicroEngine.Audio.AudioEngine.md#MicroEngine_Audio_AudioEngine_Pump) (or the call that sought it), for each stream counted
in [FailedStreams](MicroEngine.Audio.AudioEngine.md#MicroEngine_Audio_AudioEngine_FailedStreams): the voice that played it (already ended) and what its decoder threw.

```csharp
public event Action<VoiceHandle, Exception>? StreamFailed
```

#### Event Type

 [Action](https://learn.microsoft.com/dotnet/api/system.action\-2)<[VoiceHandle](MicroEngine.Audio.VoiceHandle.md), [Exception](https://learn.microsoft.com/dotnet/api/system.exception)\>?

</div>

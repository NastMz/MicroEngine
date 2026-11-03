# Class Song

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Audio](MicroEngine.Audio.md)  
Assembly: MicroEngine.Audio.dll  
Guide: [Audio](../manual/audio.md)  

A tiny step sequencer. Patterns are whitespace-separated tokens, one per step:
a note ("C4", "F#3", "Bb5"), "-" to hold the previous note, "." to rest, "|" is ignored (bar lines).
Drum tracks use "k" (kick), "s" (snare), "h" (hat), "o" (open hat) and "." for rests.
Shorter tracks loop to the song length, so a four-step drum loop can drive a long melody.

```csharp
public sealed class Song
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Song](MicroEngine.Audio.Song.md)

## Constructors

### <a id="MicroEngine_Audio_Song__ctor_System_Single_System_Int32_"></a> Song\(float, int\)

Creates an empty song.

```csharp
public Song(float bpm, int stepsPerBeat = 4)
```

#### Parameters

`bpm` [float](https://learn.microsoft.com/dotnet/api/system.single)

Tempo in beats per minute; must be positive.

`stepsPerBeat` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Pattern steps per beat; must be positive.

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">bpm</code> or <code class="paramref">stepsPerBeat</code> is not positive.

## Properties

### <a id="MicroEngine_Audio_Song_Bpm"></a> Bpm

Tempo in beats per minute.

```csharp
public float Bpm { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_Song_DrumVolume"></a> DrumVolume

Volume of the drum tracks (default 0.5).

```csharp
public float DrumVolume { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_Song_Echo"></a> Echo

Feedback echo mix (0 disables). Adds space to otherwise dry chip voices.

```csharp
public float Echo { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_Song_EchoDelayBeats"></a> EchoDelayBeats

Delay of the echo in beats (default 0.75).

```csharp
public float EchoDelayBeats { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_Song_LengthInSteps"></a> LengthInSteps

Number of steps in the longest track; the rendered song is this long.

```csharp
public int LengthInSteps { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Audio_Song_StepSeconds"></a> StepSeconds

Length of one step in seconds.

```csharp
public float StepSeconds { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_Song_StepsPerBeat"></a> StepsPerBeat

Pattern steps per beat (4 by default: each step is a sixteenth note in 4/4).

```csharp
public int StepsPerBeat { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Audio_Song_Drums_System_String___"></a> Drums\(params string\[\]\)

Adds a drum track of "k", "s", "h", "o" and "." tokens; the patterns are joined in order.

```csharp
public Song Drums(params string[] patterns)
```

#### Parameters

`patterns` [string](https://learn.microsoft.com/dotnet/api/system.string)\[\]

#### Returns

 [Song](MicroEngine.Audio.Song.md)

This song, for chaining.

### <a id="MicroEngine_Audio_Song_Render_System_Int32_"></a> Render\(int\)

Renders the whole song to stereo PCM, sized to loop seamlessly (echo tails wrap around).

```csharp
public AudioClip Render(int sampleRate = 44100)
```

#### Parameters

`sampleRate` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [AudioClip](MicroEngine.Audio.AudioClip.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The song has no steps.

 [FormatException](https://learn.microsoft.com/dotnet/api/system.formatexception)

A track has an invalid note or a drum track an unknown drum.

### <a id="MicroEngine_Audio_Song_Track_MicroEngine_Audio_Instrument_System_String___"></a> Track\(Instrument, params string\[\]\)

Adds a melodic track. The patterns are joined in order into one sequence of steps.

```csharp
public Song Track(Instrument instrument, params string[] patterns)
```

#### Parameters

`instrument` [Instrument](MicroEngine.Audio.Instrument.md)

The voice that plays the notes.

`patterns` [string](https://learn.microsoft.com/dotnet/api/system.string)\[\]

Note patterns (see the class remarks for the token format).

#### Returns

 [Song](MicroEngine.Audio.Song.md)

This song, for chaining.

</div>

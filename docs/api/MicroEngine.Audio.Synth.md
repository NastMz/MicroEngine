# Class Synth

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Audio](MicroEngine.Audio.md)  
Assembly: MicroEngine.Audio.dll  
Guide: [Audio](../manual/audio.md)  

Deterministic waveform synthesis for sound effects and music voices.

```csharp
public static class Synth
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Synth](MicroEngine.Audio.Synth.md)

## Methods

### <a id="MicroEngine_Audio_Synth_Envelope_System_Single_System_Single_System_Single_System_Single_"></a> Envelope\(float, float, float, float\)

A linear attack-sustain-decay envelope: 0..1 at <code class="paramref">t</code> seconds.

```csharp
public static float Envelope(float t, float attack, float sustain, float decay)
```

#### Parameters

`t` [float](https://learn.microsoft.com/dotnet/api/system.single)

`attack` [float](https://learn.microsoft.com/dotnet/api/system.single)

`sustain` [float](https://learn.microsoft.com/dotnet/api/system.single)

`decay` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_Synth_Layer_System_ValueTuple_MicroEngine_Audio_AudioClip_System_Single____"></a> Layer\(params \(AudioClip Clip, float Offset\)\[\]\)

Mixes several clips (mono) starting at given offsets in seconds.

```csharp
public static AudioClip Layer(params (AudioClip Clip, float Offset)[] parts)
```

#### Parameters

`parts` \([AudioClip](MicroEngine.Audio.AudioClip.md) [Clip](https://learn.microsoft.com/dotnet/api/system.valuetuple\-microengine.audio.audioclip,system.single\-.clip), [float](https://learn.microsoft.com/dotnet/api/system.single) [Offset](https://learn.microsoft.com/dotnet/api/system.valuetuple\-microengine.audio.audioclip,system.single\-.offset)\)\[\]

#### Returns

 [AudioClip](MicroEngine.Audio.AudioClip.md)

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

A clip is not mono or not at [DefaultSampleRate](MicroEngine.Audio.AudioClip.md#MicroEngine_Audio_AudioClip_DefaultSampleRate).

### <a id="MicroEngine_Audio_Synth_NoteFrequency_System_String_"></a> NoteFrequency\(string\)

Frequency of a note name such as "C4", "F#3" or "Bb5" (A4 = 440 Hz).

```csharp
public static float NoteFrequency(string note)
```

#### Parameters

`note` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_Synth_Oscillator_MicroEngine_Audio_Wave_System_Single_System_Single_"></a> Oscillator\(Wave, float, float\)

The value of <code class="paramref">wave</code> at <code class="paramref">phase</code>, in -1..1.

```csharp
public static float Oscillator(Wave wave, float phase, float noise)
```

#### Parameters

`wave` [Wave](MicroEngine.Audio.Wave.md)

The waveform.

`phase` [float](https://learn.microsoft.com/dotnet/api/system.single)

Position in the cycle, 0..1.

`noise` [float](https://learn.microsoft.com/dotnet/api/system.single)

The value returned for [Noise](MicroEngine.Audio.Wave.md).

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_Synth_Render_MicroEngine_Audio_SfxParams_System_Int32_"></a> Render\(SfxParams, int\)

Synthesizes a mono sound effect. The same parameters always produce the same samples.

```csharp
public static AudioClip Render(SfxParams p, int sampleRate = 44100)
```

#### Parameters

`p` [SfxParams](MicroEngine.Audio.SfxParams.md)

The sound description.

`sampleRate` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Samples per second of the result.

#### Returns

 [AudioClip](MicroEngine.Audio.AudioClip.md)

A mono clip, ready for [AddSound](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_AddSound_System_String_MicroEngine_Audio_AudioClip_).

</div>

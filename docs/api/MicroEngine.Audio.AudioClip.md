# Class AudioClip

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Audio](MicroEngine.Audio.md)  
Assembly: MicroEngine.Audio.dll  
Guide: [Audio](../manual/audio.md)  

Uncompressed audio: interleaved float samples in -1..1.

```csharp
public sealed class AudioClip
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[AudioClip](MicroEngine.Audio.AudioClip.md)

## Constructors

### <a id="MicroEngine_Audio_AudioClip__ctor_System_Single___System_Int32_System_Int32_"></a> AudioClip\(float\[\], int, int\)

Wraps existing samples in a clip without copying them.

```csharp
public AudioClip(float[] samples, int sampleRate = 44100, int channels = 1)
```

#### Parameters

`samples` [float](https://learn.microsoft.com/dotnet/api/system.single)\[\]

Interleaved samples in -1..1; the length must be a multiple of <code class="paramref">channels</code>.

`sampleRate` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Samples per second per channel; must be positive.

`channels` [int](https://learn.microsoft.com/dotnet/api/system.int32)

1 or 2.

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">sampleRate</code> is not positive or <code class="paramref">channels</code> is not 1 or 2.

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

The sample count is not a multiple of the channel count.

## Fields

### <a id="MicroEngine_Audio_AudioClip_DefaultSampleRate"></a> DefaultSampleRate

Sample rate used by the synthesizer and by default for new clips: 44100 Hz.

```csharp
public const int DefaultSampleRate = 44100
```

#### Field Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Properties

### <a id="MicroEngine_Audio_AudioClip_Channels"></a> Channels

1 (mono) or 2 (stereo).

```csharp
public int Channels { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Audio_AudioClip_Duration"></a> Duration

Length of the clip in seconds.

```csharp
public double Duration { get; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Audio_AudioClip_Peak"></a> Peak

The largest absolute sample value (0 for an empty clip). Computed on every read.

```csharp
public float Peak { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_AudioClip_SampleRate"></a> SampleRate

Samples per second per channel.

```csharp
public int SampleRate { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Audio_AudioClip_Samples"></a> Samples

The samples, interleaved by channel (left, right, left... for stereo), each in -1..1. The array is not copied.

```csharp
public float[] Samples { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)\[\]

## Methods

### <a id="MicroEngine_Audio_AudioClip_FromWav_System_ReadOnlySpan_System_Byte__"></a> FromWav\(ReadOnlySpan<byte\>\)

Decodes an uncompressed PCM WAV (8, 16 or 24-bit, mono or stereo).

```csharp
public static AudioClip FromWav(ReadOnlySpan<byte> data)
```

#### Parameters

`data` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[byte](https://learn.microsoft.com/dotnet/api/system.byte)\>

#### Returns

 [AudioClip](MicroEngine.Audio.AudioClip.md)

### <a id="MicroEngine_Audio_AudioClip_ToWav"></a> ToWav\(\)

Encodes 16-bit PCM WAV bytes, the interchange format backends load from memory.

```csharp
public byte[] ToWav()
```

#### Returns

 [byte](https://learn.microsoft.com/dotnet/api/system.byte)\[\]

</div>

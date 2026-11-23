# Interface ISeekableAudioStream

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Audio](MicroEngine.Audio.md)  
Assembly: MicroEngine.Audio.dll  
Guide: [Audio](../manual/audio.md)  

A stream that knows its length and can be read from any frame: the mixer can then start it anywhere
([StartAt](MicroEngine.Audio.PlayParameters.md#MicroEngine_Audio_PlayParameters_StartAt)), move it while it plays ([Seek](MicroEngine.Audio.AudioEngine.md#MicroEngine_Audio_AudioEngine_Seek_MicroEngine_Audio_VoiceHandle_System_Double_)), repeat a part of it
([LoopRegion](MicroEngine.Audio.ISeekableAudioStream.md#MicroEngine_Audio_ISeekableAudioStream_LoopRegion)) and tell how long it is. Like [Read](MicroEngine.Audio.IAudioStream.md#MicroEngine_Audio_IAudioStream_Read_System_Span_System_Single__), [Seek](MicroEngine.Audio.ISeekableAudioStream.md#MicroEngine_Audio_ISeekableAudioStream_Seek_System_Int64_) is called
on the main thread only.

```csharp
public interface ISeekableAudioStream : IAudioStream, IDisposable
```

#### Implements

[IAudioStream](MicroEngine.Audio.IAudioStream.md), 
[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

## Properties

### <a id="MicroEngine_Audio_ISeekableAudioStream_Frames"></a> Frames

The stream's length in frames; 0 when it is not known.

```csharp
long Frames { get; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Audio_ISeekableAudioStream_LoopRegion"></a> LoopRegion

The loop region the file itself names (its metadata), or null.

```csharp
LoopRegion? LoopRegion { get; }
```

#### Property Value

 [LoopRegion](MicroEngine.Audio.LoopRegion.md)?

## Methods

### <a id="MicroEngine_Audio_ISeekableAudioStream_Seek_System_Int64_"></a> Seek\(long\)

Makes <code class="paramref">frame</code> (0 to [Frames](MicroEngine.Audio.ISeekableAudioStream.md#MicroEngine_Audio_ISeekableAudioStream_Frames)) the next frame [Read](MicroEngine.Audio.IAudioStream.md#MicroEngine_Audio_IAudioStream_Read_System_Span_System_Single__) gives.

```csharp
void Seek(long frame)
```

#### Parameters

`frame` [long](https://learn.microsoft.com/dotnet/api/system.int64)

</div>

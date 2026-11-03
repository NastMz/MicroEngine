# Delegate AudioRender

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Platform](MicroEngine.Platform.md)  
Assembly: MicroEngine.Platform.dll  
Guide: [The platform host](../manual/backends.md#the-platform-host)  

Fills all of <code class="paramref">buffer</code> (<code>buffer.Length / Channels</code> frames of the sink's
[AudioFormat](MicroEngine.Platform.AudioFormat.md)). Called by one thread at a time; which thread depends on
[Schedule](MicroEngine.Platform.IAudioSink.md#MicroEngine_Platform_IAudioSink_Schedule). It never blocks, allocates, takes a lock the main thread holds, or throws.

```csharp
public delegate void AudioRender(Span<float> buffer)
```

#### Parameters

`buffer` [Span](https://learn.microsoft.com/dotnet/api/system.span\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

The interleaved samples to write.

</div>

# Enum AudioSinkSchedule

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Platform](MicroEngine.Platform.md)  
Assembly: MicroEngine.Platform.dll  
Guide: [The platform host](../manual/backends.md#the-platform-host)  

Who calls a sink's [AudioRender](MicroEngine.Platform.AudioRender.md).

```csharp
public enum AudioSinkSchedule
```

## Fields

`HostPump = 1` 

The host calls [Pump](MicroEngine.Platform.IAudioSink.md#MicroEngine_Platform_IAudioSink_Pump) once per frame on the main thread, which calls
[AudioRender](MicroEngine.Platform.AudioRender.md) (browsers: an AudioWorklet queue filled up to 100 ms ahead; tests: an offline sink
rendering exactly one simulation step per pump).



`SinkThread = 0` 

The sink calls [AudioRender](MicroEngine.Platform.AudioRender.md) on a thread it owns, and [Pump](MicroEngine.Platform.IAudioSink.md#MicroEngine_Platform_IAudioSink_Pump) does nothing
(SDL3 platforms: a feeder thread keeping 60 ms queued in SDL's audio stream).

</div>

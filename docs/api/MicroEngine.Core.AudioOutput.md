# Enum AudioOutput

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

Where a run's sound may go ([AudioOutput](MicroEngine.Core.PlatformRequest.md#MicroEngine_Core_PlatformRequest_AudioOutput), the launcher's <code>--audio</code>).

```csharp
public enum AudioOutput
```

## Fields

`Automatic = 0` 

The default. A platform opens a physical device, as for [Device](MicroEngine.Core.AudioOutput.md); the launcher's <code>--audio auto</code>
asks for none when the run is in deterministic time (scripts), whose sound would not follow the wall clock.



`Device = 1` 

Always a physical device, for listening checks and package checks that must prove a device opens.



`Silent = 2` 

Never a physical device.

</div>

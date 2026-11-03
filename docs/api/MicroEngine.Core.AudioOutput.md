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

A physical device for real-time runs (hidden windows included); none for deterministic-time (scripted) runs.



`Device = 1` 

Always a physical device, for listening checks and package checks that must prove a device opens.



`Silent = 2` 

Never a physical device.

</div>

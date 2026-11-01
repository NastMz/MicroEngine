# Enum PlatformParts

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

The subsystems a host may supply to a profile instead of the profile's own.

```csharp
[Flags]
public enum PlatformParts
```

## Fields

`Audio = 1` 

Replace the audio device ([Audio](MicroEngine.Core.PlatformRequest.md#MicroEngine_Core_PlatformRequest_Audio)).



`Input = 2` 

Add input sources next to the backend's ([ExtraInput](MicroEngine.Core.PlatformRequest.md#MicroEngine_Core_PlatformRequest_ExtraInput)).



`None = 0` 

The host may replace nothing.

</div>

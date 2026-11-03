# Interface IDisplayPacing

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Platform](MicroEngine.Platform.md)  
Assembly: MicroEngine.Platform.dll  
Guide: [The platform host](../manual/backends.md#the-platform-host)  

The refresh rate a host's frames are paced to, when a display paces them (VSync). A capability
([TryGetCapability``1](MicroEngine.Platform.IPlatformHost.md#MicroEngine_Platform_IPlatformHost_TryGetCapability__1___0__)): a host without it never paces frames to a display.

```csharp
public interface IDisplayPacing
```

## Properties

### <a id="MicroEngine_Platform_IDisplayPacing_DisplayRate"></a> DisplayRate

The refresh rate in Hz of the display that paces the frames, or 0 when frames are not paced to a display right
now or its rate is unknown. The loop then counts elapsed time in refresh periods.

```csharp
double DisplayRate { get; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

</div>

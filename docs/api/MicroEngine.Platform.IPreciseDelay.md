# Interface IPreciseDelay

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Platform](MicroEngine.Platform.md)  
Assembly: MicroEngine.Platform.dll  
Guide: [The platform host](../manual/backends.md#the-platform-host)  

A precise way to wait, used by the loop to hold a frame back until the frame limit allows it. A capability
([TryGetCapability``1](MicroEngine.Platform.IPlatformHost.md#MicroEngine_Platform_IPlatformHost_TryGetCapability__1___0__)) of hosts that can block ([CanBlock](MicroEngine.Platform.IPlatformHost.md#MicroEngine_Platform_IPlatformHost_CanBlock));
without it the loop sleeps and then spins for the last milliseconds.

```csharp
public interface IPreciseDelay
```

## Methods

### <a id="MicroEngine_Platform_IPreciseDelay_Delay_System_Double_"></a> Delay\(double\)

Blocks for about <code class="paramref">seconds</code>, precisely enough to pace frames.

```csharp
void Delay(double seconds)
```

#### Parameters

`seconds` [double](https://learn.microsoft.com/dotnet/api/system.double)

</div>

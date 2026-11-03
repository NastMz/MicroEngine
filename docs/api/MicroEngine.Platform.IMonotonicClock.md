# Interface IMonotonicClock

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Platform](MicroEngine.Platform.md)  
Assembly: MicroEngine.Platform.dll  
Guide: [The platform host](../manual/backends.md#the-platform-host)  

A monotonic clock in seconds: it never goes back and does not depend on frames, steps or rates.

```csharp
public interface IMonotonicClock
```

## Properties

### <a id="MicroEngine_Platform_IMonotonicClock_Now"></a> Now

Seconds since an arbitrary start (the host's creation); never decreases.

```csharp
double Now { get; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

</div>

# Interface IEngineLifecycle

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Kernel](MicroEngine.Kernel.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

Engine events: the things that happen to a running game from outside its simulation. Raised on the game's thread,
between frames (before the frame that follows them), in the order they happened; handlers run in the order they
subscribed.

```csharp
public interface IEngineLifecycle
```

### <a id="MicroEngine_Kernel_IEngineLifecycle_FocusChanged"></a> FocusChanged

The game's window gained (true) or lost (false) the input focus. Raised by hosts that report it.

```csharp
event Action<bool>? FocusChanged
```

#### Event Type

 [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[bool](https://learn.microsoft.com/dotnet/api/system.boolean)\>?

### <a id="MicroEngine_Kernel_IEngineLifecycle_QuitRequested"></a> QuitRequested

Something outside the game asked it to quit; a handler may [Veto](MicroEngine.Kernel.QuitRequest.md#MicroEngine_Kernel_QuitRequest_Veto) it.

```csharp
event Action<QuitRequest>? QuitRequested
```

#### Event Type

 [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[QuitRequest](MicroEngine.Kernel.QuitRequest.md)\>?

### <a id="MicroEngine_Kernel_IEngineLifecycle_Resized"></a> Resized

The drawing surface changed size. Raised by hosts that report it.

```csharp
event Action<SurfaceResized>? Resized
```

#### Event Type

 [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[SurfaceResized](MicroEngine.Kernel.SurfaceResized.md)\>?

### <a id="MicroEngine_Kernel_IEngineLifecycle_Resumed"></a> Resumed

A suspended game came back; raised before its next frame.

```csharp
event Action? Resumed
```

#### Event Type

 [Action](https://learn.microsoft.com/dotnet/api/system.action)?

### <a id="MicroEngine_Kernel_IEngineLifecycle_Suspending"></a> Suspending

The system is taking the game away (a phone app sent to the background); nothing runs until [Resumed](MicroEngine.Kernel.IEngineLifecycle.md#MicroEngine_Kernel_IEngineLifecycle_Resumed).

```csharp
event Action? Suspending
```

#### Event Type

 [Action](https://learn.microsoft.com/dotnet/api/system.action)?

</div>

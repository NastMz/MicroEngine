# Class EngineLifecycle

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Kernel](MicroEngine.Kernel.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

The engine's [IEngineLifecycle](MicroEngine.Kernel.IEngineLifecycle.md): subscribers read it through the interface; the loop and hosts raise
the events through the <code>Raise</code> methods.

```csharp
public sealed class EngineLifecycle : IEngineLifecycle
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[EngineLifecycle](MicroEngine.Kernel.EngineLifecycle.md)

#### Implements

[IEngineLifecycle](MicroEngine.Kernel.IEngineLifecycle.md)

## Constructors

### <a id="MicroEngine_Kernel_EngineLifecycle__ctor"></a> EngineLifecycle\(\)

```csharp
public EngineLifecycle()
```

## Methods

### <a id="MicroEngine_Kernel_EngineLifecycle_RaiseFocusChanged_System_Boolean_"></a> RaiseFocusChanged\(bool\)

Raises [FocusChanged](MicroEngine.Kernel.EngineLifecycle.md#MicroEngine_Kernel_EngineLifecycle_FocusChanged).

```csharp
public void RaiseFocusChanged(bool focused)
```

#### Parameters

`focused` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Kernel_EngineLifecycle_RaiseQuitRequested"></a> RaiseQuitRequested\(\)

Raises [QuitRequested](MicroEngine.Kernel.EngineLifecycle.md#MicroEngine_Kernel_EngineLifecycle_QuitRequested); every handler runs even after a veto.

```csharp
public bool RaiseQuitRequested()
```

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

True when the game should quit (no handler vetoed).

### <a id="MicroEngine_Kernel_EngineLifecycle_RaiseResized_MicroEngine_Kernel_SurfaceResized_"></a> RaiseResized\(SurfaceResized\)

Raises [Resized](MicroEngine.Kernel.EngineLifecycle.md#MicroEngine_Kernel_EngineLifecycle_Resized).

```csharp
public void RaiseResized(SurfaceResized size)
```

#### Parameters

`size` [SurfaceResized](MicroEngine.Kernel.SurfaceResized.md)

### <a id="MicroEngine_Kernel_EngineLifecycle_RaiseResumed"></a> RaiseResumed\(\)

Raises [Resumed](MicroEngine.Kernel.EngineLifecycle.md#MicroEngine_Kernel_EngineLifecycle_Resumed).

```csharp
public void RaiseResumed()
```

### <a id="MicroEngine_Kernel_EngineLifecycle_RaiseSuspending"></a> RaiseSuspending\(\)

Raises [Suspending](MicroEngine.Kernel.EngineLifecycle.md#MicroEngine_Kernel_EngineLifecycle_Suspending).

```csharp
public void RaiseSuspending()
```

### <a id="MicroEngine_Kernel_EngineLifecycle_FocusChanged"></a> FocusChanged

The game's window gained (true) or lost (false) the input focus. Raised by hosts that report it.

```csharp
public event Action<bool>? FocusChanged
```

#### Event Type

 [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[bool](https://learn.microsoft.com/dotnet/api/system.boolean)\>?

### <a id="MicroEngine_Kernel_EngineLifecycle_QuitRequested"></a> QuitRequested

Something outside the game asked it to quit; a handler may [Veto](MicroEngine.Kernel.QuitRequest.md#MicroEngine_Kernel_QuitRequest_Veto) it.

```csharp
public event Action<QuitRequest>? QuitRequested
```

#### Event Type

 [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[QuitRequest](MicroEngine.Kernel.QuitRequest.md)\>?

### <a id="MicroEngine_Kernel_EngineLifecycle_Resized"></a> Resized

The drawing surface changed size. Raised by hosts that report it.

```csharp
public event Action<SurfaceResized>? Resized
```

#### Event Type

 [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[SurfaceResized](MicroEngine.Kernel.SurfaceResized.md)\>?

### <a id="MicroEngine_Kernel_EngineLifecycle_Resumed"></a> Resumed

A suspended game came back; raised before its next frame.

```csharp
public event Action? Resumed
```

#### Event Type

 [Action](https://learn.microsoft.com/dotnet/api/system.action)?

### <a id="MicroEngine_Kernel_EngineLifecycle_Suspending"></a> Suspending

The system is taking the game away (a phone app sent to the background); nothing runs until [Resumed](MicroEngine.Kernel.IEngineLifecycle.md#MicroEngine_Kernel_IEngineLifecycle_Resumed).

```csharp
public event Action? Suspending
```

#### Event Type

 [Action](https://learn.microsoft.com/dotnet/api/system.action)?

</div>

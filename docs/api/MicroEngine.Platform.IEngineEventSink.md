# Interface IEngineEventSink

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Platform](MicroEngine.Platform.md)  
Assembly: MicroEngine.Platform.dll  
Guide: [The platform host](../manual/backends.md#the-platform-host)  

Where a host delivers what happens to the game from outside its simulation ([PumpEvents](MicroEngine.Platform.IPlatformHost.md#MicroEngine_Platform_IPlatformHost_PumpEvents_MicroEngine_Platform_IEngineEventSink_)).
The engine's loop implements it and raises the matching [IEngineLifecycle](MicroEngine.Kernel.IEngineLifecycle.md) events.

```csharp
public interface IEngineEventSink
```

## Methods

### <a id="MicroEngine_Platform_IEngineEventSink_FocusChanged_System_Boolean_"></a> FocusChanged\(bool\)

The game's window gained (true) or lost (false) the input focus.

```csharp
void FocusChanged(bool focused)
```

#### Parameters

`focused` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Platform_IEngineEventSink_QuitRequested"></a> QuitRequested\(\)

Something outside the game asked it to quit (the window's close button, the system).

```csharp
void QuitRequested()
```

### <a id="MicroEngine_Platform_IEngineEventSink_Resized_MicroEngine_Kernel_SurfaceResized_"></a> Resized\(SurfaceResized\)

The main surface changed its size in pixels.

```csharp
void Resized(SurfaceResized size)
```

#### Parameters

`size` [SurfaceResized](MicroEngine.Kernel.SurfaceResized.md)

### <a id="MicroEngine_Platform_IEngineEventSink_Resumed"></a> Resumed\(\)

A suspended game came back.

```csharp
void Resumed()
```

### <a id="MicroEngine_Platform_IEngineEventSink_Suspending"></a> Suspending\(\)

The system is taking the game away (a phone app sent to the background, a hidden page).

```csharp
void Suspending()
```

</div>

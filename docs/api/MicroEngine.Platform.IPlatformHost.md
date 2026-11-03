# Interface IPlatformHost

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Platform](MicroEngine.Platform.md)  
Assembly: MicroEngine.Platform.dll  
Guide: [The platform host](../manual/backends.md#the-platform-host)  

The platform a game runs on, as the loop sees it: a monotonic clock, the events of the window and the system, the
main surface, optional features found by type and whether the host may block its thread.

```csharp
public interface IPlatformHost : IDisposable
```

#### Implements

[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

## Remarks

<p>The platform contract, which every host follows:</p>
<ol><li><b>One thread.</b> The loop calls every member on the game's thread. A host that learns of an event on another
thread (a phone's lifecycle callbacks) holds it until the next [PumpEvents](MicroEngine.Platform.IPlatformHost.md#MicroEngine_Platform_IPlatformHost_PumpEvents_MicroEngine_Platform_IEngineEventSink_).</li><li><b>Events are delivered only by [PumpEvents](MicroEngine.Platform.IPlatformHost.md#MicroEngine_Platform_IPlatformHost_PumpEvents_MicroEngine_Platform_IEngineEventSink_)</b>, which the loop calls once at the start of every
frame, in the order they happened. Nothing is delivered between pumps.</li><li><b>Lifecycle order.</b> [Suspending](MicroEngine.Platform.IEngineEventSink.md#MicroEngine_Platform_IEngineEventSink_Suspending) and [Resumed](MicroEngine.Platform.IEngineEventSink.md#MicroEngine_Platform_IEngineEventSink_Resumed)
alternate, starting with <code>Suspending</code>: a host never sends two in a row, and never <code>Resumed</code> without a
<code>Suspending</code> before it. A trip to the background and back that happened between two pumps is delivered as both,
in that order, in one pump. [FocusChanged](MicroEngine.Platform.IEngineEventSink.md#MicroEngine_Platform_IEngineEventSink_FocusChanged_System_Boolean_) is sent only when the focus changes.
[QuitRequested](MicroEngine.Platform.IEngineEventSink.md#MicroEngine_Platform_IEngineEventSink_QuitRequested) is sent once per request; the loop decides (handlers may veto it), and
a later request is sent again.</li><li><b>Resizes.</b> When the main surface's pixel size changes, the same pump raises [Resized](MicroEngine.Platform.ISurface.md#MicroEngine_Platform_ISurface_Resized)
and delivers [Resized](MicroEngine.Platform.IEngineEventSink.md#MicroEngine_Platform_IEngineEventSink_Resized_MicroEngine_Kernel_SurfaceResized_) with the new [PixelSize](MicroEngine.Platform.ISurface.md#MicroEngine_Platform_ISurface_PixelSize); a size that did
not change is not delivered.</li><li><b>Clock.</b> [Clock](MicroEngine.Platform.IPlatformHost.md#MicroEngine_Platform_IPlatformHost_Clock) is monotonic and nothing else: it is never derived from frames, steps or the
update rate. The loop owns time.</li><li><b>Capabilities.</b> Optional features (changing the display mode, diagnostics, a touch screen, display
pacing, a precise timer) are found with [TryGetCapability``1](MicroEngine.Platform.IPlatformHost.md#MicroEngine_Platform_IPlatformHost_TryGetCapability__1___0__), never by casting the host. A host's
capabilities do not change while it runs (a capability's values may). A host that wraps another forwards the
query.</li><li><b>Page-driven hosts.</b> A host whose frames are driven by a page (a browser's animation frames) reports
[CanBlock](MicroEngine.Platform.IPlatformHost.md#MicroEngine_Platform_IPlatformHost_CanBlock) false and then never blocks: no waits, sleeps or precise delays in any member; the page paces
the frames. It reads the surface size from the canvas after start, never from the size requested at creation. It
opens the audio output inside the user gesture that starts the game.</li><li><b>No time coupling.</b> Nothing a host does depends on how the loop advances time: display pacing, device
start-up and audio output are explicit options of the host's creation.</li></ol>

## Properties

### <a id="MicroEngine_Platform_IPlatformHost_CanBlock"></a> CanBlock

Whether the host may block its thread to pace frames (a desktop window, a phone, headless runs). False for
page-driven hosts (a browser), which must never block: the loop then skips a frame that comes early instead of
waiting for it.

```csharp
bool CanBlock { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Platform_IPlatformHost_Clock"></a> Clock

The host's monotonic clock.

```csharp
IMonotonicClock Clock { get; }
```

#### Property Value

 [IMonotonicClock](MicroEngine.Platform.IMonotonicClock.md)

### <a id="MicroEngine_Platform_IPlatformHost_MainSurface"></a> MainSurface

The surface the game presents to (its window or canvas), or null when the host has none.

```csharp
ISurface? MainSurface { get; }
```

#### Property Value

 [ISurface](MicroEngine.Platform.ISurface.md)?

## Methods

### <a id="MicroEngine_Platform_IPlatformHost_PumpEvents_MicroEngine_Platform_IEngineEventSink_"></a> PumpEvents\(IEngineEventSink\)

Processes the system's pending events (window, keyboard, gamepad, touch) and delivers those the engine handles
to <code class="paramref">sink</code>, in the order they happened. Called by the loop once at the start of every frame.

```csharp
bool PumpEvents(IEngineEventSink sink)
```

#### Parameters

`sink` [IEngineEventSink](MicroEngine.Platform.IEngineEventSink.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

True when the game can run a frame now; false while it is suspended (between <code>Suspending</code> and <code>Resumed</code>).

### <a id="MicroEngine_Platform_IPlatformHost_TryGetCapability__1___0__"></a> TryGetCapability<T\>\(out T?\)

The optional feature of type <code class="typeparamref">T</code> this host offers, if it offers one.

```csharp
bool TryGetCapability<T>(out T? capability) where T : class
```

#### Parameters

`capability` T?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Type Parameters

`T`

</div>

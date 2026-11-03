# Namespace MicroEngine.Kernel

<div data-pagefind-weight="0.3">

The kernel: engine composition (builder, services, modules, capabilities), the frame schedule and its phases, loop-owned time, engine events, logging and generational handles.

Guide: [Modules and the frame schedule](../manual/lifecycle.md#modules-and-the-frame-schedule)  

### Classes

 [CapabilityRegistry](MicroEngine.Kernel.CapabilityRegistry.md)

Optional features something in the engine offers (a display that can change its mode, a touch screen, platform
diagnostics), declared by whoever provides them and queried by type. A feature that is absent is simply not
declared: callers ask with [TryGet``1](MicroEngine.Kernel.CapabilityRegistry.md#MicroEngine_Kernel_CapabilityRegistry_TryGet__1___0__) instead of casting a platform or device to find out.

 [ConsoleLog](MicroEngine.Kernel.ConsoleLog.md)

The default [ILog](MicroEngine.Kernel.ILog.md): each message at or above [MinimumLevel](MicroEngine.Kernel.ConsoleLog.md#MicroEngine_Kernel_ConsoleLog_MinimumLevel) becomes one line
<code>category: message</code> on `Error` (read when the message is written, so a redirected stream
receives it) or on the writer it was given.

 [EngineBuilder](MicroEngine.Kernel.EngineBuilder.md)

Composes an [Engine](MicroEngine.Kernel.Engine.md): modules are added in order, each registering services, frame systems and
capabilities; settings records are edited; [Build](MicroEngine.Kernel.EngineBuilder.md#MicroEngine_Kernel_EngineBuilder_Build) then fixes the schedule and the time policy.
Nothing is discovered by reflection.

 [EngineLifecycle](MicroEngine.Kernel.EngineLifecycle.md)

The engine's [IEngineLifecycle](MicroEngine.Kernel.IEngineLifecycle.md): subscribers read it through the interface; the loop and hosts raise
the events through the <code>Raise</code> methods.

 [Engine](MicroEngine.Kernel.Engine.md)

A composed engine: its services, its fixed frame schedule, its clock, its events and its capabilities. A loop (the
game runner, a host, a test) drives it frame by frame: it advances [Clock](MicroEngine.Kernel.Engine.md#MicroEngine_Kernel_Engine_Clock) and runs the phases in order.

 [FrameClock](MicroEngine.Kernel.FrameClock.md)

The loop's clock: turns the time each frame stands for into fixed steps, following [TimeSettings](MicroEngine.Kernel.TimeSettings.md). The
platform only supplies a monotonic clock; how much simulated time a frame adds, how many steps it runs, how far a slow
frame catches up and the interpolation blend are decided here, the same way on every backend.

 [FrameSchedule](MicroEngine.Kernel.FrameSchedule.md)

The systems that run each frame: named callbacks placed in a [FramePhase](MicroEngine.Kernel.FramePhase.md), run in phase order. Within a
phase, systems run in the order they were added unless a system asks to run after or before another one of the same
phase; the order is resolved once, when the engine is built, and a cycle of such constraints is an error that names
the systems in it.

 [HandlePool<T\>](MicroEngine.Kernel.HandlePool\-1.md)

Items reached through generational [Handle`1](MicroEngine.Kernel.md)s: slots are reused, and a handle to a removed item is
detected (stale) instead of reaching the item that took its slot.

 [QuitRequest](MicroEngine.Kernel.QuitRequest.md)

A request to quit that came from outside the game (the window's close button, the system), passed to
[QuitRequested](MicroEngine.Kernel.IEngineLifecycle.md#MicroEngine_Kernel_IEngineLifecycle_QuitRequested). A handler that must keep the game running (unsaved work, a confirmation
dialog) calls [Veto](MicroEngine.Kernel.QuitRequest.md#MicroEngine_Kernel_QuitRequest_Veto); the game can quit later on its own.

 [SeededRandom](MicroEngine.Kernel.SeededRandom.md)

A `Random` that draws exactly what <code>new Random(seed)</code> draws, and whose state can take part in the
per-step state hash ([IStateHashed](MicroEngine.Kernel.IStateHashed.md)): its seed, how many values it drew and a running hash of them. Two
runs that drew the same values from the same seed hash the same; one extra or different draw changes the hash.

 [ServiceRegistry](MicroEngine.Kernel.ServiceRegistry.md)

The engine's services, one instance per type, registered by modules and read by whoever needs them. Typed and
explicit: a service is found by the exact type it was registered as (no base classes, no interfaces it also
implements), nothing is created on demand and nothing is discovered by reflection, so the registry works the same
under trimming, NativeAOT and WebAssembly.

 [StateDivergence](MicroEngine.Kernel.StateDivergence.md)

Where two traces first differ ([FirstDivergence](MicroEngine.Kernel.StateTrace.md#MicroEngine_Kernel_StateTrace_FirstDivergence_MicroEngine_Kernel_StateTrace_MicroEngine_Kernel_StateTrace_)).

 [StateHasher](MicroEngine.Kernel.StateHasher.md)

Accumulates a 64-bit hash (FNV-1a) of simulation state in named sections. Values are encoded in a fixed binary form
(little-endian; floats by their bits, so <code>-0</code> and NaN payloads count; strings length-prefixed), so the same
values in the same order give the same hash on every machine and run. Nothing is read by reflection: the engine and
the game add what they choose ([IStateHashed](MicroEngine.Kernel.IStateHashed.md)).

 [StateTrace](MicroEngine.Kernel.StateTrace.md)

A run's per-step state hashes: the loop records one [StepHash](MicroEngine.Kernel.StepHash.md) after every fixed step when a run is given
one (<code>RunOptions.StateTrace</code>). Two runs of the same build, script and seed must record the same trace;
[FirstDivergence](MicroEngine.Kernel.StateTrace.md#MicroEngine_Kernel_StateTrace_FirstDivergence_MicroEngine_Kernel_StateTrace_MicroEngine_Kernel_StateTrace_) names the first update and section where two differ.

 [StepHash](MicroEngine.Kernel.StepHash.md)

The state hash after one fixed step.

 [TimeSettings](MicroEngine.Kernel.TimeSettings.md)

The loop's time policy: the fixed simulation rate, how far a slow frame may catch up, and whether time follows the
wall clock. Every value is explicit; nothing is derived from the platform or from other options.

### Structs

 [FrameContext](MicroEngine.Kernel.FrameContext.md)

What a frame system knows about time when it runs ([FrameSchedule](MicroEngine.Kernel.FrameSchedule.md)).

 [Handle<T\>](MicroEngine.Kernel.Handle\-1.md)

A reference to an item of a [HandlePool`1](MicroEngine.Kernel.md): a slot index and the generation of the item that held the
slot when the handle was made. When the item is removed its slot's generation changes, so an old handle no longer
finds anything (instead of finding whatever reuses the slot). <code>default</code> is [None](MicroEngine.Kernel.md).

 [SectionHash](MicroEngine.Kernel.SectionHash.md)

The hash of one named part of the state of a step ([Sections](MicroEngine.Kernel.StepHash.md#MicroEngine_Kernel_StepHash_Sections)).

 [SurfaceResized](MicroEngine.Kernel.SurfaceResized.md)

The new size of the drawing surface, in pixels ([Resized](MicroEngine.Kernel.IEngineLifecycle.md#MicroEngine_Kernel_IEngineLifecycle_Resized)).

### Interfaces

 [IEngineLifecycle](MicroEngine.Kernel.IEngineLifecycle.md)

Engine events: the things that happen to a running game from outside its simulation. Raised on the game's thread,
between frames (before the frame that follows them), in the order they happened; handlers run in the order they
subscribed.

 [IEngineModule](MicroEngine.Kernel.IEngineModule.md)

A part of the engine or of a game that registers itself: services, frame systems, capabilities, settings and
lifecycle handlers, all from [Configure](MicroEngine.Kernel.IEngineModule.md#MicroEngine_Kernel_IEngineModule_Configure_MicroEngine_Kernel_EngineBuilder_). Rendering, input, audio, scenes and the game itself are modules.

 [ILog](MicroEngine.Kernel.ILog.md)

Where the engine's diagnostics go. The default ([ConsoleLog](MicroEngine.Kernel.ConsoleLog.md)) writes them to the standard error stream;
a host that has its own (a phone's system log, a browser console, an editor panel) registers another sink as the
engine's [ILog](MicroEngine.Kernel.ILog.md) service.

 [IStateHashed](MicroEngine.Kernel.IStateHashed.md)

Something whose simulation state takes part in the per-step state hash ([StateHasher](MicroEngine.Kernel.StateHasher.md)): it adds the
values that decide what the game does next, and leaves out what only drawing uses.

### Enums

 [FramePhase](MicroEngine.Kernel.FramePhase.md)

The phases of a frame, in the order they run. The four fixed phases ([PreFixed](MicroEngine.Kernel.FramePhase.md),
[FixedUpdate](MicroEngine.Kernel.FramePhase.md), [Physics](MicroEngine.Kernel.FramePhase.md), [PostFixed](MicroEngine.Kernel.FramePhase.md)) run once per fixed step, zero or more times
a frame as time calls for ([TimeSettings](MicroEngine.Kernel.TimeSettings.md)); every other phase runs once per frame.

 [LogLevel](MicroEngine.Kernel.LogLevel.md)

How important a log message is.

 [TimeMode](MicroEngine.Kernel.TimeMode.md)

How the loop advances time ([Mode](MicroEngine.Kernel.TimeSettings.md#MicroEngine_Kernel_TimeSettings_Mode)).

</div>

# Class Engine

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Kernel](MicroEngine.Kernel.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Modules and the frame schedule](../manual/lifecycle.md#modules-and-the-frame-schedule)  

A composed engine: its services, its fixed frame schedule, its clock, its events and its capabilities. A loop (the
game runner, a host, a test) drives it frame by frame: it advances [Clock](MicroEngine.Kernel.Engine.md#MicroEngine_Kernel_Engine_Clock) and runs the phases in order.

```csharp
public sealed class Engine
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Engine](MicroEngine.Kernel.Engine.md)

## Properties

### <a id="MicroEngine_Kernel_Engine_Capabilities"></a> Capabilities

The declared optional features.

```csharp
public CapabilityRegistry Capabilities { get; }
```

#### Property Value

 [CapabilityRegistry](MicroEngine.Kernel.CapabilityRegistry.md)

### <a id="MicroEngine_Kernel_Engine_Clock"></a> Clock

The loop's clock, following the engine's [TimeSettings](MicroEngine.Kernel.TimeSettings.md).

```csharp
public FrameClock Clock { get; }
```

#### Property Value

 [FrameClock](MicroEngine.Kernel.FrameClock.md)

### <a id="MicroEngine_Kernel_Engine_Lifecycle"></a> Lifecycle

The engine's events; the loop and hosts raise them.

```csharp
public EngineLifecycle Lifecycle { get; }
```

#### Property Value

 [EngineLifecycle](MicroEngine.Kernel.EngineLifecycle.md)

### <a id="MicroEngine_Kernel_Engine_Log"></a> Log

The engine's log ([ILog](MicroEngine.Kernel.ILog.md) service).

```csharp
public ILog Log { get; }
```

#### Property Value

 [ILog](MicroEngine.Kernel.ILog.md)

### <a id="MicroEngine_Kernel_Engine_Schedule"></a> Schedule

The frame systems; fixed ([IsFrozen](MicroEngine.Kernel.FrameSchedule.md#MicroEngine_Kernel_FrameSchedule_IsFrozen)).

```csharp
public FrameSchedule Schedule { get; }
```

#### Property Value

 [FrameSchedule](MicroEngine.Kernel.FrameSchedule.md)

### <a id="MicroEngine_Kernel_Engine_Services"></a> Services

The services registered by the modules.

```csharp
public ServiceRegistry Services { get; }
```

#### Property Value

 [ServiceRegistry](MicroEngine.Kernel.ServiceRegistry.md)

### <a id="MicroEngine_Kernel_Engine_Time"></a> Time

The time policy the engine was built with.

```csharp
public TimeSettings Time { get; }
```

#### Property Value

 [TimeSettings](MicroEngine.Kernel.TimeSettings.md)

## Methods

### <a id="MicroEngine_Kernel_Engine_Run_MicroEngine_Kernel_FramePhase_MicroEngine_Kernel_FrameContext__"></a> Run\(FramePhase, in FrameContext\)

Runs the systems of <code class="paramref">phase</code> with <code class="paramref">context</code>.

```csharp
public void Run(FramePhase phase, in FrameContext context)
```

#### Parameters

`phase` [FramePhase](MicroEngine.Kernel.FramePhase.md)

`context` [FrameContext](MicroEngine.Kernel.FrameContext.md)

### <a id="MicroEngine_Kernel_Engine_RunFixedStep_MicroEngine_Kernel_FrameContext__"></a> RunFixedStep\(in FrameContext\)

Runs the four fixed phases, in order, for one step: [PreFixed](MicroEngine.Kernel.FramePhase.md), [FixedUpdate](MicroEngine.Kernel.FramePhase.md), [Physics](MicroEngine.Kernel.FramePhase.md), [PostFixed](MicroEngine.Kernel.FramePhase.md).

```csharp
public void RunFixedStep(in FrameContext step)
```

#### Parameters

`step` [FrameContext](MicroEngine.Kernel.FrameContext.md)

### <a id="MicroEngine_Kernel_Engine_TryGetCapability__1___0__"></a> TryGetCapability<T\>\(out T?\)

The capability declared as <code class="typeparamref">T</code>, if there is one.

```csharp
public bool TryGetCapability<T>(out T? capability) where T : class
```

#### Parameters

`capability` T?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Type Parameters

`T`

</div>

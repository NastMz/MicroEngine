# Class EngineBuilder

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Kernel](MicroEngine.Kernel.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Modules and the frame schedule](../manual/lifecycle.md#modules-and-the-frame-schedule)  

Composes an [Engine](MicroEngine.Kernel.Engine.md): modules are added in order, each registering services, frame systems and
capabilities; settings records are edited; [Build](MicroEngine.Kernel.EngineBuilder.md#MicroEngine_Kernel_EngineBuilder_Build) then fixes the schedule and the time policy.
Nothing is discovered by reflection.

```csharp
public sealed class EngineBuilder
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[EngineBuilder](MicroEngine.Kernel.EngineBuilder.md)

## Examples

```csharp
var builder = new EngineBuilder()
    .Configure<TimeSettings>(time => time with { FixedRate = 120 })
    .Add(new PhysicsModule());
builder.Schedule.Add(FramePhase.Update, "mygame.camera", frame => camera.Follow(frame.Blend));
Engine engine = builder.Build();
```

## Constructors

### <a id="MicroEngine_Kernel_EngineBuilder__ctor"></a> EngineBuilder\(\)

```csharp
public EngineBuilder()
```

## Properties

### <a id="MicroEngine_Kernel_EngineBuilder_Capabilities"></a> Capabilities

The optional features declared so far ([CapabilityRegistry](MicroEngine.Kernel.CapabilityRegistry.md)).

```csharp
public CapabilityRegistry Capabilities { get; }
```

#### Property Value

 [CapabilityRegistry](MicroEngine.Kernel.CapabilityRegistry.md)

### <a id="MicroEngine_Kernel_EngineBuilder_Lifecycle"></a> Lifecycle

The engine's events; modules may subscribe while they are configured.

```csharp
public EngineLifecycle Lifecycle { get; }
```

#### Property Value

 [EngineLifecycle](MicroEngine.Kernel.EngineLifecycle.md)

### <a id="MicroEngine_Kernel_EngineBuilder_Schedule"></a> Schedule

The frame systems, by phase ([FrameSchedule](MicroEngine.Kernel.FrameSchedule.md)).

```csharp
public FrameSchedule Schedule { get; }
```

#### Property Value

 [FrameSchedule](MicroEngine.Kernel.FrameSchedule.md)

### <a id="MicroEngine_Kernel_EngineBuilder_Services"></a> Services

The services modules register and read ([ServiceRegistry](MicroEngine.Kernel.ServiceRegistry.md)).

```csharp
public ServiceRegistry Services { get; }
```

#### Property Value

 [ServiceRegistry](MicroEngine.Kernel.ServiceRegistry.md)

## Methods

### <a id="MicroEngine_Kernel_EngineBuilder_Add__1___0_"></a> Add<TModule\>\(TModule\)

Adds <code class="paramref">module</code> and runs its [Configure](MicroEngine.Kernel.IEngineModule.md#MicroEngine_Kernel_IEngineModule_Configure_MicroEngine_Kernel_EngineBuilder_) now.

```csharp
public EngineBuilder Add<TModule>(TModule module) where TModule : IEngineModule
```

#### Parameters

`module` TModule

#### Returns

 [EngineBuilder](MicroEngine.Kernel.EngineBuilder.md)

This builder, to chain calls.

#### Type Parameters

`TModule` 

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

<code class="paramref">module</code> is null.

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

A module of the same type was already added, or the engine was built.

### <a id="MicroEngine_Kernel_EngineBuilder_Build"></a> Build\(\)

Builds the engine, once. Registers the defaults of what nobody registered ([TimeSettings](MicroEngine.Kernel.TimeSettings.md), a
[ConsoleLog](MicroEngine.Kernel.ConsoleLog.md) as [ILog](MicroEngine.Kernel.ILog.md), the lifecycle as [IEngineLifecycle](MicroEngine.Kernel.IEngineLifecycle.md)), resolves and
fixes the frame schedule and creates the loop's [FrameClock](MicroEngine.Kernel.FrameClock.md).

```csharp
public Engine Build()
```

#### Returns

 [Engine](MicroEngine.Kernel.Engine.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The engine was already built, or the schedule's order cannot be resolved (a cycle, or a constraint naming a missing system or one in another phase).

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

A [TimeSettings](MicroEngine.Kernel.TimeSettings.md) value is out of range.

### <a id="MicroEngine_Kernel_EngineBuilder_Configure__1_System_Func___0___0__"></a> Configure<TSettings\>\(Func<TSettings, TSettings\>\)

Edits the settings record <code class="typeparamref">TSettings</code>: <code class="paramref">edit</code> receives the current value
([Settings``1](MicroEngine.Kernel.EngineBuilder.md#MicroEngine_Kernel_EngineBuilder_Settings__1)) and returns the new one, which becomes the registered service. Settings are
values: the engine reads them once, when it is built.

```csharp
public EngineBuilder Configure<TSettings>(Func<TSettings, TSettings> edit) where TSettings : class, new()
```

#### Parameters

`edit` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TSettings, TSettings\>

#### Returns

 [EngineBuilder](MicroEngine.Kernel.EngineBuilder.md)

This builder, to chain calls.

#### Type Parameters

`TSettings` 

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

<code class="paramref">edit</code> is null.

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

<code class="paramref">edit</code> returned null, or the engine was built.

### <a id="MicroEngine_Kernel_EngineBuilder_HasModule__1"></a> HasModule<TModule\>\(\)

Whether a module of type <code class="typeparamref">TModule</code> was added.

```csharp
public bool HasModule<TModule>() where TModule : IEngineModule
```

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Type Parameters

`TModule` 

### <a id="MicroEngine_Kernel_EngineBuilder_Settings__1"></a> Settings<TSettings\>\(\)

The settings record <code class="typeparamref">TSettings</code> as configured so far: the registered service, or a new
instance with its defaults when none was registered.

```csharp
public TSettings Settings<TSettings>() where TSettings : class, new()
```

#### Returns

 TSettings

#### Type Parameters

`TSettings`

</div>

# Class FrameSchedule

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Kernel](MicroEngine.Kernel.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Modules and the frame schedule](../manual/lifecycle.md#modules-and-the-frame-schedule)  

The systems that run each frame: named callbacks placed in a [FramePhase](MicroEngine.Kernel.FramePhase.md), run in phase order. Within a
phase, systems run in the order they were added unless a system asks to run after or before another one of the same
phase; the order is resolved once, when the engine is built, and a cycle of such constraints is an error that names
the systems in it.

```csharp
public sealed class FrameSchedule
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[FrameSchedule](MicroEngine.Kernel.FrameSchedule.md)

## Remarks

Modules add systems while the engine is configured ([EngineBuilder](MicroEngine.Kernel.EngineBuilder.md)); once it is built the schedule is
fixed and adding, replacing or removing a system throws. Running it allocates nothing.

## Constructors

### <a id="MicroEngine_Kernel_FrameSchedule__ctor"></a> FrameSchedule\(\)

```csharp
public FrameSchedule()
```

## Properties

### <a id="MicroEngine_Kernel_FrameSchedule_Count"></a> Count

The number of systems scheduled.

```csharp
public int Count { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Kernel_FrameSchedule_IsFrozen"></a> IsFrozen

Whether the schedule is fixed (the engine was built): it can run but no longer change.

```csharp
public bool IsFrozen { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="MicroEngine_Kernel_FrameSchedule_Add_MicroEngine_Kernel_FramePhase_System_String_System_Action_MicroEngine_Kernel_FrameContext__System_String_System_String_"></a> Add\(FramePhase, string, Action<FrameContext\>, string?, string?\)

Schedules <code class="paramref">run</code> in <code class="paramref">phase</code> under <code class="paramref">name</code>, optionally after or
before another system of the same phase.

```csharp
public FrameSchedule Add(FramePhase phase, string name, Action<FrameContext> run, string? after = null, string? before = null)
```

#### Parameters

`phase` [FramePhase](MicroEngine.Kernel.FramePhase.md)

The phase it runs in.

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

A name unique in the whole schedule, such as "engine.scenes" or "mygame.camera".

`run` [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[FrameContext](MicroEngine.Kernel.FrameContext.md)\>

The system; it receives the frame's [FrameContext](MicroEngine.Kernel.FrameContext.md).

`after` [string](https://learn.microsoft.com/dotnet/api/system.string)?

The name of a system of the same phase this one must run after, or null.

`before` [string](https://learn.microsoft.com/dotnet/api/system.string)?

The name of a system of the same phase this one must run before, or null.

#### Returns

 [FrameSchedule](MicroEngine.Kernel.FrameSchedule.md)

This schedule, to chain calls.

#### Remarks

Constraints naming a system that does not exist, or one in another phase, are reported when the engine is built.

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

<code class="paramref">name</code> is empty, or a system with that name is already scheduled.

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

<code class="paramref">run</code> is null.

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">phase</code> is not a [FramePhase](MicroEngine.Kernel.FramePhase.md).

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The schedule is frozen.

### <a id="MicroEngine_Kernel_FrameSchedule_Contains_System_String_"></a> Contains\(string\)

Whether a system named <code class="paramref">name</code> is scheduled.

```csharp
public bool Contains(string name)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Kernel_FrameSchedule_Freeze"></a> Freeze\(\)

Resolves the order and fixes the schedule; the engine does this when it is built.

```csharp
public void Freeze()
```

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The order cannot be resolved (see [Order](MicroEngine.Kernel.FrameSchedule.md#MicroEngine_Kernel_FrameSchedule_Order_MicroEngine_Kernel_FramePhase_)).

### <a id="MicroEngine_Kernel_FrameSchedule_IsFixed_MicroEngine_Kernel_FramePhase_"></a> IsFixed\(FramePhase\)

Whether <code class="paramref">phase</code> runs once per fixed step ([PreFixed](MicroEngine.Kernel.FramePhase.md) to [PostFixed](MicroEngine.Kernel.FramePhase.md)) rather than once per frame.

```csharp
public static bool IsFixed(FramePhase phase)
```

#### Parameters

`phase` [FramePhase](MicroEngine.Kernel.FramePhase.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Kernel_FrameSchedule_Order_MicroEngine_Kernel_FramePhase_"></a> Order\(FramePhase\)

The names of the systems of <code class="paramref">phase</code> in the order they run, resolving the order if needed.

```csharp
public IReadOnlyList<string> Order(FramePhase phase)
```

#### Parameters

`phase` [FramePhase](MicroEngine.Kernel.FramePhase.md)

#### Returns

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

A constraint names a missing system or one in another phase, or the constraints form a cycle.

### <a id="MicroEngine_Kernel_FrameSchedule_PhaseOf_System_String_"></a> PhaseOf\(string\)

The phase of the system <code class="paramref">name</code>, or null when there is none.

```csharp
public FramePhase? PhaseOf(string name)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [FramePhase](MicroEngine.Kernel.FramePhase.md)?

### <a id="MicroEngine_Kernel_FrameSchedule_Remove_System_String_"></a> Remove\(string\)

Removes the system <code class="paramref">name</code>; false when there was none. Constraints of other systems that name it then fail when the engine is built.

```csharp
public bool Remove(string name)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The schedule is frozen.

### <a id="MicroEngine_Kernel_FrameSchedule_Replace_System_String_System_Action_MicroEngine_Kernel_FrameContext__"></a> Replace\(string, Action<FrameContext\>\)

Replaces what the system <code class="paramref">name</code> runs, keeping its phase and order: the way to substitute a built-in system.

```csharp
public FrameSchedule Replace(string name, Action<FrameContext> run)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`run` [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[FrameContext](MicroEngine.Kernel.FrameContext.md)\>

#### Returns

 [FrameSchedule](MicroEngine.Kernel.FrameSchedule.md)

#### Exceptions

 [KeyNotFoundException](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception)

No system is named <code class="paramref">name</code>.

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

<code class="paramref">run</code> is null.

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The schedule is frozen.

### <a id="MicroEngine_Kernel_FrameSchedule_Run_MicroEngine_Kernel_FramePhase_MicroEngine_Kernel_FrameContext__"></a> Run\(FramePhase, in FrameContext\)

Runs the systems of <code class="paramref">phase</code>, in order, with <code class="paramref">context</code>.

```csharp
public void Run(FramePhase phase, in FrameContext context)
```

#### Parameters

`phase` [FramePhase](MicroEngine.Kernel.FramePhase.md)

`context` [FrameContext](MicroEngine.Kernel.FrameContext.md)

#### Remarks

An exception thrown by a system stops the phase and reaches the caller.

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The order cannot be resolved (see [Order](MicroEngine.Kernel.FrameSchedule.md#MicroEngine_Kernel_FrameSchedule_Order_MicroEngine_Kernel_FramePhase_)).

</div>

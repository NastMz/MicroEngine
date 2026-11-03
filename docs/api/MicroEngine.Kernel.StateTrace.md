# Class StateTrace

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Kernel](MicroEngine.Kernel.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Modules and the frame schedule](../manual/lifecycle.md#modules-and-the-frame-schedule)  

A run's per-step state hashes: the loop records one [StepHash](MicroEngine.Kernel.StepHash.md) after every fixed step when a run is given
one (<code>RunOptions.StateTrace</code>). Two runs of the same build, script and seed must record the same trace;
[FirstDivergence](MicroEngine.Kernel.StateTrace.md#MicroEngine_Kernel_StateTrace_FirstDivergence_MicroEngine_Kernel_StateTrace_MicroEngine_Kernel_StateTrace_) names the first update and section where two differ.

```csharp
public class StateTrace
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[StateTrace](MicroEngine.Kernel.StateTrace.md)

## Remarks

The trace keeps its steps in memory; override [Record](MicroEngine.Kernel.StateTrace.md#MicroEngine_Kernel_StateTrace_Record_MicroEngine_Kernel_StepHash_) to send them elsewhere (a file, as they come).
[ToText](MicroEngine.Kernel.StateTrace.md#MicroEngine_Kernel_StateTrace_ToText) and [Parse](MicroEngine.Kernel.StateTrace.md#MicroEngine_Kernel_StateTrace_Parse_System_String_) give one line per step:
<code>update TAB hash [TAB section=hash]...</code>, hashes as 16 hexadecimal digits.

## Constructors

### <a id="MicroEngine_Kernel_StateTrace__ctor"></a> StateTrace\(\)

```csharp
public StateTrace()
```

## Properties

### <a id="MicroEngine_Kernel_StateTrace_Sections"></a> Sections

Whether each step also records the hash of every section (false by default: the whole hash only).

```csharp
public bool Sections { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Kernel_StateTrace_Steps"></a> Steps

The steps recorded so far, in order.

```csharp
public IReadOnlyList<StepHash> Steps { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[StepHash](MicroEngine.Kernel.StepHash.md)\>

## Methods

### <a id="MicroEngine_Kernel_StateTrace_FirstDivergence_MicroEngine_Kernel_StateTrace_MicroEngine_Kernel_StateTrace_"></a> FirstDivergence\(StateTrace, StateTrace\)

The first step and section where <code class="paramref">first</code> and <code class="paramref">second</code> differ, or null when they are identical.

```csharp
public static StateDivergence? FirstDivergence(StateTrace first, StateTrace second)
```

#### Parameters

`first` [StateTrace](MicroEngine.Kernel.StateTrace.md)

`second` [StateTrace](MicroEngine.Kernel.StateTrace.md)

#### Returns

 [StateDivergence](MicroEngine.Kernel.StateDivergence.md)?

### <a id="MicroEngine_Kernel_StateTrace_FirstDivergence_System_Collections_Generic_IReadOnlyList_MicroEngine_Kernel_StepHash__System_Collections_Generic_IReadOnlyList_MicroEngine_Kernel_StepHash__"></a> FirstDivergence\(IReadOnlyList<StepHash\>, IReadOnlyList<StepHash\>\)

The first step and section where two lists of steps differ, or null when they are identical. Steps are compared by
position: their update numbers and whole hashes; then, when both carry section hashes, the first section whose name
or hash differs is named.

```csharp
public static StateDivergence? FirstDivergence(IReadOnlyList<StepHash> first, IReadOnlyList<StepHash> second)
```

#### Parameters

`first` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[StepHash](MicroEngine.Kernel.StepHash.md)\>

`second` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[StepHash](MicroEngine.Kernel.StepHash.md)\>

#### Returns

 [StateDivergence](MicroEngine.Kernel.StateDivergence.md)?

### <a id="MicroEngine_Kernel_StateTrace_Format_MicroEngine_Kernel_StepHash_"></a> Format\(StepHash\)

One step as a line of text, without the line break: <code>update TAB hash [TAB section=hash]...</code>.

```csharp
public static string Format(StepHash step)
```

#### Parameters

`step` [StepHash](MicroEngine.Kernel.StepHash.md)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Kernel_StateTrace_Parse_System_String_"></a> Parse\(string\)

Reads a trace written by [Write](MicroEngine.Kernel.StateTrace.md#MicroEngine_Kernel_StateTrace_Write_System_IO_TextWriter_); it records sections when any step has them.

```csharp
public static StateTrace Parse(string text)
```

#### Parameters

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [StateTrace](MicroEngine.Kernel.StateTrace.md)

#### Exceptions

 [FormatException](https://learn.microsoft.com/dotnet/api/system.formatexception)

A line is not a step.

### <a id="MicroEngine_Kernel_StateTrace_Record_MicroEngine_Kernel_StepHash_"></a> Record\(StepHash\)

Receives the hash of a step; the default keeps it in [Steps](MicroEngine.Kernel.StateTrace.md#MicroEngine_Kernel_StateTrace_Steps).

```csharp
public virtual void Record(StepHash step)
```

#### Parameters

`step` [StepHash](MicroEngine.Kernel.StepHash.md)

### <a id="MicroEngine_Kernel_StateTrace_ToText"></a> ToText\(\)

The steps as text, one line per step.

```csharp
public string ToText()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Kernel_StateTrace_Write_System_IO_TextWriter_"></a> Write\(TextWriter\)

Writes the steps as text, one line per step (see the remarks of [StateTrace](MicroEngine.Kernel.StateTrace.md)).

```csharp
public void Write(TextWriter writer)
```

#### Parameters

`writer` [TextWriter](https://learn.microsoft.com/dotnet/api/system.io.textwriter)

</div>

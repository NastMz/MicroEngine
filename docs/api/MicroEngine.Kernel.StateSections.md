# Class StateSections

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Kernel](MicroEngine.Kernel.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Modules and the frame schedule](../manual/lifecycle.md#modules-and-the-frame-schedule)  

The parts of the per-step state hash that modules own: each is a named section hashed after the engine's own
sections and before the scenes, in the order they were added. A module that holds simulation state outside scenes
(progress, a shared world) adds one from its <code>Configure</code>, so a deterministic run covers it without the game
hashing it by hand.

```csharp
public sealed class StateSections
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[StateSections](MicroEngine.Kernel.StateSections.md)

## Examples

```csharp
public void Configure(EngineBuilder builder) =>
    builder.StateSections.Add("mygame.economy", hash => hash.Add(_gold));
```

## Constructors

### <a id="MicroEngine_Kernel_StateSections__ctor"></a> StateSections\(\)

```csharp
public StateSections()
```

## Properties

### <a id="MicroEngine_Kernel_StateSections_Count"></a> Count

How many sections were added.

```csharp
public int Count { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Kernel_StateSections_Add_System_String_System_Action_MicroEngine_Kernel_StateHasher__"></a> Add\(string, Action<StateHasher\>\)

Adds a section named <code class="paramref">name</code> whose state <code class="paramref">hash</code> writes.

```csharp
public void Add(string name, Action<StateHasher> hash)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`hash` [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[StateHasher](MicroEngine.Kernel.StateHasher.md)\>

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

<code class="paramref">name</code> is empty.

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

A section of that name was already added.

### <a id="MicroEngine_Kernel_StateSections_Add_System_String_MicroEngine_Kernel_IStateHashed_"></a> Add\(string, IStateHashed\)

Adds a section named <code class="paramref">name</code> holding the state of <code class="paramref">state</code>.

```csharp
public void Add(string name, IStateHashed state)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`state` [IStateHashed](MicroEngine.Kernel.IStateHashed.md)

### <a id="MicroEngine_Kernel_StateSections_HashAll_MicroEngine_Kernel_StateHasher_"></a> HashAll\(StateHasher\)

Writes every section to <code class="paramref">hash</code>, each under its name, in the order they were added.

```csharp
public void HashAll(StateHasher hash)
```

#### Parameters

`hash` [StateHasher](MicroEngine.Kernel.StateHasher.md)

</div>

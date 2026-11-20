# Class EcsSystems

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Ecs](MicroEngine.Ecs.md)  
Assembly: MicroEngine.Ecs.dll  
Guide: [Entity Component Systems](../guide/entity-component-systems.md)  

The systems of one place in a store's step, in the order they run ([Systems](MicroEngine.Ecs.EcsStore.md#MicroEngine_Ecs_EcsStore_Systems),
[LateSystems](MicroEngine.Ecs.EcsStore.md#MicroEngine_Ecs_EcsStore_LateSystems)). The order is the order of the <code>Add</code> calls: it is written once, where the
scene is set up, and read there.

```csharp
public sealed class EcsSystems
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[EcsSystems](MicroEngine.Ecs.EcsSystems.md)

## Properties

### <a id="MicroEngine_Ecs_EcsSystems_Count"></a> Count

How many systems the list has.

```csharp
public int Count { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Ecs_EcsSystems_Item_System_String_"></a> this\[string\]

The list's system named <code class="paramref">name</code>.

```csharp
public EcsSystem this[string name] { get; }
```

#### Property Value

 [EcsSystem](MicroEngine.Ecs.EcsSystem.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The list has no such system.

## Methods

### <a id="MicroEngine_Ecs_EcsSystems_Add__1___0_"></a> Add<T\>\(T\)

Adds <code class="paramref">system</code> after the ones already there and returns it.

```csharp
public T Add<T>(T system) where T : EcsSystem
```

#### Parameters

`system` T

#### Returns

 T

#### Type Parameters

`T` 

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The system is already in a list, or the list has a system of that name.

### <a id="MicroEngine_Ecs_EcsSystems_Add_System_String_System_Action_MicroEngine_Ecs_EcsStore_System_Single__"></a> Add\(string, Action<EcsStore, float\>\)

Adds a system that is one function, for a behaviour too small for a class, and returns it (to switch it off, or to remove it).

```csharp
public EcsSystem Add(string name, Action<EcsStore, float> update)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

Its name within the list.

`update` [Action](https://learn.microsoft.com/dotnet/api/system.action\-2)<[EcsStore](MicroEngine.Ecs.EcsStore.md), [float](https://learn.microsoft.com/dotnet/api/system.single)\>

Called once per step with the store and the step.

#### Returns

 [EcsSystem](MicroEngine.Ecs.EcsSystem.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The list has a system of that name.

### <a id="MicroEngine_Ecs_EcsSystems_Get__1"></a> Get<T\>\(\)

The list's system of type <code class="typeparamref">T</code> (that type or one derived from it).

```csharp
public T Get<T>() where T : EcsSystem
```

#### Returns

 T

#### Type Parameters

`T` 

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The list has no such system.

### <a id="MicroEngine_Ecs_EcsSystems_Remove_MicroEngine_Ecs_EcsSystem_"></a> Remove\(EcsSystem\)

Removes <code class="paramref">system</code>; false when the list does not have it.

```csharp
public bool Remove(EcsSystem system)
```

#### Parameters

`system` [EcsSystem](MicroEngine.Ecs.EcsSystem.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Ecs_EcsSystems_TryGet_System_String_MicroEngine_Ecs_EcsSystem__"></a> TryGet\(string, out EcsSystem?\)

Finds the list's system named <code class="paramref">name</code>.

```csharp
public bool TryGet(string name, out EcsSystem? system)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`system` [EcsSystem](MicroEngine.Ecs.EcsSystem.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

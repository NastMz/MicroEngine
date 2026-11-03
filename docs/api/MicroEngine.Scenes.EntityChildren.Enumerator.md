# Struct EntityChildren.Enumerator

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Scenes](MicroEngine.Scenes.md)  
Assembly: MicroEngine.Scene.dll  
Guide: [Scenes](../manual/scenes.md)  

Walks the children; allocates nothing.

```csharp
public struct EntityChildren.Enumerator : IEnumerator<EntityId>, IEnumerator, IDisposable
```

#### Implements

[IEnumerator<EntityId\>](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerator\-1), 
[IEnumerator](https://learn.microsoft.com/dotnet/api/system.collections.ienumerator), 
[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

## Properties

### <a id="MicroEngine_Scenes_EntityChildren_Enumerator_Current"></a> Current

The current child.

```csharp
public readonly EntityId Current { get; }
```

#### Property Value

 [EntityId](MicroEngine.Scenes.EntityId.md)

## Methods

### <a id="MicroEngine_Scenes_EntityChildren_Enumerator_Dispose"></a> Dispose\(\)

Nothing to release.

```csharp
public readonly void Dispose()
```

### <a id="MicroEngine_Scenes_EntityChildren_Enumerator_MoveNext"></a> MoveNext\(\)

Moves to the next live child.

```csharp
public bool MoveNext()
```

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

An entity changed parent since the walk started.

### <a id="MicroEngine_Scenes_EntityChildren_Enumerator_Reset"></a> Reset\(\)

Starts over.

```csharp
public void Reset()
```

</div>

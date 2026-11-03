# Struct Attachments<T\>.Enumerator

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Scenes](MicroEngine.Scenes.md)  
Assembly: MicroEngine.Scene.dll  
Guide: [Scenes](../manual/scenes.md)  

Walks the attachments; allocates nothing.

```csharp
public struct Attachments<T>.Enumerator : IEnumerator<T>, IEnumerator, IDisposable
```

#### Implements

[IEnumerator<T\>](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerator\-1), 
[IEnumerator](https://learn.microsoft.com/dotnet/api/system.collections.ienumerator), 
[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

## Properties

### <a id="MicroEngine_Scenes_Attachments_1_Enumerator_Current"></a> Current

The current attachment.

```csharp
public readonly T Current { get; }
```

#### Property Value

 T

### <a id="MicroEngine_Scenes_Attachments_1_Enumerator_Entity"></a> Entity

The entity of [Current](MicroEngine.Scenes.md).

```csharp
public readonly EntityId Entity { get; }
```

#### Property Value

 [EntityId](MicroEngine.Scenes.EntityId.md)

## Methods

### <a id="MicroEngine_Scenes_Attachments_1_Enumerator_Dispose"></a> Dispose\(\)

Nothing to release.

```csharp
public readonly void Dispose()
```

### <a id="MicroEngine_Scenes_Attachments_1_Enumerator_MoveNext"></a> MoveNext\(\)

Moves to the next attachment of a live entity.

```csharp
public bool MoveNext()
```

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_Attachments_1_Enumerator_Reset"></a> Reset\(\)

Starts over.

```csharp
public void Reset()
```

</div>

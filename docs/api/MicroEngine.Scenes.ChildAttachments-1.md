# Struct ChildAttachments<T\>

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Scenes](MicroEngine.Scenes.md)  
Assembly: MicroEngine.Scene.dll  
Guide: [Scenes](../manual/scenes.md)  

The attachments of type <code class="typeparamref">T</code> of an entity's live direct children, in parenting order
([Children``1](MicroEngine.Scenes.IWorld.md#MicroEngine_Scenes_IWorld_Children__1_MicroEngine_Scenes_EntityId_)).

```csharp
public readonly struct ChildAttachments<T> : IEnumerable<T>, IEnumerable where T : class
```

#### Type Parameters

`T` 

#### Implements

[IEnumerable<T\>](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1), 
[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.ienumerable)

## Methods

### <a id="MicroEngine_Scenes_ChildAttachments_1_GetEnumerator"></a> GetEnumerator\(\)

Starts a walk over the children the entity has now.

```csharp
public ChildAttachments<T>.Enumerator GetEnumerator()
```

#### Returns

 [ChildAttachments](MicroEngine.Scenes.ChildAttachments\-1.md)<T\>.[Enumerator](MicroEngine.Scenes.ChildAttachments\-1.Enumerator.md)

</div>

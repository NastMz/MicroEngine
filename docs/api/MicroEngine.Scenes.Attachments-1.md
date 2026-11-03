# Struct Attachments<T\>

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Scenes](MicroEngine.Scenes.md)  
Assembly: MicroEngine.Scene.dll  
Guide: [Scenes](../manual/scenes.md)  

The attachments of type <code class="typeparamref">T</code> of live entities, in attach order ([All``1](MicroEngine.Scenes.IWorld.md#MicroEngine_Scenes_IWorld_All__1)).

```csharp
public readonly struct Attachments<T> : IEnumerable<T>, IEnumerable where T : class
```

#### Type Parameters

`T` 

#### Implements

[IEnumerable<T\>](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1), 
[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.ienumerable)

## Methods

### <a id="MicroEngine_Scenes_Attachments_1_GetEnumerator"></a> GetEnumerator\(\)

Starts a walk over the attachments that exist now.

```csharp
public Attachments<T>.Enumerator GetEnumerator()
```

#### Returns

 [Attachments](MicroEngine.Scenes.Attachments\-1.md)<T\>.[Enumerator](MicroEngine.Scenes.Attachments\-1.Enumerator.md)

</div>

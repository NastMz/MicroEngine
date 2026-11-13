# Class TiledObjectLayer

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Tiled](MicroEngine.Tiled.md)  
Assembly: MicroEngine.Tiled.dll  
Guide: [Maps from Tiled](../manual/world.md#maps-from-tiled)  

A layer of objects: placements, areas and points with their data.

```csharp
public sealed class TiledObjectLayer : TiledLayer
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[TiledLayer](MicroEngine.Tiled.TiledLayer.md) ← 
[TiledObjectLayer](MicroEngine.Tiled.TiledObjectLayer.md)

#### Inherited Members

[TiledLayer.ToString\(\)](MicroEngine.Tiled.TiledLayer.md\#MicroEngine\_Tiled\_TiledLayer\_ToString), 
[TiledLayer.Id](MicroEngine.Tiled.TiledLayer.md\#MicroEngine\_Tiled\_TiledLayer\_Id), 
[TiledLayer.Name](MicroEngine.Tiled.TiledLayer.md\#MicroEngine\_Tiled\_TiledLayer\_Name), 
[TiledLayer.Class](MicroEngine.Tiled.TiledLayer.md\#MicroEngine\_Tiled\_TiledLayer\_Class), 
[TiledLayer.Visible](MicroEngine.Tiled.TiledLayer.md\#MicroEngine\_Tiled\_TiledLayer\_Visible), 
[TiledLayer.Opacity](MicroEngine.Tiled.TiledLayer.md\#MicroEngine\_Tiled\_TiledLayer\_Opacity), 
[TiledLayer.Tint](MicroEngine.Tiled.TiledLayer.md\#MicroEngine\_Tiled\_TiledLayer\_Tint), 
[TiledLayer.Offset](MicroEngine.Tiled.TiledLayer.md\#MicroEngine\_Tiled\_TiledLayer\_Offset), 
[TiledLayer.Parallax](MicroEngine.Tiled.TiledLayer.md\#MicroEngine\_Tiled\_TiledLayer\_Parallax), 
[TiledLayer.Properties](MicroEngine.Tiled.TiledLayer.md\#MicroEngine\_Tiled\_TiledLayer\_Properties), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring), 
[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode)

## Properties

### <a id="MicroEngine_Tiled_TiledObjectLayer_Color"></a> Color

The colour Tiled shows the layer's objects in, if set.

```csharp
public Color? Color { get; }
```

#### Property Value

 [Color](MicroEngine.Color.md)?

### <a id="MicroEngine_Tiled_TiledObjectLayer_Objects"></a> Objects

The objects in the order Tiled stores them.

```csharp
public IReadOnlyList<TiledObject> Objects { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[TiledObject](MicroEngine.Tiled.TiledObject.md)\>

</div>

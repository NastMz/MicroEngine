# Class TiledImageLayer

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Tiled](MicroEngine.Tiled.md)  
Assembly: MicroEngine.Tiled.dll  
Guide: [Maps from Tiled](../manual/world.md#maps-from-tiled)  

A layer showing one image.

```csharp
public sealed class TiledImageLayer : TiledLayer
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[TiledLayer](MicroEngine.Tiled.TiledLayer.md) ← 
[TiledImageLayer](MicroEngine.Tiled.TiledImageLayer.md)

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

### <a id="MicroEngine_Tiled_TiledImageLayer_Image"></a> Image

The image's content path, or null when the layer has none.

```csharp
public string? Image { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Tiled_TiledImageLayer_RepeatX"></a> RepeatX

Whether the image repeats horizontally.

```csharp
public bool RepeatX { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Tiled_TiledImageLayer_RepeatY"></a> RepeatY

Whether the image repeats vertically.

```csharp
public bool RepeatY { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

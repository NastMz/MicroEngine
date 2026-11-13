# Class TiledLayer

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Tiled](MicroEngine.Tiled.md)  
Assembly: MicroEngine.Tiled.dll  
Guide: [Maps from Tiled](../manual/world.md#maps-from-tiled)  

A layer of a [TiledMap](MicroEngine.Tiled.TiledMap.md): what every kind of layer has.

```csharp
public abstract class TiledLayer
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[TiledLayer](MicroEngine.Tiled.TiledLayer.md)

#### Derived

[TiledGroupLayer](MicroEngine.Tiled.TiledGroupLayer.md), 
[TiledImageLayer](MicroEngine.Tiled.TiledImageLayer.md), 
[TiledObjectLayer](MicroEngine.Tiled.TiledObjectLayer.md), 
[TiledTileLayer](MicroEngine.Tiled.TiledTileLayer.md)

## Properties

### <a id="MicroEngine_Tiled_TiledLayer_Class"></a> Class

The layer's class; empty when unset.

```csharp
public string Class { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Tiled_TiledLayer_Id"></a> Id

The layer's id, unique in the map.

```csharp
public int Id { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Tiled_TiledLayer_Name"></a> Name

The layer's name.

```csharp
public string Name { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Tiled_TiledLayer_Offset"></a> Offset

Where the layer is drawn relative to the map, in pixels, a group's offset included.

```csharp
public Vector2 Offset { get; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Tiled_TiledLayer_Opacity"></a> Opacity

Opacity from 0 to 1, a group's included.

```csharp
public float Opacity { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Tiled_TiledLayer_Parallax"></a> Parallax

The parallax factor, a group's included ((1, 1) moves with the map). The engine does not apply it when drawing.

```csharp
public Vector2 Parallax { get; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Tiled_TiledLayer_Properties"></a> Properties

The layer's custom properties.

```csharp
public TiledProperties Properties { get; }
```

#### Property Value

 [TiledProperties](MicroEngine.Tiled.TiledProperties.md)

### <a id="MicroEngine_Tiled_TiledLayer_Tint"></a> Tint

The tint colour, a group's included (white when unset).

```csharp
public Color Tint { get; }
```

#### Property Value

 [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Tiled_TiledLayer_Visible"></a> Visible

Whether it is shown; a hidden group hides its layers.

```csharp
public bool Visible { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="MicroEngine_Tiled_TiledLayer_ToString"></a> ToString\(\)

The layer's name and type.

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

</div>

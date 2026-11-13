# Enum TiledShape

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Tiled](MicroEngine.Tiled.md)  
Assembly: MicroEngine.Tiled.dll  
Guide: [Maps from Tiled](../manual/world.md#maps-from-tiled)  

The shape of a [TiledObject](MicroEngine.Tiled.TiledObject.md).

```csharp
public enum TiledShape
```

## Fields

`Ellipse = 1` 

An ellipse inside the rectangle.



`Point = 2` 

A point: the size is zero.



`Polygon = 3` 

A closed polygon through [Points](MicroEngine.Tiled.TiledObject.md#MicroEngine_Tiled_TiledObject_Points).



`Polyline = 4` 

An open line through [Points](MicroEngine.Tiled.TiledObject.md#MicroEngine_Tiled_TiledObject_Points).



`Rectangle = 0` 

A rectangle of [Size](MicroEngine.Tiled.TiledObject.md#MicroEngine_Tiled_TiledObject_Size) from [Position](MicroEngine.Tiled.TiledObject.md#MicroEngine_Tiled_TiledObject_Position).



`Text = 5` 

Text ([Text](MicroEngine.Tiled.TiledObject.md#MicroEngine_Tiled_TiledObject_Text)) in the rectangle.



`Tile = 6` 

A tile ([Gid](MicroEngine.Tiled.TiledObject.md#MicroEngine_Tiled_TiledObject_Gid)) stretched to the rectangle.

</div>

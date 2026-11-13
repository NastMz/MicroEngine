# Enum TiledPropertyType

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Tiled](MicroEngine.Tiled.md)  
Assembly: MicroEngine.Tiled.dll  
Guide: [Maps from Tiled](../manual/world.md#maps-from-tiled)  

The type of a custom property, as Tiled names it.

```csharp
public enum TiledPropertyType
```

## Fields

`Bool = 3` 

True or false: the value is a `Boolean`.



`Class = 8` 

A custom class: the value is a [TiledProperties](MicroEngine.Tiled.TiledProperties.md) of its members.



`Color = 4` 

A colour: the value is a [Color](MicroEngine.Color.md).



`Enum = 7` 

A custom enum: the value is a `String` (several flags are joined with commas).



`File = 5` 

A file: the value is its content path (a `String`, relative to the content root).



`Float = 2` 

A number: the value is a `Single`.



`Int = 1` 

A whole number: the value is an `Int32`.



`Object = 6` 

A reference to another object: the value is its id (an `Int32`, 0 for none).



`String = 0` 

Text: the value is a `String`.

</div>

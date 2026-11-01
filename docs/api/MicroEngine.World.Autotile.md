# Class Autotile

Namespace: [MicroEngine.World](MicroEngine.World.md)  
Assembly: MicroEngine.dll  
Guide: [Tile maps and collision](../manual/world.md)  

Helpers for picking tile sprites from their neighbours.

```csharp
public static class Autotile
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Autotile](MicroEngine.World.Autotile.md)

## Methods

### <a id="MicroEngine_World_Autotile_Mask4_MicroEngine_World_TileLayer_System_Int32_System_Int32_System_Func_System_Int32_System_Boolean__System_Boolean_"></a> Mask4\(TileLayer, int, int, Func<int, bool\>, bool\)

4-bit mask: 1 = up, 2 = right, 4 = down, 8 = left neighbour matches.

```csharp
public static int Mask4(TileLayer layer, int x, int y, Func<int, bool> same, bool outsideMatches = true)
```

#### Parameters

`layer` [TileLayer](MicroEngine.World.TileLayer.md)

`x` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`y` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`same` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[int](https://learn.microsoft.com/dotnet/api/system.int32), [bool](https://learn.microsoft.com/dotnet/api/system.boolean)\>

`outsideMatches` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

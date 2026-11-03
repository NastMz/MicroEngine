# Class TileLayer

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.World](MicroEngine.World.md)  
Assembly: MicroEngine.Physics.Tiles.dll  
Guide: [Tile maps and collision](../manual/world.md)  

A rectangular grid of integer tile ids (0 conventionally means empty).

```csharp
public sealed class TileLayer
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[TileLayer](MicroEngine.World.TileLayer.md)

## Constructors

### <a id="MicroEngine_World_TileLayer__ctor_System_Int32_System_Int32_"></a> TileLayer\(int, int\)

Creates a layer filled with 0.

```csharp
public TileLayer(int width, int height)
```

#### Parameters

`width` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`height` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">width</code> or <code class="paramref">height</code> is not positive.

## Properties

### <a id="MicroEngine_World_TileLayer_Height"></a> Height

Height in tiles.

```csharp
public int Height { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_World_TileLayer_Width"></a> Width

Width in tiles.

```csharp
public int Width { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_World_TileLayer_Item_System_Int32_System_Int32_"></a> this\[int, int\]

The tile id at (<code class="paramref">x</code>, <code class="paramref">y</code>), in tiles. Reading outside returns 0; writing outside does nothing.

```csharp
public int this[int x, int y] { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_World_TileLayer_Find_System_Collections_Generic_IReadOnlyList_System_String__System_Char_"></a> Find\(IReadOnlyList<string\>, char\)

Finds every cell whose source character is <code class="paramref">marker</code> (for spawning entities).

```csharp
public static IEnumerable<(int X, int Y)> Find(IReadOnlyList<string> rows, char marker)
```

#### Parameters

`rows` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

`marker` [char](https://learn.microsoft.com/dotnet/api/system.char)

#### Returns

 [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<\([int](https://learn.microsoft.com/dotnet/api/system.int32) [X](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32\-.x), [int](https://learn.microsoft.com/dotnet/api/system.int32) [Y](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32\-.y)\)\>

### <a id="MicroEngine_World_TileLayer_Get_System_Int32_System_Int32_System_Int32_"></a> Get\(int, int, int\)

Out-of-bounds reads return <code class="paramref">outside</code> so edges can act as walls.

```csharp
public int Get(int x, int y, int outside = 0)
```

#### Parameters

`x` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`y` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`outside` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_World_TileLayer_InBounds_System_Int32_System_Int32_"></a> InBounds\(int, int\)

Whether tile (<code class="paramref">x</code>, <code class="paramref">y</code>) is inside the layer.

```csharp
public bool InBounds(int x, int y)
```

#### Parameters

`x` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`y` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_World_TileLayer_Parse_System_Collections_Generic_IReadOnlyList_System_String__System_Collections_Generic_IReadOnlyDictionary_System_Char_System_Int32__"></a> Parse\(IReadOnlyList<string\>, IReadOnlyDictionary<char, int\>\)

Builds a layer from text rows. Each character is mapped through <code class="paramref">legend</code>;
characters not in the legend become 0. Rows may have different lengths.

```csharp
public static TileLayer Parse(IReadOnlyList<string> rows, IReadOnlyDictionary<char, int> legend)
```

#### Parameters

`rows` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

`legend` [IReadOnlyDictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlydictionary\-2)<[char](https://learn.microsoft.com/dotnet/api/system.char), [int](https://learn.microsoft.com/dotnet/api/system.int32)\>

#### Returns

 [TileLayer](MicroEngine.World.TileLayer.md)

</div>

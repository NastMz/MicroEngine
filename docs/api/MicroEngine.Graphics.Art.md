# Class Art

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.dll  
Guide: [Graphics](../manual/rendering.md)  

Deterministic helpers for procedural pixel art: hash noise, ordered dithering and palette
ramps. Every sample game builds its sprites and tiles from these at load time.

```csharp
public static class Art
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Art](MicroEngine.Graphics.Art.md)

## Methods

### <a id="MicroEngine_Graphics_Art_Dither_System_Int32_System_Int32_System_Single_"></a> Dither\(int, int, float\)

Ordered (Bayer 4x4) dither: true when <code class="paramref">level</code> (0..1) exceeds the threshold at x,y.

```csharp
public static bool Dither(int x, int y, float level)
```

#### Parameters

`x` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`y` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`level` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Art_Hash_System_Int32_System_Int32_System_Int32_"></a> Hash\(int, int, int\)

Stable pseudo-random value in 0..1 for integer coordinates and a seed.

```csharp
public static float Hash(int x, int y, int seed = 0)
```

#### Parameters

`x` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`y` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`seed` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_Art_Palette_System_String___"></a> Palette\(params string\[\]\)

Builds a palette dictionary from "char=#hex" pairs, e.g. Palette("a=#000000", "b=#ffffff").

```csharp
public static Dictionary<char, Color> Palette(params string[] entries)
```

#### Parameters

`entries` [string](https://learn.microsoft.com/dotnet/api/system.string)\[\]

#### Returns

 [Dictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary\-2)<[char](https://learn.microsoft.com/dotnet/api/system.char), [Color](MicroEngine.Color.md)\>

### <a id="MicroEngine_Graphics_Art_Ramp_System_Collections_Generic_IReadOnlyList_MicroEngine_Color__System_Single_System_Int32_System_Int32_"></a> Ramp\(IReadOnlyList<Color\>, float, int, int\)

Picks a ramp entry for a 0..1 value with dithered transitions between steps.

```csharp
public static Color Ramp(IReadOnlyList<Color> ramp, float value, int x, int y)
```

#### Parameters

`ramp` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[Color](MicroEngine.Color.md)\>

`value` [float](https://learn.microsoft.com/dotnet/api/system.single)

`x` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`y` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Graphics_Art_ValueNoise_System_Single_System_Single_System_Single_System_Int32_"></a> ValueNoise\(float, float, float, int\)

Smooth value noise in 0..1 with the given cell size.

```csharp
public static float ValueNoise(float x, float y, float cell, int seed = 0)
```

#### Parameters

`x` [float](https://learn.microsoft.com/dotnet/api/system.single)

`y` [float](https://learn.microsoft.com/dotnet/api/system.single)

`cell` [float](https://learn.microsoft.com/dotnet/api/system.single)

`seed` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

</div>

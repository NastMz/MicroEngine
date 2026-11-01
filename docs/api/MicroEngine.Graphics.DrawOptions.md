# Struct DrawOptions

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.dll  
Guide: [Graphics](../manual/rendering.md)  

Per-draw sprite options. Origin is in source pixels and is also the rotation pivot.

```csharp
public readonly struct DrawOptions : IEquatable<DrawOptions>
```

#### Implements

[IEquatable<DrawOptions\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_DrawOptions__ctor_System_Boolean_System_Boolean_System_Single_System_Numerics_Vector2_System_Nullable_System_Numerics_Vector2__"></a> DrawOptions\(bool, bool, float, Vector2, Vector2?\)

Per-draw sprite options. Origin is in source pixels and is also the rotation pivot.

```csharp
public DrawOptions(bool FlipX = false, bool FlipY = false, float Rotation = 0, Vector2 Origin = default, Vector2? Scale = null)
```

#### Parameters

`FlipX` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Mirrors the sprite horizontally.

`FlipY` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Mirrors the sprite vertically.

`Rotation` [float](https://learn.microsoft.com/dotnet/api/system.single)

Clockwise rotation in radians around <code class="paramref">Origin</code>.

`Origin` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

Rotation and scale pivot, in source pixels from the top-left of the source rectangle.

`Scale` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)?

Scale factors per axis; null draws at 1:1.

## Fields

### <a id="MicroEngine_Graphics_DrawOptions_None"></a> None

No flip, rotation or scale: the same as <code>default</code>.

```csharp
public static readonly DrawOptions None
```

#### Field Value

 [DrawOptions](MicroEngine.Graphics.DrawOptions.md)

## Properties

### <a id="MicroEngine_Graphics_DrawOptions_FlipX"></a> FlipX

Mirrors the sprite horizontally.

```csharp
public bool FlipX { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_DrawOptions_FlipY"></a> FlipY

Mirrors the sprite vertically.

```csharp
public bool FlipY { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_DrawOptions_Origin"></a> Origin

Rotation and scale pivot, in source pixels from the top-left of the source rectangle.

```csharp
public Vector2 Origin { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Graphics_DrawOptions_Rotation"></a> Rotation

Clockwise rotation in radians around <code class="paramref">Origin</code>.

```csharp
public float Rotation { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_DrawOptions_Scale"></a> Scale

Scale factors per axis; null draws at 1:1.

```csharp
public Vector2? Scale { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)?

## Methods

### <a id="MicroEngine_Graphics_DrawOptions_Deconstruct_System_Boolean__System_Boolean__System_Single__System_Numerics_Vector2__System_Nullable_System_Numerics_Vector2___"></a> Deconstruct\(out bool, out bool, out float, out Vector2, out Vector2?\)

```csharp
public void Deconstruct(out bool FlipX, out bool FlipY, out float Rotation, out Vector2 Origin, out Vector2? Scale)
```

#### Parameters

`FlipX` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`FlipY` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`Rotation` [float](https://learn.microsoft.com/dotnet/api/system.single)

`Origin` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`Scale` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)?

### <a id="MicroEngine_Graphics_DrawOptions_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_DrawOptions_Equals_MicroEngine_Graphics_DrawOptions_"></a> Equals\(DrawOptions\)

```csharp
public bool Equals(DrawOptions other)
```

#### Parameters

`other` [DrawOptions](MicroEngine.Graphics.DrawOptions.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_DrawOptions_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_DrawOptions_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_DrawOptions_op_Equality_MicroEngine_Graphics_DrawOptions_MicroEngine_Graphics_DrawOptions_"></a> operator ==\(DrawOptions, DrawOptions\)

```csharp
public static bool operator ==(DrawOptions left, DrawOptions right)
```

#### Parameters

`left` [DrawOptions](MicroEngine.Graphics.DrawOptions.md)

`right` [DrawOptions](MicroEngine.Graphics.DrawOptions.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_DrawOptions_op_Inequality_MicroEngine_Graphics_DrawOptions_MicroEngine_Graphics_DrawOptions_"></a> operator \!=\(DrawOptions, DrawOptions\)

```csharp
public static bool operator !=(DrawOptions left, DrawOptions right)
```

#### Parameters

`left` [DrawOptions](MicroEngine.Graphics.DrawOptions.md)

`right` [DrawOptions](MicroEngine.Graphics.DrawOptions.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

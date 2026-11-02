# Class Sprite

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.dll  
Guide: [Graphics](../manual/rendering.md)  

A region of a texture with a pivot (origin) in source pixels.

```csharp
public sealed record Sprite : IEquatable<Sprite>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Sprite](MicroEngine.Graphics.Sprite.md)

#### Implements

[IEquatable<Sprite\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Remarks

Sprites usually come from a [SpriteAtlas](MicroEngine.Graphics.SpriteAtlas.md), which owns their texture.

## Constructors

### <a id="MicroEngine_Graphics_Sprite__ctor_MicroEngine_Graphics_Texture_MicroEngine_RectF_System_Numerics_Vector2_"></a> Sprite\(Texture, RectF, Vector2\)

A region of a texture with a pivot (origin) in source pixels.

```csharp
public Sprite(Texture Texture, RectF Source, Vector2 Origin)
```

#### Parameters

`Texture` [Texture](MicroEngine.Graphics.Texture.md)

The texture that holds the pixels; owned by whoever created it (usually an atlas).

`Source` RectF

The sprite's rectangle in the texture, in texture pixels.

`Origin` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

The anchor in source pixels from the sprite's top-left: the point placed at the drawing position, and the pivot for rotation and scale.

#### Remarks

Sprites usually come from a [SpriteAtlas](MicroEngine.Graphics.SpriteAtlas.md), which owns their texture.

## Properties

### <a id="MicroEngine_Graphics_Sprite_Height"></a> Height

Height of the sprite in pixels.

```csharp
public int Height { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Sprite_Origin"></a> Origin

The anchor in source pixels from the sprite's top-left: the point placed at the drawing position, and the pivot for rotation and scale.

```csharp
public Vector2 Origin { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Graphics_Sprite_Source"></a> Source

The sprite's rectangle in the texture, in texture pixels.

```csharp
public RectF Source { get; init; }
```

#### Property Value

 RectF

### <a id="MicroEngine_Graphics_Sprite_Texture"></a> Texture

The texture that holds the pixels; owned by whoever created it (usually an atlas).

```csharp
public Texture Texture { get; init; }
```

#### Property Value

 [Texture](MicroEngine.Graphics.Texture.md)

### <a id="MicroEngine_Graphics_Sprite_Width"></a> Width

Width of the sprite in pixels.

```csharp
public int Width { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Graphics_Sprite__Clone__"></a> <Clone\>$\(\)

```csharp
public Sprite <Clone>$()
```

#### Returns

 [Sprite](MicroEngine.Graphics.Sprite.md)

### <a id="MicroEngine_Graphics_Sprite_Deconstruct_MicroEngine_Graphics_Texture__MicroEngine_RectF__System_Numerics_Vector2__"></a> Deconstruct\(out Texture, out RectF, out Vector2\)

```csharp
public void Deconstruct(out Texture Texture, out RectF Source, out Vector2 Origin)
```

#### Parameters

`Texture` [Texture](MicroEngine.Graphics.Texture.md)

`Source` RectF

`Origin` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Graphics_Sprite_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Sprite_Equals_MicroEngine_Graphics_Sprite_"></a> Equals\(Sprite?\)

```csharp
public bool Equals(Sprite? other)
```

#### Parameters

`other` [Sprite](MicroEngine.Graphics.Sprite.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Sprite_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Sprite_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_Sprite_op_Equality_MicroEngine_Graphics_Sprite_MicroEngine_Graphics_Sprite_"></a> operator ==\(Sprite?, Sprite?\)

```csharp
public static bool operator ==(Sprite? left, Sprite? right)
```

#### Parameters

`left` [Sprite](MicroEngine.Graphics.Sprite.md)?

`right` [Sprite](MicroEngine.Graphics.Sprite.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Sprite_op_Inequality_MicroEngine_Graphics_Sprite_MicroEngine_Graphics_Sprite_"></a> operator \!=\(Sprite?, Sprite?\)

```csharp
public static bool operator !=(Sprite? left, Sprite? right)
```

#### Parameters

`left` [Sprite](MicroEngine.Graphics.Sprite.md)?

`right` [Sprite](MicroEngine.Graphics.Sprite.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

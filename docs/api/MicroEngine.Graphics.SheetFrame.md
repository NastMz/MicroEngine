# Class SheetFrame

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

One frame of a sprite sheet as pixels, before it is packed into an atlas.

```csharp
public sealed record SheetFrame : IEquatable<SheetFrame>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SheetFrame](MicroEngine.Graphics.SheetFrame.md)

#### Implements

[IEquatable<SheetFrame\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_SheetFrame__ctor_System_String_MicroEngine_Graphics_PixelImage_System_Numerics_Vector2_"></a> SheetFrame\(string, PixelImage, Vector2\)

One frame of a sprite sheet as pixels, before it is packed into an atlas.

```csharp
public SheetFrame(string Name, PixelImage Image, Vector2 Origin)
```

#### Parameters

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)

The sprite's name in the atlas ("drone0", "prop_palm").

`Image` [PixelImage](MicroEngine.Graphics.PixelImage.md)

Its pixels.

`Origin` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

Its anchor in its own pixels (see [Origin](MicroEngine.Graphics.Sprite.md#MicroEngine_Graphics_Sprite_Origin)).

## Properties

### <a id="MicroEngine_Graphics_SheetFrame_Image"></a> Image

Its pixels.

```csharp
public PixelImage Image { get; init; }
```

#### Property Value

 [PixelImage](MicroEngine.Graphics.PixelImage.md)

### <a id="MicroEngine_Graphics_SheetFrame_Name"></a> Name

The sprite's name in the atlas ("drone0", "prop_palm").

```csharp
public string Name { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Graphics_SheetFrame_Origin"></a> Origin

Its anchor in its own pixels (see [Origin](MicroEngine.Graphics.Sprite.md#MicroEngine_Graphics_Sprite_Origin)).

```csharp
public Vector2 Origin { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

## Methods

### <a id="MicroEngine_Graphics_SheetFrame__Clone__"></a> <Clone\>$\(\)

```csharp
public SheetFrame <Clone>$()
```

#### Returns

 [SheetFrame](MicroEngine.Graphics.SheetFrame.md)

### <a id="MicroEngine_Graphics_SheetFrame_Deconstruct_System_String__MicroEngine_Graphics_PixelImage__System_Numerics_Vector2__"></a> Deconstruct\(out string, out PixelImage, out Vector2\)

```csharp
public void Deconstruct(out string Name, out PixelImage Image, out Vector2 Origin)
```

#### Parameters

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Image` [PixelImage](MicroEngine.Graphics.PixelImage.md)

`Origin` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Graphics_SheetFrame_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_SheetFrame_Equals_MicroEngine_Graphics_SheetFrame_"></a> Equals\(SheetFrame?\)

```csharp
public bool Equals(SheetFrame? other)
```

#### Parameters

`other` [SheetFrame](MicroEngine.Graphics.SheetFrame.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_SheetFrame_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_SheetFrame_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_SheetFrame_op_Equality_MicroEngine_Graphics_SheetFrame_MicroEngine_Graphics_SheetFrame_"></a> operator ==\(SheetFrame?, SheetFrame?\)

```csharp
public static bool operator ==(SheetFrame? left, SheetFrame? right)
```

#### Parameters

`left` [SheetFrame](MicroEngine.Graphics.SheetFrame.md)?

`right` [SheetFrame](MicroEngine.Graphics.SheetFrame.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_SheetFrame_op_Inequality_MicroEngine_Graphics_SheetFrame_MicroEngine_Graphics_SheetFrame_"></a> operator \!=\(SheetFrame?, SheetFrame?\)

```csharp
public static bool operator !=(SheetFrame? left, SheetFrame? right)
```

#### Parameters

`left` [SheetFrame](MicroEngine.Graphics.SheetFrame.md)?

`right` [SheetFrame](MicroEngine.Graphics.SheetFrame.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

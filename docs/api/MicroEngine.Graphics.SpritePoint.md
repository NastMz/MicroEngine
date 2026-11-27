# Struct SpritePoint

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

A named point of a frame: a shoulder, a gun tip, where a hat sits. Written in a <code>.sheet</code> file
(<code>"points": { "muzzle": [14, 3] }</code>) or drawn in Aseprite as the pivot of a slice.

```csharp
public readonly struct SpritePoint : IEquatable<SpritePoint>
```

#### Implements

[IEquatable<SpritePoint\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_SpritePoint__ctor_System_String_System_Numerics_Vector2_"></a> SpritePoint\(string, Vector2\)

A named point of a frame: a shoulder, a gun tip, where a hat sits. Written in a <code>.sheet</code> file
(<code>"points": { "muzzle": [14, 3] }</code>) or drawn in Aseprite as the pivot of a slice.

```csharp
public SpritePoint(string Name, Vector2 Position)
```

#### Parameters

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)

The point's name.

`Position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

Where it lies, in the frame's pixels from its top-left corner (like [Origin](MicroEngine.Graphics.Sprite.md#MicroEngine_Graphics_Sprite_Origin)); a pixel's centre is half a pixel in.

## Properties

### <a id="MicroEngine_Graphics_SpritePoint_Name"></a> Name

The point's name.

```csharp
public string Name { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Graphics_SpritePoint_Position"></a> Position

Where it lies, in the frame's pixels from its top-left corner (like [Origin](MicroEngine.Graphics.Sprite.md#MicroEngine_Graphics_Sprite_Origin)); a pixel's centre is half a pixel in.

```csharp
public Vector2 Position { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

## Methods

### <a id="MicroEngine_Graphics_SpritePoint_Deconstruct_System_String__System_Numerics_Vector2__"></a> Deconstruct\(out string, out Vector2\)

```csharp
public void Deconstruct(out string Name, out Vector2 Position)
```

#### Parameters

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Graphics_SpritePoint_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_SpritePoint_Equals_MicroEngine_Graphics_SpritePoint_"></a> Equals\(SpritePoint\)

```csharp
public bool Equals(SpritePoint other)
```

#### Parameters

`other` [SpritePoint](MicroEngine.Graphics.SpritePoint.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_SpritePoint_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_SpritePoint_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_SpritePoint_op_Equality_MicroEngine_Graphics_SpritePoint_MicroEngine_Graphics_SpritePoint_"></a> operator ==\(SpritePoint, SpritePoint\)

```csharp
public static bool operator ==(SpritePoint left, SpritePoint right)
```

#### Parameters

`left` [SpritePoint](MicroEngine.Graphics.SpritePoint.md)

`right` [SpritePoint](MicroEngine.Graphics.SpritePoint.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_SpritePoint_op_Inequality_MicroEngine_Graphics_SpritePoint_MicroEngine_Graphics_SpritePoint_"></a> operator \!=\(SpritePoint, SpritePoint\)

```csharp
public static bool operator !=(SpritePoint left, SpritePoint right)
```

#### Parameters

`left` [SpritePoint](MicroEngine.Graphics.SpritePoint.md)

`right` [SpritePoint](MicroEngine.Graphics.SpritePoint.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

# Struct SpriteBox

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

A named rectangle of a frame: a hit box, a hurt box, the part that is solid. Written in a <code>.sheet</code> file
(<code>"boxes": { "hit": [10, 2, 6, 4] }</code>) or drawn in Aseprite as a slice.

```csharp
public readonly struct SpriteBox : IEquatable<SpriteBox>
```

#### Implements

[IEquatable<SpriteBox\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_SpriteBox__ctor_System_String_MicroEngine_RectF_"></a> SpriteBox\(string, RectF\)

A named rectangle of a frame: a hit box, a hurt box, the part that is solid. Written in a <code>.sheet</code> file
(<code>"boxes": { "hit": [10, 2, 6, 4] }</code>) or drawn in Aseprite as a slice.

```csharp
public SpriteBox(string Name, RectF Bounds)
```

#### Parameters

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)

The box's name.

`Bounds` [RectF](MicroEngine.RectF.md)

Its rectangle, in the frame's pixels from its top-left corner.

## Properties

### <a id="MicroEngine_Graphics_SpriteBox_Bounds"></a> Bounds

Its rectangle, in the frame's pixels from its top-left corner.

```csharp
public RectF Bounds { get; init; }
```

#### Property Value

 [RectF](MicroEngine.RectF.md)

### <a id="MicroEngine_Graphics_SpriteBox_Name"></a> Name

The box's name.

```csharp
public string Name { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Graphics_SpriteBox_Deconstruct_System_String__MicroEngine_RectF__"></a> Deconstruct\(out string, out RectF\)

```csharp
public void Deconstruct(out string Name, out RectF Bounds)
```

#### Parameters

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Bounds` [RectF](MicroEngine.RectF.md)

### <a id="MicroEngine_Graphics_SpriteBox_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_SpriteBox_Equals_MicroEngine_Graphics_SpriteBox_"></a> Equals\(SpriteBox\)

```csharp
public bool Equals(SpriteBox other)
```

#### Parameters

`other` [SpriteBox](MicroEngine.Graphics.SpriteBox.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_SpriteBox_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_SpriteBox_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_SpriteBox_op_Equality_MicroEngine_Graphics_SpriteBox_MicroEngine_Graphics_SpriteBox_"></a> operator ==\(SpriteBox, SpriteBox\)

```csharp
public static bool operator ==(SpriteBox left, SpriteBox right)
```

#### Parameters

`left` [SpriteBox](MicroEngine.Graphics.SpriteBox.md)

`right` [SpriteBox](MicroEngine.Graphics.SpriteBox.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_SpriteBox_op_Inequality_MicroEngine_Graphics_SpriteBox_MicroEngine_Graphics_SpriteBox_"></a> operator \!=\(SpriteBox, SpriteBox\)

```csharp
public static bool operator !=(SpriteBox left, SpriteBox right)
```

#### Parameters

`left` [SpriteBox](MicroEngine.Graphics.SpriteBox.md)

`right` [SpriteBox](MicroEngine.Graphics.SpriteBox.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

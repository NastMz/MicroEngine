# Struct RecordingOverlayCanvas.Shape

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

One recorded drawing call.

```csharp
public readonly struct RecordingOverlayCanvas.Shape : IEquatable<RecordingOverlayCanvas.Shape>
```

#### Implements

[IEquatable<RecordingOverlayCanvas.Shape\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Core_RecordingOverlayCanvas_Shape__ctor_System_String_System_Numerics_Vector2_System_Single_MicroEngine_Color_System_String_"></a> Shape\(string, Vector2, float, Color, string?\)

One recorded drawing call.

```csharp
public Shape(string Kind, Vector2 Center, float Size, Color Color, string? Text = null)
```

#### Parameters

`Kind` [string](https://learn.microsoft.com/dotnet/api/system.string)

"circle", "ring", "triangle", "rect", "text" or "image".

`Center` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

Centre of the shape (the centroid for a triangle).

`Size` [float](https://learn.microsoft.com/dotnet/api/system.single)

Radius for circles and rings, width for rectangles, the length of the first side for triangles, height for text.

`Color` [Color](MicroEngine.Color.md)

The colour drawn.

`Text` [string](https://learn.microsoft.com/dotnet/api/system.string)?

The text, for "text" shapes; otherwise null.

## Properties

### <a id="MicroEngine_Core_RecordingOverlayCanvas_Shape_Center"></a> Center

Centre of the shape (the centroid for a triangle).

```csharp
public Vector2 Center { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Core_RecordingOverlayCanvas_Shape_Color"></a> Color

The colour drawn.

```csharp
public Color Color { get; init; }
```

#### Property Value

 [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Core_RecordingOverlayCanvas_Shape_Kind"></a> Kind

"circle", "ring", "triangle", "rect", "text" or "image".

```csharp
public string Kind { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Core_RecordingOverlayCanvas_Shape_Size"></a> Size

Radius for circles and rings, width for rectangles, the length of the first side for triangles, height for text.

```csharp
public float Size { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Core_RecordingOverlayCanvas_Shape_Text"></a> Text

The text, for "text" shapes; otherwise null.

```csharp
public string? Text { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

## Methods

### <a id="MicroEngine_Core_RecordingOverlayCanvas_Shape_Deconstruct_System_String__System_Numerics_Vector2__System_Single__MicroEngine_Color__System_String__"></a> Deconstruct\(out string, out Vector2, out float, out Color, out string?\)

```csharp
public void Deconstruct(out string Kind, out Vector2 Center, out float Size, out Color Color, out string? Text)
```

#### Parameters

`Kind` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Center` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`Size` [float](https://learn.microsoft.com/dotnet/api/system.single)

`Color` [Color](MicroEngine.Color.md)

`Text` [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Core_RecordingOverlayCanvas_Shape_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_RecordingOverlayCanvas_Shape_Equals_MicroEngine_Core_RecordingOverlayCanvas_Shape_"></a> Equals\(Shape\)

```csharp
public bool Equals(RecordingOverlayCanvas.Shape other)
```

#### Parameters

`other` [RecordingOverlayCanvas](MicroEngine.Core.RecordingOverlayCanvas.md).[Shape](MicroEngine.Core.RecordingOverlayCanvas.Shape.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_RecordingOverlayCanvas_Shape_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_RecordingOverlayCanvas_Shape_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Core_RecordingOverlayCanvas_Shape_op_Equality_MicroEngine_Core_RecordingOverlayCanvas_Shape_MicroEngine_Core_RecordingOverlayCanvas_Shape_"></a> operator ==\(Shape, Shape\)

```csharp
public static bool operator ==(RecordingOverlayCanvas.Shape left, RecordingOverlayCanvas.Shape right)
```

#### Parameters

`left` [RecordingOverlayCanvas](MicroEngine.Core.RecordingOverlayCanvas.md).[Shape](MicroEngine.Core.RecordingOverlayCanvas.Shape.md)

`right` [RecordingOverlayCanvas](MicroEngine.Core.RecordingOverlayCanvas.md).[Shape](MicroEngine.Core.RecordingOverlayCanvas.Shape.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_RecordingOverlayCanvas_Shape_op_Inequality_MicroEngine_Core_RecordingOverlayCanvas_Shape_MicroEngine_Core_RecordingOverlayCanvas_Shape_"></a> operator \!=\(Shape, Shape\)

```csharp
public static bool operator !=(RecordingOverlayCanvas.Shape left, RecordingOverlayCanvas.Shape right)
```

#### Parameters

`left` [RecordingOverlayCanvas](MicroEngine.Core.RecordingOverlayCanvas.md).[Shape](MicroEngine.Core.RecordingOverlayCanvas.Shape.md)

`right` [RecordingOverlayCanvas](MicroEngine.Core.RecordingOverlayCanvas.md).[Shape](MicroEngine.Core.RecordingOverlayCanvas.Shape.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

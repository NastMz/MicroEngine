# Class RecordingOverlayCanvas

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

An [IOverlayCanvas](MicroEngine.Input.IOverlayCanvas.md) that keeps a list of the shapes drawn, for tests.

```csharp
public sealed class RecordingOverlayCanvas : IOverlayCanvas
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[RecordingOverlayCanvas](MicroEngine.Core.RecordingOverlayCanvas.md)

#### Implements

[IOverlayCanvas](MicroEngine.Input.IOverlayCanvas.md)

## Constructors

### <a id="MicroEngine_Core_RecordingOverlayCanvas__ctor"></a> RecordingOverlayCanvas\(\)

```csharp
public RecordingOverlayCanvas()
```

## Properties

### <a id="MicroEngine_Core_RecordingOverlayCanvas_Shapes"></a> Shapes

The shapes drawn, in order.

```csharp
public List<RecordingOverlayCanvas.Shape> Shapes { get; }
```

#### Property Value

 [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[RecordingOverlayCanvas](MicroEngine.Core.RecordingOverlayCanvas.md).[Shape](MicroEngine.Core.RecordingOverlayCanvas.Shape.md)\>

## Methods

### <a id="MicroEngine_Core_RecordingOverlayCanvas_FillCircle_System_Numerics_Vector2_System_Single_MicroEngine_Color_"></a> FillCircle\(Vector2, float, Color\)

Fills a circle.

```csharp
public void FillCircle(Vector2 center, float radius, Color color)
```

#### Parameters

`center` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`radius` [float](https://learn.microsoft.com/dotnet/api/system.single)

`color` [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Core_RecordingOverlayCanvas_FillRect_MicroEngine_RectF_MicroEngine_Color_"></a> FillRect\(RectF, Color\)

Fills a rectangle.

```csharp
public void FillRect(RectF rect, Color color)
```

#### Parameters

`rect` [RectF](MicroEngine.RectF.md)

`color` [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Core_RecordingOverlayCanvas_FillTriangle_System_Numerics_Vector2_System_Numerics_Vector2_System_Numerics_Vector2_MicroEngine_Color_"></a> FillTriangle\(Vector2, Vector2, Vector2, Color\)

Fills a triangle.

```csharp
public void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
```

#### Parameters

`a` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`b` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`c` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`color` [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Core_RecordingOverlayCanvas_Ring_System_Numerics_Vector2_System_Single_System_Single_MicroEngine_Color_"></a> Ring\(Vector2, float, float, Color\)

A circle outline of the given thickness, drawn inward from <code class="paramref">radius</code>.

```csharp
public void Ring(Vector2 center, float radius, float thickness, Color color)
```

#### Parameters

`center` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`radius` [float](https://learn.microsoft.com/dotnet/api/system.single)

`thickness` [float](https://learn.microsoft.com/dotnet/api/system.single)

`color` [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Core_RecordingOverlayCanvas_Text_System_String_System_Numerics_Vector2_System_Single_MicroEngine_Color_"></a> Text\(string, Vector2, float, Color\)

Short text centred on <code class="paramref">center</code>, about <code class="paramref">height</code> pixels tall.

```csharp
public void Text(string text, Vector2 center, float height, Color color)
```

#### Parameters

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

`center` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`height` [float](https://learn.microsoft.com/dotnet/api/system.single)

`color` [Color](MicroEngine.Color.md)

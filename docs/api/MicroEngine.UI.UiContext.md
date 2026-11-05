# Struct UiContext

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.UI](MicroEngine.UI.md)  
Assembly: MicroEngine.UI.dll  
Guide: [UI widgets](../manual/ui.md)  

What widgets measure and draw with: the root's font and theme, the layout unit and the root's clock.

```csharp
public readonly struct UiContext : IEquatable<UiContext>
```

#### Implements

[IEquatable<UiContext\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_UI_UiContext__ctor_MicroEngine_Graphics_IFont_MicroEngine_UI_UiTheme_System_Single_System_Single_"></a> UiContext\(IFont, UiTheme, float, float\)

What widgets measure and draw with: the root's font and theme, the layout unit and the root's clock.

```csharp
public UiContext(IFont Font, UiTheme Theme, float Unit, float Time)
```

#### Parameters

`Font` [IFont](MicroEngine.Graphics.IFont.md)

The font of every widget's text.

`Theme` [UiTheme](MicroEngine.UI.UiTheme.md)

The colours.

`Unit` [float](https://learn.microsoft.com/dotnet/api/system.single)

The layout unit in UI units: 1 for the built-in 10-pixel pixel font, proportionally more for a larger font, so margins
grow with the text. Widgets express their padding and spacing in it.

`Time` [float](https://learn.microsoft.com/dotnet/api/system.single)

Seconds the root has been updated, for blinking carets and other animation.

## Properties

### <a id="MicroEngine_UI_UiContext_Font"></a> Font

The font of every widget's text.

```csharp
public IFont Font { get; init; }
```

#### Property Value

 [IFont](MicroEngine.Graphics.IFont.md)

### <a id="MicroEngine_UI_UiContext_Theme"></a> Theme

The colours.

```csharp
public UiTheme Theme { get; init; }
```

#### Property Value

 [UiTheme](MicroEngine.UI.UiTheme.md)

### <a id="MicroEngine_UI_UiContext_Time"></a> Time

Seconds the root has been updated, for blinking carets and other animation.

```csharp
public float Time { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_UI_UiContext_Unit"></a> Unit

The layout unit in UI units: 1 for the built-in 10-pixel pixel font, proportionally more for a larger font, so margins
grow with the text. Widgets express their padding and spacing in it.

```csharp
public float Unit { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_UI_UiContext_Deconstruct_MicroEngine_Graphics_IFont__MicroEngine_UI_UiTheme__System_Single__System_Single__"></a> Deconstruct\(out IFont, out UiTheme, out float, out float\)

```csharp
public void Deconstruct(out IFont Font, out UiTheme Theme, out float Unit, out float Time)
```

#### Parameters

`Font` [IFont](MicroEngine.Graphics.IFont.md)

`Theme` [UiTheme](MicroEngine.UI.UiTheme.md)

`Unit` [float](https://learn.microsoft.com/dotnet/api/system.single)

`Time` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_UI_UiContext_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_UiContext_Equals_MicroEngine_UI_UiContext_"></a> Equals\(UiContext\)

```csharp
public bool Equals(UiContext other)
```

#### Parameters

`other` [UiContext](MicroEngine.UI.UiContext.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_UiContext_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_UI_UiContext_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_UI_UiContext_op_Equality_MicroEngine_UI_UiContext_MicroEngine_UI_UiContext_"></a> operator ==\(UiContext, UiContext\)

```csharp
public static bool operator ==(UiContext left, UiContext right)
```

#### Parameters

`left` [UiContext](MicroEngine.UI.UiContext.md)

`right` [UiContext](MicroEngine.UI.UiContext.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_UiContext_op_Inequality_MicroEngine_UI_UiContext_MicroEngine_UI_UiContext_"></a> operator \!=\(UiContext, UiContext\)

```csharp
public static bool operator !=(UiContext left, UiContext right)
```

#### Parameters

`left` [UiContext](MicroEngine.UI.UiContext.md)

`right` [UiContext](MicroEngine.UI.UiContext.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

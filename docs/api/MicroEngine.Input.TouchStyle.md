# Class TouchStyle

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.Touch.dll  
Guide: [Input](../manual/input.md)  

How touch controls look: shape, colours of each state, outline, press feedback and optional images. Only the
drawing changes: hit areas, finger tracking and actions stay those of the layout, so a restyled control behaves
exactly as before. Colours left null follow [Tint](MicroEngine.Input.TouchLayout.md#MicroEngine_Input_TouchLayout_Tint) and [PressedTint](MicroEngine.Input.TouchLayout.md#MicroEngine_Input_TouchLayout_PressedTint).
Set one style for every control kind ([ButtonStyle](MicroEngine.Input.TouchLayout.md#MicroEngine_Input_TouchLayout_ButtonStyle), [DPadStyle](MicroEngine.Input.TouchLayout.md#MicroEngine_Input_TouchLayout_DPadStyle),
[StickStyle](MicroEngine.Input.TouchLayout.md#MicroEngine_Input_TouchLayout_StickStyle)) or for one control ([Style](MicroEngine.Input.TouchControl.md#MicroEngine_Input_TouchControl_Style)).

```csharp
public sealed record TouchStyle : IEquatable<TouchStyle>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[TouchStyle](MicroEngine.Input.TouchStyle.md)

#### Implements

[IEquatable<TouchStyle\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Input_TouchStyle__ctor"></a> TouchStyle\(\)

```csharp
public TouchStyle()
```

## Properties

### <a id="MicroEngine_Input_TouchStyle_Arrows"></a> Arrows

For d-pads: whether the four arrows are drawn (an image may already show them).

```csharp
public bool Arrows { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_TouchStyle_Corner"></a> Corner

Corner radius of a rounded square, as a fraction of the control's radius (0..1).

```csharp
public float Corner { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Input_TouchStyle_Fill"></a> Fill

Fill of an idle control (null: the tint, faint).

```csharp
public Color? Fill { get; init; }
```

#### Property Value

 [Color](MicroEngine.Color.md)?

### <a id="MicroEngine_Input_TouchStyle_Halo"></a> Halo

Whether a dark ring is drawn around the outline, so controls stay visible over light scenes.

```csharp
public bool Halo { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_TouchStyle_Image"></a> Image

An image drawn over the control's square (twice its radius) instead of fill and outline: a sprite button, a
d-pad cross, a joystick base. Pixel art scales without blur.

```csharp
public PixelImage? Image { get; init; }
```

#### Property Value

 [PixelImage](MicroEngine.Graphics.PixelImage.md)?

### <a id="MicroEngine_Input_TouchStyle_KnobImage"></a> KnobImage

For joysticks: the knob's image (null: a disc).

```csharp
public PixelImage? KnobImage { get; init; }
```

#### Property Value

 [PixelImage](MicroEngine.Graphics.PixelImage.md)?

### <a id="MicroEngine_Input_TouchStyle_KnobSize"></a> KnobSize

For joysticks: the knob's radius as a fraction of the stick's radius.

```csharp
public float KnobSize { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Input_TouchStyle_Label"></a> Label

Colour of a button's label (null: the tint, white while pressed).

```csharp
public Color? Label { get; init; }
```

#### Property Value

 [Color](MicroEngine.Color.md)?

### <a id="MicroEngine_Input_TouchStyle_Opacity"></a> Opacity

Opacity of idle controls with this style, 0..1 (null: [Opacity](MicroEngine.Input.TouchLayout.md#MicroEngine_Input_TouchLayout_Opacity)).

```csharp
public float? Opacity { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)?

### <a id="MicroEngine_Input_TouchStyle_Outline"></a> Outline

Outline and arrows of an idle control (null: the tint).

```csharp
public Color? Outline { get; init; }
```

#### Property Value

 [Color](MicroEngine.Color.md)?

### <a id="MicroEngine_Input_TouchStyle_OutlineWidth"></a> OutlineWidth

Thickness of the outline in dp (0: none).

```csharp
public float OutlineWidth { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Input_TouchStyle_PressedFill"></a> PressedFill

Fill of a pressed control (null: the pressed tint).

```csharp
public Color? PressedFill { get; init; }
```

#### Property Value

 [Color](MicroEngine.Color.md)?

### <a id="MicroEngine_Input_TouchStyle_PressedImage"></a> PressedImage

The image while pressed (null: [Image](MicroEngine.Input.TouchStyle.md#MicroEngine_Input_TouchStyle_Image), tinted with the pressed fill).

```csharp
public PixelImage? PressedImage { get; init; }
```

#### Property Value

 [PixelImage](MicroEngine.Graphics.PixelImage.md)?

### <a id="MicroEngine_Input_TouchStyle_PressedOutline"></a> PressedOutline

Outline, lit arrows and the joystick's direction arrow while pressed (null: the tint, brighter, and the pressed tint for arrows).

```csharp
public Color? PressedOutline { get; init; }
```

#### Property Value

 [Color](MicroEngine.Color.md)?

### <a id="MicroEngine_Input_TouchStyle_PressedScale"></a> PressedScale

Size of a pressed button relative to an idle one (1: no shrink).

```csharp
public float PressedScale { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Input_TouchStyle_Shape"></a> Shape

The outline: a disc or a rounded square.

```csharp
public TouchShape Shape { get; init; }
```

#### Property Value

 [TouchShape](MicroEngine.Input.TouchShape.md)

## Methods

### <a id="MicroEngine_Input_TouchStyle__Clone__"></a> <Clone\>$\(\)

```csharp
public TouchStyle <Clone>$()
```

#### Returns

 [TouchStyle](MicroEngine.Input.TouchStyle.md)

### <a id="MicroEngine_Input_TouchStyle_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_TouchStyle_Equals_MicroEngine_Input_TouchStyle_"></a> Equals\(TouchStyle?\)

```csharp
public bool Equals(TouchStyle? other)
```

#### Parameters

`other` [TouchStyle](MicroEngine.Input.TouchStyle.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_TouchStyle_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Input_TouchStyle_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Input_TouchStyle_op_Equality_MicroEngine_Input_TouchStyle_MicroEngine_Input_TouchStyle_"></a> operator ==\(TouchStyle?, TouchStyle?\)

```csharp
public static bool operator ==(TouchStyle? left, TouchStyle? right)
```

#### Parameters

`left` [TouchStyle](MicroEngine.Input.TouchStyle.md)?

`right` [TouchStyle](MicroEngine.Input.TouchStyle.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_TouchStyle_op_Inequality_MicroEngine_Input_TouchStyle_MicroEngine_Input_TouchStyle_"></a> operator \!=\(TouchStyle?, TouchStyle?\)

```csharp
public static bool operator !=(TouchStyle? left, TouchStyle? right)
```

#### Parameters

`left` [TouchStyle](MicroEngine.Input.TouchStyle.md)?

`right` [TouchStyle](MicroEngine.Input.TouchStyle.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

# Class TouchLayout

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.dll  
Guide: [Input](../manual/input.md)  

A game's on-screen controls: which controls, where, and how they look and feel. The same layout is used on phones
(SDL), in mobile browsers and in tests; games return theirs from <code>Game.CreateTouchLayout</code>.

```csharp
public sealed record TouchLayout : IEquatable<TouchLayout>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[TouchLayout](MicroEngine.Input.TouchLayout.md)

#### Implements

[IEquatable<TouchLayout\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Input_TouchLayout__ctor"></a> TouchLayout\(\)

```csharp
public TouchLayout()
```

## Properties

### <a id="MicroEngine_Input_TouchLayout_ButtonStyle"></a> ButtonStyle

How buttons look (null: the default style).

```csharp
public TouchStyle? ButtonStyle { get; init; }
```

#### Property Value

 [TouchStyle](MicroEngine.Input.TouchStyle.md)?

### <a id="MicroEngine_Input_TouchLayout_Controls"></a> Controls

The controls, drawn in order; ids must be unique.

```csharp
public IReadOnlyList<TouchControl> Controls { get; init; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[TouchControl](MicroEngine.Input.TouchControl.md)\>

### <a id="MicroEngine_Input_TouchLayout_DPadStyle"></a> DPadStyle

How d-pads look (null: the default style).

```csharp
public TouchStyle? DPadStyle { get; init; }
```

#### Property Value

 [TouchStyle](MicroEngine.Input.TouchStyle.md)?

### <a id="MicroEngine_Input_TouchLayout_Haptics"></a> Haptics

A short vibration on each press where the device can vibrate.

```csharp
public bool Haptics { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_TouchLayout_HitScale"></a> HitScale

The touchable area as a multiple of the drawn radius (overlaps go to the nearest control).

```csharp
public float HitScale { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Input_TouchLayout_Opacity"></a> Opacity

Opacity of idle controls, 0..1. Pressed controls are always more opaque.

```csharp
public float Opacity { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Input_TouchLayout_PressedTint"></a> PressedTint

Fill of a pressed control.

```csharp
public Color PressedTint { get; init; }
```

#### Property Value

 Color

### <a id="MicroEngine_Input_TouchLayout_Scale"></a> Scale

Size factor on top of the screen's density (a player setting).

```csharp
public float Scale { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Input_TouchLayout_Skin"></a> Skin

Draws the controls (null: [DefaultTouchSkin](MicroEngine.Input.DefaultTouchSkin.md), shaped by the styles). Replace it for a look styles cannot give.

```csharp
public ITouchSkin? Skin { get; init; }
```

#### Property Value

 [ITouchSkin](MicroEngine.Input.ITouchSkin.md)?

### <a id="MicroEngine_Input_TouchLayout_SlideBetweenButtons"></a> SlideBetweenButtons

Whether a finger that slides onto another button presses it instead (fire-and-jump rolls); toggle buttons are never slid onto.

```csharp
public bool SlideBetweenButtons { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_TouchLayout_SlideTolerance"></a> SlideTolerance

A held button stays pressed until its finger is this many touchable radii from its centre.

```csharp
public float SlideTolerance { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Input_TouchLayout_StickStyle"></a> StickStyle

How joysticks look (null: the default style).

```csharp
public TouchStyle? StickStyle { get; init; }
```

#### Property Value

 [TouchStyle](MicroEngine.Input.TouchStyle.md)?

### <a id="MicroEngine_Input_TouchLayout_Tint"></a> Tint

Colour of outlines and labels.

```csharp
public Color Tint { get; init; }
```

#### Property Value

 Color

### <a id="MicroEngine_Input_TouchLayout_Visibility"></a> Visibility

When the controls are drawn (automatic by default).

```csharp
public TouchVisibility Visibility { get; init; }
```

#### Property Value

 [TouchVisibility](MicroEngine.Input.TouchVisibility.md)

## Methods

### <a id="MicroEngine_Input_TouchLayout__Clone__"></a> <Clone\>$\(\)

```csharp
public TouchLayout <Clone>$()
```

#### Returns

 [TouchLayout](MicroEngine.Input.TouchLayout.md)

### <a id="MicroEngine_Input_TouchLayout_CreateDefault"></a> CreateDefault\(\)

The layout for [CreateDefault](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_CreateDefault): a d-pad on the left; confirm (A), cancel (B) and action (C)
on the right; pause at the top right.

```csharp
public static TouchLayout CreateDefault()
```

#### Returns

 [TouchLayout](MicroEngine.Input.TouchLayout.md)

### <a id="MicroEngine_Input_TouchLayout_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_TouchLayout_Equals_MicroEngine_Input_TouchLayout_"></a> Equals\(TouchLayout?\)

```csharp
public bool Equals(TouchLayout? other)
```

#### Parameters

`other` [TouchLayout](MicroEngine.Input.TouchLayout.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_TouchLayout_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Input_TouchLayout_Of_MicroEngine_Input_TouchControl___"></a> Of\(params TouchControl\[\]\)

A layout with these controls and the default look.

```csharp
public static TouchLayout Of(params TouchControl[] controls)
```

#### Parameters

`controls` [TouchControl](MicroEngine.Input.TouchControl.md)\[\]

#### Returns

 [TouchLayout](MicroEngine.Input.TouchLayout.md)

### <a id="MicroEngine_Input_TouchLayout_StyleOf_MicroEngine_Input_TouchControl_"></a> StyleOf\(TouchControl\)

The style <code class="paramref">control</code> is drawn with: its own, else its kind's, else the default.

```csharp
public TouchStyle StyleOf(TouchControl control)
```

#### Parameters

`control` [TouchControl](MicroEngine.Input.TouchControl.md)

#### Returns

 [TouchStyle](MicroEngine.Input.TouchStyle.md)

### <a id="MicroEngine_Input_TouchLayout_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Input_TouchLayout_op_Equality_MicroEngine_Input_TouchLayout_MicroEngine_Input_TouchLayout_"></a> operator ==\(TouchLayout?, TouchLayout?\)

```csharp
public static bool operator ==(TouchLayout? left, TouchLayout? right)
```

#### Parameters

`left` [TouchLayout](MicroEngine.Input.TouchLayout.md)?

`right` [TouchLayout](MicroEngine.Input.TouchLayout.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_TouchLayout_op_Inequality_MicroEngine_Input_TouchLayout_MicroEngine_Input_TouchLayout_"></a> operator \!=\(TouchLayout?, TouchLayout?\)

```csharp
public static bool operator !=(TouchLayout? left, TouchLayout? right)
```

#### Parameters

`left` [TouchLayout](MicroEngine.Input.TouchLayout.md)?

`right` [TouchLayout](MicroEngine.Input.TouchLayout.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

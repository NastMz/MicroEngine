# Struct TouchControlView

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.dll  
Guide: [Input](../manual/input.md)  

Everything a skin needs to draw one control as it is this frame, in screen pixels.

```csharp
public readonly struct TouchControlView : IEquatable<TouchControlView>
```

#### Implements

[IEquatable<TouchControlView\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Input_TouchControlView__ctor_MicroEngine_Input_TouchControl_MicroEngine_Input_TouchStyle_System_Numerics_Vector2_System_Single_System_Single_System_Single_System_Single_System_Boolean_System_Boolean_System_Boolean_System_Numerics_Vector2_System_Numerics_Vector2_System_Numerics_Vector2_System_Collections_Generic_IReadOnlySet_MicroEngine_Input_InputMap_StickDirection__MicroEngine_Input_TouchLayout_"></a> TouchControlView\(TouchControl, TouchStyle, Vector2, float, float, float, float, bool, bool, bool, Vector2, Vector2, Vector2, IReadOnlySet<StickDirection\>, TouchLayout\)

Everything a skin needs to draw one control as it is this frame, in screen pixels.

```csharp
public TouchControlView(TouchControl Control, TouchStyle Style, Vector2 Center, float Radius, float PixelsPerDp, float Opacity, float Glow, bool Pressed, bool Toggled, bool Held, Vector2 Origin, Vector2 Deflection, Vector2 Value, IReadOnlySet<InputMap.StickDirection> Directions, TouchLayout Layout)
```

#### Parameters

`Control` [TouchControl](MicroEngine.Input.TouchControl.md)

The control as declared in the layout.

`Style` [TouchStyle](MicroEngine.Input.TouchStyle.md)

The style that applies (the control's own, else its kind's from the layout, else the default).

`Center` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

The control's centre.

`Radius` [float](https://learn.microsoft.com/dotnet/api/system.single)

Its drawn radius.

`PixelsPerDp` [float](https://learn.microsoft.com/dotnet/api/system.single)

The layout's scale, for line widths in dp.

`Opacity` [float](https://learn.microsoft.com/dotnet/api/system.single)

Opacity of the idle control, 0..1.

`Glow` [float](https://learn.microsoft.com/dotnet/api/system.single)

1 while pressed, fading to 0 shortly after release (press feedback).

`Pressed` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether the control is pressed (held, toggled on, pushed).

`Toggled` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

For toggle buttons: whether it is on.

`Held` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether a finger is on it.

`Origin` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

Where a joystick's base is (it moves with a floating stick); the centre for other controls.

`Deflection` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

The thumb's offset from the base, each axis -1..1 (sticks and d-pads).

`Value` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

A joystick's value after its dead zone, or a d-pad's direction.

`Directions` [IReadOnlySet](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlyset\-1)<InputMap.StickDirection\>

The direction actions pressed now (sticks and d-pads).

`Layout` [TouchLayout](MicroEngine.Input.TouchLayout.md)

The layout, for its tints.

## Properties

### <a id="MicroEngine_Input_TouchControlView_Center"></a> Center

The control's centre.

```csharp
public Vector2 Center { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Input_TouchControlView_Control"></a> Control

The control as declared in the layout.

```csharp
public TouchControl Control { get; init; }
```

#### Property Value

 [TouchControl](MicroEngine.Input.TouchControl.md)

### <a id="MicroEngine_Input_TouchControlView_Deflection"></a> Deflection

The thumb's offset from the base, each axis -1..1 (sticks and d-pads).

```csharp
public Vector2 Deflection { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Input_TouchControlView_Directions"></a> Directions

The direction actions pressed now (sticks and d-pads).

```csharp
public IReadOnlySet<InputMap.StickDirection> Directions { get; init; }
```

#### Property Value

 [IReadOnlySet](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlyset\-1)<InputMap.StickDirection\>

### <a id="MicroEngine_Input_TouchControlView_Glow"></a> Glow

1 while pressed, fading to 0 shortly after release (press feedback).

```csharp
public float Glow { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Input_TouchControlView_Held"></a> Held

Whether a finger is on it.

```csharp
public bool Held { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_TouchControlView_Layout"></a> Layout

The layout, for its tints.

```csharp
public TouchLayout Layout { get; init; }
```

#### Property Value

 [TouchLayout](MicroEngine.Input.TouchLayout.md)

### <a id="MicroEngine_Input_TouchControlView_Opacity"></a> Opacity

Opacity of the idle control, 0..1.

```csharp
public float Opacity { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Input_TouchControlView_Origin"></a> Origin

Where a joystick's base is (it moves with a floating stick); the centre for other controls.

```csharp
public Vector2 Origin { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Input_TouchControlView_PixelsPerDp"></a> PixelsPerDp

The layout's scale, for line widths in dp.

```csharp
public float PixelsPerDp { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Input_TouchControlView_Pressed"></a> Pressed

Whether the control is pressed (held, toggled on, pushed).

```csharp
public bool Pressed { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_TouchControlView_Radius"></a> Radius

Its drawn radius.

```csharp
public float Radius { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Input_TouchControlView_Style"></a> Style

The style that applies (the control's own, else its kind's from the layout, else the default).

```csharp
public TouchStyle Style { get; init; }
```

#### Property Value

 [TouchStyle](MicroEngine.Input.TouchStyle.md)

### <a id="MicroEngine_Input_TouchControlView_Toggled"></a> Toggled

For toggle buttons: whether it is on.

```csharp
public bool Toggled { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_TouchControlView_Value"></a> Value

A joystick's value after its dead zone, or a d-pad's direction.

```csharp
public Vector2 Value { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

## Methods

### <a id="MicroEngine_Input_TouchControlView_Deconstruct_MicroEngine_Input_TouchControl__MicroEngine_Input_TouchStyle__System_Numerics_Vector2__System_Single__System_Single__System_Single__System_Single__System_Boolean__System_Boolean__System_Boolean__System_Numerics_Vector2__System_Numerics_Vector2__System_Numerics_Vector2__System_Collections_Generic_IReadOnlySet_MicroEngine_Input_InputMap_StickDirection___MicroEngine_Input_TouchLayout__"></a> Deconstruct\(out TouchControl, out TouchStyle, out Vector2, out float, out float, out float, out float, out bool, out bool, out bool, out Vector2, out Vector2, out Vector2, out IReadOnlySet<StickDirection\>, out TouchLayout\)

```csharp
public void Deconstruct(out TouchControl Control, out TouchStyle Style, out Vector2 Center, out float Radius, out float PixelsPerDp, out float Opacity, out float Glow, out bool Pressed, out bool Toggled, out bool Held, out Vector2 Origin, out Vector2 Deflection, out Vector2 Value, out IReadOnlySet<InputMap.StickDirection> Directions, out TouchLayout Layout)
```

#### Parameters

`Control` [TouchControl](MicroEngine.Input.TouchControl.md)

`Style` [TouchStyle](MicroEngine.Input.TouchStyle.md)

`Center` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`Radius` [float](https://learn.microsoft.com/dotnet/api/system.single)

`PixelsPerDp` [float](https://learn.microsoft.com/dotnet/api/system.single)

`Opacity` [float](https://learn.microsoft.com/dotnet/api/system.single)

`Glow` [float](https://learn.microsoft.com/dotnet/api/system.single)

`Pressed` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`Toggled` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`Held` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`Origin` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`Deflection` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`Value` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`Directions` [IReadOnlySet](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlyset\-1)<InputMap.StickDirection\>

`Layout` [TouchLayout](MicroEngine.Input.TouchLayout.md)

### <a id="MicroEngine_Input_TouchControlView_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_TouchControlView_Equals_MicroEngine_Input_TouchControlView_"></a> Equals\(TouchControlView\)

```csharp
public bool Equals(TouchControlView other)
```

#### Parameters

`other` [TouchControlView](MicroEngine.Input.TouchControlView.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_TouchControlView_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Input_TouchControlView_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Input_TouchControlView_op_Equality_MicroEngine_Input_TouchControlView_MicroEngine_Input_TouchControlView_"></a> operator ==\(TouchControlView, TouchControlView\)

```csharp
public static bool operator ==(TouchControlView left, TouchControlView right)
```

#### Parameters

`left` [TouchControlView](MicroEngine.Input.TouchControlView.md)

`right` [TouchControlView](MicroEngine.Input.TouchControlView.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_TouchControlView_op_Inequality_MicroEngine_Input_TouchControlView_MicroEngine_Input_TouchControlView_"></a> operator \!=\(TouchControlView, TouchControlView\)

```csharp
public static bool operator !=(TouchControlView left, TouchControlView right)
```

#### Parameters

`left` [TouchControlView](MicroEngine.Input.TouchControlView.md)

`right` [TouchControlView](MicroEngine.Input.TouchControlView.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

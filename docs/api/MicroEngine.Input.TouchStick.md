# Class TouchStick

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.dll  
Guide: [Input](../manual/input.md)  

An analog joystick: a 2D value with intensity ([Vector](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Vector_System_String_System_String_System_String_System_String_)) and, beyond [Threshold](MicroEngine.Input.TouchStick.md#MicroEngine_Input_TouchStick_Threshold),
the four direction actions as well, so digital code keeps working.

```csharp
public sealed record TouchStick : TouchControl, IEquatable<TouchControl>, IEquatable<TouchStick>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[TouchControl](MicroEngine.Input.TouchControl.md) ← 
[TouchStick](MicroEngine.Input.TouchStick.md)

#### Implements

[IEquatable<TouchControl\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1), 
[IEquatable<TouchStick\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

#### Inherited Members

[TouchControl.ToString\(\)](MicroEngine.Input.TouchControl.md\#MicroEngine\_Input\_TouchControl\_ToString), 
[TouchControl.GetHashCode\(\)](MicroEngine.Input.TouchControl.md\#MicroEngine\_Input\_TouchControl\_GetHashCode), 
[TouchControl.Equals\(object?\)](MicroEngine.Input.TouchControl.md\#MicroEngine\_Input\_TouchControl\_Equals\_System\_Object\_), 
[TouchControl.Equals\(TouchControl?\)](MicroEngine.Input.TouchControl.md\#MicroEngine\_Input\_TouchControl\_Equals\_MicroEngine\_Input\_TouchControl\_), 
[TouchControl.<Clone\>$\(\)](MicroEngine.Input.TouchControl.md\#MicroEngine\_Input\_TouchControl\_\_Clone\_\_), 
[TouchControl.Deconstruct\(out string\)](MicroEngine.Input.TouchControl.md\#MicroEngine\_Input\_TouchControl\_Deconstruct\_System\_String\_\_), 
[TouchControl.Id](MicroEngine.Input.TouchControl.md\#MicroEngine\_Input\_TouchControl\_Id), 
[TouchControl.Anchor](MicroEngine.Input.TouchControl.md\#MicroEngine\_Input\_TouchControl\_Anchor), 
[TouchControl.Offset](MicroEngine.Input.TouchControl.md\#MicroEngine\_Input\_TouchControl\_Offset), 
[TouchControl.Radius](MicroEngine.Input.TouchControl.md\#MicroEngine\_Input\_TouchControl\_Radius), 
[TouchControl.Visible](MicroEngine.Input.TouchControl.md\#MicroEngine\_Input\_TouchControl\_Visible), 
[TouchControl.Style](MicroEngine.Input.TouchControl.md\#MicroEngine\_Input\_TouchControl\_Style), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring), 
[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode)

## Constructors

### <a id="MicroEngine_Input_TouchStick__ctor_System_String_System_String_System_String_System_String_System_String_"></a> TouchStick\(string, string, string, string, string\)

An analog joystick: a 2D value with intensity ([Vector](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Vector_System_String_System_String_System_String_System_String_)) and, beyond [Threshold](MicroEngine.Input.TouchStick.md#MicroEngine_Input_TouchStick_Threshold),
the four direction actions as well, so digital code keeps working.

```csharp
public TouchStick(string Id = "stick", string Left = "left", string Right = "right", string Up = "up", string Down = "down")
```

#### Parameters

`Id` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Left` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Right` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Up` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Down` [string](https://learn.microsoft.com/dotnet/api/system.string)

## Properties

### <a id="MicroEngine_Input_TouchStick_DeadZone"></a> DeadZone

Radial dead zone as a fraction of the radius.

```csharp
public float DeadZone { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Input_TouchStick_Directions"></a> Directions

Whether the stick presses directions in eight sectors (the default) or four.

```csharp
public StickDirections Directions { get; init; }
```

#### Property Value

 [StickDirections](MicroEngine.Input.StickDirections.md)

### <a id="MicroEngine_Input_TouchStick_Down"></a> Down

```csharp
public string Down { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Input_TouchStick_EqualityContract"></a> EqualityContract

```csharp
protected override Type EqualityContract { get; }
```

#### Property Value

 [Type](https://learn.microsoft.com/dotnet/api/system.type)

### <a id="MicroEngine_Input_TouchStick_Left"></a> Left

```csharp
public string Left { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Input_TouchStick_Placement"></a> Placement

Whether the stick stays at its anchored position or appears under the thumb (floating, the default).

```csharp
public StickPlacement Placement { get; init; }
```

#### Property Value

 [StickPlacement](MicroEngine.Input.StickPlacement.md)

### <a id="MicroEngine_Input_TouchStick_Right"></a> Right

```csharp
public string Right { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Input_TouchStick_Threshold"></a> Threshold

Deflection beyond the dead zone at which direction actions go down.

```csharp
public float Threshold { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Input_TouchStick_Up"></a> Up

```csharp
public string Up { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Input_TouchStick_Zone"></a> Zone

For a floating stick, where a thumb may start it, as fractions (0..1) of the safe area. Default: the lower left.

```csharp
public RectF Zone { get; init; }
```

#### Property Value

 [RectF](MicroEngine.RectF.md)

## Methods

### <a id="MicroEngine_Input_TouchStick__Clone__"></a> <Clone\>$\(\)

```csharp
public override TouchStick <Clone>$()
```

#### Returns

 [TouchStick](MicroEngine.Input.TouchStick.md)

### <a id="MicroEngine_Input_TouchStick_Deconstruct_System_String__System_String__System_String__System_String__System_String__"></a> Deconstruct\(out string, out string, out string, out string, out string\)

```csharp
public void Deconstruct(out string Id, out string Left, out string Right, out string Up, out string Down)
```

#### Parameters

`Id` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Left` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Right` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Up` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Down` [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Input_TouchStick_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_TouchStick_Equals_MicroEngine_Input_TouchControl_"></a> Equals\(TouchControl?\)

```csharp
public override sealed bool Equals(TouchControl? other)
```

#### Parameters

`other` [TouchControl](MicroEngine.Input.TouchControl.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_TouchStick_Equals_MicroEngine_Input_TouchStick_"></a> Equals\(TouchStick?\)

```csharp
public bool Equals(TouchStick? other)
```

#### Parameters

`other` [TouchStick](MicroEngine.Input.TouchStick.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_TouchStick_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Input_TouchStick_PrintMembers_System_Text_StringBuilder_"></a> PrintMembers\(StringBuilder\)

```csharp
protected override bool PrintMembers(StringBuilder builder)
```

#### Parameters

`builder` [StringBuilder](https://learn.microsoft.com/dotnet/api/system.text.stringbuilder)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_TouchStick_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Input_TouchStick_op_Equality_MicroEngine_Input_TouchStick_MicroEngine_Input_TouchStick_"></a> operator ==\(TouchStick?, TouchStick?\)

```csharp
public static bool operator ==(TouchStick? left, TouchStick? right)
```

#### Parameters

`left` [TouchStick](MicroEngine.Input.TouchStick.md)?

`right` [TouchStick](MicroEngine.Input.TouchStick.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_TouchStick_op_Inequality_MicroEngine_Input_TouchStick_MicroEngine_Input_TouchStick_"></a> operator \!=\(TouchStick?, TouchStick?\)

```csharp
public static bool operator !=(TouchStick? left, TouchStick? right)
```

#### Parameters

`left` [TouchStick](MicroEngine.Input.TouchStick.md)?

`right` [TouchStick](MicroEngine.Input.TouchStick.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

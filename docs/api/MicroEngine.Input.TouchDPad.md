# Class TouchDPad

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.dll  
Guide: [Input](../manual/input.md)  

A d-pad: discrete directions with no analog value, for games where a precise press matters more than a smooth one
(platformers, grid movement, menus). The thumb anywhere around its centre presses the direction it points to.

```csharp
public sealed record TouchDPad : TouchControl, IEquatable<TouchControl>, IEquatable<TouchDPad>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[TouchControl](MicroEngine.Input.TouchControl.md) ← 
[TouchDPad](MicroEngine.Input.TouchDPad.md)

#### Implements

[IEquatable<TouchControl\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1), 
[IEquatable<TouchDPad\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

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

### <a id="MicroEngine_Input_TouchDPad__ctor_System_String_System_String_System_String_System_String_System_String_"></a> TouchDPad\(string, string, string, string, string\)

A d-pad: discrete directions with no analog value, for games where a precise press matters more than a smooth one
(platformers, grid movement, menus). The thumb anywhere around its centre presses the direction it points to.

```csharp
public TouchDPad(string Id = "dpad", string Left = "left", string Right = "right", string Up = "up", string Down = "down")
```

#### Parameters

`Id` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Left` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Right` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Up` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Down` [string](https://learn.microsoft.com/dotnet/api/system.string)

## Properties

### <a id="MicroEngine_Input_TouchDPad_DeadZone"></a> DeadZone

Centre area, as a fraction of the radius, where no direction is pressed.

```csharp
public float DeadZone { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Input_TouchDPad_Diagonals"></a> Diagonals

True: eight sectors (diagonals press two directions). False: four, only the dominant direction.

```csharp
public bool Diagonals { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_TouchDPad_Down"></a> Down

```csharp
public string Down { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Input_TouchDPad_EqualityContract"></a> EqualityContract

```csharp
protected override Type EqualityContract { get; }
```

#### Property Value

 [Type](https://learn.microsoft.com/dotnet/api/system.type)

### <a id="MicroEngine_Input_TouchDPad_Left"></a> Left

```csharp
public string Left { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Input_TouchDPad_Right"></a> Right

```csharp
public string Right { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Input_TouchDPad_Up"></a> Up

```csharp
public string Up { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Input_TouchDPad__Clone__"></a> <Clone\>$\(\)

```csharp
public override TouchDPad <Clone>$()
```

#### Returns

 [TouchDPad](MicroEngine.Input.TouchDPad.md)

### <a id="MicroEngine_Input_TouchDPad_Deconstruct_System_String__System_String__System_String__System_String__System_String__"></a> Deconstruct\(out string, out string, out string, out string, out string\)

```csharp
public void Deconstruct(out string Id, out string Left, out string Right, out string Up, out string Down)
```

#### Parameters

`Id` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Left` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Right` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Up` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Down` [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Input_TouchDPad_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_TouchDPad_Equals_MicroEngine_Input_TouchControl_"></a> Equals\(TouchControl?\)

```csharp
public override sealed bool Equals(TouchControl? other)
```

#### Parameters

`other` [TouchControl](MicroEngine.Input.TouchControl.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_TouchDPad_Equals_MicroEngine_Input_TouchDPad_"></a> Equals\(TouchDPad?\)

```csharp
public bool Equals(TouchDPad? other)
```

#### Parameters

`other` [TouchDPad](MicroEngine.Input.TouchDPad.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_TouchDPad_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Input_TouchDPad_PrintMembers_System_Text_StringBuilder_"></a> PrintMembers\(StringBuilder\)

```csharp
protected override bool PrintMembers(StringBuilder builder)
```

#### Parameters

`builder` [StringBuilder](https://learn.microsoft.com/dotnet/api/system.text.stringbuilder)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_TouchDPad_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Input_TouchDPad_op_Equality_MicroEngine_Input_TouchDPad_MicroEngine_Input_TouchDPad_"></a> operator ==\(TouchDPad?, TouchDPad?\)

```csharp
public static bool operator ==(TouchDPad? left, TouchDPad? right)
```

#### Parameters

`left` [TouchDPad](MicroEngine.Input.TouchDPad.md)?

`right` [TouchDPad](MicroEngine.Input.TouchDPad.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_TouchDPad_op_Inequality_MicroEngine_Input_TouchDPad_MicroEngine_Input_TouchDPad_"></a> operator \!=\(TouchDPad?, TouchDPad?\)

```csharp
public static bool operator !=(TouchDPad? left, TouchDPad? right)
```

#### Parameters

`left` [TouchDPad](MicroEngine.Input.TouchDPad.md)?

`right` [TouchDPad](MicroEngine.Input.TouchDPad.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

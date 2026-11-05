# Class TouchControl

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.Touch.dll  
Guide: [Input](../manual/input.md)  

One on-screen control of a [TouchLayout](MicroEngine.Input.TouchLayout.md). Positions and sizes are density-independent pixels (dp, 1/160
inch), so a control keeps its physical size on any screen; the layout shrinks everything when a screen is too small.

```csharp
public abstract record TouchControl : IEquatable<TouchControl>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[TouchControl](MicroEngine.Input.TouchControl.md)

#### Derived

[TouchButton](MicroEngine.Input.TouchButton.md), 
[TouchDPad](MicroEngine.Input.TouchDPad.md), 
[TouchStick](MicroEngine.Input.TouchStick.md)

#### Implements

[IEquatable<TouchControl\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Input_TouchControl__ctor_System_String_"></a> TouchControl\(string\)

One on-screen control of a [TouchLayout](MicroEngine.Input.TouchLayout.md). Positions and sizes are density-independent pixels (dp, 1/160
inch), so a control keeps its physical size on any screen; the layout shrinks everything when a screen is too small.

```csharp
protected TouchControl(string Id)
```

#### Parameters

`Id` [string](https://learn.microsoft.com/dotnet/api/system.string)

Names the control for [SetVisible](MicroEngine.Input.VirtualControls.md#MicroEngine_Input_VirtualControls_SetVisible_System_String_System_Boolean_), scripts and tests.

### <a id="MicroEngine_Input_TouchControl__ctor_MicroEngine_Input_TouchControl_"></a> TouchControl\(TouchControl\)

```csharp
protected TouchControl(TouchControl original)
```

#### Parameters

`original` [TouchControl](MicroEngine.Input.TouchControl.md)

## Properties

### <a id="MicroEngine_Input_TouchControl_Anchor"></a> Anchor

The corner or edge of the safe area the control is placed from.

```csharp
public TouchAnchor Anchor { get; init; }
```

#### Property Value

 [TouchAnchor](MicroEngine.Input.TouchAnchor.md)

### <a id="MicroEngine_Input_TouchControl_EqualityContract"></a> EqualityContract

```csharp
protected virtual Type EqualityContract { get; }
```

#### Property Value

 [Type](https://learn.microsoft.com/dotnet/api/system.type)

### <a id="MicroEngine_Input_TouchControl_Id"></a> Id

Names the control for [SetVisible](MicroEngine.Input.VirtualControls.md#MicroEngine_Input_VirtualControls_SetVisible_System_String_System_Boolean_), scripts and tests.

```csharp
public string Id { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Input_TouchControl_Offset"></a> Offset

Centre of the control from its anchor, in dp, pointing into the screen (x from the anchored side, y from the anchored edge).

```csharp
public Vector2 Offset { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Input_TouchControl_Radius"></a> Radius

Radius of the drawn control in dp; 0 uses the kind's default.

```csharp
public float Radius { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Input_TouchControl_Style"></a> Style

How this control looks (null: its kind's style in the layout). Changes the drawing only, never the hit area.

```csharp
public TouchStyle? Style { get; init; }
```

#### Property Value

 [TouchStyle](MicroEngine.Input.TouchStyle.md)?

### <a id="MicroEngine_Input_TouchControl_Visible"></a> Visible

Shown and touchable when the layout starts (see [SetVisible](MicroEngine.Input.VirtualControls.md#MicroEngine_Input_VirtualControls_SetVisible_System_String_System_Boolean_)).

```csharp
public bool Visible { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="MicroEngine_Input_TouchControl__Clone__"></a> <Clone\>$\(\)

```csharp
public abstract TouchControl <Clone>$()
```

#### Returns

 [TouchControl](MicroEngine.Input.TouchControl.md)

### <a id="MicroEngine_Input_TouchControl_Deconstruct_System_String__"></a> Deconstruct\(out string\)

```csharp
public void Deconstruct(out string Id)
```

#### Parameters

`Id` [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Input_TouchControl_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_TouchControl_Equals_MicroEngine_Input_TouchControl_"></a> Equals\(TouchControl?\)

```csharp
public virtual bool Equals(TouchControl? other)
```

#### Parameters

`other` [TouchControl](MicroEngine.Input.TouchControl.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_TouchControl_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Input_TouchControl_PrintMembers_System_Text_StringBuilder_"></a> PrintMembers\(StringBuilder\)

```csharp
protected virtual bool PrintMembers(StringBuilder builder)
```

#### Parameters

`builder` [StringBuilder](https://learn.microsoft.com/dotnet/api/system.text.stringbuilder)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_TouchControl_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Input_TouchControl_op_Equality_MicroEngine_Input_TouchControl_MicroEngine_Input_TouchControl_"></a> operator ==\(TouchControl?, TouchControl?\)

```csharp
public static bool operator ==(TouchControl? left, TouchControl? right)
```

#### Parameters

`left` [TouchControl](MicroEngine.Input.TouchControl.md)?

`right` [TouchControl](MicroEngine.Input.TouchControl.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_TouchControl_op_Inequality_MicroEngine_Input_TouchControl_MicroEngine_Input_TouchControl_"></a> operator \!=\(TouchControl?, TouchControl?\)

```csharp
public static bool operator !=(TouchControl? left, TouchControl? right)
```

#### Parameters

`left` [TouchControl](MicroEngine.Input.TouchControl.md)?

`right` [TouchControl](MicroEngine.Input.TouchControl.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

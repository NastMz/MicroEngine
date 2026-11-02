# Class TouchButton

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.dll  
Guide: [Input](../manual/input.md)  

A button that presses an action (and optionally more, as one key may: <code>Also = ["confirm"]</code> on a jump button).

```csharp
public sealed record TouchButton : TouchControl, IEquatable<TouchControl>, IEquatable<TouchButton>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[TouchControl](MicroEngine.Input.TouchControl.md) ← 
[TouchButton](MicroEngine.Input.TouchButton.md)

#### Implements

[IEquatable<TouchControl\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1), 
[IEquatable<TouchButton\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

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

### <a id="MicroEngine_Input_TouchButton__ctor_System_String_System_String_System_String_"></a> TouchButton\(string, string, string?\)

A button that presses an action (and optionally more, as one key may: <code>Also = ["confirm"]</code> on a jump button).

```csharp
public TouchButton(string Id, string Action, string? Label = null)
```

#### Parameters

`Id` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Action` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Label` [string](https://learn.microsoft.com/dotnet/api/system.string)?

Short text drawn on it ("A", "JUMP"); null draws none.

## Properties

### <a id="MicroEngine_Input_TouchButton_Action"></a> Action

```csharp
public string Action { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Input_TouchButton_Also"></a> Also

Further actions the button presses with [Action](MicroEngine.Input.TouchButton.md#MicroEngine_Input_TouchButton_Action), like a key bound to several actions.

```csharp
public IReadOnlyList<string> Also { get; init; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Input_TouchButton_EqualityContract"></a> EqualityContract

```csharp
protected override Type EqualityContract { get; }
```

#### Property Value

 [Type](https://learn.microsoft.com/dotnet/api/system.type)

### <a id="MicroEngine_Input_TouchButton_Label"></a> Label

Short text drawn on it ("A", "JUMP"); null draws none.

```csharp
public string? Label { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Input_TouchButton_Mode"></a> Mode

Whether the action is down while held (the default) or toggled by each tap.

```csharp
public TouchButtonMode Mode { get; init; }
```

#### Property Value

 [TouchButtonMode](MicroEngine.Input.TouchButtonMode.md)

## Methods

### <a id="MicroEngine_Input_TouchButton__Clone__"></a> <Clone\>$\(\)

```csharp
public override TouchButton <Clone>$()
```

#### Returns

 [TouchButton](MicroEngine.Input.TouchButton.md)

### <a id="MicroEngine_Input_TouchButton_Deconstruct_System_String__System_String__System_String__"></a> Deconstruct\(out string, out string, out string?\)

```csharp
public void Deconstruct(out string Id, out string Action, out string? Label)
```

#### Parameters

`Id` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Action` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Label` [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Input_TouchButton_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_TouchButton_Equals_MicroEngine_Input_TouchControl_"></a> Equals\(TouchControl?\)

```csharp
public override sealed bool Equals(TouchControl? other)
```

#### Parameters

`other` [TouchControl](MicroEngine.Input.TouchControl.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_TouchButton_Equals_MicroEngine_Input_TouchButton_"></a> Equals\(TouchButton?\)

```csharp
public bool Equals(TouchButton? other)
```

#### Parameters

`other` [TouchButton](MicroEngine.Input.TouchButton.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_TouchButton_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Input_TouchButton_PrintMembers_System_Text_StringBuilder_"></a> PrintMembers\(StringBuilder\)

```csharp
protected override bool PrintMembers(StringBuilder builder)
```

#### Parameters

`builder` [StringBuilder](https://learn.microsoft.com/dotnet/api/system.text.stringbuilder)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_TouchButton_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Input_TouchButton_op_Equality_MicroEngine_Input_TouchButton_MicroEngine_Input_TouchButton_"></a> operator ==\(TouchButton?, TouchButton?\)

```csharp
public static bool operator ==(TouchButton? left, TouchButton? right)
```

#### Parameters

`left` [TouchButton](MicroEngine.Input.TouchButton.md)?

`right` [TouchButton](MicroEngine.Input.TouchButton.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_TouchButton_op_Inequality_MicroEngine_Input_TouchButton_MicroEngine_Input_TouchButton_"></a> operator \!=\(TouchButton?, TouchButton?\)

```csharp
public static bool operator !=(TouchButton? left, TouchButton? right)
```

#### Parameters

`left` [TouchButton](MicroEngine.Input.TouchButton.md)?

`right` [TouchButton](MicroEngine.Input.TouchButton.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

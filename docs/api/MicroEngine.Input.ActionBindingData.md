# Class ActionBindingData

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.dll  
Guide: [Input](../manual/input.md)  

The bindings of one action as data: its controls as text ([ToString](MicroEngine.Input.InputControl.md#MicroEngine_Input_InputControl_ToString)), by enum name, so
saved bindings stay valid when the engine adds keys. Part of [InputBindingSet](MicroEngine.Input.InputBindingSet.md).

```csharp
public sealed class ActionBindingData
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ActionBindingData](MicroEngine.Input.ActionBindingData.md)

## Remarks

[Keys](MicroEngine.Input.ActionBindingData.md#MicroEngine_Input_ActionBindingData_Keys), [Buttons](MicroEngine.Input.ActionBindingData.md#MicroEngine_Input_ActionBindingData_Buttons), [Stick](MicroEngine.Input.ActionBindingData.md#MicroEngine_Input_ActionBindingData_Stick), [Axes](MicroEngine.Input.ActionBindingData.md#MicroEngine_Input_ActionBindingData_Axes) and [Chords](MicroEngine.Input.ActionBindingData.md#MicroEngine_Input_ActionBindingData_Chords) are the
first format (version 1), which had one list per kind of control and no place for the mouse. They are still read,
so files saved by earlier versions load; [ExportBindings](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_ExportBindings) fills [Controls](MicroEngine.Input.ActionBindingData.md#MicroEngine_Input_ActionBindingData_Controls) only.

## Constructors

### <a id="MicroEngine_Input_ActionBindingData__ctor"></a> ActionBindingData\(\)

```csharp
public ActionBindingData()
```

## Properties

### <a id="MicroEngine_Input_ActionBindingData_Action"></a> Action

The action.

```csharp
public string Action { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Input_ActionBindingData_Axes"></a> Axes

Version 1: axes as "RightTrigger+" or "LeftX-".

```csharp
public List<string> Axes { get; set; }
```

#### Property Value

 [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Input_ActionBindingData_Buttons"></a> Buttons

Version 1: gamepad buttons by name ("A", "RightShoulder").

```csharp
public List<string> Buttons { get; set; }
```

#### Property Value

 [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Input_ActionBindingData_Chords"></a> Chords

Version 1: key combinations as "LeftAlt|RightAlt+Enter": the modifiers (any one of them), then the key.

```csharp
public List<string> Chords { get; set; }
```

#### Property Value

 [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Input_ActionBindingData_Controls"></a> Controls

Every control bound to the action, as text: "Key:Space", "Pad:A", "Mouse:Secondary", "Wheel:Up",
"Stick:Right.Up", "Axis:RightTrigger+", "Chord:LeftAlt|RightAlt+Enter", "Motion:Left".

```csharp
public List<string> Controls { get; set; }
```

#### Property Value

 [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Input_ActionBindingData_Keys"></a> Keys

Version 1: keys by name ("A", "Space").

```csharp
public List<string> Keys { get; set; }
```

#### Property Value

 [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Input_ActionBindingData_Stick"></a> Stick

Version 1: a left-stick direction ("Left"...), or "None".

```csharp
public string Stick { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

</div>

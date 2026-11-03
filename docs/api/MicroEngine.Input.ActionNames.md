# Class ActionNames

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.dll  
Guide: [Input](../manual/input.md)  

The action names the engine's own parts read: menus and dialogs (<code>SelectionList</code>, <code>DialogueBox</code>), the
achievements screen, the default on-screen controls and prompts. They are defaults the game may rename: build the
map with [CreateDefault](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_CreateDefault_MicroEngine_Input_ActionNames_) or set [Names](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Names), and every engine
part follows.

```csharp
public sealed record ActionNames : IEquatable<ActionNames>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ActionNames](MicroEngine.Input.ActionNames.md)

#### Implements

[IEquatable<ActionNames\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Examples

```csharp
// The game calls its confirm action "accept"; engine menus follow.
protected override InputMap CreateInput() => InputMap.CreateDefault(ActionNames.Default with { Confirm = "accept" });
```

## Constructors

### <a id="MicroEngine_Input_ActionNames__ctor"></a> ActionNames\(\)

```csharp
public ActionNames()
```

## Properties

### <a id="MicroEngine_Input_ActionNames_Action"></a> Action

The game's secondary action ("action").

```csharp
public string Action { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Input_ActionNames_Cancel"></a> Cancel

Goes back ("cancel").

```csharp
public string Cancel { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Input_ActionNames_Confirm"></a> Confirm

Accepts a choice or advances a dialogue ("confirm").

```csharp
public string Confirm { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Input_ActionNames_Default"></a> Default

left, right, up, down, confirm, cancel, action and pause.

```csharp
public static ActionNames Default { get; }
```

#### Property Value

 [ActionNames](MicroEngine.Input.ActionNames.md)

### <a id="MicroEngine_Input_ActionNames_Down"></a> Down

Moves or selects down ("down").

```csharp
public string Down { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Input_ActionNames_Left"></a> Left

Moves or selects left ("left").

```csharp
public string Left { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Input_ActionNames_Pause"></a> Pause

Pauses ("pause").

```csharp
public string Pause { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Input_ActionNames_Right"></a> Right

Moves or selects right ("right").

```csharp
public string Right { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Input_ActionNames_Up"></a> Up

Moves or selects up ("up").

```csharp
public string Up { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Input_ActionNames__Clone__"></a> <Clone\>$\(\)

```csharp
public ActionNames <Clone>$()
```

#### Returns

 [ActionNames](MicroEngine.Input.ActionNames.md)

### <a id="MicroEngine_Input_ActionNames_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_ActionNames_Equals_MicroEngine_Input_ActionNames_"></a> Equals\(ActionNames?\)

```csharp
public bool Equals(ActionNames? other)
```

#### Parameters

`other` [ActionNames](MicroEngine.Input.ActionNames.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_ActionNames_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Input_ActionNames_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Input_ActionNames_op_Equality_MicroEngine_Input_ActionNames_MicroEngine_Input_ActionNames_"></a> operator ==\(ActionNames?, ActionNames?\)

```csharp
public static bool operator ==(ActionNames? left, ActionNames? right)
```

#### Parameters

`left` [ActionNames](MicroEngine.Input.ActionNames.md)?

`right` [ActionNames](MicroEngine.Input.ActionNames.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_ActionNames_op_Inequality_MicroEngine_Input_ActionNames_MicroEngine_Input_ActionNames_"></a> operator \!=\(ActionNames?, ActionNames?\)

```csharp
public static bool operator !=(ActionNames? left, ActionNames? right)
```

#### Parameters

`left` [ActionNames](MicroEngine.Input.ActionNames.md)?

`right` [ActionNames](MicroEngine.Input.ActionNames.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

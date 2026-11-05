# Class UiActions

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.UI](MicroEngine.UI.md)  
Assembly: MicroEngine.UI.dll  
Guide: [UI widgets](../manual/ui.md)  

The actions engine widgets answer to. A member left null (the default) is the input map's own name for it
([Names](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Names)), read at every update: a game that renames <code>confirm</code> with
[ActionNames](MicroEngine.Input.ActionNames.md) keeps working widgets, and a gameplay action that happens to be called <code>confirm</code>
never presses a button. Set a member to give the widgets an action of the game's own.

```csharp
public sealed record UiActions : IEquatable<UiActions>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[UiActions](MicroEngine.UI.UiActions.md)

#### Implements

[IEquatable<UiActions\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_UI_UiActions__ctor"></a> UiActions\(\)

```csharp
public UiActions()
```

## Properties

### <a id="MicroEngine_UI_UiActions_Cancel"></a> Cancel

Raises [Cancelled](MicroEngine.UI.UiRoot.md#MicroEngine_UI_UiRoot_Cancelled); null is [Cancel](MicroEngine.Input.ActionNames.md#MicroEngine_Input_ActionNames_Cancel).

```csharp
public string? Cancel { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_UI_UiActions_Confirm"></a> Confirm

Presses the focused button or submits the focused text field; null is [Confirm](MicroEngine.Input.ActionNames.md#MicroEngine_Input_ActionNames_Confirm).

```csharp
public string? Confirm { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_UI_UiActions_Default"></a> Default

The map's names for every action (all members null).

```csharp
public static UiActions Default { get; }
```

#### Property Value

 [UiActions](MicroEngine.UI.UiActions.md)

### <a id="MicroEngine_UI_UiActions_Down"></a> Down

Moves the focus down; null is [Down](MicroEngine.Input.ActionNames.md#MicroEngine_Input_ActionNames_Down).

```csharp
public string? Down { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_UI_UiActions_Left"></a> Left

Moves the focus left, or the caret of a text field; null is [Left](MicroEngine.Input.ActionNames.md#MicroEngine_Input_ActionNames_Left).

```csharp
public string? Left { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_UI_UiActions_Right"></a> Right

Moves the focus right, or the caret of a text field; null is [Right](MicroEngine.Input.ActionNames.md#MicroEngine_Input_ActionNames_Right).

```csharp
public string? Right { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_UI_UiActions_Up"></a> Up

Moves the focus up; null is [Up](MicroEngine.Input.ActionNames.md#MicroEngine_Input_ActionNames_Up).

```csharp
public string? Up { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

## Methods

### <a id="MicroEngine_UI_UiActions__Clone__"></a> <Clone\>$\(\)

```csharp
public UiActions <Clone>$()
```

#### Returns

 [UiActions](MicroEngine.UI.UiActions.md)

### <a id="MicroEngine_UI_UiActions_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_UiActions_Equals_MicroEngine_UI_UiActions_"></a> Equals\(UiActions?\)

```csharp
public bool Equals(UiActions? other)
```

#### Parameters

`other` [UiActions](MicroEngine.UI.UiActions.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_UiActions_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_UI_UiActions_NameOf_MicroEngine_UI_UiAction_MicroEngine_Input_ActionNames_"></a> NameOf\(UiAction, ActionNames\)

The action <code class="paramref">action</code> is read from on <code class="paramref">names</code>.

```csharp
public string NameOf(UiAction action, ActionNames names)
```

#### Parameters

`action` [UiAction](MicroEngine.UI.UiAction.md)

`names` [ActionNames](MicroEngine.Input.ActionNames.md)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_UI_UiActions_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_UI_UiActions_op_Equality_MicroEngine_UI_UiActions_MicroEngine_UI_UiActions_"></a> operator ==\(UiActions?, UiActions?\)

```csharp
public static bool operator ==(UiActions? left, UiActions? right)
```

#### Parameters

`left` [UiActions](MicroEngine.UI.UiActions.md)?

`right` [UiActions](MicroEngine.UI.UiActions.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_UiActions_op_Inequality_MicroEngine_UI_UiActions_MicroEngine_UI_UiActions_"></a> operator \!=\(UiActions?, UiActions?\)

```csharp
public static bool operator !=(UiActions? left, UiActions? right)
```

#### Parameters

`left` [UiActions](MicroEngine.UI.UiActions.md)?

`right` [UiActions](MicroEngine.UI.UiActions.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

# Interface IActionSource

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.dll  
Guide: [Input](../manual/input.md)  

A source of actions rather than of keys: on-screen touch controls, a network peer, an accessibility switch or an AI.
Added with [AddSource](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_AddSource_MicroEngine_Input_IActionSource_), it is read every step together with the keyboard and gamepad, so a game
cannot tell which produced an action.

```csharp
public interface IActionSource
```

## Properties

### <a id="MicroEngine_Input_IActionSource_Actions"></a> Actions

Actions this source may press that need no key binding (read even if the map does not bind them).

```csharp
IEnumerable<string> Actions { get; }
```

#### Property Value

 [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Input_IActionSource_Device"></a> Device

The kind of device this source stands for, reported by [LastDevice](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_LastDevice).

```csharp
InputDevice Device { get; }
```

#### Property Value

 [InputDevice](MicroEngine.Input.InputDevice.md)

## Methods

### <a id="MicroEngine_Input_IActionSource_IsDown_System_String_"></a> IsDown\(string\)

Whether the action is down (digital).

```csharp
bool IsDown(string action)
```

#### Parameters

`action` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_IActionSource_Value_System_String_"></a> Value\(string\)

The action's strength, 0..1 (analog). Digital sources keep the default.

```csharp
float Value(string action)
```

#### Parameters

`action` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

</div>

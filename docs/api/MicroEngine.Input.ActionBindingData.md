# Class ActionBindingData

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.dll  
Guide: [Input](../manual/input.md)  

The bindings of one action as data, by name (keys, buttons and axes are their enum names), so saved bindings stay
valid when the engine adds keys. Part of [InputBindingSet](MicroEngine.Input.InputBindingSet.md).

```csharp
public sealed class ActionBindingData
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ActionBindingData](MicroEngine.Input.ActionBindingData.md)

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

Axes as "RightTrigger+" or "LeftX-".

```csharp
public List<string> Axes { get; set; }
```

#### Property Value

 [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Input_ActionBindingData_Buttons"></a> Buttons

Gamepad buttons by name ("A", "RightShoulder").

```csharp
public List<string> Buttons { get; set; }
```

#### Property Value

 [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Input_ActionBindingData_Keys"></a> Keys

Keys by name ("A", "Space").

```csharp
public List<string> Keys { get; set; }
```

#### Property Value

 [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Input_ActionBindingData_Stick"></a> Stick

A left-stick direction ("Left"...), or "None".

```csharp
public string Stick { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

</div>

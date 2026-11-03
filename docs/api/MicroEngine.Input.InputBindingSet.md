# Class InputBindingSet

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.dll  
Guide: [Input](../manual/input.md)  

Every action's bindings as data, for a rebinding screen to save and load: through [SaveBindings](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_SaveBindings_MicroEngine_Platform_IStorage_System_String_)
and [LoadBindings](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_LoadBindings_MicroEngine_Platform_IStorage_System_String_) (the platform's storage), or as a value a game keeps in its own save
(<code>SaveStore.Save("bindings", map.ExportBindings())</code>; the classes have public properties).

```csharp
public sealed class InputBindingSet
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[InputBindingSet](MicroEngine.Input.InputBindingSet.md)

## Constructors

### <a id="MicroEngine_Input_InputBindingSet__ctor"></a> InputBindingSet\(\)

```csharp
public InputBindingSet()
```

## Properties

### <a id="MicroEngine_Input_InputBindingSet_Actions"></a> Actions

The actions, in ordinal order of their names.

```csharp
public List<ActionBindingData> Actions { get; set; }
```

#### Property Value

 [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[ActionBindingData](MicroEngine.Input.ActionBindingData.md)\>

### <a id="MicroEngine_Input_InputBindingSet_Version"></a> Version

The format version (1).

```csharp
public int Version { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Input_InputBindingSet_FromJson_System_String_"></a> FromJson\(string\)

Reads a set written by [ToJson](MicroEngine.Input.InputBindingSet.md#MicroEngine_Input_InputBindingSet_ToJson).

```csharp
public static InputBindingSet FromJson(string json)
```

#### Parameters

`json` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [InputBindingSet](MicroEngine.Input.InputBindingSet.md)

#### Exceptions

 [FormatException](https://learn.microsoft.com/dotnet/api/system.formatexception)

The text is not a binding set.

### <a id="MicroEngine_Input_InputBindingSet_ToJson"></a> ToJson\(\)

The set as JSON text.

```csharp
public string ToJson()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

</div>

# Class ActionBindings

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.dll  
Guide: [Input](../manual/input.md)  

What an action is bound to, for display or a rebinding screen ([GetBindings](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_GetBindings_System_String_)).

```csharp
public sealed record ActionBindings : IEquatable<ActionBindings>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ActionBindings](MicroEngine.Input.ActionBindings.md)

#### Implements

[IEquatable<ActionBindings\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Input_ActionBindings__ctor_System_Collections_Generic_IReadOnlyList_MicroEngine_Input_Key__System_Collections_Generic_IReadOnlyList_MicroEngine_Input_PadButton__MicroEngine_Input_InputMap_StickDirection_System_Collections_Generic_IReadOnlyList_System_ValueTuple_MicroEngine_Input_PadAxis_System_Boolean___"></a> ActionBindings\(IReadOnlyList<Key\>, IReadOnlyList<PadButton\>, StickDirection, IReadOnlyList<\(PadAxis Axis, bool Positive\)\>\)

What an action is bound to, for display or a rebinding screen ([GetBindings](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_GetBindings_System_String_)).

```csharp
public ActionBindings(IReadOnlyList<Key> Keys, IReadOnlyList<PadButton> Buttons, InputMap.StickDirection Stick, IReadOnlyList<(PadAxis Axis, bool Positive)> Axes)
```

#### Parameters

`Keys` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[Key](MicroEngine.Input.Key.md)\>

`Buttons` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[PadButton](MicroEngine.Input.PadButton.md)\>

`Stick` [InputMap](MicroEngine.Input.InputMap.md).[StickDirection](MicroEngine.Input.InputMap.StickDirection.md)

`Axes` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<\([PadAxis](MicroEngine.Input.PadAxis.md) [Axis](https://learn.microsoft.com/dotnet/api/system.valuetuple\-microengine.input.padaxis,system.boolean\-.axis), [bool](https://learn.microsoft.com/dotnet/api/system.boolean) [Positive](https://learn.microsoft.com/dotnet/api/system.valuetuple\-microengine.input.padaxis,system.boolean\-.positive)\)\>

## Properties

### <a id="MicroEngine_Input_ActionBindings_Axes"></a> Axes

```csharp
public IReadOnlyList<(PadAxis Axis, bool Positive)> Axes { get; init; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<\([PadAxis](MicroEngine.Input.PadAxis.md) [Axis](https://learn.microsoft.com/dotnet/api/system.valuetuple\-microengine.input.padaxis,system.boolean\-.axis), [bool](https://learn.microsoft.com/dotnet/api/system.boolean) [Positive](https://learn.microsoft.com/dotnet/api/system.valuetuple\-microengine.input.padaxis,system.boolean\-.positive)\)\>

### <a id="MicroEngine_Input_ActionBindings_Buttons"></a> Buttons

```csharp
public IReadOnlyList<PadButton> Buttons { get; init; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[PadButton](MicroEngine.Input.PadButton.md)\>

### <a id="MicroEngine_Input_ActionBindings_Keys"></a> Keys

```csharp
public IReadOnlyList<Key> Keys { get; init; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[Key](MicroEngine.Input.Key.md)\>

### <a id="MicroEngine_Input_ActionBindings_Stick"></a> Stick

```csharp
public InputMap.StickDirection Stick { get; init; }
```

#### Property Value

 [InputMap](MicroEngine.Input.InputMap.md).[StickDirection](MicroEngine.Input.InputMap.StickDirection.md)

## Methods

### <a id="MicroEngine_Input_ActionBindings__Clone__"></a> <Clone\>$\(\)

```csharp
public ActionBindings <Clone>$()
```

#### Returns

 [ActionBindings](MicroEngine.Input.ActionBindings.md)

### <a id="MicroEngine_Input_ActionBindings_Deconstruct_System_Collections_Generic_IReadOnlyList_MicroEngine_Input_Key___System_Collections_Generic_IReadOnlyList_MicroEngine_Input_PadButton___MicroEngine_Input_InputMap_StickDirection__System_Collections_Generic_IReadOnlyList_System_ValueTuple_MicroEngine_Input_PadAxis_System_Boolean____"></a> Deconstruct\(out IReadOnlyList<Key\>, out IReadOnlyList<PadButton\>, out StickDirection, out IReadOnlyList<\(PadAxis Axis, bool Positive\)\>\)

```csharp
public void Deconstruct(out IReadOnlyList<Key> Keys, out IReadOnlyList<PadButton> Buttons, out InputMap.StickDirection Stick, out IReadOnlyList<(PadAxis Axis, bool Positive)> Axes)
```

#### Parameters

`Keys` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[Key](MicroEngine.Input.Key.md)\>

`Buttons` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[PadButton](MicroEngine.Input.PadButton.md)\>

`Stick` [InputMap](MicroEngine.Input.InputMap.md).[StickDirection](MicroEngine.Input.InputMap.StickDirection.md)

`Axes` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<\([PadAxis](MicroEngine.Input.PadAxis.md) [Axis](https://learn.microsoft.com/dotnet/api/system.valuetuple\-microengine.input.padaxis,system.boolean\-.axis), [bool](https://learn.microsoft.com/dotnet/api/system.boolean) [Positive](https://learn.microsoft.com/dotnet/api/system.valuetuple\-microengine.input.padaxis,system.boolean\-.positive)\)\>

### <a id="MicroEngine_Input_ActionBindings_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_ActionBindings_Equals_MicroEngine_Input_ActionBindings_"></a> Equals\(ActionBindings?\)

```csharp
public bool Equals(ActionBindings? other)
```

#### Parameters

`other` [ActionBindings](MicroEngine.Input.ActionBindings.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_ActionBindings_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Input_ActionBindings_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Input_ActionBindings_op_Equality_MicroEngine_Input_ActionBindings_MicroEngine_Input_ActionBindings_"></a> operator ==\(ActionBindings?, ActionBindings?\)

```csharp
public static bool operator ==(ActionBindings? left, ActionBindings? right)
```

#### Parameters

`left` [ActionBindings](MicroEngine.Input.ActionBindings.md)?

`right` [ActionBindings](MicroEngine.Input.ActionBindings.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_ActionBindings_op_Inequality_MicroEngine_Input_ActionBindings_MicroEngine_Input_ActionBindings_"></a> operator \!=\(ActionBindings?, ActionBindings?\)

```csharp
public static bool operator !=(ActionBindings? left, ActionBindings? right)
```

#### Parameters

`left` [ActionBindings](MicroEngine.Input.ActionBindings.md)?

`right` [ActionBindings](MicroEngine.Input.ActionBindings.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

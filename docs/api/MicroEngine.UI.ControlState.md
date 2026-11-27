# Struct ControlState

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.UI](MicroEngine.UI.md)  
Assembly: MicroEngine.UI.dll  
Guide: [UI widgets](../manual/ui.md)  

The state a control is in when it is drawn, as its skin receives it ([ControlSkin](MicroEngine.UI.ControlSkin.md)).

```csharp
public readonly struct ControlState : IEquatable<ControlState>
```

#### Implements

[IEquatable<ControlState\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_UI_ControlState__ctor_MicroEngine_RectF_System_String_System_Boolean_System_Boolean_System_Boolean_System_Boolean_System_Single_"></a> ControlState\(RectF, string, bool, bool, bool, bool, float\)

The state a control is in when it is drawn, as its skin receives it ([ControlSkin](MicroEngine.UI.ControlSkin.md)).

```csharp
public ControlState(RectF Bounds, string Text, bool Enabled, bool Focused, bool Hovered, bool Pressed, float FocusTime)
```

#### Parameters

`Bounds` [RectF](MicroEngine.RectF.md)

Where the control is, in the root's units (canvas pixels for a root on the canvas).

`Text` [string](https://learn.microsoft.com/dotnet/api/system.string)

The control's text in the language in use: a button's label; empty for a panel.

`Enabled` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether the control and every widget around it are enabled.

`Focused` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether the control has the focus: the cursor of a menu is on it.

`Hovered` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether the pointer is over the control.

`Pressed` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether a pointer press that started on the control is held.

`FocusTime` [float](https://learn.microsoft.com/dotnet/api/system.single)

Seconds the control has had the focus, on the root's clock; zero without it. A cursor that bobs from where it
starts, or a highlight that grows in, reads it; [Time](MicroEngine.UI.UiContext.md#MicroEngine_UI_UiContext_Time) is the clock that never starts again.

## Properties

### <a id="MicroEngine_UI_ControlState_Bounds"></a> Bounds

Where the control is, in the root's units (canvas pixels for a root on the canvas).

```csharp
public RectF Bounds { get; init; }
```

#### Property Value

 [RectF](MicroEngine.RectF.md)

### <a id="MicroEngine_UI_ControlState_Enabled"></a> Enabled

Whether the control and every widget around it are enabled.

```csharp
public bool Enabled { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_ControlState_FocusTime"></a> FocusTime

Seconds the control has had the focus, on the root's clock; zero without it. A cursor that bobs from where it
starts, or a highlight that grows in, reads it; [Time](MicroEngine.UI.UiContext.md#MicroEngine_UI_UiContext_Time) is the clock that never starts again.

```csharp
public float FocusTime { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_UI_ControlState_Focused"></a> Focused

Whether the control has the focus: the cursor of a menu is on it.

```csharp
public bool Focused { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_ControlState_Hovered"></a> Hovered

Whether the pointer is over the control.

```csharp
public bool Hovered { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_ControlState_Pressed"></a> Pressed

Whether a pointer press that started on the control is held.

```csharp
public bool Pressed { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_ControlState_Text"></a> Text

The control's text in the language in use: a button's label; empty for a panel.

```csharp
public string Text { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_UI_ControlState_Deconstruct_MicroEngine_RectF__System_String__System_Boolean__System_Boolean__System_Boolean__System_Boolean__System_Single__"></a> Deconstruct\(out RectF, out string, out bool, out bool, out bool, out bool, out float\)

```csharp
public void Deconstruct(out RectF Bounds, out string Text, out bool Enabled, out bool Focused, out bool Hovered, out bool Pressed, out float FocusTime)
```

#### Parameters

`Bounds` [RectF](MicroEngine.RectF.md)

`Text` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Enabled` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`Focused` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`Hovered` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`Pressed` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`FocusTime` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_UI_ControlState_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_ControlState_Equals_MicroEngine_UI_ControlState_"></a> Equals\(ControlState\)

```csharp
public bool Equals(ControlState other)
```

#### Parameters

`other` [ControlState](MicroEngine.UI.ControlState.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_ControlState_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_UI_ControlState_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_UI_ControlState_op_Equality_MicroEngine_UI_ControlState_MicroEngine_UI_ControlState_"></a> operator ==\(ControlState, ControlState\)

```csharp
public static bool operator ==(ControlState left, ControlState right)
```

#### Parameters

`left` [ControlState](MicroEngine.UI.ControlState.md)

`right` [ControlState](MicroEngine.UI.ControlState.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_ControlState_op_Inequality_MicroEngine_UI_ControlState_MicroEngine_UI_ControlState_"></a> operator \!=\(ControlState, ControlState\)

```csharp
public static bool operator !=(ControlState left, ControlState right)
```

#### Parameters

`left` [ControlState](MicroEngine.UI.ControlState.md)

`right` [ControlState](MicroEngine.UI.ControlState.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

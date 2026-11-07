# Struct FocusChange

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.UI](MicroEngine.UI.md)  
Assembly: MicroEngine.UI.dll  
Guide: [UI widgets](../manual/ui.md)  

A move of the focus, as [FocusChanged](MicroEngine.UI.UiRoot.md#MicroEngine_UI_UiRoot_FocusChanged) reports it.

```csharp
public readonly struct FocusChange : IEquatable<FocusChange>
```

#### Implements

[IEquatable<FocusChange\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_UI_FocusChange__ctor_MicroEngine_UI_Widget_MicroEngine_UI_Widget_"></a> FocusChange\(Widget?, Widget?\)

A move of the focus, as [FocusChanged](MicroEngine.UI.UiRoot.md#MicroEngine_UI_UiRoot_FocusChanged) reports it.

```csharp
public FocusChange(Widget? From, Widget? To)
```

#### Parameters

`From` [Widget](MicroEngine.UI.Widget.md)?

The widget that had the focus, or null.

`To` [Widget](MicroEngine.UI.Widget.md)?

The widget that has it now, or null.

## Properties

### <a id="MicroEngine_UI_FocusChange_From"></a> From

The widget that had the focus, or null.

```csharp
public Widget? From { get; init; }
```

#### Property Value

 [Widget](MicroEngine.UI.Widget.md)?

### <a id="MicroEngine_UI_FocusChange_To"></a> To

The widget that has it now, or null.

```csharp
public Widget? To { get; init; }
```

#### Property Value

 [Widget](MicroEngine.UI.Widget.md)?

## Methods

### <a id="MicroEngine_UI_FocusChange_Deconstruct_MicroEngine_UI_Widget__MicroEngine_UI_Widget__"></a> Deconstruct\(out Widget?, out Widget?\)

```csharp
public void Deconstruct(out Widget? From, out Widget? To)
```

#### Parameters

`From` [Widget](MicroEngine.UI.Widget.md)?

`To` [Widget](MicroEngine.UI.Widget.md)?

### <a id="MicroEngine_UI_FocusChange_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_FocusChange_Equals_MicroEngine_UI_FocusChange_"></a> Equals\(FocusChange\)

```csharp
public bool Equals(FocusChange other)
```

#### Parameters

`other` [FocusChange](MicroEngine.UI.FocusChange.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_FocusChange_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_UI_FocusChange_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_UI_FocusChange_op_Equality_MicroEngine_UI_FocusChange_MicroEngine_UI_FocusChange_"></a> operator ==\(FocusChange, FocusChange\)

```csharp
public static bool operator ==(FocusChange left, FocusChange right)
```

#### Parameters

`left` [FocusChange](MicroEngine.UI.FocusChange.md)

`right` [FocusChange](MicroEngine.UI.FocusChange.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_FocusChange_op_Inequality_MicroEngine_UI_FocusChange_MicroEngine_UI_FocusChange_"></a> operator \!=\(FocusChange, FocusChange\)

```csharp
public static bool operator !=(FocusChange left, FocusChange right)
```

#### Parameters

`left` [FocusChange](MicroEngine.UI.FocusChange.md)

`right` [FocusChange](MicroEngine.UI.FocusChange.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

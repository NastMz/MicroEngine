# Struct TextArgument

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Localization](MicroEngine.Localization.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Graphics](../manual/rendering.md)  

A named value a message shows or chooses by: <code>("count", 3)</code>, <code>("who", "Ana")</code>. Tuples of a name and a
string or a number convert to it, so calls read <code>Format("gems", ("count", gems))</code>; nothing is boxed.

```csharp
public readonly struct TextArgument : IEquatable<TextArgument>
```

#### Implements

[IEquatable<TextArgument\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Localization_TextArgument__ctor_System_String_System_String_"></a> TextArgument\(string, string?\)

A text value.

```csharp
public TextArgument(string name, string? text)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Localization_TextArgument__ctor_System_String_System_Double_"></a> TextArgument\(string, double\)

A number: it is written without grouping (<code>1500</code>, <code>0.5</code>) and chooses plural forms.

```csharp
public TextArgument(string name, double number)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`number` [double](https://learn.microsoft.com/dotnet/api/system.double)

## Properties

### <a id="MicroEngine_Localization_TextArgument_IsNumber"></a> IsNumber

Whether the value is a number.

```csharp
public bool IsNumber { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Localization_TextArgument_Name"></a> Name

The name the message uses between braces.

```csharp
public string Name { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Localization_TextArgument_Number"></a> Number

The number, when [IsNumber](MicroEngine.Localization.TextArgument.md#MicroEngine_Localization_TextArgument_IsNumber).

```csharp
public double Number { get; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Localization_TextArgument_Text"></a> Text

The text, or null when the value is a number.

```csharp
public string? Text { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

## Methods

### <a id="MicroEngine_Localization_TextArgument_Equals_MicroEngine_Localization_TextArgument_"></a> Equals\(TextArgument\)

```csharp
public bool Equals(TextArgument other)
```

#### Parameters

`other` [TextArgument](MicroEngine.Localization.TextArgument.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Localization_TextArgument_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Localization_TextArgument_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Localization_TextArgument_ToString"></a> ToString\(\)

The value as a message shows it: the text, or the number with up to three decimals and no grouping.

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Localization_TextArgument_op_Equality_MicroEngine_Localization_TextArgument_MicroEngine_Localization_TextArgument_"></a> operator ==\(TextArgument, TextArgument\)

Whether two arguments have the same name and value.

```csharp
public static bool operator ==(TextArgument left, TextArgument right)
```

#### Parameters

`left` [TextArgument](MicroEngine.Localization.TextArgument.md)

`right` [TextArgument](MicroEngine.Localization.TextArgument.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Localization_TextArgument_op_Implicit_System_ValueTuple_System_String_System_String___MicroEngine_Localization_TextArgument"></a> implicit operator TextArgument\(\(string Name, string? Text\)\)

A text value.

```csharp
public static implicit operator TextArgument((string Name, string? Text) value)
```

#### Parameters

`value` \([string](https://learn.microsoft.com/dotnet/api/system.string) [Name](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.string,system.string\-.name), [string](https://learn.microsoft.com/dotnet/api/system.string)? [Text](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.string,system.string\-.text)\)

#### Returns

 [TextArgument](MicroEngine.Localization.TextArgument.md)

### <a id="MicroEngine_Localization_TextArgument_op_Implicit_System_ValueTuple_System_String_System_Int32___MicroEngine_Localization_TextArgument"></a> implicit operator TextArgument\(\(string Name, int Number\)\)

A whole number.

```csharp
public static implicit operator TextArgument((string Name, int Number) value)
```

#### Parameters

`value` \([string](https://learn.microsoft.com/dotnet/api/system.string) [Name](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.string,system.int32\-.name), [int](https://learn.microsoft.com/dotnet/api/system.int32) [Number](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.string,system.int32\-.number)\)

#### Returns

 [TextArgument](MicroEngine.Localization.TextArgument.md)

### <a id="MicroEngine_Localization_TextArgument_op_Implicit_System_ValueTuple_System_String_System_Int64___MicroEngine_Localization_TextArgument"></a> implicit operator TextArgument\(\(string Name, long Number\)\)

A whole number.

```csharp
public static implicit operator TextArgument((string Name, long Number) value)
```

#### Parameters

`value` \([string](https://learn.microsoft.com/dotnet/api/system.string) [Name](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.string,system.int64\-.name), [long](https://learn.microsoft.com/dotnet/api/system.int64) [Number](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.string,system.int64\-.number)\)

#### Returns

 [TextArgument](MicroEngine.Localization.TextArgument.md)

### <a id="MicroEngine_Localization_TextArgument_op_Implicit_System_ValueTuple_System_String_System_Single___MicroEngine_Localization_TextArgument"></a> implicit operator TextArgument\(\(string Name, float Number\)\)

A number.

```csharp
public static implicit operator TextArgument((string Name, float Number) value)
```

#### Parameters

`value` \([string](https://learn.microsoft.com/dotnet/api/system.string) [Name](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.string,system.single\-.name), [float](https://learn.microsoft.com/dotnet/api/system.single) [Number](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.string,system.single\-.number)\)

#### Returns

 [TextArgument](MicroEngine.Localization.TextArgument.md)

### <a id="MicroEngine_Localization_TextArgument_op_Implicit_System_ValueTuple_System_String_System_Double___MicroEngine_Localization_TextArgument"></a> implicit operator TextArgument\(\(string Name, double Number\)\)

A number.

```csharp
public static implicit operator TextArgument((string Name, double Number) value)
```

#### Parameters

`value` \([string](https://learn.microsoft.com/dotnet/api/system.string) [Name](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.string,system.double\-.name), [double](https://learn.microsoft.com/dotnet/api/system.double) [Number](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.string,system.double\-.number)\)

#### Returns

 [TextArgument](MicroEngine.Localization.TextArgument.md)

### <a id="MicroEngine_Localization_TextArgument_op_Inequality_MicroEngine_Localization_TextArgument_MicroEngine_Localization_TextArgument_"></a> operator \!=\(TextArgument, TextArgument\)

Whether two arguments differ in name or value.

```csharp
public static bool operator !=(TextArgument left, TextArgument right)
```

#### Parameters

`left` [TextArgument](MicroEngine.Localization.TextArgument.md)

`right` [TextArgument](MicroEngine.Localization.TextArgument.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

# Struct LocalizedText

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Localization](MicroEngine.Localization.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Graphics](../manual/rendering.md)  

A text named by its key, with the values it shows: what a widget keeps so its text follows the language
(<code>new Label(new LocalizedText("hud.gems", ("count", gems)))</code>). [Format](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_Format_System_String_System_ReadOnlySpan_MicroEngine_Localization_TextArgument__)
gives the text in the language in use.

```csharp
public readonly struct LocalizedText : IEquatable<LocalizedText>
```

#### Implements

[IEquatable<LocalizedText\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Localization_LocalizedText__ctor_System_String_MicroEngine_Localization_TextArgument___"></a> LocalizedText\(string, params TextArgument\[\]\)

The text of <code class="paramref">key</code> with <code class="paramref">arguments</code>.

```csharp
public LocalizedText(string key, params TextArgument[] arguments)
```

#### Parameters

`key` [string](https://learn.microsoft.com/dotnet/api/system.string)

`arguments` [TextArgument](MicroEngine.Localization.TextArgument.md)\[\]

## Properties

### <a id="MicroEngine_Localization_LocalizedText_Arguments"></a> Arguments

The values the message shows or chooses by.

```csharp
public ReadOnlySpan<TextArgument> Arguments { get; }
```

#### Property Value

 [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[TextArgument](MicroEngine.Localization.TextArgument.md)\>

### <a id="MicroEngine_Localization_LocalizedText_Key"></a> Key

The key in the string tables.

```csharp
public string Key { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Localization_LocalizedText_Equals_MicroEngine_Localization_LocalizedText_"></a> Equals\(LocalizedText\)

```csharp
public bool Equals(LocalizedText other)
```

#### Parameters

`other` [LocalizedText](MicroEngine.Localization.LocalizedText.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Localization_LocalizedText_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Localization_LocalizedText_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Localization_LocalizedText_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Localization_LocalizedText_op_Equality_MicroEngine_Localization_LocalizedText_MicroEngine_Localization_LocalizedText_"></a> operator ==\(LocalizedText, LocalizedText\)

Whether two texts have the same key and values.

```csharp
public static bool operator ==(LocalizedText left, LocalizedText right)
```

#### Parameters

`left` [LocalizedText](MicroEngine.Localization.LocalizedText.md)

`right` [LocalizedText](MicroEngine.Localization.LocalizedText.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Localization_LocalizedText_op_Explicit_System_String__MicroEngine_Localization_LocalizedText"></a> explicit operator LocalizedText\(string\)

A key alone converts to a text without arguments.

```csharp
public static explicit operator LocalizedText(string key)
```

#### Parameters

`key` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [LocalizedText](MicroEngine.Localization.LocalizedText.md)

### <a id="MicroEngine_Localization_LocalizedText_op_Inequality_MicroEngine_Localization_LocalizedText_MicroEngine_Localization_LocalizedText_"></a> operator \!=\(LocalizedText, LocalizedText\)

Whether two texts differ in key or values.

```csharp
public static bool operator !=(LocalizedText left, LocalizedText right)
```

#### Parameters

`left` [LocalizedText](MicroEngine.Localization.LocalizedText.md)

`right` [LocalizedText](MicroEngine.Localization.LocalizedText.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

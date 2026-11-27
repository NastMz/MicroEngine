# Struct InterpolatedText

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

Text built from an interpolated string without making a string: where a font, a sprite atlas or another engine call
takes an [InterpolatedText](MicroEngine.Graphics.InterpolatedText.md), <code>$"{hp}/{max}"</code> is written into a buffer the engine reuses, drawn or
looked up, and the buffer goes back, so a number shown every frame makes no garbage.

```csharp
public ref struct InterpolatedText
```

## Examples

```csharp
Font.Draw(r, $"{hp}/{maxHp}", new Vector2(8, 8), Color.White);
Font.DrawCentered(r, $"{minutes}:{seconds:00}", new Vector2(160, 12), Color.White);
r.Draw(atlas[$"water{mask}_{frame}"], at);
```

## Remarks

<p>
The text is exactly what the interpolated string gives as a `String`: the same formats
(<code>{time:0.0}</code>), alignment (<code>{score,6}</code>) and culture. Written without allocating: whole numbers and
numbers with a fraction of every built-in size (<code>decimal</code> included), <code>int?</code>, <code>long?</code>,
<code>float?</code> and <code>double?</code>, characters, booleans, strings, spans of characters, enums written by name,
`TimeSpan`, `DateTime` and `Guid`. What allocates: an enum the first time each
of its values is shown (its name is kept afterwards) or written with a format (<code>{suit:D}</code>), a few bytes; any
other value type (a struct of the game's own, a nullable of another type), boxed to be formatted; a value held as
`Object`; and a type that only has <code>ToString</code>, which is written through it.
</p>
<p>
The compiler makes it: write the interpolated string where the parameter is, and do not keep the value. A method
that receives one reads [Text](MicroEngine.Graphics.InterpolatedText.md#MicroEngine_Graphics_InterpolatedText_Text) and then calls [Dispose](MicroEngine.Graphics.InterpolatedText.md#MicroEngine_Graphics_InterpolatedText_Dispose) once. A text that was already used
refuses to be read again (`ObjectDisposedException`).
</p>
<p>
An interpolated string that holds an <code>await</code> or a <code>dynamic</code> value cannot be written into a buffer on the
stack and does not compile here: put the value in a local first (<code>var n = await Count(); Draw(r, $"{n}", ...)</code>).
</p>

## Constructors

### <a id="MicroEngine_Graphics_InterpolatedText__ctor_System_Int32_System_Int32_"></a> InterpolatedText\(int, int\)

Called by the compiler: <code class="paramref">literalLength</code> characters of fixed text and <code class="paramref">formattedCount</code> values.

```csharp
public InterpolatedText(int literalLength, int formattedCount)
```

#### Parameters

`literalLength` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`formattedCount` [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Properties

### <a id="MicroEngine_Graphics_InterpolatedText_Text"></a> Text

The text written so far. Valid until [Dispose](MicroEngine.Graphics.InterpolatedText.md#MicroEngine_Graphics_InterpolatedText_Dispose).

```csharp
public readonly ReadOnlySpan<char> Text { get; }
```

#### Property Value

 [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[char](https://learn.microsoft.com/dotnet/api/system.char)\>

#### Exceptions

 [ObjectDisposedException](https://learn.microsoft.com/dotnet/api/system.objectdisposedexception)

The text was already used.

## Methods

### <a id="MicroEngine_Graphics_InterpolatedText_AppendFormatted_System_String_"></a> AppendFormatted\(string?\)

Called by the compiler for a string value; null writes nothing.

```csharp
public void AppendFormatted(string? value)
```

#### Parameters

`value` [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Graphics_InterpolatedText_AppendFormatted_System_String_System_Int32_System_String_"></a> AppendFormatted\(string?, int, string?\)

Called by the compiler for a string with a width (<code>{name,8}</code>).

```csharp
public void AppendFormatted(string? value, int alignment, string? format = null)
```

#### Parameters

`value` [string](https://learn.microsoft.com/dotnet/api/system.string)?

`alignment` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`format` [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Graphics_InterpolatedText_AppendFormatted_System_ReadOnlySpan_System_Char__"></a> AppendFormatted\(ReadOnlySpan<char\>\)

Called by the compiler for a span of characters.

```csharp
public void AppendFormatted(ReadOnlySpan<char> value)
```

#### Parameters

`value` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[char](https://learn.microsoft.com/dotnet/api/system.char)\>

### <a id="MicroEngine_Graphics_InterpolatedText_AppendFormatted_System_ReadOnlySpan_System_Char__System_Int32_System_String_"></a> AppendFormatted\(ReadOnlySpan<char\>, int, string?\)

Called by the compiler for a span of characters with a width (<code>{span,8}</code>) or a format, which a span ignores as a string does.

```csharp
public void AppendFormatted(ReadOnlySpan<char> value, int alignment = 0, string? format = null)
```

#### Parameters

`value` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[char](https://learn.microsoft.com/dotnet/api/system.char)\>

`alignment` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`format` [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Graphics_InterpolatedText_AppendFormatted_System_Char_"></a> AppendFormatted\(char\)

Called by the compiler for a character.

```csharp
public void AppendFormatted(char value)
```

#### Parameters

`value` [char](https://learn.microsoft.com/dotnet/api/system.char)

### <a id="MicroEngine_Graphics_InterpolatedText_AppendFormatted_System_Boolean_"></a> AppendFormatted\(bool\)

Called by the compiler for true or false.

```csharp
public void AppendFormatted(bool value)
```

#### Parameters

`value` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_InterpolatedText_AppendFormatted_System_Int32_"></a> AppendFormatted\(int\)

Called by the compiler for a whole number.

```csharp
public void AppendFormatted(int value)
```

#### Parameters

`value` [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_InterpolatedText_AppendFormatted_System_Int32_System_String_"></a> AppendFormatted\(int, string?\)

Called by the compiler for a whole number with a format (<code>{seconds:00}</code>).

```csharp
public void AppendFormatted(int value, string? format)
```

#### Parameters

`value` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`format` [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Graphics_InterpolatedText_AppendFormatted_System_Int64_"></a> AppendFormatted\(long\)

Called by the compiler for a whole number.

```csharp
public void AppendFormatted(long value)
```

#### Parameters

`value` [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Graphics_InterpolatedText_AppendFormatted_System_Int64_System_String_"></a> AppendFormatted\(long, string?\)

Called by the compiler for a whole number with a format (<code>{seconds:00}</code>).

```csharp
public void AppendFormatted(long value, string? format)
```

#### Parameters

`value` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`format` [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Graphics_InterpolatedText_AppendFormatted_System_Single_"></a> AppendFormatted\(float\)

Called by the compiler for a number with a fraction.

```csharp
public void AppendFormatted(float value)
```

#### Parameters

`value` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_InterpolatedText_AppendFormatted_System_Single_System_String_"></a> AppendFormatted\(float, string?\)

Called by the compiler for a number with a fraction and a format (<code>{time:0.0}</code>).

```csharp
public void AppendFormatted(float value, string? format)
```

#### Parameters

`value` [float](https://learn.microsoft.com/dotnet/api/system.single)

`format` [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Graphics_InterpolatedText_AppendFormatted_System_Double_"></a> AppendFormatted\(double\)

Called by the compiler for a number with a fraction.

```csharp
public void AppendFormatted(double value)
```

#### Parameters

`value` [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Graphics_InterpolatedText_AppendFormatted_System_Double_System_String_"></a> AppendFormatted\(double, string?\)

Called by the compiler for a number with a fraction and a format (<code>{time:0.0}</code>).

```csharp
public void AppendFormatted(double value, string? format)
```

#### Parameters

`value` [double](https://learn.microsoft.com/dotnet/api/system.double)

`format` [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Graphics_InterpolatedText_AppendFormatted_System_Nullable_System_Int32__"></a> AppendFormatted\(int?\)

Called by the compiler for a whole number that may be null; null writes nothing.

```csharp
public void AppendFormatted(int? value)
```

#### Parameters

`value` [int](https://learn.microsoft.com/dotnet/api/system.int32)?

### <a id="MicroEngine_Graphics_InterpolatedText_AppendFormatted_System_Nullable_System_Int32__System_String_"></a> AppendFormatted\(int?, string?\)

Called by the compiler for a whole number that may be null, with a format; null writes nothing.

```csharp
public void AppendFormatted(int? value, string? format)
```

#### Parameters

`value` [int](https://learn.microsoft.com/dotnet/api/system.int32)?

`format` [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Graphics_InterpolatedText_AppendFormatted_System_Nullable_System_Int64__"></a> AppendFormatted\(long?\)

Called by the compiler for a whole number that may be null; null writes nothing.

```csharp
public void AppendFormatted(long? value)
```

#### Parameters

`value` [long](https://learn.microsoft.com/dotnet/api/system.int64)?

### <a id="MicroEngine_Graphics_InterpolatedText_AppendFormatted_System_Nullable_System_Int64__System_String_"></a> AppendFormatted\(long?, string?\)

Called by the compiler for a whole number that may be null, with a format; null writes nothing.

```csharp
public void AppendFormatted(long? value, string? format)
```

#### Parameters

`value` [long](https://learn.microsoft.com/dotnet/api/system.int64)?

`format` [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Graphics_InterpolatedText_AppendFormatted_System_Nullable_System_Single__"></a> AppendFormatted\(float?\)

Called by the compiler for a number with a fraction that may be null; null writes nothing.

```csharp
public void AppendFormatted(float? value)
```

#### Parameters

`value` [float](https://learn.microsoft.com/dotnet/api/system.single)?

### <a id="MicroEngine_Graphics_InterpolatedText_AppendFormatted_System_Nullable_System_Single__System_String_"></a> AppendFormatted\(float?, string?\)

Called by the compiler for a number with a fraction that may be null, with a format; null writes nothing.

```csharp
public void AppendFormatted(float? value, string? format)
```

#### Parameters

`value` [float](https://learn.microsoft.com/dotnet/api/system.single)?

`format` [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Graphics_InterpolatedText_AppendFormatted_System_Nullable_System_Double__"></a> AppendFormatted\(double?\)

Called by the compiler for a number with a fraction that may be null; null writes nothing.

```csharp
public void AppendFormatted(double? value)
```

#### Parameters

`value` [double](https://learn.microsoft.com/dotnet/api/system.double)?

### <a id="MicroEngine_Graphics_InterpolatedText_AppendFormatted_System_Nullable_System_Double__System_String_"></a> AppendFormatted\(double?, string?\)

Called by the compiler for a number with a fraction that may be null, with a format; null writes nothing.

```csharp
public void AppendFormatted(double? value, string? format)
```

#### Parameters

`value` [double](https://learn.microsoft.com/dotnet/api/system.double)?

`format` [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Graphics_InterpolatedText_AppendFormatted__1___0_"></a> AppendFormatted<T\>\(T\)

Called by the compiler for a value of any other type.

```csharp
public void AppendFormatted<T>(T value)
```

#### Parameters

`value` T

#### Type Parameters

`T` 

### <a id="MicroEngine_Graphics_InterpolatedText_AppendFormatted__1___0_System_String_"></a> AppendFormatted<T\>\(T, string?\)

Called by the compiler for a value of any other type with a format (<code>{value:format}</code>).

```csharp
public void AppendFormatted<T>(T value, string? format)
```

#### Parameters

`value` T

`format` [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Type Parameters

`T` 

### <a id="MicroEngine_Graphics_InterpolatedText_AppendFormatted__1___0_System_Int32_System_String_"></a> AppendFormatted<T\>\(T, int, string?\)

Called by the compiler for a value with a width (<code>{value,6}</code> right-aligned, <code>{value,-6}</code> left-aligned).

```csharp
public void AppendFormatted<T>(T value, int alignment, string? format = null)
```

#### Parameters

`value` T

`alignment` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`format` [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Type Parameters

`T` 

### <a id="MicroEngine_Graphics_InterpolatedText_AppendLiteral_System_String_"></a> AppendLiteral\(string\)

Called by the compiler for the fixed text between two values.

```csharp
public void AppendLiteral(string value)
```

#### Parameters

`value` [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Graphics_InterpolatedText_Dispose"></a> Dispose\(\)

Gives the buffer back. Called once by the method that received the text; the text must not be read afterwards.
Calling it again, on this value or on a copy, does nothing.

```csharp
public void Dispose()
```

### <a id="MicroEngine_Graphics_InterpolatedText_ToString"></a> ToString\(\)

The text as a string (allocates): for a caller that keeps it.

```csharp
public override readonly string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

</div>

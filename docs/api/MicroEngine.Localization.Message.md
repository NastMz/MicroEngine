# Class Message

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Localization](MicroEngine.Localization.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Graphics](../manual/rendering.md)  

A text with named values, plural forms and selections, in the ICU MessageFormat syntax translation tools write:
<code>{who} found {count, plural, =0 {nothing} one {# gem} other {# gems}}</code>. Parsed once, formatted any number of
times. String tables hold these; [Localizer](MicroEngine.Localization.Localizer.md) formats them in the language in use.

```csharp
public sealed class Message
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Message](MicroEngine.Localization.Message.md)

## Remarks

<ul><li><code>{name}</code> shows a value. A name that was not given stays as written, so <code>Press {attack}</code> reaches the
action prompts unchanged.</li><li><code>{n, plural, ...}</code> chooses by number: <code>=N</code> for an exact number, then the language's form
([PluralRules](MicroEngine.Localization.PluralRules.md)), else <code>other</code>, which every plural must have. <code>#</code> inside a branch is the number.</li><li><code>{x, select, a {...} b {...} other {...}}</code> chooses by text.</li><li><code>{n, number}</code> is <code>{n}</code>. Numbers are written without grouping, with up to three decimals.</li><li>An apostrophe before <code>{</code>, <code>}</code> or <code>#</code> quotes text up to the next apostrophe (<code>'{'</code> is a
brace), two apostrophes are one, and any other apostrophe is itself (<code>don't</code>).</li></ul>

Not read, and refused when parsing: <code>selectordinal</code>, <code>offset:</code>, and the date, time, duration and
spell-out formats.

## Properties

### <a id="MicroEngine_Localization_Message_Arguments"></a> Arguments

The arguments the message names, in the order they first appear, each once.

```csharp
public IReadOnlyList<MessageArgument> Arguments { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[MessageArgument](MicroEngine.Localization.MessageArgument.md)\>

### <a id="MicroEngine_Localization_Message_IsPlain"></a> IsPlain

Whether the message is text alone: no value, plural or selection.

```csharp
public bool IsPlain { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Localization_Message_Pattern"></a> Pattern

The message as written.

```csharp
public string Pattern { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Localization_Message_Format_System_String_System_ReadOnlySpan_MicroEngine_Localization_TextArgument__"></a> Format\(string, ReadOnlySpan<TextArgument\>\)

The message in <code class="paramref">language</code> with <code class="paramref">arguments</code>.

```csharp
public string Format(string language, ReadOnlySpan<TextArgument> arguments)
```

#### Parameters

`language` [string](https://learn.microsoft.com/dotnet/api/system.string)

`arguments` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[TextArgument](MicroEngine.Localization.TextArgument.md)\>

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Localization_Message_Format_System_Text_StringBuilder_System_String_System_ReadOnlySpan_MicroEngine_Localization_TextArgument__"></a> Format\(StringBuilder, string, ReadOnlySpan<TextArgument\>\)

Appends the message in <code class="paramref">language</code> with <code class="paramref">arguments</code> to <code class="paramref">text</code>.

```csharp
public void Format(StringBuilder text, string language, ReadOnlySpan<TextArgument> arguments)
```

#### Parameters

`text` [StringBuilder](https://learn.microsoft.com/dotnet/api/system.text.stringbuilder)

`language` [string](https://learn.microsoft.com/dotnet/api/system.string)

`arguments` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[TextArgument](MicroEngine.Localization.TextArgument.md)\>

### <a id="MicroEngine_Localization_Message_Parse_System_String_"></a> Parse\(string\)

Parses <code class="paramref">pattern</code>.

```csharp
public static Message Parse(string pattern)
```

#### Parameters

`pattern` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [Message](MicroEngine.Localization.Message.md)

#### Exceptions

 [FormatException](https://learn.microsoft.com/dotnet/api/system.formatexception)

The pattern is not a message; the text says what is wrong and where.

### <a id="MicroEngine_Localization_Message_TryParse_System_String_MicroEngine_Localization_Message__System_String__"></a> TryParse\(string, out Message?, out string?\)

Parses <code class="paramref">pattern</code>, or says in <code class="paramref">error</code> what is wrong with it.

```csharp
public static bool TryParse(string pattern, out Message? message, out string? error)
```

#### Parameters

`pattern` [string](https://learn.microsoft.com/dotnet/api/system.string)

`message` [Message](MicroEngine.Localization.Message.md)?

`error` [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

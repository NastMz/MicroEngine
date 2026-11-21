# Class LanguageTag

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Localization](MicroEngine.Localization.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Graphics](../manual/rendering.md)  

Language tags as tables and players write them: <code>en</code>, <code>pt-BR</code>, <code>es_MX</code>, <code>zh-Hans</code>.

```csharp
public static class LanguageTag
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[LanguageTag](MicroEngine.Localization.LanguageTag.md)

## Methods

### <a id="MicroEngine_Localization_LanguageTag_IsRightToLeft_System_String_"></a> IsRightToLeft\(string\)

Whether a language is written right to left: Arabic, Hebrew, Persian, Urdu, Pashto, Sindhi, Uyghur, Yiddish, Dhivehi and Sorani Kurdish.

```csharp
public static bool IsRightToLeft(string tag)
```

#### Parameters

`tag` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Localization_LanguageTag_IsValid_System_String_"></a> IsValid\(string\)

Whether <code class="paramref">text</code> has the shape of a language tag: two or three letters, then parts of letters or digits.

```csharp
public static bool IsValid(string text)
```

#### Parameters

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Localization_LanguageTag_Normalize_System_String_"></a> Normalize\(string\)

The tag with <code>-</code> between its parts and surrounding space removed (<code>es_MX</code> is <code>es-MX</code>); letter case is kept.

```csharp
public static string Normalize(string tag)
```

#### Parameters

`tag` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Localization_LanguageTag_Parent_System_String_"></a> Parent\(string\)

The tag without its last part (<code>zh-Hans-CN</code>, then <code>zh-Hans</code>, then <code>zh</code>), or null for a bare language.

```csharp
public static string? Parent(string tag)
```

#### Parameters

`tag` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Localization_LanguageTag_Primary_System_String_"></a> Primary\(string\)

The language of a tag: <code>pt</code> for <code>pt-BR</code>.

```csharp
public static string Primary(string tag)
```

#### Parameters

`tag` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Localization_LanguageTag_Same_System_String_System_String_"></a> Same\(string, string\)

Whether two tags name the same language and parts, whatever their letter case or separator.

```csharp
public static bool Same(string a, string b)
```

#### Parameters

`a` [string](https://learn.microsoft.com/dotnet/api/system.string)

`b` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

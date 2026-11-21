# Class PluralRules

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Localization](MicroEngine.Localization.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Graphics](../manual/rendering.md)  

Which plural form a number takes in a language, from rules built into the engine: nothing is read from the
operating system or the runtime's culture data, so trimmed, NativeAOT, phone and browser builds count alike.

```csharp
public static class PluralRules
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PluralRules](MicroEngine.Localization.PluralRules.md)

## Remarks

For whole numbers the rules are the cardinal rules of Unicode's CLDR; fractions are approximated (in most languages
they take the general form). Built in: Afrikaans, Albanian, Amharic, Arabic, Azerbaijani, Basque, Belarusian,
Bengali, Bosnian, Bulgarian, Burmese, Cantonese, Catalan, Chinese, Croatian, Czech, Danish, Dutch, English,
Estonian, Finnish, French, Galician, Georgian, German, Greek, Gujarati, Hebrew, Hindi, Hungarian, Indonesian,
Italian, Japanese, Kannada, Kazakh, Khmer, Korean, Lao, Latvian, Lithuanian, Malay, Malayalam, Mongolian, Nepali,
Norwegian, Persian, Polish, Portuguese, Romanian, Russian, Serbian, Slovak, Slovenian, Spanish, Swahili, Swedish,
Tamil, Telugu, Thai, Turkish, Ukrainian, Urdu, Uzbek, Vietnamese and Zulu. French, Italian, Portuguese, Spanish and
Catalan leave out CLDR's [Many](MicroEngine.Localization.PluralCategory.md) for whole millions. Any other language counts like
English until the game gives its rule with [Register](MicroEngine.Localization.PluralRules.md#MicroEngine_Localization_PluralRules_Register_System_String_System_Func_System_Double_MicroEngine_Localization_PluralCategory__MicroEngine_Localization_PluralCategory___).

## Methods

### <a id="MicroEngine_Localization_PluralRules_Categories_System_String_"></a> Categories\(string\)

The forms <code class="paramref">language</code> uses, in the order of [PluralCategory](MicroEngine.Localization.PluralCategory.md): what a plural message of that language should give.

```csharp
public static IReadOnlyList<PluralCategory> Categories(string language)
```

#### Parameters

`language` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[PluralCategory](MicroEngine.Localization.PluralCategory.md)\>

### <a id="MicroEngine_Localization_PluralRules_Knows_System_String_"></a> Knows\(string\)

Whether the engine or the game has a rule for <code class="paramref">language</code>; without one it counts like English.

```csharp
public static bool Knows(string language)
```

#### Parameters

`language` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Localization_PluralRules_Register_System_String_System_Func_System_Double_MicroEngine_Localization_PluralCategory__MicroEngine_Localization_PluralCategory___"></a> Register\(string, Func<double, PluralCategory\>, params PluralCategory\[\]\)

Gives the rule of a language the engine does not know, or replaces one: <code class="paramref">rule</code> receives the
number without its sign. <code class="paramref">categories</code> are the forms it can return; [Other](MicroEngine.Localization.PluralCategory.md)
is always among them.

```csharp
public static void Register(string language, Func<double, PluralCategory> rule, params PluralCategory[] categories)
```

#### Parameters

`language` [string](https://learn.microsoft.com/dotnet/api/system.string)

`rule` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[double](https://learn.microsoft.com/dotnet/api/system.double), [PluralCategory](MicroEngine.Localization.PluralCategory.md)\>

`categories` [PluralCategory](MicroEngine.Localization.PluralCategory.md)\[\]

### <a id="MicroEngine_Localization_PluralRules_Select_System_String_System_Double_"></a> Select\(string, double\)

The plural form <code class="paramref">number</code> takes in <code class="paramref">language</code> (a tag such as <code>en</code>, <code>pt-BR</code>, <code>zh-Hans</code>); the sign is ignored.

```csharp
public static PluralCategory Select(string language, double number)
```

#### Parameters

`language` [string](https://learn.microsoft.com/dotnet/api/system.string)

`number` [double](https://learn.microsoft.com/dotnet/api/system.double)

#### Returns

 [PluralCategory](MicroEngine.Localization.PluralCategory.md)

</div>

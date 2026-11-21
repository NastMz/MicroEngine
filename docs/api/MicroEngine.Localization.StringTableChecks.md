# Class StringTableChecks

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Localization](MicroEngine.Localization.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Graphics](../manual/rendering.md)  

Compares the string tables of a game's languages, as <code>microengine content check</code> does: a key a language
lacks, a message that cannot be read, a value one translation names and the others do not, a plural without the
forms its language uses. A game can run it in a test of its own over the tables it loads.

```csharp
public static class StringTableChecks
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[StringTableChecks](MicroEngine.Localization.StringTableChecks.md)

## Methods

### <a id="MicroEngine_Localization_StringTableChecks_Check_System_Collections_Generic_IEnumerable_MicroEngine_Localization_StringTableData__System_String_"></a> Check\(IEnumerable<StringTableData\>, string\)

Checks <code class="paramref">tables</code> (several tables of one language add up, as when they are loaded).
Values are compared with the text of <code class="paramref">referenceLanguage</code> (<code>en</code> by default), or of the
first language that has the key.

```csharp
public static IReadOnlyList<LocalizationIssue> Check(IEnumerable<StringTableData> tables, string referenceLanguage = "en")
```

#### Parameters

`tables` [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<[StringTableData](MicroEngine.Localization.StringTableData.md)\>

`referenceLanguage` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[LocalizationIssue](MicroEngine.Localization.LocalizationIssue.md)\>

</div>

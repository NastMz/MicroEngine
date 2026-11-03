# Class AssetPath

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Content](MicroEngine.Content.md)  
Assembly: MicroEngine.Content.dll  
Guide: [Assets](../manual/assets.md#handles-loaders-and-content-sources)  

Content paths: relative to the source, '/'-separated.

```csharp
public static class AssetPath
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[AssetPath](MicroEngine.Content.AssetPath.md)

## Methods

### <a id="MicroEngine_Content_AssetPath_Normalize_System_String_"></a> Normalize\(string\)

'/' separators, no leading '/' or "./", "a/../b" as "b"; the same string when nothing changes, so lookups allocate
nothing. A ".." that leaves the source stays, so the source refuses it.

```csharp
public static string Normalize(string path)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

<code class="paramref">path</code> is null, empty or white space.

</div>

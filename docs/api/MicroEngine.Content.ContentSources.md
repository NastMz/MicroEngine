# Class ContentSources

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Content](MicroEngine.Content.md)  
Assembly: MicroEngine.Content.dll  
Guide: [Assets](../manual/assets.md#handles-loaders-and-content-sources)  

Reading from any content source, and opening one from a path.

```csharp
public static class ContentSources
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ContentSources](MicroEngine.Content.ContentSources.md)

## Methods

### <a id="MicroEngine_Content_ContentSources_Open_System_String_"></a> Open\(string\)

The content at <code class="paramref">path</code>: a folder ([FolderContentSource](MicroEngine.Platform.FolderContentSource.md)) or a zip archive
([ZipContentSource](MicroEngine.Content.ZipContentSource.md), a file ending in ".zip").

```csharp
public static IContentSource Open(string path)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 IContentSource

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

<code class="paramref">path</code> is null, empty or white space.

 [FileNotFoundException](https://learn.microsoft.com/dotnet/api/system.io.filenotfoundexception)

Nothing exists at <code class="paramref">path</code>, or it is a file other than a zip archive.

 [InvalidDataException](https://learn.microsoft.com/dotnet/api/system.io.invaliddataexception)

The file is not a valid zip archive.

### <a id="MicroEngine_Content_ContentSources_ReadAllBytes_MicroEngine_Platform_IContentSource_System_String_"></a> ReadAllBytes\(IContentSource, string\)

Reads the whole file at <code class="paramref">path</code>.

```csharp
public static byte[] ReadAllBytes(this IContentSource source, string path)
```

#### Parameters

`source` IContentSource

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [byte](https://learn.microsoft.com/dotnet/api/system.byte)\[\]

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

The path leaves the source.

 [FileNotFoundException](https://learn.microsoft.com/dotnet/api/system.io.filenotfoundexception)

No such file, or its name is spelled with another letter case.

</div>

# Class ListedContentSource

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Content](MicroEngine.Content.md)  
Assembly: MicroEngine.Content.dll  
Guide: [Assets](../manual/assets.md#handles-loaders-and-content-sources)  

Content files a host lists and opens itself, where the files are not a folder .NET can read: an Android app's
packaged assets (opened through the platform's asset manager), files a browser page fetched. The listing is fixed
when the source is made; names follow the folder rules (exact letter case everywhere, '/' separators).

```csharp
public sealed class ListedContentSource : IContentSource
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ListedContentSource](MicroEngine.Content.ListedContentSource.md)

#### Implements

[IContentSource](MicroEngine.Platform.IContentSource.md)

#### Extension Methods

[ContentSources.ReadAllBytes\(IContentSource, string\)](MicroEngine.Content.ContentSources.md\#MicroEngine\_Content\_ContentSources\_ReadAllBytes\_MicroEngine\_Platform\_IContentSource\_System\_String\_)

## Constructors

### <a id="MicroEngine_Content_ListedContentSource__ctor_System_String_System_Collections_Generic_IEnumerable_System_String__System_Func_System_String_System_IO_Stream__"></a> ListedContentSource\(string, IEnumerable<string\>, Func<string, Stream\>\)

A source over <code class="paramref">files</code>, read with <code class="paramref">open</code>.

```csharp
public ListedContentSource(string location, IEnumerable<string> files, Func<string, Stream> open)
```

#### Parameters

`location` [string](https://learn.microsoft.com/dotnet/api/system.string)

A description of where the files are, for messages.

`files` [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

Every file, relative to the source with '/' separators.

`open` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), [Stream](https://learn.microsoft.com/dotnet/api/system.io.stream)\>

Opens one of <code class="paramref">files</code> (given exactly as listed); called from any thread.

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

<code class="paramref">location</code> is null, empty or white space.

## Properties

### <a id="MicroEngine_Content_ListedContentSource_Location"></a> Location

Where the files are (a folder's absolute path, or a description), for diagnostics.

```csharp
public string Location { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Content_ListedContentSource_Exists_System_String_"></a> Exists\(string\)

Whether a file exists at <code class="paramref">path</code>. False for a missing file and for a path outside the source.

```csharp
public bool Exists(string path)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Content_ListedContentSource_Files_System_String_"></a> Files\(string\)

The files directly inside <code class="paramref">folder</code> ("" for the top), as paths relative to the source, in ordinal order.

```csharp
public IReadOnlyList<string> Files(string folder)
```

#### Parameters

`folder` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

#### Exceptions

 [DirectoryNotFoundException](https://learn.microsoft.com/dotnet/api/system.io.directorynotfoundexception)

The folder does not exist.

### <a id="MicroEngine_Content_ListedContentSource_Open_System_String_"></a> Open\(string\)

Opens the file at <code class="paramref">path</code> for reading; the caller disposes the stream.

```csharp
public Stream Open(string path)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [Stream](https://learn.microsoft.com/dotnet/api/system.io.stream)

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

The path leaves the source.

 [FileNotFoundException](https://learn.microsoft.com/dotnet/api/system.io.filenotfoundexception)

No such file, or its name is spelled with another letter case.

</div>

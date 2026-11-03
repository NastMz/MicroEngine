# Class ZipContentSource

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Content](MicroEngine.Content.md)  
Assembly: MicroEngine.Content.dll  
Guide: [Assets](../manual/assets.md#handles-loaders-and-content-sources)  

Content files inside a zip archive: a game's content shipped as one file. Names follow the folder rules (exact
letter case everywhere, '/' separators, no way out of the archive). Each file is decompressed into memory when it is
opened, so streams are independent and the source can be read from several threads.

```csharp
public sealed class ZipContentSource : IContentSource, IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ZipContentSource](MicroEngine.Content.ZipContentSource.md)

#### Implements

IContentSource, 
[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

#### Extension Methods

[ContentSources.ReadAllBytes\(IContentSource, string\)](MicroEngine.Content.ContentSources.md\#MicroEngine\_Content\_ContentSources\_ReadAllBytes\_MicroEngine\_Platform\_IContentSource\_System\_String\_)

## Constructors

### <a id="MicroEngine_Content_ZipContentSource__ctor_System_String_"></a> ZipContentSource\(string\)

Opens the zip archive at <code class="paramref">path</code>; the source keeps the file open until disposed.

```csharp
public ZipContentSource(string path)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Exceptions

 [FileNotFoundException](https://learn.microsoft.com/dotnet/api/system.io.filenotfoundexception)

The file does not exist.

 [InvalidDataException](https://learn.microsoft.com/dotnet/api/system.io.invaliddataexception)

The file is not a valid zip archive.

### <a id="MicroEngine_Content_ZipContentSource__ctor_System_IO_Stream_System_String_System_Boolean_"></a> ZipContentSource\(Stream, string, bool\)

Reads the zip archive in <code class="paramref">archive</code> (a readable, seekable stream).

```csharp
public ZipContentSource(Stream archive, string location, bool leaveOpen = false)
```

#### Parameters

`archive` [Stream](https://learn.microsoft.com/dotnet/api/system.io.stream)

The archive's bytes.

`location` [string](https://learn.microsoft.com/dotnet/api/system.string)

A description of where it came from, for messages.

`leaveOpen` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether disposing the source leaves <code class="paramref">archive</code> open.

#### Exceptions

 [InvalidDataException](https://learn.microsoft.com/dotnet/api/system.io.invaliddataexception)

The stream is not a valid zip archive.

## Properties

### <a id="MicroEngine_Content_ZipContentSource_Location"></a> Location

The archive's path or the description it was opened with.

```csharp
public string Location { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Content_ZipContentSource_Dispose"></a> Dispose\(\)

Closes the archive.

```csharp
public void Dispose()
```

### <a id="MicroEngine_Content_ZipContentSource_Exists_System_String_"></a> Exists\(string\)

Whether a file exists at <code class="paramref">path</code>. False for a missing file and for a path outside the source.

```csharp
public bool Exists(string path)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Content_ZipContentSource_Files_System_String_"></a> Files\(string\)

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

### <a id="MicroEngine_Content_ZipContentSource_Open_System_String_"></a> Open\(string\)

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

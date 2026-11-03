# Interface IContentSource

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Platform](MicroEngine.Platform.md)  
Assembly: MicroEngine.Platform.dll  
Guide: [The platform host](../manual/backends.md#the-platform-host)  

Where a game's content files come from: a folder next to the executable, a phone's unpacked package, later an
archive or a browser's fetch. Paths are relative to the source with '/' separators and match the stored name's
letter case exactly on every system.

```csharp
public interface IContentSource
```

## Properties

### <a id="MicroEngine_Platform_IContentSource_Location"></a> Location

Where the files are (a folder's absolute path, or a description), for diagnostics.

```csharp
string Location { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Platform_IContentSource_Exists_System_String_"></a> Exists\(string\)

Whether a file exists at <code class="paramref">path</code>. False for a missing file and for a path outside the source.

```csharp
bool Exists(string path)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Platform_IContentSource_Files_System_String_"></a> Files\(string\)

The files directly inside <code class="paramref">folder</code> ("" for the top), as paths relative to the source, in ordinal order.

```csharp
IReadOnlyList<string> Files(string folder)
```

#### Parameters

`folder` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

#### Exceptions

 [DirectoryNotFoundException](https://learn.microsoft.com/dotnet/api/system.io.directorynotfoundexception)

The folder does not exist.

### <a id="MicroEngine_Platform_IContentSource_Open_System_String_"></a> Open\(string\)

Opens the file at <code class="paramref">path</code> for reading; the caller disposes the stream.

```csharp
Stream Open(string path)
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

# Class FolderContentSource

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Platform](MicroEngine.Platform.md)  
Assembly: MicroEngine.Platform.dll  
Guide: [The platform host](../manual/backends.md#the-platform-host)  

Content files in a folder on disk. Names must match the files' letter case exactly on every system: "Hero.png" for
"hero.png" works on Windows and macOS but not on Linux, Android or in browsers, so it fails everywhere, naming the
real spelling. Paths that leave the folder (<code>..</code>, another drive) are refused.

```csharp
public sealed class FolderContentSource : IContentSource
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[FolderContentSource](MicroEngine.Platform.FolderContentSource.md)

#### Implements

[IContentSource](MicroEngine.Platform.IContentSource.md)

## Remarks

Safe to read from several threads (asset workers check names concurrently).

## Constructors

### <a id="MicroEngine_Platform_FolderContentSource__ctor_System_String_"></a> FolderContentSource\(string\)

Opens a folder.

```csharp
public FolderContentSource(string root)
```

#### Parameters

`root` [string](https://learn.microsoft.com/dotnet/api/system.string)

The folder, absolute or relative to the working directory.

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

<code class="paramref">root</code> is null, empty or white space.

 [DirectoryNotFoundException](https://learn.microsoft.com/dotnet/api/system.io.directorynotfoundexception)

The folder does not exist.

## Properties

### <a id="MicroEngine_Platform_FolderContentSource_Location"></a> Location

The same as [Root](MicroEngine.Platform.FolderContentSource.md#MicroEngine_Platform_FolderContentSource_Root).

```csharp
public string Location { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Platform_FolderContentSource_Root"></a> Root

The absolute path of the folder.

```csharp
public string Root { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Platform_FolderContentSource_Exists_System_String_"></a> Exists\(string\)

Whether a file exists at <code class="paramref">path</code>. False for a missing file and for a path outside the source.

```csharp
public bool Exists(string path)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Platform_FolderContentSource_Files_System_String_"></a> Files\(string\)

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

### <a id="MicroEngine_Platform_FolderContentSource_Open_System_String_"></a> Open\(string\)

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

### <a id="MicroEngine_Platform_FolderContentSource_PathOf_System_String_"></a> PathOf\(string\)

The full path of the file at <code class="paramref">path</code>, after the checks of [Open](MicroEngine.Platform.FolderContentSource.md#MicroEngine_Platform_FolderContentSource_Open_System_String_).

```csharp
public string PathOf(string path)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

The path leaves the folder.

 [FileNotFoundException](https://learn.microsoft.com/dotnet/api/system.io.filenotfoundexception)

No such file, or its name is spelled with another letter case.

</div>

# Class ContentFolder

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Assets](../manual/assets.md)  

Locates a game's asset files. By default the root is the <code>Content</code> folder next to the
executable (games copy it there at build time). Missing files fail with the full path so a
broken install or a typo is obvious. A content source ([IContentSource](MicroEngine.Platform.IContentSource.md)) over a
[FolderContentSource](MicroEngine.Platform.FolderContentSource.md).

```csharp
public sealed class ContentFolder : IContentSource
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ContentFolder](MicroEngine.Core.ContentFolder.md)

#### Implements

[IContentSource](MicroEngine.Platform.IContentSource.md)

## Constructors

### <a id="MicroEngine_Core_ContentFolder__ctor_System_String_"></a> ContentFolder\(string?\)

Opens a content folder.

```csharp
public ContentFolder(string? root = null)
```

#### Parameters

`root` [string](https://learn.microsoft.com/dotnet/api/system.string)?

The folder, absolute or relative to the working directory; null uses the <code>Content</code> folder next to the executable. A host whose files live elsewhere (a phone app, a macOS bundle) composes its folder instead ([Content](MicroEngine.Hosting.HostBuilder.md#MicroEngine_Hosting_HostBuilder_Content)).

#### Exceptions

 [DirectoryNotFoundException](https://learn.microsoft.com/dotnet/api/system.io.directorynotfoundexception)

The folder does not exist.

## Properties

### <a id="MicroEngine_Core_ContentFolder_DefaultRoot"></a> DefaultRoot

The folder used when a game and its composition name none: <code>Content</code> next to the executable. A rule, not a
setting: a host whose files live elsewhere (a phone app, a macOS bundle) composes its folder
([Content](MicroEngine.Hosting.HostBuilder.md#MicroEngine_Hosting_HostBuilder_Content)).

```csharp
public static string DefaultRoot { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Core_ContentFolder_Location"></a> Location

The same as [Root](MicroEngine.Core.ContentFolder.md#MicroEngine_Core_ContentFolder_Root).

```csharp
public string Location { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Core_ContentFolder_Root"></a> Root

The absolute path of the content folder.

```csharp
public string Root { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Core_ContentFolder_Exists_System_String_"></a> Exists\(string\)

Whether a file exists at <code class="paramref">relative</code> (relative to [Root](MicroEngine.Core.ContentFolder.md#MicroEngine_Core_ContentFolder_Root), '/' separators). False for a missing file and for a path outside the folder.

```csharp
public bool Exists(string relative)
```

#### Parameters

`relative` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_ContentFolder_Files_System_String_"></a> Files\(string\)

The files directly inside <code class="paramref">folder</code> (relative to the root, '/' separators; "" for the root), as
'/'-separated paths relative to the root, in ordinal order.

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

### <a id="MicroEngine_Core_ContentFolder_Image_System_String_"></a> Image\(string\)

Decodes a PNG file into an image (see [Load](MicroEngine.Graphics.PixelImage.md#MicroEngine_Graphics_PixelImage_Load_System_String_)).

```csharp
public PixelImage Image(string relative)
```

#### Parameters

`relative` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [PixelImage](MicroEngine.Graphics.PixelImage.md)

#### Exceptions

 [FileNotFoundException](https://learn.microsoft.com/dotnet/api/system.io.filenotfoundexception)

The file does not exist.

### <a id="MicroEngine_Core_ContentFolder_Open_System_String_"></a> Open\(string\)

Opens a file for reading, after the checks of [PathOf](MicroEngine.Core.ContentFolder.md#MicroEngine_Core_ContentFolder_PathOf_System_String_).

```csharp
public Stream Open(string relative)
```

#### Parameters

`relative` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [Stream](https://learn.microsoft.com/dotnet/api/system.io.stream)

### <a id="MicroEngine_Core_ContentFolder_PathOf_System_String_"></a> PathOf\(string\)

Full path of a file given relative to the root with '/' separators. The name must match the file's letter case
exactly, on every system: "Hero.png" for "hero.png" works on Windows and macOS but not on Linux, Android or in
browsers, so it fails everywhere, naming the file's real spelling.

```csharp
public string PathOf(string relative)
```

#### Parameters

`relative` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

The path leaves the content folder.

 [FileNotFoundException](https://learn.microsoft.com/dotnet/api/system.io.filenotfoundexception)

No such file, or its name is spelled with another letter case.

### <a id="MicroEngine_Core_ContentFolder_ReadBytes_System_String_"></a> ReadBytes\(string\)

Reads a whole file.

```csharp
public byte[] ReadBytes(string relative)
```

#### Parameters

`relative` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [byte](https://learn.microsoft.com/dotnet/api/system.byte)\[\]

#### Exceptions

 [FileNotFoundException](https://learn.microsoft.com/dotnet/api/system.io.filenotfoundexception)

The file does not exist.

</div>

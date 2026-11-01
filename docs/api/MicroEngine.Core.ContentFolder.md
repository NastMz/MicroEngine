# Class ContentFolder

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Assets](../manual/assets.md)  

Locates a game's asset files. By default the root is the <code>Content</code> folder next to the
executable (games copy it there at build time). Missing files fail with the full path so a
broken install or a typo is obvious.

```csharp
public sealed class ContentFolder
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ContentFolder](MicroEngine.Core.ContentFolder.md)

## Constructors

### <a id="MicroEngine_Core_ContentFolder__ctor_System_String_"></a> ContentFolder\(string?\)

Opens a content folder.

```csharp
public ContentFolder(string? root = null)
```

#### Parameters

`root` [string](https://learn.microsoft.com/dotnet/api/system.string)?

The folder, absolute or relative to the working directory; null uses [DefaultRoot](MicroEngine.Core.ContentFolder.md#MicroEngine_Core_ContentFolder_DefaultRoot) or the <code>Content</code> folder next to the executable.

#### Exceptions

 [DirectoryNotFoundException](https://learn.microsoft.com/dotnet/api/system.io.directorynotfoundexception)

The folder does not exist.

## Properties

### <a id="MicroEngine_Core_ContentFolder_DefaultRoot"></a> DefaultRoot

Root used when a game passes none. Null means the <code>Content</code> folder next to the executable; a host whose
files live elsewhere (a phone app unpacking its package) sets it before the game is built.

```csharp
public static string? DefaultRoot { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Core_ContentFolder_Root"></a> Root

The absolute path of the content folder.

```csharp
public string Root { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Core_ContentFolder_Exists_System_String_"></a> Exists\(string\)

Whether a file exists at <code class="paramref">relative</code> (relative to [Root](MicroEngine.Core.ContentFolder.md#MicroEngine_Core_ContentFolder_Root), '/' separators). Never throws for a missing file.

```csharp
public bool Exists(string relative)
```

#### Parameters

`relative` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

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

### <a id="MicroEngine_Core_ContentFolder_PathOf_System_String_"></a> PathOf\(string\)

Full path of a file given relative to the root with '/' separators.

```csharp
public string PathOf(string relative)
```

#### Parameters

`relative` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

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

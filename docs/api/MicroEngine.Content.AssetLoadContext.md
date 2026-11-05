# Class AssetLoadContext

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Content](MicroEngine.Content.md)  
Assembly: MicroEngine.Content.dll  
Guide: [Assets](../manual/assets.md#handles-loaders-and-content-sources)  

What a loader is asked to load, and the content it reads it from.

```csharp
public sealed class AssetLoadContext
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[AssetLoadContext](MicroEngine.Content.AssetLoadContext.md)

## Properties

### <a id="MicroEngine_Content_AssetLoadContext_Key"></a> Key

The asset asked for (kind, normalized path, options, variant).

```csharp
public AssetKey Key { get; }
```

#### Property Value

 [AssetKey](MicroEngine.Content.AssetKey.md)

### <a id="MicroEngine_Content_AssetLoadContext_Options"></a> Options

The load settings, or null for the loader's defaults.

```csharp
public object? Options { get; }
```

#### Property Value

 [object](https://learn.microsoft.com/dotnet/api/system.object)?

### <a id="MicroEngine_Content_AssetLoadContext_Path"></a> Path

The asset's path in [Source](MicroEngine.Content.AssetLoadContext.md#MicroEngine_Content_AssetLoadContext_Source).

```csharp
public string Path { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Content_AssetLoadContext_Source"></a> Source

Where the files are.

```csharp
public IContentSource Source { get; }
```

#### Property Value

 [IContentSource](MicroEngine.Platform.IContentSource.md)

## Methods

### <a id="MicroEngine_Content_AssetLoadContext_Files_System_String_"></a> Files\(string?\)

The files directly inside <code class="paramref">folder</code> (the asset's own path when null), in ordinal order.

```csharp
public IReadOnlyList<string> Files(string? folder = null)
```

#### Parameters

`folder` [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Returns

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

#### Exceptions

 [AssetLoadException](MicroEngine.Core.AssetLoadException.md)

The folder is missing or outside the source.

### <a id="MicroEngine_Content_AssetLoadContext_ReadBytes_System_String_"></a> ReadBytes\(string?\)

The whole file at <code class="paramref">path</code> (the asset's own path when null).

```csharp
public byte[] ReadBytes(string? path = null)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Returns

 [byte](https://learn.microsoft.com/dotnet/api/system.byte)\[\]

#### Exceptions

 [AssetLoadException](MicroEngine.Core.AssetLoadException.md)

The file is missing, wrongly cased or outside the source; the message names this asset.

</div>

# Interface IAssetLoader

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Content](MicroEngine.Content.md)  
Assembly: MicroEngine.Content.dll  
Guide: [Assets](../manual/assets.md#handles-loaders-and-content-sources)  

Makes one kind of asset from content files: decodes an image into a texture, a WAV into an audio clip... A loader is
registered once per store under its [Kind](MicroEngine.Content.IAssetLoader.md#MicroEngine_Content_IAssetLoader_Kind) ([AssetLoaderRegistry](MicroEngine.Content.AssetLoaderRegistry.md)); the store calls it the
first time a key of that kind is asked for and shares the result.

```csharp
public interface IAssetLoader
```

## Properties

### <a id="MicroEngine_Content_IAssetLoader_AssetType"></a> AssetType

The type of the objects it makes.

```csharp
Type AssetType { get; }
```

#### Property Value

 [Type](https://learn.microsoft.com/dotnet/api/system.type)

### <a id="MicroEngine_Content_IAssetLoader_Extensions"></a> Extensions

File extensions (lower case, with the dot) this loader is chosen for when only a path is given; empty when it is asked for by kind only.

```csharp
IReadOnlyList<string> Extensions { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Content_IAssetLoader_Kind"></a> Kind

The name requests use for this kind of asset ("texture", "sound"...), compared ordinally.

```csharp
string Kind { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Content_IAssetLoader_SizeInBytes_System_Object_"></a> SizeInBytes\(object\)

The memory <code class="paramref">asset</code> takes now, for diagnostics (0 when unknown).

```csharp
long SizeInBytes(object asset)
```

#### Parameters

`asset` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

</div>

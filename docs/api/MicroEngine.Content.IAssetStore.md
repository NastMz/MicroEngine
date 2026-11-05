# Interface IAssetStore

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Content](MicroEngine.Content.md)  
Assembly: MicroEngine.Content.dll  
Guide: [Assets](../manual/assets.md#handles-loaders-and-content-sources)  

A game's assets: where content comes from, the loaders that read it, and the scopes that hold what was loaded.

```csharp
public interface IAssetStore
```

## Properties

### <a id="MicroEngine_Content_IAssetStore_Loaders"></a> Loaders

The loaders, one per kind of asset.

```csharp
AssetLoaderRegistry Loaders { get; }
```

#### Property Value

 [AssetLoaderRegistry](MicroEngine.Content.AssetLoaderRegistry.md)

### <a id="MicroEngine_Content_IAssetStore_Root"></a> Root

The scope that lives as long as the store; create child scopes from it for levels and scenes.

```csharp
AssetScope Root { get; }
```

#### Property Value

 [AssetScope](MicroEngine.Content.AssetScope.md)

### <a id="MicroEngine_Content_IAssetStore_Source"></a> Source

Where content files are read from.

```csharp
IContentSource Source { get; }
```

#### Property Value

 [IContentSource](MicroEngine.Platform.IContentSource.md)

## Methods

### <a id="MicroEngine_Content_IAssetStore_Get__1_MicroEngine_Content_Asset___0__"></a> Get<T\>\(Asset<T\>\)

The asset of <code class="paramref">asset</code>.

```csharp
T Get<T>(Asset<T> asset) where T : class
```

#### Parameters

`asset` [Asset](MicroEngine.Content.Asset\-1.md)<T\>

#### Returns

 T

#### Type Parameters

`T` 

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The handle is [None](MicroEngine.Content.md), stale (every scope released it) or of another store.

</div>

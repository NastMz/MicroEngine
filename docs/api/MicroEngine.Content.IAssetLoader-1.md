# Interface IAssetLoader<T\>

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Content](MicroEngine.Content.md)  
Assembly: MicroEngine.Content.dll  
Guide: [Assets](../manual/assets.md#handles-loaders-and-content-sources)  

A loader of assets of type <code class="typeparamref">T</code> (see [IAssetLoader](MicroEngine.Content.IAssetLoader.md)).

```csharp
public interface IAssetLoader<out T> : IAssetLoader where T : class
```

#### Type Parameters

`T` 

#### Implements

[IAssetLoader](MicroEngine.Content.IAssetLoader.md)

## Methods

### <a id="MicroEngine_Content_IAssetLoader_1_Load_MicroEngine_Content_AssetLoadContext_"></a> Load\(AssetLoadContext\)

Reads and makes the asset of [Key](MicroEngine.Content.AssetLoadContext.md#MicroEngine_Content_AssetLoadContext_Key). Failures are exceptions: an
[AssetLoadException](MicroEngine.Core.AssetLoadException.md), or an `IOException`, `InvalidDataException`,
`UnauthorizedAccessException`, `ArgumentException` or `NotSupportedException`,
which the store reports as an [AssetLoadException](MicroEngine.Core.AssetLoadException.md) naming the asset.

```csharp
T Load(AssetLoadContext context)
```

#### Parameters

`context` [AssetLoadContext](MicroEngine.Content.AssetLoadContext.md)

#### Returns

 T

</div>

# Interface IStagedAssetLoader

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Content](MicroEngine.Content.md)  
Assembly: MicroEngine.Content.dll  
Guide: [Assets](../manual/assets.md#handles-loaders-and-content-sources)  

A loader whose work has two stages, so a preload (<code>Assets.Preload</code>) keeps the slow one off the game's thread:
[Prepare](MicroEngine.Content.IStagedAssetLoader.md#MicroEngine_Content_IStagedAssetLoader_Prepare_MicroEngine_Content_AssetLoadContext_) reads and decodes on any thread, touching nothing but the content, and [Complete](MicroEngine.Content.IStagedAssetLoader.md#MicroEngine_Content_IStagedAssetLoader_Complete_MicroEngine_Content_AssetLoadContext_System_Object_)
makes the asset on the game's thread (uploads to the graphics device). A loader without stages is run whole on the
game's thread.

```csharp
public interface IStagedAssetLoader : IAssetLoader
```

#### Implements

[IAssetLoader](MicroEngine.Content.IAssetLoader.md)

## Methods

### <a id="MicroEngine_Content_IStagedAssetLoader_Complete_MicroEngine_Content_AssetLoadContext_System_Object_"></a> Complete\(AssetLoadContext, object\)

Makes the asset from what [Prepare](MicroEngine.Content.IStagedAssetLoader.md#MicroEngine_Content_IStagedAssetLoader_Prepare_MicroEngine_Content_AssetLoadContext_) returned, on the game's thread.

```csharp
object Complete(AssetLoadContext context, object prepared)
```

#### Parameters

`context` [AssetLoadContext](MicroEngine.Content.AssetLoadContext.md)

`prepared` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [object](https://learn.microsoft.com/dotnet/api/system.object)

### <a id="MicroEngine_Content_IStagedAssetLoader_Prepare_MicroEngine_Content_AssetLoadContext_"></a> Prepare\(AssetLoadContext\)

Reads and decodes the asset of [Key](MicroEngine.Content.AssetLoadContext.md#MicroEngine_Content_AssetLoadContext_Key), on any thread. It touches the content only: no graphics device, no state of the game.

```csharp
object Prepare(AssetLoadContext context)
```

#### Parameters

`context` [AssetLoadContext](MicroEngine.Content.AssetLoadContext.md)

#### Returns

 [object](https://learn.microsoft.com/dotnet/api/system.object)

</div>

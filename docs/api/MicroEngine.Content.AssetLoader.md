# Class AssetLoader

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Content](MicroEngine.Content.md)  
Assembly: MicroEngine.Content.dll  
Guide: [Assets](../manual/assets.md#handles-loaders-and-content-sources)  

Shorthands for loaders written as functions.

```csharp
public static class AssetLoader
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[AssetLoader](MicroEngine.Content.AssetLoader.md)

## Methods

### <a id="MicroEngine_Content_AssetLoader_Background__1_System_String_System_Func_MicroEngine_Content_AssetLoadContext___0__System_Collections_Generic_IReadOnlyList_System_String__System_Func___0_System_Int64__"></a> Background<T\>\(string, Func<AssetLoadContext, T\>, IReadOnlyList<string\>?, Func<T, long\>?\)

A loader of data that touches only the content (a level file, a table, a decoded sound): a preload may run it
whole on a worker thread. <code class="paramref">load</code> must not use the graphics device or the game's state.

```csharp
public static IAssetLoader<T> Background<T>(string kind, Func<AssetLoadContext, T> load, IReadOnlyList<string>? extensions = null, Func<T, long>? size = null) where T : class
```

#### Parameters

`kind` [string](https://learn.microsoft.com/dotnet/api/system.string)

The kind requests use.

`load` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[AssetLoadContext](MicroEngine.Content.AssetLoadContext.md), T\>

Makes the asset, on any thread.

`extensions` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>?

Extensions it is chosen for (lower case, with the dot).

`size` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<T, [long](https://learn.microsoft.com/dotnet/api/system.int64)\>?

The memory an asset takes; null for 0.

#### Returns

 [IAssetLoader](MicroEngine.Content.IAssetLoader\-1.md)<T\>

#### Type Parameters

`T` 

The type of the assets.

</div>

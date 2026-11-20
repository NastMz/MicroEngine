# Class StagedAssetLoader<TPrepared, T\>

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Content](MicroEngine.Content.md)  
Assembly: MicroEngine.Content.dll  
Guide: [Assets](../manual/assets.md#handles-loaders-and-content-sources)  

A staged loader written as two functions (see [IStagedAssetLoader](MicroEngine.Content.IStagedAssetLoader.md)); loading directly runs one after the other.

```csharp
public sealed class StagedAssetLoader<TPrepared, T> : IAssetLoader<T>, IStagedAssetLoader, IAssetLoader where TPrepared : class where T : class
```

#### Type Parameters

`TPrepared` 

What the first stage gives the second (decoded pixels, parsed data).

`T` 

The type of the assets.

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[StagedAssetLoader<TPrepared, T\>](MicroEngine.Content.StagedAssetLoader\-2.md)

#### Implements

[IAssetLoader<T\>](MicroEngine.Content.IAssetLoader\-1.md), 
[IStagedAssetLoader](MicroEngine.Content.IStagedAssetLoader.md), 
[IAssetLoader](MicroEngine.Content.IAssetLoader.md)

## Constructors

### <a id="MicroEngine_Content_StagedAssetLoader_2__ctor_System_String_System_Func_MicroEngine_Content_AssetLoadContext__0__System_Func_MicroEngine_Content_AssetLoadContext__0__1__System_Collections_Generic_IReadOnlyList_System_String__System_Func__1_System_Int64__"></a> StagedAssetLoader\(string, Func<AssetLoadContext, TPrepared\>, Func<AssetLoadContext, TPrepared, T\>, IReadOnlyList<string\>?, Func<T, long\>?\)

A staged loader written as two functions (see [IStagedAssetLoader](MicroEngine.Content.IStagedAssetLoader.md)); loading directly runs one after the other.

```csharp
public StagedAssetLoader(string kind, Func<AssetLoadContext, TPrepared> prepare, Func<AssetLoadContext, TPrepared, T> complete, IReadOnlyList<string>? extensions = null, Func<T, long>? size = null)
```

#### Parameters

`kind` [string](https://learn.microsoft.com/dotnet/api/system.string)

The kind requests use.

`prepare` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[AssetLoadContext](MicroEngine.Content.AssetLoadContext.md), TPrepared\>

Reads and decodes, on any thread.

`complete` [Func](https://learn.microsoft.com/dotnet/api/system.func\-3)<[AssetLoadContext](MicroEngine.Content.AssetLoadContext.md), TPrepared, T\>

Makes the asset, on the game's thread.

`extensions` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>?

Extensions it is chosen for (lower case, with the dot).

`size` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<T, [long](https://learn.microsoft.com/dotnet/api/system.int64)\>?

The memory an asset takes; null for 0.

## Properties

### <a id="MicroEngine_Content_StagedAssetLoader_2_AssetType"></a> AssetType

The type of the objects it makes.

```csharp
public Type AssetType { get; }
```

#### Property Value

 [Type](https://learn.microsoft.com/dotnet/api/system.type)

### <a id="MicroEngine_Content_StagedAssetLoader_2_Extensions"></a> Extensions

File extensions (lower case, with the dot) this loader is chosen for when only a path is given; empty when it is asked for by kind only.

```csharp
public IReadOnlyList<string> Extensions { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Content_StagedAssetLoader_2_Kind"></a> Kind

The name requests use for this kind of asset ("texture", "sound"...), compared ordinally.

```csharp
public string Kind { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Content_StagedAssetLoader_2_Complete_MicroEngine_Content_AssetLoadContext_System_Object_"></a> Complete\(AssetLoadContext, object\)

Makes the asset from what [Prepare](MicroEngine.Content.IStagedAssetLoader.md#MicroEngine_Content_IStagedAssetLoader_Prepare_MicroEngine_Content_AssetLoadContext_) returned, on the game's thread.

```csharp
public object Complete(AssetLoadContext context, object prepared)
```

#### Parameters

`context` [AssetLoadContext](MicroEngine.Content.AssetLoadContext.md)

`prepared` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [object](https://learn.microsoft.com/dotnet/api/system.object)

### <a id="MicroEngine_Content_StagedAssetLoader_2_Load_MicroEngine_Content_AssetLoadContext_"></a> Load\(AssetLoadContext\)

Reads and makes the asset of [Key](MicroEngine.Content.AssetLoadContext.md#MicroEngine_Content_AssetLoadContext_Key). Failures are exceptions: an
[AssetLoadException](MicroEngine.Core.AssetLoadException.md), or an `IOException`, `InvalidDataException`,
`UnauthorizedAccessException`, `ArgumentException` or `NotSupportedException`,
which the store reports as an [AssetLoadException](MicroEngine.Core.AssetLoadException.md) naming the asset.

```csharp
public T Load(AssetLoadContext context)
```

#### Parameters

`context` [AssetLoadContext](MicroEngine.Content.AssetLoadContext.md)

#### Returns

 T

### <a id="MicroEngine_Content_StagedAssetLoader_2_Prepare_MicroEngine_Content_AssetLoadContext_"></a> Prepare\(AssetLoadContext\)

Reads and decodes the asset of [Key](MicroEngine.Content.AssetLoadContext.md#MicroEngine_Content_AssetLoadContext_Key), on any thread. It touches the content only: no graphics device, no state of the game.

```csharp
public object Prepare(AssetLoadContext context)
```

#### Parameters

`context` [AssetLoadContext](MicroEngine.Content.AssetLoadContext.md)

#### Returns

 [object](https://learn.microsoft.com/dotnet/api/system.object)

### <a id="MicroEngine_Content_StagedAssetLoader_2_SizeInBytes_System_Object_"></a> SizeInBytes\(object\)

The memory <code class="paramref">asset</code> takes now, for diagnostics (0 when unknown).

```csharp
public long SizeInBytes(object asset)
```

#### Parameters

`asset` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

</div>

# Class AssetLoader<T\>

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Content](MicroEngine.Content.md)  
Assembly: MicroEngine.Content.dll  
Guide: [Assets](../manual/assets.md#handles-loaders-and-content-sources)  

A loader written as a function (see [IAssetLoader`1](MicroEngine.Content.md)).

```csharp
public sealed class AssetLoader<T> : IAssetLoader<T>, IAssetLoader where T : class
```

#### Type Parameters

`T` 

The type of the assets.

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[AssetLoader<T\>](MicroEngine.Content.AssetLoader\-1.md)

#### Implements

[IAssetLoader<T\>](MicroEngine.Content.IAssetLoader\-1.md), 
[IAssetLoader](MicroEngine.Content.IAssetLoader.md)

## Constructors

### <a id="MicroEngine_Content_AssetLoader_1__ctor_System_String_System_Func_MicroEngine_Content_AssetLoadContext__0__System_Collections_Generic_IReadOnlyList_System_String__System_Func__0_System_Int64__"></a> AssetLoader\(string, Func<AssetLoadContext, T\>, IReadOnlyList<string\>?, Func<T, long\>?\)

A loader written as a function (see [IAssetLoader`1](MicroEngine.Content.md)).

```csharp
public AssetLoader(string kind, Func<AssetLoadContext, T> load, IReadOnlyList<string>? extensions = null, Func<T, long>? size = null)
```

#### Parameters

`kind` [string](https://learn.microsoft.com/dotnet/api/system.string)

The kind requests use.

`load` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[AssetLoadContext](MicroEngine.Content.AssetLoadContext.md), T\>

Makes the asset.

`extensions` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>?

Extensions it is chosen for (lower case, with the dot).

`size` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<T, [long](https://learn.microsoft.com/dotnet/api/system.int64)\>?

The memory an asset takes; null for 0.

## Properties

### <a id="MicroEngine_Content_AssetLoader_1_AssetType"></a> AssetType

The type of the objects it makes.

```csharp
public Type AssetType { get; }
```

#### Property Value

 [Type](https://learn.microsoft.com/dotnet/api/system.type)

### <a id="MicroEngine_Content_AssetLoader_1_Extensions"></a> Extensions

File extensions (lower case, with the dot) this loader is chosen for when only a path is given; empty when it is asked for by kind only.

```csharp
public IReadOnlyList<string> Extensions { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Content_AssetLoader_1_Kind"></a> Kind

The name requests use for this kind of asset ("texture", "sound"...), compared ordinally.

```csharp
public string Kind { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Content_AssetLoader_1_Load_MicroEngine_Content_AssetLoadContext_"></a> Load\(AssetLoadContext\)

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

### <a id="MicroEngine_Content_AssetLoader_1_SizeInBytes_System_Object_"></a> SizeInBytes\(object\)

The memory <code class="paramref">asset</code> takes now, for diagnostics (0 when unknown).

```csharp
public long SizeInBytes(object asset)
```

#### Parameters

`asset` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

</div>

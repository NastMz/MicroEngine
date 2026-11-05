# Class AssetStore

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Content](MicroEngine.Content.md)  
Assembly: MicroEngine.Content.dll  
Guide: [Assets](../manual/assets.md#handles-loaders-and-content-sources)  

One entry per asset key for the whole game, shared by the scopes that hold it: a scope that asks for an asset another
scope already loaded gets the same object and holds it too; the object is disposed (when it is
`IDisposable`) once the last scope holding it releases it, and its handles become stale. Games never count
references: lifetimes follow scopes.

```csharp
public sealed class AssetStore : IAssetStore, IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[AssetStore](MicroEngine.Content.AssetStore.md)

#### Implements

[IAssetStore](MicroEngine.Content.IAssetStore.md), 
[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

## Remarks

Used from the game's thread; loaders may be given work done elsewhere through [Add``1](MicroEngine.Content.AssetScope.md#MicroEngine_Content_AssetScope_Add__1_MicroEngine_Content_AssetKey____0_System_Double_System_Nullable_System_Double__).

## Constructors

### <a id="MicroEngine_Content_AssetStore__ctor_MicroEngine_Platform_IContentSource_MicroEngine_Content_AssetLoaderRegistry_"></a> AssetStore\(IContentSource, AssetLoaderRegistry?\)

A store reading from <code class="paramref">source</code>.

```csharp
public AssetStore(IContentSource source, AssetLoaderRegistry? loaders = null)
```

#### Parameters

`source` [IContentSource](MicroEngine.Platform.IContentSource.md)

`loaders` [AssetLoaderRegistry](MicroEngine.Content.AssetLoaderRegistry.md)?

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

<code class="paramref">source</code> is null.

### <a id="MicroEngine_Content_AssetStore__ctor_System_Func_MicroEngine_Platform_IContentSource__MicroEngine_Content_AssetLoaderRegistry_"></a> AssetStore\(Func<IContentSource\>, AssetLoaderRegistry?\)

A store whose source is opened by <code class="paramref">openSource</code> on the first read, so a game without content files
never needs one.

```csharp
public AssetStore(Func<IContentSource> openSource, AssetLoaderRegistry? loaders = null)
```

#### Parameters

`openSource` [Func](https://learn.microsoft.com/dotnet/api/system.func\-1)<[IContentSource](MicroEngine.Platform.IContentSource.md)\>

`loaders` [AssetLoaderRegistry](MicroEngine.Content.AssetLoaderRegistry.md)?

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

<code class="paramref">openSource</code> is null.

## Properties

### <a id="MicroEngine_Content_AssetStore_Count"></a> Count

How many assets are loaded (each once, however many scopes hold it). Allocates nothing.

```csharp
public int Count { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Content_AssetStore_IsSourceOpen"></a> IsSourceOpen

Whether [Source](MicroEngine.Content.AssetStore.md#MicroEngine_Content_AssetStore_Source) has been opened.

```csharp
public bool IsSourceOpen { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Content_AssetStore_Loaders"></a> Loaders

The loaders, one per kind of asset.

```csharp
public AssetLoaderRegistry Loaders { get; }
```

#### Property Value

 [AssetLoaderRegistry](MicroEngine.Content.AssetLoaderRegistry.md)

### <a id="MicroEngine_Content_AssetStore_Log"></a> Log

Where reports of slow loads during play go; standard error when null.

```csharp
public ILog? Log { get; set; }
```

#### Property Value

 [ILog](MicroEngine.Kernel.ILog.md)?

### <a id="MicroEngine_Content_AssetStore_Root"></a> Root

The scope that lives as long as the store; create child scopes from it for levels and scenes.

```csharp
public AssetScope Root { get; }
```

#### Property Value

 [AssetScope](MicroEngine.Content.AssetScope.md)

### <a id="MicroEngine_Content_AssetStore_Source"></a> Source

Where content files are read from, opened on first use.

```csharp
public IContentSource Source { get; }
```

#### Property Value

 [IContentSource](MicroEngine.Platform.IContentSource.md)

#### Exceptions

 [AssetLoadException](MicroEngine.Core.AssetLoadException.md)

The source cannot be opened (a missing folder or archive); its kind is "content source".

## Methods

### <a id="MicroEngine_Content_AssetStore_Dispose"></a> Dispose\(\)

Releases every scope's assets, disposing them. The source is not disposed: whoever made it owns it.

```csharp
public void Dispose()
```

### <a id="MicroEngine_Content_AssetStore_Get__1_MicroEngine_Content_Asset___0__"></a> Get<T\>\(Asset<T\>\)

The asset of <code class="paramref">asset</code>.

```csharp
public T Get<T>(Asset<T> asset) where T : class
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

### <a id="MicroEngine_Content_AssetStore_IsLoaded__1_MicroEngine_Content_Asset___0__"></a> IsLoaded<T\>\(Asset<T\>\)

Whether <code class="paramref">asset</code> still refers to a loaded asset.

```csharp
public bool IsLoaded<T>(Asset<T> asset) where T : class
```

#### Parameters

`asset` [Asset](MicroEngine.Content.Asset\-1.md)<T\>

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Type Parameters

`T` 

### <a id="MicroEngine_Content_AssetStore_TryGet__1_MicroEngine_Content_Asset___0____0__"></a> TryGet<T\>\(Asset<T\>, out T?\)

The asset of <code class="paramref">asset</code>, if it is still loaded and of type <code class="typeparamref">T</code>.

```csharp
public bool TryGet<T>(Asset<T> asset, out T? value) where T : class
```

#### Parameters

`asset` [Asset](MicroEngine.Content.Asset\-1.md)<T\>

`value` T?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Type Parameters

`T`

</div>

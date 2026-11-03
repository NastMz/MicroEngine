# Class AssetLoaderRegistry

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Content](MicroEngine.Content.md)  
Assembly: MicroEngine.Content.dll  
Guide: [Assets](../manual/assets.md#handles-loaders-and-content-sources)  

The loaders of a store, one per kind. The engine registers its own (images, atlases, fonts, sounds, music); a game
or module adds kinds of its own, or replaces one by registering another loader with the same kind before the first
load of that kind.

```csharp
public sealed class AssetLoaderRegistry
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[AssetLoaderRegistry](MicroEngine.Content.AssetLoaderRegistry.md)

## Constructors

### <a id="MicroEngine_Content_AssetLoaderRegistry__ctor"></a> AssetLoaderRegistry\(\)

```csharp
public AssetLoaderRegistry()
```

## Properties

### <a id="MicroEngine_Content_AssetLoaderRegistry_Kinds"></a> Kinds

The kinds registered, in ordinal order.

```csharp
public IReadOnlyList<string> Kinds { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

## Methods

### <a id="MicroEngine_Content_AssetLoaderRegistry_Add_MicroEngine_Content_IAssetLoader_"></a> Add\(IAssetLoader\)

Registers <code class="paramref">loader</code>, replacing any loader of the same kind.

```csharp
public AssetLoaderRegistry Add(IAssetLoader loader)
```

#### Parameters

`loader` [IAssetLoader](MicroEngine.Content.IAssetLoader.md)

#### Returns

 [AssetLoaderRegistry](MicroEngine.Content.AssetLoaderRegistry.md)

This registry, for chaining.

### <a id="MicroEngine_Content_AssetLoaderRegistry_Get_System_String_"></a> Get\(string\)

The loader of <code class="paramref">kind</code>.

```csharp
public IAssetLoader Get(string kind)
```

#### Parameters

`kind` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [IAssetLoader](MicroEngine.Content.IAssetLoader.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

No loader of that kind is registered; the message lists those that are.

### <a id="MicroEngine_Content_AssetLoaderRegistry_KindOf_System_String_"></a> KindOf\(string\)

The kind of the loader whose [Extensions](MicroEngine.Content.IAssetLoader.md#MicroEngine_Content_IAssetLoader_Extensions) holds <code class="paramref">path</code>'s extension (the first registered), or null.

```csharp
public string? KindOf(string path)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Content_AssetLoaderRegistry_TryGet_System_String_MicroEngine_Content_IAssetLoader__"></a> TryGet\(string, out IAssetLoader?\)

The loader of <code class="paramref">kind</code>, if one is registered.

```csharp
public bool TryGet(string kind, out IAssetLoader? loader)
```

#### Parameters

`kind` [string](https://learn.microsoft.com/dotnet/api/system.string)

`loader` [IAssetLoader](MicroEngine.Content.IAssetLoader.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

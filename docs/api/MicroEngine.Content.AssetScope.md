# Class AssetScope

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Content](MicroEngine.Content.md)  
Assembly: MicroEngine.Content.dll  
Guide: [Assets](../manual/assets.md#handles-loaders-and-content-sources)  

An owner of assets: what a game, a level or a scene holds. Every scope that asks for an asset holds it, whichever
scope loaded it first, so sibling scopes share one object; disposing a scope (or [Release](MicroEngine.Content.AssetScope.md#MicroEngine_Content_AssetScope_Release_System_String_))
releases its holds, and an asset is disposed when no scope holds it any more. Child scopes are released with their
parent.

```csharp
public sealed class AssetScope : IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[AssetScope](MicroEngine.Content.AssetScope.md)

#### Implements

[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

## Properties

### <a id="MicroEngine_Content_AssetScope_Count"></a> Count

How many assets this scope and its children hold, an asset several of them share counting once; on the root
scope, every asset of the store. Allocates nothing, so it can be checked every frame.

```csharp
public int Count { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Content_AssetScope_IsDisposed"></a> IsDisposed

Whether the scope was disposed; it can no longer load or hold anything.

```csharp
public bool IsDisposed { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Content_AssetScope_Loaded"></a> Loaded

The assets this scope and its children hold, each once, in the order each scope asked for them.

```csharp
public IReadOnlyList<AssetRecord> Loaded { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[AssetRecord](MicroEngine.Content.AssetRecord.md)\>

### <a id="MicroEngine_Content_AssetScope_Name"></a> Name

The scope's name for diagnostics (null for the root and unnamed scopes).

```csharp
public string? Name { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Content_AssetScope_Objects"></a> Objects

The objects this scope and its children hold, each once, in the order they were asked for.

```csharp
public IReadOnlyList<object> Objects { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[object](https://learn.microsoft.com/dotnet/api/system.object)\>

### <a id="MicroEngine_Content_AssetScope_Parent"></a> Parent

The scope it was created from; null for the store's root.

```csharp
public AssetScope? Parent { get; }
```

#### Property Value

 [AssetScope](MicroEngine.Content.AssetScope.md)?

### <a id="MicroEngine_Content_AssetScope_Playing"></a> Playing

Whether the game is past its first update; loads from then on count as loads during play
([DuringPlay](MicroEngine.Content.AssetRecord.md#MicroEngine_Content_AssetRecord_DuringPlay)). Setting it sets it on child scopes too; new children inherit it.

```csharp
public bool Playing { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Content_AssetScope_SlowLoadMilliseconds"></a> SlowLoadMilliseconds

A load during play that blocks the game's thread at least this many milliseconds is reported once (to
[Log](MicroEngine.Content.AssetStore.md#MicroEngine_Content_AssetStore_Log)), naming the file (4 by default: a quarter of a 60 Hz frame). Zero or less turns the
reports off.

```csharp
public double SlowLoadMilliseconds { get; set; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Content_AssetScope_Store"></a> Store

The store the scope belongs to.

```csharp
public AssetStore Store { get; }
```

#### Property Value

 [AssetStore](MicroEngine.Content.AssetStore.md)

## Methods

### <a id="MicroEngine_Content_AssetScope_Acquire__1_MicroEngine_Content_AssetKey__"></a> Acquire<T\>\(in AssetKey\)

The asset of <code class="paramref">key</code>, loaded by its kind's loader when no scope has it, and held by this scope from
now on. A hit this scope already holds allocates nothing.

```csharp
public T Acquire<T>(in AssetKey key) where T : class
```

#### Parameters

`key` [AssetKey](MicroEngine.Content.AssetKey.md)

#### Returns

 T

#### Type Parameters

`T` 

#### Exceptions

 [AssetLoadException](MicroEngine.Core.AssetLoadException.md)

The file is missing, wrongly cased or cannot be read as that kind.

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

No loader of that kind is registered, or it makes something other than a <code class="typeparamref">T</code>.

### <a id="MicroEngine_Content_AssetScope_Acquire__1_MicroEngine_Content_AssetKey__System_Func_MicroEngine_Content_AssetLoadContext___0__"></a> Acquire<T\>\(in AssetKey, Func<AssetLoadContext, T\>\)

The asset of <code class="paramref">key</code> made by <code class="paramref">load</code> when no scope has it, held by this scope from now on.

```csharp
public T Acquire<T>(in AssetKey key, Func<AssetLoadContext, T> load) where T : class
```

#### Parameters

`key` [AssetKey](MicroEngine.Content.AssetKey.md)

`load` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[AssetLoadContext](MicroEngine.Content.AssetLoadContext.md), T\>

#### Returns

 T

#### Type Parameters

`T` 

#### Exceptions

 [AssetLoadException](MicroEngine.Core.AssetLoadException.md)

<code class="paramref">load</code> failed; the message names the asset.

### <a id="MicroEngine_Content_AssetScope_Add__1_MicroEngine_Content_AssetKey____0_System_Double_System_Nullable_System_Double__"></a> Add<T\>\(in AssetKey, T, double, double?\)

Stores <code class="paramref">value</code>, made elsewhere (decoded on a worker, uploaded later), under <code class="paramref">key</code>,
held by this scope. The store disposes it when the last scope releases it.

```csharp
public Asset<T> Add<T>(in AssetKey key, T value, double loadMilliseconds, double? blockingMilliseconds = null) where T : class
```

#### Parameters

`key` [AssetKey](MicroEngine.Content.AssetKey.md)

Its key.

`value` T

The asset.

`loadMilliseconds` [double](https://learn.microsoft.com/dotnet/api/system.double)

The whole load, work on other threads included.

`blockingMilliseconds` [double](https://learn.microsoft.com/dotnet/api/system.double)?

The part spent on the game's thread, which is what a player sees as a hitch; the whole load when null.

#### Returns

 [Asset](MicroEngine.Content.Asset\-1.md)<T\>

#### Type Parameters

`T` 

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

An asset is already stored under <code class="paramref">key</code>.

### <a id="MicroEngine_Content_AssetScope_CreateScope_System_String_"></a> CreateScope\(string?\)

A child scope; it is released with this one. Use one per level or scene whose assets should be freed when it ends.

```csharp
public AssetScope CreateScope(string? name = null)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Returns

 [AssetScope](MicroEngine.Content.AssetScope.md)

#### Exceptions

 [ObjectDisposedException](https://learn.microsoft.com/dotnet/api/system.objectdisposedexception)

The scope was disposed.

### <a id="MicroEngine_Content_AssetScope_Dispose"></a> Dispose\(\)

Releases every asset this scope and its children hold, disposing those no other scope holds.

```csharp
public void Dispose()
```

### <a id="MicroEngine_Content_AssetScope_Get__1_MicroEngine_Content_Asset___0__"></a> Get<T\>\(Asset<T\>\)

The asset of <code class="paramref">asset</code> (see [Get``1](MicroEngine.Content.AssetStore.md#MicroEngine_Content_AssetStore_Get__1_MicroEngine_Content_Asset___0__)).

```csharp
public T Get<T>(Asset<T> asset) where T : class
```

#### Parameters

`asset` [Asset](MicroEngine.Content.Asset\-1.md)<T\>

#### Returns

 T

#### Type Parameters

`T` 

### <a id="MicroEngine_Content_AssetScope_HandleOf__1_MicroEngine_Content_AssetKey__"></a> HandleOf<T\>\(in AssetKey\)

The handle of an asset this scope holds under <code class="paramref">key</code>, or [None](MicroEngine.Content.md).

```csharp
public Asset<T> HandleOf<T>(in AssetKey key) where T : class
```

#### Parameters

`key` [AssetKey](MicroEngine.Content.AssetKey.md)

#### Returns

 [Asset](MicroEngine.Content.Asset\-1.md)<T\>

#### Type Parameters

`T` 

### <a id="MicroEngine_Content_AssetScope_HoldLoaded_System_String_System_String_"></a> HoldLoaded\(string, string\)

Whether any scope has <code class="paramref">path</code> loaded as <code class="paramref">kind</code> (with any options); this scope then holds it.

```csharp
public bool HoldLoaded(string kind, string path)
```

#### Parameters

`kind` [string](https://learn.microsoft.com/dotnet/api/system.string)

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Content_AssetScope_IsLoaded_System_String_"></a> IsLoaded\(string\)

Whether this scope or one of its parents holds anything from <code class="paramref">path</code> (in any form).

```csharp
public bool IsLoaded(string path)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Content_AssetScope_Load__1_System_String_System_String_System_Object_System_Int64_"></a> Load<T\>\(string, string, object?, long\)

The asset of kind <code class="paramref">kind</code> at <code class="paramref">path</code>, loaded by the kind's loader the first time
any scope asks for it and held by this scope from now on.

```csharp
public Asset<T> Load<T>(string kind, string path, object? options = null, long variant = 0) where T : class
```

#### Parameters

`kind` [string](https://learn.microsoft.com/dotnet/api/system.string)

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

`options` [object](https://learn.microsoft.com/dotnet/api/system.object)?

`variant` [long](https://learn.microsoft.com/dotnet/api/system.int64)

#### Returns

 [Asset](MicroEngine.Content.Asset\-1.md)<T\>

#### Type Parameters

`T` 

#### Exceptions

 [AssetLoadException](MicroEngine.Core.AssetLoadException.md)

The file is missing, wrongly cased or cannot be read as that kind.

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

No loader of that kind is registered, or it makes something other than a <code class="typeparamref">T</code>.

### <a id="MicroEngine_Content_AssetScope_Release_System_String_"></a> Release\(string\)

Releases what this scope holds from <code class="paramref">path</code> (in any form): each asset is disposed unless another
scope still holds it, and loads again if asked for later. What only other scopes hold is not touched.

```csharp
public bool Release(string path)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether this scope held anything from the file.

### <a id="MicroEngine_Content_AssetScope_Release__1_MicroEngine_Content_Asset___0__"></a> Release<T\>\(Asset<T\>\)

Releases this scope's hold on <code class="paramref">asset</code>; it is disposed when no scope holds it any more.

```csharp
public bool Release<T>(Asset<T> asset) where T : class
```

#### Parameters

`asset` [Asset](MicroEngine.Content.Asset\-1.md)<T\>

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether this scope held it.

#### Type Parameters

`T` 

### <a id="MicroEngine_Content_AssetScope_TryAcquire__1_MicroEngine_Content_AssetKey____0__"></a> TryAcquire<T\>\(in AssetKey, out T?\)

The asset of <code class="paramref">key</code> if any scope has it (this scope then holds it); never loads.

```csharp
public bool TryAcquire<T>(in AssetKey key, out T? value) where T : class
```

#### Parameters

`key` [AssetKey](MicroEngine.Content.AssetKey.md)

`value` T?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Type Parameters

`T`

</div>

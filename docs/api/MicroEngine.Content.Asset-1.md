# Struct Asset<T\>

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Content](MicroEngine.Content.md)  
Assembly: MicroEngine.Content.dll  
Guide: [Assets](../manual/assets.md#handles-loaders-and-content-sources)  

A typed reference to an asset in an [AssetStore](MicroEngine.Content.AssetStore.md): a slot and the generation of the asset that held it.
Once the asset is released by every scope that held it (and disposed), the handle is stale: reading it fails instead
of reaching whatever reuses the slot. <code>default</code> is [None](MicroEngine.Content.md).

```csharp
public readonly struct Asset<T> : IEquatable<Asset<T>> where T : class
```

#### Type Parameters

`T` 

The asset's type (a texture, an audio clip...).

#### Implements

[IEquatable<Asset<T\>\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Content_Asset_1__ctor_System_Int32_System_UInt32_"></a> Asset\(int, uint\)

A typed reference to an asset in an [AssetStore](MicroEngine.Content.AssetStore.md): a slot and the generation of the asset that held it.
Once the asset is released by every scope that held it (and disposed), the handle is stale: reading it fails instead
of reaching whatever reuses the slot. <code>default</code> is [None](MicroEngine.Content.md).

```csharp
public Asset(int Index, uint Generation)
```

#### Parameters

`Index` [int](https://learn.microsoft.com/dotnet/api/system.int32)

The store's slot.

`Generation` [uint](https://learn.microsoft.com/dotnet/api/system.uint32)

The generation of the slot's asset; 0 only for [None](MicroEngine.Content.md).

## Properties

### <a id="MicroEngine_Content_Asset_1_Generation"></a> Generation

The generation of the slot's asset; 0 only for [None](MicroEngine.Content.md).

```csharp
public uint Generation { get; init; }
```

#### Property Value

 [uint](https://learn.microsoft.com/dotnet/api/system.uint32)

### <a id="MicroEngine_Content_Asset_1_Index"></a> Index

The store's slot.

```csharp
public int Index { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Content_Asset_1_IsNone"></a> IsNone

Whether this is [None](MicroEngine.Content.md).

```csharp
public bool IsNone { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Content_Asset_1_None"></a> None

The handle of nothing.

```csharp
public static Asset<T> None { get; }
```

#### Property Value

 [Asset](MicroEngine.Content.Asset\-1.md)<T\>

## Methods

### <a id="MicroEngine_Content_Asset_1_Deconstruct_System_Int32__System_UInt32__"></a> Deconstruct\(out int, out uint\)

```csharp
public void Deconstruct(out int Index, out uint Generation)
```

#### Parameters

`Index` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Generation` [uint](https://learn.microsoft.com/dotnet/api/system.uint32)

### <a id="MicroEngine_Content_Asset_1_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Content_Asset_1_Equals_MicroEngine_Content_Asset__0__"></a> Equals\(Asset<T\>\)

```csharp
public bool Equals(Asset<T> other)
```

#### Parameters

`other` [Asset](MicroEngine.Content.Asset\-1.md)<T\>

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Content_Asset_1_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Content_Asset_1_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Content_Asset_1_op_Equality_MicroEngine_Content_Asset__0__MicroEngine_Content_Asset__0__"></a> operator ==\(Asset<T\>, Asset<T\>\)

```csharp
public static bool operator ==(Asset<T> left, Asset<T> right)
```

#### Parameters

`left` [Asset](MicroEngine.Content.Asset\-1.md)<T\>

`right` [Asset](MicroEngine.Content.Asset\-1.md)<T\>

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Content_Asset_1_op_Inequality_MicroEngine_Content_Asset__0__MicroEngine_Content_Asset__0__"></a> operator \!=\(Asset<T\>, Asset<T\>\)

```csharp
public static bool operator !=(Asset<T> left, Asset<T> right)
```

#### Parameters

`left` [Asset](MicroEngine.Content.Asset\-1.md)<T\>

`right` [Asset](MicroEngine.Content.Asset\-1.md)<T\>

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

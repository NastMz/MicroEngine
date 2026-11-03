# Struct AssetKey

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Content](MicroEngine.Content.md)  
Assembly: MicroEngine.Content.dll  
Guide: [Assets](../manual/assets.md#handles-loaders-and-content-sources)  

What identifies a stored asset: the loader's kind, the content path and the settings it was loaded with. Two
requests with equal keys share one object. A struct of references and values, so a lookup allocates nothing.

```csharp
public readonly struct AssetKey : IEquatable<AssetKey>
```

#### Implements

[IEquatable<AssetKey\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Content_AssetKey__ctor_System_String_System_String_System_Object_System_Int64_"></a> AssetKey\(string, string, object?, long\)

What identifies a stored asset: the loader's kind, the content path and the settings it was loaded with. Two
requests with equal keys share one object. A struct of references and values, so a lookup allocates nothing.

```csharp
public AssetKey(string Kind, string Path, object? Options = null, long Variant = 0)
```

#### Parameters

`Kind` [string](https://learn.microsoft.com/dotnet/api/system.string)

The kind of the loader that makes it ("texture", "sound"...; see [AssetLoaderRegistry](MicroEngine.Content.AssetLoaderRegistry.md)).

`Path` [string](https://learn.microsoft.com/dotnet/api/system.string)

The path in the content source, '/'-separated (normalized by the store, see [Normalize](MicroEngine.Content.AssetPath.md#MicroEngine_Content_AssetPath_Normalize_System_String_)).

`Options` [object](https://learn.microsoft.com/dotnet/api/system.object)?

Load settings compared by value (a record), or null for the loader's defaults.

`Variant` [long](https://learn.microsoft.com/dotnet/api/system.int64)

A small value that also tells loads apart (a sampling filter, for example) without boxing it.

## Properties

### <a id="MicroEngine_Content_AssetKey_Kind"></a> Kind

The kind of the loader that makes it ("texture", "sound"...; see [AssetLoaderRegistry](MicroEngine.Content.AssetLoaderRegistry.md)).

```csharp
public string Kind { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Content_AssetKey_Options"></a> Options

Load settings compared by value (a record), or null for the loader's defaults.

```csharp
public object? Options { get; init; }
```

#### Property Value

 [object](https://learn.microsoft.com/dotnet/api/system.object)?

### <a id="MicroEngine_Content_AssetKey_Path"></a> Path

The path in the content source, '/'-separated (normalized by the store, see [Normalize](MicroEngine.Content.AssetPath.md#MicroEngine_Content_AssetPath_Normalize_System_String_)).

```csharp
public string Path { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Content_AssetKey_Variant"></a> Variant

A small value that also tells loads apart (a sampling filter, for example) without boxing it.

```csharp
public long Variant { get; init; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

## Methods

### <a id="MicroEngine_Content_AssetKey_Deconstruct_System_String__System_String__System_Object__System_Int64__"></a> Deconstruct\(out string, out string, out object?, out long\)

```csharp
public void Deconstruct(out string Kind, out string Path, out object? Options, out long Variant)
```

#### Parameters

`Kind` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Path` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Options` [object](https://learn.microsoft.com/dotnet/api/system.object)?

`Variant` [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Content_AssetKey_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Content_AssetKey_Equals_MicroEngine_Content_AssetKey_"></a> Equals\(AssetKey\)

```csharp
public bool Equals(AssetKey other)
```

#### Parameters

`other` [AssetKey](MicroEngine.Content.AssetKey.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Content_AssetKey_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Content_AssetKey_ToString"></a> ToString\(\)

"kind 'path'" for messages.

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Content_AssetKey_op_Equality_MicroEngine_Content_AssetKey_MicroEngine_Content_AssetKey_"></a> operator ==\(AssetKey, AssetKey\)

```csharp
public static bool operator ==(AssetKey left, AssetKey right)
```

#### Parameters

`left` [AssetKey](MicroEngine.Content.AssetKey.md)

`right` [AssetKey](MicroEngine.Content.AssetKey.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Content_AssetKey_op_Inequality_MicroEngine_Content_AssetKey_MicroEngine_Content_AssetKey_"></a> operator \!=\(AssetKey, AssetKey\)

```csharp
public static bool operator !=(AssetKey left, AssetKey right)
```

#### Parameters

`left` [AssetKey](MicroEngine.Content.AssetKey.md)

`right` [AssetKey](MicroEngine.Content.AssetKey.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

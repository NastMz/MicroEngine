# Struct AssetRecord

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Content](MicroEngine.Content.md)  
Assembly: MicroEngine.Content.dll  
Guide: [Assets](../manual/assets.md#handles-loaders-and-content-sources)  

One asset a scope holds, as [Loaded](MicroEngine.Content.AssetScope.md#MicroEngine_Content_AssetScope_Loaded) lists it.

```csharp
public readonly struct AssetRecord : IEquatable<AssetRecord>
```

#### Implements

[IEquatable<AssetRecord\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Content_AssetRecord__ctor_System_String_System_String_System_Int64_System_Double_System_Boolean_"></a> AssetRecord\(string, string, long, double, bool\)

One asset a scope holds, as [Loaded](MicroEngine.Content.AssetScope.md#MicroEngine_Content_AssetScope_Loaded) lists it.

```csharp
public AssetRecord(string Path, string Kind, long Bytes, double LoadMilliseconds, bool DuringPlay)
```

#### Parameters

`Path` [string](https://learn.microsoft.com/dotnet/api/system.string)

Its content path.

`Kind` [string](https://learn.microsoft.com/dotnet/api/system.string)

Its loader's kind.

`Bytes` [long](https://learn.microsoft.com/dotnet/api/system.int64)

Memory its loader reports for it now (device memory for textures, samples for sounds).

`LoadMilliseconds` [double](https://learn.microsoft.com/dotnet/api/system.double)

Time spent loading it (reading, decoding and creating it).

`DuringPlay` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether it was loaded after the game's first update, when a slow load shows as a hitch.

## Properties

### <a id="MicroEngine_Content_AssetRecord_Bytes"></a> Bytes

Memory its loader reports for it now (device memory for textures, samples for sounds).

```csharp
public long Bytes { get; init; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Content_AssetRecord_DuringPlay"></a> DuringPlay

Whether it was loaded after the game's first update, when a slow load shows as a hitch.

```csharp
public bool DuringPlay { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Content_AssetRecord_Kind"></a> Kind

Its loader's kind.

```csharp
public string Kind { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Content_AssetRecord_LoadMilliseconds"></a> LoadMilliseconds

Time spent loading it (reading, decoding and creating it).

```csharp
public double LoadMilliseconds { get; init; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Content_AssetRecord_Path"></a> Path

Its content path.

```csharp
public string Path { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Content_AssetRecord_Deconstruct_System_String__System_String__System_Int64__System_Double__System_Boolean__"></a> Deconstruct\(out string, out string, out long, out double, out bool\)

```csharp
public void Deconstruct(out string Path, out string Kind, out long Bytes, out double LoadMilliseconds, out bool DuringPlay)
```

#### Parameters

`Path` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Kind` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Bytes` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`LoadMilliseconds` [double](https://learn.microsoft.com/dotnet/api/system.double)

`DuringPlay` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Content_AssetRecord_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Content_AssetRecord_Equals_MicroEngine_Content_AssetRecord_"></a> Equals\(AssetRecord\)

```csharp
public bool Equals(AssetRecord other)
```

#### Parameters

`other` [AssetRecord](MicroEngine.Content.AssetRecord.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Content_AssetRecord_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Content_AssetRecord_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Content_AssetRecord_op_Equality_MicroEngine_Content_AssetRecord_MicroEngine_Content_AssetRecord_"></a> operator ==\(AssetRecord, AssetRecord\)

```csharp
public static bool operator ==(AssetRecord left, AssetRecord right)
```

#### Parameters

`left` [AssetRecord](MicroEngine.Content.AssetRecord.md)

`right` [AssetRecord](MicroEngine.Content.AssetRecord.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Content_AssetRecord_op_Inequality_MicroEngine_Content_AssetRecord_MicroEngine_Content_AssetRecord_"></a> operator \!=\(AssetRecord, AssetRecord\)

```csharp
public static bool operator !=(AssetRecord left, AssetRecord right)
```

#### Parameters

`left` [AssetRecord](MicroEngine.Content.AssetRecord.md)

`right` [AssetRecord](MicroEngine.Content.AssetRecord.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

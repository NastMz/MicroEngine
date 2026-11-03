# Struct AssetStatistics

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

Totals over a scope and its children ([Statistics](MicroEngine.Core.AssetCache.md#MicroEngine_Core_AssetCache_Statistics)).

```csharp
public readonly struct AssetStatistics : IEquatable<AssetStatistics>
```

#### Implements

[IEquatable<AssetStatistics\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Core_AssetStatistics__ctor_System_Int32_System_Int64_System_Double_System_Int32_"></a> AssetStatistics\(int, long, double, int\)

Totals over a scope and its children ([Statistics](MicroEngine.Core.AssetCache.md#MicroEngine_Core_AssetCache_Statistics)).

```csharp
public AssetStatistics(int Count, long TextureBytes, double LoadMilliseconds, int LoadsDuringPlay)
```

#### Parameters

`Count` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Assets loaded and not released.

`TextureBytes` [long](https://learn.microsoft.com/dotnet/api/system.int64)

Memory they take (device memory of their textures, and the samples of sounds).

`LoadMilliseconds` [double](https://learn.microsoft.com/dotnet/api/system.double)

Time spent loading them.

`LoadsDuringPlay` [int](https://learn.microsoft.com/dotnet/api/system.int32)

How many were loaded after the first update.

## Properties

### <a id="MicroEngine_Core_AssetStatistics_Count"></a> Count

Assets loaded and not released.

```csharp
public int Count { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_AssetStatistics_LoadMilliseconds"></a> LoadMilliseconds

Time spent loading them.

```csharp
public double LoadMilliseconds { get; init; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Core_AssetStatistics_LoadsDuringPlay"></a> LoadsDuringPlay

How many were loaded after the first update.

```csharp
public int LoadsDuringPlay { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_AssetStatistics_TextureBytes"></a> TextureBytes

Memory they take (device memory of their textures, and the samples of sounds).

```csharp
public long TextureBytes { get; init; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

## Methods

### <a id="MicroEngine_Core_AssetStatistics_Deconstruct_System_Int32__System_Int64__System_Double__System_Int32__"></a> Deconstruct\(out int, out long, out double, out int\)

```csharp
public void Deconstruct(out int Count, out long TextureBytes, out double LoadMilliseconds, out int LoadsDuringPlay)
```

#### Parameters

`Count` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`TextureBytes` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`LoadMilliseconds` [double](https://learn.microsoft.com/dotnet/api/system.double)

`LoadsDuringPlay` [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_AssetStatistics_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_AssetStatistics_Equals_MicroEngine_Core_AssetStatistics_"></a> Equals\(AssetStatistics\)

```csharp
public bool Equals(AssetStatistics other)
```

#### Parameters

`other` [AssetStatistics](MicroEngine.Core.AssetStatistics.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_AssetStatistics_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_AssetStatistics_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Core_AssetStatistics_op_Equality_MicroEngine_Core_AssetStatistics_MicroEngine_Core_AssetStatistics_"></a> operator ==\(AssetStatistics, AssetStatistics\)

```csharp
public static bool operator ==(AssetStatistics left, AssetStatistics right)
```

#### Parameters

`left` [AssetStatistics](MicroEngine.Core.AssetStatistics.md)

`right` [AssetStatistics](MicroEngine.Core.AssetStatistics.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_AssetStatistics_op_Inequality_MicroEngine_Core_AssetStatistics_MicroEngine_Core_AssetStatistics_"></a> operator \!=\(AssetStatistics, AssetStatistics\)

```csharp
public static bool operator !=(AssetStatistics left, AssetStatistics right)
```

#### Parameters

`left` [AssetStatistics](MicroEngine.Core.AssetStatistics.md)

`right` [AssetStatistics](MicroEngine.Core.AssetStatistics.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

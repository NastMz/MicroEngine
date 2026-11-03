# Struct AssetInfo

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

One loaded asset, as [Loaded](MicroEngine.Core.AssetCache.md#MicroEngine_Core_AssetCache_Loaded) lists it.

```csharp
public readonly struct AssetInfo : IEquatable<AssetInfo>
```

#### Implements

[IEquatable<AssetInfo\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Core_AssetInfo__ctor_System_String_System_String_System_Int64_System_Double_System_Boolean_"></a> AssetInfo\(string, string, long, double, bool\)

One loaded asset, as [Loaded](MicroEngine.Core.AssetCache.md#MicroEngine_Core_AssetCache_Loaded) lists it.

```csharp
public AssetInfo(string Path, string Kind, long Bytes, double LoadMilliseconds, bool DuringPlay)
```

#### Parameters

`Path` [string](https://learn.microsoft.com/dotnet/api/system.string)

Its content path.

`Kind` [string](https://learn.microsoft.com/dotnet/api/system.string)

"texture", "atlas", "font", "folder atlas", or the kind of another loader ([AssetLoaderRegistry](MicroEngine.Content.AssetLoaderRegistry.md)).

`Bytes` [long](https://learn.microsoft.com/dotnet/api/system.int64)

Memory it takes now, as its loader reports it: device memory for textures (font pages grow as glyphs are drawn), decoded samples for sounds, the file for music.

`LoadMilliseconds` [double](https://learn.microsoft.com/dotnet/api/system.double)

Time spent reading, decoding and uploading it.

`DuringPlay` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether it was loaded after the game's first update, when a slow load shows as a hitch.

## Properties

### <a id="MicroEngine_Core_AssetInfo_Bytes"></a> Bytes

Memory it takes now, as its loader reports it: device memory for textures (font pages grow as glyphs are drawn), decoded samples for sounds, the file for music.

```csharp
public long Bytes { get; init; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Core_AssetInfo_DuringPlay"></a> DuringPlay

Whether it was loaded after the game's first update, when a slow load shows as a hitch.

```csharp
public bool DuringPlay { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_AssetInfo_Kind"></a> Kind

"texture", "atlas", "font", "folder atlas", or the kind of another loader ([AssetLoaderRegistry](MicroEngine.Content.AssetLoaderRegistry.md)).

```csharp
public string Kind { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Core_AssetInfo_LoadMilliseconds"></a> LoadMilliseconds

Time spent reading, decoding and uploading it.

```csharp
public double LoadMilliseconds { get; init; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Core_AssetInfo_Path"></a> Path

Its content path.

```csharp
public string Path { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Core_AssetInfo_Deconstruct_System_String__System_String__System_Int64__System_Double__System_Boolean__"></a> Deconstruct\(out string, out string, out long, out double, out bool\)

```csharp
public void Deconstruct(out string Path, out string Kind, out long Bytes, out double LoadMilliseconds, out bool DuringPlay)
```

#### Parameters

`Path` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Kind` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Bytes` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`LoadMilliseconds` [double](https://learn.microsoft.com/dotnet/api/system.double)

`DuringPlay` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_AssetInfo_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_AssetInfo_Equals_MicroEngine_Core_AssetInfo_"></a> Equals\(AssetInfo\)

```csharp
public bool Equals(AssetInfo other)
```

#### Parameters

`other` [AssetInfo](MicroEngine.Core.AssetInfo.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_AssetInfo_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_AssetInfo_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Core_AssetInfo_op_Equality_MicroEngine_Core_AssetInfo_MicroEngine_Core_AssetInfo_"></a> operator ==\(AssetInfo, AssetInfo\)

```csharp
public static bool operator ==(AssetInfo left, AssetInfo right)
```

#### Parameters

`left` [AssetInfo](MicroEngine.Core.AssetInfo.md)

`right` [AssetInfo](MicroEngine.Core.AssetInfo.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_AssetInfo_op_Inequality_MicroEngine_Core_AssetInfo_MicroEngine_Core_AssetInfo_"></a> operator \!=\(AssetInfo, AssetInfo\)

```csharp
public static bool operator !=(AssetInfo left, AssetInfo right)
```

#### Parameters

`left` [AssetInfo](MicroEngine.Core.AssetInfo.md)

`right` [AssetInfo](MicroEngine.Core.AssetInfo.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

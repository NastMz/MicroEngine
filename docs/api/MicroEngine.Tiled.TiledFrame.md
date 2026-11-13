# Struct TiledFrame

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Tiled](MicroEngine.Tiled.md)  
Assembly: MicroEngine.Tiled.dll  
Guide: [Maps from Tiled](../manual/world.md#maps-from-tiled)  

One frame of a tile animation.

```csharp
public readonly struct TiledFrame : IEquatable<TiledFrame>
```

#### Implements

[IEquatable<TiledFrame\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Tiled_TiledFrame__ctor_System_Int32_System_Single_"></a> TiledFrame\(int, float\)

One frame of a tile animation.

```csharp
public TiledFrame(int Gid, float Seconds)
```

#### Parameters

`Gid` [int](https://learn.microsoft.com/dotnet/api/system.int32)

The global id of the tile shown.

`Seconds` [float](https://learn.microsoft.com/dotnet/api/system.single)

How long it is shown.

## Properties

### <a id="MicroEngine_Tiled_TiledFrame_Gid"></a> Gid

The global id of the tile shown.

```csharp
public int Gid { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Tiled_TiledFrame_Seconds"></a> Seconds

How long it is shown.

```csharp
public float Seconds { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_Tiled_TiledFrame_Deconstruct_System_Int32__System_Single__"></a> Deconstruct\(out int, out float\)

```csharp
public void Deconstruct(out int Gid, out float Seconds)
```

#### Parameters

`Gid` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Seconds` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Tiled_TiledFrame_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Tiled_TiledFrame_Equals_MicroEngine_Tiled_TiledFrame_"></a> Equals\(TiledFrame\)

```csharp
public bool Equals(TiledFrame other)
```

#### Parameters

`other` [TiledFrame](MicroEngine.Tiled.TiledFrame.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Tiled_TiledFrame_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Tiled_TiledFrame_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Tiled_TiledFrame_op_Equality_MicroEngine_Tiled_TiledFrame_MicroEngine_Tiled_TiledFrame_"></a> operator ==\(TiledFrame, TiledFrame\)

```csharp
public static bool operator ==(TiledFrame left, TiledFrame right)
```

#### Parameters

`left` [TiledFrame](MicroEngine.Tiled.TiledFrame.md)

`right` [TiledFrame](MicroEngine.Tiled.TiledFrame.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Tiled_TiledFrame_op_Inequality_MicroEngine_Tiled_TiledFrame_MicroEngine_Tiled_TiledFrame_"></a> operator \!=\(TiledFrame, TiledFrame\)

```csharp
public static bool operator !=(TiledFrame left, TiledFrame right)
```

#### Parameters

`left` [TiledFrame](MicroEngine.Tiled.TiledFrame.md)

`right` [TiledFrame](MicroEngine.Tiled.TiledFrame.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

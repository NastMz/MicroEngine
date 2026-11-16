# Struct NavCell

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Navigation](MicroEngine.Navigation.md)  
Assembly: MicroEngine.Navigation.dll  
Guide: [Navigation](../guide/navigation.md)  

A cell of a [NavGrid](MicroEngine.Navigation.NavGrid.md): its column and row, from the top-left cell (0, 0).

```csharp
public readonly struct NavCell : IEquatable<NavCell>
```

#### Implements

[IEquatable<NavCell\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Navigation_NavCell__ctor_System_Int32_System_Int32_"></a> NavCell\(int, int\)

A cell of a [NavGrid](MicroEngine.Navigation.NavGrid.md): its column and row, from the top-left cell (0, 0).

```csharp
public NavCell(int X, int Y)
```

#### Parameters

`X` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Y` [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Properties

### <a id="MicroEngine_Navigation_NavCell_X"></a> X

```csharp
public int X { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Navigation_NavCell_Y"></a> Y

```csharp
public int Y { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Navigation_NavCell_Deconstruct_System_Int32__System_Int32__"></a> Deconstruct\(out int, out int\)

```csharp
public void Deconstruct(out int X, out int Y)
```

#### Parameters

`X` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Y` [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Navigation_NavCell_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Navigation_NavCell_Equals_MicroEngine_Navigation_NavCell_"></a> Equals\(NavCell\)

```csharp
public bool Equals(NavCell other)
```

#### Parameters

`other` [NavCell](MicroEngine.Navigation.NavCell.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Navigation_NavCell_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Navigation_NavCell_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Navigation_NavCell_op_Equality_MicroEngine_Navigation_NavCell_MicroEngine_Navigation_NavCell_"></a> operator ==\(NavCell, NavCell\)

```csharp
public static bool operator ==(NavCell left, NavCell right)
```

#### Parameters

`left` [NavCell](MicroEngine.Navigation.NavCell.md)

`right` [NavCell](MicroEngine.Navigation.NavCell.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Navigation_NavCell_op_Inequality_MicroEngine_Navigation_NavCell_MicroEngine_Navigation_NavCell_"></a> operator \!=\(NavCell, NavCell\)

```csharp
public static bool operator !=(NavCell left, NavCell right)
```

#### Parameters

`left` [NavCell](MicroEngine.Navigation.NavCell.md)

`right` [NavCell](MicroEngine.Navigation.NavCell.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

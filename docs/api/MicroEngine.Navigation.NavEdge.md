# Struct NavEdge

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Navigation](MicroEngine.Navigation.md)  
Assembly: MicroEngine.Navigation.dll  
Guide: [Navigation](../guide/navigation.md)  

A step of a graph: the node it leads to, what it costs, and a number the game gives its own meaning.

```csharp
public readonly struct NavEdge : IEquatable<NavEdge>
```

#### Implements

[IEquatable<NavEdge\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Navigation_NavEdge__ctor_System_Int32_System_Single_System_Int32_"></a> NavEdge\(int, float, int\)

A step of a graph: the node it leads to, what it costs, and a number the game gives its own meaning.

```csharp
public NavEdge(int To, float Cost, int Tag = 0)
```

#### Parameters

`To` [int](https://learn.microsoft.com/dotnet/api/system.int32)

The node the step arrives at.

`Cost` [float](https://learn.microsoft.com/dotnet/api/system.single)

What the step costs; zero or more.

`Tag` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Free for the game: how the step is crossed (a walk, a jump, a ladder, a door). The search never reads it.

## Properties

### <a id="MicroEngine_Navigation_NavEdge_Cost"></a> Cost

What the step costs; zero or more.

```csharp
public float Cost { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Navigation_NavEdge_Tag"></a> Tag

Free for the game: how the step is crossed (a walk, a jump, a ladder, a door). The search never reads it.

```csharp
public int Tag { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Navigation_NavEdge_To"></a> To

The node the step arrives at.

```csharp
public int To { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Navigation_NavEdge_Deconstruct_System_Int32__System_Single__System_Int32__"></a> Deconstruct\(out int, out float, out int\)

```csharp
public void Deconstruct(out int To, out float Cost, out int Tag)
```

#### Parameters

`To` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Cost` [float](https://learn.microsoft.com/dotnet/api/system.single)

`Tag` [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Navigation_NavEdge_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Navigation_NavEdge_Equals_MicroEngine_Navigation_NavEdge_"></a> Equals\(NavEdge\)

```csharp
public bool Equals(NavEdge other)
```

#### Parameters

`other` [NavEdge](MicroEngine.Navigation.NavEdge.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Navigation_NavEdge_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Navigation_NavEdge_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Navigation_NavEdge_op_Equality_MicroEngine_Navigation_NavEdge_MicroEngine_Navigation_NavEdge_"></a> operator ==\(NavEdge, NavEdge\)

```csharp
public static bool operator ==(NavEdge left, NavEdge right)
```

#### Parameters

`left` [NavEdge](MicroEngine.Navigation.NavEdge.md)

`right` [NavEdge](MicroEngine.Navigation.NavEdge.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Navigation_NavEdge_op_Inequality_MicroEngine_Navigation_NavEdge_MicroEngine_Navigation_NavEdge_"></a> operator \!=\(NavEdge, NavEdge\)

```csharp
public static bool operator !=(NavEdge left, NavEdge right)
```

#### Parameters

`left` [NavEdge](MicroEngine.Navigation.NavEdge.md)

`right` [NavEdge](MicroEngine.Navigation.NavEdge.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

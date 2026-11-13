# Class TiledProperty

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Tiled](MicroEngine.Tiled.md)  
Assembly: MicroEngine.Tiled.dll  
Guide: [Maps from Tiled](../manual/world.md#maps-from-tiled)  

One custom property: its name, type and value (see [TiledPropertyType](MicroEngine.Tiled.TiledPropertyType.md) for the value's type).

```csharp
public sealed record TiledProperty : IEquatable<TiledProperty>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[TiledProperty](MicroEngine.Tiled.TiledProperty.md)

#### Implements

[IEquatable<TiledProperty\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Tiled_TiledProperty__ctor_System_String_MicroEngine_Tiled_TiledPropertyType_System_Object_System_String_"></a> TiledProperty\(string, TiledPropertyType, object, string?\)

One custom property: its name, type and value (see [TiledPropertyType](MicroEngine.Tiled.TiledPropertyType.md) for the value's type).

```csharp
public TiledProperty(string Name, TiledPropertyType Type, object Value, string? CustomType = null)
```

#### Parameters

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)

The property's name.

`Type` [TiledPropertyType](MicroEngine.Tiled.TiledPropertyType.md)

The kind of value.

`Value` [object](https://learn.microsoft.com/dotnet/api/system.object)

The value.

`CustomType` [string](https://learn.microsoft.com/dotnet/api/system.string)?

The name of the custom enum or class, for [Enum](MicroEngine.Tiled.TiledPropertyType.md) and [Class](MicroEngine.Tiled.TiledPropertyType.md); otherwise null.

## Properties

### <a id="MicroEngine_Tiled_TiledProperty_CustomType"></a> CustomType

The name of the custom enum or class, for [Enum](MicroEngine.Tiled.TiledPropertyType.md) and [Class](MicroEngine.Tiled.TiledPropertyType.md); otherwise null.

```csharp
public string? CustomType { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Tiled_TiledProperty_Name"></a> Name

The property's name.

```csharp
public string Name { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Tiled_TiledProperty_Type"></a> Type

The kind of value.

```csharp
public TiledPropertyType Type { get; init; }
```

#### Property Value

 [TiledPropertyType](MicroEngine.Tiled.TiledPropertyType.md)

### <a id="MicroEngine_Tiled_TiledProperty_Value"></a> Value

The value.

```csharp
public object Value { get; init; }
```

#### Property Value

 [object](https://learn.microsoft.com/dotnet/api/system.object)

## Methods

### <a id="MicroEngine_Tiled_TiledProperty__Clone__"></a> <Clone\>$\(\)

```csharp
public TiledProperty <Clone>$()
```

#### Returns

 [TiledProperty](MicroEngine.Tiled.TiledProperty.md)

### <a id="MicroEngine_Tiled_TiledProperty_Deconstruct_System_String__MicroEngine_Tiled_TiledPropertyType__System_Object__System_String__"></a> Deconstruct\(out string, out TiledPropertyType, out object, out string?\)

```csharp
public void Deconstruct(out string Name, out TiledPropertyType Type, out object Value, out string? CustomType)
```

#### Parameters

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Type` [TiledPropertyType](MicroEngine.Tiled.TiledPropertyType.md)

`Value` [object](https://learn.microsoft.com/dotnet/api/system.object)

`CustomType` [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Tiled_TiledProperty_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Tiled_TiledProperty_Equals_MicroEngine_Tiled_TiledProperty_"></a> Equals\(TiledProperty?\)

```csharp
public bool Equals(TiledProperty? other)
```

#### Parameters

`other` [TiledProperty](MicroEngine.Tiled.TiledProperty.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Tiled_TiledProperty_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Tiled_TiledProperty_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Tiled_TiledProperty_op_Equality_MicroEngine_Tiled_TiledProperty_MicroEngine_Tiled_TiledProperty_"></a> operator ==\(TiledProperty?, TiledProperty?\)

```csharp
public static bool operator ==(TiledProperty? left, TiledProperty? right)
```

#### Parameters

`left` [TiledProperty](MicroEngine.Tiled.TiledProperty.md)?

`right` [TiledProperty](MicroEngine.Tiled.TiledProperty.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Tiled_TiledProperty_op_Inequality_MicroEngine_Tiled_TiledProperty_MicroEngine_Tiled_TiledProperty_"></a> operator \!=\(TiledProperty?, TiledProperty?\)

```csharp
public static bool operator !=(TiledProperty? left, TiledProperty? right)
```

#### Parameters

`left` [TiledProperty](MicroEngine.Tiled.TiledProperty.md)?

`right` [TiledProperty](MicroEngine.Tiled.TiledProperty.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

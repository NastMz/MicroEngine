# Class TiledProperties

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Tiled](MicroEngine.Tiled.md)  
Assembly: MicroEngine.Tiled.dll  
Guide: [Maps from Tiled](../manual/world.md#maps-from-tiled)  

The custom properties of a map, layer, tileset, tile or object, read as data: the engine gives no property a
meaning. Each getter returns its fallback when the property is missing, and throws when it exists with another type.

```csharp
public sealed class TiledProperties : IReadOnlyCollection<TiledProperty>, IEnumerable<TiledProperty>, IEnumerable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[TiledProperties](MicroEngine.Tiled.TiledProperties.md)

#### Implements

[IReadOnlyCollection<TiledProperty\>](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlycollection\-1), 
[IEnumerable<TiledProperty\>](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1), 
[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.ienumerable)

## Constructors

### <a id="MicroEngine_Tiled_TiledProperties__ctor_System_Collections_Generic_IEnumerable_MicroEngine_Tiled_TiledProperty__"></a> TiledProperties\(IEnumerable<TiledProperty\>\)

Creates a set of properties; a name given twice keeps its last value.

```csharp
public TiledProperties(IEnumerable<TiledProperty> properties)
```

#### Parameters

`properties` [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<[TiledProperty](MicroEngine.Tiled.TiledProperty.md)\>

## Properties

### <a id="MicroEngine_Tiled_TiledProperties_Count"></a> Count

How many properties there are.

```csharp
public int Count { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Tiled_TiledProperties_Empty"></a> Empty

No properties.

```csharp
public static TiledProperties Empty { get; }
```

#### Property Value

 [TiledProperties](MicroEngine.Tiled.TiledProperties.md)

### <a id="MicroEngine_Tiled_TiledProperties_Item_System_String_"></a> this\[string\]

The property called <code class="paramref">name</code>.

```csharp
public TiledProperty this[string name] { get; }
```

#### Property Value

 [TiledProperty](MicroEngine.Tiled.TiledProperty.md)

#### Exceptions

 [KeyNotFoundException](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception)

There is none; the message lists those there are.

## Methods

### <a id="MicroEngine_Tiled_TiledProperties_Bool_System_String_System_Boolean_"></a> Bool\(string, bool\)

A true-or-false property.

```csharp
public bool Bool(string name, bool fallback = false)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`fallback` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Exceptions

 [InvalidCastException](https://learn.microsoft.com/dotnet/api/system.invalidcastexception)

The property holds another type.

### <a id="MicroEngine_Tiled_TiledProperties_Class_System_String_"></a> Class\(string\)

A class property's members ([Empty](MicroEngine.Tiled.TiledProperties.md#MicroEngine_Tiled_TiledProperties_Empty) when it is missing).

```csharp
public TiledProperties Class(string name)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [TiledProperties](MicroEngine.Tiled.TiledProperties.md)

#### Exceptions

 [InvalidCastException](https://learn.microsoft.com/dotnet/api/system.invalidcastexception)

The property holds another type.

### <a id="MicroEngine_Tiled_TiledProperties_Color_System_String_MicroEngine_Color_"></a> Color\(string, Color\)

A colour property.

```csharp
public Color Color(string name, Color fallback = default)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`fallback` [Color](MicroEngine.Color.md)

#### Returns

 [Color](MicroEngine.Color.md)

#### Exceptions

 [InvalidCastException](https://learn.microsoft.com/dotnet/api/system.invalidcastexception)

The property holds another type.

### <a id="MicroEngine_Tiled_TiledProperties_Float_System_String_System_Single_"></a> Float\(string, float\)

A number property; a whole-number property converts.

```csharp
public float Float(string name, float fallback = 0)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`fallback` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [InvalidCastException](https://learn.microsoft.com/dotnet/api/system.invalidcastexception)

The property holds another type.

### <a id="MicroEngine_Tiled_TiledProperties_GetEnumerator"></a> GetEnumerator\(\)

```csharp
public IEnumerator<TiledProperty> GetEnumerator()
```

#### Returns

 [IEnumerator](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerator\-1)<[TiledProperty](MicroEngine.Tiled.TiledProperty.md)\>

### <a id="MicroEngine_Tiled_TiledProperties_Has_System_String_"></a> Has\(string\)

Whether a property called <code class="paramref">name</code> exists.

```csharp
public bool Has(string name)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Tiled_TiledProperties_Int_System_String_System_Int32_"></a> Int\(string, int\)

A whole-number or object property.

```csharp
public int Int(string name, int fallback = 0)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`fallback` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Exceptions

 [InvalidCastException](https://learn.microsoft.com/dotnet/api/system.invalidcastexception)

The property holds another type.

### <a id="MicroEngine_Tiled_TiledProperties_String_System_String_System_String_"></a> String\(string, string?\)

A text, enum or file property.

```csharp
public string? String(string name, string? fallback = null)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`fallback` [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Exceptions

 [InvalidCastException](https://learn.microsoft.com/dotnet/api/system.invalidcastexception)

The property holds another type.

### <a id="MicroEngine_Tiled_TiledProperties_ToString"></a> ToString\(\)

The properties as "name=value" pairs, for diagnostics.

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Tiled_TiledProperties_TryGet_System_String_MicroEngine_Tiled_TiledProperty__"></a> TryGet\(string, out TiledProperty?\)

The property called <code class="paramref">name</code>, if it exists.

```csharp
public bool TryGet(string name, out TiledProperty? property)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`property` [TiledProperty](MicroEngine.Tiled.TiledProperty.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

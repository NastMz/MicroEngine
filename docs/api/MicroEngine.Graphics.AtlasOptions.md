# Class AtlasOptions

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

How atlas files are read.

```csharp
public sealed record AtlasOptions : IEquatable<AtlasOptions>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[AtlasOptions](MicroEngine.Graphics.AtlasOptions.md)

#### Implements

[IEquatable<AtlasOptions\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_AtlasOptions__ctor_System_Numerics_Vector2_System_Boolean_"></a> AtlasOptions\(Vector2, bool\)

How atlas files are read.

```csharp
public AtlasOptions(Vector2 Pivot = default, bool KeepExtensions = false)
```

#### Parameters

`Pivot` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

The anchor of frames whose file gives none, as a fraction of the untrimmed frame: (0, 0) is its top-left (the default),
(0.5, 1) the middle of its bottom edge (feet).

`KeepExtensions` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Keeps ".png", ".aseprite" and similar endings in frame names (false by default: "hero.png" is "hero").

## Properties

### <a id="MicroEngine_Graphics_AtlasOptions_Default"></a> Default

Top-left anchors, names without image extensions.

```csharp
public static AtlasOptions Default { get; }
```

#### Property Value

 [AtlasOptions](MicroEngine.Graphics.AtlasOptions.md)

### <a id="MicroEngine_Graphics_AtlasOptions_KeepExtensions"></a> KeepExtensions

Keeps ".png", ".aseprite" and similar endings in frame names (false by default: "hero.png" is "hero").

```csharp
public bool KeepExtensions { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_AtlasOptions_Pivot"></a> Pivot

The anchor of frames whose file gives none, as a fraction of the untrimmed frame: (0, 0) is its top-left (the default),
(0.5, 1) the middle of its bottom edge (feet).

```csharp
public Vector2 Pivot { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

## Methods

### <a id="MicroEngine_Graphics_AtlasOptions__Clone__"></a> <Clone\>$\(\)

```csharp
public AtlasOptions <Clone>$()
```

#### Returns

 [AtlasOptions](MicroEngine.Graphics.AtlasOptions.md)

### <a id="MicroEngine_Graphics_AtlasOptions_Deconstruct_System_Numerics_Vector2__System_Boolean__"></a> Deconstruct\(out Vector2, out bool\)

```csharp
public void Deconstruct(out Vector2 Pivot, out bool KeepExtensions)
```

#### Parameters

`Pivot` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`KeepExtensions` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_AtlasOptions_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_AtlasOptions_Equals_MicroEngine_Graphics_AtlasOptions_"></a> Equals\(AtlasOptions?\)

```csharp
public bool Equals(AtlasOptions? other)
```

#### Parameters

`other` [AtlasOptions](MicroEngine.Graphics.AtlasOptions.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_AtlasOptions_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_AtlasOptions_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_AtlasOptions_op_Equality_MicroEngine_Graphics_AtlasOptions_MicroEngine_Graphics_AtlasOptions_"></a> operator ==\(AtlasOptions?, AtlasOptions?\)

```csharp
public static bool operator ==(AtlasOptions? left, AtlasOptions? right)
```

#### Parameters

`left` [AtlasOptions](MicroEngine.Graphics.AtlasOptions.md)?

`right` [AtlasOptions](MicroEngine.Graphics.AtlasOptions.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_AtlasOptions_op_Inequality_MicroEngine_Graphics_AtlasOptions_MicroEngine_Graphics_AtlasOptions_"></a> operator \!=\(AtlasOptions?, AtlasOptions?\)

```csharp
public static bool operator !=(AtlasOptions? left, AtlasOptions? right)
```

#### Parameters

`left` [AtlasOptions](MicroEngine.Graphics.AtlasOptions.md)?

`right` [AtlasOptions](MicroEngine.Graphics.AtlasOptions.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

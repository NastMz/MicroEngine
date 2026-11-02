# Class AtlasAnimation

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.dll  
Guide: [Graphics](../manual/rendering.md)  

A named run of frames (an Aseprite frame tag).

```csharp
public sealed record AtlasAnimation : IEquatable<AtlasAnimation>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[AtlasAnimation](MicroEngine.Graphics.AtlasAnimation.md)

#### Implements

[IEquatable<AtlasAnimation\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_AtlasAnimation__ctor_System_String_System_Int32_System_Int32_MicroEngine_Graphics_AnimationDirection_"></a> AtlasAnimation\(string, int, int, AnimationDirection\)

A named run of frames (an Aseprite frame tag).

```csharp
public AtlasAnimation(string Name, int From, int To, AnimationDirection Direction)
```

#### Parameters

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)

The tag's name.

`From` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Index of the first frame in [Regions](MicroEngine.Graphics.AtlasData.md#MicroEngine_Graphics_AtlasData_Regions).

`To` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Index of the last frame, inclusive.

`Direction` [AnimationDirection](MicroEngine.Graphics.AnimationDirection.md)

How the frames play.

## Properties

### <a id="MicroEngine_Graphics_AtlasAnimation_Direction"></a> Direction

How the frames play.

```csharp
public AnimationDirection Direction { get; init; }
```

#### Property Value

 [AnimationDirection](MicroEngine.Graphics.AnimationDirection.md)

### <a id="MicroEngine_Graphics_AtlasAnimation_From"></a> From

Index of the first frame in [Regions](MicroEngine.Graphics.AtlasData.md#MicroEngine_Graphics_AtlasData_Regions).

```csharp
public int From { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_AtlasAnimation_Name"></a> Name

The tag's name.

```csharp
public string Name { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Graphics_AtlasAnimation_To"></a> To

Index of the last frame, inclusive.

```csharp
public int To { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Graphics_AtlasAnimation__Clone__"></a> <Clone\>$\(\)

```csharp
public AtlasAnimation <Clone>$()
```

#### Returns

 [AtlasAnimation](MicroEngine.Graphics.AtlasAnimation.md)

### <a id="MicroEngine_Graphics_AtlasAnimation_Deconstruct_System_String__System_Int32__System_Int32__MicroEngine_Graphics_AnimationDirection__"></a> Deconstruct\(out string, out int, out int, out AnimationDirection\)

```csharp
public void Deconstruct(out string Name, out int From, out int To, out AnimationDirection Direction)
```

#### Parameters

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`From` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`To` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Direction` [AnimationDirection](MicroEngine.Graphics.AnimationDirection.md)

### <a id="MicroEngine_Graphics_AtlasAnimation_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_AtlasAnimation_Equals_MicroEngine_Graphics_AtlasAnimation_"></a> Equals\(AtlasAnimation?\)

```csharp
public bool Equals(AtlasAnimation? other)
```

#### Parameters

`other` [AtlasAnimation](MicroEngine.Graphics.AtlasAnimation.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_AtlasAnimation_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_AtlasAnimation_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_AtlasAnimation_op_Equality_MicroEngine_Graphics_AtlasAnimation_MicroEngine_Graphics_AtlasAnimation_"></a> operator ==\(AtlasAnimation?, AtlasAnimation?\)

```csharp
public static bool operator ==(AtlasAnimation? left, AtlasAnimation? right)
```

#### Parameters

`left` [AtlasAnimation](MicroEngine.Graphics.AtlasAnimation.md)?

`right` [AtlasAnimation](MicroEngine.Graphics.AtlasAnimation.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_AtlasAnimation_op_Inequality_MicroEngine_Graphics_AtlasAnimation_MicroEngine_Graphics_AtlasAnimation_"></a> operator \!=\(AtlasAnimation?, AtlasAnimation?\)

```csharp
public static bool operator !=(AtlasAnimation? left, AtlasAnimation? right)
```

#### Parameters

`left` [AtlasAnimation](MicroEngine.Graphics.AtlasAnimation.md)?

`right` [AtlasAnimation](MicroEngine.Graphics.AtlasAnimation.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

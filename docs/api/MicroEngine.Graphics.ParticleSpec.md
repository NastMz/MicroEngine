# Class ParticleSpec

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

How a burst of particles looks and moves. Ranges are (min, max).

```csharp
public sealed record ParticleSpec : IEquatable<ParticleSpec>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ParticleSpec](MicroEngine.Graphics.ParticleSpec.md)

#### Implements

[IEquatable<ParticleSpec\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_ParticleSpec__ctor"></a> ParticleSpec\(\)

```csharp
public ParticleSpec()
```

## Properties

### <a id="MicroEngine_Graphics_ParticleSpec_Angle"></a> Angle

Emission angle range in radians (0 = right, -PI/2 = up).

```csharp
public (float Min, float Max) Angle { get; init; }
```

#### Property Value

 \([float](https://learn.microsoft.com/dotnet/api/system.single) [Min](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.single,system.single\-.min), [float](https://learn.microsoft.com/dotnet/api/system.single) [Max](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.single,system.single\-.max)\)

### <a id="MicroEngine_Graphics_ParticleSpec_Drag"></a> Drag

Velocity multiplier per second (1 = no drag).

```csharp
public float Drag { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_ParticleSpec_EndColor"></a> EndColor

Colour at death, blended linearly from [StartColor](MicroEngine.Graphics.ParticleSpec.md#MicroEngine_Graphics_ParticleSpec_StartColor) (transparent white by default).

```csharp
public Color EndColor { get; init; }
```

#### Property Value

 Color

### <a id="MicroEngine_Graphics_ParticleSpec_Gravity"></a> Gravity

Downward acceleration in pixels per second squared (0 by default; negative values rise).

```csharp
public float Gravity { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_ParticleSpec_Life"></a> Life

Lifetime range in seconds (0.3 to 0.6 by default).

```csharp
public (float Min, float Max) Life { get; init; }
```

#### Property Value

 \([float](https://learn.microsoft.com/dotnet/api/system.single) [Min](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.single,system.single\-.min), [float](https://learn.microsoft.com/dotnet/api/system.single) [Max](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.single,system.single\-.max)\)

### <a id="MicroEngine_Graphics_ParticleSpec_Round"></a> Round

Draws circles instead of pixel-aligned squares.

```csharp
public bool Round { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_ParticleSpec_Size"></a> Size

Size in pixels at birth and at death (2 to 1 by default). Particles smaller than 0.2 pixels are not drawn.

```csharp
public (float Start, float End) Size { get; init; }
```

#### Property Value

 \([float](https://learn.microsoft.com/dotnet/api/system.single) [Start](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.single,system.single\-.start), [float](https://learn.microsoft.com/dotnet/api/system.single) [End](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.single,system.single\-.end)\)

### <a id="MicroEngine_Graphics_ParticleSpec_Speed"></a> Speed

Initial speed range in pixels per second (20 to 60 by default).

```csharp
public (float Min, float Max) Speed { get; init; }
```

#### Property Value

 \([float](https://learn.microsoft.com/dotnet/api/system.single) [Min](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.single,system.single\-.min), [float](https://learn.microsoft.com/dotnet/api/system.single) [Max](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.single,system.single\-.max)\)

### <a id="MicroEngine_Graphics_ParticleSpec_Spread"></a> Spread

Random spread of the spawn position around the emit point.

```csharp
public float Spread { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_ParticleSpec_StartColor"></a> StartColor

Colour at birth (white by default).

```csharp
public Color StartColor { get; init; }
```

#### Property Value

 Color

## Methods

### <a id="MicroEngine_Graphics_ParticleSpec__Clone__"></a> <Clone\>$\(\)

```csharp
public ParticleSpec <Clone>$()
```

#### Returns

 [ParticleSpec](MicroEngine.Graphics.ParticleSpec.md)

### <a id="MicroEngine_Graphics_ParticleSpec_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_ParticleSpec_Equals_MicroEngine_Graphics_ParticleSpec_"></a> Equals\(ParticleSpec?\)

```csharp
public bool Equals(ParticleSpec? other)
```

#### Parameters

`other` [ParticleSpec](MicroEngine.Graphics.ParticleSpec.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_ParticleSpec_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_ParticleSpec_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_ParticleSpec_op_Equality_MicroEngine_Graphics_ParticleSpec_MicroEngine_Graphics_ParticleSpec_"></a> operator ==\(ParticleSpec?, ParticleSpec?\)

```csharp
public static bool operator ==(ParticleSpec? left, ParticleSpec? right)
```

#### Parameters

`left` [ParticleSpec](MicroEngine.Graphics.ParticleSpec.md)?

`right` [ParticleSpec](MicroEngine.Graphics.ParticleSpec.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_ParticleSpec_op_Inequality_MicroEngine_Graphics_ParticleSpec_MicroEngine_Graphics_ParticleSpec_"></a> operator \!=\(ParticleSpec?, ParticleSpec?\)

```csharp
public static bool operator !=(ParticleSpec? left, ParticleSpec? right)
```

#### Parameters

`left` [ParticleSpec](MicroEngine.Graphics.ParticleSpec.md)?

`right` [ParticleSpec](MicroEngine.Graphics.ParticleSpec.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>

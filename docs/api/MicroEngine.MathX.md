# Class MathX

Namespace: [MicroEngine](MicroEngine.md)  
Assembly: MicroEngine.dll  
Guide: [Graphics](../manual/rendering.md)  

Small numeric helpers that games repeatedly need for feel and motion.

```csharp
public static class MathX
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[MathX](MicroEngine.MathX.md)

## Methods

### <a id="MicroEngine_MathX_Approach_System_Single_System_Single_System_Single_"></a> Approach\(float, float, float\)

Moves <code class="paramref">value</code> toward <code class="paramref">target</code> by at most <code class="paramref">step</code>.

```csharp
public static float Approach(float value, float target, float step)
```

#### Parameters

`value` [float](https://learn.microsoft.com/dotnet/api/system.single)

`target` [float](https://learn.microsoft.com/dotnet/api/system.single)

`step` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_MathX_Approach_System_Numerics_Vector2_System_Numerics_Vector2_System_Single_"></a> Approach\(Vector2, Vector2, float\)

Moves <code class="paramref">value</code> toward <code class="paramref">target</code> by at most <code class="paramref">step</code> along the straight line.

```csharp
public static Vector2 Approach(Vector2 value, Vector2 target, float step)
```

#### Parameters

`value` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`target` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`step` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_MathX_Damp_System_Single_System_Single_System_Single_System_Single_"></a> Damp\(float, float, float, float\)

Frame-rate independent exponential smoothing; <code class="paramref">sharpness</code> is per second.

```csharp
public static float Damp(float current, float target, float sharpness, float dt)
```

#### Parameters

`current` [float](https://learn.microsoft.com/dotnet/api/system.single)

`target` [float](https://learn.microsoft.com/dotnet/api/system.single)

`sharpness` [float](https://learn.microsoft.com/dotnet/api/system.single)

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_MathX_Damp_System_Numerics_Vector2_System_Numerics_Vector2_System_Single_System_Single_"></a> Damp\(Vector2, Vector2, float, float\)

Frame-rate independent exponential smoothing of a point; <code class="paramref">sharpness</code> is per second.

```csharp
public static Vector2 Damp(Vector2 current, Vector2 target, float sharpness, float dt)
```

#### Parameters

`current` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`target` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`sharpness` [float](https://learn.microsoft.com/dotnet/api/system.single)

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_MathX_FloorDiv_System_Single_System_Int32_"></a> FloorDiv\(float, int\)

The index of the cell of width <code class="paramref">size</code> that contains <code class="paramref">value</code>, rounding toward negative infinity (so -1 is in cell -1).

```csharp
public static int FloorDiv(float value, int size)
```

#### Parameters

`value` [float](https://learn.microsoft.com/dotnet/api/system.single)

`size` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_MathX_FromAngle_System_Single_System_Single_"></a> FromAngle\(float, float\)

A vector of <code class="paramref">length</code> pointing at <code class="paramref">radians</code> (0 = right, π/2 = down on screen).

```csharp
public static Vector2 FromAngle(float radians, float length = 1)
```

#### Parameters

`radians` [float](https://learn.microsoft.com/dotnet/api/system.single)

`length` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_MathX_Lerp_System_Single_System_Single_System_Single_"></a> Lerp\(float, float, float\)

Linear interpolation: <code class="paramref">a</code> at <code class="paramref">t</code> = 0, <code class="paramref">b</code> at 1. <code class="paramref">t</code> is not clamped.

```csharp
public static float Lerp(float a, float b, float t)
```

#### Parameters

`a` [float](https://learn.microsoft.com/dotnet/api/system.single)

`b` [float](https://learn.microsoft.com/dotnet/api/system.single)

`t` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_MathX_PingPong_System_Single_System_Single_"></a> PingPong\(float, float\)

Returns 0..1..0 over a <code class="paramref">period</code>, useful for pulses and bobbing.

```csharp
public static float PingPong(float time, float period)
```

#### Parameters

`time` [float](https://learn.microsoft.com/dotnet/api/system.single)

`period` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_MathX_Wave_System_Single_System_Single_System_Single_"></a> Wave\(float, float, float\)

A sine wave: <code>sin(time × frequency × 2π) × amplitude</code>, with <code class="paramref">frequency</code> in cycles per second.

```csharp
public static float Wave(float time, float frequency, float amplitude = 1)
```

#### Parameters

`time` [float](https://learn.microsoft.com/dotnet/api/system.single)

`frequency` [float](https://learn.microsoft.com/dotnet/api/system.single)

`amplitude` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)
